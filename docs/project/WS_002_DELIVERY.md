# WS-002 可配置岗位工作台模板交付报告

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`
>
> 分支：`feature/ui-step4-role-workbench-templates`
>
> 基线：`main@67fa2a8`
>
> 目标发布：`req-ui-step4-final1`
>
> 目标冻结：`req-ui-step4-frozen-v1`

## 🎯 交付结论

部门工作区已从固定通用布局升级为服务端解析的岗位工作台模板：

```text
当前用户 + 当前部门
 → 用户/角色/部门/组织优先级匹配
 → 仅返回已发布模板
 → 逐卡执行原业务权限
 → 渲染真实 Top 事项或明确空/未接入状态
 → 无匹配时回退通用部门工作区
```

## 🧱 实现范围

- 新增 `Workspace` Schema 和 4 张模板表。
- 迁移 012 注册研发、质量、供应链、财务、经理、项目经理 6 个系统模板。
- 首批 31 张卡片全部引用现有真实能力或明确标注尚未接入。
- 新增模板读取、新建、复制、草稿、校验、发布、停用、恢复、版本历史 API。
- 新增模板写权限和部门负责人/平台管理员双重范围校验。
- `DepartmentPage.vue` 改为模板驱动卡片网格，支持六类卡片状态。
- IAM 增加“工作台模板”页签，覆盖卡片、匹配范围、预览、发布和版本历史。

## 🗄️ 数据库

- 新迁移：`20261102060037_012_WS002RoleWorkbenchTemplates`。
- 新表：`WorkbenchTemplate`、`WorkbenchTemplateVersion`、`WorkbenchCard`、
  `WorkbenchAssignment`。
- 应用表：62 → 66。
- 迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_WS002_PRE_012_20261102_140411.bak`。
- 备份 SHA256：`B9A768B11015B62EA0A1AA1AABC03FA362A883AF7FE8C51C647C11AAEF409FB5`。
- `RESTORE VERIFYONLY`：通过。
- 隔离库：`AIDT_WS002_MIGRATION_PROBE_20261102_1405`。
- 隔离库 up/down/re-up：11 → 12 → 11 → 12 迁移；模板权限 0 → 3。

## 🔐 权限和审计

- 新增 `workbench-template-read/manage/approve`。
- 所有已启用角色获得读取权限；平台管理员、项目经理、质量、供应链和财务管理角色
  获得配置/发布权限，后端继续校验部门负责人资格。
- 普通员工只能读取匹配模板结果，不能拉取全部模板自行决定权限。
- 卡片逐项执行业务动作权限，测试确认无权限卡片不返回。
- 模板创建、复制、草稿、发布、停用、恢复写追加式审计。

## 🧪 源码验证

| 门禁 | 结果 |
|---|---|
| .NET 构建 | 0 警告、0 错误 |
| 后端/SQL/API | 138/138 |
| MCP | 11/11 |
| 后端合计 | 149/149，0 失败、0 跳过 |
| 前端类型检查 | 通过 |
| Vitest | 15 个文件、28/28 |
| OpenAPI | 136 路径、157 operationId |
| OpenAPI 静态校验 | 通过 |
| 源码岗位工作台 Playwright | 5/5，覆盖 1440、1280、390px |
| 源码全量 Playwright | 36 通过、1 个部署专用 MCP 用例按设计跳过、0 失败 |

## ⚠️ 源码阶段发现

- 首次执行隔离迁移时 SQL 别名不明确，已改为完全限定 `templateSeed/cardSeed/assignmentSeed` 字段。
- `dotnet ef` 设计工厂固定连接 `AIDT`，隔离升级必须显式传 `--connection`；已记录并从备份重新验证探针库。
- 首次全量源码 E2E 未设置 `AIDT_E2E_MCP_PORT=5085`，MCP 用例误连 8088；
  设置源码端口后在干净最终库上全量通过。
- 首次从 `31c189c` 构建的包实际包含 012，但 `migration-state.json` 尚未登记，
  导致 manifest 只列 11 条。该无标签包已保留为
  `AIDT-req-ui-step4-final1-premanifest.zip`，修正迁移登记后重新构建正式 `final1`。

## 🚦 待完成正式门禁

全部完成。

## 🚀 正式发布、回滚与恢复

- 功能提交：`5e8a040`；非快进合并：`31c189c`。
- 迁移元数据修正提交：`efc4732`。
- 正式发布：`req-ui-step4-final1`。
- 发布包 SHA256：
  `26F3E66B844709F8DFA152970E32F621019585CBA067532A229F72C483613435`。
- manifest sourceRevision：
  `efc473226faabcf90304d3018c6ff527ed4f8609`，不含 `-dirty`。
- manifest 迁移：12 条，最后一条为
  `20261102060037_012_WS002RoleWorkbenchTemplates`。
- 首次遗漏迁移登记的包保留为
  `AIDT-req-ui-step4-final1-premanifest.zip`，SHA256：
  `047DEFBBC18291894BF9548041B79F1141DEBFDCB158FF80F475BB65B09D61CB`。
- 最终部署时间：`2026-11-02T15:28:59.7140665+08:00`。
- 正式 E2E：37/37，0 失败、0 跳过、0 flaky；开始于
  `2026-11-02T15:29:06.497+08:00`，晚于最终部署。
- 应用回滚：
  `req-ui-step4-final1 → req-ui-step3-final1 → req-ui-step4-final1` 通过。
- 最终部署备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-step4-final1_20261102_152844.bak`。
- 备份 SHA256：
  `993163DFB93976D1E92CAB3E5AEC33BBF0DD1122846B22330A771756484409D0`；
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库：
  `AIDT_UI_STEP4_FINAL1_RESTORE_20261102_1536`，12 迁移、66 表、
  6 系统模板、31 卡片、3 模板权限。
- 页面、API live/ready、Worker、AiGateway、MCP 健康检查均为 HTTP 200。
