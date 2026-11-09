using AIDT.Application.Contracts;
using AIDT.Domain.Identity;
using AIDT.Domain.Scenarios;

namespace AIDT.Application.Abstractions;

public interface IScenarioService
{
    Task<PagedResult<ScenarioDto>> ListVisibleAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<ScenarioDto?> GetVisibleAsync(
        UserPrincipal user,
        Guid scenarioId,
        CancellationToken cancellationToken);

    Task<PagedResult<ScenarioDto>> ListManageAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        string? search,
        CancellationToken cancellationToken);

    Task<ScenarioDto> CreateDraftAsync(
        UserPrincipal user,
        CreateScenarioDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioDto> CreateNextDraftAsync(
        UserPrincipal user,
        Guid scenarioId,
        CreateNextScenarioVersionDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        UpdateScenarioDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioDto> PublishAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioDto> DisableAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioDto> RestoreAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ScenarioVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid scenarioId,
        CancellationToken cancellationToken);

    Task<ScenarioDto> RollbackAsync(
        UserPrincipal user,
        Guid scenarioId,
        RollbackScenarioRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioPreflightDto> PreflightAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioStartInputDto input,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioLaunchDto> StartAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioStartInputDto input,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<ScenarioLaunchDto?> GetLaunchAsync(
        UserPrincipal user,
        Guid launchId,
        CancellationToken cancellationToken);

    Task<PagedResult<ScenarioLaunchSummaryDto>> ListMyLaunchesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ScenarioApprovalRequestDto>> ListApprovalRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken);

    Task<ScenarioDto> ReviewPublishAsync(
        UserPrincipal user,
        Guid requestId,
        ReviewScenarioPublishRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);
}
