using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Application.UseCases;

public sealed class StartAgentRunHandler(
    ICurrentUserAccessor currentUserAccessor,
    IAgentRepository agentRepository,
    IDepartmentRepository departmentRepository,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock,
    DataEnvironmentOptions environmentOptions)
{
    public async Task<AgentRunDto> HandleAsync(
        CreateAgentRunRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length is < 8 or > 128)
        {
            throw new ValidationException("Agent 运行必须提供 8—128 位幂等键。");
        }

        var user = currentUserAccessor.GetRequiredUser();
        user.EnsureCan("agent-run", AccessAction.Submit);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(request, JsonSerializerOptions.Web))));
        var scope = $"agent-run:create:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(ErrorCodes.Conflict, "同一幂等键不能用于不同请求。");
            }

            return (await agentRepository.GetRunAsync(Guid.Parse(existing.ResponseId), cancellationToken)
                ?? throw new NotFoundException("幂等结果对应的 Agent 运行不存在。")).ToDto();
        }

        var profile = await agentRepository.GetProfileAsync(request.AgentProfileId, cancellationToken)
            ?? throw new NotFoundException("Agent 配置不存在。");
        var departmentId = await departmentRepository.ResolveIdAsync(
            user,
            user.IsPlatformAdmin ? profile.DepartmentId : null,
            "AGENT_RUN",
            cancellationToken);
        var toolCode = string.IsNullOrWhiteSpace(request.ToolCode)
            ? request.Action switch
            {
                AgentActionType.Read => "workflow.read",
                AgentActionType.Draft or AgentActionType.Submit => "workflow.assist",
                _ => "workflow.assist"
            }
            : DomainChecks.Required(request.ToolCode, "Agent 工具能力", 128);
        if (!profile.AllowsDataScope(departmentId))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "当前部门不在数字员工数据范围内。");
        }

        var environmentCode = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var now = clock.UtcNow;
        var run = AgentRun.Request(
            profile,
            user.UserId,
            request.Action,
            request.Amount,
            request.RiskLevel,
            null,
            now,
            user.OrganizationId,
            departmentId,
            environmentCode,
            DataEnvironment.IsTestEnvironment(environmentCode),
            "Manual",
            model: request.Model,
            toolCode: toolCode);
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await agentRepository.AddRunAsync(run, cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Agent",
                    "RequestRun",
                    nameof(AgentRun),
                    run.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: run.Status.ToString()),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    hash,
                    nameof(AgentRun),
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

    public async Task<AgentRunDto> StageWithinTransactionAsync(
        UserPrincipal user,
        CreateAgentRunRequest request,
        string traceId,
        string subjectType,
        Guid departmentId,
        Guid? businessObjectId,
        Guid? workflowInstanceId,
        Guid? workflowTaskId,
        Guid? minutesSourceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(request);
        user.EnsureCan("agent-run", AccessAction.Submit);
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "场景启动建议必须由真人发起。");
        }

        var profile = await agentRepository.GetProfileAsync(
                request.AgentProfileId,
                cancellationToken)
            ?? throw new NotFoundException("数字员工不可用。");
        if (!profile.AllowsDataScope(departmentId))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "数字员工不可用：当前部门不在其数据范围内。");
        }

        var toolCode = string.IsNullOrWhiteSpace(request.ToolCode)
            ? "workflow.assist"
            : DomainChecks.Required(request.ToolCode, "Agent 工具能力", 128);
        var environmentCode = DataEnvironment.Normalize(
            environmentOptions.CurrentEnvironmentCode);
        var now = clock.UtcNow;
        var run = AgentRun.Request(
            profile,
            user.UserId,
            request.Action,
            request.Amount,
            request.RiskLevel,
            null,
            now,
            user.OrganizationId,
            departmentId,
            environmentCode,
            DataEnvironment.IsTestEnvironment(environmentCode),
            DomainChecks.RequiredIdentifier(subjectType, "Agent 业务来源", 64),
            businessObjectId,
            workflowInstanceId,
            workflowTaskId,
            minutesSourceId,
            request.Model,
            toolCode: toolCode);
        await agentRepository.AddRunAsync(run, cancellationToken);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Agent",
                "scenario.agent_suggestion_requested",
                nameof(AgentRun),
                run.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                after: run.Status.ToString()),
            cancellationToken);
        return run.ToDto();
    }
}
