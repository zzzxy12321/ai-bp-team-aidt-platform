BEGIN TRANSACTION;
IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON; SET ANSI_PADDING ON; SET ANSI_WARNINGS ON; SET CONCAT_NULL_YIELDS_NULL ON; SET NUMERIC_ROUNDABORT OFF;
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT [FK_BusinessObject_CreatedByUser];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DROP TABLE [PMO].[EventSuggestion];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DROP TABLE [PMO].[ProjectMember];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DROP TABLE [PMO].[ProjectMilestone];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DROP TABLE [PMO].[ProjectProfile];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DROP TABLE [PMO].[ProjectRelation];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DROP TABLE [PMO].[ProjectTask];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DROP TABLE [PMO].[EventInbox];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DECLARE @ProjectTypeId uniqueidentifier =
        (SELECT Id FROM [Metadata].[ObjectType] WHERE Code = N'PROJECT');
    IF @ProjectTypeId IS NOT NULL
    BEGIN
        DELETE candidate
        FROM [Workflow].[WorkflowTaskCandidate] candidate
        JOIN [Workflow].[WorkflowTask] task ON task.Id = candidate.WorkflowTaskId
        JOIN [Workflow].[WorkflowInstance] instance ON instance.Id = task.WorkflowInstanceId
        JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
        WHERE businessObject.ObjectTypeId = @ProjectTypeId;

        DELETE notification
        FROM [Workflow].[WorkflowNotification] notification
        JOIN [Workflow].[WorkflowTask] task ON task.Id = notification.WorkflowTaskId
        JOIN [Workflow].[WorkflowInstance] instance ON instance.Id = task.WorkflowInstanceId
        JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
        WHERE businessObject.ObjectTypeId = @ProjectTypeId;

        DELETE taskAction
        FROM [Workflow].[WorkflowTaskAction] taskAction
        JOIN [Workflow].[WorkflowTask] task ON task.Id = taskAction.WorkflowTaskId
        JOIN [Workflow].[WorkflowInstance] instance ON instance.Id = task.WorkflowInstanceId
        JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
        WHERE businessObject.ObjectTypeId = @ProjectTypeId;

        DELETE task
        FROM [Workflow].[WorkflowTask] task
        JOIN [Workflow].[WorkflowInstance] instance ON instance.Id = task.WorkflowInstanceId
        JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
        WHERE businessObject.ObjectTypeId = @ProjectTypeId;

        DELETE instance
        FROM [Workflow].[WorkflowInstance] instance
        JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = instance.BusinessObjectId
        WHERE businessObject.ObjectTypeId = @ProjectTypeId;

        UPDATE attachment
        SET BusinessObjectId = NULL
        FROM [Document].[Attachment] attachment
        JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = attachment.BusinessObjectId
        WHERE businessObject.ObjectTypeId = @ProjectTypeId;

        DELETE relation
        FROM [BusinessObject].[BusinessObjectRelation] relation
        WHERE relation.SourceObjectId IN (
                  SELECT Id FROM [BusinessObject].[BusinessObject] WHERE ObjectTypeId = @ProjectTypeId)
           OR relation.TargetObjectId IN (
                  SELECT Id FROM [BusinessObject].[BusinessObject] WHERE ObjectTypeId = @ProjectTypeId);

        DELETE versionRow
        FROM [BusinessObject].[BusinessObjectVersion] versionRow
        JOIN [BusinessObject].[BusinessObject] businessObject ON businessObject.Id = versionRow.BusinessObjectId
        WHERE businessObject.ObjectTypeId = @ProjectTypeId;

        DELETE FROM [BusinessObject].[BusinessObject] WHERE ObjectTypeId = @ProjectTypeId;
        DELETE FROM [Metadata].[FieldDefinition] WHERE ObjectTypeId = @ProjectTypeId;
        DELETE FROM [Metadata].[ObjectType] WHERE Id = @ProjectTypeId;
    END;

    DELETE rolePermission
    FROM [Identity].[RolePermission] rolePermission
    LEFT JOIN [Identity].[Role] roleRow ON roleRow.Id = rolePermission.RoleId
    LEFT JOIN [Identity].[Permission] permissionRow ON permissionRow.Id = rolePermission.PermissionId
    WHERE roleRow.Code = N'project-manager'
       OR permissionRow.Code IN (
        N'project-read', N'project-create', N'project-update',
        N'project-member-read', N'project-member-manage',
        N'milestone-read', N'milestone-manage',
        N'event-inbox-read', N'event-inbox-create', N'event-inbox-update', N'event-inbox-confirm',
        N'project-task-read', N'project-task-create', N'project-task-update',
        N'project-audit-read');

    DELETE FROM [Identity].[DataScope] WHERE UserId = '10000000-0000-0000-0000-000000000021';
    DELETE FROM [Identity].[UserDepartmentMembership] WHERE UserId = '10000000-0000-0000-0000-000000000021';
    DELETE FROM [Identity].[UserRole] WHERE UserId = '10000000-0000-0000-0000-000000000021';
    DELETE FROM [Agent].[AgentProfile] WHERE Name = N'项目事件建议数字员工';
    DELETE FROM [Identity].[UserRole] WHERE UserId = '10000000-0000-0000-0000-000000000022';
    DELETE FROM [Identity].[User] WHERE UserName = N'agent.project';
    DELETE FROM [Identity].[User] WHERE UserName = N'demo.pm';
    DELETE FROM [Identity].[Permission]
    WHERE Code IN (
        N'project-read', N'project-create', N'project-update',
        N'project-member-read', N'project-member-manage',
        N'milestone-read', N'milestone-manage',
        N'event-inbox-read', N'event-inbox-create', N'event-inbox-update', N'event-inbox-confirm',
        N'project-task-read', N'project-task-create', N'project-task-update',
        N'project-audit-read');
    DELETE FROM [Identity].[Role] WHERE Code = N'project-manager';
    DELETE FROM [Identity].[Role] WHERE Code = N'project-event-agent';
    IF SCHEMA_ID(N'PMO') IS NOT NULL
        EXEC(N'DROP SCHEMA [PMO]');
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DROP INDEX [IX_BusinessObject_CreatedByUserId] ON [BusinessObject].[BusinessObject];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessObject].[BusinessObject]') AND [c].[name] = N'CreatedByUserId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [BusinessObject].[BusinessObject] DROP COLUMN [CreatedByUserId];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BusinessObject].[BusinessObject]') AND [c].[name] = N'Title');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [BusinessObject].[BusinessObject] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [BusinessObject].[BusinessObject] DROP COLUMN [Title];
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    UPDATE [BusinessObject].[BusinessObject]
    SET [Status] = N'Archived'
    WHERE [Status] IN (N'Blocked', N'AwaitingAcceptance', N'Completed', N'Terminated');
END;

IF EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks'
)
BEGIN
    DELETE FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261031165645_Batch007ProjectInboxTasks';
END;

COMMIT;
GO
