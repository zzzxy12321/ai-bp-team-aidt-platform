# DEPT-WF-001 上下文包

## 🎯 当前目标

交付部门优先工作空间、部门可视流程设计、节点级动态表单、运行时冻结表单、
权限边界和四部门试点，完成部署、回滚、恢复、文档与冻结。

## 🧱 冻结基线

- 仓库：`D:/desktop/工作区合区/Vibecoding-目标模式/coding`
- 基线：`main@3e94f3a`
- 当前部署：`req-ui-step7-final1`
- 最大迁移：017
- 前序冻结：`req-ui-step7-frozen-v1`、`req-ui-step7a-frozen-v1`
- 技术栈：.NET 10、SQL Server、Vue 3、Element Plus、Elsa、Hangfire、Garnet。

## 🔑 关键资产

- 导航：`frontend/src/layouts/AppLayout.vue`、
  `SqlNavigationService.cs`、`NavigationContracts.cs`、
  `AidtModelConfigurations.Navigation.cs`
- 配置：`ConfigurationCenterPage.vue`、`ConfigurationContracts.cs`、
  `SqlWorkflowConfigurationService.cs`、`Configuration.ConfigurationVersion`
- 流程：`WorkflowModels.cs`、`Workflow.WorkflowDefinition/Version/Task/NodePolicy`、
  `ProcessWorkflowTaskHandler.cs`
- 对象：`BusinessModelingPage.vue`、`ObjectType/FieldDefinition/BusinessObject`
- 场景/文件/听记/Agent/审计：沿用现有 017 及前序服务，不建第二套真源。
- 契约：`docs/contracts/openapi/v1.json`、`frontend/src/api/types/contracts.ts`

## 🧭 设计口径

- “模板”是同一业务对象类型下可执行的流程配置视图，稳定编码和发布指针必须有真源；
  但仍必须落到既有 `ConfigurationVersion` 与 `WorkflowDefinition/Version`。
- 表单字段只有三类来源：业务对象引用、任务输入、证据引用。
- 发布模板不可原地覆盖；历史任务绑定启动时流程版本和节点表单版本。
- 调整运行时必须通过既有 Application 用例与真实 `WorkflowTask`，复用权限、审计和事务。
- 数字员工只通过已注册能力/既有 AgentRun 和 AiGateway 规则模拟。

## 🚦 用户已决定

- 四阶段连续执行，不逐段停问。
- 部门自服务普通流程，高风险与跨部门仍需平台审核。
- 不新增第二套流程模型、第二套前端页面或真实外部连接。
- 原目标为 `req-ui-step8-final1`；因正式登录 502 和测试导航隔离缺陷，
  final1/final2 均未冻结，最终以 `req-ui-step8-final3` 通过全部门禁并冻结。

## 🔍 当前风险

- 当前主视图存在测试残留，基线数量需从数据库只读核实。
- 导航硬编码与数据驱动可能并存，必须统一到服务端真源。
- 旧 Cookie 权限可能不含新授权，需明确提示重新登录。
- 现有配置 JSON 和前端类型兼容范围大，需先做结构盘点。
- 运行任务表单写入必须同时处理并发、幂等、附件、审计和 Outbox。

## ✅ 最新断点

- 8A–8D 已通过四个连续逻辑分支提交并非快进合并 `main`。
- 018 在主库完成应用；正式版本 `req-ui-step8-final3` 已部署并冻结。
- 后端/SQL/API 186、MCP 11、Vitest 47、OpenAPI 187/208、
  源码 E2E 52+1 skip、正式 E2E 53/53。
- 最终备份与独立恢复验证通过；当前无下一步开发门禁。

