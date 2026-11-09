# 需求池批次 3 会话交接

## 当前状态

`REQUIREMENT_POOL_BATCH_3_DEMO_COMPLETE`

批次 3 是最后一个业务 Demo 批次，不是整个 AIDT 生产项目完成。

## 已完成

- `PMO-001`、`PMO-002`、`EVT-001`、`TASK-002`。
- 迁移 007、隔离库 up/down/re-up、主库升级。
- 正式部署、11/11 E2E、应用回滚和数据库恢复。

## 恢复点

- 发布源码：`9b9b7fe10819e8201011909028204d533625f627`
- 标签：`req-batch3-final2`
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch3-final2.zip`
- 发布包 SHA256：`C4875993E77D183EBA8EEE1F2D857AB35A11C815C2B00BE5DED85D5C9DB7E435`
- 当前部署：`req-batch3-final2`
- 上一稳定版本：`req-batch2-final3`
- 部署前备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-batch3-final2_20261101_013031.bak`
- 备份 SHA256：`721CDFD7F290A6E47A9AB8CDB617E93B40C5EC36AA106E255FCB1B32E1CB1B64`
- 恢复库：`AIDT_REQ_BATCH3_FINAL_RESTORE_20261101_013149`

## 重要事故

`req-batch3-final1` 正式部署后管理员登录 502，根因是 Cookie claims 超限。
final2 使用压缩声明并兼容旧 Cookie；final1 保留但未打标签。

## 下一步

真实身份、外部系统 Sandbox、真实模型治理、生产 DBA、高可用和灾备需独立实施。
