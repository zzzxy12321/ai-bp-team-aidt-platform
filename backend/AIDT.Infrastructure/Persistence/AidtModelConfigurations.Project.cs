using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class ProjectProfileConfiguration : IEntityTypeConfiguration<ProjectProfileEntity>
{
    public void Configure(EntityTypeBuilder<ProjectProfileEntity> builder)
    {
        builder.ToTable("ProjectProfile", "PMO", table =>
        {
            table.HasCheckConstraint(
                "CK_ProjectProfile_Confidentiality",
                "[ConfidentialityLevel] IN ('Internal','Sensitive','Core')");
            table.HasCheckConstraint(
                "CK_ProjectProfile_Progress",
                "[ProgressPercent] BETWEEN 0 AND 100");
            table.HasCheckConstraint(
                "CK_ProjectProfile_Dates",
                "[PlannedEndDate] IS NULL OR [PlannedStartDate] IS NULL OR [PlannedEndDate] >= [PlannedStartDate]");
        });
        builder.HasKey(entity => entity.BusinessObjectId).HasName("PK_ProjectProfile");
        builder.Property(entity => entity.Objective).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.CurrentStage).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.ConfidentialityLevel).HasMaxLength(16).IsRequired();
        builder.HasIndex(entity => new { entity.OwnerUserId, entity.UpdatedAt })
            .HasDatabaseName("IX_ProjectProfile_Owner_UpdatedAt");
        builder.HasOne<BusinessObjectEntity>().WithOne()
            .HasForeignKey<ProjectProfileEntity>(entity => entity.BusinessObjectId)
            .HasConstraintName("FK_ProjectProfile_BusinessObject")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.OwnerUserId)
            .HasConstraintName("FK_ProjectProfile_Owner")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_ProjectProfile_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMemberEntity>
{
    public void Configure(EntityTypeBuilder<ProjectMemberEntity> builder)
    {
        builder.ToTable("ProjectMember", "PMO", table =>
            table.HasCheckConstraint(
                "CK_ProjectMember_Role",
                "[ProjectRole] IN ('Lead','Manager','Contributor','Viewer')"));
        builder.HasKey(entity => new { entity.BusinessObjectId, entity.UserId })
            .HasName("PK_ProjectMember");
        builder.Property(entity => entity.ProjectRole).HasMaxLength(24).IsRequired();
        builder.HasIndex(entity => new { entity.UserId, entity.IsActive, entity.BusinessObjectId })
            .HasDatabaseName("IX_ProjectMember_User_Active_Project");
        builder.HasOne<BusinessObjectEntity>().WithMany()
            .HasForeignKey(entity => entity.BusinessObjectId)
            .HasConstraintName("FK_ProjectMember_Project")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UserId)
            .HasConstraintName("FK_ProjectMember_User")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_ProjectMember_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.RemovedBy)
            .HasConstraintName("FK_ProjectMember_RemovedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProjectMilestoneConfiguration : IEntityTypeConfiguration<ProjectMilestoneEntity>
{
    public void Configure(EntityTypeBuilder<ProjectMilestoneEntity> builder)
    {
        builder.ToTable("ProjectMilestone", "PMO", table =>
            table.HasCheckConstraint(
                "CK_ProjectMilestone_Status",
                "[Status] IN ('Planned','InProgress','Completed','AtRisk','Cancelled')"));
        builder.HasKey(entity => entity.Id).HasName("PK_ProjectMilestone");
        builder.Property(entity => entity.Title).HasMaxLength(255).IsRequired();
        builder.HasIndex(entity => new { entity.ProjectId, entity.PlannedDate, entity.Id })
            .HasDatabaseName("IX_ProjectMilestone_Project_Date");
        builder.HasIndex(entity => new { entity.OwnerUserId, entity.Status })
            .HasDatabaseName("IX_ProjectMilestone_Owner_Status");
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.ProjectId)
            .HasConstraintName("FK_ProjectMilestone_Project")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.OwnerUserId)
            .HasConstraintName("FK_ProjectMilestone_Owner")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_ProjectMilestone_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProjectRelationConfiguration : IEntityTypeConfiguration<ProjectRelationEntity>
{
    public void Configure(EntityTypeBuilder<ProjectRelationEntity> builder)
    {
        builder.ToTable("ProjectRelation", "PMO");
        builder.HasKey(entity => entity.Id).HasName("PK_ProjectRelation");
        builder.Property(entity => entity.EntityType).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.RelationCode).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.EvidenceExcerpt).HasMaxLength(4000);
        builder.HasIndex(entity => new { entity.ProjectId, entity.EntityType, entity.EntityId, entity.RelationCode })
            .IsUnique()
            .HasDatabaseName("UX_ProjectRelation_Project_Entity_Code");
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId })
            .HasDatabaseName("IX_ProjectRelation_Entity");
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.ProjectId)
            .HasConstraintName("FK_ProjectRelation_Project")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_ProjectRelation_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTaskEntity>
{
    public void Configure(EntityTypeBuilder<ProjectTaskEntity> builder)
    {
        builder.ToTable("ProjectTask", "PMO", table =>
        {
            table.HasCheckConstraint(
                "CK_ProjectTask_Status",
                "[Status] IN ('Pending','InProgress','Completed','Cancelled')");
            table.HasCheckConstraint(
                "CK_ProjectTask_Priority",
                "[Priority] IN ('P0','P1','P2')");
            table.HasCheckConstraint(
                "CK_ProjectTask_Environment",
                "[EnvironmentCode] IN ('demo','test','prod')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_ProjectTask");
        builder.Property(entity => entity.Title).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.Content).HasMaxLength(4000).IsRequired();
        builder.Property(entity => entity.Priority).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.HasIndex(entity => new { entity.ProjectId, entity.Status, entity.DueAt, entity.Id })
            .HasDatabaseName("IX_ProjectTask_Project_Status_Due");
        builder.HasIndex(entity => new { entity.AssigneeId, entity.Status, entity.DueAt })
            .HasDatabaseName("IX_ProjectTask_Assignee_Status_Due");
        builder.HasIndex(entity => entity.EventInboxId)
            .IsUnique()
            .HasFilter("[EventInboxId] IS NOT NULL")
            .HasDatabaseName("UX_ProjectTask_EventInbox");
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.ProjectId)
            .HasConstraintName("FK_ProjectTask_Project")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<EventInboxEntity>().WithMany().HasForeignKey(entity => entity.EventInboxId)
            .HasConstraintName("FK_ProjectTask_EventInbox")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.AssigneeId)
            .HasConstraintName("FK_ProjectTask_Assignee")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_ProjectTask_Department")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.BusinessObjectId)
            .HasConstraintName("FK_ProjectTask_BusinessObject")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowInstanceEntity>().WithMany().HasForeignKey(entity => entity.WorkflowInstanceId)
            .HasConstraintName("FK_ProjectTask_WorkflowInstance")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowTaskEntity>().WithMany().HasForeignKey(entity => entity.WorkflowTaskId)
            .HasConstraintName("FK_ProjectTask_WorkflowTask")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_ProjectTask_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventInboxConfiguration : IEntityTypeConfiguration<EventInboxEntity>
{
    public void Configure(EntityTypeBuilder<EventInboxEntity> builder)
    {
        builder.ToTable("EventInbox", "PMO", table =>
        {
            table.HasCheckConstraint(
                "CK_EventInbox_Status",
                "[Status] IN ('Received','AwaitingConfirmation','Confirmed','Rejected')");
            table.HasCheckConstraint(
                "CK_EventInbox_Source",
                "[SourceType] IN ('ManualText','ManualUpload','MinutesSource','MinutesActionItem','WorkflowNotification','Attachment')");
            table.HasCheckConstraint(
                "CK_EventInbox_Environment",
                "[EnvironmentCode] IN ('demo','test','prod')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_EventInbox");
        builder.Property(entity => entity.SourceType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.RawText).HasMaxLength(2_000_000).IsRequired();
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.HasIndex(entity => new { entity.DepartmentId, entity.EnvironmentCode, entity.Status, entity.CreatedAt })
            .HasDatabaseName("IX_EventInbox_Department_Environment_Status");
        builder.HasIndex(entity => new { entity.SubmittedBy, entity.CreatedAt })
            .HasDatabaseName("IX_EventInbox_Submitter_CreatedAt");
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId)
            .HasConstraintName("FK_EventInbox_Organization")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_EventInbox_Department")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AttachmentEntity>().WithMany().HasForeignKey(entity => entity.AttachmentId)
            .HasConstraintName("FK_EventInbox_Attachment")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.SubmittedBy)
            .HasConstraintName("FK_EventInbox_SubmittedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgentRunEntity>().WithMany().HasForeignKey(entity => entity.AnalysisRunId)
            .HasConstraintName("FK_EventInbox_AnalysisRun")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class EventSuggestionConfiguration : IEntityTypeConfiguration<EventSuggestionEntity>
{
    public void Configure(EntityTypeBuilder<EventSuggestionEntity> builder)
    {
        builder.ToTable("EventSuggestion", "PMO", table =>
        {
            table.HasCheckConstraint(
                "CK_EventSuggestion_Status",
                "[Status] IN ('Suggested','HighRisk','Confirmed','Rejected')");
            table.HasCheckConstraint(
                "CK_EventSuggestion_Risk",
                "[RiskLevel] IN ('Low','Medium','High')");
            table.HasCheckConstraint(
                "CK_EventSuggestion_Priority",
                "[Priority] IN ('P0','P1','P2')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_EventSuggestion");
        builder.Property(entity => entity.RiskLevel).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Priority).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.BoundaryReason).HasMaxLength(500);
        builder.Property(entity => entity.MissingInformation).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.EvidenceExcerpt).HasMaxLength(4000).IsRequired();
        builder.HasIndex(entity => entity.EventInboxId).IsUnique()
            .HasDatabaseName("UX_EventSuggestion_EventInbox");
        builder.HasOne<EventInboxEntity>().WithMany().HasForeignKey(entity => entity.EventInboxId)
            .HasConstraintName("FK_EventSuggestion_EventInbox")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgentRunEntity>().WithMany().HasForeignKey(entity => entity.AgentRunId)
            .HasConstraintName("FK_EventSuggestion_AgentRun")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.SuggestedProjectId)
            .HasConstraintName("FK_EventSuggestion_Project")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.SuggestedBusinessObjectId)
            .HasConstraintName("FK_EventSuggestion_BusinessObject")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.SuggestedAssigneeId)
            .HasConstraintName("FK_EventSuggestion_Assignee")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowVersionEntity>().WithMany().HasForeignKey(entity => entity.SuggestedWorkflowVersionId)
            .HasConstraintName("FK_EventSuggestion_WorkflowVersion")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
