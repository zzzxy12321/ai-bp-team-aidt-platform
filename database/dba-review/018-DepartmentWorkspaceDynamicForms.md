# DBA Review: 018 DepartmentWorkspaceDynamicForms

## 📌 结论

018 有条件通过测试环境 DBA 审核，可在部署前备份与 `RESTORE VERIFYONLY` 通过后应用。
正式生产仍需甲方 DBA 在维护窗口复核，并确认 017 基线、备份恢复点和应用回滚窗口。

## 🗄️ 变更范围

- 不新增第二套流程、任务、身份或审计表。
- 扩展导航工作区、配置版本、流程定义、节点策略和任务实体。
- 新增必要索引、默认模板唯一约束和 JSON `ISJSON` 检查。
- 测试导航与配置使用测试标记和退役状态保留历史，不物理删除。
- 001–017 未修改。

## 📦 脚本

- Up：`database/migrations/018_DepartmentWorkspaceDynamicForms.up.sql`
- Down：`database/migrations/018_DepartmentWorkspaceDynamicForms.down.sql`
- Up SHA256：`D0CBCC1576E7639EB79851AE1E2417B992EA235D9E17B3262063CE65B262F776`
- Down SHA256：`78B168448433CB182B787EE3B6062F47264B9F204E33E35AA8289D0239B26F65`
- 执行时必须使用 SQLCMD `-I -b -f 65001`。

## 🧪 隔离验证

- 备份：`C:\ProgramData\AIDT\Backups\AIDT_DEPT_WF_001_PRE_8A_20261103_232808.bak`
- 备份 SHA256：`E301473F67F7D8F66CAAEA57D9DFF1DE60F706C07BB0CA36287E46BF3B12F45F`
- `RESTORE VERIFYONLY WITH CHECKSUM`：通过。
- 迁移库：`AIDT_DEPT_WF_001_MIGRATION_20261104_0100`
- up：17/71 → 18/71。
- down：18/71 → 17/71。
- re-up：17/71 → 18/71。
- up/down/re-up 后无无效表单 JSON。
- 7 个部门默认工作区；32 个测试导航退役；319 个测试配置标记；
  测试配置发布中为 0。

## ⚠️ 上线注意

- 018 down 会移除新增列与索引，已写入的任务表单数据不可保留在旧结构。
- 应用回滚可以独立于数据库结构，但必须保持 018 列存在；
  数据库结构回滚只在明确不需要新表单历史时执行。
- 导航和配置退役只改状态/测试标记，不删除审计、历史流程、任务或证据。
- 主库应用前必须重新备份并执行恢复校验。

## ✅ 正式应用结果

- 主库应用前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_DEPT_WF_001_PRE_018_20261104_0145.bak`
- SHA256：`0CE44730E6C5DDBEE529AEF1076E3658D85F3410504AF93E507744E69961391C`
- 主库 `AIDT`：18 迁移、71 张应用表。
- 最终部署 `req-ui-step8-final3` 正式 E2E 53/53。
- 最终备份与独立恢复库均验证为 18 迁移、71 表、7 个部门默认工作区、
  发布中测试默认入口 0。
