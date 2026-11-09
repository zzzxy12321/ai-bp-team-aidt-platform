using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Projects;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlEventInboxService(
    AidtDbContext dbContext,
    SqlProjectService projectService,
    IProjectService projectQueryService,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    DataEnvironmentOptions environmentOptions,
    BusinessTimeOptions businessTimeOptions) : IEventInboxService, IAtomicEventInboxConfirmationService
{
    private static readonly string[] SkippedHighRiskActions =
    [
        "付款",
        "质量放行",
        "配方变更",
        "DCS/PLC 设备控制",
        "安全联锁",
        "合同承诺",
        "客户重大赔付"
    ];

    private static readonly HashSet<string> RelationEntityTypes =
        new HashSet<string>(
            [
                "BusinessObject",
                "WorkflowInstance",
                "WorkflowTask",
                "WorkflowNotification",
                "MinutesSource",
                "MinutesActionItem",
                "Attachment",
                "ProjectMilestone",
                "AuditEvent",
                "InboxEvent"
            ],
            StringComparer.Ordinal);

    public async Task<PagedResult<InboxEventDto>> ListAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("event-inbox", AccessAction.Read);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var query = ApplyEventScope(user, includeTestData);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(item => item.Status == status);
        }

        query = query.OrderByDescending(item => item.UpdatedAt).ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var events = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<InboxEventDto>(
            await MapEventsAsync(events, cancellationToken),
            normalizedPage,
            normalizedPageSize,
            total);
    }

    public async Task<InboxEventDto?> GetAsync(
        UserPrincipal user,
        Guid eventId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("event-inbox", AccessAction.Read);
        user.EnsureOrganizationAccess(user.OrganizationId);
        var entity = await ApplyEventScope(user, includeTestData)
            .SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);
        if (entity is not null)
        {
            return (await MapEventsAsync([entity], cancellationToken)).Single();
        }

        var exists = await dbContext.EventInboxes.AsNoTracking().AnyAsync(
            item => item.Id == eventId && item.OrganizationId == user.OrganizationId,
            cancellationToken);
        if (exists)
        {
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "EventInbox",
                    "event.access_denied",
                    "EventInbox",
                    eventId.ToString("D"),
                    traceId,
                    AuditOutcome.Denied,
                    clock.UtcNow),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份不能读取该事件。");
        }

        return null;
    }

    public async Task<IReadOnlyList<InboxSourceOptionDto>> ListSourcesAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("event-inbox", AccessAction.Read);
        user.EnsureOrganizationAccess(user.OrganizationId);
        user.EnsureDepartmentAccess(departmentId);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var sources = new List<InboxSourceOptionDto>();
        var minutes = await dbContext.MinutesSources.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && item.EnvironmentCode == environment)
            .OrderByDescending(item => item.MeetingAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        sources.AddRange(minutes.Select(item => new InboxSourceOptionDto(
            "MinutesSource",
            item.Id.ToString("D"),
            item.Title,
            item.DepartmentId.ToString("D"),
            "听记：" + item.SourceType,
            item.MeetingAt)));

        var actionRows = await (
            from action in dbContext.MinutesActionItems.AsNoTracking()
            join source in dbContext.MinutesSources.AsNoTracking()
                on action.MinutesSourceId equals source.Id
            where source.OrganizationId == user.OrganizationId
                && source.DepartmentId == departmentId
                && source.EnvironmentCode == environment
            orderby action.CreatedAt descending
            select new { Action = action, Source = source }).Take(50).ToListAsync(cancellationToken);
        sources.AddRange(actionRows.Select(item => new InboxSourceOptionDto(
            "MinutesActionItem",
            item.Action.Id.ToString("D"),
            item.Action.Content,
            item.Source.DepartmentId.ToString("D"),
            $"听记行动项：{item.Source.Title} / {item.Action.Speaker} {item.Action.SourceTimestamp}",
            item.Action.CreatedAt)));

        var notifications = await dbContext.WorkflowNotifications.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && (user.IsPlatformAdmin || item.RecipientId == user.UserId))
            .OrderByDescending(item => item.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        sources.AddRange(notifications.Select(item => new InboxSourceOptionDto(
            "WorkflowNotification",
            item.Id.ToString("D"),
            item.Title,
            item.DepartmentId.ToString("D"),
            "流程通知：" + item.Message,
            item.CreatedAt)));

        var files = await dbContext.Attachments.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId
                && item.DepartmentId == departmentId
                && item.EnvironmentCode == environment
                && item.Status == "Active")
            .OrderByDescending(item => item.UpdatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);
        sources.AddRange(files.Select(item => new InboxSourceOptionDto(
            "Attachment",
            item.Id.ToString("D"),
            item.OriginalFileName,
            item.DepartmentId.ToString("D"),
            $"文件：{item.MimeType} / {item.SizeBytes} bytes",
            item.UpdatedAt)));
        return sources.OrderByDescending(item => item.OccurredAt).ToList();
    }

    public async Task<InboxEventDto> ReceiveAsync(
        UserPrincipal user,
        ReceiveInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("event-inbox", AccessAction.Create);
        user.EnsureOrganizationAccess(user.OrganizationId);
        user.EnsureDepartmentAccess(request.DepartmentId);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(request);
        var scope = $"event-inbox:receive:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同事件请求。");
            }

            return await GetAsync(
                user,
                Guid.Parse(existing.ResponseId),
                false,
                traceId,
                cancellationToken) ?? throw new NotFoundException("幂等结果对应的事件不存在。");
        }

        var source = await ResolveSourceAsync(user, request, cancellationToken);
        if (request.AttachmentId.HasValue)
        {
            var attachmentVisible = await dbContext.Attachments.AsNoTracking().AnyAsync(
                item => item.Id == request.AttachmentId.Value
                    && item.OrganizationId == user.OrganizationId
                    && item.DepartmentId == request.DepartmentId,
                cancellationToken);
            if (!attachmentVisible)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "事件附件不属于当前部门。");
            }
        }

        var now = clock.UtcNow;
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var inbox = new EventInboxEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            DepartmentId = request.DepartmentId,
            SourceType = source.SourceType,
            SourceId = request.SourceId,
            AttachmentId = request.AttachmentId,
            Title = DomainChecks.Required(source.Title, "事件标题", 255),
            RawText = DomainChecks.Required(source.Content, "事件原文", 2_000_000),
            Status = nameof(InboxEventStatus.Received),
            SubmittedBy = user.UserId,
            EnvironmentCode = environment,
            IsTestData = DataEnvironment.IsTestEnvironment(environment),
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.EventInboxes.Add(inbox);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "EventInbox",
                    "event.inbox_received",
                    "EventInbox",
                    inbox.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(new
                    {
                        inbox.SourceType,
                        inbox.SourceId,
                        inbox.AttachmentId,
                        inbox.Title,
                        inbox.DepartmentId
                    }, JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "EventInbox",
                    inbox.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return (await MapEventsAsync([inbox], cancellationToken)).Single();
    }

    public async Task<InboxEventDto> StageReceiveWithinTransactionAsync(
        UserPrincipal user,
        ReceiveInboxEventRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("event-inbox", AccessAction.Create);
        user.EnsureOrganizationAccess(user.OrganizationId);
        user.EnsureDepartmentAccess(request.DepartmentId);
        var source = await ResolveSourceAsync(user, request, cancellationToken);
        if (request.AttachmentId.HasValue)
        {
            var attachmentVisible = await dbContext.Attachments.AsNoTracking().AnyAsync(
                item => item.Id == request.AttachmentId.Value
                    && item.OrganizationId == user.OrganizationId
                    && item.DepartmentId == request.DepartmentId,
                cancellationToken);
            if (!attachmentVisible)
            {
                throw new PermissionException(
                    ErrorCodes.DepartmentScopeDenied,
                    "场景来源附件不属于当前部门。");
            }
        }

        var now = clock.UtcNow;
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var inbox = new EventInboxEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            DepartmentId = request.DepartmentId,
            SourceType = source.SourceType,
            SourceId = request.SourceId,
            AttachmentId = request.AttachmentId,
            Title = DomainChecks.Required(source.Title, "事件标题", 255),
            RawText = DomainChecks.Required(source.Content, "事件原文", 2_000_000),
            Status = nameof(InboxEventStatus.Received),
            SubmittedBy = user.UserId,
            EnvironmentCode = environment,
            IsTestData = DataEnvironment.IsTestEnvironment(environment),
            CreatedAt = now,
            UpdatedAt = now
        };
        dbContext.EventInboxes.Add(inbox);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "EventInbox",
                "scenario.event_received",
                "EventInbox",
                inbox.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: JsonSerializer.Serialize(new
                {
                    inbox.SourceType,
                    inbox.SourceId,
                    inbox.AttachmentId,
                    inbox.Title,
                    inbox.DepartmentId,
                    Source = "Scenario"
                }, JsonSerializerOptions.Web)),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapEventsAsync([inbox], cancellationToken)).Single();
    }

    public async Task<InboxEventDto> AnalyzeAsync(
        UserPrincipal user,
        Guid eventId,
        AnalyzeInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("event-inbox", AccessAction.Update);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(request);
        var scope = $"event-inbox:analyze:{eventId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同分析请求。");
            }

            return await GetAsync(user, eventId, false, traceId, cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的事件不存在。");
        }

        var inbox = await LoadAccessibleEventAsync(
            user,
            eventId,
            false,
            traceId,
            cancellationToken);
        if (inbox.Status == nameof(InboxEventStatus.Confirmed))
        {
            throw new ConflictException(ErrorCodes.Conflict, "已确认事件不能重新分析。");
        }

        if (await dbContext.EventSuggestions.AnyAsync(
                item => item.EventInboxId == eventId,
                cancellationToken))
        {
            return (await MapEventsAsync([inbox], cancellationToken)).Single();
        }

        var profileEntity = await dbContext.AgentProfiles.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == request.AgentProfileId && item.IsActive,
            cancellationToken) ?? throw new NotFoundException("Agent 配置不存在或已停用。");
        var profile = MapAgentProfile(profileEntity);
        var assessment = ProjectRules.AssessRisk(inbox.RawText);
        var now = clock.UtcNow;
        var run = AgentRun.Request(
            profile,
            user.UserId,
            AgentActionType.Draft,
            0,
            AgentRiskLevel.Low,
            null,
            now,
            inbox.OrganizationId,
            inbox.DepartmentId,
            inbox.EnvironmentCode,
            inbox.IsTestData,
            "EventInbox",
            model: "aidt-rule-sim");
        if (run.Status == AgentRunStatus.AwaitingConfirmation)
        {
            throw new ConflictException(
                ErrorCodes.AgentConfirmationRequired,
                "当前 AgentProfile 需要真人确认后才可执行事件分析，请选择 A2 业务协同代理。");
        }

        run.MarkRunning();
        var projects = await projectQueryService.ListAsync(
            user,
            1,
            100,
            false,
            cancellationToken);
        var suggestedProject = ChooseProject(projects.Items, inbox);
        var suggestedObjectId = await ChooseBusinessObjectAsync(
            suggestedProject?.Id,
            inbox,
            cancellationToken);
        var suggestedWorkflowVersionId = await ChooseWorkflowVersionAsync(
            suggestedObjectId,
            cancellationToken);
        var suggestedAssigneeId = suggestedProject is null
            ? (Guid?)null
            : Guid.Parse(suggestedProject.OwnerUserId);
        var deadline = ProjectRules.TryExtractDeadline(
                inbox.RawText,
                businessTimeOptions.ResolveTimeZone(),
                businessTimeOptions.DeadlineHour)
            ?? suggestedProject?.PlannedEndDate;
        var priority = ProjectRules.DetectPriority(inbox.RawText, assessment.RiskLevel);
        var missing = ProjectRules.BuildMissingInformation(
            suggestedProject is null ? null : Guid.Parse(suggestedProject.Id),
            suggestedAssigneeId,
            suggestedWorkflowVersionId,
            deadline);
        var evidenceExcerpt = Abbreviate(inbox.RawText, 1200);
        var outputDigest = Sha256(JsonSerializer.Serialize(new
        {
            suggestedProject?.Id,
            suggestedObjectId,
            suggestedAssigneeId,
            suggestedWorkflowVersionId,
            deadline,
            priority,
            assessment.RiskLevel,
            assessment.Reason,
            missing
        }, JsonSerializerOptions.Web));
        var callId = Guid.NewGuid();
        var call = new AgentGatewayCallEntity
        {
            Id = callId,
            AgentRunId = run.Id,
            OrganizationId = inbox.OrganizationId,
            DepartmentId = inbox.DepartmentId,
            EnvironmentCode = inbox.EnvironmentCode,
            Model = "aidt-rule-sim",
            Mode = "RuleSimulator",
            GatewayCallId = $"rule-event-{eventId:N}",
            ExternalModelCalled = false,
            InputDigest = Sha256(inbox.RawText),
            OutputDigest = outputDigest,
            Status = "Succeeded",
            DurationMs = 0,
            CreatedAt = now
        };
        run.RecordGatewayCall(call.Id, call.Mode, false);
        run.Complete();
        var suggestion = new EventSuggestionEntity
        {
            Id = Guid.NewGuid(),
            EventInboxId = eventId,
            AgentRunId = run.Id,
            SuggestedProjectId = suggestedProject is null ? null : Guid.Parse(suggestedProject.Id),
            SuggestedBusinessObjectId = suggestedObjectId,
            SuggestedAssigneeId = suggestedAssigneeId,
            SuggestedWorkflowVersionId = suggestedWorkflowVersionId,
            SuggestedDueAt = deadline,
            Priority = priority,
            RiskLevel = assessment.RiskLevel,
            RequiresHumanBoundary = assessment.RequiresHumanBoundary,
            BoundaryReason = string.IsNullOrWhiteSpace(assessment.Reason) ? null : assessment.Reason,
            MissingInformation = missing,
            EvidenceExcerpt = evidenceExcerpt,
            Status = assessment.RequiresHumanBoundary
                ? nameof(InboxSuggestionStatus.HighRisk)
                : nameof(InboxSuggestionStatus.Suggested),
            CreatedAt = now
        };
        inbox.Status = nameof(InboxEventStatus.AwaitingConfirmation);
        inbox.AnalysisRunId = run.Id;
        inbox.UpdatedAt = now;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.AgentRuns.Add(ToAgentRunEntity(run));
            dbContext.AgentGatewayCalls.Add(call);
            dbContext.EventSuggestions.Add(suggestion);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "EventInbox",
                    "event.agent_analyzed",
                    "EventInbox",
                    inbox.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(new
                    {
                        run.Id,
                        suggestion.RiskLevel,
                        suggestion.RequiresHumanBoundary,
                        suggestion.SuggestedProjectId,
                        suggestion.SuggestedAssigneeId,
                        suggestion.SuggestedWorkflowVersionId,
                        suggestion.MissingInformation,
                        ExternalModelCalled = false
                    }, JsonSerializerOptions.Web),
                    reason: suggestion.BoundaryReason),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    "EventInbox",
                    inbox.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return (await MapEventsAsync([inbox], cancellationToken)).Single();
    }

    public async Task<InboxConfirmationDto> ConfirmAsync(
        UserPrincipal user,
        Guid eventId,
        ConfirmInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("event-inbox", AccessAction.Confirm);
        ValidateIdempotencyKey(idempotencyKey);
        var existing = await GetIdempotentConfirmationAsync(
            user,
            eventId,
            request,
            idempotencyKey,
            traceId,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await ConfirmWithinTransactionAsync(
                user,
                eventId,
                request,
                idempotencyKey,
                traceId,
                startedWorkflowTaskId: null,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<InboxConfirmationDto?> GetIdempotentConfirmationAsync(
        UserPrincipal user,
        Guid eventId,
        ConfirmInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("event-inbox", AccessAction.Confirm);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(request);
        var scope = $"event-inbox:confirm:{eventId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
        {
            throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同确认请求。");
        }

        var existingEvent = await GetAsync(user, eventId, false, traceId, cancellationToken)
            ?? throw new NotFoundException("幂等结果对应的事件不存在。");
        var existingTask = await dbContext.ProjectTasks.AsNoTracking().SingleOrDefaultAsync(
            item => item.EventInboxId == eventId,
            cancellationToken);
        var projectId = request.ProjectId ?? existingTask?.ProjectId
            ?? throw new NotFoundException("幂等结果对应的项目不存在。");
        var existingProject = await projectQueryService.GetAsync(
            user,
            projectId,
            false,
            traceId,
            cancellationToken)
            ?? throw new NotFoundException("幂等结果对应的项目不存在。");
        ProjectTaskDto? idempotentTaskDto = null;
        if (existingTask is not null)
        {
            var assigneeName = await dbContext.Users.AsNoTracking()
                .Where(item => item.Id == existingTask.AssigneeId)
                .Select(item => item.DisplayName)
                .SingleAsync(cancellationToken);
            idempotentTaskDto = SqlProjectService.MapTaskWithAssignee(existingTask, assigneeName);
        }

        var workflowInstanceId = request.WorkflowInstanceId ?? existingTask?.WorkflowInstanceId;
        var workflowEntity = workflowInstanceId.HasValue
            ? await dbContext.WorkflowInstances.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == workflowInstanceId.Value,
                cancellationToken)
            : null;
        return new InboxConfirmationDto(
            existingEvent,
            idempotentTaskDto,
            existingProject.Project,
            workflowEntity is null ? null : SqlProjectService.MapWorkflowInstance(workflowEntity));
    }

    public async Task<InboxConfirmationDto> ConfirmWithinTransactionAsync(
        UserPrincipal user,
        Guid eventId,
        ConfirmInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        Guid? startedWorkflowTaskId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("event-inbox", AccessAction.Confirm);
        ValidateIdempotencyKey(idempotencyKey);
        var existing = await GetIdempotentConfirmationAsync(
            user,
            eventId,
            request,
            idempotencyKey,
            traceId,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var hash = Hash(request);
        var scope = $"event-inbox:confirm:{eventId:D}:{user.UserId:D}";
        var inbox = await LoadAccessibleEventAsync(
            user,
            eventId,
            false,
            traceId,
            cancellationToken);
        if (inbox.Status == nameof(InboxEventStatus.Confirmed))
        {
            throw new ConflictException(ErrorCodes.Conflict, "事件已经确认，不能重复派发。");
        }

        var suggestion = await dbContext.EventSuggestions.SingleOrDefaultAsync(
            item => item.EventInboxId == eventId,
            cancellationToken) ?? throw new ConflictException(
                ErrorCodes.Conflict,
                "事件尚未完成 Agent 分析，不能确认派发。");
        var claimTime = clock.UtcNow;
        var claimed = await dbContext.EventInboxes
            .Where(item => item.Id == eventId
                && item.Status == nameof(InboxEventStatus.AwaitingConfirmation))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(item => item.Status, nameof(InboxEventStatus.Confirmed))
                    .SetProperty(item => item.UpdatedAt, claimTime),
                cancellationToken);
        if (claimed != 1)
        {
            throw new ConflictException(ErrorCodes.Conflict, "事件已经确认，不能重复派发。");
        }

        var projectId = request.ProjectId ?? suggestion.SuggestedProjectId
            ?? throw new ValidationException("确认事件前必须选择项目。");
        var project = await projectService.LoadManagedProjectAsync(
            user,
            projectId,
            traceId,
            cancellationToken);
        if (project.DepartmentId != inbox.DepartmentId)
        {
            throw new PermissionException(
                ErrorCodes.ProjectScopeDenied,
                "事件确认只能关联事件所属部门的项目。");
        }

        if (request.WorkflowInstanceId.HasValue && !startedWorkflowTaskId.HasValue)
        {
            await ValidateWorkflowAsync(
                projectId,
                request.WorkflowInstanceId.Value,
                cancellationToken);
        }

        var assigneeId = request.AssigneeId
            ?? suggestion.SuggestedAssigneeId
            ?? throw new ValidationException("确认事件前必须选择负责人。");
        await projectService.ValidateActiveUserAsync(user.OrganizationId, assigneeId, cancellationToken);
        var isProjectMember = await dbContext.ProjectMembers.AsNoTracking().AnyAsync(
            item => item.BusinessObjectId == projectId
                && item.UserId == assigneeId
                && item.IsActive,
            cancellationToken);
        if (!isProjectMember)
        {
            throw new PermissionException(
                ErrorCodes.ProjectScopeDenied,
                "负责人必须是所选项目的有效成员。");
        }

        var assigneeDepartmentId = await ResolveUserDepartmentAsync(
            assigneeId,
            project.DepartmentId,
            cancellationToken);
        var dueAt = request.DueAt?.ToUniversalTime()
            ?? suggestion.SuggestedDueAt?.ToUniversalTime()
            ?? throw new ValidationException("确认事件前必须选择截止时间。");
        var now = clock.UtcNow;
        ProjectTaskEntity? task = null;
        if (request.CreateTask)
        {
            if (await dbContext.ProjectTasks.AnyAsync(item => item.EventInboxId == eventId, cancellationToken))
            {
                throw new ConflictException(ErrorCodes.Conflict, "该事件已经创建过项目任务。");
            }

            task = new ProjectTaskEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                EventInboxId = eventId,
                AssigneeId = assigneeId,
                DepartmentId = assigneeDepartmentId,
                Title = DomainChecks.Required(
                    request.Content?.Split('\n')[0] ?? inbox.Title,
                    "项目任务标题",
                    255),
                Content = DomainChecks.Required(request.Content ?? inbox.RawText, "项目任务内容", 4000),
                DueAt = dueAt,
                Priority = NormalizePriority(request.Priority ?? suggestion.Priority),
                Status = nameof(ProjectTaskStatus.Pending),
                Version = 1,
                BusinessObjectId = suggestion.SuggestedBusinessObjectId,
                WorkflowInstanceId = request.WorkflowInstanceId,
                WorkflowTaskId = startedWorkflowTaskId,
                EnvironmentCode = inbox.EnvironmentCode,
                IsTestData = inbox.IsTestData,
                CreatedBy = user.UserId,
                CreatedAt = now
            };
            dbContext.ProjectTasks.Add(task);
        }

        AddRelationIfMissing(projectId, "InboxEvent", eventId, "SourceEvent", user.UserId, now);
        if (task is not null)
        {
            AddRelationIfMissing(projectId, "ProjectTask", task.Id, "ActionTask", user.UserId, now);
        }

        if (inbox.SourceId.HasValue && RelationEntityTypes.Contains(MapSourceEntityType(inbox.SourceType)))
        {
            AddRelationIfMissing(
                projectId,
                MapSourceEntityType(inbox.SourceType),
                inbox.SourceId.Value,
                "SourceEvidence",
                user.UserId,
                now);
        }

        if (request.WorkflowInstanceId.HasValue)
        {
            AddRelationIfMissing(
                projectId,
                "WorkflowInstance",
                request.WorkflowInstanceId.Value,
                "ProjectFlow",
                user.UserId,
                now);
        }
        if (startedWorkflowTaskId.HasValue)
        {
            AddRelationIfMissing(
                projectId,
                "WorkflowTask",
                startedWorkflowTaskId.Value,
                "WorkflowAction",
                user.UserId,
                now);
        }

        inbox.Status = nameof(InboxEventStatus.Confirmed);
        inbox.UpdatedAt = now;
        suggestion.Status = nameof(InboxSuggestionStatus.Confirmed);

        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "EventInbox",
                "event.confirmed",
                "EventInbox",
                eventId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: nameof(InboxEventStatus.AwaitingConfirmation),
                after: nameof(InboxEventStatus.Confirmed),
                reason: JsonSerializer.Serialize(
                    new
                    {
                        projectId,
                        assigneeId,
                        dueAt,
                        priority = NormalizePriority(request.Priority ?? suggestion.Priority),
                        request.CreateTask,
                        workflowInstanceId = request.WorkflowInstanceId,
                        workflowTaskId = startedWorkflowTaskId,
                        skippedActions = SkippedHighRiskActions
                    },
                    JsonSerializerOptions.Web)),
            cancellationToken);
        if (task is not null)
        {
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "PMO",
                    "project.task_created",
                    "Project",
                    projectId.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(new
                    {
                        task.Id,
                        task.Title,
                        task.AssigneeId,
                        task.DueAt,
                        task.Priority,
                        EventInboxId = eventId,
                        task.WorkflowInstanceId,
                        task.WorkflowTaskId
                    }, JsonSerializerOptions.Web)),
                cancellationToken);
        }

        await idempotencyStore.AddAsync(
            new IdempotencyEntry(
                Guid.NewGuid(),
                scope,
                idempotencyKey,
                hash,
                "EventInbox",
                eventId.ToString("D"),
                "completed",
                now,
                now.AddDays(30)),
            cancellationToken);

        if (task is not null)
        {
            await projectService.RecalculateProjectProgressAsync(projectId, now, cancellationToken);
        }

        var eventDto = (await MapEventsAsync([inbox], cancellationToken)).Single();
        var projectDto = await projectQueryService.GetAsync(
            user,
            projectId,
            false,
            traceId,
            cancellationToken) ?? throw new NotFoundException("项目不存在。");
        ProjectTaskDto? taskDto = null;
        if (task is not null)
        {
            var assigneeName = await dbContext.Users.AsNoTracking()
                .Where(item => item.Id == task.AssigneeId)
                .Select(item => item.DisplayName)
                .SingleAsync(cancellationToken);
            taskDto = SqlProjectService.MapTaskWithAssignee(task, assigneeName);
        }

        var workflow = request.WorkflowInstanceId.HasValue
            ? await dbContext.WorkflowInstances.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == request.WorkflowInstanceId.Value,
                cancellationToken)
            : null;
        return new InboxConfirmationDto(
            eventDto,
            taskDto,
            projectDto.Project,
            workflow is null ? null : SqlProjectService.MapWorkflowInstance(workflow));
    }

    public async Task<InboxTaskTraceDto?> TraceTaskAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await projectService.LoadProjectTaskEntityAsync(taskId, cancellationToken);
        var project = await projectQueryService.GetAsync(
            user,
            task.ProjectId,
            false,
            "project-task-trace",
            cancellationToken);
        if (project is null)
        {
            return null;
        }

        if (!task.EventInboxId.HasValue)
        {
            return null;
        }

        var inbox = await GetAsync(
            user,
            task.EventInboxId.Value,
            false,
            "project-task-trace",
            cancellationToken);
        if (inbox is null)
        {
            return null;
        }

        var assigneeName = await dbContext.Users.AsNoTracking()
            .Where(item => item.Id == task.AssigneeId)
            .Select(item => item.DisplayName)
            .SingleAsync(cancellationToken);
        return new InboxTaskTraceDto(
            SqlProjectService.MapTaskWithAssignee(task, assigneeName),
            inbox,
            project.Project);
    }

    private IQueryable<EventInboxEntity> ApplyEventScope(
        UserPrincipal user,
        bool includeTestData,
        bool trackChanges = false)
    {
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAllEnvironments = user.IsPlatformAdmin && includeTestData;
        var departmentIds = user.DepartmentIds;
        var query = trackChanges
            ? dbContext.EventInboxes.AsTracking()
            : dbContext.EventInboxes.AsNoTracking();
        return query.Where(item =>
            item.OrganizationId == user.OrganizationId
            && (includeAllEnvironments || item.EnvironmentCode == environment)
            && (user.IsPlatformAdmin
                || item.SubmittedBy == user.UserId
                || departmentIds.Contains(item.DepartmentId)
                || dbContext.ProjectTasks.Any(task =>
                    task.EventInboxId == item.Id
                    && dbContext.ProjectMembers.Any(member =>
                        member.BusinessObjectId == task.ProjectId
                        && member.UserId == user.UserId
                        && member.IsActive))));
    }

    private async Task<EventInboxEntity> LoadAccessibleEventAsync(
        UserPrincipal user,
        Guid eventId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        var entity = await ApplyEventScope(user, includeTestData, trackChanges: true)
            .SingleOrDefaultAsync(item => item.Id == eventId, cancellationToken);
        if (entity is not null)
        {
            return entity;
        }

        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "EventInbox",
                "event.access_denied",
                "EventInbox",
                eventId.ToString("D"),
                traceId,
                AuditOutcome.Denied,
                clock.UtcNow),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份不能访问该事件。");
    }

    private async Task<List<InboxEventDto>> MapEventsAsync(
        List<EventInboxEntity> events,
        CancellationToken cancellationToken)
    {
        if (events.Count == 0)
        {
            return [];
        }

        var ids = events.Select(item => item.Id).ToArray();
        var suggestions = await dbContext.EventSuggestions.AsNoTracking()
            .Where(item => ids.Contains(item.EventInboxId))
            .ToDictionaryAsync(item => item.EventInboxId, cancellationToken);
        var userIds = events.Select(item => item.SubmittedBy)
            .Concat(suggestions.Values.Where(item => item.SuggestedAssigneeId.HasValue)
                .Select(item => item.SuggestedAssigneeId!.Value))
            .Distinct()
            .ToArray();
        var users = await dbContext.Users.AsNoTracking()
            .Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => events.Select(source => source.DepartmentId).Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var sourceTitles = await LoadSourceTitlesAsync(events, cancellationToken);
        var projectIds = suggestions.Values.Where(item => item.SuggestedProjectId.HasValue)
            .Select(item => item.SuggestedProjectId!.Value)
            .Distinct()
            .ToArray();
        var projects = await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => projectIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Title ?? item.ObjectCode, cancellationToken);
        var objectIds = suggestions.Values.Where(item => item.SuggestedBusinessObjectId.HasValue)
            .Select(item => item.SuggestedBusinessObjectId!.Value)
            .Distinct()
            .ToArray();
        var objects = await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => objectIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.ObjectCode, cancellationToken);
        var workflowVersionIds = suggestions.Values
            .Where(item => item.SuggestedWorkflowVersionId.HasValue)
            .Select(item => item.SuggestedWorkflowVersionId!.Value)
            .Distinct()
            .ToArray();
        var workflows = await (
            from version in dbContext.WorkflowVersions.AsNoTracking()
            join definition in dbContext.WorkflowDefinitions.AsNoTracking()
                on version.WorkflowDefinitionId equals definition.Id
            where workflowVersionIds.Contains(version.Id)
            select new { version.Id, definition.Name })
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        return events.Select(item =>
        {
            suggestions.TryGetValue(item.Id, out var suggestion);
            return new InboxEventDto(
                item.Id.ToString("D"),
                item.DepartmentId.ToString("D"),
                departments.GetValueOrDefault(item.DepartmentId, "未知部门"),
                item.SourceType,
                item.SourceId?.ToString("D"),
                item.SourceId.HasValue ? sourceTitles.GetValueOrDefault(item.SourceId.Value) : null,
                item.AttachmentId?.ToString("D"),
                item.Title,
                item.RawText,
                item.Status,
                item.SubmittedBy.ToString("D"),
                users.GetValueOrDefault(item.SubmittedBy, "未知用户"),
                item.AnalysisRunId?.ToString("D"),
                item.EnvironmentCode,
                item.IsTestData,
                item.CreatedAt,
                item.UpdatedAt,
                suggestion is null
                    ? null
                    : MapSuggestion(
                        suggestion,
                        projects,
                        objects,
                        users,
                        workflows));
        }).ToList();
    }

    private static InboxSuggestionDto MapSuggestion(
        EventSuggestionEntity suggestion,
        IReadOnlyDictionary<Guid, string> projects,
        IReadOnlyDictionary<Guid, string> objects,
        IReadOnlyDictionary<Guid, string> users,
        IReadOnlyDictionary<Guid, string> workflows) =>
        new(
            suggestion.Id.ToString("D"),
            suggestion.EventInboxId.ToString("D"),
            suggestion.AgentRunId.ToString("D"),
            suggestion.SuggestedProjectId?.ToString("D"),
            suggestion.SuggestedProjectId.HasValue
                ? projects.GetValueOrDefault(suggestion.SuggestedProjectId.Value)
                : null,
            suggestion.SuggestedBusinessObjectId?.ToString("D"),
            suggestion.SuggestedBusinessObjectId.HasValue
                ? objects.GetValueOrDefault(suggestion.SuggestedBusinessObjectId.Value)
                : null,
            suggestion.SuggestedAssigneeId?.ToString("D"),
            suggestion.SuggestedAssigneeId.HasValue
                ? users.GetValueOrDefault(suggestion.SuggestedAssigneeId.Value)
                : null,
            suggestion.SuggestedWorkflowVersionId?.ToString("D"),
            suggestion.SuggestedWorkflowVersionId.HasValue
                ? workflows.GetValueOrDefault(suggestion.SuggestedWorkflowVersionId.Value)
                : null,
            suggestion.SuggestedDueAt,
            suggestion.Priority,
            suggestion.RiskLevel,
            suggestion.RequiresHumanBoundary,
            suggestion.BoundaryReason,
            suggestion.MissingInformation,
            suggestion.EvidenceExcerpt,
            suggestion.Status,
            suggestion.CreatedAt);

    private async Task<Dictionary<Guid, string>> LoadSourceTitlesAsync(
        IReadOnlyList<EventInboxEntity> events,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, string>();
        var sourceIds = events.Where(item => item.SourceId.HasValue)
            .Select(item => item.SourceId!.Value)
            .ToHashSet();
        if (sourceIds.Count == 0)
        {
            return result;
        }

        var minutes = await dbContext.MinutesSources.AsNoTracking()
            .Where(item => sourceIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Title })
            .ToListAsync(cancellationToken);
        foreach (var minute in minutes)
        {
            result[minute.Id] = minute.Title;
        }

        var actions = await dbContext.MinutesActionItems.AsNoTracking()
            .Where(item => sourceIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Content })
            .ToListAsync(cancellationToken);
        foreach (var action in actions)
        {
            result[action.Id] = action.Content;
        }

        var notifications = await dbContext.WorkflowNotifications.AsNoTracking()
            .Where(item => sourceIds.Contains(item.Id))
            .Select(item => new { item.Id, item.Title })
            .ToListAsync(cancellationToken);
        foreach (var notification in notifications)
        {
            result[notification.Id] = notification.Title;
        }

        var attachments = await dbContext.Attachments.AsNoTracking()
            .Where(item => sourceIds.Contains(item.Id))
            .Select(item => new { item.Id, item.OriginalFileName })
            .ToListAsync(cancellationToken);
        foreach (var attachment in attachments)
        {
            result[attachment.Id] = attachment.OriginalFileName;
        }

        return result;
    }

    private async Task<ResolvedSource> ResolveSourceAsync(
        UserPrincipal user,
        ReceiveInboxEventRequest request,
        CancellationToken cancellationToken)
    {
        var sourceType = NormalizeSourceType(request.SourceType);
        switch (sourceType)
        {
            case "ManualText":
            case "ManualUpload":
                return new ResolvedSource(
                    sourceType,
                    DomainChecks.Required(request.Title, "事件标题", 255),
                    DomainChecks.Required(request.Content, "事件原文", 2_000_000));
            case "MinutesSource":
                {
                    var id = RequireSourceId(request.SourceId);
                    var source = await dbContext.MinutesSources.AsNoTracking().SingleOrDefaultAsync(
                        item => item.Id == id
                            && item.OrganizationId == user.OrganizationId
                            && item.DepartmentId == request.DepartmentId,
                        cancellationToken) ?? throw new NotFoundException("所选听记不存在或不在当前部门。");
                    return new ResolvedSource(sourceType, source.Title, source.Transcript);
                }
            case "MinutesActionItem":
                {
                    var id = RequireSourceId(request.SourceId);
                    var row = await (
                        from action in dbContext.MinutesActionItems.AsNoTracking()
                        join source in dbContext.MinutesSources.AsNoTracking()
                            on action.MinutesSourceId equals source.Id
                        where action.Id == id
                            && source.OrganizationId == user.OrganizationId
                            && source.DepartmentId == request.DepartmentId
                        select new { action, source }).SingleOrDefaultAsync(cancellationToken)
                        ?? throw new NotFoundException("所选听记行动项不存在或不在当前部门。");
                    return new ResolvedSource(
                        sourceType,
                        row.action.Content,
                        $"发言人：{row.action.Speaker}\n时间戳：{row.action.SourceTimestamp}\n原文：{row.action.SourceExcerpt}\n事项：{row.action.Content}");
                }
            case "WorkflowNotification":
                {
                    var id = RequireSourceId(request.SourceId);
                    var row = await dbContext.WorkflowNotifications.AsNoTracking().SingleOrDefaultAsync(
                        item => item.Id == id
                            && item.OrganizationId == user.OrganizationId
                            && item.DepartmentId == request.DepartmentId
                            && (user.IsPlatformAdmin || item.RecipientId == user.UserId),
                        cancellationToken) ?? throw new NotFoundException("所选流程通知不存在或不在当前用户范围。");
                    return new ResolvedSource(sourceType, row.Title, row.Message);
                }
            case "Attachment":
                {
                    var id = RequireSourceId(request.SourceId);
                    var attachment = await dbContext.Attachments.AsNoTracking().SingleOrDefaultAsync(
                        item => item.Id == id
                            && item.OrganizationId == user.OrganizationId
                            && item.DepartmentId == request.DepartmentId,
                        cancellationToken) ?? throw new NotFoundException("所选附件不存在或不在当前部门。");
                    return new ResolvedSource(
                        sourceType,
                        attachment.OriginalFileName,
                        $"附件来源：{attachment.OriginalFileName}\n类型：{attachment.MimeType}\n大小：{attachment.SizeBytes} bytes\nSHA256：{attachment.Sha256}");
                }
            default:
                throw new ValidationException("不支持的事件来源类型。");
        }
    }

    private static string NormalizeSourceType(string value)
    {
        var normalized = DomainChecks.Required(value, "事件来源类型", 32);
        var allowed = new HashSet<string>(
            [
                "ManualText",
                "ManualUpload",
                "MinutesSource",
                "MinutesActionItem",
                "WorkflowNotification",
                "Attachment"
            ],
            StringComparer.Ordinal);
        if (!allowed.Contains(normalized))
        {
            throw new ValidationException("不支持的事件来源类型。");
        }

        return normalized;
    }

    private static Guid RequireSourceId(Guid? sourceId) =>
        sourceId.HasValue && sourceId.Value != Guid.Empty
            ? sourceId.Value
            : throw new ValidationException("选择现有来源时必须提供来源 ID。");

    private static ProjectDto? ChooseProject(IReadOnlyList<ProjectDto> projects, EventInboxEntity inbox) =>
        projects.FirstOrDefault(project =>
            inbox.RawText.Contains(project.Name, StringComparison.OrdinalIgnoreCase)
            || inbox.RawText.Contains(project.ObjectCode, StringComparison.OrdinalIgnoreCase)
            || inbox.Title.Contains(project.Name, StringComparison.OrdinalIgnoreCase))
        ?? projects.FirstOrDefault(project => project.DepartmentId == inbox.DepartmentId.ToString("D"))
        ?? (projects.Count > 0 ? projects[0] : null);

    private async Task<Guid?> ChooseBusinessObjectAsync(
        string? projectId,
        EventInboxEntity inbox,
        CancellationToken cancellationToken)
    {
        if (projectId is not null)
        {
            var linked = await dbContext.ProjectRelations.AsNoTracking()
                .Where(item => item.ProjectId == Guid.Parse(projectId) && item.EntityType == "BusinessObject")
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => (Guid?)item.EntityId)
                .FirstOrDefaultAsync(cancellationToken);
            if (linked.HasValue)
            {
                return linked;
            }
        }

        return await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => item.DepartmentId == inbox.DepartmentId
                && item.EnvironmentCode == inbox.EnvironmentCode
                && !item.IsDeleted)
            .OrderByDescending(item => item.UpdatedAt)
            .Select(item => (Guid?)item.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Guid?> ChooseWorkflowVersionAsync(
        Guid? businessObjectId,
        CancellationToken cancellationToken)
    {
        if (!businessObjectId.HasValue)
        {
            return null;
        }

        var objectTypeCode = await (
            from businessObject in dbContext.BusinessObjects.AsNoTracking()
            join objectType in dbContext.ObjectTypes.AsNoTracking()
                on businessObject.ObjectTypeId equals objectType.Id
            where businessObject.Id == businessObjectId.Value
            select objectType.Code).SingleOrDefaultAsync(cancellationToken);
        if (objectTypeCode is null)
        {
            return null;
        }

        var versions = await (
            from version in dbContext.WorkflowVersions.AsNoTracking()
            join definition in dbContext.WorkflowDefinitions.AsNoTracking()
                on version.WorkflowDefinitionId equals definition.Id
            where version.Status == "Published" && definition.Status == "Published"
            orderby version.PublishedAt descending
            select version).ToListAsync(cancellationToken);
        return versions.FirstOrDefault(version =>
                version.DefinitionJson.Contains(
                    $"\"objectTypeCode\":\"{objectTypeCode}\"",
                    StringComparison.OrdinalIgnoreCase))
            ?.Id;
    }

    private async Task ValidateWorkflowAsync(
        Guid projectId,
        Guid workflowInstanceId,
        CancellationToken cancellationToken)
    {
        var project = await dbContext.BusinessObjects.AsNoTracking()
            .SingleAsync(item => item.Id == projectId, cancellationToken);
        var workflow = await dbContext.WorkflowInstances.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == workflowInstanceId,
            cancellationToken) ?? throw new NotFoundException("所选流程实例不存在。");
        var alreadyLinked = await dbContext.ProjectRelations.AsNoTracking().AnyAsync(
            item => item.ProjectId == projectId
                && item.EntityType == "WorkflowInstance"
                && item.EntityId == workflowInstanceId,
            cancellationToken);
        if (workflow.DepartmentId != project.DepartmentId && !alreadyLinked)
        {
            throw new PermissionException(
                ErrorCodes.ProjectScopeDenied,
                "跨部门流程必须先由有权用户显式关联到项目。");
        }
    }

    private async Task<Guid> ResolveUserDepartmentAsync(
        Guid userId,
        Guid fallbackDepartmentId,
        CancellationToken cancellationToken)
    {
        var departmentId = await dbContext.UserDepartmentMemberships.AsNoTracking()
            .Where(item => item.UserId == userId && item.IsActive && item.IsPrimary)
            .Select(item => (Guid?)item.DepartmentId)
            .FirstOrDefaultAsync(cancellationToken);
        return departmentId ?? fallbackDepartmentId;
    }

    private void AddRelationIfMissing(
        Guid projectId,
        string entityType,
        Guid entityId,
        string relationCode,
        Guid actorId,
        DateTimeOffset now)
    {
        if (dbContext.ProjectRelations.Local.Any(item =>
                item.ProjectId == projectId
                && item.EntityType == entityType
                && item.EntityId == entityId
                && item.RelationCode == relationCode))
        {
            return;
        }

        var exists = dbContext.ProjectRelations.AsNoTracking().Any(item =>
            item.ProjectId == projectId
            && item.EntityType == entityType
            && item.EntityId == entityId
            && item.RelationCode == relationCode);
        if (exists)
        {
            return;
        }

        dbContext.ProjectRelations.Add(new ProjectRelationEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            EntityType = entityType,
            EntityId = entityId,
            RelationCode = relationCode,
            CreatedBy = actorId,
            CreatedAt = now
        });
    }

    private static string MapSourceEntityType(string sourceType) =>
        sourceType switch
        {
            "MinutesSource" => "MinutesSource",
            "MinutesActionItem" => "MinutesActionItem",
            "WorkflowNotification" => "WorkflowNotification",
            "Attachment" => "Attachment",
            _ => string.Empty
        };

    private static AgentProfile MapAgentProfile(AgentProfileEntity entity) =>
        AgentProfile.Restore(
            entity.Id,
            entity.ServiceUserId,
            entity.Name,
            Enum.Parse<AgentAutonomyLevel>(entity.AutonomyLevel),
            entity.AmountLimit,
            Enum.Parse<AgentRiskLevel>(entity.RiskLimit),
            entity.IsActive,
            JsonSerializer.Deserialize<string[]>(entity.AllowedToolsJson) ?? [],
            entity.Code,
            entity.DepartmentId,
            JsonSerializer.Deserialize<string[]>(entity.DataScopesJson) ?? [],
            JsonSerializer.Deserialize<string[]>(entity.ModelScopesJson) ?? [],
            JsonSerializer.Deserialize<string[]>(entity.RequiredConfirmationsJson) ?? [],
            JsonSerializer.Deserialize<string[]>(entity.StopConditionsJson) ?? [],
            Enum.Parse<AgentProfileLifecycleStatus>(entity.LifecycleStatus),
            entity.CurrentPublishedVersionId);

    private static AgentRunEntity ToAgentRunEntity(AgentRun run) =>
        new()
        {
            Id = run.Id,
            AgentProfileId = run.AgentProfileId,
            RequestedBy = run.RequestedBy,
            Action = run.Action,
            Amount = run.Amount,
            RiskLevel = run.RiskLevel.ToString(),
            Status = run.Status.ToString(),
            ConfirmationId = run.ConfirmationId,
            Model = run.Model,
            CreatedAt = run.CreatedAt,
            StartedAt = run.StartedAt,
            EndedAt = run.EndedAt,
            OrganizationId = run.OrganizationId,
            DepartmentId = run.DepartmentId,
            EnvironmentCode = run.EnvironmentCode,
            IsTestData = run.IsTestData,
            SubjectType = run.SubjectType,
            BusinessObjectId = run.BusinessObjectId,
            WorkflowInstanceId = run.WorkflowInstanceId,
            WorkflowTaskId = run.WorkflowTaskId,
            MinutesSourceId = run.MinutesSourceId,
            GatewayCallId = run.GatewayCallId,
            GatewayMode = run.GatewayMode,
            ExternalModelCalled = run.ExternalModelCalled,
            ProxyTriggerMode = run.ProxyTriggerMode,
            ProxyTriggeredAt = run.ProxyTriggeredAt,
            OriginalAssigneeId = run.OriginalAssigneeId,
            CompensationStatus = run.CompensationStatus,
            ProxyIdempotencyKey = run.ProxyIdempotencyKey
        };

    private static string NormalizePriority(string priority)
    {
        var normalized = DomainChecks.Required(priority, "优先级", 8).ToUpperInvariant();
        return normalized is "P0" or "P1" or "P2"
            ? normalized
            : throw new ValidationException("优先级必须是 P0、P1 或 P2。");
    }

    private static void ValidateIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
        {
            throw new ValidationException("写操作必须提供 8—128 位幂等键。");
        }
    }

    private static string Hash<T>(T request) =>
        Sha256(JsonSerializer.Serialize(request, JsonSerializerOptions.Web));

    private static string Sha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Abbreviate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ResolvedSource(string SourceType, string Title, string Content);
}
