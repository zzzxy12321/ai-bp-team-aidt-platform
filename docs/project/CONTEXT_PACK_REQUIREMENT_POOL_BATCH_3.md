# AIDT 需求池批次 3 上下文包

> 历史说明：本文件记录批次 3 初始 Demo 上下文。当前持久化缺口、文件证据、时区和
> 历史数据修护以
> [`CONTEXT_PACK_REQUIREMENT_POOL_BATCH_3_CLOSEOUT.md`](./CONTEXT_PACK_REQUIREMENT_POOL_BATCH_3_CLOSEOUT.md)
> 为准。

## 🎯 当前目标

在现有 SQL Server、Elsa、Vue 和权限体系上完成最后一个业务 Demo 闭环，不更换技术栈，不把规则模拟描述为真实 AI。

## 🆔 功能

- `PMO-001`：项目主对象、项目档案和生命周期。
- `PMO-002`：成员、里程碑、项目工作台、风险与证据时间线。
- `EVT-001`：企业事件收集、文件/听记/通知/行动项/附件来源。
- `TASK-002`：事件建议、真人确认、项目任务、幂等和来源反查。

## 📂 后端入口

- `backend/AIDT.Domain/BusinessObjects/BusinessObject.cs`
- `backend/AIDT.Domain/Identity/UserPrincipal.cs`
- `backend/AIDT.Domain/Workflows/WorkflowModels.cs`
- `backend/AIDT.Application/Contracts/CommonContracts.cs`
- `backend/AIDT.Application/Contracts/Batch2Contracts.cs`
- `backend/AIDT.Application/Abstractions/Ports.cs`
- `backend/AIDT.Application/Abstractions/Batch2Ports.cs`
- `backend/AIDT.Infrastructure/Persistence/Entities.cs`
- `backend/AIDT.Infrastructure/Persistence/AidtDbContext.cs`
- `backend/AIDT.Infrastructure/Persistence/AidtModelConfigurations*.cs`
- `backend/AIDT.Infrastructure/Services/SqlBatch2Service.cs`
- `backend/AIDT.Infrastructure/Services/RuntimeServices.cs`
- `backend/AIDT.Api/Controllers/WorkflowController.cs`
- `backend/AIDT.Api/Controllers/MinutesController.cs`
- `backend/AIDT.Api/Program.cs`

## 📂 前端入口

- `frontend/src/router/index.ts`
- `frontend/src/layouts/AppLayout.vue`
- `frontend/src/pages/LoginPage.vue`
- `frontend/src/pages/WorkflowPage.vue`
- `frontend/src/pages/MinutesPage.vue`
- `frontend/src/api/modules/workspaceApi.ts`
- `frontend/src/api/types/contracts.ts`
- `frontend/src/style.css`

## 🗄️ 现有数据

- 主库 `AIDT`：6 条 EF 迁移、44 张应用表。
- 业务对象唯一主账本：`BusinessObject.BusinessObject`。
- 流程与任务已有业务对象引用、部门/环境、文件和听记证据。
- 听记已有 `MinutesSource`、`MinutesActionItem`、`MinutesTask`。
- 附件已有 `AttachmentLink`，允许 `BusinessObject`、`WorkflowInstance`、`WorkflowTask`。
- Agent 已有 `AgentRun`、`AgentGatewayCall` 和 RuleSimulator。

## 🔗 关键调用链

```text
ProjectsPage/ProjectDetailPage
 → projectApi
 → ProjectsController
 → IProjectService (SQL)
 → BusinessObject + PMO.ProjectProfile/Member/Milestone/Relation/Task
 → Audit

InboxPage
 → inboxApi
 → InboxController
 → IEventInboxService (SQL)
 → EventInbox + AgentRun/EventSuggestion
 → 真人确认
 → ProjectTask + ProjectRelation + Audit
```

## 🔐 权限与数据范围

- `demo.pm` 为 `project-manager`，研发部门范围。
- 项目可见：平台管理员、项目成员、部门负责人、项目负责人。
- 项目管理：平台管理员、授权部门负责人、项目负责人且具备管理权限。
- 非成员直接 ID 读取必须拒绝并写审计。
- 普通成员不能修改成员/里程碑/项目状态。
- Agent 只能建议，不能确认事件或派发任务。

## 🚨 高风险边界

以下词义触发高风险建议：财务付款、质量放行、配方变更、DCS/PLC、安全联锁、合同承诺、客户重大赔付。系统不得自动发起或完成对应流程。

## 🧪 必测

- 项目创建、状态和部门/环境持久化。
- 项目成员、主负责人、里程碑及逾期。
- 项目经理/普通成员/跨部门/平台管理员四类访问。
- 事件多种来源、AI 建议、分析零副作用、确认幂等。
- 从项目任务反查事件、听记、文件、流程、SLA 和审计。
- 006→007 up/down/re-up、备份恢复、部署与回滚。

## 🚫 排除

真实钉钉/外部模型、音频转写、正式 SSO、资源成本、WBS 排程、关键路径、生产运维和高可用。
