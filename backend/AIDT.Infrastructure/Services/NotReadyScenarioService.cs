using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

internal sealed class NotReadyScenarioService : IScenarioService
{
    public Task<PagedResult<ScenarioDto>> ListVisibleAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        bool includeTestData,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto?> GetVisibleAsync(
        UserPrincipal user,
        Guid scenarioId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<PagedResult<ScenarioDto>> ListManageAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? status,
        string? search,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto> CreateDraftAsync(
        UserPrincipal user,
        CreateScenarioDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto> CreateNextDraftAsync(
        UserPrincipal user,
        Guid scenarioId,
        CreateNextScenarioVersionDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        UpdateScenarioDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto> PublishAsync(
        UserPrincipal user,
        Guid scenarioId,
        Guid versionId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto> DisableAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto> RestoreAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioActionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<IReadOnlyList<ScenarioVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid scenarioId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto> RollbackAsync(
        UserPrincipal user,
        Guid scenarioId,
        RollbackScenarioRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioPreflightDto> PreflightAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioStartInputDto input,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioLaunchDto> StartAsync(
        UserPrincipal user,
        Guid scenarioId,
        ScenarioStartInputDto input,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioLaunchDto?> GetLaunchAsync(
        UserPrincipal user,
        Guid launchId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<PagedResult<ScenarioLaunchSummaryDto>> ListMyLaunchesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<IReadOnlyList<ScenarioApprovalRequestDto>> ListApprovalRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<ScenarioDto> ReviewPublishAsync(
        UserPrincipal user,
        Guid requestId,
        ReviewScenarioPublishRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();
}
