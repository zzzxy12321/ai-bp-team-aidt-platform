# 备份与恢复说明

## 📌 当前状态

已完成测试环境备份恢复演练。

- 全阶段修改前备份：`C:\ProgramData\AIDT\Backups\AIDT_FULLSTAGE_20261030_092845.bak`
- SHA256：`18C2637590B0E3CFDF6948BC6E26B9CE997B5FA50B9B8C058B1FC89F52EDC3AE`
- `RESTORE VERIFYONLY`：通过
- 临时库恢复：35 张应用表、13 个业务对象、59 条审计、51 条 Outbox
- 恢复临时库已删除，AIDT 主 Demo 库未受影响

## ✅ 阶段 B 最低检查

1. 发布脚本在每次部署前执行 `COPY_ONLY` 备份和恢复校验。
2. 备份放在 `C:\ProgramData\AIDT\Backups`，不进入代码仓库。
3. 回滚记录写入 `deployment-history.json`。
4. 生产环境仍需按甲方 RTO/RPO 做正式容量和恢复演练。

禁止把备份文件放在应用代码目录，禁止把真实生产备份提交进仓库。
