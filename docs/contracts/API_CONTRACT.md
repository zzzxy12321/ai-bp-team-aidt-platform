# AIDT API 契约 V1

## 批次 DEPT-WF-001 增量

- OpenAPI：`docs/contracts/openapi/v1.json`；187 路径、208 operationId。
- `GET /api/v1/navigation/current`：返回公司公共入口、部门工作空间和平台治理三层，
  以及 `currentDepartmentId`；普通用户仅收到授权部门。
- `GET /api/v1/auth/session`：新增 `authorizationState: Current|Stale|Anonymous`
  和 `authorizationMessage`。权限更新或旧 Cookie 缺指纹时返回 `Stale`。
- `/api/v1/configurations` 扩展模板元数据、默认模板、适用岗位、风险级别和节点字段；
  继续返回现有 `WorkflowConfigurationDto`，发布时仍落到 `WorkflowDefinition/Version`。
- 配置生命周期使用既有草稿/更新/校验/发布接口，并增加版本、停用和恢复：
  - `GET /api/v1/configurations/{id}/versions`
  - `POST /api/v1/configurations/{id}/actions/disable`
  - `POST /api/v1/configurations/{id}/actions/restore`
  - `POST /api/v1/configurations/{id}/actions/simulate`
  - `POST /api/v1/configurations/{id}/actions/rollback`
- `GET /api/v1/workflow-tasks/{id}/form`：返回任务创建时冻结的节点名称、说明、字段、
  动作、附件要求、AI 配置、Schema 版本和内容哈希。
- `PUT /api/v1/workflow-tasks/{id}/form-draft`：保存任务表单草稿，要求
  `ExpectedVersion`、冻结哈希和幂等键。
- `POST /api/v1/workflow-tasks/{id}/actions/ai-review`：仅允许已注册的完整性检查、
  风险识别、审查意见或草稿动作，返回 `aidt-rule-sim` 建议并固定
  `externalModelCalled=false`。
- 通过、退回和终止动作增加冻结表单提交；服务端重新验证必填、字段类型、角色可见/可写、
  人员/部门范围、附件、敏感等级、Schema 版本和哈希。

字段来源固定为：

- `BusinessObjectReference`：只保存对象字段编码，不复制字段值。
- `TaskInput`：保存真实任务人工输入。
- `EvidenceReference`：引用既有文件、听记、AgentRun 或审计证据。

业务错误增加并复用：模板停用、部门无权、字段定义变化、缺少必填字段、附件未满足、
数字员工不可用、永久人工边界和重复提交。所有写操作继续要求 CSRF、业务权限、
部门范围、幂等键和并发版本，并在同一事务中写任务、表单、审计与 Outbox。

## 批次 SCN-001-A 增量

| API | 用途 |
|---|---|
| `GET /api/v1/scenarios` | 当前用户可见的已发布场景 |
| `GET /api/v1/scenarios/manage` | 授权部门/平台场景治理列表 |
| `GET /api/v1/scenarios/{id}` | 场景详情、步骤和准备材料 |
| `POST /api/v1/scenarios/drafts` | 创建初始场景草稿 |
| `POST /api/v1/scenarios/{id}/drafts` | 从已发布版本创建下一草稿 |
| `PUT /api/v1/scenarios/{id}/drafts/{versionId}` | 更新草稿并做 Revision 并发校验 |
| 生命周期 actions | 校验、发布、停用、恢复和回滚 |
| `POST /api/v1/scenarios/{id}/actions/preflight` | 启动前材料、对象、部门、流程和 Agent 检查 |
| `POST /api/v1/scenarios/{id}/actions/start` | 幂等启动真实对象/流程/任务/事件链 |
| `GET /api/v1/scenarios/launches/{id}` | 启动记录与真实业务链反查入口 |
| `/api/v1/scenarios/approval-requests*` | 高风险场景发布双人审核 |

场景错误包含 `SCENARIO_NOT_PUBLISHED`、`SCENARIO_DISABLED`、
`SCENARIO_VERSION_CONFLICT`、`SCENARIO_REFERENCE_INVALID`、
`SCENARIO_MATERIAL_MISSING`、`SCENARIO_BOUNDARY_MISSING` 和
`SCENARIO_DUPLICATE_SUBMISSION`。

## 批次 AGT-003-B 增量

- OpenAPI：`docs/contracts/openapi/v1.json`
- 新增 `GET /api/v1/agents/options`：返回当前操作者可用的部门、真人负责人、
  未绑定服务账号、已注册能力、永久人工边界、确认规则、熔断条件和模型范围。
- 新增 `GET /api/v1/agents/{id}/context`：聚合身份、近期运行、流程节点、
  WorkflowTask、MCP 客户端/令牌、GatewayCall 和审计时间线。
- `AgentProfileDetail` 增加 `lastRunAt`、`recentRunCount`；MCP 客户端和令牌概要增加
  `agentProfileId`、`agentProfileName`、`agentProfileAvailable`。
- 详情聚合对 MCP 证据和审计时间线分别返回可见性开关，数据仍由后端按真实权限裁剪。
- 所有生命周期写操作继续使用既有 6A 端点、幂等键、Revision/Version 和业务错误契约。
- 不返回令牌明文、TokenHash、密钥、物理路径或完整敏感材料。

## 批次 WS-002 增量

- OpenAPI：`docs/contracts/openapi/v1.json`
- 版本：`1.8.0-ws002`
- 路径：136；operationId：157。
- 新增 `GET /api/v1/departments/{id}/workbench`：服务端匹配岗位模板并返回真实卡片。
- 新增模板列表、详情、创建、复制、草稿、校验、发布、停用、恢复和版本历史 API。
- 匹配优先级固定为用户级 → 角色级 → 部门级 → 组织默认。
- 卡片过滤只接受结构化白名单字段；禁止任意 SQL、脚本、HTML、JavaScript。
- 无匹配模板返回 `matched=false`，前端回退通用部门工作区。

## 批次 UX-003 增量

- OpenAPI：`docs/contracts/openapi/v1.json`
- 版本：`1.7.0-ux003`
- `GET /api/v1/my-work` 响应升级为统一只读 `WorkItemView`。
- 聚合来源：`WorkflowTask`、`ProjectTask`、`EventInbox`、`MinutesActionItem`、
  `McpHumanConfirmation`、`AgentRun`。
- 显示视图包含来源、业务上下文、阶段、责任人、截止、风险、下一步、来源数量和详情深链。
- 聚合层不创建第二套业务状态；所有操作继续调用各来源原详情 API。
- 详情动作仍执行原动作权限、组织/部门/项目/环境范围和真人确认。
- `/api/v1/my-work` 增加 `todayFocus`、`pendingItems`、`initiatedByMe`、
  `participating`、`needsAttention`、`recentSourceEvidence` 和 `summary`。

## 批次 MCP-001 增量

- OpenAPI：`docs/contracts/openapi/v1.json`
- 版本：`1.6.0-mcp`
- 路径：122
- operationId：142
- 新增 MCP 管理接口 8 个路径、8 个 operationId。
- MCP 协议由官方 SDK 暴露在 `/mcp`；OAuth 2.1/PKCE 端点位于
  `/.well-known/oauth-authorization-server`、`/oauth/register`、`/oauth/authorize`、
  `/oauth/token`、`/oauth/revoke`，不混入业务 REST operationId 计数。
- MCP 工具继承 AIDT 用户权限、部门范围和环境隔离；写入必须提供幂等键。
- 审批、事件确认、流程转交、项目最终状态和敏感文件正文需要一次性真人确认。
- 调用审计写入 `Audit.AuditEvent`，`Module=MCP`，可按客户端、用户、工具、资源和 TraceId 查询。

## 批次 2 增量

- OpenAPI：`docs/contracts/openapi/v1.json`
- 版本：`1.2.0-batch2`
- 路径：53
- operationId：62
- 新增：Minutes、节点策略、SLA 扫描、WorkflowNotification、受限代理、
  AgentRun 执行和 GatewayCall 查询。
- 所有写接口使用 Cookie 身份、CSRF、部门/环境校验和幂等键。

> 机器可读契约：[`openapi/v1.json`](./openapi/v1.json)  
> 契约版本：`1.2.0-batch2`
>
> 状态：需求池批次 1 契约已通过 SQL、API 和桌面/390px 浏览器验证

## 📦 统一响应

```json
{
  "code": "OK",
  "message": "success",
  "data": {},
  "traceId": "trace-id"
}
```

错误响应：

```json
{
  "code": "PERSISTENCE_NOT_READY",
  "message": "SQL Server 尚未完成安装和迁移，持久化能力暂不可用。",
  "data": null,
  "traceId": "trace-id"
}
```

规则：

- HTTP 状态码表达协议级结果。
- `code` 是稳定业务错误码，前端不使用文案判断类型。
- `X-Trace-Id` 在响应头回传；客户端提供值只接受不超过 128 位的字母、数字、点、下划线和短横线。
- JSON 使用 camelCase，时间使用 UTC ISO 8601。

## 🔐 认证与权限

OpenAPI 登记 Cookie 认证契约 `AIDTSession`。Development 本地演示身份从 SQL Server 读取，
Cookie 与 CSRF 已启用。该身份明确不是生产 SSO。

正式启用条件：

- SSO/Cookie/CSRF 完成
- 会话装载用户、角色和权限
- API 动作权限过滤器启用
- Application 数据范围校验完成
- 前端 `VITE_AUTH_ENFORCEMENT=enforced`

## 📄 资源路径

| Feature | 路径 |
|---|---|
| 平台能力 | `/api/v1/platform/capabilities` |
| 工作台 | `/api/v1/dashboard` |
| 我的工作 | `/api/v1/my-work` |
| 部门 | `/api/v1/departments` |
| 部门成员 | `/api/v1/departments/{id}/members` |
| 部门工作区 | `/api/v1/departments/{id}/workspace` |
| 岗位工作台 | `/api/v1/departments/{id}/workbench` |
| 工作台模板 | `/api/v1/workbench-templates` 及草稿/校验/发布/停用/恢复/版本 |
| 文件中心 | `/api/v1/files` |
| 文件详情 | `/api/v1/files/{id}` |
| 文件版本 | `/api/v1/files/{id}/versions` |
| 文件下载 | `/api/v1/files/{id}/download` |
| 文件预览 | `/api/v1/files/{id}/preview` |
| 删除申请 | `/api/v1/files/{id}/actions/delete-request` |
| CSRF | `/api/v1/auth/csrf` |
| 本地演示登录 | `/api/v1/auth/demo-login` |
| 会话 | `/api/v1/auth/session` |
| 退出 | `/api/v1/auth/logout` |
| 我的权限 | `/api/v1/permissions/me` |
| 对象类型 | `/api/v1/object-types` |
| 业务对象 | `/api/v1/business-objects` |
| 流程实例 | `/api/v1/workflow-instances` |
| 流程定义 | `/api/v1/workflow-definitions` |
| 流程时间线 | `/api/v1/workflow-instances/{id}/timeline` |
| 流程任务 | `/api/v1/workflow-tasks` |
| 任务动作 | `/api/v1/workflow-tasks/{id}/actions/decide` |
| 审计 | `/api/v1/audit-events` |
| Agent | `/api/v1/agents`、`/available`、`/options`、`/manage`、`/{id}`、`/{id}/context`、生命周期、版本、复核和运行动作 |
| MCP 治理 | `/api/v1/mcp/snapshot`、`/tools`、`/confirmations/me`、令牌/客户端撤销 |
| 连接器 | `/api/v1/connectors` |
| 配置草稿、校验、发布和回滚 | `/api/v1/configurations`、`/api/v1/configurations/{id}/actions/{validate|publish|rollback}` |
| 健康检查 | `/health/live`、`/health/ready` |

独立 AiGateway 监听 `127.0.0.1:5100`，提供 `/health/*`、`/v1/models` 和
`/v1/chat/completions`。它只执行规则模拟并返回 `ExternalModelCalled=false`。

## 🔁 幂等与并发

- 写接口使用 `Idempotency-Key`，任务动作 DTO 同时包含 `idempotencyKey`。
- 同键同内容应返回原结果；同键异内容应返回冲突。
- 业务对象和流程任务使用显式版本。
- 版本不一致返回 `409 WORKFLOW_VERSION_CONFLICT` 或 `CONFLICT`。

## ⚠️ 错误码

错误码源文件：`backend/AIDT.Domain/Common/ErrorCodes.cs`。

| 错误码 | HTTP | 场景 |
|---|---:|---|
| `VALIDATION_FAILED` | 400 | 格式或业务前置校验失败 |
| `AUTHENTICATION_REQUIRED` | 401 | 缺少可信身份 |
| `PERMISSION_DENIED` | 403 | 动作权限不足 |
| `DATA_SCOPE_DENIED` | 403 | 数据范围不足 |
| `DEPARTMENT_SCOPE_DENIED` | 403 | 部门范围或环境范围不足 |
| `FILE_SCAN_REQUIRED` | 403 | 敏感文件尚未完成扫描和人工放行 |
| `FILE_PREVIEW_UNSUPPORTED` | 415 | 文件类型不支持内嵌预览 |
| `RESOURCE_NOT_FOUND` | 404 | 资源不存在 |
| `DUPLICATE_OBJECT_CODE` | 409 | 对象编号重复 |
| `WORKFLOW_VERSION_CONFLICT` | 409 | 流程或任务版本冲突 |
| `TASK_ALREADY_PROCESSED` | 409 | 任务重复处理 |
| `AGENT_CONFIRMATION_REQUIRED` | 409 | 缺少真人确认 |
| `AGENT_LIMIT_EXCEEDED` | 409 | 金额或风险超过 Agent 授权 |
| `PERSISTENCE_NOT_READY` | 503 | SQL Server 未就绪时的稳定失败关闭 |
| `SERVICE_UNAVAILABLE` | 503 | 运行时依赖未配置 |
| `MCP_INSUFFICIENT_SCOPE` | 403 | MCP 令牌缺少当前工具或资源作用域 |
| `MCP_CONFIRMATION_REQUIRED` | 409 | 高风险动作缺少当前真人确认 |
| `MCP_CONFIRMATION_EXPIRED` | 409 | 真人确认已过期 |
| `MCP_IDEMPOTENCY_KEY_REQUIRED` | 400 | 写工具缺少幂等键 |
| `MCP_IDEMPOTENCY_CONFLICT` | 409 | 同一幂等键对应不同请求 |
| `MCP_RESPONSE_TOO_LARGE` | 400 | 工具响应超过配置上限 |

## ✅ 本轮验证

- OpenAPI JSON 可解析。
- 前端使用的全部 API 路径均登记在 OpenAPI。
- 所有 operationId 唯一。
- 写接口包含验证或冲突响应。
- API 从部署目录提供该契约。
- 新路径包含部门、文件、环境、扫描和敏感数据约束。
- 主库迁移 003/004 已执行，SQL/API 集成 64/64 通过。
- 文件上传、下载、预览、版本、通用关联、敏感文件拒绝和越权审计通过。
- Playwright 5/5 通过，包含 WF-001 回归和 390px。

## ⛔ 仍需甲方环境

- 正式企业 SSO/OIDC。
- 真实 U9/ERP/DCS/钉钉接口。
- 生产证书、SPN 和 DBA 账号。
