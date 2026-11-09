# AIDT 需求池批次 1 交付报告

> 状态：`BATCH1_RELEASE_FROZEN_AND_BATCH2_BRANCH_READY`
>
> 日期：2026-10-31
>
> 范围：`UX-001`、`WS-001`、`DOC-001`、`DATA-001`
>
> 整个 AIDT 需求池与生产系统：未完成

## 🎯 一句话结论

四个需求已完成真实 SQL Server 持久化、部门与环境隔离、文件安全、桌面/390px E2E、
干净提交构建、部署、备份恢复和离线构建验证；`req-batch1-final6` 已冻结，本结论只覆盖需求池批次 1。

## ✅ 四个需求

| Feature ID | 结果 | 主要证据 |
|---|---|---|
| `UX-001` | 完成 | `/` 为我的工作；员工/管理导航分离；桌面和移动 E2E |
| `WS-001` | 完成 | Department、成员、主部门、department DataScope；跨部门任务流转 |
| `DOC-001` | 完成 | 上传、下载、预览、版本、SHA256、扫描/保密等级、通用关联和审计 |
| `DATA-001` | 完成 | 对象、流程、文件环境过滤；管理员显式跨环境；普通员工不可绕过 |

## 🗄️ 数据模型与迁移

- 003：`20261030172128_003_DepartmentWorkspaceFilesAndEnvironment`
- 004：`20261030174644_004_WorkflowTaskDepartmentBackfillCorrection`
- 主 `AIDT`：38 张应用表、45 外键、26 检查约束。
- 6 个默认部门、8 条成员关系、8 条 department DataScope、7 个部门/文件权限。
- 业务对象和流程实例保存业务归属部门；任务保存处理人主部门，支持跨部门流程。
- 003 在隔离库完成 up→down→re-up；004 完成 up 和历史任务校正。
- 主库 003/004 已应用，任务责任部门与处理人主部门不一致数为 0。

## 🔌 API 与页面

新增 API：

- `/api/v1/my-work`
- `/api/v1/departments`
- `/api/v1/departments/{id}/members`
- `/api/v1/departments/{id}/workspace`
- `/api/v1/files`
- `/api/v1/files/{id}`
- `/api/v1/files/{id}/versions`
- `/api/v1/files/{id}/download`
- `/api/v1/files/{id}/preview`
- `/api/v1/files/{id}/actions/delete-request`

新增页面：

- `/`：我的工作
- `/department`：部门工作区
- `/documents`：部门文件中心
- `/management`：管理概览

## 🔐 权限与审计

- 普通员工只能读取授权部门；跨部门直接访问返回 403 或受控 404。
- 平台管理员可跨部门和显式跨环境。
- 部门管理员维护成员和负责人，并同步 DataScope。
- 文件上传、下载、预览、新增版本、删除申请和拒绝访问写审计。
- 敏感/核心文件在未完成扫描和人工放行前不允许普通员工下载或预览。

## 🛡️ 文件安全

- 同时校验扩展名、MIME、文件签名、大小和 SHA256。
- 拒绝空文件、未知扩展名、类型伪装、MIME 不匹配和路径穿越。
- 物理文件名使用 GUID，数据库只存相对路径，前端不接收物理路径。
- PDF、图片、UTF-8 文本原生预览；Office 明确下载查看。
- 旧版本保留；删除为申请/软删除，不物理删除流程证据。
- 原始文件名不参与服务器物理路径。

## 🧪 测试证据

| 门禁 | 结果 |
|---|---|
| SQL/API 集成 | 64/64 通过，0 跳过 |
| 默认非 SQL 测试 | 48 通过、16 个 SQL 用例显式跳过 |
| 前端类型检查 | 通过 |
| 前端组件/状态测试 | 7/7 通过 |
| OpenAPI | 38 路径、46 operationId |
| Playwright | 5/5，桌面 + 390px，0 skipped/flaky |
| WF-001 回归 | 对象创建到 5 角色完成，通过 |
| 文件 E2E | 拖拽/点击上传、预览、版本、删除申请、敏感拒绝 |
| 迁移 | 003 up/down/re-up、004 up |
| 备份恢复 | 备份校验、临时库恢复 38 表/4 迁移 |
| 应用回滚 | `req-batch1-b → req-batch1-a`，健康 200 |
| 离线构建 | 233 NuGet 包；npm 缓存约 45.6 MB；构建通过 |
| NuGet/npm 漏洞 | 0 已知漏洞 |
| 后端格式 / Markdown / Git 空白 | 通过 |

## 🚀 部署与离线

- 测试应用：`D:\Apps\AIDT-Test\versions\req-batch1-final6`
- 浏览器入口：`http://127.0.0.1:8088`
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch1-final6.zip`
- 发布包 SHA256：`008711843CDED2B6F0D0814959FE9B36F432C73FF4277E69E93D5164CE8E4895`
- manifest sourceRevision：`7aa50459acc0f1ec66ba70caa16d82cc574415af`
- 注释标签：`req-batch1-final6`
- 迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH1_PRE_003_20261031_013442.bak`
- 迁移前备份 SHA256：`7C46A4C5BFAECB1FD0EBE76EB4D8D0380A526F02F6DAA6AA1A60C38F093CB784`
- 最终部署前备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-batch1-final6_20261031_092357.bak`
- 最终部署前备份 SHA256：`F73950AF218642E5F218DA1091452104437C3D9C2215A1CC86CFAB407146F40A`
- 应用回滚点：`D:\Apps\AIDT-Test\versions\req-batch1-final5`
- 离线目录：`C:\ProgramData\AIDT\Offline\20261031-015139`

## 🧾 版本治理

- 批次 1 提交：`e37c55d`、`d385c5e`、`4cb4738`、`ed210b7`
- `main` 非快进合并：`7aa50459acc0f1ec66ba70caa16d82cc574415af`
- 发布包 manifest 的 `sourceRevision` 不含 `-dirty`
- 最终部署健康检查：root、API、Worker、AiGateway、Nginx capabilities 全部 200
- 最终部署 Playwright：5/5 通过，0 failure、0 skipped、0 flaky
- 主库：38 张应用表、4 条 EF 迁移、6 个部门、任务主部门不一致 0
- 发布记录提交：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`
- 批次 2 分支：`feature/requirement-pool-batch-2`
- 批次 2 起始基线：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`
- 批次 2 只作为下一批次基线，不代表批次 2 功能已完成

## 🚧 明确未完成

- 正式生产 SSO/OIDC、MFA 和企业目录同步。
- 真实 U9、ERP、DCS、WMS、LIMS、钉钉。
- 真实病毒扫描引擎。
- 钉钉听记、SLA、转办、委托、数字员工代理和真实模型。
- 独立 PMO/Project 领域模型。
- 生产 DBA 签认、高可用、灾备和正式上线。
- 整个 AIDT 需求池未完成。

## 🔒 保密提醒

本轮只使用 Demo 数据，未使用真实配方、客户、成本、报价、工艺参数或生产接口数据。
