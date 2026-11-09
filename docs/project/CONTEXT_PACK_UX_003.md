# UX-003 最小上下文包

## 🎯 当前目标

统一员工待处理事项的只读展示模型和“我的工作”入口。

## 🧩 相关功能

- `UX-001`：员工优先入口
- `TASK-001`：流程待办
- `PMO-002`：项目任务
- `EVT-001`：事件确认
- `MIN-001`：听记行动项
- `MCP-001`：MCP 真人确认
- `AGT-001` / `AGT-002`：AgentRun 真人确认
- `UX-003`：本批统一工作事项中心

## 🖥️ 前端

- `frontend/src/pages/MyWorkPage.vue`
- `frontend/src/features/work-items/components/UnifiedWorkItemCard.vue`
- `frontend/src/pages/InboxPage.vue`（深链聚焦）
- `frontend/src/pages/MinutesPage.vue`（深链聚焦）
- `frontend/src/pages/AgentPage.vue`（深链聚焦）
- `frontend/src/api/types/contracts.ts`

## ⚙️ 后端

- `backend/AIDT.Application/Contracts/EmployeeWorkspaceContracts.cs`
- `backend/AIDT.Application/WorkItems/WorkItemPresentation.cs`
- `backend/AIDT.Infrastructure/Services/SqlEmployeeWorkspaceService.cs`
- `backend/AIDT.Infrastructure/Services/RuntimeServices.cs`

## 🗄️ 数据

只读使用：

- `Workflow.WorkflowTask`
- `PMO.ProjectTask`
- `PMO.EventInbox`
- `Minutes.MinutesActionItem`
- `Mcp.HumanConfirmation`
- `Agent.AgentRun`

当前最大迁移为 010；本批预计无迁移。

## 🧪 测试

- `tests/AIDT.UnitTests/Application/WorkItemPresentationTests.cs`
- `tests/AIDT.UnitTests/Api/ApiContractTests.cs`
- `frontend/src/features/work-items/components/UnifiedWorkItemCard.test.ts`
- `frontend/e2e/aidt.spec.ts`

## ⚠️ 风险

- EF 跨模块只读 join 的可翻译性和查询规模。
- 新事件与生成任务、项目任务与流程任务的重复展示。
- 普通员工和项目负责人权限差异。
- MCP 确认无独立部门字段，只能按当前用户与组织限定。
