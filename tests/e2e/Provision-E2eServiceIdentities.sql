SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Suffix nvarchar(40) = N'$(E2eSuffix)';
DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @OrganizationId uniqueidentifier =
(
    SELECT TOP (1) Id
    FROM [Identity].[Organization]
    WHERE IsActive = 1
    ORDER BY CreatedAt, Id
);
DECLARE @DepartmentId uniqueidentifier =
(
    SELECT TOP (1) Id
    FROM [Identity].[Department]
    WHERE OrganizationId = @OrganizationId
      AND Code = N'RND'
      AND IsActive = 1
);
DECLARE @ServiceRoleId uniqueidentifier =
(
    SELECT TOP (1) Id
    FROM [Identity].[Role]
    WHERE Code = N'agent-service'
      AND IsActive = 1
);

IF @OrganizationId IS NULL OR @DepartmentId IS NULL OR @ServiceRoleId IS NULL
    THROW 51000, N'E2E 服务身份夹具缺少组织、研发部门或 agent-service 角色。', 1;

DECLARE @Accounts TABLE
(
    Id uniqueidentifier NOT NULL,
    Tier nvarchar(16) NOT NULL,
    UserName nvarchar(100) NOT NULL,
    DisplayName nvarchar(128) NOT NULL
);

INSERT INTO @Accounts (Id, Tier, UserName, DisplayName)
VALUES
    (NEWID(), N'LowRisk', CONCAT(N'e2e.lowrisk.', @Suffix), CONCAT(N'E2E 低风险数字员工服务账号 ', @Suffix)),
    (NEWID(), N'HighRisk', CONCAT(N'e2e.highrisk.', @Suffix), CONCAT(N'E2E 高风险数字员工服务账号 ', @Suffix));

BEGIN TRANSACTION;

INSERT INTO [Identity].[User]
    (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt, Version)
SELECT
    Id,
    @OrganizationId,
    UserName,
    DisplayName,
    1,
    1,
    @Now,
    1
FROM @Accounts;

INSERT INTO [Identity].[UserRole] (UserId, RoleId)
SELECT Id, @ServiceRoleId
FROM @Accounts;

INSERT INTO [Identity].[UserDepartmentMembership]
    (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
SELECT Id, @DepartmentId, 1, 0, 1, @Now
FROM @Accounts;

INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
SELECT NEWID(), Id, N'organization', CONVERT(nvarchar(36), @OrganizationId)
FROM @Accounts
UNION ALL
SELECT NEWID(), Id, N'department', CONVERT(nvarchar(36), @DepartmentId)
FROM @Accounts;

COMMIT TRANSACTION;

SELECT Tier, UserName, DisplayName, CONVERT(nvarchar(36), Id) AS UserId
FROM @Accounts
ORDER BY Tier DESC;
