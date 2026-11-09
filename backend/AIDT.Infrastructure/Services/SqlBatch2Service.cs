using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed partial class SqlBatch2Service(
    AidtDbContext dbContext,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    DataEnvironmentOptions environmentOptions,
    IHttpClientFactory httpClientFactory,
    IWorkflowEngine workflowEngine,
    IWorkflowRepository workflowRepository,
    IBusinessObjectRepository businessObjectRepository,
    IDepartmentRepository departmentRepository,
    BusinessTimeOptions businessTimeOptions) :
    IMinutesService,
    IWorkflowPolicyService,
    ISlaProcessor,
    IAgentGatewayService
{
    private const string AnalysisVersion = "rule-minutes-v1";
    private const string DefaultAgentName = "业务协同数字员工";
    private const string ProxyAgentName = "流程代理数字员工";

    public async Task<PagedResult<MinutesSourceDto>> ListSourcesAsync(
        UserPrincipal user,
        MinutesSourceQuery query,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("minutes", AccessAction.Read);
        var sources = ApplySourceScope(
            user,
            query.IncludeTestData,
            dbContext.MinutesSources.AsNoTracking());
        if (query.DepartmentId.HasValue)
        {
            sources = sources.Where(item => item.DepartmentId == query.DepartmentId.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim();
            sources = sources.Where(item => item.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(query.SourceType))
        {
            var sourceType = query.SourceType.Trim();
            sources = sources.Where(item => item.SourceType == sourceType);
        }
        if (!string.IsNullOrWhiteSpace(query.ActionItemStatus))
        {
            var actionItemStatus = query.ActionItemStatus.Trim();
            sources = sources.Where(item => dbContext.MinutesActionItems.Any(action =>
                action.MinutesSourceId == item.Id && action.Status == actionItemStatus));
        }
        if (query.MeetingFrom.HasValue)
        {
            sources = sources.Where(item => item.MeetingAt >= query.MeetingFrom.Value);
        }
        if (query.MeetingTo.HasValue)
        {
            sources = sources.Where(item => item.MeetingAt <= query.MeetingTo.Value);
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            sources = sources.Where(item =>
                item.Title.Contains(search)
                || item.Transcript.Contains(search)
                || dbContext.MinutesActionItems.Any(action =>
                    action.MinutesSourceId == item.Id && action.Speaker.Contains(search)));
        }
        sources = sources.OrderByDescending(item => item.CreatedAt).ThenByDescending(item => item.Id);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var total = await sources.LongCountAsync(cancellationToken);
        var rows = await sources
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<MinutesSourceDto>(
            rows.Select(MapSource).ToList(),
            page,
            pageSize,
            total);
    }

    public async Task<MinutesSummaryDto> GetSummaryAsync(
        UserPrincipal user,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("minutes", AccessAction.Read);
        var sources = ApplySourceScope(
            user,
            includeTestData,
            dbContext.MinutesSources.AsNoTracking());
        var total = await sources.LongCountAsync(cancellationToken);
        var analyzed = await sources.LongCountAsync(
            item => item.Status == "Analyzed" || item.Status == "Confirmed",
            cancellationToken);
        var pendingActions = await dbContext.MinutesActionItems
            .AsNoTracking()
            .LongCountAsync(
                action => action.Status == "Draft"
                    && sources.Any(source => source.Id == action.MinutesSourceId),
                cancellationToken);
        var weekStart = CurrentBusinessWeekStart();
        var createdThisWeek = await sources.LongCountAsync(
            item => item.CreatedAt >= weekStart,
            cancellationToken);
        return new MinutesSummaryDto(total, analyzed, pendingActions, createdThisWeek);
    }

    public async Task<MinutesSourceDto?> GetSourceAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("minutes", AccessAction.Read);
        var source = await ApplySourceScope(user, includeTestData, dbContext.MinutesSources.AsNoTracking())
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return source is null ? null : MapSource(source);
    }

    public async Task<MinutesSourceDto> RegisterAsync(
        UserPrincipal user,
        RegisterMinutesRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("minutes", AccessAction.Create);
        ValidateIdempotencyKey(idempotencyKey);
        var hash = Hash(request);
        var scope = $"minutes:register:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同听记请求。");
            }

            return await GetSourceAsync(user, Guid.Parse(existing.ResponseId), false, cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的听记不存在。");
        }

        await EnsureDepartmentAsync(user, request.DepartmentId, cancellationToken);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var transcript = DomainChecks.Required(request.Transcript, "听记文字稿", 2_000_000);
        if (request.AttachmentId.HasValue)
        {
            var attachmentOk = await dbContext.Attachments.AsNoTracking().AnyAsync(
                item => item.Id == request.AttachmentId.Value
                    && item.OrganizationId == user.OrganizationId
                    && item.DepartmentId == request.DepartmentId
                    && item.EnvironmentCode == environment,
                cancellationToken);
            if (!attachmentOk)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "听记附件不属于当前部门或环境。");
            }
        }

        var now = clock.UtcNow;
        var source = new MinutesSourceEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = user.OrganizationId,
            DepartmentId = request.DepartmentId,
            AttachmentId = request.AttachmentId,
            Title = DomainChecks.Required(request.Title, "会议标题", 255),
            MeetingAt = (request.MeetingAt ?? now).ToUniversalTime(),
            SourceType = request.AttachmentId.HasValue ? "ManualUpload" : "ManualText",
            Transcript = transcript,
            EnvironmentCode = environment,
            IsTestData = DataEnvironment.IsTestEnvironment(environment),
            Status = "Imported",
            AnalysisVersion = "none",
            CreatedBy = user.UserId,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.MinutesSources.Add(source);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Minutes",
                    "minutes.uploaded",
                    nameof(MinutesSourceEntity),
                    source.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"{{\"title\":\"{Escape(source.Title)}\",\"sourceType\":\"{source.SourceType}\"}}"),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(MinutesSourceEntity),
                    source.Id.ToString("D"),
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

        return MapSource(source);
    }

    public async Task<AgentRunDto> RequestAnalysisAsync(
        UserPrincipal user,
        Guid sourceId,
        AnalyzeMinutesRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("minutes", AccessAction.Update);
        ValidateIdempotencyKey(idempotencyKey);
        var source = await LoadAccessibleSourceAsync(user, sourceId, false, cancellationToken);
        var scope = $"minutes:analyze:{sourceId:D}:{user.UserId:D}";
        var hash = Hash(request);
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var existingRun = await dbContext.AgentRuns.AsNoTracking()
                .SingleAsync(item => item.Id == Guid.Parse(existing.ResponseId), cancellationToken);
            return MapAgentRun(existingRun).ToDto();
        }

        var profileEntity = await dbContext.AgentProfiles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == request.AgentProfileId && item.IsActive, cancellationToken)
            ?? throw new NotFoundException("Agent 配置不存在或已停用。");
        var profile = MapProfile(profileEntity);
        var now = clock.UtcNow;
        var run = AgentRun.Request(
            profile,
            user.UserId,
            AgentActionType.Draft,
            0,
            AgentRiskLevel.Low,
            null,
            now,
            source.OrganizationId,
            source.DepartmentId,
            source.EnvironmentCode,
            source.IsTestData,
            "MinutesSource",
            minutesSourceId: source.Id,
            model: "aidt-rule-sim");

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.AgentRuns.Add(ToEntity(run));
            source.AnalysisRunId = run.Id;
            source.Status = "AwaitingConfirmation";
            source.UpdatedAt = now;
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Minutes",
                    "minutes.analysis_requested",
                    nameof(MinutesSourceEntity),
                    source.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"{{\"agentRunId\":\"{run.Id:D}\",\"mode\":\"RuleSimulator\"}}"),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(AgentRunEntity),
                    run.Id.ToString("D"),
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

        return run.ToDto();
    }

    public async Task<IReadOnlyList<MinutesActionItemDto>> ListActionItemsAsync(
        UserPrincipal user,
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        _ = await LoadAccessibleSourceAsync(user, sourceId, false, cancellationToken);
        var rows = await dbContext.MinutesActionItems.AsNoTracking()
            .Where(item => item.MinutesSourceId == sourceId)
            .OrderBy(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return await EnrichActionItemsAsync(rows, cancellationToken);
    }

    public async Task<IReadOnlyList<MinutesConfirmationUserDto>> ListConfirmationUsersAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("minutes", AccessAction.Read);
        user.EnsureDepartmentAccess(departmentId);
        var departmentName = await dbContext.Departments.AsNoTracking()
            .Where(item => item.Id == departmentId && item.OrganizationId == user.OrganizationId)
            .Select(item => item.Name)
            .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("听记部门不存在。");
        return await (
            from membership in dbContext.UserDepartmentMemberships.AsNoTracking()
            join account in dbContext.Users.AsNoTracking() on membership.UserId equals account.Id
            where membership.DepartmentId == departmentId
                && membership.IsActive
                && account.OrganizationId == user.OrganizationId
                && account.IsActive
                && !account.IsServiceIdentity
            orderby membership.IsPrimary descending, account.DisplayName
            select new MinutesConfirmationUserDto(
                account.Id.ToString("D"),
                account.UserName,
                account.DisplayName,
                departmentId.ToString("D"),
                departmentName))
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<MinutesActionItemDto> UpdateActionItemAsync(
        UserPrincipal user,
        Guid sourceId,
        Guid actionItemId,
        UpdateMinutesActionItemRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("minutes", AccessAction.Update);
        var source = await LoadAccessibleSourceAsync(user, sourceId, false, cancellationToken);
        var item = await dbContext.MinutesActionItems.SingleOrDefaultAsync(
            row => row.Id == actionItemId && row.MinutesSourceId == sourceId,
            cancellationToken) ?? throw new NotFoundException("行动项不存在。");
        if (item.Status == "Confirmed")
        {
            throw new ConflictException(ErrorCodes.Conflict, "已确认行动项不能再次修改。");
        }

        item.Content = DomainChecks.Required(request.Content, "行动项内容", 2000);
        item.AssigneeId = await ValidateOptionalUserInDepartmentAsync(
            user.OrganizationId,
            source.DepartmentId,
            request.AssigneeId,
            cancellationToken);
        item.DueAt = request.DueAt?.ToUniversalTime();
        item.Priority = NormalizePriority(request.Priority);
        item.ObjectTypeId = await ValidateOptionalObjectTypeAsync(request.ObjectTypeId, cancellationToken);
        item.WorkflowVersionId = await ValidateOptionalWorkflowVersionAsync(
            request.WorkflowVersionId,
            cancellationToken);
        var links = await ValidateConfirmationLinksAsync(
            user.OrganizationId,
            source.DepartmentId,
            source.EnvironmentCode,
            request.ProjectId,
            request.BusinessObjectId,
            request.WorkflowInstanceId,
            cancellationToken);
        item.ProjectId = links.ProjectId;
        item.BusinessObjectId = links.BusinessObjectId;
        item.WorkflowInstanceId = links.WorkflowInstanceId;
        item.MissingInformation = BuildMinutesMissingInformation(
            item.AssigneeId,
            item.DueAt,
            item.Content,
            request.MissingInformation);
        item.UpdatedAt = clock.UtcNow;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Minutes",
                "minutes.action_updated",
                nameof(MinutesActionItemEntity),
                item.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                clock.UtcNow,
                reason: $"sourceDepartment={source.DepartmentId:D}"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await EnrichActionItemsAsync([item], cancellationToken)).Single();
    }

    public async Task<MinutesTaskDto> ConfirmActionItemAsync(
        UserPrincipal user,
        Guid actionItemId,
        ConfirmMinutesActionItemRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("minutes", AccessAction.Approve);
        ValidateIdempotencyKey(idempotencyKey);
        var scope = $"minutes:confirm:{actionItemId:D}:{user.UserId:D}";
        var hash = Hash(request);
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同确认请求。");
            }

            var task = await dbContext.MinutesTasks.AsNoTracking()
                .SingleAsync(item => item.Id == Guid.Parse(existing.ResponseId), cancellationToken);
            return await EnrichMinutesTaskAsync(task, cancellationToken);
        }

        var item = await dbContext.MinutesActionItems
            .SingleOrDefaultAsync(row => row.Id == actionItemId, cancellationToken)
            ?? throw new NotFoundException("行动项不存在。");
        var source = await LoadAccessibleSourceAsync(user, item.MinutesSourceId, false, cancellationToken);
        var assigneeId = request.AssigneeId ?? item.AssigneeId ?? item.SuggestedAssigneeId
            ?? throw new ValidationException("MINUTES_ACTION_NEEDS_ASSIGNEE", "确认创建任务前必须选择负责人。");
        await ValidateOptionalUserInDepartmentAsync(
            user.OrganizationId,
            source.DepartmentId,
            assigneeId,
            cancellationToken);

        var links = await ValidateConfirmationLinksAsync(
            user.OrganizationId,
            source.DepartmentId,
            source.EnvironmentCode,
            request.ProjectId ?? item.ProjectId,
            request.BusinessObjectId ?? item.BusinessObjectId,
            request.WorkflowInstanceId ?? item.WorkflowInstanceId,
            cancellationToken);
        var businessObjectId = links.BusinessObjectId;

        var now = clock.UtcNow;
        var content = DomainChecks.Required(
            request.Content ?? item.Content,
            "行动项内容",
            2000);
        var dueAt = request.DueAt?.ToUniversalTime()
            ?? item.DueAt?.ToUniversalTime()
            ?? item.SuggestedDueAt?.ToUniversalTime()
            ?? throw new ValidationException("MINUTES_ACTION_NEEDS_DUE_AT", "确认创建任务前必须选择截止时间。");
        var taskEntity = new MinutesTaskEntity
        {
            Id = Guid.NewGuid(),
            MinutesSourceId = source.Id,
            ActionItemId = item.Id,
            AttachmentId = source.AttachmentId,
            OrganizationId = source.OrganizationId,
            DepartmentId = source.DepartmentId,
            AssigneeId = assigneeId,
            Title = $"{source.Title} · 行动项",
            Content = content,
            DueAt = dueAt,
            Priority = NormalizePriority(request.Priority),
            BusinessObjectId = businessObjectId,
            WorkflowInstanceId = links.WorkflowInstanceId,
            ProjectId = links.ProjectId,
            EnvironmentCode = source.EnvironmentCode,
            IsTestData = source.IsTestData,
            Status = "Pending",
            CreatedBy = user.UserId,
            CreatedAt = now
        };

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            item.Content = content;
            item.AssigneeId = assigneeId;
            item.DueAt = taskEntity.DueAt;
            item.Priority = taskEntity.Priority;
            item.ObjectTypeId = await ValidateOptionalObjectTypeAsync(request.ObjectTypeId, cancellationToken);
            item.WorkflowVersionId = await ValidateOptionalWorkflowVersionAsync(
                request.WorkflowVersionId,
                cancellationToken);
            item.ProjectId = links.ProjectId;
            item.BusinessObjectId = businessObjectId;
            item.WorkflowInstanceId = links.WorkflowInstanceId;
            item.MissingInformation = BuildMinutesMissingInformation(
                item.AssigneeId,
                item.DueAt,
                item.Content,
                item.MissingInformation);
            item.Status = "Confirmed";
            item.ConfirmedBy = user.UserId;
            item.ConfirmedAt = now;
            item.MinutesTaskId = taskEntity.Id;
            item.UpdatedAt = now;
            source.Status = "Confirmed";
            source.UpdatedAt = now;
            dbContext.MinutesTasks.Add(taskEntity);
            if (links.ProjectId.HasValue)
            {
                await AddMinutesProjectRelationsAsync(
                    links.ProjectId.Value,
                    item,
                    taskEntity,
                    businessObjectId,
                    links.WorkflowInstanceId,
                    user.UserId,
                    now,
                    cancellationToken);
            }
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Minutes",
                    "minutes.action_confirm",
                    nameof(MinutesActionItemEntity),
                    item.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new
                        {
                            taskId = taskEntity.Id,
                            taskEntity.ProjectId,
                            taskEntity.BusinessObjectId,
                            taskEntity.WorkflowInstanceId,
                            taskEntity.AssigneeId,
                            taskEntity.DueAt,
                            taskEntity.Priority
                        },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Minutes",
                    "minutes.task_created",
                    nameof(MinutesTaskEntity),
                    taskEntity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"{{\"sourceId\":\"{source.Id:D}\",\"assigneeId\":\"{assigneeId:D}\"}}"),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(MinutesTaskEntity),
                    taskEntity.Id.ToString("D"),
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

        return await EnrichMinutesTaskAsync(taskEntity, cancellationToken);
    }

    public async Task<MinutesTaskTraceDto?> TraceTaskAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("minutes", AccessAction.Read);
        var task = await dbContext.MinutesTasks.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken);
        if (task is null || task.OrganizationId != user.OrganizationId)
        {
            return null;
        }
        user.EnsureDepartmentAccess(task.DepartmentId);
        var source = await ApplySourceScope(user, false, dbContext.MinutesSources.AsNoTracking())
            .SingleAsync(item => item.Id == task.MinutesSourceId, cancellationToken);
        var action = await dbContext.MinutesActionItems.AsNoTracking()
            .SingleAsync(item => item.Id == task.ActionItemId, cancellationToken);
        return new MinutesTaskTraceDto(
            await EnrichMinutesTaskAsync(task, cancellationToken),
            MapSource(source),
            (await EnrichActionItemsAsync([action], cancellationToken)).Single());
    }

    public async Task<IReadOnlyList<WorkflowPolicyUserDto>> ListUsersAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-policy", AccessAction.Read);
        return await dbContext.Users.AsNoTracking()
            .Where(item => item.OrganizationId == user.OrganizationId && item.IsActive && !item.IsServiceIdentity)
            .OrderBy(item => item.DisplayName)
            .Select(item => new WorkflowPolicyUserDto(
                item.Id.ToString("D"),
                item.UserName,
                item.DisplayName))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowVersionNodeDto>> ListNodeCandidatesAsync(
        UserPrincipal user,
        Guid workflowVersionId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-policy", AccessAction.Read);
        var version = await dbContext.WorkflowVersions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == workflowVersionId, cancellationToken)
            ?? throw new NotFoundException("流程版本不存在。");
        using var definition = JsonDocument.Parse(version.DefinitionJson);
        if (!definition.RootElement.TryGetProperty("nodes", out var nodes)
            || nodes.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return nodes.EnumerateArray()
            .Select(node => new WorkflowVersionNodeDto(
                node.TryGetProperty("code", out var code) ? code.GetString() ?? string.Empty : string.Empty,
                node.TryGetProperty("name", out var name) ? name.GetString() ?? string.Empty : string.Empty,
                node.TryGetProperty("assigneeUserName", out var assignee) ? assignee.GetString() : null,
                node.TryGetProperty("nextNode", out var next) && next.ValueKind != JsonValueKind.Null
                    ? next.GetString()
                    : null))
            .Where(node => !string.IsNullOrWhiteSpace(node.Code))
            .ToList();
    }

    public async Task<IReadOnlyList<WorkflowNodePolicyDto>> ListAsync(
        UserPrincipal user,
        Guid workflowVersionId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-policy", AccessAction.Read);
        var rows = await dbContext.WorkflowNodePolicies.AsNoTracking()
            .Where(item => item.WorkflowVersionId == workflowVersionId)
            .OrderBy(item => item.NodeCode)
            .ThenByDescending(item => item.VersionNumber)
            .ToListAsync(cancellationToken);
        return rows.Select(MapPolicy).ToList();
    }

    public async Task<WorkflowNodePolicyDto> SaveAsync(
        UserPrincipal user,
        Guid workflowVersionId,
        string nodeCode,
        SaveWorkflowNodePolicyRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-policy", AccessAction.Update);
        ValidateIdempotencyKey(idempotencyKey);
        var versionExists = await dbContext.WorkflowVersions.AsNoTracking()
            .AnyAsync(item => item.Id == workflowVersionId, cancellationToken);
        if (!versionExists)
        {
            throw new NotFoundException("流程版本不存在。");
        }
        var normalizedNode = DomainChecks.RequiredCode(nodeCode, "流程节点", 80);
        var scope = $"workflow-policy:{workflowVersionId:D}:{normalizedNode}";
        var hash = Hash(request);
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            var policy = await dbContext.WorkflowNodePolicies.AsNoTracking()
                .SingleAsync(item => item.Id == Guid.Parse(existing.ResponseId), cancellationToken);
            return MapPolicy(policy);
        }
        if (request.SlaMinutes is <= 0 || request.ReminderMinutes is < 0 || request.EscalationMinutes is < 0)
        {
            throw new ValidationException("SLA 和提醒分钟必须为正数或非负整数。");
        }
        if (request.ReminderMinutes > request.SlaMinutes || request.EscalationMinutes > request.SlaMinutes)
        {
            throw new ValidationException("提醒和升级时间不能晚于 SLA。");
        }

        await ValidateOptionalUserAsync(user.OrganizationId, request.ApproverUserId, cancellationToken);
        await ValidateOptionalUserAsync(user.OrganizationId, request.BackupApproverUserId, cancellationToken);
        if (!Enum.TryParse<WorkflowAssignmentStrategy>(request.AssigneeStrategy, true, out var assigneeStrategy))
        {
            throw new ValidationException("流程节点处理人策略无效。");
        }
        Guid? businessRoleId = null;
        if (assigneeStrategy == WorkflowAssignmentStrategy.BusinessRole)
        {
            var role = await dbContext.Roles.AsNoTracking().SingleOrDefaultAsync(
                item => item.Code == request.BusinessRoleCode
                    && item.RoleType == "Business"
                    && item.IsActive,
                cancellationToken) ?? throw new ValidationException("业务流程角色不存在或已停用。");
            businessRoleId = role.Id;
        }
        if (request.TransferTargetStrategy is not null
            && request.TransferTargetStrategy is not ("BackupApprover" or "DepartmentManager"))
        {
            throw new ValidationException("转交目标策略必须是 BackupApprover 或 DepartmentManager。");
        }
        if (!Enum.TryParse<AgentRiskLevel>(request.AgentMaxRisk, true, out var risk))
        {
            throw new ValidationException("Agent 风险上限无效。");
        }
        if (!Enum.TryParse<AgentAutonomyLevel>(request.AgentMaxAutonomy, true, out var autonomy))
        {
            throw new ValidationException("Agent 执行等级上限无效。");
        }
        if (!Enum.TryParse<WorkflowAgentProxyMode>(request.AgentProxyMode, true, out var proxyMode))
        {
            throw new ValidationException("Agent 代理模式无效。");
        }
        if (request.AllowAgentProxy
            && (risk != AgentRiskLevel.Low
                || request.PermanentHumanBoundary
                || autonomy is AgentAutonomyLevel.A0 or AgentAutonomyLevel.A1))
        {
            throw new ValidationException("代理只允许 Low 风险、A2 以上且非永久人工边界节点。");
        }
        if (proxyMode != WorkflowAgentProxyMode.RemindOnly && !request.AllowAgentProxy)
        {
            throw new ValidationException("启用代理模式前必须允许 Agent 代理。");
        }
        if (proxyMode == WorkflowAgentProxyMode.PreAuthorizedAutoProxy)
        {
            if (request.PermanentHumanBoundary
                || risk != AgentRiskLevel.Low
                || autonomy is AgentAutonomyLevel.A0 or AgentAutonomyLevel.A1
                || request.AutoProxyAfterMinutes is null or <= 0)
            {
                throw new ValidationException("预授权自动代理必须为 Low 风险、A2 以上且配置正数自动代理时间。");
            }
            if (request.AutoProxyAfterMinutes > request.SlaMinutes)
            {
                throw new ValidationException("自动代理时间不能晚于 SLA。");
            }
        }
        if (request.AgentProfileId.HasValue)
        {
            var profile = await dbContext.AgentProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == request.AgentProfileId.Value && item.IsActive, cancellationToken)
                ?? throw new ValidationException("Agent 配置不存在或已停用。");
            var profileRisk = Enum.Parse<AgentRiskLevel>(profile.RiskLimit);
            var profileAutonomy = Enum.Parse<AgentAutonomyLevel>(profile.AutonomyLevel);
            if (profileRisk < risk || profile.AmountLimit < request.AgentMaxAmount || profileAutonomy < autonomy)
            {
                throw new ValidationException("Agent 配置不满足节点风险、金额或执行等级上限。");
            }
        }

        var nextVersion = (await dbContext.WorkflowNodePolicies
            .Where(item => item.WorkflowVersionId == workflowVersionId && item.NodeCode == normalizedNode)
            .MaxAsync(item => (int?)item.VersionNumber, cancellationToken) ?? 0) + 1;
        if (request.Publish)
        {
            await dbContext.WorkflowNodePolicies
                .Where(item => item.WorkflowVersionId == workflowVersionId
                    && item.NodeCode == normalizedNode
                    && item.Status == "Published")
                .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.Status, "Retired"), cancellationToken);
        }
        var now = clock.UtcNow;
        var entity = new WorkflowNodePolicyEntity
        {
            Id = Guid.NewGuid(),
            WorkflowVersionId = workflowVersionId,
            NodeCode = normalizedNode,
            VersionNumber = nextVersion,
            ApproverUserId = request.ApproverUserId,
            BackupApproverUserId = request.BackupApproverUserId,
            AssigneeStrategy = assigneeStrategy.ToString(),
            BusinessRoleId = businessRoleId,
            AllowTransfer = request.AllowTransfer,
            TransferTargetStrategy = request.TransferTargetStrategy,
            SlaMinutes = request.SlaMinutes,
            ReminderMinutes = request.ReminderMinutes,
            EscalationMinutes = request.EscalationMinutes,
            TimeoutStrategy = DomainChecks.Required(request.TimeoutStrategy, "超时策略", 32),
            AllowAgentAssist = request.AllowAgentAssist,
            AllowAgentProxy = request.AllowAgentProxy,
            AgentProfileId = request.AgentProfileId,
            AgentMaxRisk = risk.ToString(),
            AgentMaxAmount = request.AgentMaxAmount,
            AgentMaxAutonomy = autonomy.ToString(),
            AgentProxyMode = proxyMode.ToString(),
            AutoProxyAfterMinutes = request.AutoProxyAfterMinutes,
            PermanentHumanBoundary = request.PermanentHumanBoundary,
            RequiredAttachmentCount = Math.Max(0, request.RequiredAttachmentCount),
            PrecheckItems = request.PrecheckItems?.Trim(),
            Status = request.Publish ? "Published" : "Draft",
            PublishedBy = user.UserId,
            PublishedAt = now
        };
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.WorkflowNodePolicies.Add(entity);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Workflow",
                    request.Publish ? "workflow.node_policy_published" : "workflow.node_policy_draft_saved",
                    nameof(WorkflowNodePolicyEntity),
                    entity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"{{\"workflowVersionId\":\"{workflowVersionId:D}\",\"nodeCode\":\"{normalizedNode}\",\"version\":{nextVersion}}}"),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(WorkflowNodePolicyEntity),
                    entity.Id.ToString("D"),
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
        return MapPolicy(entity);
    }

    public async Task<WorkflowTaskDto> SimulateTimeoutAsync(
        UserPrincipal user,
        Guid taskId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (!user.IsPlatformAdmin)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "只有平台管理员可以模拟 SLA 超时。");
        }
        ValidateIdempotencyKey(idempotencyKey);
        var task = await dbContext.WorkflowTasks.SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken) ?? throw new NotFoundException("流程任务不存在。");
        if (task.Status != "Pending" || !task.NodePolicyId.HasValue)
        {
            throw new ConflictException(ErrorCodes.WorkflowStateInvalid, "只有绑定了策略的待办任务可以模拟超时。");
        }
        var policy = await dbContext.WorkflowNodePolicies.AsNoTracking()
            .SingleAsync(item => item.Id == task.NodePolicyId.Value, cancellationToken);
        var minutes = new[]
        {
            policy.ReminderMinutes ?? 0,
            policy.EscalationMinutes ?? 0,
            policy.AutoProxyAfterMinutes ?? 0
        }.Max() + 1;
        var now = clock.UtcNow;
        task.CreatedAt = now.AddMinutes(-minutes);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Workflow",
                "task.sla_timeout_simulated",
                nameof(WorkflowTaskEntity),
                task.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: $"{{\"createdAt\":\"{task.CreatedAt:o}\",\"nodePolicyId\":\"{task.NodePolicyId:D}\"}}"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapWorkflowTask(task);
    }

    public async Task<IReadOnlyList<WorkflowNotificationDto>> ListNotificationsAsync(
        UserPrincipal user,
        int pageSize,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-task", AccessAction.Read);
        return await dbContext.WorkflowNotifications.AsNoTracking()
            .Where(item => item.RecipientId == user.UserId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(Math.Clamp(pageSize, 1, 100))
            .Select(item => new WorkflowNotificationDto(
                item.Id.ToString("D"),
                item.RecipientId.ToString("D"),
                item.WorkflowTaskId.ToString("D"),
                item.Type,
                item.Title,
                item.Message,
                item.IsRead,
                item.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<SlaProcessingResultDto> ProcessAsync(
        string traceId,
        CancellationToken cancellationToken)
    {
        var now = clock.UtcNow;
        var tasks = await dbContext.WorkflowTasks
            .Where(item => item.Status == "Pending")
            .OrderBy(item => item.CreatedAt)
            .Take(500)
            .ToListAsync(cancellationToken);
        var policyIds = tasks.Where(item => item.NodePolicyId.HasValue)
            .Select(item => item.NodePolicyId!.Value).Distinct().ToArray();
        var policies = await dbContext.WorkflowNodePolicies.AsNoTracking()
            .Where(item => policyIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var agent = await GetAgentProfileEntityAsync(DefaultAgentName, cancellationToken);
        var reminded = 0;
        var escalated = 0;
        var proposed = 0;
        var proxied = 0;

        foreach (var task in tasks)
        {
            if (string.Equals(task.AssigneeStrategy, nameof(WorkflowAssignmentStrategy.Agent), StringComparison.Ordinal)
                && task.AgentRunId is null)
            {
                await ProcessAgentNodeAsync(task, traceId, now, cancellationToken);
                continue;
            }

            if (!task.NodePolicyId.HasValue
                || !policies.TryGetValue(task.NodePolicyId.Value, out var policy))
            {
                continue;
            }

            var instanceIsActive = await dbContext.WorkflowInstances.AsNoTracking().AnyAsync(
                item => item.Id == task.WorkflowInstanceId
                    && (item.Status == "Running" || item.Status == "Waiting"),
                cancellationToken);
            if (!instanceIsActive)
            {
                continue;
            }

            if (policy.ReminderMinutes is int reminderMinutes
                && task.ReminderSentAt is null
                && task.CreatedAt.AddMinutes(reminderMinutes) <= now)
            {
                task.ReminderSentAt = now;
                task.SlaStatus = "Reminded";
                await AddNotificationAsync(
                    task,
                    task.AssigneeId,
                    "SlaReminder",
                    "流程任务即将到期",
                    $"任务 {task.NodeCode} 已到达提醒时间，请及时处理。",
                    $"sla-reminder:{task.Id:D}",
                    now,
                    cancellationToken);
                await AuditSlaAsync(agent.ServiceUserId, "task.sla_reminded", task, traceId, now, cancellationToken);
                reminded++;
            }

            if (policy.EscalationMinutes is int escalationMinutes
                && task.EscalatedAt is null
                && task.CreatedAt.AddMinutes(escalationMinutes) <= now)
            {
                var recipientId = policy.BackupApproverUserId
                    ?? await ResolveDepartmentManagerAsync(task.DepartmentId, cancellationToken)
                    ?? task.AssigneeId;
                task.EscalatedAt = now;
                task.SlaStatus = "Escalated";
                await AddNotificationAsync(
                    task,
                    recipientId,
                    "SlaEscalated",
                    "流程任务已升级",
                    $"{task.NodeCode} 超时，已升级处理。原责任人：{task.AssigneeId:D}。",
                    $"sla-escalated:{task.Id:D}",
                    now,
                    cancellationToken);
                await AuditSlaAsync(agent.ServiceUserId, "task.sla_escalated", task, traceId, now, cancellationToken);
                escalated++;

                if (policy.AllowAgentAssist
                    && policy.AgentProfileId.HasValue
                    && task.AgentRunId is null
                    && !await dbContext.AgentRuns.AnyAsync(
                        item => item.WorkflowTaskId == task.Id
                            && item.SubjectType == "WorkflowTaskAssist"
                            && item.Status != "Stopped"
                            && item.Status != "Failed",
                        cancellationToken))
                {
                    var profile = await dbContext.AgentProfiles.AsNoTracking()
                        .SingleAsync(item => item.Id == policy.AgentProfileId && item.IsActive, cancellationToken);
                    var organizationId = await ResolveOrganizationIdAsync(task.DepartmentId, cancellationToken);
                    var run = AgentRun.Request(
                        MapProfile(profile),
                        task.AssigneeId,
                        AgentActionType.Draft,
                        0,
                        AgentRiskLevel.Low,
                        null,
                        now,
                        organizationId,
                        task.DepartmentId,
                        task.EnvironmentCode,
                        task.IsTestData,
                        "WorkflowTaskAssist",
                        workflowInstanceId: task.WorkflowInstanceId,
                        workflowTaskId: task.Id,
                        model: "aidt-rule-sim");
                    dbContext.AgentRuns.Add(ToEntity(run));
                    await AddNotificationAsync(
                        task,
                        task.AssigneeId,
                        "AgentAssistProposed",
                        "可请求数字员工辅助",
                        "该节点已授权 Agent 生成处理建议，仍需真人确认。",
                        $"agent-assist:{task.Id:D}",
                        now,
                        cancellationToken);
                    await AuditSlaAsync(agent.ServiceUserId, "task.agent_assisted", task, traceId, now, cancellationToken);
                    proposed++;
                }
            }

            if (policy.AgentProxyMode == nameof(WorkflowAgentProxyMode.PreAuthorizedAutoProxy)
                && policy.AutoProxyAfterMinutes is int autoProxyMinutes
                && task.CreatedAt.AddMinutes(autoProxyMinutes) <= now
                && task.Status == "Pending"
                && task.AgentRunId is null
                && !policy.PermanentHumanBoundary
                && policy.AllowAgentProxy
                && policy.AgentMaxRisk == nameof(AgentRiskLevel.Low)
                && policy.AgentProfileId.HasValue
                && !IsPermanentHumanNode(task.NodeCode))
            {
                var proxyProfile = await dbContext.AgentProfiles.AsNoTracking()
                    .SingleOrDefaultAsync(item => item.Id == policy.AgentProfileId.Value && item.IsActive, cancellationToken);
                if (proxyProfile is not null
                    && Enum.Parse<AgentRiskLevel>(proxyProfile.RiskLimit) == AgentRiskLevel.Low
                    && Enum.Parse<AgentAutonomyLevel>(proxyProfile.AutonomyLevel)
                        >= Enum.Parse<AgentAutonomyLevel>(policy.AgentMaxAutonomy))
                {
                    var idempotencyKey = $"sla-auto-proxy:{task.Id:D}";
                    var run = AgentRun.RequestPreAuthorizedProxy(
                        MapProfile(proxyProfile),
                        proxyProfile.ServiceUserId,
                        task.AssigneeId,
                        await ResolveOrganizationIdAsync(task.DepartmentId, cancellationToken),
                        task.DepartmentId,
                        task.EnvironmentCode,
                        task.IsTestData,
                        task.WorkflowInstanceId,
                        task.Id,
                        await dbContext.WorkflowInstances.AsNoTracking()
                            .Where(item => item.Id == task.WorkflowInstanceId)
                            .Select(item => item.BusinessObjectId)
                            .SingleAsync(cancellationToken),
                        idempotencyKey,
                        now);
                    var runEntity = ToEntity(run);
                    dbContext.AgentRuns.Add(runEntity);
                    task.Status = "Approved";
                    task.CompletedAt = now;
                    task.DecisionComment = "SLA 超时预授权低风险自动代理";
                    task.DecisionMode = "AgentProxy";
                    task.AgentRunId = run.Id;
                    task.Version++;
                    run.Complete();
                    runEntity.Status = run.Status.ToString();
                    runEntity.StartedAt = run.StartedAt;
                    runEntity.EndedAt = run.EndedAt;
                    runEntity.ProxyTriggerMode = run.ProxyTriggerMode;
                    runEntity.ProxyTriggeredAt = run.ProxyTriggeredAt;
                    runEntity.OriginalAssigneeId = run.OriginalAssigneeId;
                    runEntity.CompensationStatus = run.CompensationStatus;
                    runEntity.ProxyIdempotencyKey = run.ProxyIdempotencyKey;
                    await CompleteTerminalProxyAsync(task, now, cancellationToken);
                    dbContext.WorkflowTaskActions.Add(new WorkflowTaskActionEntity
                    {
                        WorkflowTaskId = task.Id,
                        ActorId = proxyProfile.ServiceUserId,
                        Action = "AgentProxy",
                        Comment = idempotencyKey,
                        CreatedAt = now
                    });
                    await AddNotificationAsync(
                        task,
                        task.AssigneeId,
                        "AgentProxied",
                        "数字员工已按超时策略代理",
                        $"任务 {task.NodeCode} 已由独立数字员工身份完成，请查看审计和补偿记录。",
                        $"agent-proxied:{task.Id:D}",
                        now,
                        cancellationToken);
                    await auditRepository.AddAsync(
                        AuditEvent.Record(
                            proxyProfile.ServiceUserId,
                            "Workflow",
                            "task.agent_proxied",
                            nameof(WorkflowTaskEntity),
                            task.Id.ToString("D"),
                            traceId,
                            AuditOutcome.Succeeded,
                            now,
                            before: "Pending",
                            after: "Approved",
                            reason: $"DecisionMode=AgentProxy;Trigger=SlaTimeout;AgentRunId={run.Id:D};NodePolicyId={policy.Id:D};OriginalAssignee={task.AssigneeId:D}"),
                        cancellationToken);
                    proxied++;
                }
                else
                {
                    var recipientId = policy.BackupApproverUserId
                        ?? await ResolveDepartmentManagerAsync(task.DepartmentId, cancellationToken)
                        ?? task.AssigneeId;
                    var notificationAdded = await AddNotificationAsync(
                        task,
                        recipientId,
                        "AgentProxyBlocked",
                        "自动代理配置未通过检查",
                        "任务保持待人工处理：Agent 已停用、等级不足或风险范围不匹配。",
                        $"agent-proxy-blocked:{task.Id:D}",
                        now,
                        cancellationToken);
                    if (notificationAdded)
                    {
                        await AuditSlaAsync(
                            agent.ServiceUserId,
                            "task.agent_proxy_blocked",
                            task,
                            traceId,
                            now,
                            cancellationToken);
                    }
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new SlaProcessingResultDto(now, reminded, escalated, proposed, proxied);
    }

    private async Task ProcessAgentNodeAsync(
        WorkflowTaskEntity task,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.AgentProfiles.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ServiceUserId == task.AssigneeId && item.IsActive,
                cancellationToken);
        var confirmationUserId = await ResolveAgentConfirmationUserAsync(
            task,
            cancellationToken);
        if (profile is null || IsPermanentHumanNode(task.NodeCode))
        {
            await BlockAgentNodeAsync(
                task,
                confirmationUserId,
                profile?.ServiceUserId,
                profile is null ? "数字员工 Profile 不存在或已停用。" : "节点命中永久人工边界。",
                traceId,
                now,
                cancellationToken);
            return;
        }

        if (confirmationUserId is null)
        {
            await BlockAgentNodeAsync(
                task,
                null,
                profile.ServiceUserId,
                "节点没有可用于接收失败或人工确认结果的有效真人。",
                traceId,
                now,
                cancellationToken);
            return;
        }

        var idempotencyKey = $"workflow-agent-node:{task.Id:D}";
        var existingRun = await dbContext.AgentRuns.AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.ProxyIdempotencyKey == idempotencyKey,
                cancellationToken);
        if (existingRun is not null)
        {
            task.AgentRunId = existingRun.Id;
            task.AssigneeId = confirmationUserId.Value;
            if (existingRun.Status == nameof(AgentRunStatus.Completed))
            {
                task.Status = nameof(WorkflowTaskStatus.Approved);
                task.CompletedAt ??= now;
                task.DecisionMode = "AgentProxy";
                task.SlaStatus = "AgentProxied";
                task.Version++;
                await AdvanceAfterAgentNodeAsync(
                    task,
                    profile.ServiceUserId,
                    traceId,
                    now,
                    cancellationToken);
            }
            else
            {
                task.DecisionMode = "AwaitingAgentConfirmation";
                task.SlaStatus = "AwaitingAgentConfirmation";
            }

            return;
        }

        var requiresConfirmation = profile.AutonomyLevel is
            nameof(AgentAutonomyLevel.A0) or nameof(AgentAutonomyLevel.A1);
        var run = AgentRun.Request(
            MapProfile(profile),
            confirmationUserId.Value,
            AgentActionType.Submit,
            0,
            AgentRiskLevel.Low,
            null,
            now,
            await ResolveOrganizationIdAsync(task.DepartmentId, cancellationToken),
            task.DepartmentId,
            task.EnvironmentCode,
            task.IsTestData,
            "WorkflowAgentNode",
            workflowInstanceId: task.WorkflowInstanceId,
            workflowTaskId: task.Id,
            model: "aidt-rule-sim",
            forceConfirmation: requiresConfirmation,
            proxyTriggerMode: "AgentNode",
            originalAssigneeId: task.OriginalAssigneeId ?? confirmationUserId,
            compensationStatus: "Available",
            proxyIdempotencyKey: idempotencyKey);
        var runEntity = ToEntity(run);
        dbContext.AgentRuns.Add(runEntity);
        var call = await InvokeAgentGatewayAsync(runEntity, traceId, cancellationToken);
        dbContext.AgentGatewayCalls.Add(call);
        run.RecordGatewayCall(call.Id, call.Mode, call.ExternalModelCalled);
        runEntity.GatewayCallId = call.Id;
        runEntity.GatewayMode = call.Mode;
        runEntity.ExternalModelCalled = call.ExternalModelCalled;
        runEntity.StartedAt = now;

        if (call.Status != "Succeeded")
        {
            run.Fail();
            runEntity.Status = run.Status.ToString();
            runEntity.EndedAt = run.EndedAt;
            await BlockAgentNodeAsync(
                task,
                confirmationUserId,
                profile.ServiceUserId,
                $"数字员工执行失败：{call.ErrorCode ?? "GATEWAY_ERROR"}。",
                traceId,
                now,
                cancellationToken,
                run.Id);
            return;
        }

        var candidateIds = (JsonSerializer.Deserialize<Guid[]>(task.CandidateUserIdsJson) ?? [])
            .Append(confirmationUserId.Value)
            .Distinct()
            .ToArray();
        task.CandidateUserIdsJson = JsonSerializer.Serialize(candidateIds);
        await EnsureTaskCandidateAsync(task.Id, confirmationUserId.Value, cancellationToken);
        task.AssigneeId = confirmationUserId.Value;
        task.AgentRunId = run.Id;
        task.OriginalAssigneeId ??= profile.ServiceUserId;
        task.Version++;

        if (requiresConfirmation)
        {
            task.DecisionMode = "AwaitingAgentConfirmation";
            task.SlaStatus = "AwaitingAgentConfirmation";
            runEntity.Status = AgentRunStatus.AwaitingConfirmation.ToString();
            runEntity.EndedAt = null;
            dbContext.WorkflowTaskActions.Add(new WorkflowTaskActionEntity
            {
                WorkflowTaskId = task.Id,
                ActorId = profile.ServiceUserId,
                Action = "AgentNodeDrafted",
                Comment = $"agentRun={run.Id:D}; awaiting human confirmation",
                CreatedAt = now
            });
            await AddNotificationAsync(
                task,
                confirmationUserId.Value,
                "AgentConfirmationRequired",
                "数字员工结果等待人工确认",
                $"节点 {task.NodeCode} 已生成代理结果，请由真人确认后推进流程。",
                $"agent-confirmation:{task.Id:D}",
                now,
                cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    profile.ServiceUserId,
                    "Workflow",
                    "task.agent_node_awaiting_confirmation",
                    nameof(WorkflowTaskEntity),
                    task.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"agentRun={run.Id:D}; confirmationUser={confirmationUserId:D}; gatewayCall={call.Id:D}"),
                cancellationToken);
            return;
        }

        run.Complete();
        runEntity.Status = run.Status.ToString();
        runEntity.EndedAt = run.EndedAt;
        task.Status = nameof(WorkflowTaskStatus.Approved);
        task.CompletedAt = now;
        task.DecisionComment = $"AgentNode:{run.Id:D}";
        task.DecisionMode = "AgentProxy";
        task.SlaStatus = "AgentProxied";
        dbContext.WorkflowTaskActions.Add(new WorkflowTaskActionEntity
        {
            WorkflowTaskId = task.Id,
            ActorId = profile.ServiceUserId,
            Action = "AgentProxy",
            Comment = $"agentRun={run.Id:D}; gatewayCall={call.Id:D}",
            CreatedAt = now
        });
        await auditRepository.AddAsync(
            AuditEvent.Record(
                profile.ServiceUserId,
                "Workflow",
                "task.agent_node_completed",
                nameof(WorkflowTaskEntity),
                task.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: nameof(WorkflowTaskStatus.Pending),
                after: nameof(WorkflowTaskStatus.Approved),
                reason: $"agentRun={run.Id:D}; gatewayCall={call.Id:D}; confirmationUser={confirmationUserId:D}"),
            cancellationToken);
        await AddNotificationAsync(
            task,
            confirmationUserId.Value,
            "AgentProxied",
            "低风险节点已由数字员工完成",
            $"节点 {task.NodeCode} 已按 Agent Profile 自主等级执行，可查看运行与网关证据。",
            $"agent-proxied:{task.Id:D}",
            now,
            cancellationToken);
        await AdvanceAfterAgentNodeAsync(task, profile.ServiceUserId, traceId, now, cancellationToken);
    }

    private async Task BlockAgentNodeAsync(
        WorkflowTaskEntity task,
        Guid? confirmationUserId,
        Guid? serviceUserId,
        string reason,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        Guid? failedRunId = null)
    {
        if (confirmationUserId.HasValue)
        {
            var candidateIds = (JsonSerializer.Deserialize<Guid[]>(task.CandidateUserIdsJson) ?? [])
                .Append(confirmationUserId.Value)
                .Distinct()
                .ToArray();
            task.CandidateUserIdsJson = JsonSerializer.Serialize(candidateIds);
            task.AssigneeId = confirmationUserId.Value;
            await EnsureTaskCandidateAsync(task.Id, confirmationUserId.Value, cancellationToken);
        }

        task.Status = nameof(WorkflowTaskStatus.Pending);
        task.DecisionMode = "AgentBlocked";
        task.SlaStatus = "AgentBlocked";
        task.AgentRunId = failedRunId;
        task.Version++;
        dbContext.WorkflowTaskActions.Add(new WorkflowTaskActionEntity
        {
            WorkflowTaskId = task.Id,
            ActorId = serviceUserId ?? task.AssigneeId,
            Action = "AgentBlocked",
            Comment = reason,
            CreatedAt = now
        });
        if (confirmationUserId.HasValue)
        {
            await AddNotificationAsync(
                task,
                confirmationUserId.Value,
                "AgentBlocked",
                "数字员工节点已阻断",
                reason,
                $"agent-blocked:{task.Id:D}",
                now,
                cancellationToken);
        }
        await auditRepository.AddAsync(
            AuditEvent.Record(
                serviceUserId ?? task.AssigneeId,
                "Workflow",
                "task.agent_node_blocked",
                nameof(WorkflowTaskEntity),
                task.Id.ToString("D"),
                traceId,
                AuditOutcome.Denied,
                now,
                reason: reason),
            cancellationToken);
    }

    private async Task<Guid?> ResolveAgentConfirmationUserAsync(
        WorkflowTaskEntity task,
        CancellationToken cancellationToken)
    {
        var organizationId = await ResolveOrganizationIdAsync(task.DepartmentId, cancellationToken);
        var candidates = new[]
        {
            task.OriginalAssigneeId,
            TryReadInitiatorUserId(task.AssigneeSnapshotJson),
            await dbContext.WorkflowInstances.AsNoTracking()
                .Where(item => item.Id == task.WorkflowInstanceId)
                .Select(item => item.StartedByUserId)
                .SingleOrDefaultAsync(cancellationToken),
            await ResolveDepartmentManagerAsync(task.DepartmentId, cancellationToken)
        };
        foreach (var candidate in candidates.Where(item => item.HasValue).Select(item => item!.Value).Distinct())
        {
            var valid = await dbContext.Users.AsNoTracking().AnyAsync(
                item => item.Id == candidate
                    && item.IsActive
                    && !item.IsServiceIdentity
                    && item.OrganizationId == organizationId,
                cancellationToken);
            if (valid)
            {
                return candidate;
            }
        }

        return await (
            from membership in dbContext.UserDepartmentMemberships.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on membership.UserId equals user.Id
            where membership.DepartmentId == task.DepartmentId
                && membership.IsActive
                && user.IsActive
                && !user.IsServiceIdentity
            orderby membership.IsPrimary descending, user.Id
            select (Guid?)user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task AdvanceAfterAgentNodeAsync(
        WorkflowTaskEntity taskEntity,
        Guid serviceUserId,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var task = await workflowRepository.GetTaskAsync(taskEntity.Id, cancellationToken)
            ?? throw new NotFoundException("Agent 节点任务不存在。");
        var instance = await workflowRepository.GetInstanceAsync(
            taskEntity.WorkflowInstanceId,
            cancellationToken) ?? throw new NotFoundException("Agent 节点流程实例不存在。");
        var businessObject = await businessObjectRepository.GetByIdAsync(
            instance.BusinessObjectId,
            cancellationToken) ?? throw new NotFoundException("Agent 节点业务对象不存在。");
        var advance = await workflowEngine.AdvanceAsync(
            instance,
            task,
            WorkflowDecision.Approve,
            cancellationToken);
        if (advance.NextNodeCode is not null
            && advance.Assignment is not null)
        {
            var assignment = advance.Assignment;
            var nextDepartmentId = assignment.Strategy == WorkflowAssignmentStrategy.Agent
                ? instance.DepartmentId
                : await departmentRepository.ResolveAssigneeDepartmentAsync(
                    businessObject.OrganizationId,
                    assignment.PrimaryAssigneeId,
                    cancellationToken);
            instance.MoveTo(advance.NextNodeCode, instance.Version);
            var nextTask = WorkflowTask.Create(
                instance.Id,
                advance.NextNodeCode,
                assignment.PrimaryAssigneeId,
                now,
                nextDepartmentId,
                instance.EnvironmentCode,
                instance.IsTestData,
                assigneeStrategy: assignment.Strategy,
                businessRoleId: assignment.BusinessRoleId,
                businessRoleCode: assignment.BusinessRoleCode,
                businessRoleVersion: assignment.BusinessRoleVersion,
                candidateUserIds: assignment.CandidateUserIds,
                assigneeSnapshotJson: assignment.SnapshotJson,
                backupAssigneeId: assignment.BackupAssigneeId,
                originalAssigneeId: assignment.Strategy == WorkflowAssignmentStrategy.Agent
                    ? instance.StartedByUserId
                    : null);
            await workflowRepository.AddTaskAsync(nextTask, cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    serviceUserId,
                    "Workflow",
                    "workflow.assignment_resolved",
                    nameof(WorkflowTaskEntity),
                    nextTask.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: assignment.SnapshotJson),
                cancellationToken);
        }
        else if (advance.InstanceStatus != instance.Status)
        {
            instance.Complete(WorkflowDecision.Approve, instance.Version, now);
        }

        await workflowRepository.SaveInstanceAsync(instance, cancellationToken);
    }

    private async Task<AgentGatewayCallEntity> InvokeAgentGatewayAsync(
        AgentRunEntity runEntity,
        string traceId,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var model = string.IsNullOrWhiteSpace(runEntity.Model) ? "aidt-rule-sim" : runEntity.Model;
        var inputPayload = JsonSerializer.Serialize(new
        {
            runId = runEntity.Id,
            subjectType = runEntity.SubjectType,
            workflowTaskId = runEntity.WorkflowTaskId,
            workflowInstanceId = runEntity.WorkflowInstanceId
        });
        var call = new AgentGatewayCallEntity
        {
            Id = Guid.NewGuid(),
            AgentRunId = runEntity.Id,
            OrganizationId = runEntity.OrganizationId,
            DepartmentId = runEntity.DepartmentId,
            EnvironmentCode = runEntity.EnvironmentCode,
            Model = model,
            Mode = "RuleSimulator",
            InputDigest = Sha(inputPayload),
            Status = "Pending",
            CreatedAt = clock.UtcNow
        };
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = JsonContent.Create(new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = "AIDT deterministic workflow agent node" },
                        new { role = "user", content = inputPayload }
                    },
                    temperature = 0
                })
            };
            request.Headers.Add("X-AIDT-Caller-Identity", runEntity.RequestedBy.ToString("D"));
            request.Headers.Add("X-AIDT-Agent-Run-Id", runEntity.Id.ToString("D"));
            request.Headers.Add("X-AIDT-Trace-Id", traceId);
            var client = httpClientFactory.CreateClient("AiGateway");
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            call.DurationMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            call.Status = response.IsSuccessStatusCode ? "Succeeded" : "Failed";
            call.ErrorCode = response.IsSuccessStatusCode ? null : ExtractGatewayCode(body);
            call.ErrorMessage = response.IsSuccessStatusCode ? null : Truncate(body, 2000);
            if (response.IsSuccessStatusCode)
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                call.GatewayCallId = root.TryGetProperty("id", out var id) ? id.GetString() : null;
                if (root.TryGetProperty("audit", out var audit))
                {
                    call.Mode = audit.TryGetProperty("mode", out var mode)
                        ? mode.GetString() ?? "RuleSimulator"
                        : "RuleSimulator";
                    call.ExternalModelCalled = audit.TryGetProperty("externalModelCalled", out var external)
                        && external.GetBoolean();
                    call.OutputDigest = audit.TryGetProperty("outputDigest", out var output)
                        ? output.GetString()
                        : null;
                }
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            call.DurationMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            call.Status = exception is TaskCanceledException ? "TimedOut" : "Failed";
            call.ErrorCode = exception is TaskCanceledException ? "GATEWAY_TIMEOUT" : "GATEWAY_UNAVAILABLE";
            call.ErrorMessage = exception.Message;
            call.Mode = "RuleSimulator";
        }

        return call;
    }

    private async Task EnsureTaskCandidateAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (await dbContext.WorkflowTaskCandidates.AnyAsync(
                item => item.WorkflowTaskId == taskId && item.UserId == userId,
                cancellationToken))
        {
            return;
        }

        dbContext.WorkflowTaskCandidates.Add(new WorkflowTaskCandidateEntity
        {
            WorkflowTaskId = taskId,
            UserId = userId
        });
    }

    private static Guid? TryReadInitiatorUserId(string snapshotJson)
    {
        try
        {
            using var document = JsonDocument.Parse(snapshotJson);
            return document.RootElement.TryGetProperty("initiatorUserId", out var value)
                && value.ValueKind == JsonValueKind.String
                && Guid.TryParse(value.GetString(), out var userId)
                ? userId
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<AgentExecutionResultDto> ExecuteConfirmedRunAsync(
        UserPrincipal user,
        Guid runId,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("agent-run", AccessAction.Update);
        var runEntity = await dbContext.AgentRuns
            .SingleOrDefaultAsync(item => item.Id == runId, cancellationToken)
            ?? throw new NotFoundException("Agent 运行记录不存在。");
        if (runEntity.OrganizationId != Guid.Empty)
        {
            user.EnsureOrganizationAccess(runEntity.OrganizationId);
            user.EnsureDepartmentAccess(runEntity.DepartmentId);
        }
        if (runEntity.Status != "Running")
        {
            throw new ConflictException(ErrorCodes.AgentConfirmationRequired, "Agent 运行尚未通过真人确认或不在运行状态。");
        }

        var started = Stopwatch.GetTimestamp();
        var model = string.IsNullOrWhiteSpace(runEntity.Model) ? "aidt-rule-sim" : runEntity.Model;
        var inputPayload = JsonSerializer.Serialize(new
        {
            runId = runEntity.Id,
            subjectType = runEntity.SubjectType,
            minutesSourceId = runEntity.MinutesSourceId,
            workflowTaskId = runEntity.WorkflowTaskId
        });
        var inputDigest = Sha(inputPayload);
        var call = new AgentGatewayCallEntity
        {
            Id = Guid.NewGuid(),
            AgentRunId = runEntity.Id,
            OrganizationId = runEntity.OrganizationId,
            DepartmentId = runEntity.DepartmentId,
            EnvironmentCode = runEntity.EnvironmentCode,
            Model = model,
            Mode = "RuleSimulator",
            InputDigest = inputDigest,
            Status = "Pending",
            CreatedAt = clock.UtcNow
        };
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
            {
                Content = JsonContent.Create(new
                {
                    model,
                    messages = new[]
                    {
                        new { role = "system", content = "AIDT deterministic minutes action extractor" },
                        new { role = "user", content = inputPayload }
                    },
                    temperature = 0
                })
            };
            request.Headers.Add("X-AIDT-Caller-Identity", runEntity.RequestedBy.ToString("D"));
            request.Headers.Add("X-AIDT-Agent-Run-Id", runEntity.Id.ToString("D"));
            request.Headers.Add("X-AIDT-Trace-Id", traceId);
            var client = httpClientFactory.CreateClient("AiGateway");
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            call.DurationMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            call.Status = response.IsSuccessStatusCode ? "Succeeded" : "Failed";
            call.ErrorCode = response.IsSuccessStatusCode ? null : ExtractGatewayCode(body);
            call.ErrorMessage = response.IsSuccessStatusCode ? null : Truncate(body, 2000);
            if (response.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;
                call.GatewayCallId = root.TryGetProperty("id", out var id) ? id.GetString() : null;
                if (root.TryGetProperty("audit", out var audit))
                {
                    call.Mode = audit.TryGetProperty("mode", out var mode) ? mode.GetString() ?? "RuleSimulator" : "RuleSimulator";
                    call.ExternalModelCalled = audit.TryGetProperty("externalModelCalled", out var external) && external.GetBoolean();
                    call.OutputDigest = audit.TryGetProperty("outputDigest", out var output) ? output.GetString() : null;
                }
            }
            else
            {
                runEntity.Status = "Failed";
                runEntity.EndedAt = clock.UtcNow;
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            call.DurationMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            call.Status = exception is TaskCanceledException ? "TimedOut" : "Failed";
            call.ErrorCode = exception is TaskCanceledException ? "GATEWAY_TIMEOUT" : "GATEWAY_UNAVAILABLE";
            call.ErrorMessage = exception.Message;
            call.Mode = "RuleSimulator";
            runEntity.Status = "Failed";
            runEntity.EndedAt = clock.UtcNow;
        }

        List<MinutesActionItemEntity> generated = [];
        if (call.Status == "Succeeded" && runEntity.SubjectType == "MinutesSource" && runEntity.MinutesSourceId.HasValue)
        {
            var source = await dbContext.MinutesSources.SingleAsync(
                item => item.Id == runEntity.MinutesSourceId.Value,
                cancellationToken);
            generated = ExtractActionItems(source, runEntity.Id);
            foreach (var item in generated)
            {
                dbContext.MinutesActionItems.Add(item);
            }
            source.Status = "Analyzed";
            source.AnalysisVersion = AnalysisVersion;
            source.UpdatedAt = clock.UtcNow;
            runEntity.Status = "Completed";
            runEntity.EndedAt = clock.UtcNow;
            runEntity.OutputDigest = call.OutputDigest;
            var serviceActorId = await dbContext.AgentProfiles.AsNoTracking()
                .Where(item => item.Id == runEntity.AgentProfileId)
                .Select(item => item.ServiceUserId)
                .SingleAsync(cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    serviceActorId,
                    "Minutes",
                    "minutes.analyzed",
                    nameof(MinutesSourceEntity),
                    source.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    clock.UtcNow,
                    after: $"{{\"agentRunId\":\"{runEntity.Id:D}\",\"actions\":{generated.Count},\"externalModelCalled\":{call.ExternalModelCalled.ToString().ToLowerInvariant()}}}"),
                cancellationToken);
        }

        runEntity.GatewayCallId = call.Id;
        runEntity.GatewayMode = call.Mode;
        runEntity.ExternalModelCalled = call.ExternalModelCalled;
        dbContext.AgentGatewayCalls.Add(call);
        await dbContext.SaveChangesAsync(cancellationToken);

        var run = MapAgentRun(runEntity);
        return new AgentExecutionResultDto(
            run.ToDto(),
            MapGatewayCall(call),
            generated.Select(MapActionItem).ToList());
    }

    public async Task<IReadOnlyList<AgentGatewayCallDto>> ListCallsAsync(
        UserPrincipal user,
        Guid runId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("agent-run", AccessAction.Read);
        var run = await dbContext.AgentRuns.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == runId,
            cancellationToken) ?? throw new NotFoundException("Agent 运行记录不存在。");
        if (run.OrganizationId != Guid.Empty)
        {
            user.EnsureOrganizationAccess(run.OrganizationId);
            if (!user.IsPlatformAdmin)
            {
                user.EnsureDepartmentAccess(run.DepartmentId);
            }
        }
        var rows = await dbContext.AgentGatewayCalls.AsNoTracking()
            .Where(item => item.AgentRunId == runId)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return rows.Select(MapGatewayCall).ToList();
    }

    public async Task<AgentRunDto> RequestProxyAsync(
        UserPrincipal user,
        Guid taskId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-task", AccessAction.Approve);
        ValidateIdempotencyKey(idempotencyKey);
        var task = await dbContext.WorkflowTasks.SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken) ?? throw new NotFoundException("流程任务不存在。");
        user.EnsureDepartmentAccess(task.DepartmentId);
        if (task.Status != "Pending")
        {
            throw new ConflictException(ErrorCodes.TaskAlreadyProcessed, "任务已处理，不能请求代理。");
        }
        var policy = task.NodePolicyId.HasValue
            ? await dbContext.WorkflowNodePolicies.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == task.NodePolicyId, cancellationToken)
            : null;
        if (policy is null || !policy.AllowAgentProxy || policy.PermanentHumanBoundary
            || policy.AgentMaxRisk != "Low" || !policy.AgentProfileId.HasValue)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "该节点未授权低风险数字员工代理。");
        }
        if (IsPermanentHumanNode(task.NodeCode))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "高风险节点永久保留人工处理。");
        }
        var existing = await dbContext.AgentRuns.AsNoTracking().FirstOrDefaultAsync(
            item => item.WorkflowTaskId == taskId
                && item.SubjectType == "WorkflowTaskProxy"
                && item.Status != "Stopped"
                && item.Status != "Failed",
            cancellationToken);
        if (existing is not null)
        {
            return MapAgentRun(existing).ToDto();
        }

        var profileEntity = await dbContext.AgentProfiles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == policy.AgentProfileId && item.IsActive, cancellationToken)
            ?? throw new NotFoundException("代理 Agent 配置不存在或已停用。");
        var instance = await dbContext.WorkflowInstances.AsNoTracking()
            .SingleAsync(item => item.Id == task.WorkflowInstanceId, cancellationToken);
        var organizationId = await ResolveOrganizationIdAsync(task.DepartmentId, cancellationToken);
        var run = AgentRun.Request(
            MapProfile(profileEntity),
            user.UserId,
            AgentActionType.Submit,
            0,
            AgentRiskLevel.Low,
            null,
            clock.UtcNow,
            organizationId,
            task.DepartmentId,
            task.EnvironmentCode,
            task.IsTestData,
            "WorkflowTaskProxy",
            businessObjectId: instance.BusinessObjectId,
            workflowInstanceId: task.WorkflowInstanceId,
            workflowTaskId: task.Id,
            model: "aidt-rule-sim",
            forceConfirmation: true);
        dbContext.AgentRuns.Add(ToEntity(run));
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Workflow",
                "task.agent_assisted",
                nameof(WorkflowTaskEntity),
                task.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                clock.UtcNow,
                after: $"{{\"agentRunId\":\"{run.Id:D}\",\"mode\":\"AwaitingConfirmation\"}}"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return run.ToDto();
    }

    public async Task<AgentRunDto> ConfirmProxyAsync(
        UserPrincipal user,
        Guid runId,
        ConfirmAgentProxyRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "数字员工不能确认自己的代理运行。");
        }
        user.EnsureCan("workflow-task", AccessAction.Approve);
        var runEntity = await dbContext.AgentRuns.SingleOrDefaultAsync(
            item => item.Id == runId,
            cancellationToken) ?? throw new NotFoundException("Agent 运行记录不存在。");
        if (runEntity.SubjectType == "WorkflowAgentNode" && runEntity.WorkflowTaskId.HasValue)
        {
            return await ConfirmWorkflowAgentNodeAsync(
                user,
                runEntity,
                request,
                traceId,
                cancellationToken);
        }

        if (runEntity.SubjectType != "WorkflowTaskProxy" || !runEntity.WorkflowTaskId.HasValue)
        {
            throw new ValidationException("该 AgentRun 不是流程代理运行。");
        }
        var task = await dbContext.WorkflowTasks.SingleAsync(
            item => item.Id == runEntity.WorkflowTaskId,
            cancellationToken);
        user.EnsureDepartmentAccess(task.DepartmentId);
        var policy = task.NodePolicyId.HasValue
            ? await dbContext.WorkflowNodePolicies.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == task.NodePolicyId, cancellationToken)
            : null;
        if (policy is null
            || !policy.AllowAgentProxy
            || policy.PermanentHumanBoundary
            || policy.AgentMaxRisk != "Low"
            || IsPermanentHumanNode(task.NodeCode))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "代理策略不再允许该节点由数字员工执行。");
        }
        if (runEntity.Status == "Completed" || task.Status == "Approved")
        {
            return MapAgentRun(runEntity).ToDto();
        }
        if (runEntity.Status != "AwaitingConfirmation")
        {
            throw new ConflictException(ErrorCodes.Conflict, "代理运行不在待确认状态。");
        }
        var profileEntity = await dbContext.AgentProfiles.SingleAsync(
            item => item.Id == runEntity.AgentProfileId && item.IsActive,
            cancellationToken);
        if (profileEntity.ServiceUserId == user.UserId)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "真人确认身份不能与 Agent 服务身份相同。");
        }
        var now = clock.UtcNow;
        var run = MapAgentRun(runEntity);
        run.Confirm(request.ConfirmationId, true);
        dbContext.AgentConfirmations.Add(new AgentConfirmationEntity
        {
            Id = request.ConfirmationId,
            AgentRunId = run.Id,
            ConfirmedBy = user.UserId,
            Decision = "Approved",
            Comment = "confirmed-proxy",
            ConfirmedAt = now
        });
        task.Status = "Approved";
        task.CompletedAt = now;
        task.DecisionComment = $"AgentProxy:{run.Id:D}";
        task.DecisionMode = "AgentProxy";
        task.AgentRunId = run.Id;
        task.Version++;
        dbContext.WorkflowTaskActions.Add(new WorkflowTaskActionEntity
        {
            WorkflowTaskId = task.Id,
            ActorId = profileEntity.ServiceUserId,
            Action = "AgentProxy",
            Comment = $"confirmedBy={user.UserId:D}",
            CreatedAt = now
        });
        run.Complete();
        runEntity.Status = run.Status.ToString();
        runEntity.ConfirmationId = run.ConfirmationId;
        runEntity.StartedAt = run.StartedAt;
        runEntity.EndedAt = run.EndedAt;
        await AddNotificationAsync(
            task,
            task.AssigneeId,
            "AgentProxied",
            "数字员工代理结果",
            $"任务 {task.NodeCode} 已由受限数字员工代理完成，可查看审计和补偿记录。",
            $"agent-proxied:{task.Id:D}",
            now,
            cancellationToken);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                profileEntity.ServiceUserId,
                "Workflow",
                "task.agent_proxied",
                nameof(WorkflowTaskEntity),
                task.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: "Pending",
                after: "Approved",
                reason: $"agentRun={run.Id:D}; confirmedBy={user.UserId:D}; DecisionMode=AgentProxy"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAgentRun(runEntity).ToDto();
    }

    private async Task<AgentRunDto> ConfirmWorkflowAgentNodeAsync(
        UserPrincipal user,
        AgentRunEntity runEntity,
        ConfirmAgentProxyRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.WorkflowTasks.SingleAsync(
            item => item.Id == runEntity.WorkflowTaskId,
            cancellationToken);
        user.EnsureDepartmentAccess(task.DepartmentId);
        if (task.AssigneeId != user.UserId)
        {
            throw new PermissionException(
                ErrorCodes.TaskAssigneeMismatch,
                "只有当前人工确认人可以确认数字员工节点结果。");
        }
        var candidateIds = JsonSerializer.Deserialize<Guid[]>(task.CandidateUserIdsJson) ?? [];
        if (!candidateIds.Contains(user.UserId))
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "当前身份不在数字员工节点的人工确认候选资格中。");
        }
        if (runEntity.Status == nameof(AgentRunStatus.Completed)
            || task.Status == nameof(WorkflowTaskStatus.Approved))
        {
            return MapAgentRun(runEntity).ToDto();
        }
        if (runEntity.Status != nameof(AgentRunStatus.AwaitingConfirmation))
        {
            throw new ConflictException(ErrorCodes.Conflict, "数字员工节点不在待人工确认状态。");
        }

        var profile = await dbContext.AgentProfiles.AsNoTracking().SingleAsync(
            item => item.Id == runEntity.AgentProfileId && item.IsActive,
            cancellationToken);
        if (profile.ServiceUserId == user.UserId)
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "数字员工不能确认自己的节点结果。");
        }
        if (IsPermanentHumanNode(task.NodeCode))
        {
            throw new PermissionException(ErrorCodes.PermissionDenied, "永久人工边界节点不能通过 Agent 确认。");
        }

        var now = clock.UtcNow;
        var run = MapAgentRun(runEntity);
        run.Confirm(request.ConfirmationId, true);
        run.Complete();
        dbContext.AgentConfirmations.Add(new AgentConfirmationEntity
        {
            Id = request.ConfirmationId,
            AgentRunId = run.Id,
            ConfirmedBy = user.UserId,
            Decision = "Approved",
            Comment = "confirmed-workflow-agent-node",
            ConfirmedAt = now
        });
        runEntity.Status = run.Status.ToString();
        runEntity.ConfirmationId = run.ConfirmationId;
        runEntity.StartedAt ??= now;
        runEntity.EndedAt = run.EndedAt;

        task.Status = nameof(WorkflowTaskStatus.Approved);
        task.CompletedAt = now;
        task.DecisionComment = $"AgentNode:{run.Id:D}; confirmedBy={user.UserId:D}";
        task.DecisionMode = "AgentProxy";
        task.AgentRunId = run.Id;
        task.Version++;
        dbContext.WorkflowTaskActions.Add(new WorkflowTaskActionEntity
        {
            WorkflowTaskId = task.Id,
            ActorId = profile.ServiceUserId,
            Action = "AgentProxy",
            Comment = $"agentRun={run.Id:D}; confirmedBy={user.UserId:D}",
            CreatedAt = now
        });
        await auditRepository.AddAsync(
            AuditEvent.Record(
                profile.ServiceUserId,
                "Workflow",
                "task.agent_node_human_confirmed",
                nameof(WorkflowTaskEntity),
                task.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                before: nameof(WorkflowTaskStatus.Pending),
                after: nameof(WorkflowTaskStatus.Approved),
                reason: $"agentRun={run.Id:D}; confirmedBy={user.UserId:D}"),
            cancellationToken);
        await AddNotificationAsync(
            task,
            user.UserId,
            "AgentProxied",
            "数字员工节点已由真人确认",
            $"节点 {task.NodeCode} 的代理结果已确认，流程将继续推进。",
            $"agent-node-confirmed:{task.Id:D}",
            now,
            cancellationToken);
        await AdvanceAfterAgentNodeAsync(task, profile.ServiceUserId, traceId, now, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return MapAgentRun(runEntity).ToDto();
    }

    private async Task<MinutesSourceEntity> LoadAccessibleSourceAsync(
        UserPrincipal user,
        Guid sourceId,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        var source = await ApplySourceScope(user, includeTestData, dbContext.MinutesSources)
            .SingleOrDefaultAsync(item => item.Id == sourceId, cancellationToken);
        return source ?? throw new NotFoundException("听记来源不存在或无权访问。");
    }

    private DateTimeOffset CurrentBusinessWeekStart()
    {
        var timeZone = businessTimeOptions.ResolveTimeZone();
        var localNow = TimeZoneInfo.ConvertTime(clock.UtcNow, timeZone);
        var daysSinceMonday = ((int)localNow.DayOfWeek + 6) % 7;
        var localMonday = localNow.Date.AddDays(-daysSinceMonday);
        var unspecifiedMonday = DateTime.SpecifyKind(localMonday, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecifiedMonday, timeZone.GetUtcOffset(unspecifiedMonday))
            .ToUniversalTime();
    }

    private IQueryable<MinutesSourceEntity> ApplySourceScope(
        UserPrincipal user,
        bool includeTestData,
        IQueryable<MinutesSourceEntity> query)
    {
        user.EnsureOrganizationAccess(user.OrganizationId);
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var includeAll = user.IsPlatformAdmin && includeTestData;
        if (!user.IsPlatformAdmin)
        {
            var departmentIds = user.DepartmentIds;
            if (departmentIds.Count == 0)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份没有部门数据范围。");
            }
            query = query.Where(item => departmentIds.Contains(item.DepartmentId));
        }
        return query.Where(item => item.OrganizationId == user.OrganizationId
            && (includeAll || item.EnvironmentCode == environment));
    }

    private async Task EnsureDepartmentAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.Departments.AsNoTracking().AnyAsync(
            item => item.Id == departmentId
                && item.OrganizationId == user.OrganizationId
                && item.IsActive,
            cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("部门不存在或已停用。");
        }
        user.EnsureDepartmentAccess(departmentId);
    }

    private async Task<Guid?> ValidateOptionalUserAsync(
        Guid organizationId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return null;
        }
        var exists = await dbContext.Users.AsNoTracking().AnyAsync(
            item => item.Id == userId && item.OrganizationId == organizationId && item.IsActive,
            cancellationToken);
        return exists ? userId : throw new ValidationException("用户不属于当前组织或已停用。");
    }

    private async Task<Guid?> ValidateOptionalUserInDepartmentAsync(
        Guid organizationId,
        Guid departmentId,
        Guid? userId,
        CancellationToken cancellationToken)
    {
        if (!userId.HasValue)
        {
            return null;
        }
        var exists = await (
            from account in dbContext.Users.AsNoTracking()
            join membership in dbContext.UserDepartmentMemberships.AsNoTracking()
                on account.Id equals membership.UserId
            where account.Id == userId
                && account.OrganizationId == organizationId
                && account.IsActive
                && !account.IsServiceIdentity
                && membership.DepartmentId == departmentId
                && membership.IsActive
            select account.Id).AnyAsync(cancellationToken);
        return exists ? userId : throw new ValidationException("负责人不是该听记部门的有效真人成员。");
    }

    private async Task<(Guid? ProjectId, Guid? BusinessObjectId, Guid? WorkflowInstanceId)>
        ValidateConfirmationLinksAsync(
            Guid organizationId,
            Guid departmentId,
            string environmentCode,
            Guid? projectId,
            Guid? businessObjectId,
            Guid? workflowInstanceId,
            CancellationToken cancellationToken)
    {
        if (projectId.HasValue)
        {
            var exists = await (
                from project in dbContext.BusinessObjects.AsNoTracking()
                join objectType in dbContext.ObjectTypes.AsNoTracking()
                    on project.ObjectTypeId equals objectType.Id
                where project.Id == projectId
                    && project.OrganizationId == organizationId
                    && project.DepartmentId == departmentId
                    && project.EnvironmentCode == environmentCode
                    && !project.IsDeleted
                    && objectType.Code == "PROJECT"
                select project.Id).AnyAsync(cancellationToken);
            if (!exists)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "关联项目越权、环境或对象类型不一致。");
            }
        }

        if (businessObjectId.HasValue)
        {
            var exists = await dbContext.BusinessObjects.AsNoTracking().AnyAsync(
                item => item.Id == businessObjectId
                    && item.OrganizationId == organizationId
                    && item.DepartmentId == departmentId
                    && item.EnvironmentCode == environmentCode
                    && !item.IsDeleted,
                cancellationToken);
            if (!exists)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "关联业务对象越权或环境不一致。");
            }
        }

        if (workflowInstanceId.HasValue)
        {
            var workflow = await dbContext.WorkflowInstances.AsNoTracking()
                .SingleOrDefaultAsync(
                    item => item.Id == workflowInstanceId
                        && item.DepartmentId == departmentId
                        && item.EnvironmentCode == environmentCode,
                    cancellationToken)
                ?? throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "关联流程越权或环境不一致。");
            if ((projectId.HasValue || businessObjectId.HasValue)
                && workflow.BusinessObjectId != projectId
                && workflow.BusinessObjectId != businessObjectId)
            {
                throw new ValidationException("关联流程必须属于已选择的项目或业务对象。");
            }
        }

        return (projectId, businessObjectId, workflowInstanceId);
    }

    private static string BuildMinutesMissingInformation(
        Guid? assigneeId,
        DateTimeOffset? dueAt,
        string content,
        string? existing)
    {
        var missing = new List<string>();
        if (!assigneeId.HasValue) missing.Add("负责人");
        if (!dueAt.HasValue) missing.Add("截止时间");
        if (string.IsNullOrWhiteSpace(content)) missing.Add("任务内容");
        if (!string.IsNullOrWhiteSpace(existing)) missing.Add(existing.Trim());
        return string.Join("、", missing.Distinct(StringComparer.Ordinal));
    }

    private async Task AddMinutesProjectRelationsAsync(
        Guid projectId,
        MinutesActionItemEntity action,
        MinutesTaskEntity task,
        Guid? businessObjectId,
        Guid? workflowInstanceId,
        Guid actorId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await AddProjectRelationIfMissingAsync(
            projectId, "MinutesActionItem", action.Id, "SourceActionItem", actorId, now, cancellationToken);
        await AddProjectRelationIfMissingAsync(
            projectId, "MinutesTask", task.Id, "ActionTask", actorId, now, cancellationToken);
        if (businessObjectId.HasValue)
        {
            await AddProjectRelationIfMissingAsync(
                projectId, "BusinessObject", businessObjectId.Value, "RelatedBusinessObject", actorId, now, cancellationToken);
        }
        if (workflowInstanceId.HasValue)
        {
            await AddProjectRelationIfMissingAsync(
                projectId, "WorkflowInstance", workflowInstanceId.Value, "ProjectFlow", actorId, now, cancellationToken);
        }
    }

    private async Task AddProjectRelationIfMissingAsync(
        Guid projectId,
        string entityType,
        Guid entityId,
        string relationCode,
        Guid actorId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.ProjectRelations.AsNoTracking().AnyAsync(
            item => item.ProjectId == projectId
                && item.EntityType == entityType
                && item.EntityId == entityId
                && item.RelationCode == relationCode,
            cancellationToken);
        if (!exists)
        {
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
    }

    private async Task<Guid?> ValidateOptionalObjectTypeAsync(
        Guid? objectTypeId,
        CancellationToken cancellationToken)
    {
        if (!objectTypeId.HasValue)
        {
            return null;
        }
        return await dbContext.ObjectTypes.AsNoTracking().AnyAsync(
            item => item.Id == objectTypeId,
            cancellationToken)
            ? objectTypeId
            : throw new NotFoundException("业务对象类型不存在。");
    }

    private async Task<Guid?> ValidateOptionalWorkflowVersionAsync(
        Guid? workflowVersionId,
        CancellationToken cancellationToken)
    {
        if (!workflowVersionId.HasValue)
        {
            return null;
        }
        return await dbContext.WorkflowVersions.AsNoTracking().AnyAsync(
            item => item.Id == workflowVersionId && item.Status == "Published",
            cancellationToken)
            ? workflowVersionId
            : throw new NotFoundException("已发布流程版本不存在。");
    }

    private async Task<Guid?> ResolveDepartmentManagerAsync(
        Guid departmentId,
        CancellationToken cancellationToken) =>
        await dbContext.Departments.AsNoTracking()
            .Where(item => item.Id == departmentId)
            .Select(item => item.ManagerUserId)
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<Guid> ResolveOrganizationIdAsync(
        Guid departmentId,
        CancellationToken cancellationToken) =>
        await dbContext.Departments.AsNoTracking()
            .Where(item => item.Id == departmentId)
            .Select(item => item.OrganizationId)
            .SingleAsync(cancellationToken);

    private async Task<AgentProfileEntity> GetAgentProfileEntityAsync(
        string name,
        CancellationToken cancellationToken) =>
        await dbContext.AgentProfiles.AsNoTracking()
            .SingleAsync(item => item.Name == name && item.IsActive, cancellationToken);

    private async Task<bool> AddNotificationAsync(
        WorkflowTaskEntity task,
        Guid recipientId,
        string type,
        string title,
        string message,
        string idempotencyKey,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (await dbContext.WorkflowNotifications.AnyAsync(
                item => item.IdempotencyKey == idempotencyKey,
                cancellationToken))
        {
            return false;
        }
        var organizationId = await ResolveOrganizationIdAsync(task.DepartmentId, cancellationToken);
        dbContext.WorkflowNotifications.Add(new WorkflowNotificationEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            DepartmentId = task.DepartmentId,
            RecipientId = recipientId,
            WorkflowTaskId = task.Id,
            Type = type,
            Title = title,
            Message = message,
            IdempotencyKey = idempotencyKey,
            IsRead = false,
            CreatedAt = now
        });
        return true;
    }

    private async Task CompleteTerminalProxyAsync(
        WorkflowTaskEntity task,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var instance = await dbContext.WorkflowInstances.SingleAsync(
            item => item.Id == task.WorkflowInstanceId,
            cancellationToken);
        var version = await dbContext.WorkflowVersions.AsNoTracking().SingleAsync(
            item => item.Id == instance.WorkflowVersionId,
            cancellationToken);
        using var definition = JsonDocument.Parse(version.DefinitionJson);
        if (!definition.RootElement.TryGetProperty("nodes", out var nodes)
            || nodes.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        var node = nodes.EnumerateArray().FirstOrDefault(item =>
            item.TryGetProperty("code", out var code)
            && string.Equals(code.GetString(), task.NodeCode, StringComparison.Ordinal));
        if (node.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }
        var completeOnApprove = node.TryGetProperty("completeOnApprove", out var complete)
            && complete.ValueKind == JsonValueKind.True;
        var nextIsNull = !node.TryGetProperty("nextNode", out var next)
            || next.ValueKind == JsonValueKind.Null;
        if (!completeOnApprove && !nextIsNull)
        {
            return;
        }

        instance.Status = "Completed";
        instance.CurrentNodeCode = null;
        instance.EndedAt = now;
        instance.Version++;
        var businessObject = await dbContext.BusinessObjects.SingleAsync(
            item => item.Id == instance.BusinessObjectId,
            cancellationToken);
        businessObject.Status = "Active";
        businessObject.Version++;
        businessObject.UpdatedAt = now;
    }

    private async Task AuditSlaAsync(
        Guid actorId,
        string action,
        WorkflowTaskEntity task,
        string traceId,
        DateTimeOffset now,
        CancellationToken cancellationToken) =>
        await auditRepository.AddAsync(
            AuditEvent.Record(
                actorId,
                "Workflow",
                action,
                nameof(WorkflowTaskEntity),
                task.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                reason: $"nodePolicy={task.NodePolicyId:D}; dueAt={task.DueAt:o}"),
            cancellationToken);

    private static List<MinutesActionItemEntity> ExtractActionItems(
        MinutesSourceEntity source,
        Guid runId)
    {
        var now = source.UpdatedAt;
        var result = new List<MinutesActionItemEntity>();
        var lines = source.Transcript
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var line in lines)
        {
            if (!TryParseTranscriptLine(line, out var timestamp, out var speaker, out var text))
            {
                continue;
            }
            if (!ActionKeywords().IsMatch(text))
            {
                continue;
            }
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(speaker))
            {
                missing.Add("发言人");
            }
            if (!DueDateRegex().IsMatch(text))
            {
                missing.Add("截止时间");
            }
            if (!NameMentionRegex().IsMatch(text))
            {
                missing.Add("明确执行人");
            }
            DateTimeOffset? dueAt = null;
            var dueMatch = DueDateRegex().Match(text);
            if (dueMatch.Success
                && DateTime.TryParse(
                    dueMatch.Groups["date"].Value.Replace('/', '-'),
                    out var dueDate))
            {
                dueAt = new DateTimeOffset(dueDate.Date.AddHours(18), TimeSpan.FromHours(8));
            }
            result.Add(new MinutesActionItemEntity
            {
                Id = Guid.NewGuid(),
                MinutesSourceId = source.Id,
                Speaker = speaker.Trim(),
                SourceTimestamp = timestamp,
                SourceExcerpt = Truncate(text, 2000),
                OriginalContent = text,
                Content = text,
                SuggestedDueAt = dueAt,
                DueAt = dueAt,
                Priority = UrgentRegex().IsMatch(text) ? "P0" : "P1",
                SuggestedObjectTypeId = null,
                ObjectTypeId = null,
                SuggestedWorkflowVersionId = null,
                WorkflowVersionId = null,
                MissingInformation = string.Join("、", missing),
                Confidence = missing.Count == 0 ? 0.92m : 0.72m,
                AnalysisVersion = AnalysisVersion,
                Status = "Draft",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            _ = runId;
        }
        return result;
    }

    private static bool TryParseTranscriptLine(
        string line,
        out string timestamp,
        out string speaker,
        out string text)
    {
        var withTimestamp = TranscriptWithTimestampRegex().Match(line);
        if (withTimestamp.Success)
        {
            timestamp = withTimestamp.Groups["time"].Value;
            speaker = withTimestamp.Groups["speaker"].Value.Trim();
            text = withTimestamp.Groups["text"].Value.Trim();
            return true;
        }
        var plain = TranscriptPlainRegex().Match(line);
        if (plain.Success)
        {
            timestamp = "未标注";
            speaker = plain.Groups["speaker"].Value.Trim();
            text = plain.Groups["text"].Value.Trim();
            return true;
        }
        timestamp = string.Empty;
        speaker = string.Empty;
        text = string.Empty;
        return false;
    }

    private static string SuggestObjectType(string text)
    {
        if (text.Contains("样品", StringComparison.Ordinal)) return "CUSTOMER_SAMPLE";
        if (text.Contains("偏差", StringComparison.Ordinal)) return "QUALITY_DEVIATION";
        if (text.Contains("采购", StringComparison.Ordinal)) return "PURCHASE_REQUEST";
        if (text.Contains("投诉", StringComparison.Ordinal)) return "CUSTOMER_COMPLAINT";
        return "ANY";
    }

    private static MinutesSourceDto MapSource(MinutesSourceEntity entity) =>
        new(
            entity.Id.ToString("D"),
            entity.DepartmentId.ToString("D"),
            entity.AttachmentId?.ToString("D"),
            entity.Title,
            entity.MeetingAt,
            entity.SourceType,
            entity.Transcript,
            entity.EnvironmentCode,
            entity.IsTestData,
            entity.Status,
            entity.AnalysisVersion,
            entity.AnalysisRunId?.ToString("D"),
            entity.CreatedAt);

    private async Task<IReadOnlyList<MinutesActionItemDto>> EnrichActionItemsAsync(
        List<MinutesActionItemEntity> rows,
        CancellationToken cancellationToken)
    {
        if (rows.Count == 0)
        {
            return [];
        }

        var userIds = rows.SelectMany(item => new[] { item.AssigneeId, item.SuggestedAssigneeId })
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var users = await dbContext.Users.AsNoTracking()
            .Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        var objectIds = rows.SelectMany(item => new[] { item.ProjectId, item.BusinessObjectId })
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var objects = await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => objectIds.Contains(item.Id))
            .Select(item => new { item.Id, item.ObjectCode, item.Title })
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var workflowIds = rows.Where(item => item.WorkflowInstanceId.HasValue)
            .Select(item => item.WorkflowInstanceId!.Value)
            .Distinct()
            .ToArray();
        var workflows = await dbContext.WorkflowInstances.AsNoTracking()
            .Where(item => workflowIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Status, cancellationToken);

        return rows.Select(item =>
        {
            var project = item.ProjectId.HasValue ? objects.GetValueOrDefault(item.ProjectId.Value) : null;
            var businessObject = item.BusinessObjectId.HasValue
                ? objects.GetValueOrDefault(item.BusinessObjectId.Value)
                : null;
            return MapActionItem(item) with
            {
                AssigneeDisplayName = item.AssigneeId.HasValue
                    ? users.GetValueOrDefault(item.AssigneeId.Value)
                    : null,
                ProjectName = project is null ? null : project.Title ?? project.ObjectCode,
                BusinessObjectCode = businessObject?.ObjectCode,
                BusinessObjectTitle = businessObject?.Title,
                WorkflowInstanceStatus = item.WorkflowInstanceId.HasValue
                    ? workflows.GetValueOrDefault(item.WorkflowInstanceId.Value)
                    : null
            };
        }).ToList();
    }

    private async Task<MinutesTaskDto> EnrichMinutesTaskAsync(
        MinutesTaskEntity task,
        CancellationToken cancellationToken)
    {
        var projectName = task.ProjectId.HasValue
            ? await dbContext.BusinessObjects.AsNoTracking()
                .Where(item => item.Id == task.ProjectId.Value)
                .Select(item => item.Title ?? item.ObjectCode)
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        return MapTask(task) with { ProjectName = projectName };
    }

    private static MinutesActionItemDto MapActionItem(MinutesActionItemEntity item) =>
        new(
            item.Id.ToString("D"),
            item.MinutesSourceId.ToString("D"),
            item.Speaker,
            item.SourceTimestamp,
            item.SourceExcerpt,
            item.OriginalContent,
            item.Content,
            item.SuggestedAssigneeId?.ToString("D"),
            item.AssigneeId?.ToString("D"),
            item.SuggestedDueAt,
            item.DueAt,
            item.Priority,
            item.SuggestedObjectTypeId?.ToString("D"),
            item.ObjectTypeId?.ToString("D"),
            item.SuggestedWorkflowVersionId?.ToString("D"),
            item.WorkflowVersionId?.ToString("D"),
            item.MissingInformation,
            item.Confidence,
            item.AnalysisVersion,
            item.Status,
            item.ConfirmedBy?.ToString("D"),
            item.ConfirmedAt,
            item.MinutesTaskId?.ToString("D"),
            ProjectId: item.ProjectId?.ToString("D"),
            BusinessObjectId: item.BusinessObjectId?.ToString("D"),
            WorkflowInstanceId: item.WorkflowInstanceId?.ToString("D"));

    private static MinutesTaskDto MapTask(MinutesTaskEntity task) =>
        new(
            task.Id.ToString("D"),
            task.MinutesSourceId.ToString("D"),
            task.ActionItemId.ToString("D"),
            task.AttachmentId?.ToString("D"),
            task.DepartmentId.ToString("D"),
            task.AssigneeId.ToString("D"),
            task.Title,
            task.Content,
            task.DueAt,
            task.Priority,
            task.BusinessObjectId?.ToString("D"),
            task.WorkflowInstanceId?.ToString("D"),
            task.EnvironmentCode,
            task.IsTestData,
            task.Status,
            task.CreatedAt,
            ProjectId: task.ProjectId?.ToString("D"));

    private static WorkflowNodePolicyDto MapPolicy(WorkflowNodePolicyEntity item) =>
        new(
            item.Id.ToString("D"),
            item.WorkflowVersionId.ToString("D"),
            item.NodeCode,
            item.VersionNumber,
            item.ApproverUserId?.ToString("D"),
            item.BackupApproverUserId?.ToString("D"),
            item.SlaMinutes,
            item.ReminderMinutes,
            item.EscalationMinutes,
            item.TimeoutStrategy,
            item.AllowAgentAssist,
            item.AllowAgentProxy,
            item.AgentProfileId?.ToString("D"),
            item.AgentMaxRisk,
            item.AgentMaxAmount,
            item.AgentMaxAutonomy,
            item.AgentProxyMode,
            item.AutoProxyAfterMinutes,
            item.PermanentHumanBoundary,
            item.RequiredAttachmentCount,
            item.PrecheckItems,
            item.Status,
            item.PublishedAt,
            item.AssigneeStrategy,
            null,
            item.AllowTransfer,
            item.TransferTargetStrategy);

    private static AgentProfile MapProfile(AgentProfileEntity item) =>
        AgentProfile.Restore(
            item.Id,
            item.ServiceUserId,
            item.Name,
            Enum.Parse<AgentAutonomyLevel>(item.AutonomyLevel),
            item.AmountLimit,
            Enum.Parse<AgentRiskLevel>(item.RiskLimit),
            item.IsActive,
            JsonSerializer.Deserialize<string[]>(item.AllowedToolsJson) ?? [],
            item.Code,
            item.DepartmentId,
            JsonSerializer.Deserialize<string[]>(item.DataScopesJson) ?? [],
            JsonSerializer.Deserialize<string[]>(item.ModelScopesJson) ?? [],
            JsonSerializer.Deserialize<string[]>(item.RequiredConfirmationsJson) ?? [],
            JsonSerializer.Deserialize<string[]>(item.StopConditionsJson) ?? [],
            Enum.Parse<AgentProfileLifecycleStatus>(item.LifecycleStatus),
            item.CurrentPublishedVersionId);

    private static AgentRunEntity ToEntity(AgentRun run) =>
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

    private static AgentRun MapAgentRun(AgentRunEntity entity) =>
        AgentRun.Restore(
            entity.Id,
            entity.AgentProfileId,
            entity.RequestedBy,
            entity.Action,
            entity.Amount,
            Enum.Parse<AgentRiskLevel>(entity.RiskLevel),
            Enum.Parse<AgentRunStatus>(entity.Status),
            entity.ConfirmationId,
            entity.CreatedAt,
            entity.OrganizationId,
            entity.DepartmentId,
            entity.EnvironmentCode,
            entity.IsTestData,
            entity.SubjectType,
            entity.BusinessObjectId,
            entity.WorkflowInstanceId,
            entity.WorkflowTaskId,
            entity.MinutesSourceId,
            entity.Model,
            entity.GatewayCallId,
            entity.GatewayMode,
            entity.ExternalModelCalled,
            entity.StartedAt,
            entity.EndedAt,
            entity.ProxyTriggerMode,
            entity.ProxyTriggeredAt,
            entity.OriginalAssigneeId,
            entity.CompensationStatus,
            entity.ProxyIdempotencyKey);

    private static WorkflowTaskDto MapWorkflowTask(WorkflowTaskEntity item) =>
        new(
            item.Id.ToString("D"),
            item.WorkflowInstanceId.ToString("D"),
            item.NodeCode,
            item.AssigneeId.ToString("D"),
            Enum.Parse<AIDT.Domain.Workflows.WorkflowTaskStatus>(item.Status),
            item.Version,
            item.DecisionComment,
            item.CreatedAt,
            item.CompletedAt,
            item.DepartmentId.ToString("D"),
            item.EnvironmentCode,
            item.IsTestData,
            item.DueAt,
            item.SlaStatus,
            item.ReminderSentAt,
            item.EscalatedAt,
            item.DecisionMode,
            item.AgentRunId?.ToString("D"),
            item.AssigneeStrategy,
            item.BusinessRoleCode,
            item.BusinessRoleVersion,
            JsonSerializer.Deserialize<string[]>(item.CandidateUserIdsJson) ?? [],
            item.OriginalAssigneeId?.ToString("D"),
            item.BackupAssigneeId?.ToString("D"),
            item.TransferredBy?.ToString("D"),
            item.TransferredAt,
            item.TransferReason,
            item.NodeName,
            item.FormDefinitionJson,
            item.FormDataJson,
            item.FormSchemaVersion,
            item.FormDefinitionHash);

    private static AgentGatewayCallDto MapGatewayCall(AgentGatewayCallEntity item) =>
        new(
            item.Id.ToString("D"),
            item.AgentRunId.ToString("D"),
            item.Model,
            item.Mode,
            item.GatewayCallId,
            item.ExternalModelCalled,
            item.InputDigest,
            item.OutputDigest,
            item.Status,
            item.ErrorCode,
            item.ErrorMessage,
            item.DurationMs,
            item.CreatedAt);

    private static string NormalizePriority(string value)
    {
        var normalized = value.Trim().ToUpperInvariant();
        return normalized is "P0" or "P1" or "P2"
            ? normalized
            : throw new ValidationException("优先级必须是 P0、P1 或 P2。");
    }

    private static void ValidateIdempotencyKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length is < 8 or > 128)
        {
            throw new ValidationException("幂等键必须为 8—128 位。");
        }
    }

    private static string Hash<T>(T value) =>
        Sha(JsonSerializer.Serialize(value, JsonSerializerOptions.Web));

    private static string Sha(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private static string Escape(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "\\\"", StringComparison.Ordinal);

    private static string Truncate(string value, int length) =>
        value.Length <= length ? value : value[..length];

    private static string ExtractGatewayCode(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("code", out var code)
                ? code.GetString() ?? "GATEWAY_ERROR"
                : "GATEWAY_ERROR";
        }
        catch (JsonException)
        {
            return "GATEWAY_ERROR";
        }
    }

    private static bool IsPermanentHumanNode(string nodeCode)
    {
        string[] markers =
        [
            "PAYMENT", "FINAL_RELEASE", "MAJOR_REVIEW", "CAPA_DECISION",
            "CUSTOMER_RESPONSE", "QUALITY_RELEASE", "FORMULA", "SAFETY"
        ];
        return markers.Any(marker => nodeCode.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    [GeneratedRegex(@"^\[(?<time>[0-9]{1,2}:[0-9]{2}(?::[0-9]{2})?)\]\s*(?<speaker>[^:：]+)[:：]\s*(?<text>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex TranscriptWithTimestampRegex();

    [GeneratedRegex(@"^(?<speaker>[^:：]{1,50})[:：]\s*(?<text>.+)$", RegexOptions.CultureInvariant)]
    private static partial Regex TranscriptPlainRegex();

    [GeneratedRegex(@"负责|需要|请|完成|提交|确认|整理|跟进|安排|输出", RegexOptions.CultureInvariant)]
    private static partial Regex ActionKeywords();

    [GeneratedRegex(@"紧急|立即|马上", RegexOptions.CultureInvariant)]
    private static partial Regex UrgentRegex();

    [GeneratedRegex(@"(?<date>\d{4}[/-]\d{1,2}[/-]\d{1,2}|\d{1,2}[/-]\d{1,2})", RegexOptions.CultureInvariant)]
    private static partial Regex DueDateRegex();

    [GeneratedRegex(@"@[^\s，。]+|交给[^\s，。]+|由[^\s，。]+负责", RegexOptions.CultureInvariant)]
    private static partial Regex NameMentionRegex();
}
