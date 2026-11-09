# AIDT 需求池批次 2.1 变更计划

> 状态：`REQUIREMENT_POOL_BATCH_2_1_READY`
>
> 起始基线：`a6d270f4f78fefa2f2b47b029d2a37d8e9a310ae`
>
> 范围：批次 2 可用性补完，不进入批次 3

## 🎯 本轮目标

把批次 2 从“接口和测试环境可用”补强为默认 Demo 环境可直接演示：

- DOCX 听记直接解析。
- 节点策略页面可新增、编辑、保存和发布。
- 默认 Demo 预置低风险策略。
- SLA 超时后可按预授权规则自动代理。
- 正式部署新增批次 2.1 E2E。

## 📄 DOCX 解析方案

- 使用 .NET 内置 `System.IO.Compression.ZipArchive` 与 `System.Xml.Linq`。
- 读取 `word/document.xml`，仅提取段落 `<w:p>` 内 `<w:t>` 文本。
- 不解析图片、批注、修订、宏或 OCR。
- 不新增 NuGet 包，因此不需要新增依赖 ADR。
- 原 DOCX 仍通过 `IFileCenterService` 保存、计算 MIME/签名/SHA256/版本。
- 空文档、损坏 ZIP、缺 `word/document.xml` 或超限返回稳定错误码。
- PDF 仍只保存原文件，听记页面明确提示粘贴或上传 DOCX/TXT/MD/JSON。

## 🗄️ 数据变化

- `Workflow.WorkflowNodePolicy.AgentProxyMode`
  - `RemindOnly`
  - `HumanConfirmedProxy`
  - `PreAuthorizedAutoProxy`
- `Workflow.WorkflowNodePolicy.AutoProxyAfterMinutes`
- `Agent.AgentRun.ProxyTriggerMode`：`Manual` 或 `SlaTimeout`
- `Agent.AgentRun.ProxyTriggeredAt`
- `Agent.AgentRun.OriginalAssigneeId`
- `Agent.AgentRun.CompensationStatus`
- `Agent.AgentRun.ProxyIdempotencyKey`，唯一过滤索引防止自动代理重复。
- 迁移 006 新增字段、默认策略和专用低风险流程。

## 🔁 自动代理状态机

1. 任务到期且仍为 `Pending`。
2. 先写提醒通知和审计。
3. 到升级时间后写升级通知和审计。
4. 读取任务冻结的 `NodePolicyId`，不读取最新策略。
5. 仅 `PreAuthorizedAutoProxy` 继续。
6. 拒绝永久人工边界、Medium/High 风险、错误等级/金额、停用 Agent 或已结束实例。
7. 用 `sla-auto-proxy:{taskId}` 唯一键创建 AgentRun。
8. Agent 使用独立服务身份完成审批，任务标记 `DecisionMode=AgentProxy`。
9. 通知原责任人，写 `Trigger=SlaTimeout` 审计。
10. 重复或并发扫描只允许一个 AgentRun 和一个任务动作。

## 🛡️ 永久人工边界

- 财务付款。
- 质量放行。
- 配方变更。
- DCS/PLC 控制。
- 安全联锁。
- 高风险 EHS。
- 外部合同承诺。
- 客户重大赔付。
- 无法补偿或撤销的节点。

默认自动代理示例使用专用 `MATERIALS_CONFIRM`（研发资料完整性确认）节点，
不借用质量、付款、配方或设备节点。

## 🔌 API 与页面

- `/api/v1/minutes/upload` 新增 `.docx` 解析。
- `/api/v1/workflow-policy/users`：管理员获取审批人候选。
- `/api/v1/workflow-versions/{id}/nodes`：返回流程版本节点。
- 现有策略保存接口支持草稿/发布和三种代理模式。
- 新增策略管理页 `/workflow-policies`，仅平台管理员可进入。
- 页面支持选择版本/节点、审批人、SLA、提醒、升级、模式、Agent 限额、人工边界及保存/发布。
- 普通员工只读取任务 SLA 与代理结果，不显示编辑入口，后端仍强制 403。

## 🧪 测试矩阵

- TXT/MD/JSON 回归。
- DOCX 正常、多段落、空文档、损坏文档、超限和非 DOCX 伪装。
- 策略草稿、发布、版本号、版本冻结和普通员工 403。
- 默认策略幂等 seed。
- 120 SLA / 30 提醒 / 60 升级 / 60 自动代理。
- 重复与并发 SLA 扫描。
- 自动代理成功一次、独立 Agent 身份、原责任人通知和审计。
- 永久人工边界、错误风险/等级/金额/停用 Agent 拒绝并升级。
- 人工确认代理回归。
- 部门/组织/环境越权。
- 006 up/down/re-up。
- 正式部署策略配置 + 自动代理专用 E2E。

## 🧾 审计

- `workflow.node_policy_draft_saved`
- `workflow.node_policy_published`
- `task.sla_reminded`
- `task.sla_escalated`
- `task.agent_proxy_auto_started`
- `task.agent_proxied`
- `task.agent_proxy_failed`

审计字段必须包含 `DecisionMode=AgentProxy`、`Trigger=SlaTimeout`、
`AgentRunId`、`NodePolicyId` 和 `OriginalAssignee`。

## ↩️ 回滚

- 代码回滚到 `a6d270f` / `req-batch2-final2`。
- 数据库恢复 2.1 迁移前备份。
- 隔离库验证 006 down 和 re-up。
- 不覆盖标签 `req-batch1-final6`、`req-batch2-final1`、`req-batch2-final2` 及发布包。

## 🚫 明确不做

- PDF 正文解析。
- 音频转写、真实钉钉。
- 真实模型和正式 SSO。
- 会签、或签、完整转办/委托/加签。
- 批次 3 或 PMO/WBS 新领域。
