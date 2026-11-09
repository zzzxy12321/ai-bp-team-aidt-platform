SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRANSACTION;

DECLARE @AdminUserId uniqueidentifier =
    (SELECT Id FROM [Identity].[User] WHERE UserName = N'demo.admin');
DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @EventBefore int, @EventRepaired int;
DECLARE @ProjectBefore int, @ProjectRepaired int;
DECLARE @AttachmentRelationBefore int, @AttachmentRelationRepaired int;

SELECT @EventBefore = COUNT(*)
FROM [PMO].[EventInbox] inbox
JOIN [PMO].[EventSuggestion] suggestion ON suggestion.EventInboxId = inbox.Id
WHERE inbox.Status = N'Received'
  AND suggestion.Status IN (N'Suggested', N'HighRisk', N'Confirmed', N'Rejected');

;WITH DerivedEventStatus AS
(
    SELECT
        inbox.Id,
        CASE suggestion.Status
            WHEN N'Confirmed' THEN N'Confirmed'
            WHEN N'Rejected' THEN N'Rejected'
            ELSE N'AwaitingConfirmation'
        END AS DerivedStatus,
        suggestion.AgentRunId
    FROM [PMO].[EventInbox] inbox
    JOIN [PMO].[EventSuggestion] suggestion ON suggestion.EventInboxId = inbox.Id
    WHERE inbox.Status = N'Received'
      AND suggestion.Status IN (N'Suggested', N'HighRisk', N'Confirmed', N'Rejected')
)
UPDATE inbox
SET
    inbox.Status = derived.DerivedStatus,
    inbox.AnalysisRunId = COALESCE(inbox.AnalysisRunId, derived.AgentRunId),
    inbox.UpdatedAt = @Now
FROM [PMO].[EventInbox] inbox
JOIN DerivedEventStatus derived ON derived.Id = inbox.Id;
SET @EventRepaired = @@ROWCOUNT;

SELECT @ProjectBefore = COUNT(*)
FROM
(
    SELECT
        project.Id,
        project.Status AS CurrentStatus,
        LTRIM(RTRIM(afterEvent.AfterJson)) AS AuditedStatus
    FROM [BusinessObject].[BusinessObject] project
    JOIN [Metadata].[ObjectType] objectType ON objectType.Id = project.ObjectTypeId
    CROSS APPLY
    (
        SELECT TOP (1) audit.BeforeJson, audit.AfterJson
        FROM [Audit].[AuditEvent] audit
        WHERE audit.EntityType = N'Project'
          AND audit.EntityId = CONVERT(nvarchar(36), project.Id)
          AND audit.Action = N'project.status_changed'
          AND audit.Outcome = N'Succeeded'
        ORDER BY audit.OccurredAt DESC, audit.Id DESC
    ) afterEvent
    WHERE objectType.Code = N'PROJECT'
      AND LTRIM(RTRIM(afterEvent.BeforeJson)) = project.Status
      AND LTRIM(RTRIM(afterEvent.AfterJson)) IN
          (N'Draft', N'Active', N'Blocked', N'AwaitingAcceptance', N'Completed', N'Terminated')
      AND project.Status <> LTRIM(RTRIM(afterEvent.AfterJson))
) candidates;

DECLARE @ProjectRepairs TABLE
(
    Id uniqueidentifier NOT NULL,
    Version bigint NOT NULL,
    Status nvarchar(32) NOT NULL,
    OrganizationId uniqueidentifier NOT NULL,
    DepartmentId uniqueidentifier NOT NULL,
    EnvironmentCode nvarchar(16) NOT NULL,
    IsTestData bit NOT NULL,
    SourceSystem nvarchar(64) NOT NULL,
    UpdatedAt datetime2(3) NOT NULL
);

;WITH LatestStatusAudit AS
(
    SELECT
        project.Id AS ProjectId,
        project.Status AS CurrentStatus,
        LTRIM(RTRIM(audit.BeforeJson)) AS BeforeStatus,
        LTRIM(RTRIM(audit.AfterJson)) AS AfterStatus,
        ROW_NUMBER() OVER
            (PARTITION BY project.Id ORDER BY audit.OccurredAt DESC, audit.Id DESC) AS RowNumber
    FROM [BusinessObject].[BusinessObject] project
    JOIN [Metadata].[ObjectType] objectType ON objectType.Id = project.ObjectTypeId
    JOIN [Audit].[AuditEvent] audit
        ON audit.EntityType = N'Project'
        AND audit.EntityId = CONVERT(nvarchar(36), project.Id)
        AND audit.Action = N'project.status_changed'
        AND audit.Outcome = N'Succeeded'
    WHERE objectType.Code = N'PROJECT'
)
UPDATE project
SET
    project.Status = audit.AfterStatus,
    project.Version = project.Version + 1,
    project.UpdatedAt = @Now
OUTPUT
    inserted.Id,
    inserted.Version,
    inserted.Status,
    inserted.OrganizationId,
    inserted.DepartmentId,
    inserted.EnvironmentCode,
    inserted.IsTestData,
    inserted.SourceSystem,
    inserted.UpdatedAt
INTO @ProjectRepairs
FROM [BusinessObject].[BusinessObject] project
JOIN LatestStatusAudit audit
    ON audit.ProjectId = project.Id
    AND audit.RowNumber = 1
WHERE audit.BeforeStatus = project.Status
  AND audit.AfterStatus IN
      (N'Draft', N'Active', N'Blocked', N'AwaitingAcceptance', N'Completed', N'Terminated')
  AND project.Status <> audit.AfterStatus;
SET @ProjectRepaired = @@ROWCOUNT;

IF @ProjectRepaired > 0
BEGIN
    INSERT INTO [BusinessObject].[BusinessObjectVersion]
        (BusinessObjectId, Version, SnapshotJson, ActorId, Reason, CreatedAt)
    SELECT
        repaired.Id,
        repaired.Version,
        (
            SELECT
                project.ObjectCode,
                project.Title,
                project.ObjectTypeId,
                project.OrganizationId,
                project.DepartmentId,
                repaired.Status,
                repaired.Version,
                repaired.EnvironmentCode,
                repaired.IsTestData,
                repaired.SourceSystem
            FROM [BusinessObject].[BusinessObject] project
            WHERE project.Id = repaired.Id
            FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
        ),
        @AdminUserId,
        N'DataRepair:Batch3Closeout:project.status_changed',
        repaired.UpdatedAt
    FROM @ProjectRepairs repaired;

    UPDATE profile
    SET
        profile.ProgressPercent = 100,
        profile.UpdatedAt = repaired.UpdatedAt
    FROM [PMO].[ProjectProfile] profile
    JOIN @ProjectRepairs repaired ON repaired.Id = profile.BusinessObjectId
    WHERE repaired.Status = N'Completed';
END;

SELECT @AttachmentRelationBefore = COUNT(*)
FROM [Document].[Attachment] attachment
JOIN [BusinessObject].[BusinessObject] project ON project.Id = attachment.BusinessObjectId
JOIN [Metadata].[ObjectType] objectType ON objectType.Id = project.ObjectTypeId
WHERE objectType.Code = N'PROJECT'
  AND NOT EXISTS
  (
      SELECT 1
      FROM [PMO].[ProjectRelation] relation
      WHERE relation.ProjectId = project.Id
        AND relation.EntityType = N'Attachment'
        AND relation.EntityId = attachment.Id
        AND relation.RelationCode = N'SupportingEvidence'
  );

INSERT INTO [PMO].[ProjectRelation]
    (Id, ProjectId, EntityType, EntityId, RelationCode, EvidenceExcerpt, CreatedBy, CreatedAt)
SELECT
    NEWID(),
    attachment.BusinessObjectId,
    N'Attachment',
    attachment.Id,
    N'SupportingEvidence',
    N'DataRepair:Batch3Closeout:auto project file evidence',
    attachment.CreatedBy,
    @Now
FROM [Document].[Attachment] attachment
JOIN [BusinessObject].[BusinessObject] project ON project.Id = attachment.BusinessObjectId
JOIN [Metadata].[ObjectType] objectType ON objectType.Id = project.ObjectTypeId
WHERE objectType.Code = N'PROJECT'
  AND attachment.Status = N'Active'
  AND NOT EXISTS
  (
      SELECT 1
      FROM [PMO].[ProjectRelation] relation
      WHERE relation.ProjectId = project.Id
        AND relation.EntityType = N'Attachment'
        AND relation.EntityId = attachment.Id
        AND relation.RelationCode = N'SupportingEvidence'
  );
SET @AttachmentRelationRepaired = @@ROWCOUNT;

IF @EventRepaired + @ProjectRepaired + @AttachmentRelationRepaired > 0
BEGIN
    INSERT INTO [Audit].[AuditEvent]
        (Id, ActorId, Module, Action, EntityType, EntityId, TraceId, Outcome,
         BeforeJson, AfterJson, Reason, IpHash, OccurredAt)
    SELECT
        NEWID(), @AdminUserId, N'PMO', N'batch3.closeout_data_repair',
        N'DataRepair', N'BATCH3-CLOSEOUT-20261101', N'batch3-closeout-repair',
        N'Succeeded', NULL,
        N'{"repair":"Batch3Closeout","scope":"events,projects,attachments"}',
        N'Controlled repair: derive only from EventSuggestion and explicit successful status audit evidence.',
        NULL, @Now;
END;

COMMIT TRANSACTION;
