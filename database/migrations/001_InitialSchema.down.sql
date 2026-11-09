BEGIN TRANSACTION;
DROP TABLE [Agent].[AgentConfirmation];

DROP TABLE [Document].[Attachment];

DROP TABLE [Audit].[AuditEvent];

DROP TABLE [BusinessObject].[BusinessObjectRelation];

DROP TABLE [BusinessObject].[BusinessObjectVersion];

DROP TABLE [Configuration].[ConfigurationVersion];

DROP TABLE [Integration].[ConnectorReceipt];

DROP TABLE [Identity].[DataScope];

DROP TABLE [Integration].[DeadLetter];

DROP TABLE [Document].[DocumentVersion];

DROP TABLE [Audit].[EvidenceAsset];

DROP TABLE [Metadata].[FieldDefinition];

DROP TABLE [Metadata].[ObjectRelation];

DROP TABLE [Integration].[Reconciliation];

DROP TABLE [Identity].[RolePermission];

DROP TABLE [Document].[Signature];

DROP TABLE [Identity].[UserRole];

DROP TABLE [Workflow].[WorkflowTaskAction];

DROP TABLE [Workflow].[WorkflowTaskCandidate];

DROP TABLE [Agent].[AgentRun];

DROP TABLE [Integration].[ConnectorCommand];

DROP TABLE [Integration].[OutboxEvent];

DROP TABLE [Identity].[Permission];

DROP TABLE [Identity].[Role];

DROP TABLE [Workflow].[WorkflowTask];

DROP TABLE [Agent].[AgentProfile];

DROP TABLE [Integration].[Connector];

DROP TABLE [Identity].[User];

DROP TABLE [Workflow].[WorkflowInstance];

DROP TABLE [Identity].[Organization];

DROP TABLE [BusinessObject].[BusinessObject];

DROP TABLE [Workflow].[WorkflowVersion];

DROP TABLE [Metadata].[ObjectType];

DROP TABLE [Workflow].[WorkflowDefinition];

DELETE FROM [__EFMigrationsHistory]
WHERE [MigrationId] = N'20261029170022_InitialSchema';

COMMIT;
GO

