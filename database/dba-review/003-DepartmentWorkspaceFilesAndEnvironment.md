# 003 部门工作区、文件中心与环境隔离 DBA 审核

> 迁移：`20261030172128_003_DepartmentWorkspaceFilesAndEnvironment`
>
> 状态：`authored_not_applied`
>
> 本轮未连接 SQL Server，未执行迁移、回滚或恢复验证。

## 📌 变更目的

为需求池批次 1 增加后端可强制的部门范围、文件通用关联和 `demo/test/prod`
环境隔离，同时保留现有业务对象、流程、任务和文档主键。

## 🗃️ 结构变化

- 新增 `Identity.Department`
- 新增 `Identity.UserDepartmentMembership`
- 新增 `Document.AttachmentLink`
- `BusinessObject.BusinessObject` 增加部门、环境和来源字段
- `Workflow.WorkflowInstance` 增加部门、发起人、环境和来源字段
- `Workflow.WorkflowTask` 增加部门和环境字段
- `Document.Attachment` 增加组织、部门、环境、状态、删除申请和版本号
- `Document.DocumentVersion` 增加原文件名、MIME、大小、扫描状态和保密等级

## 🔁 数据回填

- 现有 Demo 数据回填为 `demo`、`IsTestData=1`、`AIDT-SEED`
- 按对象类型建立默认部门：
  - `CUSTOMER_SAMPLE` → `RND`
  - `QUALITY_DEVIATION`、`CUSTOMER_COMPLAINT` → `QA`
  - `PURCHASE_REQUEST` → `SUPPLY`
  - 其他 → 组织内首个部门
- Demo 用户建立主部门和负责人关系
- 文件版本从所属 Attachment 回填文件名、MIME、大小、扫描和保密元数据
- 新增 `department:*`、`file:*` 权限并授予现有业务角色

## ⚠️ 锁表与执行风险

- 涉及业务对象、流程实例、任务、附件和版本的 `ALTER TABLE`
- 当前 Demo 数据量小；生产执行前必须在同规模副本测量锁等待和日志增长
- 迁移使用单事务；失败应整体回滚
- 应用发布前不得启动依赖新列的版本

## ✅ 执行前检查

1. 全量备份并执行 `RESTORE VERIFYONLY`
2. 记录 `__EFMigrationsHistory` 最后版本为 `20261030004100_WorkflowPersistenceAndSecurity`
3. 在隔离测试库从 002 升级至 003
4. 验证 38 张应用表、外键、唯一索引和检查约束
5. 执行 003 down，再重新升级
6. 校验现有对象、流程、任务和附件均可回填，无空 `DepartmentId`
7. 校验每个用户最多一个启用主部门

## 🔎 验证 SQL

```sql
SELECT COUNT(*) FROM [Identity].[Department];
SELECT COUNT(*) FROM [Identity].[UserDepartmentMembership] WHERE IsActive = 1;
SELECT COUNT(*) FROM [BusinessObject].[BusinessObject] WHERE DepartmentId = '00000000-0000-0000-0000-000000000000';
SELECT COUNT(*) FROM [Workflow].[WorkflowInstance] WHERE DepartmentId = '00000000-0000-0000-0000-000000000000';
SELECT COUNT(*) FROM [Workflow].[WorkflowTask] WHERE DepartmentId = '00000000-0000-0000-0000-000000000000';
SELECT COUNT(*) FROM [Document].[Attachment] WHERE DepartmentId = '00000000-0000-0000-0000-000000000000';
SELECT UserId, COUNT(*) FROM [Identity].[UserDepartmentMembership]
WHERE IsActive = 1 AND IsPrimary = 1 GROUP BY UserId HAVING COUNT(*) > 1;
```

## ↩️ 回滚

- 使用 `003_DepartmentWorkspaceFilesAndEnvironment.down.sql`
- 回滚删除新表、列、索引、约束、权限和 department DataScope
- 已上传文件正文不由迁移删除，避免误删证据
- 若数据回填造成业务异常，优先恢复迁移前备份

## 🚫 本轮结论

迁移已编写但未应用。SQL Server 执行、集成测试、回滚和恢复验证全部属于阶段 B。
