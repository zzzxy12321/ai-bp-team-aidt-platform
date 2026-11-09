# AIDT 需求池批次 2.1 上下文包

## 当前目标

完成批次 2 的默认数据、DOCX、策略配置和自动代理可用性，达到
`REQUIREMENT_POOL_BATCH_2_1_READY`。

## 相关功能

- `MIN-001`
- `WF-005`
- `AGT-002`
- `AIG-002`

## 相关页面

- `frontend/src/pages/MinutesPage.vue`
- `frontend/src/pages/WorkflowPage.vue`
- 新增 `frontend/src/pages/WorkflowPoliciesPage.vue`

## 相关 API

- `/api/v1/minutes/upload`
- `/api/v1/workflow-policy/users`
- `/api/v1/workflow-versions/{id}/nodes`
- `/api/v1/workflow-versions/{id}/node-policies`
- `/api/v1/workflow-sla/actions/run`

## 相关数据

- `Minutes.MinutesSource`
- `Workflow.WorkflowNodePolicy`
- `Workflow.WorkflowTask`
- `Workflow.WorkflowNotification`
- `Agent.AgentProfile`
- `Agent.AgentRun`

## 关键边界

- DOCX 只读正文段落，不做 OCR。
- PDF 不解析。
- 自动代理只适用于显式预授权的 Low 风险、可逆节点。
- 使用任务冻结策略，不读取后续新策略。
- Agent 使用独立服务身份。
- 高风险永久人工边界绝不自动通过。
