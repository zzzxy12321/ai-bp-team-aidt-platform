using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class WorkflowController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkspaceQueryService queryService,
    IWorkflowPolicyService workflowPolicyService,
    IWorkflowTaskFormService workflowTaskFormService,
    IWorkflowTaskAiReviewService workflowTaskAiReviewService,
    ISlaProcessor slaProcessor,
    IAgentGatewayService agentGatewayService,
    StartWorkflowInstanceHandler startWorkflowInstanceHandler,
    ProcessWorkflowTaskHandler processWorkflowTaskHandler) : ControllerBase
{
    [HttpGet("workflow-definitions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowDefinitionDto>>>> GetDefinitions(
        CancellationToken cancellationToken)
    {
        var result = await queryService.ListWorkflowDefinitionsAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-policy/users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowPolicyUserDto>>>> GetPolicyUsers(
        CancellationToken cancellationToken)
    {
        var result = await workflowPolicyService.ListUsersAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-versions/{id:guid}/nodes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowVersionNodeDto>>>> GetWorkflowVersionNodes(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workflowPolicyService.ListNodeCandidatesAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-instances")]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkflowInstanceDto>>>> GetInstances(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.ListWorkflowInstancesAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            status,
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-instances/{id:guid}")]
    public async Task<ActionResult<ApiResponse<WorkflowInstanceDto>>> GetInstance(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var result = await queryService.GetWorkflowInstanceAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error("WORKFLOW_NOT_FOUND", "流程实例不存在。", HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-instances/{id:guid}/timeline")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowTimelineItemDto>>>> GetTimeline(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var result = await queryService.GetWorkflowTimelineAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-instances")]
    public async Task<ActionResult<ApiResponse<WorkflowInstanceDto>>> StartInstance(
        [FromBody] StartWorkflowInstanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await startWorkflowInstanceHandler.HandleAsync(
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-tasks")]
    public async Task<ActionResult<ApiResponse<PagedResult<WorkflowTaskDto>>>> GetTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? status = null,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.ListTasksAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            status,
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-tasks/{id:guid}/actions/decide")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskDto>>> Decide(
        Guid id,
        [FromBody] TaskDecisionRequest request,
        CancellationToken cancellationToken)
    {
        var task = await processWorkflowTaskHandler.HandleAsync(
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(task, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-tasks/{id:guid}/form")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskFormDto>>> GetTaskForm(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workflowTaskFormService.GetAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("workflow-tasks/{id:guid}/form-draft")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskDto>>> SaveTaskFormDraft(
        Guid id,
        [FromBody] SaveWorkflowTaskFormDraftRequest request,
        CancellationToken cancellationToken)
    {
        var result = await workflowTaskFormService.SaveDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-tasks/{id:guid}/actions/ai-review")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskAiReviewResultDto>>> RunTaskAiReview(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await workflowTaskAiReviewService.ReviewAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-versions/{id:guid}/node-policies")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowNodePolicyDto>>>> GetNodePolicies(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await workflowPolicyService.ListAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-versions/{id:guid}/nodes/{nodeCode}/policy")]
    public async Task<ActionResult<ApiResponse<WorkflowNodePolicyDto>>> SaveNodePolicy(
        Guid id,
        string nodeCode,
        [FromBody] SaveWorkflowNodePolicyRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await workflowPolicyService.SaveAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            nodeCode,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-sla/actions/run")]
    public async Task<ActionResult<ApiResponse<SlaProcessingResultDto>>> RunSlaScan(
        CancellationToken cancellationToken)
    {
        var result = await slaProcessor.ProcessAsync(
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-sla/tasks/{id:guid}/actions/simulate-timeout")]
    public async Task<ActionResult<ApiResponse<WorkflowTaskDto>>> SimulateTimeout(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await workflowPolicyService.SimulateTimeoutAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("workflow-notifications")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WorkflowNotificationDto>>>> Notifications(
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await workflowPolicyService.ListNotificationsAsync(
            currentUserAccessor.GetRequiredUser(),
            pageSize,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("workflow-tasks/{id:guid}/actions/agent-proxy")]
    public async Task<ActionResult<ApiResponse<AgentRunDto>>> RequestAgentProxy(
        Guid id,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await agentGatewayService.RequestProxyAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status202Accepted,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
