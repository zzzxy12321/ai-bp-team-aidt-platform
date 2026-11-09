# PMO-003 影响分析

## 变更目标

完成项目详情编辑、负责人更换、成员管理、任务创建、证据关联、进度更新和状态流转的持久化操作闭环。

## 涉及功能 ID

- `PMO-001`：项目主对象与生命周期
- `PMO-002`：项目成员、里程碑与工作台
- `PMO-003`：项目操作闭环
- `TASK-002`：项目任务状态与证据追溯

## 🔄 调用链

```text
ProjectDetailPage
 → workspaceApi / fileApi / minutes API
 → ProjectsController
 → IProjectService / SqlProjectService
 → BusinessObject + PMO.ProjectProfile
   + PMO.ProjectMember + PMO.ProjectTask + PMO.ProjectRelation
 → BusinessObjectVersion + AuditEvent
 → ProjectDetailDto 重新读取
```

## 预期修改文件

- `backend/AIDT.Application/Contracts/ProjectAndInboxContracts.cs`
- `backend/AIDT.Application/Abstractions/ProjectAndInboxPorts.cs`
- `backend/AIDT.Api/Controllers/ProjectsController.cs`
- `backend/AIDT.Infrastructure/Services/SqlProjectService.cs`
- `backend/AIDT.Infrastructure/Services/NotReadyProjectAndInboxServices.cs`
- `frontend/src/api/types/contracts.ts`
- `frontend/src/api/modules/workspaceApi.ts`
- `frontend/src/pages/ProjectDetailPage.vue`
- `tests/AIDT.UnitTests/Api/ApiContractTests.cs`
- `frontend/e2e/aidt.spec.ts`
- `docs/contracts/openapi/v1.json`
- 功能地图、交付文档和规划日志

## 上游调用方

- `/projects/:id` 项目详情页。
- `/projects` 项目工作台列表。
- 项目成员候选、文件中心、听记和流程实例只读接口。
- MCP 项目任务完成与追溯工具。

## 下游依赖

- 员工统一工作事项读取 `PMO.ProjectTask`。
- 项目进度由任务和里程碑完成状态计算。
- 项目完成状态检查未完成项目任务。
- 项目时间线读取任务、事件、里程碑和项目审计。

## API 变化

- 新增 `POST /api/v1/projects/{id}/tasks`。
- 请求：标题、内容、负责人、截止时间、优先级。
- 响应：现有 `ProjectTaskDto`。
- 复用 `PUT /projects/{id}`、成员新增/移除、关系和状态动作。
- 新接口使用 `Idempotency-Key`，同键同内容返回原任务，同键异内容返回冲突。

## 数据库变化

- 无结构变化，无新迁移。
- 继续使用 `PMO.ProjectTask`、`PMO.ProjectRelation`、`PMO.ProjectMember`、`Audit.AuditEvent`。
- 迁移数保持 11，应用表保持 62。

## 权限变化

- 创建任务要求 `project-task:create`，且当前身份可管理项目。
- 任务负责人必须是该项目有效成员。
- 编辑/状态/关系继续要求 `project:update` 和项目管理范围。
- 成员管理继续要求 `project-member:manage`。

## 审计变化

- 新增 `project.task_created`。
- 继续复用 `project.updated`、`project.member_added`、`project.member_removed`、
  `project.relation_added`、`project.task_completed`、`project.status_changed`。

## 前端状态变化

- 新增项目编辑、成员管理、任务创建和分类证据关联弹窗。
- 写操作成功后重新加载服务端详情，不使用内存对象冒充持久化成功。
- 任务完成、里程碑完成和状态变更后刷新项目进度。

## 测试范围

- 领域：项目状态、进度公式和任务优先级。
- SQL/API：任务创建、幂等、版本冲突、成员负责人校验、编辑负责人、
  成员角色/移除非负责人、关系持久化、进度和状态刷新。
- 契约：OpenAPI operationId、请求字段和前端类型。
- Vue：项目操作规则纯函数或页面交互。
- E2E：完整操作链和 390px 可操作性。

## 回归风险

- 负责人变更必须保留唯一 Lead 和成员可访问性。
- 任务创建后可能影响项目完成阻断与进度统计。
- 关系目标必须继续执行组织和部门范围校验。
- 旧事件确认任务与直接创建任务必须共用状态规则。
- 状态选择器不能展示后端不接受的任意跳转。

## 回滚方式

代码回滚到 `main@fa1a6d4` 即可恢复旧项目操作能力。本批无数据库结构变化，
无需迁移回滚；已创建测试业务数据按环境标签保留，不进入正式演示数据。
