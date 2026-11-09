using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

internal sealed class NotReadyAgentGovernanceService : IAgentGovernanceService
{
    public Task<PagedResult<AgentProfileDetailDto>> ListProfilesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? lifecycleStatus,
        string? departmentCode,
        string? search,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfileDetailDto?> GetProfileAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentIdentityOptionsDto> GetOptionsAsync(
        UserPrincipal user,
        Guid? currentAgentProfileId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfileContextDto> GetContextAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<IReadOnlyList<AgentProfileDto>> ListAvailableAsync(
        UserPrincipal user,
        string? departmentCode,
        string? search,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfileDetailDto> CreateDraftAsync(
        UserPrincipal user,
        CreateAgentProfileDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfileVersionDto> CreateNextDraftAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CreateNextAgentProfileDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfileVersionDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        UpdateAgentProfileDraftRequest request,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfileValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        ValidateAgentProfileVersionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfilePublishResultDto> PublishAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        PublishAgentProfileVersionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfileDetailDto> DisableAsync(
        UserPrincipal user,
        Guid agentProfileId,
        DisableAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfileDetailDto> ResumeAsync(
        UserPrincipal user,
        Guid agentProfileId,
        ResumeAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<IReadOnlyList<AgentProfileVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfilePublishResultDto> RollbackAsync(
        UserPrincipal user,
        Guid agentProfileId,
        RollbackAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<IReadOnlyList<AgentProfileChangeRequestDto>> ListChangeRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<AgentProfilePublishResultDto> ReviewChangeRequestAsync(
        UserPrincipal user,
        Guid requestId,
        ReviewAgentProfileChangeRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();
}
