using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class ScenarioDefinitionConfiguration :
    IEntityTypeConfiguration<ScenarioDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<ScenarioDefinitionEntity> builder)
    {
        builder.ToTable("ScenarioDefinition", "Scenario", table =>
        {
            table.HasCheckConstraint(
                "CK_ScenarioDefinition_Status",
                "[LifecycleStatus] IN ('Draft','Published','Disabled')");
            table.HasCheckConstraint(
                "CK_ScenarioDefinition_Disabled",
                "[LifecycleStatus] <> 'Disabled' OR ([DisabledBy] IS NOT NULL AND [DisabledAt] IS NOT NULL)");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_ScenarioDefinition");
        builder.Property(entity => entity.Code).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(160).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.LifecycleStatus).HasMaxLength(24).IsRequired();
        builder.Property(entity => entity.DisabledReason).HasMaxLength(1000);
        builder.Property(entity => entity.Version).HasDefaultValue(1L);
        builder.Property(entity => entity.Revision).HasDefaultValue(1L);
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Code })
            .IsUnique()
            .HasDatabaseName("UX_ScenarioDefinition_Organization_Code");
        builder.HasIndex(entity => new { entity.OwnerDepartmentId, entity.LifecycleStatus, entity.UpdatedAt })
            .HasDatabaseName("IX_ScenarioDefinition_Department_Status_UpdatedAt");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.LifecycleStatus, entity.Code })
            .HasDatabaseName("IX_ScenarioDefinition_Organization_Status_Code");
        builder.HasOne<OrganizationEntity>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .HasConstraintName("FK_ScenarioDefinition_Organization")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany()
            .HasForeignKey(entity => entity.OwnerDepartmentId)
            .HasConstraintName("FK_ScenarioDefinition_Department")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_ScenarioDefinition_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_ScenarioDefinition_UpdatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.DisabledBy)
            .HasConstraintName("FK_ScenarioDefinition_DisabledBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ScenarioVersionConfiguration :
    IEntityTypeConfiguration<ScenarioVersionEntity>
{
    public void Configure(EntityTypeBuilder<ScenarioVersionEntity> builder)
    {
        builder.ToTable("ScenarioVersion", "Scenario", table =>
        {
            table.HasCheckConstraint(
                "CK_ScenarioVersion_Status",
                "[Status] IN ('Draft','Validated','Published','Retired')");
            table.HasCheckConstraint(
                "CK_ScenarioVersion_PayloadJson",
                "ISJSON([PayloadJson]) = 1");
            table.HasCheckConstraint(
                "CK_ScenarioVersion_ReferenceSnapshotJson",
                "[ReferenceSnapshotJson] IS NULL OR ISJSON([ReferenceSnapshotJson]) = 1");
            table.HasCheckConstraint(
                "CK_ScenarioVersion_ValidationJson",
                "[ValidationJson] IS NULL OR ISJSON([ValidationJson]) = 1");
            table.HasCheckConstraint(
                "CK_ScenarioVersion_ImpactAnalysisJson",
                "[ImpactAnalysisJson] IS NULL OR ISJSON([ImpactAnalysisJson]) = 1");
            table.HasCheckConstraint(
                "CK_ScenarioVersion_PublishedAt",
                "[Status] <> 'Published' OR [PublishedAt] IS NOT NULL");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_ScenarioVersion");
        builder.Property(entity => entity.Status).HasMaxLength(24).IsRequired();
        builder.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ReferenceSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.ValidationJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.ImpactAnalysisJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.ChangeSummary).HasMaxLength(1000);
        builder.Property(entity => entity.Revision).HasDefaultValue(1L);
        builder.HasIndex(entity => new { entity.ScenarioId, entity.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_ScenarioVersion_Scenario_Version");
        builder.HasIndex(entity => new { entity.ScenarioId, entity.Status, entity.VersionNumber })
            .HasDatabaseName("IX_ScenarioVersion_Scenario_Status_Version");
        builder.HasOne<ScenarioDefinitionEntity>().WithMany()
            .HasForeignKey(entity => entity.ScenarioId)
            .HasConstraintName("FK_ScenarioVersion_Scenario")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_ScenarioVersion_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_ScenarioVersion_UpdatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.ValidatedBy)
            .HasConstraintName("FK_ScenarioVersion_ValidatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.PublishedBy)
            .HasConstraintName("FK_ScenarioVersion_PublishedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.RetiredBy)
            .HasConstraintName("FK_ScenarioVersion_RetiredBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ScenarioLaunchConfiguration :
    IEntityTypeConfiguration<ScenarioLaunchEntity>
{
    public void Configure(EntityTypeBuilder<ScenarioLaunchEntity> builder)
    {
        builder.ToTable("ScenarioLaunch", "Scenario", table =>
        {
            table.HasCheckConstraint(
                "CK_ScenarioLaunch_Environment",
                "[EnvironmentCode] IN ('demo','test','prod')");
            table.HasCheckConstraint(
                "CK_ScenarioLaunch_AttachmentIdsJson",
                "ISJSON([AttachmentIdsJson]) = 1");
            table.HasCheckConstraint(
                "CK_ScenarioLaunch_MinutesSourceIdsJson",
                "ISJSON([MinutesSourceIdsJson]) = 1");
            table.HasCheckConstraint(
                "CK_ScenarioLaunch_BusinessLinksJson",
                "ISJSON([BusinessLinksJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_ScenarioLaunch");
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.AttachmentIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.MinutesSourceIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.BusinessLinksJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.StartedBy, entity.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_ScenarioLaunch_Org_User_Idempotency");
        builder.HasIndex(entity => new { entity.ScenarioId, entity.ScenarioVersionNumber, entity.StartedAt })
            .HasDatabaseName("IX_ScenarioLaunch_Scenario_Version_StartedAt");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.StartedAt, entity.Id })
            .HasDatabaseName("IX_ScenarioLaunch_Department_StartedAt");
        builder.HasIndex(entity => entity.BusinessObjectId)
            .HasDatabaseName("IX_ScenarioLaunch_BusinessObjectId");
        builder.HasIndex(entity => entity.WorkflowInstanceId)
            .HasDatabaseName("IX_ScenarioLaunch_WorkflowInstanceId");
        builder.HasOne<OrganizationEntity>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .HasConstraintName("FK_ScenarioLaunch_Organization")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_ScenarioLaunch_Department")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ScenarioDefinitionEntity>().WithMany()
            .HasForeignKey(entity => entity.ScenarioId)
            .HasConstraintName("FK_ScenarioLaunch_Scenario")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ScenarioVersionEntity>().WithMany()
            .HasForeignKey(entity => entity.ScenarioVersionId)
            .HasConstraintName("FK_ScenarioLaunch_ScenarioVersion")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.StartedBy)
            .HasConstraintName("FK_ScenarioLaunch_StartedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany()
            .HasForeignKey(entity => entity.BusinessObjectId)
            .HasConstraintName("FK_ScenarioLaunch_BusinessObject")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowInstanceEntity>().WithMany()
            .HasForeignKey(entity => entity.WorkflowInstanceId)
            .HasConstraintName("FK_ScenarioLaunch_WorkflowInstance")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowTaskEntity>().WithMany()
            .HasForeignKey(entity => entity.WorkflowTaskId)
            .HasConstraintName("FK_ScenarioLaunch_WorkflowTask")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany()
            .HasForeignKey(entity => entity.ProjectId)
            .HasConstraintName("FK_ScenarioLaunch_Project")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProjectTaskEntity>().WithMany()
            .HasForeignKey(entity => entity.ProjectTaskId)
            .HasConstraintName("FK_ScenarioLaunch_ProjectTask")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventInboxEntity>().WithMany()
            .HasForeignKey(entity => entity.EventInboxId)
            .HasConstraintName("FK_ScenarioLaunch_EventInbox")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgentRunEntity>().WithMany()
            .HasForeignKey(entity => entity.AgentRunId)
            .HasConstraintName("FK_ScenarioLaunch_AgentRun")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
