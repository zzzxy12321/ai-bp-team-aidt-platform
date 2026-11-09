# 007 项目、事件收件箱与项目任务数据模型

> Schema：`PMO`
>
> 日期：2026-11-01
>
> 功能：`PMO-001`、`PMO-002`、`EVT-001`、`TASK-002`

## 🎯 主对象原则

- `PROJECT` 注册为 `Metadata.ObjectType`。
- 项目公共标题、状态、创建人、部门、环境和版本保存在 `BusinessObject.BusinessObject`。
- 项目专属字段保存在 `PMO.ProjectProfile`，通过 `BusinessObjectId` 一对一关联。
- 成员、里程碑、任务、流程、听记、文件和审计只保存引用，不复制源对象主字段。

## 🗂️ 表

| 表 | 主键/唯一键 | 用途 |
|---|---|---|
| `PMO.ProjectProfile` | `BusinessObjectId` PK | 负责人、计划日期、目标、阶段、保密等级、进度 |
| `PMO.ProjectMember` | `(BusinessObjectId, UserId)` PK | 项目成员、角色、主负责人、软移除 |
| `PMO.ProjectMilestone` | `Id`、`(ProjectId, PlannedDate, Id)` | 里程碑、负责人、状态、完成时间和版本 |
| `PMO.ProjectRelation` | `(ProjectId, EntityType, EntityId, RelationCode)` unique | 项目到对象、流程、任务、听记、文件、事件和审计的引用 |
| `PMO.ProjectTask` | `Id`；`EventInboxId` filtered unique | 项目任务、事件来源、负责人、SLA、流程引用和进度 |
| `PMO.EventInbox` | `Id` | 手动/文件/听记/行动项/通知/附件来源事件 |
| `PMO.EventSuggestion` | `EventInboxId` unique | Agent 建议、风险、永久人工边界、缺失信息和证据片段 |

## 🔗 关联

- 项目 → 业务对象：`ProjectRelation(EntityType='BusinessObject')`
- 项目 → 流程/任务：`ProjectRelation`，并可由 `ProjectTask` 直接引用
- 项目 → 文件：`ProjectRelation(EntityType='Attachment')` 或 `Document.AttachmentLink`
- 项目 → 听记/行动项：`ProjectRelation(EntityType='MinutesSource|MinutesActionItem')`
- 项目 → 事件：`ProjectRelation(EntityType='InboxEvent')`
- 项目任务 → 事件：`ProjectTask.EventInboxId`
- 人员/部门/环境/状态：通过主对象和 `Identity` 表约束

## 🛡️ 约束

- 环境仅允许 `demo/test/prod`。
- 项目状态限定为草拟、进行中、阻塞、待验收、已完成、已终止。
- 项目进度必须在 0—100。
- 里程碑、任务、事件、建议、风险、优先级均有检查约束。
- 一个事件最多创建一个项目任务；重复确认由幂等表和服务共同保护。
- 审计、SourceEvent 和 TraceId 在业务事务内写入。

## 🚨 高风险

- `EventSuggestion.RequiresHumanBoundary=true` 的事项只能进入待确认。
- Agent 不自动创建任务、不发起流程、不关闭高风险事项。
- 财务付款、质量放行、配方、设备控制、安全联锁、合同和重大赔付仍为永久人工边界。
