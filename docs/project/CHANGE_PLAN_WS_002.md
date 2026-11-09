# WS-002 可配置岗位工作台模板变更计划

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`
>
> 功能分支：`feature/ui-step4-role-workbench-templates`
>
> 基线：`main@67fa2a8`
>
> 前序冻结：`req-ui-step3-frozen-v1`
>
> 目标发布：`req-ui-step4-final1`
>
> 目标冻结：`req-ui-step4-frozen-v1`

## 🎯 目标

把固定部门工作区升级为服务端解析的岗位工作台：导航决定“有哪些入口”，工作台模板决定“进入部门后首屏显示哪些现有业务卡片”。

## 📦 范围

1. 新增 `Workspace.WorkbenchTemplate / WorkbenchTemplateVersion / WorkbenchCard / WorkbenchAssignment`。
2. 首批注册六类系统模板：研发、质量、供应链、财务、经理、项目经理。
3. 新增模板读取、草稿、复制、校验、发布、停用、恢复、版本历史和影响范围 API。
4. 新增 `GET /api/v1/departments/{id}/workbench` 服务端解析入口。
5. 部门工作区按模板渲染真实卡片；失败时回退通用工作区。
6. IAM 导航配置增加“工作台模板”页签。
7. 新增读取/配置/发布权限、部门范围校验和追加式审计。

## 🔐 不可变边界

- 模板只引用现有能力，不复制部门、项目、流程、文件或业务对象数据。
- 卡片过滤条件为受限结构化字段，不接受 SQL、脚本、HTML、JavaScript 或任意 URL。
- 普通员工只读取已匹配模板；前端不接收全部模板后自行裁决权限。
- 卡片数据执行原业务模块权限、部门/项目/环境范围和永久人工边界。
- 不存在真实 API 的能力只能显示“尚未接入”，不得造数、造指标或造流程。

## 🧱 数据设计

| 表 | 用途 |
|---|---|
| `Workspace.WorkbenchTemplate` | 模板主档、编码、默认/系统标记、组织/部门/角色/岗位范围和状态 |
| `Workspace.WorkbenchTemplateVersion` | 草稿/发布版本、版本号、修订号、校验和影响范围、发布人和时间 |
| `Workspace.WorkbenchCard` | 模板版本内卡片类型、标题、排序、宽度、数据来源、过滤 JSON、权限和启停 |
| `Workspace.WorkbenchAssignment` | 版本级匹配分配，支持用户/角色/部门/组织默认及优先级 |

迁移仅新增 `012_WS002RoleWorkbenchTemplates`，不修改 001–011。

## 🔌 API

| 方法和路径 | 用途 | 权限 |
|---|---|---|
| `GET /api/v1/departments/{id}/workbench` | 匹配模板并聚合当前用户可见卡片 | `workbench-template:read` + 原业务权限 |
| `GET /api/v1/workbench-templates` | 管理列表与授权范围 | `workbench-template:read/manage` |
| `GET /api/v1/workbench-templates/{id}` | 模板、当前草稿/发布版本、卡片和分配 | 同上 |
| `POST /api/v1/workbench-templates` | 新建业务模板 | `workbench-template:manage` |
| `POST /api/v1/workbench-templates/{id}/actions/copy` | 复制系统模板为业务模板 | `workbench-template:manage` |
| `PUT /api/v1/workbench-templates/{id}/draft` | 保存草稿、卡片和分配 | `workbench-template:manage` |
| `POST /api/v1/workbench-templates/{id}/actions/validate` | 校验并返回影响范围 | 同上 |
| `POST /api/v1/workbench-templates/{id}/actions/publish` | 发布新版本 | `workbench-template:publish` |
| `POST /api/v1/workbench-templates/{id}/actions/retire` | 停用 | `workbench-template:manage` |
| `POST /api/v1/workbench-templates/{id}/actions/restore` | 恢复 | `workbench-template:manage` |
| `GET /api/v1/workbench-templates/{id}/versions` | 版本历史 | `workbench-template:read` |

复制、保存和发布使用幂等键；发布与恢复写审计。

## 🧭 匹配优先级

1. 用户级
2. 角色级
3. 部门级
4. 组织默认

同级按 `Priority DESC, SortOrder ASC, PublishedAt DESC`，最终仅返回一个已发布模板。无匹配时响应 `matched=false, fallback=GenericDepartmentWorkspace`。

## 🗂️ 六类默认模板

| 模板 | 首屏问题 | 首批卡片 |
|---|---|---|
| 研发 | 哪些实验、资料和任务需要处理 | 我的研发事项、研发测试对象、研发项目、最近听记、研发文件、逾期任务 |
| 质量 | 哪些检测和异常等待判断 | 质量待办、批次/偏差对象、待复核与 CAPA、质量文件、逾期高风险 |
| 供应链 | 哪些采购和交付事项待处理 | 采购请购、采购流程待办、合同/供应商文件、到货与异常、逾期风险 |
| 财务 | 哪些付款和异常待处理 | 付款审批、财务对象、合同/发票证据、异常待确认、无自动付款提示 |
| 经理 | 团队卡点和风险 | 部门成员待办、部门逾期风险、项目组合、跨部门协调、最近文件 |
| 项目经理 | 哪些项目有风险或待验收 | 我负责的项目、里程碑/任务风险、项目证据、事件/听记来源、流程 SLA |

## 🖥️ 前端

- 重构 `DepartmentPage.vue` 为模板驱动的多卡片工作台。
- 卡片支持 `Loading / Ready / Empty / Error / Denied / NotConnected`。
- 每张卡显示 Top 事项或风险，并可深链到真实详情。
- 390px 单列，1440/1280 多列，无横向溢出。
- IAM 增加“工作台模板”页签，覆盖配置、复制、预览、保存、校验、发布、停用、恢复、历史和影响范围。

## 🧪 门禁

- 6 个系统模板可解析且首屏有实际差异。
- 部门/角色优先级、无匹配回退、跨部门拒绝。
- 无权限卡片不返回，模板不扩大业务权限。
- 草稿不生效；发布、停用、恢复和历史版本可验证。
- 后端/SQL/API、MCP 全量回归；Vitest、OpenAPI、生产构建。
- 源码和正式 E2E 覆盖 1440、1280、390px 及原 32/32 回归。
- 迁移 012 在隔离库 up/down/re-up，主库备份、应用、恢复验证。

## 🚀 发布

从干净提交构建 `req-ui-step4-final1`，部署到 `127.0.0.1:8088`，执行正式 E2E、应用回滚和数据库恢复，再创建 `req-ui-step4-frozen-v1`。
