using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class ProjectsController(
    ICurrentUserAccessor currentUserAccessor,
    IProjectService projectService) : ControllerBase
{
    [HttpGet("projects")]
    public async Task<ActionResult<ApiResponse<PagedResult<ProjectDto>>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await projectService.ListAsync(
            currentUserAccessor.GetRequiredUser(),
            page,
            pageSize,
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("projects/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectDetailDto>>> Get(
        Guid id,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await projectService.GetAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return result is null
            ? NotFound(ApiResponse.Error(
                "RESOURCE_NOT_FOUND",
                "项目不存在。",
                HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("projects")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Create(
        [FromBody] CreateProjectRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await projectService.CreateAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("projects/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> Update(
        Guid id,
        [FromBody] UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.UpdateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("projects/{id:guid}/actions/change-status")]
    public async Task<ActionResult<ApiResponse<ProjectDto>>> ChangeStatus(
        Guid id,
        [FromBody] ChangeProjectStatusRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.ChangeStatusAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("projects/{id:guid}/member-candidates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ProjectCandidateUserDto>>>> MemberCandidates(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await projectService.ListCandidatesAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("projects/{id:guid}/members")]
    public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> AddMember(
        Guid id,
        [FromBody] AddProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.AddMemberAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpDelete("projects/{id:guid}/members/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectMemberDto>>> RemoveMember(
        Guid id,
        Guid userId,
        [FromBody] RemoveProjectMemberRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.RemoveMemberAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            userId,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("projects/{id:guid}/milestones")]
    public async Task<ActionResult<ApiResponse<ProjectMilestoneDto>>> AddMilestone(
        Guid id,
        [FromBody] CreateProjectMilestoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.AddMilestoneAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("projects/{id:guid}/milestones/{milestoneId:guid}")]
    public async Task<ActionResult<ApiResponse<ProjectMilestoneDto>>> UpdateMilestone(
        Guid id,
        Guid milestoneId,
        [FromBody] UpdateProjectMilestoneRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.UpdateMilestoneAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            milestoneId,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("projects/{id:guid}/relations")]
    public async Task<ActionResult<ApiResponse<ProjectRelationDto>>> AddRelation(
        Guid id,
        [FromBody] CreateProjectRelationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.AddRelationAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("projects/{id:guid}/tasks")]
    public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> CreateTask(
        Guid id,
        [FromBody] CreateProjectTaskRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await projectService.CreateTaskAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("project-tasks/{id:guid}/actions/complete")]
    public async Task<ActionResult<ApiResponse<ProjectTaskDto>>> CompleteTask(
        Guid id,
        [FromBody] CompleteProjectTaskRequest request,
        CancellationToken cancellationToken)
    {
        var result = await projectService.CompleteTaskAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
