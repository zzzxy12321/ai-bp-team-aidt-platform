# 前端导航与 IAM 权限批次上下文包

> 状态：`CONTEXT_PACK_READY`
>
> 分支：`feature/frontend-navigation-iam`

## 🎯 本轮目标

实现 `NAV-001`、`IAM-001`、`WFROLE-001`，使授权人员可通过页面维护组织、人员、角色、权限、导航、业务角色和流程节点处理人策略，并由后端真实执行权限校验和审计。

## ✅ 已确认需求

- 平台管理员可创建“研发部”栏目并生成默认入口。
- 部门负责人只能维护授权部门导航。
- 系统入口不可删除。
- 导航停用/恢复不删除业务数据。
- 后端按当前用户权限返回导航树；直接 API 访问仍校验最终权限。
- 公司/中心/部门/班组树形组织。
- 主职和兼职部门不复制人员主数据。
- 角色区分系统、业务、Agent 服务角色。
- 角色权限可配置，权限目录只读。
- 用户角色可按组织/部门范围分配。
- 数据范围至少支持组织、部门、本人、参与项目。
- 禁止自提权；高风险授权有双人审核接缝。
- 流程节点支持六类处理人策略。
- 任务创建时冻结角色和候选处理人解析结果。
- 历史任务不受后续角色成员变更影响。
- 项目经理必须能以真实岗位身份完成分配给自己的审批。
- 菜单隐藏不能代替后端权限。

## 🧱 当前可复用资产

| 资产 | 当前位置 | 复用方式 |
|---|---|---|
| 组织与人员 | `Identity.Organization/User` | 保留主数据 |
| 部门与成员 | `Identity.Department/UserDepartmentMembership` | 扩展树与层级 |
| 角色/权限 | `Identity.Role/Permission/UserRole/RolePermission` | 扩展类型并保留旧关联 |
| 数据范围 | `Identity.DataScope` | 扩展本人/项目语义 |
| 本地 Cookie 身份 | `AuthController`、`HttpContextCurrentUserAccessor` | 继续仅作 Demo 身份 |
| 导航 | `frontend/src/layouts/AppLayout.vue` | 改为后端动态树 |
| 路由权限 | `frontend/src/router/index.ts` | 保留前端提示，API 为真源 |
| 流程配置 | `Configuration.ConfigurationVersion` | 扩展节点处理人策略 |
| 流程任务 | `Workflow.WorkflowTask/Candidate/Policy` | 增加冻结快照和转交 |
| 项目负责人 | `PMO.ProjectProfile.OwnerUserId` | 动态解析 `ProjectOwner` |
| 审计 | `Audit.AuditEvent` | 追加所有 IAM/导航/角色解析动作 |

## 🗄️ 当前基线与关键数据

- 当前主库：`AIDT`，8 条 EF 迁移，51 张应用表。
- 当前角色：`platform-admin`、`rd-engineer`、`quality-engineer`、`supply-specialist`、`finance-approver`、`production-team`、`project-manager`、`project-event-agent`。
- `project-manager` 有配置读取/创建/更新和项目能力，也已有 `workflow-task:read`。
- `project-manager` 缺少 `workflow-task:approve/return/terminate`，导致真实经理任务可收到但不能处理。
- 当前配置节点只强制 `assigneeUserName`，发布时只校验有效真人用户。
- 当前任务只有 `AssigneeId`，没有业务角色版本、候选快照和原责任人。
- 当前部门只有直接成员关系与部门 DataScope，没有 Parent、Path、HierarchyType、SortOrder。
- 当前本地数据规模无真实生产敏感数据；仍禁止把演示身份描述为生产 SSO。

## 📚 已读取基线

- `AGENTS.md`
- `docs/001-产品PRD/00-项目目标.md`
- `docs/001-产品PRD/01-需求基线.md`
- `docs/001-产品PRD/02-技术栈与架构.md`
- `docs/001-产品PRD/03-计划方案.md`
- `docs/001-产品PRD/04-需求池.md` 中 `IDEA-NAV-001/IAM-001/WFROLE-001`
- `docs/project/REQ_CONFIG_CENTER_FREEZE_MANIFEST.md`
- `docs/project/FEATURE_MAP.md`
- `docs/project/PERMISSION_MATRIX.md`
- `docs/project/CONFIGURATION_CENTER_BACKEND_DELIVERY.md`
- `docs/project/CONFIGURATION_CENTER_FRONTEND_DELIVERY.md`
- `docs/project/STEP6_MULTI_ROLE_PILOT_REPORT.md`
- `docs/project/STEP7_KPI_VALIDATION_REPORT.md`
- `task_plan.md`、`findings.md`、`progress.md`
- `standards/00`–`14`

## 🔎 代码上下文

### 身份与权限

- `backend/AIDT.Domain/Identity/UserPrincipal.cs`
- `backend/AIDT.Domain/Identity/Department.cs`
- `backend/AIDT.Infrastructure/Persistence/Entities.cs`
- `backend/AIDT.Infrastructure/Persistence/AidtModelConfigurations.cs`
- `backend/AIDT.Infrastructure/Persistence/Repositories/SqlIdentityRepositories.cs`
- `backend/AIDT.Api/Identity/HttpContextCurrentUserAccessor.cs`
- `backend/AIDT.Api/Controllers/AuthController.cs`

### 流程

- `backend/AIDT.Domain/Workflows/WorkflowModels.cs`
- `backend/AIDT.Workflow/WorkflowEngineAdapter.cs`
- `backend/AIDT.Application/UseCases/StartWorkflowInstanceHandler.cs`
- `backend/AIDT.Application/UseCases/ProcessWorkflowTaskHandler.cs`
- `backend/AIDT.Infrastructure/Services/SqlWorkflowConfigurationService.cs`
- `backend/AIDT.Infrastructure/Services/SqlBatch2Service.cs`
- `backend/AIDT.Infrastructure/Persistence/Repositories/SqlWorkflowRepository.cs`

### 前端

- `frontend/src/layouts/AppLayout.vue`
- `frontend/src/router/index.ts`
- `frontend/src/stores/auth.ts`
- `frontend/src/pages/ConfigurationCenterPage.vue`
- `frontend/src/pages/WorkflowPage.vue`
- `frontend/src/api/types/contracts.ts`

### 数据库与部署

- `backend/AIDT.Infrastructure/Persistence/Migrations/`
- `database/migrations/008_ConfigurationCenter.up.sql`
- `database/migrations/migration-state.json`
- `database/dba-review/`
- `deploy/build-release.ps1`
- `deploy/deploy.ps1`
- `deploy/rollback.ps1`
- `frontend/e2e/aidt.spec.ts`

## 🔐 硬边界

- 冻结标签不修改、不移动、不覆盖。
- 001–008 不修改。
- 新迁移从 009 开始。
- 不引入第二数据库、第二认证、第二 UI 组件库或新消息系统。
- 不连接真实钉钉、U9、ERP、DCS。
- 不启用真实外部模型。
- 本地演示 Cookie 不是生产 SSO。
- 高风险永久人工动作不由 Agent 自动完成。

## 🧾 本批不处理问题

- 事件分析/确认状态持久化。
- 项目主状态持久化。
- 文件上传自动项目证据关系。
- 数字员工截止日期偏移。
- “我的待办”状态过滤。

上述问题继续在 `findings.md` 和交付报告中登记，除非本批 E2E 证明其直接阻断本批必需能力。

## 🧭 验收对象

- 平台管理员：跨组织维护、发布和审计。
- 部门负责人/项目经理：仅维护授权部门导航和分配允许的角色。
- 研发工程师：发起流程并以研发业务角色处理研发节点。
- 项目经理：以经理审核业务角色处理自己的经理节点。
- 质量工程师：以质量复核业务角色处理质量节点。
- 普通员工：看不到无权限导航，直接访问 API 返回 403。
