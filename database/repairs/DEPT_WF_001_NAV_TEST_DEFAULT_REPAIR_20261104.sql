SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE [Navigation].[NavigationWorkspace]
SET IsDefault = 0,
    Status = CASE WHEN IsTestData = 1 THEN N'Retired' ELSE Status END,
    UpdatedAt = SYSUTCDATETIME()
WHERE IsTestData = 1;

UPDATE workspace
SET IsDefault = 1,
    Status = N'Published',
    IsTestData = 0,
    UpdatedAt = SYSUTCDATETIME()
FROM [Navigation].[NavigationWorkspace] workspace
JOIN [Identity].[Department] department ON department.Id = workspace.DepartmentId
WHERE workspace.WorkspaceKey = CONCAT(N'DEPT-', department.Code)
  AND department.IsActive = 1
  AND department.Code IN (N'FINANCE', N'SUPPLY', N'PRODUCTION', N'PROJECT', N'RND', N'QA', N'DELIVERY');

COMMIT TRANSACTION;

SELECT
    department.Code,
    workspace.WorkspaceKey,
    workspace.Status,
    workspace.IsDefault,
    workspace.IsTestData
FROM [Identity].[Department] department
JOIN [Navigation].[NavigationWorkspace] workspace
    ON workspace.DepartmentId = department.Id
   AND workspace.IsDefault = 1
   AND workspace.Status = 'Published'
   AND workspace.IsTestData = 0
WHERE department.IsActive = 1
  AND department.Code IN (N'FINANCE', N'SUPPLY', N'PRODUCTION', N'PROJECT', N'RND', N'QA', N'DELIVERY')
ORDER BY department.Code;
