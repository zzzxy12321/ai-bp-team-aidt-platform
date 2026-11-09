# 018 部门工作空间与动态表单数据模型

## 🎯 原则

018 只扩展现有真源，不新增第二套流程、任务、身份、权限、文件、审计或 Agent 系统。
业务对象仍是唯一业务主对象；`TaskInput` 只保存节点人工填写值，
`BusinessObjectReference` 只保存字段编码，`EvidenceReference` 只保存既有证据 ID。

## 🧭 导航

- `Navigation.NavigationWorkspace.IsTestData`：测试隔离标记。
- 唯一索引保证每个组织、部门只存在一个有效、已发布、非测试默认工作区。
- 历史测试入口退役，但不物理删除工作区、版本、入口或审计。

## 🧩 配置与流程

- `Configuration.ConfigurationVersion` 增加测试标记。
- `Workflow.WorkflowDefinition` 增加部门默认模板、适用岗位、风险级别和测试标记。
- `Workflow.WorkflowNodePolicy` 增加：
  - 节点名称、业务说明、SLA、交付物、相关角色和完成条件；
  - `FormSchemaJson`、`FormSchemaVersion`；
  - 节点动作、数字员工、AI 审查、通知升级和永久人工边界。
- `Workflow.WorkflowTask` 增加：
  - `NodeName`；
  - `FormDefinitionJson`、`FormSchemaVersion`、`FormDefinitionHash`；
  - `FormDataJson`。

## 🔐 约束与索引

- 节点表单和任务表单 JSON 使用 `ISJSON` 检查约束。
- 部门默认导航与部门默认模板使用过滤唯一索引，测试数据不占用业务默认位。
- 任务冻结表单通过任务 ID、流程实例、部门/环境、更新时间和 Schema 版本索引查询。
- 表单内容哈希用于服务端检测“页面定义已变化，请刷新”。

## ↩️ 回滚

- 018 down 移除新增列、约束和索引，恢复 017 结构。
- 为避免配置外键冲突，交付中心 Demo 部门和用户保留停用态，不物理删除。
- 历史任务、审计、文件、AgentRun、流程实例和业务对象不做物理删除。
