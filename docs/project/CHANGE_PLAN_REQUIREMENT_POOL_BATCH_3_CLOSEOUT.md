# AIDT 需求池批次 3 收尾变更计划

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`
>
> 分支：`fix/requirement-pool-batch-3-closeout`
>
> 基线：`main@93c4cd41129cf92033c4e0a109bee967a21a5552`
>
> 历史标签：`req-batch3-final2`
>
> 目标发布：`req-batch3-final3`
>
> 目标冻结标签：`req-batch3-closeout-frozen-v1`

## 🎯 目标

关闭 `PMO-001`、`PMO-002`、`EVT-001`、`TASK-002` 的四个已确认持久化与证据缺口，使批次 3 达到可用于公司内部受控试点的状态：

```text
事件分析/确认状态可刷新
项目状态与版本可刷新
项目文件自动形成证据关系
截止日期固定为 Asia/Shanghai 18:00
历史不一致数据可解释、可审计、可幂等修复
```

## ✅ 基线事实

- `main` 在创建分支前干净，HEAD 为 `93c4cd4`。
- 当前部署为 `req-navigation-iam-final2`。
- `req-batch3-final2` 是历史冻结标签，不得移动。
- `req-navigation-iam-closeout-frozen-v1` 不得移动。
- 主库当前 9 条 EF 迁移、57 张应用表。
- 当前历史异常：
  - 12 条事件为 `Received + Confirmed`。
  - 12 条事件为 `Received + HighRisk`。
  - 1 个项目存在成功的 `project.status_changed: Active → Completed` 审计，但主对象仍为 `Active`。
  - 当前未发现 PROJECT 附件缺少 ProjectRelation 的记录，但需修复未来上传链并保留幂等补建能力。

## 🧩 上下文包

### 事件

- `SqlEventInboxService.ApplyEventScope` 强制 `AsNoTracking`。
- `AnalyzeAsync` 与 `ConfirmAsync` 通过 `LoadAccessibleEventAsync` 修改事件，但修改对象未被 EF 跟踪。
- `EventSuggestion` 通过跟踪查询修改，因此出现 suggestion 已确认、inbox 仍 Received。

### 项目状态

- `ChangeStatusAsync` 通过 `LoadManagedProjectAsync → LoadProjectEntityAsync → AsNoTracking` 取得项目。
- 修改 `Status/Version` 后调用 `SaveChanges` 不会更新主对象。
- 审计与 profile 可能先行写入，形成成功审计但状态未变。

### 文件证据

- `SqlFileCenterService.UploadAsync` 写入 Attachment、DocumentVersion、AttachmentLink 和审计。
- 对 PROJECT 对象只写 `BusinessObject` AttachmentLink，没有写 `PMO.ProjectRelation`。
- `ProjectDetail` 主要依赖 ProjectRelation 和项目/任务 AttachmentLink。
- `UX_ProjectRelation_Project_Entity_Code` 已有数据库唯一索引，可用于并发幂等保护。

### 日期

- `ProjectRules.TryExtractDeadline` 使用 `now.Offset`。
- Worker UTC 上下文中结果为 18:00Z，上海页面显示次日 02:00。
- 当前业务时区应集中配置为 `Asia/Shanghai`。

## 🛠️ 实施设计

### 1. 事件状态

- `ApplyEventScope` 支持可选 tracking；列表和 GET 保持 `AsNoTracking`。
- Analyze/Confirm 使用 `.AsTracking()` 读取事件。
- 事件与 suggestion 在同一事务中保存。
- 幂等回复也必须重新读取持久化状态。
- 前端继续只在 `Confirmed` 时隐藏确认操作。

### 2. 项目状态

- 为管理动作增加受跟踪项目实体读取。
- `UpdateAsync` 和 `ChangeStatusAsync` 修改真实跟踪的 `BusinessObjectEntity`。
- 继续使用 ExpectedVersion 乐观并发。
- 完成前继续检查 Pending/InProgress 项目任务。
- Completed 时 profile 进度固定为 100。

### 3. 项目证据

- 文件上传关联 PROJECT 业务对象时，在同一事务内创建 `ProjectRelation(Attachment, SupportingEvidence)`。
- 使用现有唯一索引和本地/数据库查重，重复上传或重试不产生重复关系。
- 非 PROJECT 对象保持原 AttachmentLink 行为。
- 项目详情读取仍使用 ProjectRelation + AttachmentLink，不新增第三套关系。

### 4. 业务时区

- 新增集中配置 `AIDT:BusinessTime`：
  - `TimeZoneId=Asia/Shanghai`
  - `DeadlineHour=18`
- 领域规则接收 `TimeZoneInfo` 和截止小时，不依赖服务器默认时区。
- 事件建议、确认和项目任务复用同一 deadline。
- API/页面通过 UTC 存储并正确还原上海本地时间。

### 5. 历史数据修护

- 新增一次性、幂等、可审计的受控 SQL 修护脚本，不修改审计历史。
- 事件状态从 suggestion 安全推导：
  - Confirmed → Confirmed
  - Rejected → Rejected
  - Suggested/HighRisk → AwaitingConfirmation
- 项目状态仅在最新成功状态审计的 BeforeJson 等于当前状态、AfterJson 为合法项目状态时回填。
- PROJECT 附件根据现有 BusinessObjectId 幂等补建 SupportingEvidence 关系。
- 脚本输出前值、修护数量和未自动处理数量。
- 无数据库结构变更，因此不新增 EF 迁移 010，不修改 001–009。

## ✅ 已完成验证

- 后端构建：0 警告、0 错误。
- 隔离库 `AIDT_BATCH3_CLOSEOUT_FINAL_20261101_185000`：115/115，0 失败、0 跳过。
- 前端：类型检查、9 个文件 13/13、OpenAPI 114 路径/134 operationId、生产构建通过。
- 依赖：NuGet 无已知漏洞；`npm audit --omit=dev` 为 0。
- 源码桌面 + 390px Playwright：18/18，0 失败、0 跳过、0 flaky。
- 历史修护在隔离库完成：24 条事件、1 个项目状态，异常均归零；附件关系缺失保持 0。
- 已完成：提交与合并、主库修护、final3 干净构建、正式部署、正式 E2E 18/18、
  应用回滚与数据库恢复、冻结标签。

## 📦 影响范围

| 范围 | 变更 | 验证 |
|---|---|---|
| EventInbox | 跟踪读取、事务持久化、重新读取 | SQL/API/DOM/E2E |
| Project | 跟踪更新、状态/版本/进度一致 | API 状态链测试 |
| Document/PMO | PROJECT 上传自动关系 | 上传后重新 GET、SQL 唯一性 |
| Domain Time | Asia/Shanghai 18:00 精确口径 | Domain/API/页面 |
| Data repair | 24 条事件、1 个项目、附件关系 | 隔离库前后计数 |
| Frontend | 确认按钮、状态刷新、日期显示 | 组件/E2E/390px |

## 🧪 测试计划

- 先增加失败测试，证明状态刷新未持久化。
- 事件：分析后 GET、确认后 GET、重新列表、独立 SQL 上下文、单事件单任务。
- 项目：Active→Blocked→Active、Active→AwaitingAcceptance→Completed、版本递增、未完成任务阻断、进度 100。
- 文件：PROJECT 上传后 GET 项目 Files、ProjectRelation 存在、重试不重复、非项目对象行为不变。
- 日期：2025-08-15 18:00 +08:00、UTC 10:00Z、API 上海还原仍为 15 日。
- 前端：确认按钮刷新消失、项目状态刷新保持、文件自动出现、日期显示。
- 回归：原 112 项后端、17 项 E2E、类型、Vitest、OpenAPI、生产构建、迁移和部署回滚。

## 🔐 安全与边界

- 不接真实钉钉、U9、ERP、DCS，不启用真实外部模型。
- 本地演示 Cookie 不是生产 SSO。
- 不直接猜测项目历史状态；仅使用审计明确证据。
- 不删除或改写审计。
- 不修改迁移 001–009。
- 不处理本批排除的 SSO、外部系统、WBS、成本、工时和已完成任务待办过滤。

## 🚀 发布与回滚

1. 先备份主库并验证。
2. 在隔离库运行测试和历史修护脚本。
3. 提交、非快进合并、从干净提交构建 `req-batch3-final3`。
4. 正式部署前再次备份和 `RESTORE VERIFYONLY`。
5. 部署 final3，运行全部正式 E2E。
6. 回滚到 `req-navigation-iam-final2` 再恢复 final3。
7. 恢复部署备份到独立库并核对状态与关系。
8. 创建 `req-batch3-closeout-frozen-v1`。
