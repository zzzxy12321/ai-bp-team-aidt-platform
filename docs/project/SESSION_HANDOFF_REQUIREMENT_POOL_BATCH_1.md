# 需求池批次 1 会话交接

## 当前状态

`BATCH1_RELEASE_FROZEN_AND_BATCH2_BRANCH_READY`

只表示 `UX-001`、`WS-001`、`DOC-001`、`DATA-001` 完成；整个 AIDT 需求池和生产系统未完成。

## 已完成

- 部门、成员、主部门、负责人和 department DataScope。
- 员工“我的工作”、部门工作区、文件中心和管理入口分层。
- 文件上传、下载、预览、版本、SHA256、扫描/保密等级、通用关联和审计。
- 对象、流程、任务、文件环境隔离；管理员跨环境开关。
- 迁移 003、004；主库 38 表。
- SQL/API 64/64、前端 7/7、Playwright 5/5。
- 部署回滚、备份恢复和离线构建。
- 批次 1 已提交并以 `--no-ff` 合并到 `main`。
- 干净 `main` 发布 `req-batch1-final6` 并创建注释标签。

## 关键恢复点

- 迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH1_PRE_003_20261031_013442.bak`
- SHA256：`7C46A4C5BFAECB1FD0EBE76EB4D8D0380A526F02F6DAA6AA1A60C38F093CB784`
- 隔离测试库：`AIDT_REQ_BATCH1_TEST_20261031_013442`
- 最终发布：`D:\Apps\AIDT-Test\releases\AIDT-req-batch1-final6.zip`
- 发布 SHA256：`008711843CDED2B6F0D0814959FE9B36F432C73FF4277E69E93D5164CE8E4895`
- manifest sourceRevision：`7aa50459acc0f1ec66ba70caa16d82cc574415af`
- 标签：`req-batch1-final6`
- 部署前备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-batch1-final6_20261031_092357.bak`
- 应用回滚点：`D:\Apps\AIDT-Test\versions\req-batch1-final5`
- 部署状态：`D:\Apps\AIDT-Test\deployment-state.json`
- 发布记录提交：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`
- 批次 2 分支：`feature/requirement-pool-batch-2`
- 批次 2 起始基线：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`

## 已知限制

- 正式 SSO/OIDC、MFA、企业目录同步未接入。
- 真实病毒扫描引擎未配置。
- U9/ERP/DCS/WMS/LIMS/钉钉仍为 Mock 或未连接。
- 真实模型、听记、SLA、转办/委托和数字员工代理不在本批。
- 生产 DBA、高可用、灾备和正式上线未完成。

## 下一步建议

1. 由甲方确认部门和岗位主数据责任。
2. 接入正式身份源和文件病毒扫描服务。
3. 在 `feature/requirement-pool-batch-2` 上单独设计和实施下一批需求，不得把分支已创建当作功能完成。
