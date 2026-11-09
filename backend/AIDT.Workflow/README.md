# AIDT.Workflow

## 📌 当前状态

已建立 `IWorkflowEngine` 应用端口、`WorkflowEngineAdapter` 和 Elsa 3.9 SQL 持久化运行时。
当前锁定的软件包版本为 `3.9.0`。

## 🧭 冻结边界

- Elsa 负责流程实例、人工任务、计时器、版本和持久化。
- AIDT Application 负责业务对象状态和流程命令编排。
- Hangfire 不承担人工流程状态。
- Vue 不直接操作 Elsa 数据表。

## ⏭️ 阶段 B

已验证流程版本冻结、条件分支、任务生成、通过、退回、终止和 SQL 持久化。
会签、或签、委托和 SLA 仍属于后续扩展范围。

## 🧭 处理人策略

流程配置支持指定人员、业务角色、部门负责人、项目负责人、发起人主管和 Agent。
任务创建时冻结候选人、业务角色 ID/Code/Version 和解析证据，后续成员变化不重解释历史任务。

## 🧾 节点动态表单与版本冻结

- `WorkflowNodePolicy` 保存节点业务说明、SLA、交付物、动态字段、动作、数字员工、
  AI 审查、通知和永久人工边界。
- 任务创建时冻结 `FormDefinitionJson`、`FormSchemaVersion` 和内容哈希；
  后续模板发布、停用或回滚不改变历史任务。
- `TaskInput` 值只写入既有 `WorkflowTask`；业务对象字段和证据仅保存引用。
- 通过、退回和终止由 Application 事务统一处理表单校验、任务动作、审计和 Outbox。
- AiGateway 只允许 `aidt-rule-sim`，输出为建议或草稿，不可替代真人审批。
