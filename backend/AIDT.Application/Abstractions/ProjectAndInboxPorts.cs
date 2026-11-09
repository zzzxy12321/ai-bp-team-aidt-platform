using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Application.Abstractions;

public interface IProjectService
{
    Task<PagedResult<ProjectDto>> ListAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<ProjectDetailDto?> GetAsync(
        UserPrincipal user,
        Guid projectId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectDto> CreateAsync(
        UserPrincipal user,
        CreateProjectRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectDto> UpdateAsync(
        UserPrincipal user,
        Guid projectId,
        UpdateProjectRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectDto> ChangeStatusAsync(
        UserPrincipal user,
        Guid projectId,
        ChangeProjectStatusRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectCandidateUserDto>> ListCandidatesAsync(
        UserPrincipal user,
        Guid projectId,
        CancellationToken cancellationToken);

    Task<ProjectMemberDto> AddMemberAsync(
        UserPrincipal user,
        Guid projectId,
        AddProjectMemberRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectMemberDto> RemoveMemberAsync(
        UserPrincipal user,
        Guid projectId,
        Guid userId,
        RemoveProjectMemberRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectMilestoneDto> AddMilestoneAsync(
        UserPrincipal user,
        Guid projectId,
        CreateProjectMilestoneRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectMilestoneDto> UpdateMilestoneAsync(
        UserPrincipal user,
        Guid projectId,
        Guid milestoneId,
        UpdateProjectMilestoneRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectRelationDto> AddRelationAsync(
        UserPrincipal user,
        Guid projectId,
        CreateProjectRelationRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectTaskDto> CreateTaskAsync(
        UserPrincipal user,
        Guid projectId,
        CreateProjectTaskRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ProjectTaskDto> CompleteTaskAsync(
        UserPrincipal user,
        Guid taskId,
        CompleteProjectTaskRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

public interface IEventInboxService
{
    Task<PagedResult<InboxEventDto>> ListAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<InboxEventDto?> GetAsync(
        UserPrincipal user,
        Guid eventId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<InboxSourceOptionDto>> ListSourcesAsync(
        UserPrincipal user,
        Guid departmentId,
        CancellationToken cancellationToken);

    Task<InboxEventDto> ReceiveAsync(
        UserPrincipal user,
        ReceiveInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<InboxEventDto> AnalyzeAsync(
        UserPrincipal user,
        Guid eventId,
        AnalyzeInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<InboxConfirmationDto> ConfirmAsync(
        UserPrincipal user,
        Guid eventId,
        ConfirmInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<InboxTaskTraceDto?> TraceTaskAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken);
}

public interface IAtomicEventInboxConfirmationService
{
    Task<InboxConfirmationDto?> GetIdempotentConfirmationAsync(
        UserPrincipal user,
        Guid eventId,
        ConfirmInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<InboxConfirmationDto> ConfirmWithinTransactionAsync(
        UserPrincipal user,
        Guid eventId,
        ConfirmInboxEventRequest request,
        string idempotencyKey,
        string traceId,
        Guid? startedWorkflowTaskId,
        CancellationToken cancellationToken);
}
