# 005 批次 2 数据模型

## Minutes

| 表 | 用途 |
|---|---|
| `Minutes.MinutesSource` | 手动文字稿/文件来源、会议、环境、分析版本和 AgentRun |
| `Minutes.MinutesActionItem` | 发言人、时间戳、原文、草稿、人工确认和去向 |
| `Minutes.MinutesTask` | 确认后创建的协同任务，可关联业务对象或流程 |

会议和行动项不是新的业务主账本。业务对象仍由 `BusinessObject` 模块持有。

## Workflow

| 表/字段 | 用途 |
|---|---|
| `Workflow.WorkflowNodePolicy` | 按 WorkflowVersion + NodeCode 保存版本化审批、SLA、Agent 授权与前置检查 |
| `Workflow.WorkflowNotification` | 站内提醒、升级和代理结果通知 |
| `WorkflowTask.NodePolicyId/DueAt` | 启动任务时冻结策略引用与截止时间 |
| `WorkflowTask.DecisionMode/AgentRunId` | 区分 Human 与 AgentProxy，关联独立 AgentRun |

## Agent

| 表/字段 | 用途 |
|---|---|
| `Agent.AgentProfile.ServiceUserId` | 独立数字员工服务身份 |
| `Agent.AgentRun` 业务关联 | 组织、部门、环境、对象、流程、任务、MinutesSource |
| `Agent.AgentGatewayCall` | 模型、模式、Gateway 调用 ID、摘要、耗时和错误 |

## 约束

- 节点策略 `Sla/Reminder/Escalation` 非负且提醒/升级不得晚于 SLA。
- AgentProfile 服务身份唯一。
- MinutesTask 与 ActionItem 一对一唯一。
- WorkflowNotification 幂等键唯一。
