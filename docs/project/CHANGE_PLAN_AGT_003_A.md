# AGT-003-A 数字员工身份生命周期与能力治理后端变更计划

状态：IN_PROGRESS

## 🎯 目标

在不建立第二套身份、权限、OAuth 或令牌系统的前提下，将现有 `AgentProfile`
升级为正式、版本化、可审计的数字员工身份主数据，并由后端强制执行生命周期、
组织边界、能力白名单、自主等级、风险、金额、人工确认、停止和令牌撤销规则。

## 🧩 Feature 与发布

- Feature：`IDEA-AGENT-003 / AGT-003 / 6A`
- 功能分支：`feature/ui-step6a-agent-profile-backend`
- 目标发布：`req-ui-step6a-final1`
- 目标冻结标签：`req-ui-step6a-frozen-v1`
- 前序冻结：`req-ui-step5-frozen-v1`

## ✅ 实现范围

1. 扩展 `AgentProfile` 当前有效主数据，并新增不可变 `AgentProfileVersion`。
2. 支持草稿创建/编辑、校验、发布、停用、恢复、历史查询和回滚。
3. 已发布版本不可覆盖；修改必须创建新草稿版本；发布在事务中原子切换当前有效版本。
4. 新运行、工作流 Agent 节点和 MCP 服务令牌只解析已发布、有效身份。
5. 新增/复用 `Agent` 权限：读取、创建、更新、管理、发布和审核。
6. 平台管理员可全平台治理；部门负责人只能在授权部门及自身权限上限内操作；
   普通人员只读授权范围内的有效身份。
7. 高风险能力变更进入既有 `Identity.AccessChangeRequest` 双人审核；申请人不能自审。
8. 工具从固定注册表选择；禁止 SQL、Shell、任意 HTTP、文件系统、代码执行和外部写库。
9. 数据/模型/人工确认/熔断规则结构化验证；永久人工边界不可删除或放宽。
10. 停用在同一事务中更新生命周期、撤销关联 MCP 服务令牌并写审计和 Outbox。
11. API、错误码、OpenAPI 和前端 TypeScript 契约同步。

## 🚫 非目标

- 不连接真实钉钉、U9、ERP、DCS 或外部大模型。
- 不新增第二套 OAuth、令牌、身份或权限体系。
- 不开放任意表达式、脚本、SQL、Shell、HTTP 或文件系统工具。
- 不删除历史 `AgentRun`、`WorkflowTaskAction`、审计和回执。
- 不改变既有 `AGENTS?run=...` 深链与运行确认语义。

## 🗄️ 数据与迁移

- 仅新增迁移 015；001–014 不变。
- `Agent.AgentProfile`：增加编码、业务说明、部门、负责人、生命周期、当前发布版本、
  数据/模型/工具/确认/熔断快照及创建/更新审计字段。
- `Agent.AgentProfileVersion`：不可变版本快照、Revision、状态、发布日期和发布人。
- `Mcp.Client` / `Mcp.AccessToken`：增加可空 `AgentProfileId`，服务令牌显式关联数字员工。
- `Mcp.AccessToken`：增加撤销人、撤销原因；保留既有令牌摘要和撤销时间。
- 复用 `Identity.AccessChangeRequest` 承载高风险发布审核。
- 唯一索引防重复编码、重复版本号、重复当前发布指针；过滤索引约束单一有效发布版本。

## 🔌 API

- `GET /api/v1/agents`
- `GET /api/v1/agents/available`
- `GET /api/v1/agents/{id}`
- `POST /api/v1/agents/drafts`
- `PUT /api/v1/agents/{id}/drafts/{versionId}`
- `POST /api/v1/agents/{id}/versions/{versionId}/actions/validate`
- `POST /api/v1/agents/{id}/versions/{versionId}/actions/publish`
- `POST /api/v1/agents/{id}/actions/disable`
- `POST /api/v1/agents/{id}/actions/resume`
- `GET /api/v1/agents/{id}/versions`
- `POST /api/v1/agents/{id}/actions/rollback`
- `POST /api/v1/agents/change-requests/{requestId}/actions/review`

所有写接口支持 `Idempotency-Key`；草稿更新、发布、停用和恢复使用 ExpectedRevision/版本冲突。

## 🔐 权限

| 动作 | 平台管理员 | 部门负责人 | 普通业务人员 | 服务身份 |
|---|---|---|---|---|
| 查看治理详情/版本 | 全平台 | 授权部门且不超编辑上限 | 不可见治理详情 | 禁止 |
| 创建/编辑草稿 | 是 | 授权部门且不超编辑上限 | 否 | 禁止 |
| 校验 | 是 | 授权部门 | 否 | 禁止 |
| 发布/恢复/回滚 | 是 | 授权部门且不超发布上限 | 否 | 禁止 |
| 高风险变更审核 | 是 | 否 | 否 | 禁止 |
| 停用/紧急停止 | 是 | 授权部门 | 否 | 禁止 |
| 查看有效候选 | 是 | 是 | 授权范围内只读 | 不可签发/不可自审 |

服务身份不能登录治理页面、审批自己的身份、确认自身运行或完成需要真人确认的动作。
前端按钮隐藏不作为授权边界。

## 🛡️ 关键规则

- 发布前必须校验服务身份为同组织有效独立服务用户。
- 发布前必须校验部门、业务负责人、技术负责人、数据范围和模型范围。
- 仅允许注册能力码；永久人工边界始终包含付款、质量放行、配方变更、设备控制、
  安全联锁、外部合同承诺和客户重大赔付。
- A0/A1 永远要求人工确认；风险/金额超过身份上限的动作拒绝。
- 高风险发布差值包括提升自主等级、风险、金额、扩大数据/模型/工具范围、
  更换服务身份、删除人工确认或熔断条件。
- 停用后立即拒绝新运行、新流程解析和新 MCP 服务令牌签发，并撤销已有服务令牌。
- 历史运行、任务动作、审计与回执保持只读可追溯。

## 🧪 验证矩阵

- Domain：全生命周期、永久边界、超限、自审、服务身份限制。
- API/SQL：草稿、编辑、校验、发布、停用、恢复、版本、回滚、并发、幂等、部门边界、
  普通用户只读、高风险双人审核、令牌撤销、历史保留、工具/模型白名单。
- 联动：Workflow Agent 节点、AgentRun、MCP 服务令牌、审计和 Outbox。
- 迁移：015 up/down/re-up，上一版本升级和独立恢复。
- 全量：后端、SQL、API、MCP、OpenAPI、前端类型/Vitest、构建、源码 E2E。
- 发布：干净构建 `req-ui-step6a-final1`、正式 E2E、应用回滚、数据库备份与恢复。

## ↩️ 回滚

应用回滚到 `req-ui-step5-final1`；数据库先恢复迁移前备份。隔离库执行 015 down，
确认 AgentProfileVersion、新增列、权限和服务令牌关联可回退且不删除历史运行与审计。

