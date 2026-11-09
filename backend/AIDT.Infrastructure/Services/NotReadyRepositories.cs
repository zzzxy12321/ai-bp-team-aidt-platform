using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Integrations;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;

namespace AIDT.Infrastructure.Services;

internal static class NotReady
{
    public static ServiceUnavailableException Error() =>
        new(
            ErrorCodes.PersistenceNotReady,
            "SQL Server 尚未完成安装和迁移，持久化能力暂不可用。");
}

internal sealed class NotReadyObjectTypeRepository : IObjectTypeRepository
{
    public Task<ObjectType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ObjectType?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddAsync(ObjectType objectType, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<List<ObjectType>> ListAsync(int page, int pageSize, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<long> CountAsync(CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyBusinessObjectRepository : IBusinessObjectRepository
{
    public Task<BusinessObject?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<bool> ExistsCodeAsync(Guid objectTypeId, string objectCode, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddAsync(BusinessObject businessObject, Guid actorId, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task UpdateAsync(BusinessObject businessObject, Guid actorId, string reason, CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyObjectModelService : IObjectModelService
{
    public Task<ObjectTypeDto> CreateAsync(UserPrincipal user, CreateObjectTypeRequest request, string traceId, string idempotencyKey, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<ObjectTypeDto> UpdateDraftAsync(UserPrincipal user, Guid id, UpdateObjectTypeDraftRequest request, string traceId, string idempotencyKey, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<ObjectTypeValidationDto> ValidateAsync(UserPrincipal user, Guid id, ObjectTypeActionRequest request, string traceId, string idempotencyKey, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<ObjectTypeDto> PublishAsync(UserPrincipal user, Guid id, ObjectTypeActionRequest request, string traceId, string idempotencyKey, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<ObjectTypeDto> RetireAsync(UserPrincipal user, Guid id, ObjectTypeActionRequest request, string traceId, string idempotencyKey, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<ObjectTypeDto> RestoreAsync(UserPrincipal user, Guid id, ObjectTypeActionRequest request, string traceId, string idempotencyKey, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<IReadOnlyList<ObjectTypeVersionDto>> ListVersionsAsync(Guid id, UserPrincipal user, CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyWorkflowRepository : IWorkflowRepository
{
    public Task<WorkflowInstance?> GetInstanceAsync(Guid id, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<WorkflowTask?> GetTaskAsync(Guid id, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddTaskAsync(WorkflowTask task, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task SaveInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task SaveTaskAsync(WorkflowTask task, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddTaskActionAsync(
        Guid taskId,
        Guid actorId,
        string action,
        string? comment,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyAuditRepository : IAuditRepository
{
    public Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyOutboxRepository : IOutboxRepository
{
    public Task AddAsync(OutboxMessage message, CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyAgentRepository : IAgentRepository
{
    public Task<AgentProfile?> GetProfileAsync(Guid id, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentRun?> GetRunAsync(Guid id, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddRunAsync(AgentRun run, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task SaveRunAsync(AgentRun run, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddConfirmationAsync(Guid runId, Guid confirmedBy, Guid confirmationId, string decision, string? comment, DateTimeOffset confirmedAt, CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyUnitOfWork : IUnitOfWork
{
    public Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyUserRepository : IUserRepository
{
    public Task<UserPrincipal?> GetPrincipalAsync(Guid userId, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<UserPrincipal?> GetPrincipalByUserNameAsync(string userName, CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyDepartmentRepository : IDepartmentRepository
{
    public Task<Guid> ResolveIdAsync(
        UserPrincipal user,
        Guid? requestedDepartmentId,
        string objectTypeCode,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<Guid> ResolveAssigneeDepartmentAsync(
        Guid organizationId,
        Guid assigneeUserId,
        CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyWorkflowCatalogRepository : IWorkflowCatalogRepository
{
    public Task<WorkflowVersionDefinition?> GetVersionAsync(Guid workflowVersionId, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<IReadOnlyList<WorkflowVersionDefinition>> ListPublishedAsync(CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyIdempotencyStore : IIdempotencyStore
{
    public Task<IdempotencyEntry?> FindAsync(string scope, string key, CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddAsync(IdempotencyEntry entry, CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyWorkspaceQueryService : IWorkspaceQueryService
{
    public Task<PagedResult<ObjectTypeDto>> ListObjectTypesAsync(UserPrincipal user, int page, int pageSize, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<IReadOnlyList<ObjectTypeDto>> ListPublishedObjectTypesAsync(UserPrincipal user, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<PagedResult<BusinessObjectDto>> ListBusinessObjectsAsync(UserPrincipal user, int page, int pageSize, bool includeTestData, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<BusinessObjectDto?> GetBusinessObjectAsync(UserPrincipal user, Guid id, bool includeTestData, string traceId, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<IReadOnlyList<BusinessObjectVersionDto>?> GetBusinessObjectVersionsAsync(UserPrincipal user, Guid id, bool includeTestData, string traceId, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<BusinessObjectContextDto?> GetBusinessObjectContextAsync(UserPrincipal user, Guid id, bool includeTestData, string traceId, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<IReadOnlyList<ObjectFieldUserOptionDto>> ListObjectFieldUsersAsync(UserPrincipal user, string? search, Guid? departmentId, int limit, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<PagedResult<WorkflowInstanceDto>> ListWorkflowInstancesAsync(UserPrincipal user, int page, int pageSize, string? status, bool includeTestData, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<IReadOnlyList<WorkflowDefinitionDto>> ListWorkflowDefinitionsAsync(UserPrincipal user, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<WorkflowInstanceDto?> GetWorkflowInstanceAsync(UserPrincipal user, Guid id, bool includeTestData, string traceId, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<PagedResult<WorkflowTaskDto>> ListTasksAsync(UserPrincipal user, int page, int pageSize, string? status, bool includeTestData, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<Guid?> GetTaskDepartmentAsync(UserPrincipal user, Guid taskId, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<IReadOnlyList<WorkflowTimelineItemDto>> GetWorkflowTimelineAsync(UserPrincipal user, Guid instanceId, bool includeTestData, string traceId, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<PagedResult<AuditEventDto>> ListAuditEventsAsync(UserPrincipal user, int page, int pageSize, string? entityType, string? entityId, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<PagedResult<AgentRunDto>> ListAgentRunsAsync(UserPrincipal user, int page, int pageSize, string? status, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<IReadOnlyList<AgentProfileDto>> ListAgentProfilesAsync(UserPrincipal user, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<IReadOnlyList<ConnectorDto>> ListConnectorsAsync(UserPrincipal user, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<PagedResult<OutboxMessageDto>> ListOutboxMessagesAsync(UserPrincipal user, Guid connectorId, int page, int pageSize, CancellationToken cancellationToken) => throw NotReady.Error();
    public Task<DashboardDto> GetDashboardAsync(UserPrincipal user, bool includeTestData, CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyEmployeeWorkspaceQueryService : IEmployeeWorkspaceQueryService
{
    public Task<MyWorkDto> GetMyWorkAsync(
        UserPrincipal user,
        bool includeTestData,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<DepartmentWorkspaceDto> GetDepartmentWorkspaceAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyDepartmentCommandService : IDepartmentCommandService
{
    public Task<DepartmentDto> CreateAsync(
        UserPrincipal user,
        CreateDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task AddMemberAsync(
        UserPrincipal user,
        Guid departmentId,
        AddDepartmentMemberRequest request,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyFileCenterService : IFileCenterService
{
    public Task<PagedResult<FileAttachmentDto>> ListAsync(
        UserPrincipal user,
        AttachmentListQuery query,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<FileAttachmentDto?> GetAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool includeTestData,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<IReadOnlyList<FileVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool includeTestData,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<FileAttachmentDto> UploadAsync(
        UserPrincipal user,
        UploadFileCommand command,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<FileAttachmentDto> AddVersionAsync(
        UserPrincipal user,
        Guid attachmentId,
        UploadFileCommand command,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<FileAttachmentDto> RequestDeletionAsync(
        UserPrincipal user,
        Guid attachmentId,
        RequestFileDeletionCommand command,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<FileContentDto> OpenContentAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool preview,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();
}
