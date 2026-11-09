using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class WorkbenchTemplateConfiguration :
    IEntityTypeConfiguration<WorkbenchTemplateEntity>
{
    public void Configure(EntityTypeBuilder<WorkbenchTemplateEntity> builder)
    {
        builder.ToTable("WorkbenchTemplate", "Workspace", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkbenchTemplate_Status",
                "[Status] IN ('Draft','Published','Retired')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_WorkbenchTemplate");
        builder.Property(entity => entity.Code).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.AudienceTitle).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.PrimaryQuestion).HasMaxLength(300).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Code })
            .IsUnique()
            .HasDatabaseName("UX_WorkbenchTemplate_Organization_Code");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.DepartmentId, entity.Status, entity.SortOrder })
            .HasDatabaseName("IX_WorkbenchTemplate_Org_Department_Status_Sort");
        builder.HasOne<OrganizationEntity>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .HasConstraintName("FK_WorkbenchTemplate_Organization")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_WorkbenchTemplate_Department")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_WorkbenchTemplate_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_WorkbenchTemplate_UpdatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.RetiredBy)
            .HasConstraintName("FK_WorkbenchTemplate_RetiredBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkbenchTemplateVersionConfiguration :
    IEntityTypeConfiguration<WorkbenchTemplateVersionEntity>
{
    public void Configure(EntityTypeBuilder<WorkbenchTemplateVersionEntity> builder)
    {
        builder.ToTable("WorkbenchTemplateVersion", "Workspace", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkbenchTemplateVersion_Status",
                "[Status] IN ('Draft','Published','Retired')");
            table.HasCheckConstraint(
                "CK_WorkbenchTemplateVersion_Validation",
                "[ValidationJson] IS NULL OR ISJSON([ValidationJson]) = 1");
            table.HasCheckConstraint(
                "CK_WorkbenchTemplateVersion_Impact",
                "[ImpactAnalysisJson] IS NULL OR ISJSON([ImpactAnalysisJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_WorkbenchTemplateVersion");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ValidationJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.ImpactAnalysisJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(entity => new { entity.TemplateId, entity.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_WorkbenchTemplateVersion_Template_Version");
        builder.HasIndex(entity => new { entity.TemplateId, entity.Status, entity.Revision })
            .HasDatabaseName("IX_WorkbenchTemplateVersion_Template_Status");
        builder.HasOne<WorkbenchTemplateEntity>().WithMany()
            .HasForeignKey(entity => entity.TemplateId)
            .HasConstraintName("FK_WorkbenchTemplateVersion_Template")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_WorkbenchTemplateVersion_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_WorkbenchTemplateVersion_UpdatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.PublishedBy)
            .HasConstraintName("FK_WorkbenchTemplateVersion_PublishedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkbenchCardConfiguration : IEntityTypeConfiguration<WorkbenchCardEntity>
{
    public void Configure(EntityTypeBuilder<WorkbenchCardEntity> builder)
    {
        builder.ToTable("WorkbenchCard", "Workspace", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkbenchCard_Width",
                "[Width] IN ('Third','Half','Full')");
            table.HasCheckConstraint(
                "CK_WorkbenchCard_CapabilityState",
                "[CapabilityState] IN ('Connected','NotConnected')");
            table.HasCheckConstraint(
                "CK_WorkbenchCard_FilterJson",
                "[FilterJson] IS NOT NULL AND ISJSON([FilterJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_WorkbenchCard");
        builder.Property(entity => entity.CardType).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(500);
        builder.Property(entity => entity.Width).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.DataSourceType).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.FilterJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.RequiredResource).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RequiredAction).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.CapabilityState).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.TemplateVersionId, entity.SortOrder })
            .HasDatabaseName("IX_WorkbenchCard_Version_Sort");
        builder.HasOne<WorkbenchTemplateVersionEntity>().WithMany()
            .HasForeignKey(entity => entity.TemplateVersionId)
            .HasConstraintName("FK_WorkbenchCard_Version")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkbenchAssignmentConfiguration :
    IEntityTypeConfiguration<WorkbenchAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<WorkbenchAssignmentEntity> builder)
    {
        builder.ToTable("WorkbenchAssignment", "Workspace", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkbenchAssignment_Type",
                "[AssignmentType] IN ('User','Role','Department','Organization')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_WorkbenchAssignment");
        builder.Property(entity => entity.AssignmentType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ScopeValue).HasMaxLength(128).IsRequired();
        builder.HasIndex(entity => new { entity.TemplateVersionId, entity.AssignmentType, entity.ScopeValue })
            .IsUnique()
            .HasDatabaseName("UX_WorkbenchAssignment_Version_Type_Scope");
        builder.HasOne<WorkbenchTemplateVersionEntity>().WithMany()
            .HasForeignKey(entity => entity.TemplateVersionId)
            .HasConstraintName("FK_WorkbenchAssignment_Version")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
