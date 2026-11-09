# AIDT 需求池批次 3 收尾交付报告

> 状态：`REQUIREMENT_POOL_BATCH_3_CLOSEOUT_COMPLETE_DEPLOYED_AND_FROZEN`
>
> 最终发布：`req-batch3-final3`
>
> 新冻结标签：`req-batch3-closeout-frozen-v1`

## 🎯 判定

批次 3 已收尾。四个已知缺口全部修复并通过重新读取、独立 SQL、桌面刷新 E2E
和正式部署验证；历史数据已完成可审计修护；应用回滚和数据库独立恢复均通过。

本判定只表示需求池批次 3 的受控 Demo/试点基线收尾，不表示整个 AIDT 达到生产上线条件。

## ✅ 四项修复

| 问题 | 修复 | 已验证证据 |
|---|---|---|
| 事件状态不持久化 | Analyze/Confirm 使用受跟踪事件；事件与 suggestion 同事务保存；重新 GET 和独立 SQL 验证 | `Inbox_StatusPersistsAcrossFreshReadsAndSharedDeadline`；刷新后 `AwaitingConfirmation` → `Confirmed`；独立 SQL 为 Confirmed/Confirmed、任务 1 |
| 项目状态不持久化 | 管理动作读取受跟踪 `BusinessObject`；状态、Version、版本快照、进度和审计一致 | `ProjectStatusTransitions_PersistVersionProgressAndCompletionGuards`；Active→Blocked→Active→AwaitingAcceptance→Completed；未完成任务 Conflict；Completed 100% |
| 项目文件无自动证据 | PROJECT 上传同一事务写 `ProjectRelation(Attachment, SupportingEvidence)`，保留 AttachmentLink | `ProjectFileUpload_AutoLinksProjectEvidenceAndPreservesOtherObjects`；重新 GET 可见；关系 1 条/附件；重复 0；非项目对象不变 |
| 截止日期偏移 | 集中 `Asia/Shanghai`/18 点；事件建议、确认和项目任务共享同一 deadline | Domain/API 精确断言：2025-08-15 18:00 +08:00，UTC 2025-08-15 10:00Z；页面显示 2025/8/15 18:00 |

### 🖥️ 浏览器刷新闭环

新增 Playwright：

```text
batch3 closeout persists inbox project status time and project evidence after refresh
```

在同一真实 SQL API 场景中验证：

1. 项目 Active → Blocked → Active → AwaitingAcceptance，每次刷新后保持。
2. 上传 PROJECT PDF 后刷新项目详情仍显示证据文件。
3. 事件分析后刷新仍为 AwaitingConfirmation，截止时间显示上海 2025/8/15 18:00。
4. 真人确认后刷新为 Confirmed，确认按钮消失。
5. 项目任务存在并可完成。
6. 项目完成后刷新仍为 Completed，进度 100%。

### 👔 项目经理流程审批

- 本批不重复改写 IAM 实现，但保留并回归导航/IAM 收尾已解决的 P0：
  `demo.pm` / `project-manager` 能审批分配给本人且满足冻结业务角色与部门范围的任务。
- 全量桌面 E2E `real project manager approves manager business-role node` 继续通过。
- 审批仍要求 `workflow-task:approve/return/terminate`、当前处理人匹配、
  任务创建时冻结的候选资格和任务部门范围同时成立，不授予任意代审权。

## 🗄️ 历史数据修护

- 主库最终修护前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH3_CLOSEOUT_PRE_MAIN_REPAIR_20261101_183512.bak`
- 备份 SHA256：
  `08AF9BE74009DD137ACBDBBB7AB963EF751066920329CE8E525A8A3BA0E368BE`
- `RESTORE VERIFYONLY`：通过。
- 隔离库：`AIDT_BATCH3_CLOSEOUT_FINAL_20261101_185000`。
- 修复前事件异常 24，修复 24，修护后 0。
  - 12 条 `Received + HighRisk` → `AwaitingConfirmation`。
  - 12 条 `Received + Confirmed` → `Confirmed`。
- 修复前项目状态审计异常 1，修复 1，修护后 0：
  `PRJ-3157C-20250808` `Active v2` → `Completed v3`，进度 100。
- 修复前 PROJECT 附件关系缺失 0，修复 0，修护后 0。
- 修护审计 1 条；脚本幂等重跑后仍为 1 条，重复项目附件关系为 0。
- 主库已执行并复测：事件异常 24→0、项目状态异常 1→0、附件关系缺失 0→0。
- 主库幂等复跑后修护审计仍为 1，重复项目附件关系为 0。
- 未从无审计依据的数据猜测状态，未修改或删除历史审计。

## 🧪 验证门禁

- 后端全量、SQL/API、Domain、Contract：115/115，0 失败、0 跳过。
- 后端构建：0 警告、0 错误；格式检查通过。
- 前端类型检查通过；Vitest 9 个文件、13/13。
- OpenAPI：114 路径、134 operationId。
- 生产构建通过；npm 生产依赖 0 漏洞；NuGet 无已知漏洞。
- 源码桌面 + 390px Playwright：18/18，0 失败、0 跳过、0 flaky。
- 隔离库迁移结构与历史修护：9 条迁移、57 张应用表；001–009 未修改，本批无新迁移。
- 正式部署桌面 + 390px Playwright：18/18，0 failure、0 skipped、0 flaky。
- 正式 E2E 开始时间 `2026-11-01T18:35:57.774+08:00`，晚于 final3 部署时间
  `2026-11-01T18:35:47.702+08:00`。

## 🚀 发布

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch3-final3.zip`
- 发布包 SHA256：`EAD2F5D6135EE5BFB525EEE46B2F57191C528F8FAE38EB1023517D5C86FAB4F6`
- sourceRevision：`d9ee050df2de473ca68433f1d8dd00b28d06c0a0`
- manifest：9 条迁移、`applied_verified`、`RuleSimulator`、外部系统仅 Mock/Disconnected；
  不包含 `-dirty`。
- 部署时间：`2026-11-01T18:35:47.702+08:00`
- 部署前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-batch3-final3_20261101_183542.bak`
- 部署前备份 SHA256：`6091CEEEF32DCAB51DB6F2A649E38C34AE70AB33A2F6686289A8AC56DF665C1B`
- 页面、API live/ready、Worker、AiGateway、capabilities：均 HTTP 200。
- 应用回滚：`final3 → req-navigation-iam-final2 → final3`，两次切换健康检查均通过。
- 最终部署：`req-batch3-final3`。
- 迁移探针库：`AIDT_REQ_BATCH3_CLOSEOUT_MIGRATION_PROBE_20261101_183922`；
  009 down/re-up 为 9/57 → 8/51 → 9/57，项目经理三项审批权限恢复为 3/3。
- 数据库恢复库：`AIDT_REQ_BATCH3_CLOSEOUT_FINAL3_RESTORE_20261101_183818`。
- 恢复库：ONLINE、9 条迁移、57 张应用表、事件/项目/文件关系异常均为 0，
  `PRJ-3157C-20250808` 为 `Completed v3 / 100%`。

## 🚫 非本批范围

- 正式 SSO/OIDC/MFA。
- 真实钉钉、ERP、U9、DCS 和真实外部模型。
- 生产高可用、公网部署。
- WBS、成本预算、资源工时。
- “已完成任务仍显示在我的待办”。
- 构建时开发依赖仍有 5 个 moderate 级 npm audit 提示；生产依赖
  `npm audit --omit=dev` 为 0，本批不升级测试/构建依赖。
