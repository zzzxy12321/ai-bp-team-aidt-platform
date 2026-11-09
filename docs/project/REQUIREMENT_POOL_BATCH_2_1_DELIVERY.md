# AIDT 需求池批次 2.1 交付报告

> 状态：`REQUIREMENT_POOL_BATCH_2_1_READY`
>
> 日期：2026-10-31
>
> 范围：`MIN-001`、`WF-005`、`AGT-002`、`AIG-002` 的可用性补完

## 🎯 结论

批次 2.1 已完成 DOCX 听记、策略管理页、默认低风险策略、SLA 预授权自动代理、
独立服务身份、幂等、通知、审计、正式 final3 部署、回滚和恢复验证。

## 📄 DOCX

- 支持 `.docx/.txt/.md/.json`。
- DOCX 使用内置 ZIP/XML 读取 `word/document.xml` 的正文段落。
- 不解析图片、批注、修订、宏或 OCR。
- 空、损坏、缺正文和伪装文件返回明确失败；不产生空听记。
- PDF 不解析，页面提示粘贴文字或转换为 DOCX。

## 🧩 节点策略

- 页面：`/workflow-policies`，仅平台管理员。
- 支持选择流程版本/节点、审批人、备份审批人、SLA、提醒、升级、附件数。
- 支持 `RemindOnly`、`HumanConfirmedProxy`、`PreAuthorizedAutoProxy`。
- 支持 AgentProfile、风险、金额、等级、永久人工边界。
- 支持保存草稿和发布；历史任务读取启动时冻结的策略。

## 🛡️ 默认策略

- 流程：`RND_MATERIALS_CONFIRM`
- 节点：`MATERIALS_CONFIRM`
- SLA：120 分钟
- 提醒：30 分钟
- 升级：60 分钟
- 自动代理：60 分钟
- 模式：`PreAuthorizedAutoProxy`
- 风险/等级/金额：Low / A2 / 0
- 永久人工边界：false

## 🤖 自动代理

Worker 依次执行提醒、升级和预授权检查。只有任务冻结策略为预授权、
任务仍 Pending、实例仍进行中、风险 Low、Agent 启用且满足等级和金额时才代理。
运行使用 AgentProfile 的服务身份，幂等键为 `sla-auto-proxy:{taskId}`。
原责任人收到通知，审计包含 `DecisionMode=AgentProxy`、`Trigger=SlaTimeout`、
`AgentRunId`、`NodePolicyId` 和 `OriginalAssignee`。

## 🗄️ 迁移

- EF：`20261031044811_006_Batch2_1DocxPolicyAutoProxy`
- 升级/回滚 SQL 和 DBA 审核已加入 `database/`。
- 隔离库：`AIDT_REQ_BATCH2_1_TEST_20261031_121903`
- 序列：006 up → down → re-up
- 结果：6 条迁移、44 张应用表

## 🧪 门禁

- 后端构建：0 警告、0 错误。
- 默认后端：55 通过、21 个 SQL 用例按预期跳过。
- 隔离 SQL/API：76/76。
- 前端：类型检查通过，9/9 测试，OpenAPI 56 路径/65 operationId，构建通过。
- NuGet/npm：无已知漏洞。
- 正式部署 E2E：8/8，0 failure、0 skipped、0 flaky。

## 🚀 发布

- 版本：`req-batch2-final3`
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch2-final3.zip`
- SHA256：`31524319F5BCD543CA6343C62C50C012DAF9C7EA76FB6E5A765140DA09B3B188`
- manifest sourceRevision：`5382fe7738eedb6a6cf80cac74d7371a042fdd4e`
- 部署目录：`D:\Apps\AIDT-Test\versions\req-batch2-final3`
- 上一版本/回滚点：`req-batch2-final2`
- 应用回滚：`final3 → final2 → final3`，健康恢复 200
- 部署前备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-batch2-final3_20261031_130335.bak`
- 备份 SHA256：`697200B81CBAC8266C7DEF96BD1C361B0FC8A8EACCC65B0CC342B0A10B10000E`
- 恢复库：`AIDT_REQ_BATCH2_1_RESTORE_20261031_130335`
- 恢复验证：6 条迁移、44 张应用表、默认策略存在
- 注释标签：`req-batch2-final3`

## 🚧 未完成

- 真实钉钉、音频转写、真实外部模型、正式 SSO。
- 完整转办、委托、会签、或签和 PMO 领域模型。
- 生产 DBA、高可用、灾备与正式上线。
