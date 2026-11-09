# AIDT 需求池批次 3 变更计划

> 历史说明：本文件记录批次 3 初始 Demo 计划。后续收尾修复以
> [`CHANGE_PLAN_REQUIREMENT_POOL_BATCH_3_CLOSEOUT.md`](./CHANGE_PLAN_REQUIREMENT_POOL_BATCH_3_CLOSEOUT.md)
> 和对应上下文包、影响分析为准。

> 日期：2026-11-01
>
> 分支：`feature/requirement-pool-batch-3`
>
> 定位：最后一个业务 Demo 闭环批次，不代表整个 AIDT 生产项目完成

## 🎯 变更目标

完成 `IDEA-ROLE-001`、`IDEA-AGENT-002`、项目主对象/成员/里程碑、事件转项目任务四项范围，形成可从企业事件输入、AI 建议、真人确认、项目任务创建到证据追溯的可运行纵向切片。

## 🆔 正式功能

| Feature ID | 名称 | 范围 |
|---|---|---|
| `PMO-001` | 项目主对象与生命周期 | `PROJECT` 对象类型、项目档案、状态、部门/环境、负责人和保密等级 |
| `PMO-002` | 项目成员、里程碑与项目工作台 | 成员、主负责人、里程碑、逾期风险和项目证据时间线 |
| `EVT-001` | 企业事件收集收件箱 | 手动文本、TXT/MD/JSON/DOCX 上传、选择听记/通知/行动项/附件 |
| `TASK-002` | 事件行动项转项目任务 | AI 规则建议、真人确认、幂等任务、项目进度和来源反查 |

## 🧱 数据设计

### 主对象

- `Metadata.ObjectType` 新增 `PROJECT` 及必需字段定义。
- 项目主记录存储在 `BusinessObject.BusinessObject`。
- `BusinessObject` 增量增加公共标题和创建人字段；部门、环境、版本和状态继续由主对象保存。
- `BusinessObjectStatus` 增加 `Blocked`、`AwaitingAcceptance`、`Completed`、`Terminated`，数据库状态约束同步扩展。
- 项目专属字段只保存在 `PMO.ProjectProfile`，通过 `BusinessObjectId` 一对一关联。

### 新增表

| 表 | 责任 |
|---|---|
| `PMO.ProjectProfile` | 项目负责人、计划日期、目标、当前阶段、保密等级 |
| `PMO.ProjectMember` | 项目成员、项目角色、主负责人标记、移除历史 |
| `PMO.ProjectMilestone` | 里程碑、负责人、计划日期、状态、完成时间、版本 |
| `PMO.ProjectRelation` | 项目到对象、流程、任务、听记、行动项、附件、审计和里程碑的引用 |
| `PMO.ProjectTask` | 项目任务、事件来源、执行人、截止日期、优先级、状态、流程关联 |
| `PMO.EventInbox` | 企业事件原文、来源、附件、状态、分析运行 |
| `PMO.EventSuggestion` | 建议项目/对象/负责人/流程/截止/优先级/风险/缺失信息和证据片段 |

## 🔐 身份与权限

- 新增本地演示用户 `demo.pm`，角色 `project-manager`，部门范围研发。
- 新增权限：
  - `project:read/create/update`
  - `project-member:read/manage`
  - `milestone:read/manage`
  - `event-inbox:read/confirm`
  - `project-task:read/create`
  - `project-audit:read`
- 项目经理可读取授权部门项目并管理自己负责或部门负责人可见的项目。
- 普通员工可读取自己参与的项目和项目任务，但不能修改成员/里程碑。
- 非项目成员不能通过直接 ID 读取；平台管理员可跨部门。
- 拒绝访问写审计。

## 🔁 事件流程

```text
手动文本/文件/听记/通知/行动项/附件
  → EventInbox（Received）
  → AgentRun（RuleSimulator，不调用外部模型）
  → EventSuggestion（Suggested / HighRisk）
  → 真人确认
  → ProjectTask 或关联已有项目/流程
  → ProjectRelation + Audit
```

硬边界：

- 分析阶段不创建任务、不发起流程、不修改项目完成状态。
- 高风险关键词只生成高风险建议；永远不自动发起付款、质量放行、配方、设备控制、安全联锁、合同或赔付流程。
- 确认接口必须幂等，同一事件只创建一个项目任务。
- 高风险事项仍需真人显式确认，Agent 不可关闭。

## 🖥️ 前端

- `/projects`：我的项目、部门项目、状态、阶段、里程碑进度、任务风险和待确认事件。
- `/projects/:id`：项目档案、成员、里程碑、任务、关联对象/流程/文件/听记和审计时间线。
- `/inbox`：粘贴文本、上传文本、选择来源、分析、选择项目和确认创建任务。
- 项目经理入口与普通员工入口共用，按钮和策略配置按权限隐藏且后端强制校验。
- 桌面和 390px 移动端均需可用。

## 🧪 验证范围

- Domain：项目状态、里程碑逾期、成员权限、高风险事件规则。
- SQL/API：创建项目、成员/里程碑、直接 ID 越权、事件分析零副作用、幂等确认、任务完成、高风险边界、证据反查。
- 前端：类型、组件、OpenAPI、构建和桌面/移动 E2E。
- 迁移：006→007 up/down/re-up；主库隔离验证后升级。
- 发布：备份、部署、应用回滚、数据库恢复、干净 manifest 和 `req-batch3-final1`。

## 🚫 明确不做

- 真实钉钉/邮件/会议监听、音频转写和真实大模型。
- 正式 SSO/OIDC/MFA/企业目录。
- 资源工时、WBS 自动排程、成本预算、关键路径和项目组合。
- 真实 U9/ERP/DCS/WMS/LIMS 写入。
- 生产 DBA、高可用、灾备和正式上线。

## ↩️ 回滚

- 代码按批次 3 分支撤销或部署上一版本。
- 007 down 删除 PMO 表、项目字段扩展、角色/权限/用户和 `PROJECT` 元数据。
- 应用回滚后恢复 006 主库备份；发布包保留且不覆盖 final1/final2/final3。

## 🧭 完成状态

仅当全部验收项、测试、迁移、部署、回滚和文档通过后写入：

```text
REQUIREMENT_POOL_BATCH_3_DEMO_COMPLETE
```

整个 AIDT 生产项目仍不完成。
