# 需求池批次 1 变更计划

> 状态：`REQUIREMENT_POOL_BATCH_1_COMPLETE`
>
> 日期：2026-10-31
>
> 基线提交：`8aff291 chore(baseline): freeze full-stage demo before requirement pool batch 1`
>
> 分支：`feature/requirement-pool-batch-1`

## 🎯 范围

仅实现：

| 正式 Feature ID | 需求池来源 | 功能 |
|---|---|---|
| `UX-001` | `IDEA-UX-001` | 员工优先的“我的工作”入口 |
| `WS-001` | `IDEA-WS-001` | 部门工作区 |
| `DOC-001` | `IDEA-DOC-001` | 部门文件与附件中心 |
| `DATA-001` | `IDEA-DATA-001` | 演示/测试/生产环境数据隔离 |

不实现钉钉听记、真实模型、SLA、转办、委托、Agent 代理、PMO 项目模型、生产 SSO 或真实外部业务系统。

## 🔎 当前资产

- `BusinessObject`、`WorkflowInstance`、`WorkflowTask` 已有 SQL 持久化，但只有组织范围。
- `Document.Attachment`、`Document.DocumentVersion` 存在，但没有上传 API、通用关联和版本元数据。
- 前端根路径仍直接进入管理型 Dashboard。
- 当前用户 DataScope 只有 `organization`。
- 当前 Demo 数据无统一环境标识。

## 🗃️ 数据库变更

新增迁移 `003_DepartmentWorkspaceFilesAndEnvironment`：

- `Identity.Department`
- `Identity.UserDepartmentMembership`
- `BusinessObject.BusinessObject`：`DepartmentId`、`EnvironmentCode`、`IsTestData`、`SourceSystem`
- `Workflow.WorkflowInstance`：`DepartmentId`、`EnvironmentCode`、`IsTestData`、`SourceSystem`、`StartedByUserId`
- `Workflow.WorkflowTask`：`DepartmentId`、`EnvironmentCode`、`IsTestData`；用于服务端任务隔离
- `Document.Attachment`：`DepartmentId`、环境字段、状态、删除申请字段
- `Document.DocumentVersion`：原文件名、MIME、大小、版本扫描和保密等级
- `Document.AttachmentLink`：`AttachmentId + EntityType + EntityId` 通用关联
- 现有 Demo 数据回填 `demo`、`IsTestData=1`、`AIDT-SEED`
- 部门成员关系和 DataScope 通过幂等种子写入

回滚：

- 迁移 down 删除新增表、列、索引和约束。
- 数据回填通过 SQL 备份恢复；代码分支可撤回。

## 🔌 API 变更

### UX / Department

- `GET /api/v1/my-work`
- `GET /api/v1/departments`
- `GET /api/v1/departments/{id}/workspace`

### File Center

- `GET /api/v1/files`
- `POST /api/v1/files`，multipart 上传
- `GET /api/v1/files/{id}`
- `GET /api/v1/files/{id}/versions`
- `POST /api/v1/files/{id}/versions`
- `GET /api/v1/files/{id}/download`
- `GET /api/v1/files/{id}/preview`
- `POST /api/v1/files/{id}/actions/delete-request`

所有业务查询由后端应用部门、组织、环境和企业数据范围过滤。

## 🌐 前端页面

- `/`：我的工作
- `/department`：部门工作区
- `/documents`：文件中心
- `/management`：管理概览，仅管理权限可见
- `/workflows`、`/objects`：专业业务视图
- `/audit`、`/agents`：管理区域入口

员工端卡片隐藏 UUID、版本号、节点代码和 TraceId；专业详情页仍保留技术引用。

## 🔐 权限与审计

- 普通员工默认仅看授权部门数据。
- 平台管理员可跨部门。
- 无部门范围返回 `DEPARTMENT_SCOPE_DENIED`。
- 上传、下载、预览、版本新增、删除申请和拒绝访问写审计。
- 文件物理路径不返回前端，只返回文件 ID。

## 🔒 文件安全

- 白名单扩展名：PDF、DOCX、XLSX、PPTX、PNG、JPG/JPEG、TXT、CSV。
- 同时校验扩展名、MIME、文件签名。
- 物理文件名使用 GUID；原文件名只存元数据。
- 默认上限 50 MiB，可通过配置覆盖。
- 病毒扫描未配置时：
  - Internal：`NotConfigured`
  - Sensitive/Core：`PendingManualReview`，仅平台管理员可下载。

## 🌍 环境隔离

- `EnvironmentCode`: `demo` / `test` / `prod`
- `IsTestData`
- `SourceSystem`
- 工作台和列表默认使用当前部署环境。
- 平台管理员可显式切换包含测试数据。
- UI 始终显示环境标签。

## 🧪 测试

- Domain：文件状态、环境代码、部门规则。
- Application：上传、版本、权限、软删除申请。
- SQL：唯一约束、部门过滤、环境过滤、通用关联。
- API：上传/下载/预览、类型伪装、大小、路径穿越、跨部门 403、审计。
- Data：MOCK 文件签名、SHA256、旧版本保留、未扫描状态。
- E2E：我的工作、部门工作区、文件上传/预览/版本、390px、WF-001 回归。
- Migration：002→003→003 down。
- Backup：恢复验证。

## 🚫 明确不修改

- 旧 `AIDT/LOCAL-MVP`
- 冻结技术栈
- 现有 Elsa、Hangfire、Garnet 机制
- 真实外部系统、真实模型、生产 SSO
- 钉钉听记和 SLA/代理能力

## ✅ 完成证据

- 部门、文件、环境领域和契约已编译。
- 我的工作、部门工作区、文件中心和管理入口页面已完成。
- 003 up/down/re-up 与 004 up 已验证，主库迁移完成。
- SQL/API 集成 64/64；前端 7/7；Playwright 5/5。
- 文件上传、下载、预览、版本、通用关联、敏感文件门禁、跨部门拒绝和审计已验证。
- 应用部署、A 版本回滚、备份恢复和离线依赖验证完成。
- 整个 AIDT 需求池仍未完成，本批次只交付四个需求。

## ↩️ 回滚

1. 应用回滚到基线提交 `8aff291` 或上一发布版本。
2. 数据库使用 `AIDT_REQ_BATCH1_PRE_20261031_010423.bak` 恢复。
3. 若仅回滚结构，执行新迁移 down。
4. 文件物理存储目录保留，避免误删已上传证据。
