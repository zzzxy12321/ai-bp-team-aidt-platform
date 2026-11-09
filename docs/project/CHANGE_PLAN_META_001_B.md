# META-001-B 业务对象建模与动态表单前端变更计划

状态：COMPLETE_DEPLOYED_AND_FROZEN

## 🎯 目标

让平台管理员在网页完成对象类型和字段设计、预览、校验、发布、停用及版本查看；让普通用户只使用已发布版本动态录入和编辑业务对象，并查看版本、项目、流程、文件和审计关联。

## 🚫 非目标

- 不开放任意 HTML、JavaScript、SQL、脚本或公式。
- 不允许前端状态代替 SQL Server 持久化。
- 不改变工作流通过 `objectTypeCode` 引用的公开语义。
- 不重建现有五类对象、历史对象快照或历史流程。

## 🧩 Feature

- `IDEA-META-001 / META-001-B`
- 前序：`req-ui-step5a-frozen-v1`

## 🖥️ 页面

- 新增 `/modeling` 业务建模工作室。
- 升级 `/objects` 动态对象工作区。
- 流程配置中心只允许选择已发布对象类型。
- 岗位工作台业务对象卡片可选择已发布对象类型编码。

## 🔌 API

- 复用 5A 对象类型生命周期 API。
- 新增 `GET /api/v1/business-objects/{id}/versions`。
- 新增 `GET /api/v1/business-objects/{id}/context`。
- 新增 `GET /api/v1/business-objects/field-options/users`。
- 完成 `PUT /api/v1/business-objects/{id}` 前端调用。

## 🗄️ 数据库

- 新增 014，仅向既有业务角色授予 `business-object:update`，不新增表。
- 版本和上下文读取现有 BusinessObjectVersion、ProjectRelation、WorkflowInstance、Attachment、AuditEvent。

## 🔐 权限

- 建模入口与按钮读取 `object-model:*`，后端继续要求平台管理员。
- 普通用户对象查询仅返回已发布类型及其发布字段快照。
- User/Department 候选只返回当前组织且操作者有权访问的数据。
- Core 字段继续按服务端规则脱敏。

## 🧪 状态覆盖

- Loading、Empty、Error、Denied、Conflict、NotPublished。
- 桌面 1440/1280、移动 390。

## 📋 实施步骤

1. 更新前端/API 类型和 5B 只读端点。
2. 实现业务建模工作室和字段设计器。
3. 升级动态业务对象创建、详情、编辑、版本和关联证据。
4. 打通流程配置和岗位工作台对象类型选择。
5. 单元、Vitest、SQL/API、OpenAPI 和源码 E2E。
6. 干净构建、部署、正式 E2E、回滚、恢复和冻结。

## 🏁 完成证据

- [x] 管理员网页完成对象类型全生命周期
- [x] 普通用户仅使用发布版本
- [x] 九类字段动态渲染与持久化通过
- [x] 版本、关联证据、权限和脱敏通过
- [x] 流程、项目、岗位工作台无回归
- [x] 正式 E2E、回滚、恢复和冻结通过
