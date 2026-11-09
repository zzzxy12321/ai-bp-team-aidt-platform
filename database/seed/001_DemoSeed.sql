SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Now datetime2(3) = SYSUTCDATETIME();
DECLARE @OrgId uniqueidentifier = '10000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM [Identity].[Organization] WHERE Id = @OrgId)
BEGIN
    INSERT INTO [Identity].[Organization] (Id, Code, Name, IsActive, CreatedAt)
    VALUES (@OrgId, N'PSD-DEMO', N'普赛达 AIDT Demo 组织', 1, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE Id = '10000000-0000-0000-0000-000000000010')
    INSERT INTO [Identity].[User] (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
    VALUES ('10000000-0000-0000-0000-000000000010', @OrgId, N'demo.admin', N'平台管理员', 0, 1, @Now),
           ('10000000-0000-0000-0000-000000000011', @OrgId, N'demo.researcher', N'研发工程师', 0, 1, @Now),
           ('10000000-0000-0000-0000-000000000012', @OrgId, N'demo.qa', N'质量工程师', 0, 1, @Now),
           ('10000000-0000-0000-0000-000000000013', @OrgId, N'demo.supply', N'供应链专员', 0, 1, @Now),
           ('10000000-0000-0000-0000-000000000014', @OrgId, N'demo.finance', N'财务审批人', 0, 1, @Now),
           ('10000000-0000-0000-0000-000000000015', @OrgId, N'demo.production', N'生产班组', 0, 1, @Now);

IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE Id = '10000000-0000-0000-0000-000000000016')
    INSERT INTO [Identity].[User] (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
    VALUES ('10000000-0000-0000-0000-000000000016', @OrgId, N'demo.noscope', N'无组织范围测试身份', 0, 1, @Now);

IF NOT EXISTS (SELECT 1 FROM [Identity].[User] WHERE Id = '10000000-0000-0000-0000-000000000019')
    INSERT INTO [Identity].[User] (Id, OrganizationId, UserName, DisplayName, IsServiceIdentity, IsActive, CreatedAt)
    VALUES ('10000000-0000-0000-0000-000000000019', @OrgId, N'agent.demo', N'业务协同数字员工', 1, 1, @Now);

IF NOT EXISTS (SELECT 1 FROM [Identity].[Role] WHERE Code = N'platform-admin')
    INSERT INTO [Identity].[Role] (Id, Code, Name)
    VALUES ('20000000-0000-0000-0000-000000000001', N'platform-admin', N'平台管理员'),
           ('20000000-0000-0000-0000-000000000002', N'rd-engineer', N'研发工程师'),
           ('20000000-0000-0000-0000-000000000003', N'quality-engineer', N'质量工程师'),
           ('20000000-0000-0000-0000-000000000004', N'supply-specialist', N'供应链专员'),
           ('20000000-0000-0000-0000-000000000005', N'finance-approver', N'财务审批人'),
           ('20000000-0000-0000-0000-000000000006', N'production-team', N'生产班组');

IF NOT EXISTS (SELECT 1 FROM [Identity].[Permission] WHERE Code = N'dashboard-read')
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    VALUES
      ('30000000-0000-0000-0000-000000000001', N'dashboard-read', N'dashboard', N'Read'),
      ('30000000-0000-0000-0000-000000000002', N'platform-read', N'platform', N'Read'),
      ('30000000-0000-0000-0000-000000000003', N'object-type-read', N'object-type', N'Read'),
      ('30000000-0000-0000-0000-000000000004', N'business-object-read', N'business-object', N'Read'),
      ('30000000-0000-0000-0000-000000000005', N'business-object-create', N'business-object', N'Create'),
      ('30000000-0000-0000-0000-000000000006', N'business-object-update', N'business-object', N'Update'),
      ('30000000-0000-0000-0000-000000000007', N'workflow-instance-read', N'workflow-instance', N'Read'),
      ('30000000-0000-0000-0000-000000000008', N'workflow-instance-submit', N'workflow-instance', N'Submit'),
      ('30000000-0000-0000-0000-000000000009', N'workflow-task-read', N'workflow-task', N'Read'),
      ('30000000-0000-0000-0000-000000000010', N'workflow-task-approve', N'workflow-task', N'Approve'),
      ('30000000-0000-0000-0000-000000000011', N'workflow-task-return', N'workflow-task', N'Return'),
      ('30000000-0000-0000-0000-000000000012', N'workflow-task-terminate', N'workflow-task', N'Terminate'),
      ('30000000-0000-0000-0000-000000000013', N'audit-read', N'audit', N'Read'),
      ('30000000-0000-0000-0000-000000000014', N'agent-read', N'agent', N'Read'),
      ('30000000-0000-0000-0000-000000000015', N'agent-run-read', N'agent-run', N'Read'),
      ('30000000-0000-0000-0000-000000000016', N'agent-run-approve', N'agent-run', N'Approve'),
      ('30000000-0000-0000-0000-000000000017', N'connector-read', N'connector', N'Read'),
      ('30000000-0000-0000-0000-000000000018', N'configuration-read', N'configuration', N'Read');

IF NOT EXISTS (SELECT 1 FROM [Identity].[Permission] WHERE Code = N'agent-run-submit')
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    VALUES ('30000000-0000-0000-0000-000000000019', N'agent-run-submit', N'agent-run', N'Submit');
IF NOT EXISTS (SELECT 1 FROM [Identity].[Permission] WHERE Code = N'agent-run-stop')
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    VALUES ('30000000-0000-0000-0000-000000000020', N'agent-run-stop', N'agent-run', N'Stop');
IF NOT EXISTS (SELECT 1 FROM [Identity].[Permission] WHERE Code = N'department-read')
    INSERT INTO [Identity].[Permission] (Id, Code, Resource, Action)
    VALUES
      ('30000000-0000-0000-0000-000000000021', N'department-read', N'department', N'Read'),
      ('30000000-0000-0000-0000-000000000022', N'department-create', N'department', N'Create'),
      ('30000000-0000-0000-0000-000000000023', N'department-update', N'department', N'Update'),
      ('30000000-0000-0000-0000-000000000024', N'file-read', N'file', N'Read'),
      ('30000000-0000-0000-0000-000000000025', N'file-create', N'file', N'Create'),
      ('30000000-0000-0000-0000-000000000026', N'file-update', N'file', N'Update'),
      ('30000000-0000-0000-0000-000000000027', N'file-delete', N'file', N'Delete');

INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
SELECT roleRow.RoleId, permissionRow.Id
FROM (VALUES
    ('20000000-0000-0000-0000-000000000001'),
    ('20000000-0000-0000-0000-000000000002'),
    ('20000000-0000-0000-0000-000000000003'),
    ('20000000-0000-0000-0000-000000000004')) AS roleRow(RoleId)
CROSS JOIN [Identity].[Permission] permissionRow
WHERE permissionRow.Code = N'agent-run-submit'
  AND NOT EXISTS (
      SELECT 1 FROM [Identity].[RolePermission] existing
      WHERE existing.RoleId = roleRow.RoleId AND existing.PermissionId = permissionRow.Id
  );

IF NOT EXISTS (
    SELECT 1 FROM [Identity].[RolePermission]
    WHERE RoleId = '20000000-0000-0000-0000-000000000001'
      AND PermissionId = '30000000-0000-0000-0000-000000000020'
)
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    VALUES ('20000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000020');

IF NOT EXISTS (SELECT 1 FROM [Identity].[UserRole] WHERE UserId = '10000000-0000-0000-0000-000000000010')
BEGIN
    INSERT INTO [Identity].[UserRole] (UserId, RoleId) VALUES
      ('10000000-0000-0000-0000-000000000010', '20000000-0000-0000-0000-000000000001'),
      ('10000000-0000-0000-0000-000000000011', '20000000-0000-0000-0000-000000000002'),
      ('10000000-0000-0000-0000-000000000012', '20000000-0000-0000-0000-000000000003'),
      ('10000000-0000-0000-0000-000000000013', '20000000-0000-0000-0000-000000000004'),
      ('10000000-0000-0000-0000-000000000014', '20000000-0000-0000-0000-000000000005'),
      ('10000000-0000-0000-0000-000000000015', '20000000-0000-0000-0000-000000000006');
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[RolePermission] WHERE RoleId = '20000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT '20000000-0000-0000-0000-000000000001', Id FROM [Identity].[Permission];
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[RolePermission] WHERE RoleId = '20000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT '20000000-0000-0000-0000-000000000002', Id
    FROM [Identity].[Permission]
    WHERE Code IN (N'dashboard-read', N'platform-read', N'object-type-read', N'business-object-read',
                   N'business-object-create', N'workflow-instance-read', N'workflow-instance-submit',
                   N'workflow-task-read', N'workflow-task-approve', N'workflow-task-return',
                   N'audit-read', N'agent-read', N'department-read',
                   N'file-read', N'file-create', N'file-update', N'file-delete');
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[RolePermission] WHERE RoleId = '20000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT '20000000-0000-0000-0000-000000000003', Id
    FROM [Identity].[Permission]
    WHERE Code IN (N'dashboard-read', N'object-type-read', N'business-object-read',
                   N'workflow-instance-read', N'workflow-task-read', N'workflow-task-approve',
                   N'workflow-task-return', N'workflow-task-terminate', N'audit-read', N'agent-read',
                   N'department-read', N'file-read', N'file-create', N'file-update', N'file-delete');
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[RolePermission] WHERE RoleId = '20000000-0000-0000-0000-000000000004')
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT '20000000-0000-0000-0000-000000000004', Id
    FROM [Identity].[Permission]
    WHERE Code IN (N'dashboard-read', N'object-type-read', N'business-object-read', N'business-object-create',
                   N'workflow-instance-read', N'workflow-instance-submit', N'workflow-task-read',
                   N'workflow-task-approve', N'workflow-task-return', N'audit-read', N'connector-read',
                   N'department-read', N'file-read', N'file-create', N'file-update', N'file-delete');
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[RolePermission] WHERE RoleId = '20000000-0000-0000-0000-000000000005')
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT '20000000-0000-0000-0000-000000000005', Id
    FROM [Identity].[Permission]
    WHERE Code IN (N'dashboard-read', N'business-object-read', N'workflow-instance-read',
                   N'workflow-task-read', N'workflow-task-approve', N'workflow-task-return',
                   N'audit-read', N'department-read', N'file-read', N'file-create',
                   N'file-update', N'file-delete');
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[RolePermission] WHERE RoleId = '20000000-0000-0000-0000-000000000006')
BEGIN
    INSERT INTO [Identity].[RolePermission] (RoleId, PermissionId)
    SELECT '20000000-0000-0000-0000-000000000006', Id
    FROM [Identity].[Permission]
    WHERE Code IN (N'dashboard-read', N'business-object-read', N'workflow-instance-read',
                   N'workflow-task-read', N'workflow-task-approve', N'workflow-task-return',
                   N'audit-read', N'department-read', N'file-read', N'file-create',
                   N'file-update', N'file-delete');
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[DataScope] WHERE UserId = '10000000-0000-0000-0000-000000000010')
BEGIN
    INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
    SELECT NEWID(), Id, N'organization', CONVERT(nvarchar(36), @OrgId)
    FROM [Identity].[User]
    WHERE OrganizationId = @OrgId;
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[Department] WHERE OrganizationId = @OrgId AND Code = N'RND')
BEGIN
    INSERT INTO [Identity].[Department]
        (Id, OrganizationId, Code, Name, ManagerUserId, IsActive, CreatedAt, UpdatedAt)
    VALUES
      ('50000000-0000-0000-0000-000000000001', @OrgId, N'RND', N'研发', '10000000-0000-0000-0000-000000000010', 1, @Now, @Now),
      ('50000000-0000-0000-0000-000000000002', @OrgId, N'QA', N'质量', '10000000-0000-0000-0000-000000000012', 1, @Now, @Now),
      ('50000000-0000-0000-0000-000000000003', @OrgId, N'SUPPLY', N'供应链', '10000000-0000-0000-0000-000000000013', 1, @Now, @Now),
      ('50000000-0000-0000-0000-000000000004', @OrgId, N'FINANCE', N'财务', '10000000-0000-0000-0000-000000000014', 1, @Now, @Now),
      ('50000000-0000-0000-0000-000000000005', @OrgId, N'PRODUCTION', N'生产', '10000000-0000-0000-0000-000000000015', 1, @Now, @Now),
      ('50000000-0000-0000-0000-000000000006', @OrgId, N'PROJECT', N'项目', NULL, 1, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Identity].[UserDepartmentMembership] WHERE UserId = '10000000-0000-0000-0000-000000000010')
BEGIN
    INSERT INTO [Identity].[UserDepartmentMembership]
        (UserId, DepartmentId, IsPrimary, IsManager, IsActive, JoinedAt)
    VALUES
      ('10000000-0000-0000-0000-000000000010', '50000000-0000-0000-0000-000000000001', 1, 1, 1, @Now),
      ('10000000-0000-0000-0000-000000000011', '50000000-0000-0000-0000-000000000001', 1, 0, 1, @Now),
      ('10000000-0000-0000-0000-000000000012', '50000000-0000-0000-0000-000000000002', 1, 1, 1, @Now),
      ('10000000-0000-0000-0000-000000000013', '50000000-0000-0000-0000-000000000003', 1, 1, 1, @Now),
      ('10000000-0000-0000-0000-000000000014', '50000000-0000-0000-0000-000000000004', 1, 1, 1, @Now),
      ('10000000-0000-0000-0000-000000000015', '50000000-0000-0000-0000-000000000005', 1, 1, 1, @Now),
      ('10000000-0000-0000-0000-000000000016', '50000000-0000-0000-0000-000000000001', 1, 0, 1, @Now);
END;

INSERT INTO [Identity].[DataScope] (Id, UserId, ScopeType, ScopeReference)
SELECT NEWID(), membershipRow.UserId, N'department', CONVERT(nvarchar(36), membershipRow.DepartmentId)
FROM [Identity].[UserDepartmentMembership] membershipRow
WHERE membershipRow.IsActive = 1
  AND NOT EXISTS (
      SELECT 1
      FROM [Identity].[DataScope] existing
      WHERE existing.UserId = membershipRow.UserId
        AND existing.ScopeType = N'department'
        AND existing.ScopeReference = CONVERT(nvarchar(36), membershipRow.DepartmentId)
  );

IF NOT EXISTS (
    SELECT 1 FROM [Identity].[UserRole]
    WHERE UserId = '10000000-0000-0000-0000-000000000016'
      AND RoleId = '20000000-0000-0000-0000-000000000002'
)
    INSERT INTO [Identity].[UserRole] (UserId, RoleId)
    VALUES ('10000000-0000-0000-0000-000000000016', '20000000-0000-0000-0000-000000000002');

IF NOT EXISTS (SELECT 1 FROM [Metadata].[ObjectType] WHERE Code = N'CUSTOMER_SAMPLE')
BEGIN
    INSERT INTO [Metadata].[ObjectType] (Id, Code, Name, Version, CreatedAt, UpdatedAt) VALUES
      ('40000000-0000-0000-0000-000000000001', N'CUSTOMER_SAMPLE', N'客户样品', 1, @Now, @Now),
      ('40000000-0000-0000-0000-000000000002', N'QUALITY_DEVIATION', N'批次质量偏差', 1, @Now, @Now),
      ('40000000-0000-0000-0000-000000000003', N'PURCHASE_REQUEST', N'采购请购', 1, @Now, @Now),
      ('40000000-0000-0000-0000-000000000004', N'CUSTOMER_COMPLAINT', N'客户投诉', 1, @Now, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Metadata].[FieldDefinition] WHERE ObjectTypeId = '40000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO [Metadata].[FieldDefinition] (Id, ObjectTypeId, [Key], DisplayName, DataType, IsRequired, MaxLength, Sensitivity, SchemaVersion) VALUES
      (NEWID(), '40000000-0000-0000-0000-000000000001', N'Customer', N'客户', N'string', 1, 100, N'Sensitive', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000001', N'Product', N'产品', N'string', 1, 100, N'Internal', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000001', N'Requirement', N'客户需求', N'text', 1, 2000, N'Sensitive', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000001', N'TrialRequired', N'是否试产', N'string', 0, 20, N'Internal', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000002', N'BatchNo', N'批次号', N'string', 1, 80, N'Internal', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000002', N'Alarm', N'报警摘要', N'text', 1, 1000, N'Internal', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000002', N'Severity', N'严重程度', N'string', 1, 20, N'Internal', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000003', N'Requester', N'请购人', N'string', 1, 80, N'Internal', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000003', N'Supplier', N'供应商', N'string', 1, 120, N'Sensitive', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000003', N'Amount', N'金额', N'decimal', 1, 30, N'Sensitive', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000003', N'Category', N'采购类别', N'string', 1, 80, N'Internal', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000004', N'Customer', N'客户', N'string', 1, 100, N'Sensitive', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000004', N'BatchNo', N'批次号', N'string', 1, 80, N'Internal', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000004', N'Issue', N'客诉问题', N'text', 1, 2000, N'Sensitive', 1),
      (NEWID(), '40000000-0000-0000-0000-000000000004', N'Severity', N'严重程度', N'string', 1, 20, N'Internal', 1);
END;

IF NOT EXISTS (
    SELECT 1 FROM [Metadata].[FieldDefinition]
    WHERE ObjectTypeId = '40000000-0000-0000-0000-000000000001'
      AND [Key] = N'InternalFormulaReference'
)
    INSERT INTO [Metadata].[FieldDefinition]
        (Id, ObjectTypeId, [Key], DisplayName, DataType, IsRequired, MaxLength, Sensitivity, SchemaVersion)
    VALUES
        (NEWID(), '40000000-0000-0000-0000-000000000001',
         N'InternalFormulaReference', N'内部配方引用', N'string', 0, 80, N'Core', 1);

DECLARE @CustomerWorkflow nvarchar(max) = N'{"objectTypeCode":"CUSTOMER_SAMPLE","startNode":"RD_INITIATE","nodes":[{"code":"RD_INITIATE","name":"研发立项","assigneeUserName":"demo.researcher","nextNode":"QA_REVIEW","completeOnApprove":false},{"code":"QA_REVIEW","name":"质量测试判定","assigneeUserName":"demo.qa","nextNode":"PRODUCTION_RELEASE","completeOnApprove":false},{"code":"PRODUCTION_RELEASE","name":"生产接收","assigneeUserName":"demo.production","nextNode":"TRIAL_REVIEW","completeOnApprove":false},{"code":"TRIAL_REVIEW","name":"试产复核","assigneeUserName":"demo.qa","nextNode":"FINAL_RELEASE","completeOnApprove":false,"condition":{"field":"TrialRequired","operator":"equals","value":"Yes"}},{"code":"FINAL_RELEASE","name":"转产发布","assigneeUserName":"demo.admin","nextNode":null,"completeOnApprove":true}]}';
DECLARE @DeviationWorkflow nvarchar(max) = N'{"objectTypeCode":"QUALITY_DEVIATION","startNode":"SHIFT_CONFIRM","nodes":[{"code":"SHIFT_CONFIRM","name":"班组现场确认","assigneeUserName":"demo.production","nextNode":"JOINT_REVIEW","completeOnApprove":false},{"code":"JOINT_REVIEW","name":"工艺质量评审","assigneeUserName":"demo.qa","nextNode":"MAJOR_REVIEW","completeOnApprove":false},{"code":"MAJOR_REVIEW","name":"重大偏差复核","assigneeUserName":"demo.admin","nextNode":"CAPA_DECISION","completeOnApprove":false,"condition":{"field":"Severity","operator":"equals","value":"Major"}},{"code":"CAPA_DECISION","name":"CAPA 决策","assigneeUserName":"demo.qa","nextNode":null,"completeOnApprove":true}]}';
DECLARE @PurchaseWorkflow nvarchar(max) = N'{"objectTypeCode":"PURCHASE_REQUEST","startNode":"BUDGET_REVIEW","nodes":[{"code":"BUDGET_REVIEW","name":"预算审核","assigneeUserName":"demo.supply","nextNode":"HIGH_AMOUNT_REVIEW","completeOnApprove":false},{"code":"HIGH_AMOUNT_REVIEW","name":"大额采购复核","assigneeUserName":"demo.finance","nextNode":"PURCHASE_REVIEW","completeOnApprove":false,"condition":{"field":"Amount","operator":"greaterThan","value":"10000"}},{"code":"PURCHASE_REVIEW","name":"采购执行确认","assigneeUserName":"demo.supply","nextNode":"RECEIPT_CHECK","completeOnApprove":false},{"code":"RECEIPT_CHECK","name":"到货验收","assigneeUserName":"demo.production","nextNode":"PAYMENT_APPROVAL","completeOnApprove":false},{"code":"PAYMENT_APPROVAL","name":"付款人工审批（模拟）","assigneeUserName":"demo.finance","nextNode":null,"completeOnApprove":true}]}';
DECLARE @ComplaintWorkflow nvarchar(max) = N'{"objectTypeCode":"CUSTOMER_COMPLAINT","startNode":"COMPLAINT_INTAKE","nodes":[{"code":"COMPLAINT_INTAKE","name":"客诉受理","assigneeUserName":"demo.qa","nextNode":"BATCH_TRACE","completeOnApprove":false},{"code":"BATCH_TRACE","name":"批次追溯","assigneeUserName":"demo.production","nextNode":"JOINT_INVESTIGATION","completeOnApprove":false},{"code":"JOINT_INVESTIGATION","name":"联合调查","assigneeUserName":"demo.qa","nextNode":"CUSTOMER_RESPONSE","completeOnApprove":false},{"code":"CUSTOMER_RESPONSE","name":"客户处置与回访","assigneeUserName":"demo.admin","nextNode":null,"completeOnApprove":true}]}';

IF NOT EXISTS (SELECT 1 FROM [Workflow].[WorkflowDefinition] WHERE Code = N'CUSTOMER_SAMPLE_TO_PRODUCTION')
BEGIN
    INSERT INTO [Workflow].[WorkflowDefinition] (Id, Code, Name, Status, CreatedAt) VALUES
      ('50000000-0000-0000-0000-000000000001', N'CUSTOMER_SAMPLE_TO_PRODUCTION', N'客户样品到研发转产', N'Published', @Now),
      ('50000000-0000-0000-0000-000000000002', N'BATCH_DEVIATION_CAPA', N'批次异常到 CAPA', N'Published', @Now),
      ('50000000-0000-0000-0000-000000000003', N'PURCHASE_TO_PAYMENT', N'请购到付款', N'Published', @Now),
      ('50000000-0000-0000-0000-000000000004', N'CUSTOMER_COMPLAINT_CAPA', N'客诉到 CAPA', N'Published', @Now);

    INSERT INTO [Workflow].[WorkflowVersion] (Id, WorkflowDefinitionId, VersionNumber, Status, DefinitionJson, PublishedAt) VALUES
      ('51000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', 1, N'Published', @CustomerWorkflow, @Now),
      ('51000000-0000-0000-0000-000000000002', '50000000-0000-0000-0000-000000000002', 1, N'Published', @DeviationWorkflow, @Now),
      ('51000000-0000-0000-0000-000000000003', '50000000-0000-0000-0000-000000000003', 1, N'Published', @PurchaseWorkflow, @Now),
      ('51000000-0000-0000-0000-000000000004', '50000000-0000-0000-0000-000000000004', 1, N'Published', @ComplaintWorkflow, @Now);
END;

IF NOT EXISTS (SELECT 1 FROM [Agent].[AgentProfile] WHERE Name = N'业务协同数字员工')
BEGIN
    INSERT INTO [Agent].[AgentProfile]
        (Id, ServiceUserId, Code, Name, Description, DepartmentId,
         BusinessOwnerUserId, TechnicalOwnerUserId, AutonomyLevel, AmountLimit, RiskLimit,
         DataScopesJson, ModelScopesJson, AllowedToolsJson, RequiredConfirmationsJson,
         StopConditionsJson, LifecycleStatus, CurrentPublishedVersionId, Version, Revision,
         IsActive, CreatedBy, CreatedAt, UpdatedBy, UpdatedAt)
    VALUES
        ('60000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000019',
         N'AGENT-DEMO', N'业务协同数字员工', N'演示用流程辅助身份',
         '50000000-0000-0000-0000-000000000001',
         '10000000-0000-0000-0000-000000000010',
         '10000000-0000-0000-0000-000000000010',
         N'A1', 0, N'Low',
         CONCAT(N'["organization:', CONVERT(nvarchar(36), @OrgId),
                N'","department:50000000-0000-0000-0000-000000000001"]'),
         N'["aidt-rule-sim"]', N'["workflow.read","workflow.assist"]',
         N'["contract-commitment","customer-compensation","equipment-control","formula-change","payment","quality-release","safety-interlock"]',
         N'["amount-limit","manual-stop","risk-limit","unauthorized-tool"]',
         N'Published', NULL, 1, 1, 1,
         '10000000-0000-0000-0000-000000000010', @Now,
         '10000000-0000-0000-0000-000000000010', @Now);

    INSERT INTO [Agent].[AgentProfileVersion]
        (Id, AgentProfileId, VersionNumber, Revision, Status, Code, Name, Description,
         ServiceUserId, DepartmentId, BusinessOwnerUserId, TechnicalOwnerUserId,
         AutonomyLevel, AmountLimit, RiskLimit, DataScopesJson, ModelScopesJson,
         AllowedToolsJson, RequiredConfirmationsJson, StopConditionsJson,
         ValidationJson, IsHighRisk, HighRiskReasonsJson,
         CreatedBy, CreatedAt, UpdatedBy, UpdatedAt,
         ValidatedBy, ValidatedAt, PublishedBy, PublishedAt, RetiredAt)
    VALUES
        ('70000000-0000-0000-0000-000000000001',
         '60000000-0000-0000-0000-000000000001', 1, 1, N'Published',
         N'AGENT-DEMO', N'业务协同数字员工', N'演示用流程辅助身份',
         '10000000-0000-0000-0000-000000000019',
         '50000000-0000-0000-0000-000000000001',
         '10000000-0000-0000-0000-000000000010',
         '10000000-0000-0000-0000-000000000010',
         N'A1', 0, N'Low',
         CONCAT(N'["organization:', CONVERT(nvarchar(36), @OrgId),
                N'","department:50000000-0000-0000-0000-000000000001"]'),
         N'["aidt-rule-sim"]', N'["workflow.read","workflow.assist"]',
         N'["contract-commitment","customer-compensation","equipment-control","formula-change","payment","quality-release","safety-interlock"]',
         N'["amount-limit","manual-stop","risk-limit","unauthorized-tool"]',
         N'{"isValid":true,"errors":[],"warnings":[],"isHighRisk":false,"highRiskReasons":[],"validatedAt":"2026-11-03T00:00:00Z"}',
         0, N'[]',
         '10000000-0000-0000-0000-000000000010', @Now,
         '10000000-0000-0000-0000-000000000010', @Now,
         '10000000-0000-0000-0000-000000000010', @Now,
         '10000000-0000-0000-0000-000000000010', @Now, NULL);

    UPDATE [Agent].[AgentProfile]
    SET CurrentPublishedVersionId = '70000000-0000-0000-0000-000000000001'
    WHERE Id = '60000000-0000-0000-0000-000000000001';
END;

IF NOT EXISTS (SELECT 1 FROM [Integration].[Connector] WHERE Code = N'U9-MOCK')
BEGIN
    INSERT INTO [Integration].[Connector] (Id, Code, Name, AdapterType, ConnectionStatus, AuthorityOwner)
    VALUES
      ('70000000-0000-0000-0000-000000000001', N'U9-MOCK', N'U9 财务 Mock', N'Mock', N'Mock', N'U9'),
      ('70000000-0000-0000-0000-000000000002', N'ERP-MOCK', N'ERP Mock', N'Mock', N'Mock', N'ERP'),
      ('70000000-0000-0000-0000-000000000003', N'DCS-MOCK', N'DCS 只读报警 Mock', N'Mock', N'Mock', N'DCS'),
      ('70000000-0000-0000-0000-000000000004', N'DINGTALK-DISCONNECTED', N'钉钉未连接', N'Disconnected', N'Disconnected', N'钉钉');
END;

COMMIT TRANSACTION;

SELECT
    (SELECT COUNT(*) FROM [Identity].[User]) AS UserCount,
    (SELECT COUNT(*) FROM [Metadata].[ObjectType]) AS ObjectTypeCount,
    (SELECT COUNT(*) FROM [Workflow].[WorkflowVersion] WHERE Status = N'Published') AS PublishedWorkflowCount,
    (SELECT COUNT(*) FROM [Integration].[Connector]) AS ConnectorCount;
