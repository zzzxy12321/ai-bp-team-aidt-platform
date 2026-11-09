using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEventEntity>
{
    public void Configure(EntityTypeBuilder<AuditEventEntity> builder)
    {
        builder.ToTable("AuditEvent", "Audit");
        builder.HasKey(entity => entity.Id).HasName("PK_AuditEvent");
        builder.Property(entity => entity.Module).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Action).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.TraceId).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Outcome).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.BeforeJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.AfterJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.Reason).HasMaxLength(2000);
        builder.Property(entity => entity.IpHash).HasMaxLength(128);
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId, entity.OccurredAt }).HasDatabaseName("IX_AuditEvent_Entity_OccurredAt");
        builder.HasIndex(entity => new { entity.TraceId, entity.OccurredAt }).HasDatabaseName("IX_AuditEvent_Trace_OccurredAt");
        builder.HasIndex(entity => new { entity.OccurredAt, entity.Id }).HasDatabaseName("IX_AuditEvent_OccurredAt_Id");
    }
}

internal sealed class EvidenceAssetConfiguration : IEntityTypeConfiguration<EvidenceAssetEntity>
{
    public void Configure(EntityTypeBuilder<EvidenceAssetEntity> builder)
    {
        builder.ToTable("EvidenceAsset", "Audit");
        builder.HasKey(entity => entity.Id).HasName("PK_EvidenceAsset");
        builder.Property(entity => entity.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Sensitivity).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.Sha256).HasDatabaseName("IX_EvidenceAsset_Sha256");
        builder.HasIndex(entity => new { entity.WorkflowInstanceId, entity.CreatedAt }).HasDatabaseName("IX_EvidenceAsset_Workflow_CreatedAt");
    }
}

internal sealed class ConnectorConfiguration : IEntityTypeConfiguration<ConnectorEntity>
{
    public void Configure(EntityTypeBuilder<ConnectorEntity> builder)
    {
        builder.ToTable("Connector", "Integration");
        builder.HasKey(entity => entity.Id).HasName("PK_Connector");
        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.AdapterType).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.ConnectionStatus).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.AuthorityOwner).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName("UX_Connector_Code");
    }
}

internal sealed class ConnectorCommandConfiguration : IEntityTypeConfiguration<ConnectorCommandEntity>
{
    public void Configure(EntityTypeBuilder<ConnectorCommandEntity> builder)
    {
        builder.ToTable("ConnectorCommand", "Integration", table =>
            table.HasCheckConstraint("CK_ConnectorCommand_PayloadJson", "ISJSON([PayloadJson]) = 1"));
        builder.HasKey(entity => entity.Id).HasName("PK_ConnectorCommand");
        builder.Property(entity => entity.CommandType).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ApprovalEvidenceJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.ConnectorId, entity.IdempotencyKey }).IsUnique().HasDatabaseName("UX_ConnectorCommand_Connector_Idempotency");
        builder.HasIndex(entity => new { entity.Status, entity.UpdatedAt }).HasDatabaseName("IX_ConnectorCommand_Status_UpdatedAt");
        builder.HasOne<ConnectorEntity>().WithMany().HasForeignKey(entity => entity.ConnectorId).HasConstraintName("FK_ConnectorCommand_Connector").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ConnectorReceiptConfiguration : IEntityTypeConfiguration<ConnectorReceiptEntity>
{
    public void Configure(EntityTypeBuilder<ConnectorReceiptEntity> builder)
    {
        builder.ToTable("ConnectorReceipt", "Integration");
        builder.HasKey(entity => entity.Id).HasName("PK_ConnectorReceipt");
        builder.Property(entity => entity.ExternalId).HasMaxLength(128);
        builder.Property(entity => entity.ResponseDigest).HasMaxLength(128).IsRequired();
        builder.HasIndex(entity => new { entity.ConnectorCommandId, entity.ReceivedAt }).HasDatabaseName("IX_ConnectorReceipt_Command_ReceivedAt");
        builder.HasOne<ConnectorCommandEntity>().WithMany().HasForeignKey(entity => entity.ConnectorCommandId).HasConstraintName("FK_ConnectorReceipt_Command").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OutboxEventConfiguration : IEntityTypeConfiguration<OutboxEventEntity>
{
    public void Configure(EntityTypeBuilder<OutboxEventEntity> builder)
    {
        builder.ToTable("OutboxEvent", "Integration", table =>
            table.HasCheckConstraint("CK_OutboxEvent_PayloadJson", "ISJSON([PayloadJson]) = 1"));
        builder.HasKey(entity => entity.Id).HasName("PK_OutboxEvent");
        builder.Property(entity => entity.EventType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.AggregateType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.IdempotencyKey).IsUnique().HasDatabaseName("UX_OutboxEvent_Idempotency");
        builder.HasIndex(entity => new { entity.Status, entity.NextAttemptAt, entity.CreatedAt }).HasDatabaseName("IX_OutboxEvent_Status_NextAttemptAt");
        builder.HasIndex(entity => new { entity.AggregateType, entity.AggregateId }).HasDatabaseName("IX_OutboxEvent_Aggregate");
    }
}

internal sealed class DeadLetterConfiguration : IEntityTypeConfiguration<DeadLetterEntity>
{
    public void Configure(EntityTypeBuilder<DeadLetterEntity> builder)
    {
        builder.ToTable("DeadLetter", "Integration");
        builder.HasKey(entity => entity.Id).HasName("PK_DeadLetter");
        builder.Property(entity => entity.Id).ValueGeneratedOnAdd();
        builder.Property(entity => entity.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.ResolutionNote).HasMaxLength(2000);
        builder.HasIndex(entity => new { entity.OutboxEventId, entity.CreatedAt }).HasDatabaseName("IX_DeadLetter_Outbox_CreatedAt");
        builder.HasOne<OutboxEventEntity>().WithMany().HasForeignKey(entity => entity.OutboxEventId).HasConstraintName("FK_DeadLetter_OutboxEvent").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ReconciliationConfiguration : IEntityTypeConfiguration<ReconciliationEntity>
{
    public void Configure(EntityTypeBuilder<ReconciliationEntity> builder)
    {
        builder.ToTable("Reconciliation", "Integration", table =>
            table.HasCheckConstraint("CK_Reconciliation_Period", "[PeriodEnd] >= [PeriodStart]"));
        builder.HasKey(entity => entity.Id).HasName("PK_Reconciliation");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.ConnectorId, entity.PeriodStart, entity.PeriodEnd }).HasDatabaseName("IX_Reconciliation_Connector_Period");
        builder.HasOne<ConnectorEntity>().WithMany().HasForeignKey(entity => entity.ConnectorId).HasConstraintName("FK_Reconciliation_Connector").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecordEntity>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecordEntity> builder)
    {
        builder.ToTable("IdempotencyRecord", "Integration");
        builder.HasKey(entity => entity.Id).HasName("PK_IdempotencyRecord");
        builder.Property(entity => entity.Scope).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.RequestHash).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.ResponseType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.ResponseId).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => new { entity.Scope, entity.IdempotencyKey }).IsUnique().HasDatabaseName("UX_IdempotencyRecord_Scope_Key");
        builder.HasIndex(entity => entity.ExpiresAt).HasDatabaseName("IX_IdempotencyRecord_ExpiresAt");
    }
}

internal sealed class AgentProfileConfiguration : IEntityTypeConfiguration<AgentProfileEntity>
{
    public void Configure(EntityTypeBuilder<AgentProfileEntity> builder)
    {
        builder.ToTable("AgentProfile", "Agent", table =>
        {
            table.HasCheckConstraint("CK_AgentProfile_AmountLimit", "[AmountLimit] >= 0");
            table.HasCheckConstraint(
                "CK_AgentProfile_Lifecycle",
                "[LifecycleStatus] IN ('Draft','Published','Disabled','Retired')");
            table.HasCheckConstraint(
                "CK_AgentProfile_DataScopesJson",
                "ISJSON([DataScopesJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfile_ModelScopesJson",
                "ISJSON([ModelScopesJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfile_AllowedToolsJson",
                "ISJSON([AllowedToolsJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfile_ConfirmationsJson",
                "ISJSON([RequiredConfirmationsJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfile_StopConditionsJson",
                "ISJSON([StopConditionsJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_AgentProfile");
        builder.Property(entity => entity.ServiceUserId).IsRequired();
        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.AutonomyLevel).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.AmountLimit).HasColumnType("decimal(18,4)");
        builder.Property(entity => entity.RiskLimit).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.DataScopesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ModelScopesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.AllowedToolsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.RequiredConfirmationsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.StopConditionsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.LifecycleStatus).HasMaxLength(16).IsRequired();
        builder.HasIndex(entity => entity.Code).IsUnique().HasDatabaseName("UX_AgentProfile_Code");
        builder.HasIndex(entity => entity.Name).HasDatabaseName("IX_AgentProfile_Name");
        builder.HasIndex(entity => entity.ServiceUserId).IsUnique().HasDatabaseName("UX_AgentProfile_ServiceUser");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.LifecycleStatus, entity.IsActive })
            .HasDatabaseName("IX_AgentProfile_Department_Lifecycle");
        builder.HasIndex(entity => entity.CurrentPublishedVersionId)
            .HasDatabaseName("IX_AgentProfile_CurrentPublishedVersionId");
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.ServiceUserId).HasConstraintName("FK_AgentProfile_ServiceUser").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_AgentProfile_Department").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.BusinessOwnerUserId)
            .HasConstraintName("FK_AgentProfile_BusinessOwner").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.TechnicalOwnerUserId)
            .HasConstraintName("FK_AgentProfile_TechnicalOwner").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_AgentProfile_CreatedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_AgentProfile_UpdatedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AgentProfileVersionEntity>().WithMany()
            .HasForeignKey(entity => entity.CurrentPublishedVersionId)
            .HasConstraintName("FK_AgentProfile_CurrentPublishedVersion")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AgentProfileVersionConfiguration : IEntityTypeConfiguration<AgentProfileVersionEntity>
{
    public void Configure(EntityTypeBuilder<AgentProfileVersionEntity> builder)
    {
        builder.ToTable("AgentProfileVersion", "Agent", table =>
        {
            table.HasCheckConstraint("CK_AgentProfileVersion_AmountLimit", "[AmountLimit] >= 0");
            table.HasCheckConstraint(
                "CK_AgentProfileVersion_Status",
                "[Status] IN ('Draft','Validated','Published','Retired')");
            table.HasCheckConstraint(
                "CK_AgentProfileVersion_DataScopesJson",
                "ISJSON([DataScopesJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfileVersion_ModelScopesJson",
                "ISJSON([ModelScopesJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfileVersion_AllowedToolsJson",
                "ISJSON([AllowedToolsJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfileVersion_ConfirmationsJson",
                "ISJSON([RequiredConfirmationsJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfileVersion_StopConditionsJson",
                "ISJSON([StopConditionsJson]) = 1");
            table.HasCheckConstraint(
                "CK_AgentProfileVersion_HighRiskReasonsJson",
                "ISJSON([HighRiskReasonsJson]) = 1");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_AgentProfileVersion");
        builder.Property(entity => entity.Status).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Code).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000).IsRequired();
        builder.Property(entity => entity.AutonomyLevel).HasMaxLength(8).IsRequired();
        builder.Property(entity => entity.AmountLimit).HasColumnType("decimal(18,4)");
        builder.Property(entity => entity.RiskLimit).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.DataScopesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ModelScopesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.AllowedToolsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.RequiredConfirmationsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.StopConditionsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ValidationJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.HighRiskReasonsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(entity => new { entity.AgentProfileId, entity.VersionNumber })
            .IsUnique()
            .HasDatabaseName("UX_AgentProfileVersion_Profile_Version");
        builder.HasIndex(entity => entity.AgentProfileId)
            .IsUnique()
            .HasFilter("[Status] = 'Published'")
            .HasDatabaseName("UX_AgentProfileVersion_Profile_Published");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.Status, entity.UpdatedAt })
            .HasDatabaseName("IX_AgentProfileVersion_Department_Status_UpdatedAt");
        builder.HasOne<AgentProfileEntity>().WithMany()
            .HasForeignKey(entity => entity.AgentProfileId)
            .HasConstraintName("FK_AgentProfileVersion_Profile")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.ServiceUserId)
            .HasConstraintName("FK_AgentProfileVersion_ServiceUser").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_AgentProfileVersion_Department").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.BusinessOwnerUserId)
            .HasConstraintName("FK_AgentProfileVersion_BusinessOwner").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.TechnicalOwnerUserId)
            .HasConstraintName("FK_AgentProfileVersion_TechnicalOwner").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_AgentProfileVersion_CreatedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_AgentProfileVersion_UpdatedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.ValidatedBy)
            .HasConstraintName("FK_AgentProfileVersion_ValidatedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.PublishedBy)
            .HasConstraintName("FK_AgentProfileVersion_PublishedBy").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AgentRunConfiguration : IEntityTypeConfiguration<AgentRunEntity>
{
    public void Configure(EntityTypeBuilder<AgentRunEntity> builder)
    {
        builder.ToTable("AgentRun", "Agent", table =>
        {
            table.HasCheckConstraint("CK_AgentRun_Amount", "[Amount] >= 0");
            table.HasCheckConstraint("CK_AgentRun_Cost", "[Cost] IS NULL OR [Cost] >= 0");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_AgentRun");
        builder.Property(entity => entity.Action).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Amount).HasColumnType("decimal(18,4)");
        builder.Property(entity => entity.RiskLevel).HasMaxLength(16).IsRequired().HasDefaultValue("Low");
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(128);
        builder.Property(entity => entity.SkillVersion).HasMaxLength(64);
        builder.Property(entity => entity.InputDigest).HasMaxLength(128);
        builder.Property(entity => entity.OutputDigest).HasMaxLength(128);
        builder.Property(entity => entity.Cost).HasColumnType("decimal(18,6)");
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.SubjectType).HasMaxLength(64);
        builder.Property(entity => entity.GatewayMode).HasMaxLength(32);
        builder.Property(entity => entity.ProxyTriggerMode).HasMaxLength(32);
        builder.Property(entity => entity.CompensationStatus).HasMaxLength(32);
        builder.Property(entity => entity.ProxyIdempotencyKey).HasMaxLength(160);
        builder.HasIndex(entity => new { entity.AgentProfileId, entity.CreatedAt }).HasDatabaseName("IX_AgentRun_Profile_CreatedAt");
        builder.HasIndex(entity => new { entity.Status, entity.CreatedAt }).HasDatabaseName("IX_AgentRun_Status_CreatedAt");
        builder.HasIndex(entity => entity.ProxyIdempotencyKey)
            .IsUnique()
            .HasFilter("[ProxyIdempotencyKey] IS NOT NULL")
            .HasDatabaseName("UX_AgentRun_ProxyIdempotency");
        builder.HasOne<AgentProfileEntity>().WithMany().HasForeignKey(entity => entity.AgentProfileId).HasConstraintName("FK_AgentRun_Profile").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.RequestedBy).HasConstraintName("FK_AgentRun_RequestedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_AgentRun_Department").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId).HasConstraintName("FK_AgentRun_Organization").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AgentGatewayCallConfiguration : IEntityTypeConfiguration<AgentGatewayCallEntity>
{
    public void Configure(EntityTypeBuilder<AgentGatewayCallEntity> builder)
    {
        builder.ToTable("AgentGatewayCall", "Agent");
        builder.HasKey(entity => entity.Id).HasName("PK_AgentGatewayCall");
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.Model).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Mode).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.GatewayCallId).HasMaxLength(128);
        builder.Property(entity => entity.InputDigest).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.OutputDigest).HasMaxLength(128);
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.ErrorCode).HasMaxLength(64);
        builder.Property(entity => entity.ErrorMessage).HasMaxLength(2000);
        builder.HasIndex(entity => new { entity.AgentRunId, entity.CreatedAt }).HasDatabaseName("IX_AgentGatewayCall_Run_CreatedAt");
        builder.HasOne<AgentRunEntity>().WithMany().HasForeignKey(entity => entity.AgentRunId).HasConstraintName("FK_AgentGatewayCall_Run").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId).HasConstraintName("FK_AgentGatewayCall_Organization").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_AgentGatewayCall_Department").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AgentConfirmationConfiguration : IEntityTypeConfiguration<AgentConfirmationEntity>
{
    public void Configure(EntityTypeBuilder<AgentConfirmationEntity> builder)
    {
        builder.ToTable("AgentConfirmation", "Agent");
        builder.HasKey(entity => entity.Id).HasName("PK_AgentConfirmation");
        builder.Property(entity => entity.Decision).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Comment).HasMaxLength(2000);
        builder.HasIndex(entity => new { entity.AgentRunId, entity.ConfirmedAt }).HasDatabaseName("IX_AgentConfirmation_Run_ConfirmedAt");
        builder.HasOne<AgentRunEntity>().WithMany().HasForeignKey(entity => entity.AgentRunId).HasConstraintName("FK_AgentConfirmation_Run").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.ConfirmedBy).HasConstraintName("FK_AgentConfirmation_User").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AttachmentConfiguration : IEntityTypeConfiguration<AttachmentEntity>
{
    public void Configure(EntityTypeBuilder<AttachmentEntity> builder)
    {
        builder.ToTable("Attachment", "Document", table =>
        {
            table.HasCheckConstraint("CK_Attachment_Environment", "[EnvironmentCode] IN ('demo','test','prod')");
            table.HasCheckConstraint("CK_Attachment_Status", "[Status] IN ('Active','DeletionRequested')");
            table.HasCheckConstraint("CK_Attachment_Sensitivity", "[Sensitivity] IN ('Internal','Sensitive','Core')");
            table.HasCheckConstraint("CK_Attachment_ScanStatus", "[VirusScanStatus] IN ('NotConfigured','PendingManualReview','Clean','Infected')");
            table.HasCheckConstraint("CK_Attachment_Size", "[SizeBytes] > 0");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_Attachment");
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired();
        builder.Property(entity => entity.SourceSystem).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.Status).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.DeletionReason).HasMaxLength(1000);
        builder.Property(entity => entity.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.VirusScanStatus).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Sensitivity).HasMaxLength(32).IsRequired();
        builder.HasIndex(entity => entity.BusinessObjectId).HasDatabaseName("IX_Attachment_BusinessObjectId");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.DepartmentId }).HasDatabaseName("IX_Attachment_Organization_Department");
        builder.HasIndex(entity => entity.Sha256).HasDatabaseName("IX_Attachment_Sha256");
        builder.HasIndex(entity => new { entity.DepartmentId, entity.EnvironmentCode, entity.CreatedAt, entity.Id }).HasDatabaseName("IX_Attachment_Department_Environment_CreatedAt");
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId).HasConstraintName("FK_Attachment_Organization").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId).HasConstraintName("FK_Attachment_Department").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersionEntity>
{
    public void Configure(EntityTypeBuilder<DocumentVersionEntity> builder)
    {
        builder.ToTable("DocumentVersion", "Document", table =>
        {
            table.HasCheckConstraint("CK_DocumentVersion_Sensitivity", "[Sensitivity] IN ('Internal','Sensitive','Core')");
            table.HasCheckConstraint("CK_DocumentVersion_ScanStatus", "[VirusScanStatus] IN ('NotConfigured','PendingManualReview','Clean','Infected')");
            table.HasCheckConstraint("CK_DocumentVersion_Size", "[SizeBytes] > 0");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_DocumentVersion");
        builder.Property(entity => entity.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(entity => entity.MimeType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.VirusScanStatus).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Sensitivity).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.Sha256).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => new { entity.DocumentId, entity.VersionNumber }).IsUnique().HasDatabaseName("UX_DocumentVersion_Document_Version");
        builder.HasOne<AttachmentEntity>().WithMany().HasForeignKey(entity => entity.DocumentId).HasConstraintName("FK_DocumentVersion_Attachment").OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SignatureConfiguration : IEntityTypeConfiguration<SignatureEntity>
{
    public void Configure(EntityTypeBuilder<SignatureEntity> builder)
    {
        builder.ToTable("Signature", "Document");
        builder.HasKey(entity => entity.Id).HasName("PK_Signature");
        builder.Property(entity => entity.EntityType).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.EntityId).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.EvidenceReference).HasMaxLength(500).IsRequired();
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId, entity.SignedAt }).HasDatabaseName("IX_Signature_Entity_SignedAt");
    }
}

internal sealed class ConfigurationVersionConfiguration : IEntityTypeConfiguration<ConfigurationVersionEntity>
{
    public void Configure(EntityTypeBuilder<ConfigurationVersionEntity> builder)
    {
        builder.ToTable("ConfigurationVersion", "Configuration", table =>
        {
            table.HasCheckConstraint("CK_ConfigurationVersion_PayloadJson", "ISJSON([PayloadJson]) = 1");
            table.HasCheckConstraint(
                "CK_ConfigurationVersion_ApplicableRolesJson",
                "ISJSON([ApplicableRoleCodesJson]) = 1");
            table.HasCheckConstraint(
                "CK_ConfigurationVersion_State",
                "[State] IN ('Draft','Validated','Published','Retired','Active')");
            table.HasCheckConstraint(
                "CK_ConfigurationVersion_Type",
                "[ConfigurationType] IN ('Workflow','System')");
            table.HasCheckConstraint(
                "CK_ConfigurationVersion_RiskLevel",
                "[RiskLevel] IN ('Low','Medium','High')");
            table.HasCheckConstraint(
                "CK_ConfigurationVersion_Environment",
                "[EnvironmentCode] IN ('demo','test','prod')");
        });
        builder.HasKey(entity => entity.Id).HasName("PK_ConfigurationVersion");
        builder.Property(entity => entity.ConfigurationKey).HasMaxLength(128).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(200).IsRequired();
        builder.Property(entity => entity.Description).HasMaxLength(2000);
        builder.Property(entity => entity.ConfigurationType).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.State).HasMaxLength(32).IsRequired();
        builder.Property(entity => entity.RiskLevel).HasMaxLength(16).IsRequired().HasDefaultValue("Low");
        builder.Property(entity => entity.ApplicableRoleCodesJson)
            .HasColumnType("nvarchar(max)")
            .IsRequired()
            .HasDefaultValue("[]");
        builder.Property(entity => entity.EnvironmentCode).HasMaxLength(16).IsRequired().HasDefaultValue("demo");
        builder.Property(entity => entity.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(entity => entity.ImpactAnalysisJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(entity => new { entity.ConfigurationKey, entity.VersionNumber }).IsUnique().HasDatabaseName("UX_ConfigurationVersion_Key_Version");
        builder.HasIndex(entity => new { entity.OrganizationId, entity.ConfigurationType, entity.State, entity.UpdatedAt })
            .HasDatabaseName("IX_ConfigurationVersion_Org_Type_State_UpdatedAt");
        builder.HasIndex(entity => new
        {
            entity.OrganizationId,
            entity.DepartmentId,
            entity.IsTestData,
            entity.EnvironmentCode,
            entity.UpdatedAt
        })
            .HasDatabaseName("IX_ConfigurationVersion_Org_Dept_Test_Env_Updated");
        builder.HasOne<OrganizationEntity>().WithMany().HasForeignKey(entity => entity.OrganizationId)
            .HasConstraintName("FK_ConfigurationVersion_Organization").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DepartmentEntity>().WithMany().HasForeignKey(entity => entity.DepartmentId)
            .HasConstraintName("FK_ConfigurationVersion_Department").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.CreatedBy)
            .HasConstraintName("FK_ConfigurationVersion_CreatedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.UpdatedBy)
            .HasConstraintName("FK_ConfigurationVersion_UpdatedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany().HasForeignKey(entity => entity.PublishedBy)
            .HasConstraintName("FK_ConfigurationVersion_PublishedBy").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkflowVersionEntity>().WithMany().HasForeignKey(entity => entity.PublishedWorkflowVersionId)
            .HasConstraintName("FK_ConfigurationVersion_PublishedWorkflowVersion").OnDelete(DeleteBehavior.Restrict);
    }
}
