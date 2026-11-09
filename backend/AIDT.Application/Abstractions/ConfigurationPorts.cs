using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Application.Abstractions;

public interface IWorkflowConfigurationService
{
    Task<IReadOnlyList<WorkflowPolicyUserDto>> ListUsersAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<PagedResult<WorkflowConfigurationDto>> ListAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? state,
        string? configurationKey,
        CancellationToken cancellationToken);

    Task<WorkflowConfigurationDto?> GetAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken);

    Task<WorkflowConfigurationDto> CreateAsync(
        UserPrincipal user,
        CreateWorkflowConfigurationRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkflowConfigurationDto> UpdateAsync(
        UserPrincipal user,
        Guid configurationId,
        UpdateWorkflowConfigurationRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkflowConfigurationValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid configurationId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkflowConfigurationDto> PublishAsync(
        UserPrincipal user,
        Guid configurationId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkflowConfigurationDto> RollbackAsync(
        UserPrincipal user,
        Guid configurationId,
        string? comment,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkflowConfigurationDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken);

    Task<WorkflowConfigurationDto> SetStatusAsync(
        UserPrincipal user,
        Guid configurationId,
        bool disable,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkflowConfigurationSimulationDto> SimulateAsync(
        UserPrincipal user,
        Guid configurationId,
        SimulateWorkflowConfigurationRequest request,
        CancellationToken cancellationToken);
}
