using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Application.Abstractions;

public interface IAgentGovernanceService
{
    Task<PagedResult<AgentProfileDetailDto>> ListProfilesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        string? lifecycleStatus,
        string? departmentCode,
        string? search,
        CancellationToken cancellationToken);

    Task<AgentProfileDetailDto?> GetProfileAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken);

    Task<AgentIdentityOptionsDto> GetOptionsAsync(
        UserPrincipal user,
        Guid? currentAgentProfileId,
        CancellationToken cancellationToken);

    Task<AgentProfileContextDto> GetContextAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentProfileDto>> ListAvailableAsync(
        UserPrincipal user,
        string? departmentCode,
        string? search,
        CancellationToken cancellationToken);

    Task<AgentProfileDetailDto> CreateDraftAsync(
        UserPrincipal user,
        CreateAgentProfileDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<AgentProfileVersionDto> CreateNextDraftAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CreateNextAgentProfileDraftRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<AgentProfileVersionDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        UpdateAgentProfileDraftRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<AgentProfileValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        ValidateAgentProfileVersionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<AgentProfilePublishResultDto> PublishAsync(
        UserPrincipal user,
        Guid agentProfileId,
        Guid versionId,
        PublishAgentProfileVersionRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<AgentProfileDetailDto> DisableAsync(
        UserPrincipal user,
        Guid agentProfileId,
        DisableAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<AgentProfileDetailDto> ResumeAsync(
        UserPrincipal user,
        Guid agentProfileId,
        ResumeAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentProfileVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid agentProfileId,
        CancellationToken cancellationToken);

    Task<AgentProfilePublishResultDto> RollbackAsync(
        UserPrincipal user,
        Guid agentProfileId,
        RollbackAgentProfileRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AgentProfileChangeRequestDto>> ListChangeRequestsAsync(
        UserPrincipal user,
        string? status,
        CancellationToken cancellationToken);

    Task<AgentProfilePublishResultDto> ReviewChangeRequestAsync(
        UserPrincipal user,
        Guid requestId,
        ReviewAgentProfileChangeRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);
}
