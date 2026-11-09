using AIDT.Application.Contracts;
using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Identity;
using AIDT.Domain.Integrations;
using AIDT.Domain.Workflows;

namespace AIDT.Application.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface ICurrentUserAccessor
{
    UserPrincipal GetRequiredUser();
}

public interface IApplicationTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}

public interface IUnitOfWork
{
    Task<IApplicationTransaction> BeginTransactionAsync(CancellationToken cancellationToken);
}

public interface IObjectTypeRepository
{
    Task<ObjectType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ObjectType?> GetPublishedByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ObjectType objectType, CancellationToken cancellationToken);

    Task<List<ObjectType>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);

    Task<long> CountAsync(CancellationToken cancellationToken);
}

public interface IBusinessObjectRepository
{
    Task<BusinessObject?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsCodeAsync(Guid objectTypeId, string objectCode, CancellationToken cancellationToken);

    Task AddAsync(
        BusinessObject businessObject,
        Guid actorId,
        CancellationToken cancellationToken);

    Task UpdateAsync(
        BusinessObject businessObject,
        Guid actorId,
        string reason,
        CancellationToken cancellationToken);
}

public interface IObjectModelService
{
    Task<ObjectTypeDto> CreateAsync(
        UserPrincipal user,
        CreateObjectTypeRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ObjectTypeDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid id,
        UpdateObjectTypeDraftRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ObjectTypeValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ObjectTypeDto> PublishAsync(
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ObjectTypeDto> RetireAsync(
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<ObjectTypeDto> RestoreAsync(
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string traceId,
        string idempotencyKey,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ObjectTypeVersionDto>> ListVersionsAsync(
        Guid id,
        UserPrincipal user,
        CancellationToken cancellationToken);
}

public interface IWorkflowRepository
{
    Task<WorkflowInstance?> GetInstanceAsync(Guid id, CancellationToken cancellationToken);

    Task<WorkflowTask?> GetTaskAsync(Guid id, CancellationToken cancellationToken);

    Task AddInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken);

    Task AddTaskAsync(WorkflowTask task, CancellationToken cancellationToken);

    Task SaveInstanceAsync(WorkflowInstance instance, CancellationToken cancellationToken);

    Task SaveTaskAsync(WorkflowTask task, CancellationToken cancellationToken);

    Task AddTaskActionAsync(
        Guid taskId,
        Guid actorId,
        string action,
        string? comment,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken);
}

public interface IAuditRepository
{
    Task AddAsync(AuditEvent auditEvent, CancellationToken cancellationToken);
}

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message, CancellationToken cancellationToken);
}

public interface IAgentRepository
{
    Task<AgentProfile?> GetProfileAsync(Guid id, CancellationToken cancellationToken);

    Task<AgentRun?> GetRunAsync(Guid id, CancellationToken cancellationToken);

    Task AddRunAsync(AgentRun run, CancellationToken cancellationToken);

    Task SaveRunAsync(AgentRun run, CancellationToken cancellationToken);

    Task AddConfirmationAsync(
        Guid runId,
        Guid confirmedBy,
        Guid confirmationId,
        string decision,
        string? comment,
        DateTimeOffset confirmedAt,
        CancellationToken cancellationToken);
}

public interface IUserRepository
{
    Task<UserPrincipal?> GetPrincipalAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserPrincipal?> GetPrincipalByUserNameAsync(string userName, CancellationToken cancellationToken);
}

public interface IDepartmentRepository
{
    Task<Guid> ResolveIdAsync(
        UserPrincipal user,
        Guid? requestedDepartmentId,
        string objectTypeCode,
        CancellationToken cancellationToken);

    Task<Guid> ResolveAssigneeDepartmentAsync(
        Guid organizationId,
        Guid assigneeUserId,
        CancellationToken cancellationToken);
}

public sealed record IdempotencyEntry(
    Guid Id,
    string Scope,
    string IdempotencyKey,
    string RequestHash,
    string ResponseType,
    string ResponseId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);

public interface IIdempotencyStore
{
    Task<IdempotencyEntry?> FindAsync(string scope, string key, CancellationToken cancellationToken);

    Task AddAsync(IdempotencyEntry entry, CancellationToken cancellationToken);
}

public sealed record WorkflowVersionDefinition(
    Guid WorkflowDefinitionId,
    Guid WorkflowVersionId,
    string Code,
    string Name,
    int VersionNumber,
    string DefinitionJson);

public interface IWorkflowCatalogRepository
{
    Task<WorkflowVersionDefinition?> GetVersionAsync(Guid workflowVersionId, CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowVersionDefinition>> ListPublishedAsync(CancellationToken cancellationToken);
}

public interface IWorkspaceQueryService
{
    Task<PagedResult<ObjectTypeDto>> ListObjectTypesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ObjectTypeDto>> ListPublishedObjectTypesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<PagedResult<BusinessObjectDto>> ListBusinessObjectsAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<BusinessObjectDto?> GetBusinessObjectAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BusinessObjectVersionDto>?> GetBusinessObjectVersionsAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);

    Task<BusinessObjectContextDto?> GetBusinessObjectContextAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ObjectFieldUserOptionDto>> ListObjectFieldUsersAsync(
        UserPrincipal user,
        string? search,
        Guid? departmentId,
        int limit,
        CancellationToken cancellationToken);

    Task<PagedResult<WorkflowInstanceDto>> ListWorkflowInstancesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowDefinitionDto>> ListWorkflowDefinitionsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<WorkflowInstanceDto?> GetWorkflowInstanceAsync(
        UserPrincipal user,
        Guid id,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);

    Task<PagedResult<WorkflowTaskDto>> ListTasksAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<Guid?> GetTaskDepartmentAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowTimelineItemDto>> GetWorkflowTimelineAsync(
        UserPrincipal user,
        Guid instanceId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);

    Task<PagedResult<AuditEventDto>> ListAuditEventsAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? entityType,
        string? entityId,
        CancellationToken cancellationToken);

    Task<PagedResult<AgentRunDto>> ListAgentRunsAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentProfileDto>> ListAgentProfilesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ConnectorDto>> ListConnectorsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<PagedResult<OutboxMessageDto>> ListOutboxMessagesAsync(
        UserPrincipal user,
        Guid connectorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<DashboardDto> GetDashboardAsync(
        UserPrincipal user,
        bool includeTestData,
        CancellationToken cancellationToken);
}

public sealed record WorkflowAdvanceResult(
    string? NextNodeCode,
    Guid? NextAssigneeId,
    WorkflowInstanceStatus InstanceStatus,
    ResolvedWorkflowAssignment? Assignment = null);

public sealed record WorkflowStartResult(
    string FirstNodeCode,
    Guid FirstAssigneeId,
    ResolvedWorkflowAssignment? Assignment = null);

public interface IWorkflowEngine
{
    Task<WorkflowStartResult> StartAsync(
        WorkflowVersionDefinition definition,
        BusinessObject businessObject,
        CancellationToken cancellationToken,
        Guid? initiatorUserId = null);

    Task<WorkflowAdvanceResult> AdvanceAsync(
        WorkflowInstance instance,
        WorkflowTask task,
        WorkflowDecision decision,
        CancellationToken cancellationToken);
}

public interface IPlatformCapabilityService
{
    PlatformCapabilitiesDto GetCapabilities();
}
