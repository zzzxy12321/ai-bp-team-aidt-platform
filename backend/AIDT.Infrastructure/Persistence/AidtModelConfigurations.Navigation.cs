using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class NavigationWorkspaceConfiguration :
    IEntityTypeConfiguration<NavigationWorkspaceEntity>
{
    public void Configure(EntityTypeBuilder<NavigationWorkspaceEntity> builder)
    {
        builder.ToTable("NavigationWorkspace", "Navigation", table =>
        {
            table.HasCheckConstraint(
                "CK_NavigationWorkspace_Type",
                "[WorkspaceType] IN ('System','OrganizationWorkspace','DepartmentWorkspace','BusinessView')");
            table.HasCheckConstraint(
                "CK_NavigationWorkspace_Status",
                "[Status] IN ('Draft','Published','Retired')");
            table.HasCheckConstraint(
                "CK_NavigationWorkspace_Environment",
                "[EnvironmentCode] IN ('demo','test','prod')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_NavigationWorkspace");
        builder.Property(entity => entity.WorkspaceKey).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.WorkspaceType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired().HasDefaultValue("demo");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.WorkspaceKey })
            .IsUnique()
            .HasDatabaseName("UX_NavigationWorkspace_Organization_Key");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.DepartmentId, entity.Status, entity.Version })
            .HasDatabaseName("IX_NavigationWorkspace_Org_Department_Status");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.IsTestData, entity.Status })
            .HasDatabaseName("IX_NavigationWorkspace_Org_Test_Status");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.DepartmentId })
            .IsUnique()
            .HasFilter("[IsDefault] = 1 AND [IsTestData] = 0 AND [Status] = 'Published' AND [DepartmentId] IS NOT NULL")
            .HasDatabaseName("UX_NavigationWorkspace_ActiveDepartmentDefault");
        builder.HasOne<OrganizationEntity>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .HasConstraintName("FK_NavigationWorkspace_Organization")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_NavigationWorkspace_Department")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_NavigationWorkspace_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_NavigationWorkspace_UpdatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.RetiredBy)
            .HasConstraintName("FK_NavigationWorkspace_RetiredBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class NavigationVersionConfiguration :
    IEntityTypeConfiguration<NavigationVersionEntity>
{
    public void Configure(EntityTypeBuilder<NavigationVersionEntity> builder)
    {
        builder.ToTable("NavigationVersion", "Navigation", table =>
        {
            table.HasCheckConstraint(
                "CK_NavigationVersion_Status",
                "[Status] IN ('Draft','Published','Retired')");
            table.HasCheckConstraint(
                "CK_NavigationVersion_ImpactAnalysis",
                "[ImpactAnalysisJson] IS NULL OR ISJSON([ImpactAnalysisJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_NavigationVersion");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ImpactAnalysisJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(entity => new { entity.WorkspaceId, entity.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_NavigationVersion_Workspace_Version");
        builder.HasIndex(entity => new { entity.WorkspaceId, entity.Status, entity.Revision })
            .HasDatabaseName("IX_NavigationVersion_Workspace_Status");
        builder.HasOne<NavigationWorkspaceEntity>().WithMany()
            .HasForeignKey(entity => entity.WorkspaceId)
            .HasConstraintName("FK_NavigationVersion_Workspace")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_NavigationVersion_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_NavigationVersion_UpdatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.PublishedBy)
            .HasConstraintName("FK_NavigationVersion_PublishedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class NavigationItemConfiguration :
    IEntityTypeConfiguration<NavigationItemEntity>
{
    public void Configure(EntityTypeBuilder<NavigationItemEntity> builder)
    {
        builder.ToTable("NavigationItem", "Navigation", table =>
        {
            table.HasCheckConstraint(
                "CK_NavigationItem_Type",
                "[ItemType] IN ('System','Workspace','Task','Inbox','Workflow','BusinessObject','File','Project','Minutes','Report','Administration')");
            table.HasCheckConstraint(
                "CK_NavigationItem_Route",
                "LEN([Route]) > 0");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_NavigationItem");
        builder.Property(entity => entity.ItemKey).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.ItemType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Route).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.RequiredResource).HasMaxLength(128);
        builder.Property(entity => entity.RequiredAction).HasMaxLength(64);
        builder.Property(entity => entity.Icon).HasMaxLength(32);
        builder.HasIndex(entity => new { entity.NavigationVersionId, entity.ItemKey })
            .IsUnique()
            .HasDatabaseName("UX_NavigationItem_Version_Key");
        builder.HasIndex(entity => new { entity.NavigationVersionId, entity.SortOrder })
            .HasDatabaseName("IX_NavigationItem_Version_Sort");
        builder.HasOne<NavigationVersionEntity>().WithMany()
            .HasForeignKey(entity => entity.NavigationVersionId)
            .HasConstraintName("FK_NavigationItem_Version")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class NavigationUserPreferenceConfiguration :
    IEntityTypeConfiguration<NavigationUserPreferenceEntity>
{
    public void Configure(EntityTypeBuilder<NavigationUserPreferenceEntity> builder)
    {
        builder.ToTable("NavigationUserPreference", "Navigation");
        builder.HasKey(entity => entity.Id).HasName("PK_NavigationUserPreference");
        builder.Property(entity => entity.WorkspaceKey).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.ItemKey).HasMaxLength(80).IsRequired();
        builder.HasIndex(entity => new { entity.UserId, entity.WorkspaceKey, entity.ItemKey })
            .IsUnique()
            .HasDatabaseName("UX_NavigationUserPreference_User_Workspace_Item");
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UserId)
            .HasConstraintName("FK_NavigationUserPreference_User")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
