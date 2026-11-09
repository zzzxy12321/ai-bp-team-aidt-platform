# 009 DBA 审核记录

> 迁移：`20261031195740_009_FrontendNavigationIamWorkflowRoles`
>
> 审核状态：`READY_FOR_MAIN_APPLICATION`

## 📌 变更摘要

- Identity：部门树字段、用户邀请状态、角色分类、范围化角色分配、高风险授权申请。
- Navigation：4 张新表与系统导航种子。
- Workflow：节点处理人策略字段、任务处理人快照和转交证据。
- 权限：IAM、导航、数据范围、流程角色和转交动作。
- P0 修复：`project-manager` 增加 `workflow-task:approve/return/terminate`。

## 🗄️ 表与数据量

- 新增应用表：6。
- 应用表总数：51 → 57。
- 历史数据以新增列默认值和显式回填兼容。
- 不删除、不改写历史对象、流程、任务、文件、项目或审计业务记录。

## ⚠️ 锁与执行

- 所有 DDL 为新增列、索引、约束和新表，不重建既有聚簇结构。
- 迁移前执行 `COPY_ONLY, CHECKSUM` 全量备份并 `RESTORE VERIFYONLY`。
- 脚本设置必要 ANSI/QUOTED_IDENTIFIER 选项。
- `GO` 分批用于保证新增列可见性并避免 SQL Server 同批次元数据问题。
- 种子 SQL 变量按批次使用唯一名称。

## 🧪 隔离验证

- 主库备份：`C:\ProgramData\AIDT\Backups\AIDT_FRONTEND_NAV_009_PRE_20261101_040523.bak`
- 备份 SHA256：`112246EBB6F19AE5695B0F0BC694C1A92B54D8A312710F6F2FD7CFE125FDF235`
- Up/down/re-up：通过。
- 最终隔离库：`AIDT_FRONTEND_NAV_FINAL_20261101_041947`。
- SQL/API 全量：104/104，0 失败、0 跳过。

## ✅ 主库应用前置

- 主库再次备份并校验。
- 备份发布二进制和回滚目录。
- 停止旧 API/Worker/AiGateway/Nginx。
- 通过 SQLCMD 执行正式 009 up 脚本。
- 校验 9 条迁移、57 张应用表和关键种子。
- 启动新版本并执行健康、API、桌面和 390px E2E。

## ↩️ 回滚

1. 优先恢复迁移前备份到恢复库并验证。
2. 应用回滚到 `req-config-center-final3`。
3. 在恢复窗口按 009 down 逆序移除本批结构。
4. 不移动冻结标签 `req-config-center-frozen-v1`。
