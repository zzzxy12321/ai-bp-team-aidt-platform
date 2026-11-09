BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030004100_WorkflowPersistenceAndSecurity'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD [OrganizationId] uniqueidentifier NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030004100_WorkflowPersistenceAndSecurity'
)
BEGIN
    CREATE TABLE [Integration].[IdempotencyRecord] (
        [Id] uniqueidentifier NOT NULL,
        [Scope] nvarchar(128) NOT NULL,
        [IdempotencyKey] nvarchar(128) NOT NULL,
        [RequestHash] nvarchar(128) NOT NULL,
        [ResponseType] nvarchar(128) NOT NULL,
        [ResponseId] nvarchar(128) NOT NULL,
        [Status] nvarchar(32) NOT NULL,
        [CreatedAt] datetime2(3) NOT NULL,
        [ExpiresAt] datetime2(3) NOT NULL,
        CONSTRAINT [PK_IdempotencyRecord] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030004100_WorkflowPersistenceAndSecurity'
)
BEGIN
    CREATE INDEX [IX_BusinessObject_Organization_UpdatedAt_Id] ON [BusinessObject].[BusinessObject] ([OrganizationId], [UpdatedAt], [Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030004100_WorkflowPersistenceAndSecurity'
)
BEGIN
    CREATE INDEX [IX_IdempotencyRecord_ExpiresAt] ON [Integration].[IdempotencyRecord] ([ExpiresAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030004100_WorkflowPersistenceAndSecurity'
)
BEGIN
    CREATE UNIQUE INDEX [UX_IdempotencyRecord_Scope_Key] ON [Integration].[IdempotencyRecord] ([Scope], [IdempotencyKey]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030004100_WorkflowPersistenceAndSecurity'
)
BEGIN
    ALTER TABLE [BusinessObject].[BusinessObject] ADD CONSTRAINT [FK_BusinessObject_Organization] FOREIGN KEY ([OrganizationId]) REFERENCES [Identity].[Organization] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261030004100_WorkflowPersistenceAndSecurity'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261030004100_WorkflowPersistenceAndSecurity', N'10.0.14');
END;

COMMIT;
GO

