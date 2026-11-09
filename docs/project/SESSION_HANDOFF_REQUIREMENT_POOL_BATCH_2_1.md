# 需求池批次 2.1 会话交接

## 当前状态

`REQUIREMENT_POOL_BATCH_2_1_READY`

代码、迁移、隔离测试、final3 部署、E2E、回滚和数据库恢复均已完成。

## 已完成

- DOCX 正文解析和测试。
- 策略管理页面及保存/发布权限。
- 默认 `MATERIALS_CONFIRM` 策略。
- SLA 预授权自动代理、独立服务身份、幂等、通知和审计。
- 006 迁移、up/down/re-up 和 DBA 审核。
- OpenAPI、功能地图、权限矩阵和测试证据更新。
- final3 正式部署 Playwright 8/8。
- 应用回滚 `final3 → final2 → final3`。
- 数据库恢复库验证 6 迁移、44 应用表。

## 恢复点

- 分支：`fix/requirement-pool-batch-2-1`
- 基线：`a6d270f4f78fefa2f2b47b029d2a37d8e9a310ae`
- 迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_1_PRE_006_20261031_121903.bak`
- 备份 SHA256：`E5F17A4E50B9FF845704A4A3198E9A1ED28A5BF7A81458EAA65563CDCB5470F2`
- 隔离库：`AIDT_REQ_BATCH2_1_TEST_20261031_121903`
- 主库：`AIDT`，已应用 006；迁移为增量且未删除历史数据
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch2-final3.zip`
- 发布包 SHA256：`31524319F5BCD543CA6343C62C50C012DAF9C7EA76FB6E5A765140DA09B3B188`
- manifest sourceRevision：`5382fe7738eedb6a6cf80cac74d7371a042fdd4e`
- 标签：`req-batch2-final3`
- 回滚点：`req-batch2-final2`
- 部署前备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-batch2-final3_20261031_130335.bak`
- 备份 SHA256：`697200B81CBAC8266C7DEF96BD1C361B0FC8A8EACCC65B0CC342B0A10B10000E`
- 恢复库：`AIDT_REQ_BATCH2_1_RESTORE_20261031_130335`

## 下一步

1. 后续批次需单独评审，不把本状态解释为生产上线。
2. 真实钉钉、音频转写、外部模型和正式 SSO 仍需独立实施与安全评审。
3. 不推送远程，除非用户明确授权。
