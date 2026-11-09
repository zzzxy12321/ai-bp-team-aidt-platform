using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class BusinessObjectConfiguration : IEntityTypeConfiguration<BusinessObjectEntity>
{
    public void Configure(EntityTypeBuilder<BusinessObjectEntity> builder)
    {
        builder.ToTable("BusinessObject", "BusinessObject", table =>
        {
            table.HasCheckConstraint("CK_BusinessObject_FieldsJson", "ISJSON([FieldsJson]) = 1");
            table.HasCheckConstraint("CK_BusinessObject_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_BusinessObject");
        builder.Property(entity => entity.ObjectCode).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(255);
        builder.Property(entity => entity.CreatedByUserId);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.SourceSystem).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.FieldsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2(7)");
        builder.Property(entity => entity.UpdatedAt).HasColumnType("datetime2(7)");
        builder.HasIndex(entity => new { entity.ObjectTypeId, entity.ObjectCode }).IsUnique().HasDatabaseName("UX_BusinessObject_Type_Code");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.UpdatedAt, entity.Id }).HasDatabaseName("IX_BusinessObject_Organization_UpdatedAt_Id");
        builder.HasIndex(entity => new { entity.Status, entity.UpdatedAt, entity.Id }).HasDatabaseName("IX_BusinessObject_Status_UpdatedAt_Id");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.EnvironmentCode, entity.UpdatedAt, entity.Id }).HasDatabaseName("IX_BusinessObject_Department_Environment_UpdatedAt");
        builder.HasOne<ObjectTypeEntity>().WithMany().HasForeignKey(entity => entity.ObjectTypeId).HasConstraintName("FK_BusinessObject_ObjectType").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId).HasConstraintName("FK_BusinessObject_Organization").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_BusinessObject_Department").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedByUserId).HasConstraintName("FK_BusinessObject_CreatedByUser").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BusinessObjectVersionConfiguration : IEntityTypeConfiguration<BusinessObjectVersionEntity>
{
    public void Configure(EntityTypeBuilder<BusinessObjectVersionEntity> builder)
    {
        builder.ToTable("BusinessObjectVersion", "BusinessObject", table =>
            table.HasCheckConstraint("CK_BusinessObjectVersion_SnapshotJson", "ISJSON([SnapshotJson]) = 1"));
        builder.HasKey(entity => entity.Id).HasName("PK_BusinessObjectVersion");
        builder.Property(entity => entity.Id).ValueGeneratedOnAdd();
        builder.Property(entity => entity.SnapshotJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(1000);
        builder.Property(entity => entity.CreatedAt).HasColumnType("datetime2(7)");
        builder.HasIndex(entity => new { entity.BusinessObjectId, entity.Version }).IsUnique().HasDatabaseName("UX_BusinessObjectVersion_Object_Version");
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.BusinessObjectId).HasConstraintName("FK_BusinessObjectVersion_BusinessObject").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class BusinessObjectRelationConfiguration : IEntityTypeConfiguration<BusinessObjectRelationEntity>
{
    public void Configure(EntityTypeBuilder<BusinessObjectRelationEntity> builder)
    {
        builder.ToTable("BusinessObjectRelation", "BusinessObject");
        builder.HasKey(entity => entity.Id).HasName("PK_BusinessObjectRelation");
        builder.Property(entity => entity.RelationCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => new { entity.SourceObjectId, entity.RelationCode, entity.TargetObjectId }).IsUnique().HasDatabaseName("UX_BusinessObjectRelation_Source_Code_Target");
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.SourceObjectId).HasConstraintName("FK_BusinessObjectRelation_Source").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.TargetObjectId).HasConstraintName("FK_BusinessObjectRelation_Target").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinitionEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinitionEntity> builder)
    {
        builder.ToTable("WorkflowDefinition", "Workflow", table =>
        {
            table.HasCheckConstraint(
                "CK_WorkflowDefinition_RiskLevel",
                "[RiskLevel] IN ('Low','Medium','High')");
            table.HasCheckConstraint(
                "CK_WorkflowDefinition_Environment",
                "[EnvironmentCode] IN ('demo','test','prod')");
            table.HasCheckConstraint(
                "CK_WorkflowDefinition_ApplicableRolesJson",
                "ISJSON([ApplicableRoleCodesJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_WorkflowDefinition");
        builder.Property(entity => entity.Code).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ObjectTypeCode).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.ApplicableRoleCodesJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasDefaultValue("[]");
        builder.Property(entity => entity.RiskLevel).HasMaxLength(16).IsRequired().HasDefaultValue("Low");
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired().HasDefaultValue("demo");
        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName("UX_WorkflowDefinition_Code");
        builder.HasIndex(entity => new
        {
            entity.DepartmentId,
            entity.ObjectTypeCode,
            entity.IsDefaultForDepartment,
            entity.RiskLevel,
            entity.IsTestData
        })
            .HasDatabaseName("IX_WorkflowDefinition_Department_Object_Default_Risk");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.ObjectTypeCode })
            .IsUnique()
            .HasFilter("[IsDefaultForDepartment] = 1 AND [IsTestData] = 0 AND [Status] = 'Published'")
            .HasDatabaseName("UX_WorkflowDefinition_ActiveDepartmentDefault");
        builder.HasOne<DepartmentEntity>().WithMany()
            .HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_WorkflowDefinition_Department")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_WorkflowDefinition_CreatedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_WorkflowDefinition_UpdatedBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkflowVersionConfiguration : IEntityTypeConfiguration<WorkflowVersionEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowVersionEntity> builder)
    {
        builder.ToTable("WorkflowVersion", "Workflow", table =>
            table.HasCheckConstraint("CK_WorkflowVersion_DefinitionJson", "ISJSON([DefinitionJson]) = 1"));
        builder.HasKey(entity => entity.Id).HasName("PK_WorkflowVersion");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.DefinitionJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(entity => new { entity.WorkflowDefinitionId, entity.VersionNumber }).IsUnique().HasDatabaseName("UX_WorkflowVersion_Definition_Version");
        builder.HasOne<WorkflowDefinitionEntity>().WithMany().HasForeignKey(entity => entity.WorkflowDefinitionId).HasConstraintName("FK_WorkflowVersion_Definition").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstanceEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowInstanceEntity> builder)
    {
        builder.ToTable("WorkflowInstance", "Workflow", table =>
            table.HasCheckConstraint("CK_WorkflowInstance_Environment", "[EnvironmentCode] IN ('demo','test','prod')"));
        builder.HasKey(entity => entity.Id).HasName("PK_WorkflowInstance");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.CurrentNodeCode).HasMaxLength(80);
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.SourceSystem).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => new { entity.BusinessObjectId, entity.StartedAt }).HasDatabaseName("IX_WorkflowInstance_Object_StartedAt");
        builder.HasIndex(entity => new { entity.Status, entity.StartedAt, entity.Id }).HasDatabaseName("IX_WorkflowInstance_Status_StartedAt_Id");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.EnvironmentCode, entity.StartedAt, entity.Id }).HasDatabaseName("IX_WorkflowInstance_Department_Environment_StartedAt");
        builder.HasOne<WorkflowDefinitionEntity>().WithMany().HasForeignKey(entity => entity.WorkflowDefinitionId).HasConstraintName("FK_WorkflowInstance_Definition").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowVersionEntity>().WithMany().HasForeignKey(entity => entity.WorkflowVersionId).HasConstraintName("FK_WorkflowInstance_Version").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BusinessObjectEntity>().WithMany().HasForeignKey(entity => entity.BusinessObjectId).HasConstraintName("FK_WorkflowInstance_BusinessObject").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_WorkflowInstance_Department").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTaskEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskEntity> builder)
    {
        builder.ToTable("WorkflowTask", "Workflow", table =>
        {
            table.HasCheckConstraint("CK_WorkflowTask_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
            table.HasCheckConstraint("CK_WorkflowTask_FormDefinitionJson", "ISJSON([FormDefinitionJson]) = 1");
            table.HasCheckConstraint("CK_WorkflowTask_FormDataJson", "ISJSON([FormDataJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_WorkflowTask");
        builder.Property(entity => entity.NodeCode).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.NodeName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.SlaStatus).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.DecisionMode).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.AssigneeStrategy).HasMaxLength(32).IsRequired().HasDefaultValue("SpecificUser");
        builder.Property(entity => entity.BusinessRoleCode).HasMaxLength(64);
        builder.Property(entity => entity.CandidateUserIdsJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("[]");
        builder.Property(entity => entity.AssigneeSnapshotJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("{}");
        builder.Property(entity => entity.FormDefinitionJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("[]");
        builder.Property(entity => entity.FormDataJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("{}");
        builder.Property(entity => entity.FormSchemaVersion).HasMaxLength(32).IsRequired().HasDefaultValue("1.0");
        builder.Property(entity => entity.FormDefinitionHash).HasMaxLength(64).IsRequired().HasDefaultValue("");
        builder.Property(entity => entity.TransferReason).HasMaxLength(500);
        builder.Property(entity => entity.RowVersion).IsRowVersion();
        builder.Property(entity => entity.DecisionComment).HasMaxLength(2000);
        builder.HasIndex(entity => new { entity.AssigneeId, entity.Status, entity.CreatedAt }).HasDatabaseName("IX_WorkflowTask_Assignee_Status_CreatedAt");
        builder.HasIndex(entity => new { entity.WorkflowInstanceId, entity.CreatedAt }).HasDatabaseName("IX_WorkflowTask_Instance_CreatedAt");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.EnvironmentCode, entity.Status, entity.CreatedAt }).HasDatabaseName("IX_WorkflowTask_Department_Environment_Status_CreatedAt");
        builder.HasIndex(entity => new { entity.Status, entity.DueAt, entity.SlaStatus }).HasDatabaseName("IX_WorkflowTask_Sla");
        builder.HasIndex(entity => new { entity.WorkflowInstanceId, entity.NodeCode, entity.CreatedAt })
            .HasDatabaseName("IX_WorkflowTask_Instance_Node_CreatedAt");
        builder.HasIndex(entity => new { entity.BusinessRoleId, entity.Status })
            .HasDatabaseName("IX_WorkflowTask_BusinessRole_Status");
        builder.HasOne<WorkflowInstanceEntity>().WithMany().HasForeignKey(entity => entity.WorkflowInstanceId).HasConstraintName("FK_WorkflowTask_WorkflowInstance").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.AssigneeId).HasConstraintName("FK_WorkflowTask_Assignee").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_WorkflowTask_Department").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoleEntity>().WithMany()
            .HasForeignKey(entity => entity.BusinessRoleId)
            .HasConstraintName("FK_WorkflowTask_BusinessRole")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.OriginalAssigneeId)
            .HasConstraintName("FK_WorkflowTask_OriginalAssignee")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.BackupAssigneeId)
            .HasConstraintName("FK_WorkflowTask_BackupAssignee")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.TransferredBy)
            .HasConstraintName("FK_WorkflowTask_TransferredBy")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkflowNodePolicyConfiguration : IEntityTypeConfiguration<WorkflowNodePolicyEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowNodePolicyEntity> builder)
    {
        builder.ToTable("WorkflowNodePolicy", "Workflow", table =>
        {
            table.HasCheckConstraint("CK_WorkflowNodePolicy_Sla", "[SlaMinutes] IS NULL OR [SlaMinutes] > 0");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_Reminder", "[ReminderMinutes] IS NULL OR [ReminderMinutes] >= 0");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_Escalation", "[EscalationMinutes] IS NULL OR [EscalationMinutes] >= 0");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_Status", "[Status] IN ('Draft','Published','Retired')");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_Risk", "[AgentMaxRisk] IN ('Low','Medium','High')");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_ProxyMode", "[AgentProxyMode] IN ('RemindOnly','HumanConfirmedProxy','PreAuthorizedAutoProxy')");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_AutoProxyAfter", "[AutoProxyAfterMinutes] IS NULL OR [AutoProxyAfterMinutes] > 0");
            table.HasCheckConstraint(
                "CK_WorkflowNodePolicy_AssigneeStrategy",
                "[AssigneeStrategy] IN ('SpecificUser','BusinessRole','DepartmentManager','ProjectOwner','InitiatorManager','Agent')");
            table.HasCheckConstraint(
                "CK_WorkflowNodePolicy_TransferTargetStrategy",
                "[TransferTargetStrategy] IS NULL OR [TransferTargetStrategy] IN ('BackupApprover','DepartmentManager')");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_FormSchemaJson", "ISJSON([FormSchemaJson]) = 1");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_DeliverablesJson", "ISJSON([DeliverablesJson]) = 1");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_RelatedRolesJson", "ISJSON([RelatedRoleCodesJson]) = 1");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_NodeActionsJson", "ISJSON([NodeActionsJson]) = 1");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_AiReviewJson", "ISJSON([AiReviewJson]) = 1");
            table.HasCheckConstraint("CK_WorkflowNodePolicy_NotificationJson", "ISJSON([NotificationPolicyJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_WorkflowNodePolicy");
        builder.Property(entity => entity.NodeCode).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.NodeName).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.BusinessDescription).HasMaxLength(2000);
        builder.Property(entity => entity.TimeoutStrategy).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.AssigneeStrategy).HasMaxLength(32).IsRequired().HasDefaultValue("SpecificUser");
        builder.Property(entity => entity.TransferTargetStrategy).HasMaxLength(32);
        builder.Property(entity => entity.AgentMaxRisk).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.AgentMaxAutonomy).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.AgentProxyMode).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.AgentMaxAmount).HasColumnType("decimal(18,4)");
        builder.Property(entity => entity.PrecheckItems).HasMaxLength(2000);
        builder.Property(entity => entity.DeliverablesJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("[]");
        builder.Property(entity => entity.RelatedRoleCodesJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("[]");
        builder.Property(entity => entity.CompletionCondition).HasMaxLength(2000);
        builder.Property(entity => entity.FormSchemaJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("[]");
        builder.Property(entity => entity.FormSchemaVersion).HasMaxLength(32).IsRequired().HasDefaultValue("1.0");
        builder.Property(entity => entity.NodeActionsJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("[]");
        builder.Property(entity => entity.AiReviewJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("{}");
        builder.Property(entity => entity.NotificationPolicyJson).HasColumnType("nvarchar(max)").IsRequired().HasDefaultValue("{}");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.WorkflowVersionId, entity.NodeCode, entity.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_WorkflowNodePolicy_Version_Node_VersionNumber");
        builder.HasIndex(entity => new { entity.WorkflowVersionId, entity.NodeCode, entity.Status })
            .HasDatabaseName("IX_WorkflowNodePolicy_Version_Node_Status");
        builder.HasOne<WorkflowVersionEntity>().WithMany()
            .HasForeignKey(entity => entity.WorkflowVersionId)
            .HasConstraintName("FK_WorkflowNodePolicy_WorkflowVersion")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.PublishedBy)
            .HasConstraintName("FK_WorkflowNodePolicy_PublishedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.ApproverUserId)
            .HasConstraintName("FK_WorkflowNodePolicy_Approver")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.BackupApproverUserId)
            .HasConstraintName("FK_WorkflowNodePolicy_BackupApprover")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgentProfileEntity>().WithMany()
            .HasForeignKey(entity => entity.AgentProfileId)
            .HasConstraintName("FK_WorkflowNodePolicy_AgentProfile")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<RoleEntity>().WithMany()
            .HasForeignKey(entity => entity.BusinessRoleId)
            .HasConstraintName("FK_WorkflowNodePolicy_BusinessRole")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkflowNotificationConfiguration : IEntityTypeConfiguration<WorkflowNotificationEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowNotificationEntity> builder)
    {
        builder.ToTable("WorkflowNotification", "Workflow");
        builder.HasKey(entity => entity.Id).HasName("PK_WorkflowNotification");
        builder.Property(entity => entity.Type).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Title).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.Message).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(160).IsRequired();
        builder.HasIndex(entity => entity.IdempotencyKey).IsUnique().HasDatabaseName("UX_WorkflowNotification_Idempotency");
        builder.HasIndex(entity => new { entity.RecipientId, entity.IsRead, entity.CreatedAt }).HasDatabaseName("IX_WorkflowNotification_Recipient");
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId).HasConstraintName("FK_WorkflowNotification_Organization").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_WorkflowNotification_Department").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.RecipientId).HasConstraintName("FK_WorkflowNotification_Recipient").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowTaskEntity>().WithMany().HasForeignKey(entity => entity.WorkflowTaskId).HasConstraintName("FK_WorkflowNotification_Task").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkflowTaskCandidateConfiguration : IEntityTypeConfiguration<WorkflowTaskCandidateEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskCandidateEntity> builder)
    {
        builder.ToTable("WorkflowTaskCandidate", "Workflow");
        builder.HasKey(entity => new { entity.WorkflowTaskId, entity.UserId }).HasName("PK_WorkflowTaskCandidate");
        builder.HasOne<WorkflowTaskEntity>().WithMany().HasForeignKey(entity => entity.WorkflowTaskId).HasConstraintName("FK_WorkflowTaskCandidate_Task").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UserId).HasConstraintName("FK_WorkflowTaskCandidate_User").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class WorkflowTaskActionConfiguration : IEntityTypeConfiguration<WorkflowTaskActionEntity>
{
    public void Configure(EntityTypeBuilder<WorkflowTaskActionEntity> builder)
    {
        builder.ToTable("WorkflowTaskAction", "Workflow");
        builder.HasKey(entity => entity.Id).HasName("PK_WorkflowTaskAction");
        builder.Property(entity => entity.Id).ValueGeneratedOnAdd();
        builder.Property(entity => entity.Action).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Comment).HasMaxLength(2000);
        builder.HasIndex(entity => new { entity.WorkflowTaskId, entity.CreatedAt }).HasDatabaseName("IX_WorkflowTaskAction_Task_CreatedAt");
        builder.HasOne<WorkflowTaskEntity>().WithMany().HasForeignKey(entity => entity.WorkflowTaskId).HasConstraintName("FK_WorkflowTaskAction_Task").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.ActorId).HasConstraintName("FK_WorkflowTaskAction_Actor").OnDelete(DeleteBehavior.Restrict);
    }
}
