using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Infrastructure.Persistence;
using AIDT.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlWorkflowTaskAiReviewService(
    AidtDbContext dbContext,
    StartAgentRunHandler startAgentRunHandler,
    IAgentGatewayService agentGatewayService,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock) : IWorkflowTaskAiReviewService
{
    public async Task<WorkflowTaskAiReviewResultDto> ReviewAsync(
        UserPrincipal user,
        Guid taskId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-task", AccessAction.Read);
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "服务身份不能发起真人节点审查。");
        }
        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || idempotencyKey.Length is < 8 or > 128)
        {
            throw new ValidationException("AI 审查幂等键必须为 8—128 位。");
        }

        var task = await dbContext.WorkflowTasks.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken) ?? throw new NotFoundException("流程任务不存在。");
        user.EnsureDepartmentAccess(task.DepartmentId);
        if (!user.IsPlatformAdmin && task.AssigneeId != user.UserId)
        {
            throw new PermissionException(
                ErrorCodes.TaskAssigneeMismatch,
                "只有当前任务处理人可以请求节点 AI 审查。");
        }
        if (!task.NodePolicyId.HasValue)
        {
            throw new ConflictException(
                ErrorCodes.WorkflowAgentUnavailable,
                "当前节点没有已冻结的 AI 审查配置。");
        }

        var policy = await dbContext.WorkflowNodePolicies.AsNoTracking().SingleAsync(
            item => item.Id == task.NodePolicyId.Value,
            cancellationToken);
        var review = JsonStorage.Deserialize<WorkflowAiReviewRequest>(policy.AiReviewJson)
            ?? new WorkflowAiReviewRequest();
        if (!review.Enabled || string.IsNullOrWhiteSpace(review.RegisteredAction))
        {
            throw new ConflictException(
                ErrorCodes.WorkflowAgentUnavailable,
                "当前节点未启用受控 AI 评论动作。");
        }
        if (!review.AgentProfileId.HasValue)
        {
            throw new ConflictException(
                ErrorCodes.WorkflowAgentUnavailable,
                "当前节点未绑定可用数字员工。");
        }

        var fields = JsonStorage.Deserialize<WorkflowFormFieldRequest[]>(policy.FormSchemaJson) ?? [];
        var checkedKeys = (review.ReadableFieldKeys ?? fields.Select(field => field.Key))
            .Where(key => fields.Any(field => field.Key == key))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var formData = TryReadFormData(task.FormDataJson);
        var missingRequired = fields
            .Where(field =>
                checkedKeys.Contains(field.Key)
                && field.Required
                && field.DataSource == "TaskInput"
                && !formData.ContainsKey(field.Key))
            .Select(field => field.DisplayName)
            .ToList();
        var suggestion = review.RegisteredAction switch
        {
            "CompletenessCheck" when missingRequired.Count > 0 =>
                "规则模拟：仍有必填字段未完成：" + string.Join("、", missingRequired) + "。",
            "CompletenessCheck" =>
                "规则模拟：已检查字段均已填写；仍需真人核对业务事实和材料真实性。",
            "RiskIdentify" =>
                "规则模拟：未发现可自动判定的高风险结论；请注意附件、人员范围和期限仍需真人复核。",
            "GenerateReviewOpinion" =>
                "规则模拟：建议真人围绕资料完整性、责任边界、期限和证据一致性形成审查意见。",
            "GenerateDraft" =>
                "规则模拟：可形成处理草稿，但草稿不改变正式状态，必须由真人确认。",
            _ => "规则模拟：仅提供参考信息，不代替真人判断。"
        };

        var scope = $"workflow-task:ai-review:{taskId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return await LoadExistingResultAsync(
                user,
                Guid.Parse(existing.ResponseId),
                review.RegisteredAction,
                suggestion,
                checkedKeys,
                cancellationToken);
        }

        var instance = await dbContext.WorkflowInstances.AsNoTracking().SingleAsync(
            item => item.Id == task.WorkflowInstanceId,
            cancellationToken);
        var now = clock.UtcNow;
        AgentRunDto run;
        await using (var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken))
        {
            run = await startAgentRunHandler.StageWithinTransactionAsync(
                user,
                new CreateAgentRunRequest(
                    review.AgentProfileId.Value,
                    AgentActionType.Draft,
                    0,
                    AgentRiskLevel.Low,
                    "aidt-rule-sim",
                    "workflow.assist"),
                traceId,
                "WorkflowTask",
                task.DepartmentId,
                instance.BusinessObjectId,
                task.WorkflowInstanceId,
                task.Id,
                null,
                cancellationToken);
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Workflow",
                    "workflow-task.ai_review_requested",
                    nameof(WorkflowTaskEntity),
                    task.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: JsonSerializer.Serialize(
                        new
                        {
                            review.RegisteredAction,
                            review.AgentProfileId,
                            CheckedFields = checkedKeys,
                            MaterialScopes = review.MaterialScopes
                        },
                        JsonSerializerOptions.Web)),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    idempotencyKey,
                    JsonStorage.Serialize(new { taskId, review.RegisteredAction }),
                    nameof(AgentRun),
                    run.Id,
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        if (run.Status == AgentRunStatus.AwaitingConfirmation)
        {
            return new WorkflowTaskAiReviewResultDto(
                run.Id,
                null,
                "RuleSimulator",
                false,
                true,
                review.RegisteredAction,
                "数字员工运行等待真人确认后再执行规则模拟。",
                checkedKeys,
                now);
        }

        var execution = await agentGatewayService.ExecuteConfirmedRunAsync(
            user,
            Guid.Parse(run.Id),
            traceId,
            cancellationToken);
        var runEntity = await dbContext.AgentRuns.SingleAsync(
            item => item.Id == Guid.Parse(run.Id),
            cancellationToken);
        runEntity.Status = execution.Call.Status == "Succeeded" ? "Completed" : "Failed";
        runEntity.EndedAt = clock.UtcNow;
        runEntity.OutputDigest = execution.Call.OutputDigest;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new WorkflowTaskAiReviewResultDto(
            run.Id,
            execution.Call.Id,
            execution.Call.Mode,
            execution.Call.ExternalModelCalled,
            false,
            review.RegisteredAction,
            suggestion,
            checkedKeys,
            now);
    }

    private async Task<WorkflowTaskAiReviewResultDto> LoadExistingResultAsync(
        UserPrincipal user,
        Guid runId,
        string registeredAction,
        string suggestion,
        IReadOnlyList<string> checkedKeys,
        CancellationToken cancellationToken)
    {
        var run = await dbContext.AgentRuns.AsNoTracking().SingleAsync(
            item => item.Id == runId,
            cancellationToken);
        user.EnsureDepartmentAccess(run.DepartmentId);
        var call = run.GatewayCallId.HasValue
            ? await dbContext.AgentGatewayCalls.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == run.GatewayCallId.Value, cancellationToken)
            : null;
        return new WorkflowTaskAiReviewResultDto(
            run.Id.ToString("D"),
            call?.Id.ToString("D"),
            call?.Mode ?? "RuleSimulator",
            call?.ExternalModelCalled ?? false,
            run.Status == "AwaitingConfirmation",
            registeredAction,
            run.Status == "AwaitingConfirmation"
                ? "数字员工运行等待真人确认后再执行规则模拟。"
                : suggestion,
            checkedKeys,
            run.CreatedAt);
    }

    private static Dictionary<string, JsonElement> TryReadFormData(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                json,
                JsonSerializerOptions.Web) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
