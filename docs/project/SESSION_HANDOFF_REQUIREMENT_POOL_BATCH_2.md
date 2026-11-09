# 需求池批次 2 会话交接

## 当前状态

`REQUIREMENT_POOL_BATCH_2_DEMO_COMPLETE`

此状态只覆盖 `MIN-001`、`WF-005`、`AGT-002`、`AIG-002`，整个 AIDT 需求池和生产系统未完成。

## 已完成

- 005 迁移和隔离库 up/down/re-up。
- 手动听记、规则行动项与人工确认任务。
- 节点策略、SLA、站内提醒和升级。
- 低风险 Agent 代理和永久人工边界。
- AgentRun-AiGateway-GatewayCall 闭环。
- 后端 70/70、前端 9/9、OpenAPI 53/62。
- 正式部署 E2E 6/6；应用回滚 `final2 → final1 → final2`。

## 恢复点

- 迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_PRE_005_20261031_094122.bak`
- 备份 SHA256：`4F67C0A6CFF8CEC733CD863D5A8CDABF48B16B42A3F668F087DA1AB75160EA8F`
- 隔离库：`AIDT_REQ_BATCH2_TEST_20261031_094122`
- 批次 1 应用回滚点：`D:\Apps\AIDT-Test\versions\req-batch1-final6`
- 最终发布：`D:\Apps\AIDT-Test\releases\AIDT-req-batch2-final2.zip`
- 发布 SHA256：`339AC0BBF400F378FA0E570960F06EF85B0C97DEA5B3EFA1267B066A4301888E`
- manifest sourceRevision：`875204ed5d7fbc7cce7054959eab8c4fead255b8`

## 已知限制

- 钉钉手动导入，真实接口和音频转写未完成。
- AiGateway RuleSimulator，真实模型未启用。
- 完整转办/委托/会签/或签和 PMO 模型不在本批。

## 下一步

1. 当前 `req-batch2-final2` 已完成发布、健康、回滚和 E2E 冻结。
2. 后续单独评审真实钉钉、模型和身份接入。
