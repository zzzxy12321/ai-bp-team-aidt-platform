# META-001-B 上下文包

## 🎯 任务

把 5A 对象模型后端能力产品化为管理员的建模工作室和普通用户的动态对象工作区。

## 🧩 Feature

`IDEA-META-001 / META-001 / META-001-B`

## 🧾 验收标准

- 管理员可不改代码创建、设计、预览、校验、发布、停用和恢复对象类型。
- 字段支持九种类型及全部属性，排序、选项和预览可见。
- 普通用户看不到草稿/停用类型，只可基于发布版本创建和编辑实例。
- User/Department 使用可搜索选择器，不要求输入 UUID。
- 对象详情可查看版本历史及项目、流程、文件、审计关联。
- 已发布类型立即出现在流程配置和岗位工作台对象卡片范围中。
- 权限按钮可见性与 API 403 一致，Core 字段持续脱敏。
- 所有页面覆盖 Loading/Empty/Error/Denied/Conflict/NotPublished。
- 1440、1280、390 和原 38/38 正式 E2E 不回归。

## 📚 必读

- `AGENTS.md`、技术栈、需求池、功能地图和权限矩阵。
- 5A 交付、OpenAPI、BusinessObjectsController、对象模型服务。
- `BusinessObjectPage.vue`、`ConfigurationCenterPage.vue`、`IamWorkbenchPanel.vue`。
- 现有 API 类型、auth store、Vitest 与 Playwright 模式。

## 🗃️ 数据

- 对象类型当前行 + ObjectTypeVersion 发布快照；014 仅补充普通业务角色更新授权。
- 业务对象当前行 + BusinessObjectVersion 历史快照。
- ProjectRelation、WorkflowInstance、Attachment、AuditEvent 作为关联证据。

## 🚫 边界

- 不建第二套表单/版本/权限/审计模型。
- 不执行用户提供的代码或数据源表达式。
- 不把草稿用于普通用户表单或流程启动。
- 不返回文件物理路径、Token、密钥或无权 UUID。

## ✅ 输出

- `req-ui-step5-final1`、`META_001_B_DELIVERY.md`、正式 E2E、回滚恢复与 `req-ui-step5-frozen-v1`。
