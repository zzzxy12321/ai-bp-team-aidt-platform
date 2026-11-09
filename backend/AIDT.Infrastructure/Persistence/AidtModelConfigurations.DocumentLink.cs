using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

internal sealed class AttachmentLinkConfiguration : IEntityTypeConfiguration<AttachmentLinkEntity>
{
    public void Configure(EntityTypeBuilder<AttachmentLinkEntity> builder)
    {
        builder.ToTable("AttachmentLink", "Document", table =>
            table.HasCheckConstraint(
                "CK_AttachmentLink_EntityType",
                "[EntityType] IN ('Department','BusinessObject','WorkflowInstance','WorkflowTask','ProjectTask','EventInbox','ScenarioLaunch')"));
        builder.HasKey(entity => entity.Id).HasName("PK_AttachmentLink");
        builder.Property(entity => entity.EntityType).HasMaxLength(64).IsRequired();
        builder.HasIndex(entity => new { entity.AttachmentId, entity.EntityType, entity.EntityId })
            .IsUnique()
            .HasDatabaseName("UX_AttachmentLink_Attachment_Entity");
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId, entity.CreatedAt })
            .HasDatabaseName("IX_AttachmentLink_Entity_CreatedAt");
        builder.HasOne<AttachmentEntity>()
            .WithMany()
            .HasForeignKey(entity => entity.AttachmentId)
            .HasConstraintName("FK_AttachmentLink_Attachment")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
