using AIDT.Application.Abstractions;
using AIDT.Domain.Agents;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Persistence.Repositories;

public sealed class SqlAgentRepository(AidtDbContext dbContext) : IAgentRepository
{
    private readonly Dictionary<Guid, AgentRunEntity> _trackedRuns = [];

    public async Task<AgentProfile?> GetProfileAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AgentProfiles.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return entity is null ? null : MapProfile(entity);
    }

    public async Task<AgentRun?> GetRunAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AgentRuns
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        _trackedRuns[id] = entity;
        return MapRun(entity);
    }

    public async Task AddRunAsync(AgentRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        var entity = new AgentRunEntity
        {
            Id = run.Id,
            AgentProfileId = run.AgentProfileId,
            RequestedBy = run.RequestedBy,
            Action = run.Action,
            Amount = run.Amount,
            RiskLevel = run.RiskLevel.ToString(),
            Status = run.Status.ToString(),
            ConfirmationId = run.ConfirmationId,
            CreatedAt = run.CreatedAt,
            OrganizationId = run.OrganizationId,
            DepartmentId = run.DepartmentId,
            EnvironmentCode = run.EnvironmentCode,
            IsTestData = run.IsTestData,
            SubjectType = run.SubjectType,
            BusinessObjectId = run.BusinessObjectId,
            WorkflowInstanceId = run.WorkflowInstanceId,
            WorkflowTaskId = run.WorkflowTaskId,
            MinutesSourceId = run.MinutesSourceId,
            Model = run.Model,
            GatewayCallId = run.GatewayCallId,
            GatewayMode = run.GatewayMode,
            ExternalModelCalled = run.ExternalModelCalled,
            ProxyTriggerMode = run.ProxyTriggerMode,
            ProxyTriggeredAt = run.ProxyTriggeredAt,
            OriginalAssigneeId = run.OriginalAssigneeId,
            CompensationStatus = run.CompensationStatus,
            ProxyIdempotencyKey = run.ProxyIdempotencyKey,
            StartedAt = run.StartedAt,
            EndedAt = run.EndedAt
        };
        await dbContext.AgentRuns.AddAsync(entity, cancellationToken);
        _trackedRuns[run.Id] = entity;
    }

    public async Task SaveRunAsync(AgentRun run, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(run);
        if (!_trackedRuns.TryGetValue(run.Id, out var entity))
        {
            entity = await dbContext.AgentRuns.SingleAsync(item => item.Id == run.Id, cancellationToken);
            _trackedRuns[run.Id] = entity;
        }

        entity.Status = run.Status.ToString();
        entity.ConfirmationId = run.ConfirmationId;
        entity.GatewayCallId = run.GatewayCallId;
        entity.GatewayMode = run.GatewayMode;
        entity.ExternalModelCalled = run.ExternalModelCalled;
        entity.CompensationStatus = run.CompensationStatus;
        entity.StartedAt = run.StartedAt;
        entity.EndedAt = run.EndedAt;
    }

    public Task AddConfirmationAsync(
        Guid runId,
        Guid confirmedBy,
        Guid confirmationId,
        string decision,
        string? comment,
        DateTimeOffset confirmedAt,
        CancellationToken cancellationToken) =>
        dbContext.AgentConfirmations.AddAsync(
            new AgentConfirmationEntity
            {
                Id = confirmationId,
                AgentRunId = runId,
                ConfirmedBy = confirmedBy,
                Decision = decision,
                Comment = comment,
                ConfirmedAt = confirmedAt
            },
            cancellationToken).AsTask();

    private static AgentProfile MapProfile(AgentProfileEntity entity) =>
        AgentProfile.Restore(
            entity.Id,
            entity.ServiceUserId,
            entity.Name,
            Enum.Parse<AgentAutonomyLevel>(entity.AutonomyLevel),
            entity.AmountLimit,
            Enum.Parse<AgentRiskLevel>(entity.RiskLimit),
            entity.IsActive,
            JsonStorage.Deserialize<string[]>(entity.AllowedToolsJson) ?? [],
            entity.Code,
            entity.DepartmentId,
            JsonStorage.Deserialize<string[]>(entity.DataScopesJson) ?? [],
            JsonStorage.Deserialize<string[]>(entity.ModelScopesJson) ?? [],
            JsonStorage.Deserialize<string[]>(entity.RequiredConfirmationsJson) ?? [],
            JsonStorage.Deserialize<string[]>(entity.StopConditionsJson) ?? [],
            Enum.Parse<AgentProfileLifecycleStatus>(entity.LifecycleStatus),
            entity.CurrentPublishedVersionId);

    private static AgentRun MapRun(AgentRunEntity entity) =>
        AgentRun.Restore(
            entity.Id,
            entity.AgentProfileId,
            entity.RequestedBy,
            entity.Action,
            entity.Amount,
            Enum.Parse<AgentRiskLevel>(entity.RiskLevel),
            Enum.Parse<AgentRunStatus>(entity.Status),
            entity.ConfirmationId,
            entity.CreatedAt,
            entity.OrganizationId,
            entity.DepartmentId,
            entity.EnvironmentCode,
            entity.IsTestData,
            entity.SubjectType,
            entity.BusinessObjectId,
            entity.WorkflowInstanceId,
            entity.WorkflowTaskId,
            entity.MinutesSourceId,
            entity.Model,
            entity.GatewayCallId,
            entity.GatewayMode,
            entity.ExternalModelCalled,
            entity.StartedAt,
            entity.EndedAt,
            entity.ProxyTriggerMode,
            entity.ProxyTriggeredAt,
            entity.OriginalAssigneeId,
            entity.CompensationStatus,
            entity.ProxyIdempotencyKey);
}
