# AIDT 需求池批次 3 收尾上下文包

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`
>
> 分支：`fix/requirement-pool-batch-3-closeout`
>
> 基线：`main@93c4cd41129cf92033c4e0a109bee967a21a5552`

## 🎯 当前目标

关闭需求池批次 3 已确认的四个功能与持久化缺口，并用隔离库、真实 API、刷新场景 E2E、
正式部署、回滚和恢复证据形成可复核的收尾基线。

本批不把本地 Cookie 身份描述为生产 SSO，不连接真实钉钉、U9、ERP、DCS，也不启用真实外部模型。

## 🆔 功能

- `PMO-001`：项目主对象、生命周期、状态和版本。
- `PMO-002`：项目成员、里程碑、任务、文件与证据时间线。
- `EVT-001`：企业事件收件箱、Agent 建议和人工确认边界。
- `TASK-002`：事件确认后幂等创建项目任务并反查来源。

## 📂 核心代码入口

- `backend/AIDT.Infrastructure/Services/SqlEventInboxService.cs`
- `backend/AIDT.Infrastructure/Services/SqlProjectService.cs`
- `backend/AIDT.Infrastructure/Services/SqlFileCenterService.cs`
- `backend/AIDT.Domain/Projects/ProjectModels.cs`
- `backend/AIDT.Infrastructure/Configuration/AidtRuntimeOptions.cs`
- `backend/AIDT.Infrastructure/DependencyInjection.cs`
- `backend/AIDT.Api/Configuration/RuntimeOptionsValidation.cs`
- `backend/AIDT.Api/appsettings.json`
- `backend/AIDT.Worker/appsettings.json`
- `frontend/src/pages/ProjectDetailPage.vue`
- `frontend/e2e/aidt.spec.ts`
- `tests/AIDT.UnitTests/Api/ApiContractTests.cs`
- `tests/AIDT.UnitTests/Domain/ProjectRulesTests.cs`
- `database/repairs/BATCH3_CLOSEOUT_DATA_REPAIR_20261101.sql`

## 🗄️ 数据库与迁移边界

- 主库迁移基线为 001–009，均不得修改。
- 本批不新增结构迁移；隔离库与主库继续使用 9 条 EF 迁移、57 张应用表。
- 历史不一致使用一次性、事务化、幂等修护脚本，不篡改审计历史。
- 修护脚本只从 `EventSuggestion.Status` 和明确的成功状态审计推导可证明的状态。
- PROJECT 文件证据关系按现有 `Attachment.BusinessObjectId` 和 `ProjectRelation` 补建。

## 🔁 关键不变量

### 事件

```text
Received
 → Agent 分析
 → AwaitingConfirmation / HighRisk
 → 真人确认
 → Confirmed
 → 同事务幂等创建最多一个 ProjectTask
```

- 分析/确认必须更新真实跟踪的 `EventInbox`。
- 事件与建议状态必须在同一事务中提交。
- 刷新列表和重新 GET 必须返回已持久化状态。
- 已确认事件不再显示“真人确认并创建项目任务”。

### 项目

```text
Active ⇄ Blocked
Active → AwaitingAcceptance
AwaitingAcceptance → Completed
任意有效状态 → Terminated（按既定规则）
```

- 状态、版本、进度、版本快照和审计必须一致。
- 有 Pending/InProgress 项目任务时不得完成项目。
- Completed 时 `ProgressPercent=100`。
- 流程完成不自动关闭项目，历史流程版本不回算。

### 项目文件证据

- 附件关联 `PROJECT` 时，在原上传事务中同时保留 `AttachmentLink` 并创建
  `ProjectRelation(EntityType=Attachment, RelationCode=SupportingEvidence)`。
- 同一项目/附件/关系编码最多一条记录。
- 非 PROJECT 对象继续只走原有 `AttachmentLink` 行为。

### 业务时间

- 集中配置：`AIDT:BusinessTime:TimeZoneId=Asia/Shanghai`、`DeadlineHour=18`。
- `截止日期：2025-08-15` 解释为 `2025-08-15 18:00 +08:00`。
- 数据库和 API 互操作仍使用 UTC；上海本地展示必须为 2025-08-15，而不是次日。
- 事件建议、事件确认和项目任务复用同一截止时间值。

## 🔐 权限与数据范围

- 项目经理通过 `project:update` 和项目负责人/授权部门范围变更项目状态。
- 项目经理的流程审批能力沿用前端导航与 IAM 收尾结果：
  `workflow-task:approve/return/terminate` 已补齐，但仍必须满足本人任务、冻结候选人和部门范围。
- 文件上传仍校验部门、环境、权限、保密等级和 MIME/签名。
- 事件确认必须为真人权限；Agent 不得自确认。
- 本批不新增权限定义，不绕过后端 403。

## 🧪 验证入口

- 后端/SQL/API：`dotnet test AIDT.slnx --no-build --no-restore`，隔离库显式开启 SQL 测试。
- Domain 精确时间：`TryExtractDeadline_ParsesExplicitDate`。
- API 持久化：`Inbox_StatusPersistsAcrossFreshReadsAndSharedDeadline`、
  `ProjectStatusTransitions_PersistVersionProgressAndCompletionGuards`、
  `ProjectFileUpload_AutoLinksProjectEvidenceAndPreservesOtherObjects`。
- 前端：类型检查、Vitest、OpenAPI、生产构建。
- 浏览器：源码及正式部署的桌面 + 390px Playwright。
- 数据：修护脚本在隔离库前测、执行、复测、幂等重跑；主库执行前后计数。
- 发布：干净 manifest、应用回滚、部署备份 `RESTORE VERIFYONLY`、独立恢复库核对。

## 🚫 明确排除

- 正式 SSO/OIDC/MFA 和企业目录。
- 真实钉钉、邮件、聊天、会议监听、音频转写和外部模型。
- 真实 U9/ERP/DCS/WMS/LIMS 写入及外部副作用。
- WBS、成本预算、资源工时、生产高可用和公网部署。
- 与本批无直接关系的“已完成任务仍显示在我的待办”等既有问题。
