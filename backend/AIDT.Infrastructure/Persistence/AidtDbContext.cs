using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AIDT.Infrastructure.Persistence;

public sealed class AidtDbContext(DbContextOptions<AidtDbContext> options) : DbContext(options)
{
    public DbSet<OrganizationEntity> Organizations => Set<OrganizationEntity>();
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<UserRoleAssignmentEntity> UserRoleAssignments => Set<UserRoleAssignmentEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();
    public DbSet<DataScopeEntity> DataScopes => Set<DataScopeEntity>();
    public DbSet<AccessChangeRequestEntity> AccessChangeRequests => Set<AccessChangeRequestEntity>();
    public DbSet<DepartmentEntity> Departments => Set<DepartmentEntity>();
    public DbSet<UserDepartmentMembershipEntity> UserDepartmentMemberships => Set<UserDepartmentMembershipEntity>();
    public DbSet<ObjectTypeEntity> ObjectTypes => Set<ObjectTypeEntity>();
    public DbSet<FieldDefinitionEntity> FieldDefinitions => Set<FieldDefinitionEntity>();
    public DbSet<ObjectTypeVersionEntity> ObjectTypeVersions => Set<ObjectTypeVersionEntity>();
    public DbSet<ObjectRelationEntity> ObjectRelations => Set<ObjectRelationEntity>();
    public DbSet<BusinessObjectEntity> BusinessObjects => Set<BusinessObjectEntity>();
    public DbSet<BusinessObjectVersionEntity> BusinessObjectVersions => Set<BusinessObjectVersionEntity>();
    public DbSet<BusinessObjectRelationEntity> BusinessObjectRelations => Set<BusinessObjectRelationEntity>();
    public DbSet<WorkflowDefinitionEntity> WorkflowDefinitions => Set<WorkflowDefinitionEntity>();
    public DbSet<WorkflowVersionEntity> WorkflowVersions => Set<WorkflowVersionEntity>();
    public DbSet<WorkflowInstanceEntity> WorkflowInstances => Set<WorkflowInstanceEntity>();
    public DbSet<WorkflowTaskEntity> WorkflowTasks => Set<WorkflowTaskEntity>();
    public DbSet<WorkflowTaskCandidateEntity> WorkflowTaskCandidates => Set<WorkflowTaskCandidateEntity>();
    public DbSet<WorkflowTaskActionEntity> WorkflowTaskActions => Set<WorkflowTaskActionEntity>();
    public DbSet<WorkflowNodePolicyEntity> WorkflowNodePolicies => Set<WorkflowNodePolicyEntity>();
    public DbSet<WorkflowNotificationEntity> WorkflowNotifications => Set<WorkflowNotificationEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();
    public DbSet<EvidenceAssetEntity> EvidenceAssets => Set<EvidenceAssetEntity>();
    public DbSet<ConnectorEntity> Connectors => Set<ConnectorEntity>();
    public DbSet<ConnectorCommandEntity> ConnectorCommands => Set<ConnectorCommandEntity>();
    public DbSet<ConnectorReceiptEntity> ConnectorReceipts => Set<ConnectorReceiptEntity>();
    public DbSet<OutboxEventEntity> OutboxEvents => Set<OutboxEventEntity>();
    public DbSet<DeadLetterEntity> DeadLetters => Set<DeadLetterEntity>();
    public DbSet<ReconciliationEntity> Reconciliations => Set<ReconciliationEntity>();
    public DbSet<IdempotencyRecordEntity> IdempotencyRecords => Set<IdempotencyRecordEntity>();
    public DbSet<AgentProfileEntity> AgentProfiles => Set<AgentProfileEntity>();
    public DbSet<AgentProfileVersionEntity> AgentProfileVersions => Set<AgentProfileVersionEntity>();
    public DbSet<AgentRunEntity> AgentRuns => Set<AgentRunEntity>();
    public DbSet<AgentConfirmationEntity> AgentConfirmations => Set<AgentConfirmationEntity>();
    public DbSet<AgentGatewayCallEntity> AgentGatewayCalls => Set<AgentGatewayCallEntity>();
    public DbSet<MinutesSourceEntity> MinutesSources => Set<MinutesSourceEntity>();
    public DbSet<MinutesActionItemEntity> MinutesActionItems => Set<MinutesActionItemEntity>();
    public DbSet<MinutesTaskEntity> MinutesTasks => Set<MinutesTaskEntity>();
    public DbSet<AttachmentEntity> Attachments => Set<AttachmentEntity>();
    public DbSet<DocumentVersionEntity> DocumentVersions => Set<DocumentVersionEntity>();
    public DbSet<AttachmentLinkEntity> AttachmentLinks => Set<AttachmentLinkEntity>();
    public DbSet<SignatureEntity> Signatures => Set<SignatureEntity>();
    public DbSet<ConfigurationVersionEntity> ConfigurationVersions => Set<ConfigurationVersionEntity>();
    public DbSet<NavigationWorkspaceEntity> NavigationWorkspaces => Set<NavigationWorkspaceEntity>();
    public DbSet<NavigationVersionEntity> NavigationVersions => Set<NavigationVersionEntity>();
    public DbSet<NavigationItemEntity> NavigationItems => Set<NavigationItemEntity>();
    public DbSet<NavigationUserPreferenceEntity> NavigationUserPreferences => Set<NavigationUserPreferenceEntity>();
    public DbSet<WorkbenchTemplateEntity> WorkbenchTemplates => Set<WorkbenchTemplateEntity>();
    public DbSet<WorkbenchTemplateVersionEntity> WorkbenchTemplateVersions => Set<WorkbenchTemplateVersionEntity>();
    public DbSet<WorkbenchCardEntity> WorkbenchCards => Set<WorkbenchCardEntity>();
    public DbSet<WorkbenchAssignmentEntity> WorkbenchAssignments => Set<WorkbenchAssignmentEntity>();
    public DbSet<ProjectProfileEntity> ProjectProfiles => Set<ProjectProfileEntity>();
    public DbSet<ProjectMemberEntity> ProjectMembers => Set<ProjectMemberEntity>();
    public DbSet<ProjectMilestoneEntity> ProjectMilestones => Set<ProjectMilestoneEntity>();
    public DbSet<ProjectRelationEntity> ProjectRelations => Set<ProjectRelationEntity>();
    public DbSet<ProjectTaskEntity> ProjectTasks => Set<ProjectTaskEntity>();
    public DbSet<EventInboxEntity> EventInboxes => Set<EventInboxEntity>();
    public DbSet<EventSuggestionEntity> EventSuggestions => Set<EventSuggestionEntity>();
    public DbSet<McpClientEntity> McpClients => Set<McpClientEntity>();
    public DbSet<McpAuthorizationRequestEntity> McpAuthorizationRequests => Set<McpAuthorizationRequestEntity>();
    public DbSet<McpAuthorizationCodeEntity> McpAuthorizationCodes => Set<McpAuthorizationCodeEntity>();
    public DbSet<McpAccessTokenEntity> McpAccessTokens => Set<McpAccessTokenEntity>();
    public DbSet<McpHumanConfirmationEntity> McpHumanConfirmations => Set<McpHumanConfirmationEntity>();
    public DbSet<ScenarioDefinitionEntity> ScenarioDefinitions => Set<ScenarioDefinitionEntity>();
    public DbSet<ScenarioVersionEntity> ScenarioVersions => Set<ScenarioVersionEntity>();
    public DbSet<ScenarioLaunchEntity> ScenarioLaunches => Set<ScenarioLaunchEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AidtDbContext).Assembly);
        MapUtcDateTimeOffset(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    private static void MapUtcDateTimeOffset(ModelBuilder modelBuilder)
    {
        var dateTimeOffsetConverter = new ValueConverter<DateTimeOffset, DateTime>(
            value => value.UtcDateTime,
            value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));
        var nullableDateTimeOffsetConverter = new ValueConverter<DateTimeOffset?, DateTime?>(
            value => value.HasValue ? value.Value.UtcDateTime : null,
            value => value.HasValue
                ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                : null);

        foreach (var property in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()))
        {
            if (property.ClrType == typeof(DateTimeOffset))
            {
                property.SetValueConverter(dateTimeOffsetConverter);
                property.SetColumnType("datetime2(3)");
            }
            else if (property.ClrType == typeof(DateTimeOffset?))
            {
                property.SetValueConverter(nullableDateTimeOffsetConverter);
                property.SetColumnType("datetime2(3)");
            }
        }
    }
}
