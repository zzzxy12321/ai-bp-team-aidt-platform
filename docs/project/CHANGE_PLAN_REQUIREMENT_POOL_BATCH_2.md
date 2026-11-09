# AIDT 需求池批次 2 变更计划

> 状态：`BATCH2_IMPLEMENTING`
>
> 日期：2026-10-31
>
> 起始基线：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`
>
> 整个 AIDT 需求池与生产系统：未完成

## 🎯 批次范围

| 需求池来源 | 正式 Feature ID | 目标 |
|---|---|---|
| `IDEA-MINUTES-001` | `MIN-001` | 手动导入听记、规则提取行动项、人工确认和任务追溯 |
| `IDEA-WORKFLOW-001` | `WF-005` | 可版本化节点能力包、SLA、提醒和升级 |
| `IDEA-AGENT-001` | `AGT-002` | 超时处理、数字员工辅助和受限代理 |
| `IDEA-AI-001` | `AIG-002` | AgentRun 与 AiGateway 受控调用关联 |

## 🧭 当前行为

- 文件中心可以保存附件，但没有听记来源、文字稿、行动项或任务草稿。
- 流程任务没有 DueAt、节点 SLA、提醒、升级或代理状态。
- AgentRun 可以人工确认和停止，但没有业务对象、流程、听记关联，也不调用 AiGateway。
- AiGateway 只有独立 RuleSimulator 接口，调用结果不进入 AgentRun。
- 钉钉连接器为 `Disconnected`，真实外部模型关闭。

## ✅ 预期行为

1. 用户上传文字稿文件或在页面粘贴文本，建立 `MinutesSource`。
2. 真人确认 AgentRun 后，由规则模拟服务调用 AiGateway，生成多条行动项草稿。
3. 用户校对行动项，确认后创建可追溯协同任务；重复确认幂等。
4. 管理员按流程版本和节点保存策略，启动流程时复制策略引用和 DueAt 到任务。
5. Worker 按注入时钟扫描到期任务，幂等写站内通知、提醒和升级审计。
6. 低风险且显式授权节点可由独立数字员工身份代理；高风险和永久人工边界节点拒绝代理。
7. AgentRun 和 GatewayCall 可反查业务对象、流程实例、任务和 MinutesSource。

## 🧱 数据与迁移

- `Minutes.MinutesSource`
- `Minutes.MinutesActionItem`
- `Minutes.MinutesTask`
- `Workflow.WorkflowNodePolicy`
- `Workflow.WorkflowNotification`
- `Agent.AgentGatewayCall`
- `Agent.AgentRun` 增加组织、部门、环境、业务关联、AiGateway 摘要和调用模式。
- `Agent.AgentProfile` 增加独立服务用户 ID。
- `Workflow.WorkflowTask` 增加 NodePolicyId、DueAt、SlaStatus、ReminderSentAt、EscalatedAt、DecisionMode、AgentRunId。
- 新迁移从 004 升级，目标应用表 44。

## 🔌 API 与页面

- `/api/v1/minutes`：登记、上传、列表、详情。
- `/api/v1/minutes/{id}/actions/analyze`：创建并关联听记分析 AgentRun。
- `/api/v1/minutes/{id}/action-items`：草稿列表与人工修改。
- `/api/v1/minutes/action-items/{id}/actions/confirm`：确认并创建幂等任务。
- `/api/v1/minutes/by-task/{taskId}`：从任务反查听记、发言人和时间戳。
- `/api/v1/workflow-versions/{id}/node-policies`：策略读取、保存和发布。
- `/api/v1/workflow-sla/actions/run`：受控触发同一 SLA 处理器，供测试使用。
- `/api/v1/workflow-tasks/{id}/actions/agent-proxy`：请求并确认低风险代理。
- `/api/v1/agents/runs/{id}/actions/execute`：执行已确认 AgentRun 并调用 AiGateway。
- `/api/v1/agents/runs/{id}/gateway-calls`：查询模型调用证据。
- 前端新增“AI 听记”页面；流程页显示策略与 SLA；Agent 页显示 GatewayCall 和代理状态。

## 🔐 权限、环境与审计

- 所有写接口执行 Cookie 身份、CSRF、组织、部门、环境和动作权限校验。
- AgentRun、MinutesSource、ActionItem、MinutesTask 和 NodePolicy 保存部门与环境。
- 普通员工只能读取授权部门；管理员可显式跨环境。
- 新增权限：`minutes:*`、`workflow-policy:*`、`workflow-sla:*`。
- 审计事件覆盖 `minutes.uploaded`、`minutes.analyzed`、`minutes.action_confirm`、
  `minutes.task_created`、`workflow.node_policy_published`、`task.sla_reminded`、
  `task.sla_escalated`、`task.agent_assisted`、`task.agent_proxied`。
- 数字员工以独立服务身份写任务动作，`DecisionMode=AgentProxy`，并关联 AgentRun。

## 🤖 外部边界

- 钉钉：`Disconnected`，只支持手动上传或粘贴。
- 音频转写：不实现。
- DOCX/PDF：可保存原件，但必须提示粘贴或上传 `.txt/.md/.json` 文字稿。
- AiGateway：`RuleSimulator`，`ExternalModelCalled=false`。
- 真实模型：未启用；无真实端点、凭据和数据分级授权。

## 🧪 测试矩阵

- 听记文本/文件类型、大小、权限和环境。
- 规则提取多条行动项、人工修改和确认。
- 行动项重复确认幂等及任务反查。
- 节点策略版本冻结、30 分钟提醒、60 分钟升级、重复扫描幂等。
- 低风险代理成功、高风险代理拒绝、服务身份不能自我确认。
- Gateway 成功、超时、模型未授权和调用证据查询。
- 部门/组织/环境越权拒绝。
- 005 up/down/re-up，WF-001 至 WF-004 回归。
- 前端组件、OpenAPI、桌面和 390px E2E。

## 🚫 明确不做

- 真实钉钉 API、音频转写、真实消息监听。
- 会签、或签、完整转办/委托/加签产品。
- 真实 U9/ERP/DCS/WMS/LIMS、正式 SSO、PMO 项目模型。
- 数字员工付款、质量放行、配方修改、设备控制或超时自动批准高风险节点。

## ↩️ 回滚

- 代码按功能提交回退。
- 数据库先恢复到迁移前备份；隔离库执行 005 down 验证。
- 应用回滚到 `req-batch1-final6`，保留该标签和发布包。
