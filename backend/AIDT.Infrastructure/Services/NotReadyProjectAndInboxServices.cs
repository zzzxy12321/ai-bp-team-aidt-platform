using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

internal sealed class NotReadyProjectService : IProjectService
{
    private static Task<T> Fail<T>() => throw NotReady.Error();

    public Task<PagedResult<ProjectDto>> ListAsync(UserPrincipal user, int page, int pageSize, bool includeTestData, CancellationToken cancellationToken) => Fail<PagedResult<ProjectDto>>();
    public Task<ProjectDetailDto?> GetAsync(UserPrincipal user, Guid projectId, bool includeTestData, string traceId, CancellationToken cancellationToken) => Fail<ProjectDetailDto?>();
    public Task<ProjectDto> CreateAsync(UserPrincipal user, CreateProjectRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<ProjectDto>();
    public Task<ProjectDto> UpdateAsync(UserPrincipal user, Guid projectId, UpdateProjectRequest request, string traceId, CancellationToken cancellationToken) => Fail<ProjectDto>();
    public Task<ProjectDto> ChangeStatusAsync(UserPrincipal user, Guid projectId, ChangeProjectStatusRequest request, string traceId, CancellationToken cancellationToken) => Fail<ProjectDto>();
    public Task<IReadOnlyList<ProjectCandidateUserDto>> ListCandidatesAsync(UserPrincipal user, Guid projectId, CancellationToken cancellationToken) => Fail<IReadOnlyList<ProjectCandidateUserDto>>();
    public Task<ProjectMemberDto> AddMemberAsync(UserPrincipal user, Guid projectId, AddProjectMemberRequest request, string traceId, CancellationToken cancellationToken) => Fail<ProjectMemberDto>();
    public Task<ProjectMemberDto> RemoveMemberAsync(UserPrincipal user, Guid projectId, Guid userId, RemoveProjectMemberRequest request, string traceId, CancellationToken cancellationToken) => Fail<ProjectMemberDto>();
    public Task<ProjectMilestoneDto> AddMilestoneAsync(UserPrincipal user, Guid projectId, CreateProjectMilestoneRequest request, string traceId, CancellationToken cancellationToken) => Fail<ProjectMilestoneDto>();
    public Task<ProjectMilestoneDto> UpdateMilestoneAsync(UserPrincipal user, Guid projectId, Guid milestoneId, UpdateProjectMilestoneRequest request, string traceId, CancellationToken cancellationToken) => Fail<ProjectMilestoneDto>();
    public Task<ProjectRelationDto> AddRelationAsync(UserPrincipal user, Guid projectId, CreateProjectRelationRequest request, string traceId, CancellationToken cancellationToken) => Fail<ProjectRelationDto>();
    public Task<ProjectTaskDto> CreateTaskAsync(UserPrincipal user, Guid projectId, CreateProjectTaskRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<ProjectTaskDto>();
    public Task<ProjectTaskDto> CompleteTaskAsync(UserPrincipal user, Guid taskId, CompleteProjectTaskRequest request, string traceId, CancellationToken cancellationToken) => Fail<ProjectTaskDto>();
}

internal sealed class NotReadyEventInboxService : IEventInboxService, IAtomicEventInboxConfirmationService
{
    private static Task<T> Fail<T>() => throw NotReady.Error();

    public Task<PagedResult<InboxEventDto>> ListAsync(UserPrincipal user, int page, int pageSize, string? status, bool includeTestData, CancellationToken cancellationToken) => Fail<PagedResult<InboxEventDto>>();
    public Task<InboxEventDto?> GetAsync(UserPrincipal user, Guid eventId, bool includeTestData, string traceId, CancellationToken cancellationToken) => Fail<InboxEventDto?>();
    public Task<IReadOnlyList<InboxSourceOptionDto>> ListSourcesAsync(UserPrincipal user, Guid departmentId, CancellationToken cancellationToken) => Fail<IReadOnlyList<InboxSourceOptionDto>>();
    public Task<InboxEventDto> ReceiveAsync(UserPrincipal user, ReceiveInboxEventRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<InboxEventDto>();
    public Task<InboxEventDto> AnalyzeAsync(UserPrincipal user, Guid eventId, AnalyzeInboxEventRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<InboxEventDto>();
    public Task<InboxConfirmationDto> ConfirmAsync(UserPrincipal user, Guid eventId, ConfirmInboxEventRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<InboxConfirmationDto>();
    public Task<InboxConfirmationDto?> GetIdempotentConfirmationAsync(UserPrincipal user, Guid eventId, ConfirmInboxEventRequest request, string idempotencyKey, string traceId, CancellationToken cancellationToken) => Fail<InboxConfirmationDto?>();
    public Task<InboxConfirmationDto> ConfirmWithinTransactionAsync(UserPrincipal user, Guid eventId, ConfirmInboxEventRequest request, string idempotencyKey, string traceId, Guid? startedWorkflowTaskId, CancellationToken cancellationToken) => Fail<InboxConfirmationDto>();
    public Task<InboxTaskTraceDto?> TraceTaskAsync(UserPrincipal user, Guid taskId, CancellationToken cancellationToken) => Fail<InboxTaskTraceDto?>();
}
