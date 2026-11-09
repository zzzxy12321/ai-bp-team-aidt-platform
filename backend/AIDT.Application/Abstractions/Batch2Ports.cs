using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Application.Abstractions;

public interface IMinutesService
{
    Task<PagedResult<MinutesSourceDto>> ListSourcesAsync(
        UserPrincipal user,
        MinutesSourceQuery query,
        CancellationToken cancellationToken);

    Task<MinutesSummaryDto> GetSummaryAsync(
        UserPrincipal user,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<MinutesSourceDto?> GetSourceAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<MinutesSourceDto> RegisterAsync(
        UserPrincipal user,
        RegisterMinutesRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<AgentRunDto> RequestAnalysisAsync(
        UserPrincipal user,
        Guid sourceId,
        AnalyzeMinutesRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MinutesActionItemDto>> ListActionItemsAsync(
        UserPrincipal user,
        Guid sourceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MinutesConfirmationUserDto>> ListConfirmationUsersAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken);

    Task<MinutesActionItemDto> UpdateActionItemAsync(
        UserPrincipal user,
        Guid sourceId,
        Guid actionItemId,
        UpdateMinutesActionItemRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<MinutesTaskDto> ConfirmActionItemAsync(
        UserPrincipal user,
        Guid actionItemId,
        ConfirmMinutesActionItemRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<MinutesTaskTraceDto?> TraceTaskAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken);
}

public interface IWorkflowPolicyService
{
    Task<IReadOnlyList<WorkflowPolicyUserDto>> ListUsersAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowVersionNodeDto>> ListNodeCandidatesAsync(
        UserPrincipal user,
        Guid workflowVersionId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowNodePolicyDto>> ListAsync(
        UserPrincipal user,
        Guid workflowVersionId,
        CancellationToken cancellationToken);

    Task<WorkflowNodePolicyDto> SaveAsync(
        UserPrincipal user,
        Guid workflowVersionId,
        string nodeCode,
        SaveWorkflowNodePolicyRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkflowTaskDto> SimulateTimeoutAsync(
        UserPrincipal user,
        Guid taskId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowNotificationDto>> ListNotificationsAsync(
        UserPrincipal user,
        int pageSize,
        CancellationToken cancellationToken);
}

public interface IWorkflowTaskFormService
{
    Task<WorkflowTaskFormDto> GetAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken);

    Task<WorkflowTaskDto> SaveDraftAsync(
        UserPrincipal user,
        Guid taskId,
        SaveWorkflowTaskFormDraftRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

public interface IWorkflowTaskFormValidator
{
    Task<WorkflowTaskFormValidationDto> ValidateAsync(
        UserPrincipal user,
        AIDT.Domain.Workflows.WorkflowTask task,
        string? formDataJson,
        bool enforceRequired,
        CancellationToken cancellationToken);
}

public interface IWorkflowTaskAiReviewService
{
    Task<WorkflowTaskAiReviewResultDto> ReviewAsync(
        UserPrincipal user,
        Guid taskId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);
}

public interface ISlaProcessor
{
    Task<SlaProcessingResultDto> ProcessAsync(string traceId, CancellationToken cancellationToken);
}

public interface IAgentGatewayService
{
    Task<AgentExecutionResultDto> ExecuteConfirmedRunAsync(
        UserPrincipal user,
        Guid runId,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentGatewayCallDto>> ListCallsAsync(
        UserPrincipal user,
        Guid runId,
        CancellationToken cancellationToken);

    Task<AgentRunDto> RequestProxyAsync(
        UserPrincipal user,
        Guid taskId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<AgentRunDto> ConfirmProxyAsync(
        UserPrincipal user,
        Guid runId,
        ConfirmAgentProxyRequest request,
        string traceId,
        CancellationToken cancellationToken);
}
