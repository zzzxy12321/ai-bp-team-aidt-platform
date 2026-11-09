using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

internal sealed class NotReadyBatch2Service : IMinutesService, IWorkflowPolicyService, ISlaProcessor, IAgentGatewayService
{
    private static Task<T> Fail<T>() => throw NotReady.Error();

    public Task<PagedResult<MinutesSourceDto>> ListSourcesAsync(UserPrincipal user, MinutesSourceQuery query, CancellationToken cancellationToken) => Fail<PagedResult<MinutesSourceDto>>();
    public Task<MinutesSummaryDto> GetSummaryAsync(UserPrincipal user, bool includeTestData, CancellationToken cancellationToken) => Fail<MinutesSummaryDto>();
    public Task<MinutesSourceDto?> GetSourceAsync(UserPrincipal user, Guid id, bool includeTestData, CancellationToken cancellationToken) => Fail<MinutesSourceDto?>();
    public Task<MinutesSourceDto> RegisterAsync(UserPrincipal user, RegisterMinutesRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<MinutesSourceDto>();
    public Task<AgentRunDto> RequestAnalysisAsync(UserPrincipal user, Guid sourceId, AnalyzeMinutesRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<AgentRunDto>();
    public Task<IReadOnlyList<MinutesActionItemDto>> ListActionItemsAsync(UserPrincipal user, Guid sourceId, CancellationToken cancellationToken) => Fail<IReadOnlyList<MinutesActionItemDto>>();
    public Task<IReadOnlyList<MinutesConfirmationUserDto>> ListConfirmationUsersAsync(UserPrincipal user, Guid departmentId, CancellationToken cancellationToken) => Fail<IReadOnlyList<MinutesConfirmationUserDto>>();
    public Task<MinutesActionItemDto> UpdateActionItemAsync(UserPrincipal user, Guid sourceId, Guid actionItemId, UpdateMinutesActionItemRequest request, string traceId, CancellationToken cancellationToken) => Fail<MinutesActionItemDto>();
    public Task<MinutesTaskDto> ConfirmActionItemAsync(UserPrincipal user, Guid actionItemId, ConfirmMinutesActionItemRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<MinutesTaskDto>();
    public Task<MinutesTaskTraceDto?> TraceTaskAsync(UserPrincipal user, Guid taskId, CancellationToken cancellationToken) => Fail<MinutesTaskTraceDto?>();
    public Task<IReadOnlyList<WorkflowPolicyUserDto>> ListUsersAsync(UserPrincipal user, CancellationToken cancellationToken) => Fail<IReadOnlyList<WorkflowPolicyUserDto>>();
    public Task<IReadOnlyList<WorkflowVersionNodeDto>> ListNodeCandidatesAsync(UserPrincipal user, Guid workflowVersionId, CancellationToken cancellationToken) => Fail<IReadOnlyList<WorkflowVersionNodeDto>>();
    public Task<IReadOnlyList<WorkflowNodePolicyDto>> ListAsync(UserPrincipal user, Guid workflowVersionId, CancellationToken cancellationToken) => Fail<IReadOnlyList<WorkflowNodePolicyDto>>();
    public Task<WorkflowNodePolicyDto> SaveAsync(UserPrincipal user, Guid workflowVersionId, string nodeCode, SaveWorkflowNodePolicyRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<WorkflowNodePolicyDto>();
    public Task<WorkflowTaskDto> SimulateTimeoutAsync(UserPrincipal user, Guid taskId, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<WorkflowTaskDto>();
    public Task<IReadOnlyList<WorkflowNotificationDto>> ListNotificationsAsync(UserPrincipal user, int pageSize, CancellationToken cancellationToken) => Fail<IReadOnlyList<WorkflowNotificationDto>>();
    public Task<SlaProcessingResultDto> ProcessAsync(string traceId, CancellationToken cancellationToken) => Fail<SlaProcessingResultDto>();
    public Task<AgentExecutionResultDto> ExecuteConfirmedRunAsync(UserPrincipal user, Guid runId, string traceId, CancellationToken cancellationToken) => Fail<AgentExecutionResultDto>();
    public Task<IReadOnlyList<AgentGatewayCallDto>> ListCallsAsync(UserPrincipal user, Guid runId, CancellationToken cancellationToken) => Fail<IReadOnlyList<AgentGatewayCallDto>>();
    public Task<AgentRunDto> RequestProxyAsync(UserPrincipal user, Guid taskId, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<AgentRunDto>();
    public Task<AgentRunDto> ConfirmProxyAsync(UserPrincipal user, Guid runId, ConfirmAgentProxyRequest request, string traceId, CancellationToken cancellationToken) => Fail<AgentRunDto>();
}
