# META-001-A 上下文包

## 🎯 任务

实现业务对象类型生命周期、字段定义、版本历史和业务对象安全编辑后端。

## 🧩 Feature

`IDEA-META-001 / META-001 / META-001-A`

## 🧾 验收标准

- 管理员可新建草稿、编辑、校验、发布、停用、恢复并读取版本历史。
- 普通员工不能建模；Core 字段继续脱敏。
- 字段支持九种数据类型和全部要求属性。
- 已发布版本不可修改；变更生成新版本；历史业务对象快照不被改写。
- 业务对象更新校验字段、拒绝稳定编码/类型修改、递增版本、写快照和审计。
- `expectedVersion` 冲突、无权限和跨部门请求均被拒绝。
- 现有五类对象、工作流引用和 PROJECT/PMO 兼容。
- 013 可隔离 up/down/re-up，正式 E2E 37/37 不回归。

## 📚 必读

- `AGENTS.md`、技术栈、需求池、`FEATURE_MAP`、`PERMISSION_MATRIX`。
- `BusinessObjectsController`、`AidtDbContext`、实体配置与 Metadata/BusinessObject 仓储。
- `CreateBusinessObjectHandler`、`SqlWorkspaceQueryService`、审计/幂等接口。
- 001–012 迁移、OpenAPI、API 契约和 SQL 集成测试。

## 🗃️ 数据与流程

- `Metadata.ObjectType`、`Metadata.FieldDefinition`、新增 `Metadata.ObjectTypeVersion`。
- `BusinessObject.BusinessObject`、`BusinessObjectVersion`。
- 工作流仍按对象类型编码引用；PROJECT 专属数据仍由 PMO 服务维护。

## 🚫 边界

- 不修改 001–012。
- 不创建第二套业务对象、权限、审计或工作流模型。
- 不执行用户提交的任意代码、SQL、脚本或 HTML。
- 不把 Core 明文返回给非平台管理员。

## ✅ 输出

- 已部署版本 `req-ui-step5a-final1`、交付报告、测试证据、迁移和冻结标签。
