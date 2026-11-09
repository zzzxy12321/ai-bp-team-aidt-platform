using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<OrganizationEntity>
{
    public void Configure(EntityTypeBuilder<OrganizationEntity> builder)
    {
        builder.ToTable("Organization", "Identity", table => table.HasCheckConstraint("CK_Organization_Code", "[Code] <> ''"));
        builder.HasKey(entity => entity.Id).HasName("PK_Organization");
        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName("UX_Organization_Code");
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("User", "Identity");
        builder.HasKey(entity => entity.Id).HasName("PK_User");
        builder.Property(entity => entity.UserName).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.InvitationStatus).HasMaxLength(32).IsRequired().HasDefaultValue("Active");
        builder.Property(entity => entity.Version).HasDefaultValue(1L);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2(7)").HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.UserName }).IsUnique().HasDatabaseName("UX_User_Organization_UserName");
        builder.HasIndex(entity => entity.OrganizationId).HasDatabaseName("IX_User_OrganizationId");
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId).HasConstraintName("FK_User_Organization");
    }
}

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<DepartmentEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
    {
        builder.ToTable("Department", "Identity", table =>
        {
            table.HasCheckConstraint("CK_Department_Code", "LEN([Code]) > 0");
            table.HasCheckConstraint("CK_Department_Name", "LEN([Name]) > 0");
            table.HasCheckConstraint(
                "CK_Department_HierarchyType",
                "[HierarchyType] IN ('Center','Department','Team')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_Department");
        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.HierarchyType).HasMaxLength(32).IsRequired().HasDefaultValue("Department");
        builder.Property(entity => entity.Path).HasMaxLength(1000).IsRequired().HasDefaultValue("/");
        builder.Property(entity => entity.Version).HasDefaultValue(1L);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetime2(3)");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Code }).IsUnique().HasDatabaseName("UX_Department_Organization_Code");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.IsActive }).HasDatabaseName("IX_Department_Organization_Active");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.ParentDepartmentId, entity.SortOrder })
            .HasDatabaseName("IX_Department_Tree");
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId).HasConstraintName("FK_Department_Organization").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.ParentDepartmentId).HasConstraintName("FK_Department_Parent").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.ManagerUserId).HasConstraintName("FK_Department_Manager").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UserDepartmentMembershipConfiguration : IEntityTypeConfiguration<UserDepartmentMembershipEntity>
{
    public void Configure(EntityTypeBuilder<UserDepartmentMembershipEntity> builder)
    {
        builder.ToTable("UserDepartmentMembership", "Identity", table =>
            table.HasCheckConstraint(
                "CK_UserDepartmentMembership_PrimaryActive",
                "[IsPrimary] = 0 OR [IsActive] = 1"));
        builder.HasKey(entity => new { entity.UserId, entity.DepartmentId }).HasName("PK_UserDepartmentMembership");
        builder.Property(entity => entity.JoinedAt).HasColumnType("datetime2(3)");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.IsActive }).HasDatabaseName("IX_UserDepartmentMembership_Department_Active");
        builder.HasIndex(entity => new { entity.UserId, entity.IsPrimary }).HasDatabaseName("IX_UserDepartmentMembership_User_Primary");
        builder.HasIndex(entity => entity.UserId)
            .IsUnique()
            .HasFilter("[IsPrimary] = 1 AND [IsActive] = 1")
            .HasDatabaseName("UX_UserDepartmentMembership_User_ActivePrimary");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UserId).HasConstraintName("FK_UserDepartmentMembership_User").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_UserDepartmentMembership_Department").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("Role", "Identity", table =>
            table.HasCheckConstraint(
                "CK_Role_Type",
                "[RoleType] IN ('System','Business','AgentService')"));
        builder.HasKey(entity => entity.Id).HasName("PK_Role");
        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RoleType).HasMaxLength(32).IsRequired().HasDefaultValue("Business");
        builder.Property(entity => entity.Description).HasMaxLength(1000);
        builder.Property(entity => entity.IsActive).HasDefaultValue(true);
        builder.Property(entity => entity.Version).HasDefaultValue(1L);
        builder.Property(entity => entity.CreatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.Property(entity => entity.UpdatedAt)
            .HasColumnType("datetime2(3)")
            .HasDefaultValueSql("SYSUTCDATETIME()");
        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName("UX_Role_Code");
        builder.HasIndex(entity => new { entity.RoleType, entity.IsActive }).HasDatabaseName("IX_Role_Type_Active");
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<PermissionEntity>
{
    public void Configure(EntityTypeBuilder<PermissionEntity> builder)
    {
        builder.ToTable("Permission", "Identity");
        builder.HasKey(entity => entity.Id).HasName("PK_Permission");
        builder.Property(entity => entity.Code).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Resource).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Action).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName("UX_Permission_Code");
        builder.HasIndex(entity => new { entity.Resource, entity.Action }).IsUnique().HasDatabaseName("UX_Permission_Resource_Action");
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRoleEntity>
{
    public void Configure(EntityTypeBuilder<UserRoleEntity> builder)
    {
        builder.ToTable("UserRole", "Identity");
        builder.HasKey(entity => new { entity.UserId, entity.RoleId }).HasName("PK_UserRole");
        builder.HasIndex(entity => entity.RoleId).HasDatabaseName("IX_UserRole_RoleId");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UserId).HasConstraintName("FK_UserRole_User");
        builder.HasOne<RoleEntity>().WithMany().HasForeignKey(entity => entity.RoleId).HasConstraintName("FK_UserRole_Role");
    }
}

internal sealed class UserRoleAssignmentConfiguration :
    IEntityTypeConfiguration<UserRoleAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignmentEntity> builder)
    {
        builder.ToTable("UserRoleAssignment", "Identity", table =>
        {
            table.HasCheckConstraint(
                "CK_UserRoleAssignment_ScopeType",
                "[ScopeType] IN ('organization','department')");
            table.HasCheckConstraint(
                "CK_UserRoleAssignment_ScopeReference",
                "LEN([ScopeReference]) > 0");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_UserRoleAssignment");
        builder.Property(entity => entity.ScopeType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ScopeReference).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2(3)");
        builder.Property(entity => entity.RevokedAt).HasColumnType("datetime2(3)");
        builder.HasIndex(entity => new { entity.UserId, entity.RoleId, entity.ScopeType, entity.ScopeReference })
            .IsUnique()
            .HasDatabaseName("UX_UserRoleAssignment_User_Role_Scope");
        builder.HasIndex(entity => new { entity.RoleId, entity.IsActive })
            .HasDatabaseName("IX_UserRoleAssignment_Role_Active");
        builder.HasIndex(entity => new { entity.UserId, entity.IsActive })
            .HasDatabaseName("IX_UserRoleAssignment_User_Active");
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UserId)
            .HasConstraintName("FK_UserRoleAssignment_User")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoleEntity>().WithMany()
            .HasForeignKey(entity => entity.RoleId)
            .HasConstraintName("FK_UserRoleAssignment_Role")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.AssignedBy)
            .HasConstraintName("FK_UserRoleAssignment_AssignedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.RevokedBy)
            .HasConstraintName("FK_UserRoleAssignment_RevokedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AccessChangeRequestConfiguration :
    IEntityTypeConfiguration<AccessChangeRequestEntity>
{
    public void Configure(EntityTypeBuilder<AccessChangeRequestEntity> builder)
    {
        builder.ToTable("AccessChangeRequest", "Identity", table =>
        {
            table.HasCheckConstraint(
                "CK_AccessChangeRequest_Type",
                "[RequestType] IN ('RoleAssignment','RolePermissions','AgentProfilePublish','ScenarioPublish')");
            table.HasCheckConstraint(
                "CK_AccessChangeRequest_Status",
                "[Status] IN ('Pending','Approved','Rejected','Applied','Cancelled')");
            table.HasCheckConstraint(
                "CK_AccessChangeRequest_PayloadJson",
                "ISJSON([PayloadJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_AccessChangeRequest");
        builder.Property(entity => entity.RequestType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.RiskReason).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ReviewComment).HasMaxLength(1000);
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.HasIndex(entity => new { entity.OrganizationId, entity.Status, entity.RequestedAt })
            .HasDatabaseName("IX_AccessChangeRequest_Org_Status_RequestedAt");
        builder.HasIndex(entity => new { entity.RequestedBy, entity.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_AccessChangeRequest_Requester_Idempotency");
        builder.HasOne<OrganizationEntity>().WithMany()
            .HasForeignKey(entity => entity.OrganizationId)
            .HasConstraintName("FK_AccessChangeRequest_Organization")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.TargetUserId)
            .HasConstraintName("FK_AccessChangeRequest_TargetUser")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoleEntity>().WithMany()
            .HasForeignKey(entity => entity.TargetRoleId)
            .HasConstraintName("FK_AccessChangeRequest_TargetRole")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.RequestedBy)
            .HasConstraintName("FK_AccessChangeRequest_RequestedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.ReviewedBy)
            .HasConstraintName("FK_AccessChangeRequest_ReviewedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermissionEntity>
{
    public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
    {
        builder.ToTable("RolePermission", "Identity");
        builder.HasKey(entity => new { entity.RoleId, entity.PermissionId }).HasName("PK_RolePermission");
        builder.HasIndex(entity => entity.PermissionId).HasDatabaseName("IX_RolePermission_PermissionId");
        builder.HasOne<RoleEntity>().WithMany().HasForeignKey(entity => entity.RoleId).HasConstraintName("FK_RolePermission_Role");
        builder.HasOne<PermissionEntity>().WithMany().HasForeignKey(entity => entity.PermissionId).HasConstraintName("FK_RolePermission_Permission");
    }
}

internal sealed class DataScopeConfiguration : IEntityTypeConfiguration<DataScopeEntity>
{
    public void Configure(EntityTypeBuilder<DataScopeEntity> builder)
    {
        builder.ToTable("DataScope", "Identity");
        builder.HasKey(entity => entity.Id).HasName("PK_DataScope");
        builder.Property(entity => entity.ScopeType).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ScopeReference).HasMaxLength(128).IsRequired();
        builder.HasIndex(entity => new { entity.UserId, entity.ScopeType, entity.ScopeReference }).IsUnique().HasDatabaseName("UX_DataScope_User_Type_Reference");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UserId).HasConstraintName("FK_DataScope_User");
    }
}

internal sealed class ObjectTypeConfiguration : IEntityTypeConfiguration<ObjectTypeEntity>
{
    public void Configure(EntityTypeBuilder<ObjectTypeEntity> builder)
    {
        builder.ToTable("ObjectType", "Metadata", table => table.HasCheckConstraint(
            "CK_ObjectType_Status",
            "[Status] IN ('Draft','Validated','Published','Retired','Superseded')"));
        builder.HasKey(entity => entity.Id).HasName("PK_ObjectType");
        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2(7)");
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetime2(7)");
        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName("UX_ObjectType_Code");
        builder.HasIndex(entity => new { entity.Status, entity.UpdatedAt })
            .HasDatabaseName("IX_ObjectType_Status_UpdatedAt");
    }
}

internal sealed class FieldDefinitionConfiguration : IEntityTypeConfiguration<FieldDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<FieldDefinitionEntity> builder)
    {
        builder.ToTable("FieldDefinition", "Metadata");
        builder.HasKey(entity => entity.Id).HasName("PK_FieldDefinition");
        builder.Property(entity => entity.Key).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.DisplayName).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.DataType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Sensitivity).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.DefaultValue).HasMaxLength(4000);
        builder.HasIndex(entity => new { entity.ObjectTypeId, entity.Key }).IsUnique().HasDatabaseName("UX_FieldDefinition_ObjectType_Key");
        builder.HasOne<ObjectTypeEntity>().WithMany().HasForeignKey(entity => entity.ObjectTypeId).HasConstraintName("FK_FieldDefinition_ObjectType").OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ObjectTypeVersionConfiguration :
    IEntityTypeConfiguration<ObjectTypeVersionEntity>
{
    public void Configure(EntityTypeBuilder<ObjectTypeVersionEntity> builder)
    {
        builder.ToTable("ObjectTypeVersion", "Metadata", table =>
        {
            table.HasCheckConstraint(
                "CK_ObjectTypeVersion_Status",
                "[Status] IN ('Draft','Validated','Published','Retired','Superseded')");
            table.HasCheckConstraint(
                "CK_ObjectTypeVersion_FieldsJson",
                "ISJSON([FieldsJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_ObjectTypeVersion");
        builder.Property(entity => entity.Id).ValueGeneratedOnAdd();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.FieldsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(entity => new { entity.ObjectTypeId, entity.Version })
            .IsUnique()
            .HasDatabaseName("UX_ObjectTypeVersion_Type_Version");
        builder.HasIndex(entity => new { entity.ObjectTypeId, entity.Status, entity.Version })
            .HasDatabaseName("IX_ObjectTypeVersion_Type_Status_Version");
        builder.HasOne<ObjectTypeEntity>().WithMany()
            .HasForeignKey(entity => entity.ObjectTypeId)
            .HasConstraintName("FK_ObjectTypeVersion_ObjectType")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ObjectRelationConfiguration : IEntityTypeConfiguration<ObjectRelationEntity>
{
    public void Configure(EntityTypeBuilder<ObjectRelationEntity> builder)
    {
        builder.ToTable("ObjectRelation", "Metadata");
        builder.HasKey(entity => entity.Id).HasName("PK_ObjectRelation");
        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => new { entity.ObjectTypeId, entity.TargetObjectTypeId, entity.Code }).IsUnique().HasDatabaseName("UX_ObjectRelation_Type_Target_Code");
        builder.HasOne<ObjectTypeEntity>().WithMany().HasForeignKey(entity => entity.ObjectTypeId).HasConstraintName("FK_ObjectRelation_SourceType").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ObjectTypeEntity>().WithMany().HasForeignKey(entity => entity.TargetObjectTypeId).HasConstraintName("FK_ObjectRelation_TargetType").OnDelete(DeleteBehavior.Restrict);
    }
}
