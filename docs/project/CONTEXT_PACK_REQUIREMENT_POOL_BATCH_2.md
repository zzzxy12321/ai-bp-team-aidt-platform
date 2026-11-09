# AIDT 需求池批次 2 上下文包

## 当前目标

完成 `MIN-001`、`WF-005`、`AGT-002`、`AIG-002`，状态达到
`REQUIREMENT_POOL_BATCH_2_DEMO_COMPLETE`。

## 相关模块

- Domain：`AIDT.Domain.Workflows`、`AIDT.Domain.Agents`
- Application：AgentRun、Workflow Task、DTO 和端口
- Infrastructure：SQL 仓储、Minutes 服务、SLA 处理、AiGateway 客户端
- Api：Minutes、Workflow、Agent 控制器
- Worker：SLA 定时扫描
- AiGateway：受控调用审计
- Frontend：Minutes、Workflow、Agent 页面

## 相关数据

- `Document.Attachment` / `DocumentVersion` / `AttachmentLink`
- `Workflow.WorkflowVersion` / `WorkflowInstance` / `WorkflowTask`
- `Agent.AgentProfile` / `AgentRun` / `AgentConfirmation`
- `Audit.AuditEvent`
- `Integration.IdempotencyRecord`

## 关键约束

- 业务对象仍是业务主数据，分钟只作为来源。
- 人工确认前不创建任务或流程。
- 节点策略必须固定到流程版本；任务引用创建时策略。
- 高风险动作永不自动通过。
- AiGateway 仅 RuleSimulator，`ExternalModelCalled=false`。

## 相关测试

- `ApiContractTests`
- `WorkflowTests`
- `AgentTests`
- `MigrationModelTests`
- `FileContentValidatorTests`
- `frontend/e2e/aidt.spec.ts`

## 已知风险

- 现有流程引擎没有通知模块，本批使用结构化 WorkflowNotification。
- 现有 AgentRun 模型未绑定业务，需兼容扩展且不破坏批次 1 API。
- 代理动作必须复用流程推进，不得伪造真人审批字段。
