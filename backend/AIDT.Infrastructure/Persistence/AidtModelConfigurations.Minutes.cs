using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class MinutesSourceConfiguration : IEntityTypeConfiguration<MinutesSourceEntity>
{
    public void Configure(EntityTypeBuilder<MinutesSourceEntity> builder)
    {
        builder.ToTable("MinutesSource", "Minutes", table =>
        {
            table.HasCheckConstraint("CK_MinutesSource_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
            table.HasCheckConstraint("CK_MinutesSource_Type", "[SourceType] IN ('ManualText','ManualUpload')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_MinutesSource");
        builder.Property(entity => entity.Title).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.SourceType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Transcript).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.AnalysisVersion).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => new { entity.DepartmentId, entity.EnvironmentCode, entity.CreatedAt, entity.Id }).HasDatabaseName("IX_MinutesSource_Department_Environment");
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId).HasConstraintName("FK_MinutesSource_Organization").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_MinutesSource_Department").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AttachmentEntity>().WithMany().HasForeignKey(entity => entity.AttachmentId).HasConstraintName("FK_MinutesSource_Attachment").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgentRunEntity>().WithMany().HasForeignKey(entity => entity.AnalysisRunId).HasConstraintName("FK_MinutesSource_AnalysisRun").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MinutesActionItemConfiguration : IEntityTypeConfiguration<MinutesActionItemEntity>
{
    public void Configure(EntityTypeBuilder<MinutesActionItemEntity> builder)
    {
        builder.ToTable("MinutesActionItem", "Minutes", table =>
        {
            table.HasCheckConstraint("CK_MinutesActionItem_Status", "[Status] IN ('Draft','Confirmed')");
            table.HasCheckConstraint("CK_MinutesActionItem_Confidence", "[Confidence] >= 0 AND [Confidence] <= 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_MinutesActionItem");
        builder.Property(entity => entity.Speaker).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.SourceTimestamp).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.SourceExcerpt).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.OriginalContent).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.Content).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.Priority).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.MissingInformation).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Confidence).HasColumnType("decimal(5,4)");
        builder.Property(entity => entity.AnalysisVersion).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.MinutesSourceId, entity.Status }).HasDatabaseName("IX_MinutesActionItem_Source_Status");
        builder.HasOne<MinutesSourceEntity>().WithMany().HasForeignKey(entity => entity.MinutesSourceId).HasConstraintName("FK_MinutesActionItem_Source").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.AssigneeId).HasConstraintName("FK_MinutesActionItem_Assignee").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ObjectTypeEntity>().WithMany().HasForeignKey(entity => entity.ObjectTypeId).HasConstraintName("FK_MinutesActionItem_ObjectType").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowVersionEntity>().WithMany().HasForeignKey(entity => entity.WorkflowVersionId).HasConstraintName("FK_MinutesActionItem_WorkflowVersion").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.ProjectId).HasConstraintName("FK_MinutesActionItem_Project").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.BusinessObjectId).HasConstraintName("FK_MinutesActionItem_BusinessObject").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowInstanceEntity>().WithMany().HasForeignKey(entity => entity.WorkflowInstanceId).HasConstraintName("FK_MinutesActionItem_WorkflowInstance").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class MinutesTaskConfiguration : IEntityTypeConfiguration<MinutesTaskEntity>
{
    public void Configure(EntityTypeBuilder<MinutesTaskEntity> builder)
    {
        builder.ToTable("MinutesTask", "Minutes", table =>
        {
            table.HasCheckConstraint("CK_MinutesTask_Status", "[Status] IN ('Pending','Completed','Cancelled')");
            table.HasCheckConstraint("CK_MinutesTask_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_MinutesTask");
        builder.Property(entity => entity.Title).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.Content).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.Priority).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.ActionItemId).IsUnique().HasDatabaseName("UX_MinutesTask_ActionItem");
        builder.HasIndex(entity => new { entity.AssigneeId, entity.Status, entity.DueAt }).HasDatabaseName("IX_MinutesTask_Assignee_Status_Due");
        builder.HasOne<MinutesSourceEntity>().WithMany().HasForeignKey(entity => entity.MinutesSourceId).HasConstraintName("FK_MinutesTask_Source").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MinutesActionItemEntity>().WithMany().HasForeignKey(entity => entity.ActionItemId).HasConstraintName("FK_MinutesTask_ActionItem").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AttachmentEntity>().WithMany().HasForeignKey(entity => entity.AttachmentId).HasConstraintName("FK_MinutesTask_Attachment").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.AssigneeId).HasConstraintName("FK_MinutesTask_Assignee").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.BusinessObjectId).HasConstraintName("FK_MinutesTask_BusinessObject").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowInstanceEntity>().WithMany().HasForeignKey(entity => entity.WorkflowInstanceId).HasConstraintName("FK_MinutesTask_WorkflowInstance").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.ProjectId).HasConstraintName("FK_MinutesTask_Project").OnDelete(DeleteBehavior.Restrict);
    }
}
