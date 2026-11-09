BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    DROP TABLE [Scenario].[ScenarioLaunch];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    DROP TABLE [Scenario].[ScenarioVersion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    DROP TABLE [Scenario].[ScenarioDefinition];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM [Workflow].[WorkflowInstance] instanceRow
        JOIN [Workflow].[WorkflowDefinition] definitionRow
            ON definitionRow.Id = instanceRow.WorkflowDefinitionId
        WHERE definitionRow.Code = N'FINANCE_PAYMENT_REVIEW')
    BEGIN
        DELETE versionRow
        FROM [Workflow].[WorkflowVersion] versionRow
        JOIN [Workflow].[WorkflowDefinition] definitionRow
            ON definitionRow.Id = versionRow.WorkflowDefinitionId
        WHERE definitionRow.Code = N'FINANCE_PAYMENT_REVIEW';
        DELETE FROM [Workflow].[WorkflowDefinition]
        WHERE Code = N'FINANCE_PAYMENT_REVIEW';
    END;

    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Id = rolePermission.PermissionId
    WHERE permissionRow.Resource = N'scenario';
    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    JOIN [Identity].[Role] roleRow
        ON roleRow.Id = rolePermission.RoleId
    JOIN [Identity].[Permission] permissionRow
        ON permissionRow.Id = rolePermission.PermissionId
    WHERE roleRow.Code = N'quality-engineer'
      AND permissionRow.Code IN (N'business-object-create', N'workflow-instance-submit');
    DELETE FROM [Identity].[Permission] WHERE Resource = N'scenario';
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    ALTER TABLE [Document].[AttachmentLink] DROP CONSTRAINT [CK_AttachmentLink_EntityType];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    EXEC(N'ALTER TABLE [Document].[AttachmentLink] ADD CONSTRAINT [CK_AttachmentLink_EntityType] CHECK ([EntityType] IN (''Department'',''BusinessObject'',''WorkflowInstance'',''WorkflowTask''))');
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    ALTER TABLE [Identity].[AccessChangeRequest] DROP CONSTRAINT [CK_AccessChangeRequest_Type];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    EXEC(N'ALTER TABLE [Identity].[AccessChangeRequest] ADD CONSTRAINT [CK_AccessChangeRequest_Type] CHECK ([RequestType] IN (''RoleAssignment'',''RolePermissions'',''AgentProfilePublish''))');
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261103043627_017_ScenarioCatalogBackend';
END;

COMMIT;
GO

