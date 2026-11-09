using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIDT.Infrastructure.Persistence;

public sealed class McpClientConfiguration : IEntityTypeConfiguration<McpClientEntity>
{
    public void Configure(EntityTypeBuilder<McpClientEntity> builder)
    {
        builder.ToTable("Client", "Mcp");
        builder.HasKey(entity => entity.Id).HasName("PK_McpClient");
        builder.Property(entity => entity.ClientId).HasMaxLength(80);
        builder.Property(entity => entity.ClientName).HasMaxLength(200);
        builder.Property(entity => entity.ClientSecretHash).HasMaxLength(128);
        builder.Property(entity => entity.RedirectUrisJson).HasColumnType("nvarchar(max)");
        builder.Property(entity => entity.GrantTypesJson).HasMaxLength(500);
        builder.Property(entity => entity.ScopesJson).HasMaxLength(500);
        builder.Property(entity => entity.TokenEndpointAuthMethod).HasMaxLength(32);
        builder.HasIndex(entity => entity.ClientId).IsUnique().HasDatabaseName("UX_McpClient_ClientId");
        builder.HasIndex(entity => new { entity.IsActive, entity.LastUsedAt })
            .HasDatabaseName("IX_McpClient_Active_LastUsed");
        builder.HasIndex(entity => entity.AgentProfileId)
            .HasDatabaseName("IX_McpClient_AgentProfileId");
        builder.HasOne<AgentProfileEntity>().WithMany()
            .HasForeignKey(entity => entity.AgentProfileId)
            .HasConstraintName("FK_McpClient_AgentProfile")
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_McpClient_AuthMethod",
                "[TokenEndpointAuthMethod] IN ('none','client_secret_post','client_secret_basic')");
        });
    }
}

public sealed class McpAuthorizationRequestConfiguration : IEntityTypeConfiguration<McpAuthorizationRequestEntity>
{
    public void Configure(EntityTypeBuilder<McpAuthorizationRequestEntity> builder)
    {
        builder.ToTable("AuthorizationRequest", "Mcp");
        builder.HasKey(entity => entity.Id).HasName("PK_McpAuthorizationRequest");
        builder.Property(entity => entity.ClientId).HasMaxLength(80);
        builder.Property(entity => entity.RedirectUri).HasMaxLength(2000);
        builder.Property(entity => entity.RequestedScopesJson).HasMaxLength(500);
        builder.Property(entity => entity.State).HasMaxLength(512);
        builder.Property(entity => entity.CodeChallenge).HasMaxLength(256);
        builder.Property(entity => entity.CodeChallengeMethod).HasMaxLength(16);
        builder.HasIndex(entity => new { entity.ClientId, entity.ExpiresAt })
            .HasDatabaseName("IX_McpAuthorizationRequest_Client_Expires");
        builder.HasIndex(entity => new { entity.UserId, entity.ExpiresAt })
            .HasDatabaseName("IX_McpAuthorizationRequest_User_Expires");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_McpAuthorizationRequest_ChallengeMethod",
                "[CodeChallengeMethod]='S256'");
        });
    }
}

public sealed class McpAuthorizationCodeConfiguration : IEntityTypeConfiguration<McpAuthorizationCodeEntity>
{
    public void Configure(EntityTypeBuilder<McpAuthorizationCodeEntity> builder)
    {
        builder.ToTable("AuthorizationCode", "Mcp");
        builder.HasKey(entity => entity.Id).HasName("PK_McpAuthorizationCode");
        builder.Property(entity => entity.CodeHash).HasMaxLength(64);
        builder.Property(entity => entity.ClientId).HasMaxLength(80);
        builder.Property(entity => entity.RedirectUri).HasMaxLength(2000);
        builder.Property(entity => entity.GrantedScopesJson).HasMaxLength(500);
        builder.Property(entity => entity.DepartmentIdsJson).HasMaxLength(4000);
        builder.Property(entity => entity.CodeChallenge).HasMaxLength(256);
        builder.Property(entity => entity.CodeChallengeMethod).HasMaxLength(16);
        builder.HasIndex(entity => entity.CodeHash).IsUnique().HasDatabaseName("UX_McpAuthorizationCode_Hash");
        builder.HasIndex(entity => new { entity.ClientId, entity.ExpiresAt })
            .HasDatabaseName("IX_McpAuthorizationCode_Client_Expires");
    }
}

public sealed class McpAccessTokenConfiguration : IEntityTypeConfiguration<McpAccessTokenEntity>
{
    public void Configure(EntityTypeBuilder<McpAccessTokenEntity> builder)
    {
        builder.ToTable("AccessToken", "Mcp");
        builder.HasKey(entity => entity.Id).HasName("PK_McpAccessToken");
        builder.Property(entity => entity.TokenHash).HasMaxLength(64);
        builder.Property(entity => entity.TokenKind).HasMaxLength(16);
        builder.Property(entity => entity.ClientId).HasMaxLength(80);
        builder.Property(entity => entity.GrantedScopesJson).HasMaxLength(500);
        builder.Property(entity => entity.DepartmentIdsJson).HasMaxLength(4000);
        builder.Property(entity => entity.RevocationReason).HasMaxLength(1000);
        builder.HasIndex(entity => entity.TokenHash).IsUnique().HasDatabaseName("UX_McpAccessToken_Hash");
        builder.HasIndex(entity => new { entity.UserId, entity.ExpiresAt, entity.RevokedAt })
            .HasDatabaseName("IX_McpAccessToken_User_Expiry");
        builder.HasIndex(entity => new { entity.ClientId, entity.TokenKind, entity.ExpiresAt })
            .HasDatabaseName("IX_McpAccessToken_Client_Kind_Expiry");
        builder.HasIndex(entity => new { entity.TokenFamilyId, entity.ExpiresAt })
            .HasDatabaseName("IX_McpAccessToken_Family_Expiry");
        builder.HasIndex(entity => new { entity.AgentProfileId, entity.RevokedAt, entity.ExpiresAt })
            .HasDatabaseName("IX_McpAccessToken_AgentProfile_Active");
        builder.HasOne<AgentProfileEntity>().WithMany()
            .HasForeignKey(entity => entity.AgentProfileId)
            .HasConstraintName("FK_McpAccessToken_AgentProfile")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserEntity>().WithMany()
            .HasForeignKey(entity => entity.RevokedBy)
            .HasConstraintName("FK_McpAccessToken_RevokedBy")
            .OnDelete(DeleteBehavior.Restrict);
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_McpAccessToken_Kind",
                "[TokenKind] IN ('Access','Refresh')");
        });
    }
}

public sealed class McpHumanConfirmationConfiguration : IEntityTypeConfiguration<McpHumanConfirmationEntity>
{
    public void Configure(EntityTypeBuilder<McpHumanConfirmationEntity> builder)
    {
        builder.ToTable("HumanConfirmation", "Mcp");
        builder.HasKey(entity => entity.Id).HasName("PK_McpHumanConfirmation");
        builder.Property(entity => entity.ClientId).HasMaxLength(80);
        builder.Property(entity => entity.ToolName).HasMaxLength(128);
        builder.Property(entity => entity.ResourceType).HasMaxLength(128);
        builder.Property(entity => entity.ResourceId).HasMaxLength(128);
        builder.Property(entity => entity.RequestHash).HasMaxLength(64);
        builder.Property(entity => entity.Summary).HasMaxLength(2000);
        builder.Property(entity => entity.Status).HasMaxLength(16);
        builder.Property(entity => entity.DecisionComment).HasMaxLength(1000);
        builder.HasIndex(entity => new { entity.UserId, entity.Status, entity.ExpiresAt })
            .HasDatabaseName("IX_McpConfirmation_User_Status_Expiry");
        builder.HasIndex(entity => new { entity.ClientId, entity.ToolName, entity.RequestHash })
            .HasDatabaseName("IX_McpConfirmation_Request");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_McpConfirmation_Status",
                "[Status] IN ('Pending','Confirmed','Rejected','Consumed')");
        });
    }
}
