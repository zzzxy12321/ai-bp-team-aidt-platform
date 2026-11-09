using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AIDT.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Agent");

            migrationBuilder.EnsureSchema(
                name: "Document");

            migrationBuilder.EnsureSchema(
                name: "Audit");

            migrationBuilder.EnsureSchema(
                name: "BusinessObject");

            migrationBuilder.EnsureSchema(
                name: "Configuration");

            migrationBuilder.EnsureSchema(
                name: "Integration");

            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.EnsureSchema(
                name: "Metadata");

            migrationBuilder.EnsureSchema(
                name: "Workflow");

            migrationBuilder.CreateTable(
                name: "AgentProfile",
                schema: "Agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AutonomyLevel = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    AmountLimit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RiskLimit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    AllowedToolsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentProfile", x => x.Id);
                    table.CheckConstraint("CK_AgentProfile_AmountLimit", "[AmountLimit] >= 0");
                });

            migrationBuilder.CreateTable(
                name: "Attachment",
                schema: "Document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VirusScanStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Sensitivity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attachment", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEvent",
                schema: "Audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Module = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    TraceId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IpHash = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvent", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationVersion",
                schema: "Configuration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfigurationKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ImpactAnalysisJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationVersion", x => x.Id);
                    table.CheckConstraint("CK_ConfigurationVersion_PayloadJson", "ISJSON([PayloadJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "Connector",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AdapterType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ConnectionStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AuthorityOwner = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connector", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersion",
                schema: "Document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvidenceAsset",
                schema: "Audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StoragePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Sha256 = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MimeType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Sensitivity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvidenceAsset", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ObjectType",
                schema: "Metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Organization",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organization", x => x.Id);
                    table.CheckConstraint("CK_Organization_Code", "[Code] <> ''");
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvent",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvent", x => x.Id);
                    table.CheckConstraint("CK_OutboxEvent_PayloadJson", "ISJSON([PayloadJson]) = 1");
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Resource = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Signature",
                schema: "Document",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    SignedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvidenceReference = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Signature", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinition",
                schema: "Workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorCommand",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CommandType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApprovalEvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorCommand", x => x.Id);
                    table.CheckConstraint("CK_ConnectorCommand_PayloadJson", "ISJSON([PayloadJson]) = 1");
                    table.ForeignKey(
                        name: "FK_ConnectorCommand_Connector",
                        column: x => x.ConnectorId,
                        principalSchema: "Integration",
                        principalTable: "Connector",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Reconciliation",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStart = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DifferenceCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reconciliation", x => x.Id);
                    table.CheckConstraint("CK_Reconciliation_Period", "[PeriodEnd] >= [PeriodStart]");
                    table.ForeignKey(
                        name: "FK_Reconciliation_Connector",
                        column: x => x.ConnectorId,
                        principalSchema: "Integration",
                        principalTable: "Connector",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessObject",
                schema: "BusinessObject",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    FieldsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessObject", x => x.Id);
                    table.CheckConstraint("CK_BusinessObject_FieldsJson", "ISJSON([FieldsJson]) = 1");
                    table.ForeignKey(
                        name: "FK_BusinessObject_ObjectType",
                        column: x => x.ObjectTypeId,
                        principalSchema: "Metadata",
                        principalTable: "ObjectType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FieldDefinition",
                schema: "Metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Key = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DataType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    MaxLength = table.Column<int>(type: "int", nullable: false),
                    Sensitivity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SchemaVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FieldDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FieldDefinition_ObjectType",
                        column: x => x.ObjectTypeId,
                        principalSchema: "Metadata",
                        principalTable: "ObjectType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjectRelation",
                schema: "Metadata",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObjectTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetObjectTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjectRelation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjectRelation_SourceType",
                        column: x => x.ObjectTypeId,
                        principalSchema: "Metadata",
                        principalTable: "ObjectType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ObjectRelation_TargetType",
                        column: x => x.TargetObjectTypeId,
                        principalSchema: "Metadata",
                        principalTable: "ObjectType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "User",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    IsServiceIdentity = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                    table.ForeignKey(
                        name: "FK_User_Organization",
                        column: x => x.OrganizationId,
                        principalSchema: "Identity",
                        principalTable: "Organization",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeadLetter",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OutboxEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResolutionNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeadLetter", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeadLetter_OutboxEvent",
                        column: x => x.OutboxEventId,
                        principalSchema: "Integration",
                        principalTable: "OutboxEvent",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                schema: "Identity",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission",
                        column: x => x.PermissionId,
                        principalSchema: "Identity",
                        principalTable: "Permission",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermission_Role",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowVersion",
                schema: "Workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    DefinitionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowVersion", x => x.Id);
                    table.CheckConstraint("CK_WorkflowVersion_DefinitionJson", "ISJSON([DefinitionJson]) = 1");
                    table.ForeignKey(
                        name: "FK_WorkflowVersion_Definition",
                        column: x => x.WorkflowDefinitionId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ConnectorReceipt",
                schema: "Integration",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConnectorCommandId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    StatusCode = table.Column<int>(type: "int", nullable: false),
                    ResponseDigest = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectorReceipt", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectorReceipt_Command",
                        column: x => x.ConnectorCommandId,
                        principalSchema: "Integration",
                        principalTable: "ConnectorCommand",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessObjectRelation",
                schema: "BusinessObject",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessObjectRelation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessObjectRelation_Source",
                        column: x => x.SourceObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessObjectRelation_Target",
                        column: x => x.TargetObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessObjectVersion",
                schema: "BusinessObject",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessObjectVersion", x => x.Id);
                    table.CheckConstraint("CK_BusinessObjectVersion_SnapshotJson", "ISJSON([SnapshotJson]) = 1");
                    table.ForeignKey(
                        name: "FK_BusinessObjectVersion_BusinessObject",
                        column: x => x.BusinessObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentRun",
                schema: "Agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ConfirmationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Model = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SkillVersion = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    InputDigest = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OutputDigest = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    Cost = table.Column<decimal>(type: "decimal(18,6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    EndedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentRun", x => x.Id);
                    table.CheckConstraint("CK_AgentRun_Amount", "[Amount] >= 0");
                    table.CheckConstraint("CK_AgentRun_Cost", "[Cost] IS NULL OR [Cost] >= 0");
                    table.ForeignKey(
                        name: "FK_AgentRun_Profile",
                        column: x => x.AgentProfileId,
                        principalSchema: "Agent",
                        principalTable: "AgentProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentRun_RequestedBy",
                        column: x => x.RequestedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataScope",
                schema: "Identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScopeReference = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataScope", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataScope_User",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRole",
                schema: "Identity",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRole", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRole_Role",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Role",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRole_User",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstance",
                schema: "Workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowDefinitionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessObjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    CurrentNodeCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    StartedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowInstance_BusinessObject",
                        column: x => x.BusinessObjectId,
                        principalSchema: "BusinessObject",
                        principalTable: "BusinessObject",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowInstance_Definition",
                        column: x => x.WorkflowDefinitionId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowDefinition",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowInstance_Version",
                        column: x => x.WorkflowVersionId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowVersion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentConfirmation",
                schema: "Agent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AgentRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConfirmedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Decision = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentConfirmation", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentConfirmation_Run",
                        column: x => x.AgentRunId,
                        principalSchema: "Agent",
                        principalTable: "AgentRun",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AgentConfirmation_User",
                        column: x => x.ConfirmedBy,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTask",
                schema: "Workflow",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkflowInstanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NodeCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    AssigneeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    DecisionComment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTask", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTask_Assignee",
                        column: x => x.AssigneeId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTask_WorkflowInstance",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowInstance",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTaskAction",
                schema: "Workflow",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTaskAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAction_Actor",
                        column: x => x.ActorId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowTaskAction_Task",
                        column: x => x.WorkflowTaskId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowTaskCandidate",
                schema: "Workflow",
                columns: table => new
                {
                    WorkflowTaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowTaskCandidate", x => new { x.WorkflowTaskId, x.UserId });
                    table.ForeignKey(
                        name: "FK_WorkflowTaskCandidate_Task",
                        column: x => x.WorkflowTaskId,
                        principalSchema: "Workflow",
                        principalTable: "WorkflowTask",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkflowTaskCandidate_User",
                        column: x => x.UserId,
                        principalSchema: "Identity",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentConfirmation_ConfirmedBy",
                schema: "Agent",
                table: "AgentConfirmation",
                column: "ConfirmedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AgentConfirmation_Run_ConfirmedAt",
                schema: "Agent",
                table: "AgentConfirmation",
                columns: new[] { "AgentRunId", "ConfirmedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_AgentProfile_Name",
                schema: "Agent",
                table: "AgentProfile",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AgentRun_Profile_CreatedAt",
                schema: "Agent",
                table: "AgentRun",
                columns: new[] { "AgentProfileId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentRun_RequestedBy",
                schema: "Agent",
                table: "AgentRun",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AgentRun_Status_CreatedAt",
                schema: "Agent",
                table: "AgentRun",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_BusinessObjectId",
                schema: "Document",
                table: "Attachment",
                column: "BusinessObjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_Sha256",
                schema: "Document",
                table: "Attachment",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_Entity_OccurredAt",
                schema: "Audit",
                table: "AuditEvent",
                columns: new[] { "EntityType", "EntityId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_OccurredAt_Id",
                schema: "Audit",
                table: "AuditEvent",
                columns: new[] { "OccurredAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvent_Trace_OccurredAt",
                schema: "Audit",
                table: "AuditEvent",
                columns: new[] { "TraceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessObject_Status_UpdatedAt_Id",
                schema: "BusinessObject",
                table: "BusinessObject",
                columns: new[] { "Status", "UpdatedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "UX_BusinessObject_Type_Code",
                schema: "BusinessObject",
                table: "BusinessObject",
                columns: new[] { "ObjectTypeId", "ObjectCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessObjectRelation_TargetObjectId",
                schema: "BusinessObject",
                table: "BusinessObjectRelation",
                column: "TargetObjectId");

            migrationBuilder.CreateIndex(
                name: "UX_BusinessObjectRelation_Source_Code_Target",
                schema: "BusinessObject",
                table: "BusinessObjectRelation",
                columns: new[] { "SourceObjectId", "RelationCode", "TargetObjectId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_BusinessObjectVersion_Object_Version",
                schema: "BusinessObject",
                table: "BusinessObjectVersion",
                columns: new[] { "BusinessObjectId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ConfigurationVersion_Key_Version",
                schema: "Configuration",
                table: "ConfigurationVersion",
                columns: new[] { "ConfigurationKey", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Connector_Code",
                schema: "Integration",
                table: "Connector",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorCommand_Status_UpdatedAt",
                schema: "Integration",
                table: "ConnectorCommand",
                columns: new[] { "Status", "UpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_ConnectorCommand_Connector_Idempotency",
                schema: "Integration",
                table: "ConnectorCommand",
                columns: new[] { "ConnectorId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConnectorReceipt_Command_ReceivedAt",
                schema: "Integration",
                table: "ConnectorReceipt",
                columns: new[] { "ConnectorCommandId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_DataScope_User_Type_Reference",
                schema: "Identity",
                table: "DataScope",
                columns: new[] { "UserId", "ScopeType", "ScopeReference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeadLetter_Outbox_CreatedAt",
                schema: "Integration",
                table: "DeadLetter",
                columns: new[] { "OutboxEventId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_DocumentVersion_Document_Version",
                schema: "Document",
                table: "DocumentVersion",
                columns: new[] { "DocumentId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceAsset_Sha256",
                schema: "Audit",
                table: "EvidenceAsset",
                column: "Sha256");

            migrationBuilder.CreateIndex(
                name: "IX_EvidenceAsset_Workflow_CreatedAt",
                schema: "Audit",
                table: "EvidenceAsset",
                columns: new[] { "WorkflowInstanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_FieldDefinition_ObjectType_Key",
                schema: "Metadata",
                table: "FieldDefinition",
                columns: new[] { "ObjectTypeId", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ObjectRelation_TargetObjectTypeId",
                schema: "Metadata",
                table: "ObjectRelation",
                column: "TargetObjectTypeId");

            migrationBuilder.CreateIndex(
                name: "UX_ObjectRelation_Type_Target_Code",
                schema: "Metadata",
                table: "ObjectRelation",
                columns: new[] { "ObjectTypeId", "TargetObjectTypeId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_ObjectType_Code",
                schema: "Metadata",
                table: "ObjectType",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Organization_Code",
                schema: "Identity",
                table: "Organization",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_Aggregate",
                schema: "Integration",
                table: "OutboxEvent",
                columns: new[] { "AggregateType", "AggregateId" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvent_Status_NextAttemptAt",
                schema: "Integration",
                table: "OutboxEvent",
                columns: new[] { "Status", "NextAttemptAt", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "UX_OutboxEvent_Idempotency",
                schema: "Integration",
                table: "OutboxEvent",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Permission_Code",
                schema: "Identity",
                table: "Permission",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Permission_Resource_Action",
                schema: "Identity",
                table: "Permission",
                columns: new[] { "Resource", "Action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reconciliation_Connector_Period",
                schema: "Integration",
                table: "Reconciliation",
                columns: new[] { "ConnectorId", "PeriodStart", "PeriodEnd" });

            migrationBuilder.CreateIndex(
                name: "UX_Role_Code",
                schema: "Identity",
                table: "Role",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                schema: "Identity",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Signature_Entity_SignedAt",
                schema: "Document",
                table: "Signature",
                columns: new[] { "EntityType", "EntityId", "SignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_User_OrganizationId",
                schema: "Identity",
                table: "User",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "UX_User_Organization_UserName",
                schema: "Identity",
                table: "User",
                columns: new[] { "OrganizationId", "UserName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserRole_RoleId",
                schema: "Identity",
                table: "UserRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowDefinition_Code",
                schema: "Workflow",
                table: "WorkflowDefinition",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Object_StartedAt",
                schema: "Workflow",
                table: "WorkflowInstance",
                columns: new[] { "BusinessObjectId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_Status_StartedAt_Id",
                schema: "Workflow",
                table: "WorkflowInstance",
                columns: new[] { "Status", "StartedAt", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_WorkflowDefinitionId",
                schema: "Workflow",
                table: "WorkflowInstance",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstance_WorkflowVersionId",
                schema: "Workflow",
                table: "WorkflowInstance",
                column: "WorkflowVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTask_Assignee_Status_CreatedAt",
                schema: "Workflow",
                table: "WorkflowTask",
                columns: new[] { "AssigneeId", "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTask_Instance_CreatedAt",
                schema: "Workflow",
                table: "WorkflowTask",
                columns: new[] { "WorkflowInstanceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAction_ActorId",
                schema: "Workflow",
                table: "WorkflowTaskAction",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskAction_Task_CreatedAt",
                schema: "Workflow",
                table: "WorkflowTaskAction",
                columns: new[] { "WorkflowTaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowTaskCandidate_UserId",
                schema: "Workflow",
                table: "WorkflowTaskCandidate",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_WorkflowVersion_Definition_Version",
                schema: "Workflow",
                table: "WorkflowVersion",
                columns: new[] { "WorkflowDefinitionId", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentConfirmation",
                schema: "Agent");

            migrationBuilder.DropTable(
                name: "Attachment",
                schema: "Document");

            migrationBuilder.DropTable(
                name: "AuditEvent",
                schema: "Audit");

            migrationBuilder.DropTable(
                name: "BusinessObjectRelation",
                schema: "BusinessObject");

            migrationBuilder.DropTable(
                name: "BusinessObjectVersion",
                schema: "BusinessObject");

            migrationBuilder.DropTable(
                name: "ConfigurationVersion",
                schema: "Configuration");

            migrationBuilder.DropTable(
                name: "ConnectorReceipt",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "DataScope",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "DeadLetter",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "DocumentVersion",
                schema: "Document");

            migrationBuilder.DropTable(
                name: "EvidenceAsset",
                schema: "Audit");

            migrationBuilder.DropTable(
                name: "FieldDefinition",
                schema: "Metadata");

            migrationBuilder.DropTable(
                name: "ObjectRelation",
                schema: "Metadata");

            migrationBuilder.DropTable(
                name: "Reconciliation",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "RolePermission",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Signature",
                schema: "Document");

            migrationBuilder.DropTable(
                name: "UserRole",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "WorkflowTaskAction",
                schema: "Workflow");

            migrationBuilder.DropTable(
                name: "WorkflowTaskCandidate",
                schema: "Workflow");

            migrationBuilder.DropTable(
                name: "AgentRun",
                schema: "Agent");

            migrationBuilder.DropTable(
                name: "ConnectorCommand",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "OutboxEvent",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "Permission",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "WorkflowTask",
                schema: "Workflow");

            migrationBuilder.DropTable(
                name: "AgentProfile",
                schema: "Agent");

            migrationBuilder.DropTable(
                name: "Connector",
                schema: "Integration");

            migrationBuilder.DropTable(
                name: "User",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "WorkflowInstance",
                schema: "Workflow");

            migrationBuilder.DropTable(
                name: "Organization",
                schema: "Identity");

            migrationBuilder.DropTable(
                name: "BusinessObject",
                schema: "BusinessObject");

            migrationBuilder.DropTable(
                name: "WorkflowVersion",
                schema: "Workflow");

            migrationBuilder.DropTable(
                name: "ObjectType",
                schema: "Metadata");

            migrationBuilder.DropTable(
                name: "WorkflowDefinition",
                schema: "Workflow");
        }
    }
}
