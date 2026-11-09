using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

internal sealed class NotReadyWorkflowConfigurationService : IWorkflowConfigurationService
{
    private static Task<T> Fail<T>() => throw NotReady.Error();

    public Task<IReadOnlyList<WorkflowPolicyUserDto>> ListUsersAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) => Fail<IReadOnlyList<WorkflowPolicyUserDto>>();

    public Task<PagedResult<WorkflowConfigurationDto>> ListAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? state,
        string? configurationKey,
        CancellationToken cancellationToken) => Fail<PagedResult<WorkflowConfigurationDto>>();

    public Task<WorkflowConfigurationDto?> GetAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken) => Fail<WorkflowConfigurationDto?>();

    public Task<WorkflowConfigurationDto> CreateAsync(
        UserPrincipal user,
        CreateWorkflowConfigurationRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => Fail<WorkflowConfigurationDto>();

    public Task<WorkflowConfigurationDto> UpdateAsync(
        UserPrincipal user,
        Guid configurationId,
        UpdateWorkflowConfigurationRequest request,
        string traceId,
        CancellationToken cancellationToken) => Fail<WorkflowConfigurationDto>();

    public Task<WorkflowConfigurationValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid configurationId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => Fail<WorkflowConfigurationValidationDto>();

    public Task<WorkflowConfigurationDto> PublishAsync(
        UserPrincipal user,
        Guid configurationId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => Fail<WorkflowConfigurationDto>();

    public Task<WorkflowConfigurationDto> RollbackAsync(
        UserPrincipal user,
        Guid configurationId,
        string? comment,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => Fail<WorkflowConfigurationDto>();

    public Task<IReadOnlyList<WorkflowConfigurationDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid configurationId,
        CancellationToken cancellationToken) => Fail<IReadOnlyList<WorkflowConfigurationDto>>();

    public Task<WorkflowConfigurationDto> SetStatusAsync(
        UserPrincipal user,
        Guid configurationId,
        bool disable,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => Fail<WorkflowConfigurationDto>();

    public Task<WorkflowConfigurationSimulationDto> SimulateAsync(
        UserPrincipal user,
        Guid configurationId,
        SimulateWorkflowConfigurationRequest request,
        CancellationToken cancellationToken) => Fail<WorkflowConfigurationSimulationDto>();
}
