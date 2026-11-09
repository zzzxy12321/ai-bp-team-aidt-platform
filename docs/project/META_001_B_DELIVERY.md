# META-001-B 业务对象建模与动态表单前端交付

## 🎯 结论

管理员可完全通过网页创建、设计、预览、校验、发布、停用和恢复对象类型；
普通业务用户只看到发布表单，并按九类字段创建和编辑对象。对象版本、项目、流程、
文件和审计关联均由 SQL Server 持久化。

## 🖥️ 页面

- `/modeling` 业务建模工作室
- `/objects` 动态业务对象工作区
- `/configurations` 已发布对象类型选择
- `/iam` 岗位工作台业务对象卡片类型范围

## 🧩 字段与验证

- Text、LongText、Number、Date、DateTime、Boolean、Select、User、Department。
- Code/Key、显示名称、必填、长度、默认值、敏感等级、选项、列表显示、筛选和排序。
- 本地结构检查用于即时反馈；服务端为最终校验与持久化真源。
- User/Department 使用搜索选择器，不要求手输 UUID。

## 🔄 版本与生命周期

- 已发布版本不可覆盖；网页保存生成新草稿版本。
- 草稿与停用类型不进入普通用户表单、流程配置或岗位卡片类型选择。
- 业务对象编辑递增版本并写 BusinessObjectVersion、审计和 Outbox。
- 历史对象和字段快照不改写；删除字段保留历史值。

## 🔐 权限

- 建模仅平台管理员：`object-model:read/create/update/manage/approve`。
- 014 向六个既有业务角色授予 `business-object:update`，仍受组织和部门范围限制。
- Core 字段在列表、详情、编辑和历史版本中持续脱敏。

## 🧪 源码结果

- 后端/SQL/API 163/163；MCP 11/11；合计 174/174。
- Vitest 18 文件、35/35。
- OpenAPI 146 路径、167 operationId。
- 源码 E2E 40 通过、1 个部署专用 MCP OAuth 跳过、0 失败。

## 🚦 发布门禁

- [x] 提交功能分支并非快进合并 `main`
- [x] 干净构建 `req-ui-step5-final1`
- [x] 部署与正式 E2E
- [x] 应用回滚
- [x] 最终备份恢复
- [x] 创建 `req-ui-step5-frozen-v1`

## 📦 正式发布

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step5-final1.zip`
- SHA256：`384291D1F053BCDAD5B831F82A3EEC5C03F019E9C55E0F8BFE40D29660721941`
- 发布源码：`9875ad2abc71dacf6caab13268c4b0b806ce9b3b`
- 最终部署：`2026-11-03T00:30:41.2525647+08:00`
- 正式 E2E：41/41，0 失败、0 跳过、0 flaky；开始时间
  `2026-11-03T00:30:44.488+08:00`
- 应用回滚：`final1 → req-ui-step5a-final1 → final1` 通过。
- 部署前备份 SHA256：
  `47D8DAFD1866FB5451A0A0F7142E25D48B007B1974BD8934C080C0F4517D18F0`
- 最终备份 SHA256：
  `0D3762398F19C6FBBAA940692EC42FA729FDA15AC56C500350E91B8D1FCC2114`
- 独立恢复库：`AIDT_META_001_B_FINAL1_RESTORE_20261103_003623`，
  ONLINE、14 迁移、67 表、6 个业务角色更新授权、4 个动态模型版本。
