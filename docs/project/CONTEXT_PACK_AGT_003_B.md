# AGT-003-B 上下文包

状态：`COMPLETE_DEPLOYED_AND_FROZEN`

## 当前基线

- `main@337dd51`，工作区干净。
- `req-ui-step5-frozen-v1`、`req-ui-step6a-frozen-v1` 存在。
- 当前部署：`req-ui-step6a-final1`。
- 最大迁移：015，68 张应用表。
- 6A 已完成身份版本、组织权限、双人审核、MCP 服务令牌撤销和审计。

## 当前前端

- `/agents` 主要是三张身份卡和运行表格，只调用有效候选和 AgentRun。
- 配置中心/流程策略已调用 `/api/v1/agents`，后端只返回 Published + Active 身份。
- MCP 中心已选择有效数字员工签发服务令牌，但缺少身份名称、不可用状态和撤销原因展示。
- 现有基础组件：`StatusBadge`、`FeaturePlaceholder`、统一 HTTP 客户端和 API Error。

## 6A 可复用 API

- `GET /agents/manage`、`GET /agents/{id}`、生命周期/版本/回滚/审核 API。
- `GET /agents/runs`、GatewayCalls、流程任务、MCP Snapshot、Audit。
- 所有写接口均有幂等键、Revision/Version 和业务错误消息。

## 后端缺口

- 前端无权使用 IAM 全量人员接口，需要 Agent 专用候选人/部门/能力选项端点。
- 详情证据分散在多个接口，需要稳定聚合 Context。
- Agent 列表缺少最近运行时间和次数。
- MCP 概要没有关联身份名称与当前有效状态。

## 关键决策

- 只补只读聚合和业务元数据，不把权限判断迁移到前端。
- 表单使用后端 options，不维护第二套能力目录。
- 发布/停用/回滚全部调用 6A API，不使用本地状态伪造。
- 普通用户调用治理列表只返回其可见有效身份；服务身份不进入浏览器治理会话。
- 016 只补三个业务角色的 `agent-read`，不改其它权限或数据范围。

## 关键入口

- `frontend/src/pages/AgentPage.vue`
- `frontend/src/features/agents/AgentIdentityEditor.vue`
- `frontend/src/api/modules/agentApi.ts`
- `frontend/src/pages/ConfigurationCenterPage.vue`
- `frontend/src/pages/WorkflowPoliciesPage.vue`
- `frontend/src/pages/McpCenterPage.vue`
- `backend/AIDT.Api/Controllers/AgentsController.cs`
- `backend/AIDT.Infrastructure/Services/SqlAgentGovernanceService.cs`

## 验证边界

- 不连接真实钉钉、U9、ERP、DCS 或外部大模型。
- 使用现有部署专用 MCP OAuth 和测试能力完成联动验收。

## 实施结论

- `/agents` 已改为真实身份中心，网页完成新建、编辑、校验、发布、停用、恢复、回滚、
  运行请求和高风险审核。
- 新 API `GET /agents/options` 和 `GET /agents/{id}/context` 已由 SQL/API 测试覆盖。
- 016 已应用于主库，新增三个角色 `agent-read` 和六个未绑定服务账号。
- 当前源码门禁：后端/SQL/API 172/172、MCP 11/11、Vitest 41/41、
  OpenAPI 162/183、源码 E2E 44 通过 + 1 个部署专用用例跳过。
- 正式发布 `req-ui-step6-final1`、正式 E2E 45/45、应用回滚、最终恢复和冻结均完成。

