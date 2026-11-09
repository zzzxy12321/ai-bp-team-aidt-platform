# PMO-003 最小上下文包

## 🎯 当前目标

让项目详情成为可直接操作的项目空间，并把所有操作持久化到现有项目主对象、成员、任务、关系和审计账本。

## 🧩 相关功能

- `PMO-001`：项目主对象、状态、版本和部门范围。
- `PMO-002`：成员、负责人、里程碑、任务和证据时间线。
- `PMO-003`：本批项目操作闭环。
- `TASK-002`：任务完成、进度重算和来源反查。
- `DOC-001`：文件中心与项目 SupportingEvidence。
- `MIN-001`：听记来源。
- `WF-001`：流程实例。

## 🖥️ 前端

- `frontend/src/pages/ProjectDetailPage.vue`
- `frontend/src/pages/ProjectsPage.vue`
- `frontend/src/api/modules/workspaceApi.ts`
- `frontend/src/api/modules/fileApi.ts`
- `frontend/src/api/types/contracts.ts`
- `frontend/src/main.ts`（仅在需要新增 Element Plus 组件时）

## ⚙️ 后端

- `backend/AIDT.Api/Controllers/ProjectsController.cs`
- `backend/AIDT.Application/Abstractions/ProjectAndInboxPorts.cs`
- `backend/AIDT.Application/Contracts/ProjectAndInboxContracts.cs`
- `backend/AIDT.Infrastructure/Services/SqlProjectService.cs`
- `backend/AIDT.Infrastructure/Services/NotReadyProjectAndInboxServices.cs`
- `backend/AIDT.Application/Mcp/McpToolCatalog.cs`（只在需要同步 MCP 时）

## 🗄️ 数据

读写：

- `BusinessObject.BusinessObject`
- `BusinessObject.BusinessObjectVersion`
- `PMO.ProjectProfile`
- `PMO.ProjectMember`
- `PMO.ProjectTask`
- `PMO.ProjectRelation`
- `Audit.AuditEvent`

只读：

- `Document.Attachment`
- `Minutes.MinutesSource`
- `Workflow.WorkflowInstance`
- `PMO.EventInbox`

当前最大迁移为 011；本批预计无迁移。

## 🧪 测试

- `tests/AIDT.UnitTests/Api/ApiContractTests.cs`
- `tests/AIDT.UnitTests/Contracts/MigrationModelTests.cs`
- `frontend/e2e/aidt.spec.ts`
- OpenAPI 契约检查脚本

## ⚠️ 风险

- 新负责人必须成为 Lead，原 Lead 不能残留。
- 非成员不能作为任务负责人。
- 直接任务不能破坏事件任务唯一约束。
- 关系目标必须同时校验组织、项目和部门范围。
- 项目完成动作必须继续阻断未完成任务。
- 390px 下弹窗、表单和长列表不得出现页面级横向溢出。
