# 005 批次 2 DBA 审核

## 目的

新增 AI 听记、行动项、协同任务、Workflow 节点策略、SLA 通知和 AgentGatewayCall，
并扩展 AgentProfile、AgentRun 与 WorkflowTask。

## 影响对象

- 新 Schema：`Minutes`
- 新表：`MinutesSource`、`MinutesActionItem`、`MinutesTask`
- 新表：`Workflow.WorkflowNodePolicy`、`Workflow.WorkflowNotification`
- 新表：`Agent.AgentGatewayCall`
- 扩展表：`Agent.AgentProfile`、`Agent.AgentRun`、`Workflow.WorkflowTask`

## 数据回填

- 现有 AgentProfile 关联独立服务用户。
- 现有 AgentRun 按发起人回填组织、主部门、demo 环境和测试标记。
- 现有 WorkflowTask 回填 `DecisionMode=Human`、`SlaStatus=None`。
- 新增 `agent.proxy` 独立服务身份与低风险代理 Profile。

## 锁表与耗时

- 当前 Demo 数据量小，审批表采用简单 `ALTER TABLE`。
- 生产前需在接近生产规模的数据副本重新评估锁表和执行时长。

## 回滚

- 005 down 删除新增表、列、权限、代理 Profile 和服务用户。
- 历史 AgentRun 原列数据不删除。
- 主库回滚前从部署恢复点恢复，不与应用回滚混为一谈。

## 验证

- 隔离库 004→005 up、005 down、005 re-up 通过。
- 迁移后 5 条 EF 迁移、44 张应用表。
- 新权限 8 项，AgentProfile 2 个，历史 AgentRun 作用域空值 0。
