# META-001-A 业务对象建模与字段编辑后端变更计划

## 🎯 目标

在不修改历史迁移、不要求每次改模型都发布源码的前提下，让授权管理员通过后端 API 管理对象类型、字段和版本，并安全编辑业务对象实例。

## 🚫 非目标

- 不实现 5B 的可视化管理端页面。
- 不引入任意 SQL、脚本、公式执行器或第二套 ORM。
- 不改变工作流通过 `objectTypeCode` 引用对象类型的方式。
- 不重建现有五类对象或改写历史业务对象快照。

## 🧩 Feature

- `IDEA-META-001 / META-001`
- 本批仅 `META-001-A`

## 📌 当前行为

- 对象类型列表、业务对象列表/创建已存在。
- 对象类型创建和业务对象更新返回 501。
- 对象类型状态、版本历史、字段扩展属性与建模权限未落地。

## ✅ 预期行为

- 草稿创建、草稿编辑、校验、发布、停用、恢复和历史版本可调用。
- 已发布模型不可原地修改；再次编辑生成新草稿版本。
- 业务对象更新校验字段定义、必填、长度、选项和类型，并递增版本、写快照和审计。
- `expectedVersion` 与数据库行版本共同拒绝并发覆盖。
- 所有写动作支持幂等键。

## 🧠 最小上下文

- 需求池、FEATURE_MAP、PERMISSION_MATRIX。
- `BusinessObjectsController`、`AidtDbContext`、Metadata/BusinessObject 实体与映射。
- `CreateBusinessObjectHandler`、`SqlObjectTypeRepository`、`SqlBusinessObjectRepository`。
- `SqlWorkspaceQueryService`、审计、幂等、单位和 SQL/API 测试。

## 🔌 API 影响

- 完成 `POST /api/v1/object-types`。
- 完成 `PUT /api/v1/business-objects/{id}`。
- 新增草稿更新、校验、发布、停用、恢复、版本历史六个对象模型端点。

## 🗄️ 数据库影响

- 仅新增 `013_MetaObjectModeling`。
- 扩展 `Metadata.ObjectType` 与 `Metadata.FieldDefinition`。
- 新增 `Metadata.ObjectTypeVersion` 不可变快照表。
- 现有类型回填为 Published v1，历史业务对象不改写。

## 🔐 权限与审计

- 新增对象模型五项权限，仅平台管理员获得。
- 业务对象更新复用 `business-object:update`。
- 对象类型与业务对象写动作均追加审计，Core 字段读取继续脱敏。

## 🧪 回归

- 后端、SQL/API、MCP、OpenAPI、正式 E2E 全量。
- 五类现有对象类型、工作流 `objectTypeCode`、PROJECT/PMO 专项兼容。
- 013 备份、RESTORE VERIFYONLY、隔离库 up/down/re-up。

## ↩️ 回滚

- 应用回滚至 `req-ui-step4-final1`。
- 013 Down 删除新增版本表和扩展列，保留原始对象类型/字段/业务对象主数据。
- 在隔离库先完成 down/re-up，并恢复部署前备份验证。

## 📋 实施步骤

1. 固化契约、权限、实体和 013。
2. 实现对象模型生命周期和业务对象更新。
3. 补单元、SQL/API、OpenAPI 和专项 E2E。
4. 隔离迁移、全量门禁、构建部署和正式 E2E。
5. 应用回滚、数据库恢复、交付和冻结。

## 🏁 完成证据

- [ ] 后端/MCP 全量通过
- [ ] 正式 E2E 不回归
- [ ] 013 up/down/re-up 通过
- [ ] 文档、功能地图、权限矩阵和 OpenAPI 同步
- [ ] 回滚和恢复通过
