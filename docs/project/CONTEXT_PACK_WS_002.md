# WS-002 可配置岗位工作台模板上下文包

## 🎯 目标声明

在不复制业务数据、不新建部门后端的前提下，让六类岗位进入部门工作区后看到由服务端匹配并有权限过滤的真实业务首屏。

## 🧱 已有基线

- `main@67fa2a8`，前序冻结 `req-ui-step3-frozen-v1`。
- 当前部署 `req-ui-step3-final1`，源码 `e8db2b2131c6d9807c1f1bdf6dac7eedefcdd59b`。
- 11 条迁移、62 张应用表。
- 正式 E2E 32/32；后端/SQL/API/MCP 146/146；Vitest 27/27。

## 🧩 可复用能力

| 能力 | 现有入口 |
|---|---|
| 部门范围与成员 | `Identity.Department`、`UserDepartmentMembership`、`UserPrincipal` |
| 角色和用户角色 | `Identity.Role`、`UserRole`、`UserRoleAssignment` |
| 统一事项 | `GET /api/v1/my-work` 及 `SqlEmployeeWorkspaceService` |
| 部门工作区 | `GET /api/v1/departments/{id}/workspace` |
| 项目 | `/api/v1/projects`、项目成员/任务/里程碑/关系 |
| 流程任务与 SLA | `/api/v1/workflow-tasks`、节点策略与通知 |
| 业务对象 | `/api/v1/business-objects` |
| 文件 | `/api/v1/files` |
| 听记 | `/api/v1/minutes` |
| Agent | `/api/v1/agents/runs` |
| 导航配置 | `/api/v1/navigation/workspaces` |

## 🧱 当前页面和代码入口

- `frontend/src/pages/DepartmentPage.vue`
- `frontend/src/pages/MyWorkPage.vue`
- `frontend/src/features/iam/components/IamNavigationPanel.vue`
- `frontend/src/pages/IamCenterPage.vue`
- `frontend/src/api/modules/workspaceApi.ts`
- `backend/AIDT.Infrastructure/Services/SqlEmployeeWorkspaceService.cs`
- `backend/AIDT.Infrastructure/Services/SqlNavigationService.cs`
- `backend/AIDT.Api/Controllers/DepartmentsController.cs`
- `backend/AIDT.Api/Controllers/IamController.cs`
- `backend/AIDT.Infrastructure/Persistence/AidtDbContext.cs`
- `backend/AIDT.Infrastructure/Persistence/Entities.cs`

## 🔐 权威边界

- 导航和工作台分离：导航控制入口，模板控制首屏。
- 模板解析、匹配、权限、组织/部门/项目/环境过滤全部在服务端。
- 卡片数据必须沿用原业务读取权限，不因模板存在而扩大。
- `NotConnected` 与 `Denied` 必须分开；无权限不能伪装成“暂无”。
- 永久人工边界不提供绕过动作。

## 📚 本次必读文档

- `docs/001-产品PRD/00-项目目标.md`
- `docs/001-产品PRD/01-需求基线.md`
- `docs/001-产品PRD/02-技术栈与架构.md`
- `docs/001-产品PRD/03-计划方案.md`
- `docs/001-产品PRD/04-需求池.md`
- `docs/project/PMO_003_DELIVERY.md`
- `docs/project/UX_004_DELIVERY.md`
- `docs/project/FEATURE_MAP.md`
- `docs/project/PERMISSION_MATRIX.md`
- `task_plan.md`、`findings.md`、`progress.md`

## 🧪 当前回归基线

- 原 32/32 正式 E2E 必须不回归。
- 146/146 后端/SQL/API/MCP 必须保持全通过，并新增 WS-002 用例。
- 27/27 Vitest 必须保持全通过，并新增模板配置和卡片状态用例。
- OpenAPI 必须登记全部新增路径和 operationId。
