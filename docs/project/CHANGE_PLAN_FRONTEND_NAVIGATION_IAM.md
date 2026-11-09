# 前端导航与 IAM 权限批次变更计划

> 状态：`DEPLOYED_AND_VERIFIED_NOT_PRODUCTION_READY`
>
> 分支：`feature/frontend-navigation-iam`
>
> 基线：`main@40c30d5`（冻结标签 `req-config-center-frozen-v1` 的发布记录之后仅新增本批任务定义）
>
> 功能：`NAV-001`、`IAM-001`、`WFROLE-001`

## 🎯 目标

把当前固定在前端源码中的部门导航、由数据库种子维护的角色权限，以及只能指定用户名的流程节点处理人，升级为受权限、版本、审计和测试约束的配置能力。

最终闭环：

```text
组织/人员
 → 角色与数据范围
 → 权限目录与角色授权
 → 导航/工作区发布
 → 流程业务角色
 → 节点处理人解析快照
 → 后端动作校验
 → 审计与回放证据
```

## 📌 基线事实

- `main` 工作区在创建分支前干净，HEAD 为 `40c30d5`。
- 冻结标签不移动、不覆盖；本批不修改迁移 001–008。
- 主库 `AIDT` 当前 8 条 EF 迁移，应用 Schema 表数为 51。
- 当前 8 个角色由种子和迁移维护；已有 `platform-admin`、`project-manager` 等角色。
- `project-manager` 已有 `workflow-task:read`，缺少 `approve/return/terminate`，是冻结清单 P0。
- 部门已有主部门、兼职成员和负责人字段，但没有父子树、层级类型和排序。
- `WorkflowNodePolicy` 已支持固定处理人、备份处理人、SLA、Agent 策略，但任务创建和审批没有业务角色资格快照。
- 前端导航在 `AppLayout.vue` 硬编码，后端没有导航模型和当前用户导航 API。

## ✅ 本批范围

### NAV-001

- 新增导航工作区、导航版本、导航项和用户偏好数据模型。
- 导航工作区复用 `Identity.Department`，不建立第二套部门主数据。
- 系统入口“我的工作、待办、事件收件箱”为系统保留项，禁止停用或隐藏。
- 默认模板支持部门工作区、流程事项、业务视图、部门文件，以及可选项目、听记、报表。
- 平台管理员维护组织级导航；拥有导航权限且具备部门范围的负责人只能维护授权部门工作区。
- 导航发布版本化；停用/恢复只改变展示状态，不删除部门或业务数据。
- 当前导航树由后端按组织、部门、菜单权限和用户偏好生成。
- 直接访问无权限路由仍由前端守卫提示，后端 API 始终执行最终权限拒绝。

### IAM-001

- 部门增加父级、路径、层级类型、排序、负责人和启停状态。
- 组织树返回公司根节点与中心/部门/班组子树。
- 提供人员列表、详情、邀请/本地开通、停用、重新启用和批量导入。
- 人员允许主职和兼职部门，不复制人员主数据。
- 角色增加类型、说明、系统保留、启停和版本字段。
- 提供角色新增、复制、修改、停用、权限矩阵和用户角色分配。
- 系统角色不可删除或停用；使用中的角色停用必须阻断。
- 用户角色分配可限定组织或部门范围。
- 数据范围支持组织、部门、本人、参与项目。
- 禁止用户给自己提升权限；高风险角色/权限变更进入双人审核接缝。
- 所有组织、人员、角色、权限、成员关系和数据范围变更写追加式审计。

### WFROLE-001

- 流程配置节点新增处理人策略：
  - `SpecificUser`
  - `BusinessRole`
  - `DepartmentManager`
  - `ProjectOwner`
  - `InitiatorManager`
  - `Agent`
- 发布校验处理人策略、部门范围、角色成员、主管、项目负责人和 Agent 配置是否可解析。
- 任务创建时冻结策略、业务角色版本、候选人、原责任人和解析证据。
- 审批时必须同时校验：
  - 当前任务处理人；
  - 冻结的节点业务角色资格；
  - 节点动作权限；
  - 部门和环境数据范围。
- 角色成员后续变化只影响新任务，不改变历史任务的处理资格快照。
- 支持备份处理人、升级和转交；转交后保留原责任人。
- `project-manager` 获得流程任务动作权限，但只能处理分配给自己的节点，不获得任意任务代审权。
- 永久人工边界保持不变，Agent 不得绕过高风险审批。

## 🚫 明确不扩大修复范围

以下冻结问题仅登记、验证未被破坏，不因本批非必要原因修改：

1. 事件状态持久化。
2. 项目状态持久化。
3. 文件上传自动形成项目证据关系。
4. 数字员工截止日期偏移。
5. 已完成任务仍出现在“我的待办”。

若实现过程中发现上述问题直接阻断本批验收，将先在变更计划中登记原因，再做最小必要修复。

## 🧱 数据设计

### Identity

扩展现有表：

- `Department`：`ParentDepartmentId`、`HierarchyType`、`Path`、`SortOrder`。
- `User`：本地邀请/开通状态、邀请时间、开通时间。
- `Role`：`RoleType`、`Description`、`IsSystem`、`IsActive`、`Version`、时间戳。

新增表：

- `UserRoleAssignment`：用户、角色、组织/部门范围、启停、授权人和授权时间。
- `AccessChangeRequest`：高风险 IAM 变更、申请人、审批人、状态、差异和幂等键。

不复制人员主数据；不新建部门主数据表。

### Navigation

新增 `Navigation` Schema 和表：

- `NavigationWorkspace`：稳定工作区身份、组织、关联部门、类型和展示状态。
- `NavigationVersion`：草稿/发布/退役版本、Revision、发布证据。
- `NavigationItem`：版本内入口、路由、排序、显隐、启停、系统保留和权限绑定。
- `NavigationUserPreference`：用户级排序、收藏和隐藏。

不保存业务数据副本；导航项只存路由和权限引用。

### Workflow

扩展 `WorkflowNodePolicy`：

- 处理人策略和业务角色引用。
- 转交/升级策略。

扩展 `WorkflowTask`：

- 处理人策略；
- 业务角色及冻结版本；
- 候选人快照 JSON；
- 原责任人、备份处理人和转交证据。

### 迁移

- 原设计曾拆为 009/010/011；实施评审后将组织树、范围化角色、高风险变更请求、导航和流程角色快照合并为单一迁移 009。
- 合并原因：三类结构共享同一批冻结数据和权限种子，拆分会增加三次备份/恢复和跨迁移临时状态风险；单一 009 已在隔离库完成 `up → down → re-up` 后应用到主库。
- 001–008 未修改。

所有迁移遵循 SQL Server `GO` 批处理规则。先备份主库并 `RESTORE VERIFYONLY`，再从基线备份恢复隔离库执行 `up → down → re-up`。隔离验证通过后才可应用到主库。

## 🔌 API 设计

### IAM

```text
GET    /api/v1/iam/organization-tree
POST   /api/v1/iam/departments
PUT    /api/v1/iam/departments/{id}
POST   /api/v1/iam/departments/{id}/actions/deactivate
POST   /api/v1/iam/departments/{id}/actions/reactivate
GET    /api/v1/iam/users
GET    /api/v1/iam/users/{id}
POST   /api/v1/iam/users
POST   /api/v1/iam/users/import
PUT    /api/v1/iam/users/{id}/memberships
PUT    /api/v1/iam/users/{id}/data-scopes
POST   /api/v1/iam/users/{id}/actions/deactivate
POST   /api/v1/iam/users/{id}/actions/reactivate
GET    /api/v1/iam/permissions
GET    /api/v1/iam/roles
POST   /api/v1/iam/roles
POST   /api/v1/iam/roles/{id}/actions/copy
PUT    /api/v1/iam/roles/{id}
PUT    /api/v1/iam/roles/{id}/permissions
PUT    /api/v1/iam/roles/{id}/members
POST   /api/v1/iam/roles/{id}/actions/deactivate
POST   /api/v1/iam/roles/{id}/actions/reactivate
GET    /api/v1/iam/access-requests
POST   /api/v1/iam/access-requests/{id}/actions/approve
```

### 导航

```text
GET    /api/v1/navigation/current
GET    /api/v1/navigation/preferences
PUT    /api/v1/navigation/preferences
GET    /api/v1/navigation/workspaces
POST   /api/v1/navigation/workspaces
GET    /api/v1/navigation/workspaces/{id}
PUT    /api/v1/navigation/workspaces/{id}/draft
POST   /api/v1/navigation/workspaces/{id}/actions/publish
POST   /api/v1/navigation/workspaces/{id}/actions/retire
POST   /api/v1/navigation/workspaces/{id}/actions/restore
```

### 流程业务角色

```text
GET    /api/v1/workflow-roles
GET    /api/v1/workflow-roles/{id}/members
PUT    /api/v1/workflow-roles/{id}/members
POST   /api/v1/workflow-tasks/{id}/actions/transfer
POST   /api/v1/workflow-tasks/{id}/actions/escalate
```

所有写接口要求 CSRF、权限、幂等键和事务内审计。列表默认分页，错误码保持稳定。

## 🖥️ 前端

- 新增 `/iam`：组织树、人员、角色权限矩阵、用户角色、数据范围、导航配置、影响预览与审计。
- `AppLayout.vue` 改为读取 `/api/v1/navigation/current`，按后端返回分组渲染。
- 导航支持桌面侧栏和 390px 横向滚动，无横向溢出。
- 流程配置节点增加处理人策略和业务角色选择。
- 流程待办展示节点角色、原责任人和转交/升级状态，但动作仍由后端裁决。
- 新增组件测试覆盖导航渲染、IAM 权限矩阵和流程角色配置。

## 🔐 权限与审计

新增权限：

- `identity-admin:read/manage`
- `role:read/create/update/manage`
- `permission:read/assign`
- `navigation:read/manage/publish`
- `data-scope:update`
- `workflow-role:read/manage`
- `workflow-task:update`（转交/升级）

审计动作：

- `iam.department.created/updated/deactivated/reactivated`
- `iam.user.invited/activated/deactivated/reactivated/imported`
- `iam.membership.updated`
- `iam.role.created/copied/updated/deactivated`
- `iam.role_permissions.updated`
- `iam.user_roles.assigned/removed`
- `iam.data_scope.updated`
- `iam.access_request.created/approved/rejected`
- `navigation.workspace.created/draft_updated/published/retired/restored`
- `navigation.preference.updated`
- `workflow.assignment_resolved`
- `workflow.role.members_updated`
- `task.transferred/escalated`

## 🧪 验收与测试

- Domain：组织树、角色停用、自提权拒绝、处理人策略和任务冻结快照。
- Application：节点资格、动作权限、历史快照、转交和权限拒绝。
- SQL/API：IAM CRUD、导航发布/停用/恢复、403、自提权、使用中角色阻断、双人审核。
- 真实经理链路：研发工程师发起 → 项目经理按经理业务角色审批 → 质量工程师复核完成。
- 前端：类型检查、组件测试、OpenAPI、生产构建。
- Playwright：正式部署桌面与 390px；IAM、导航、流程角色、真实项目经理审批。
- 数据库：009 隔离库 `up → down → re-up`，主库应用、备份恢复。
- 应用：新版本部署、健康检查、E2E、回滚到冻结版本再回滚。
- 发布：从干净提交构建，manifest `sourceRevision` 不含 `-dirty`。

## ↩️ 回滚

1. 应用回滚到 `req-config-center-final3`。
2. 迁移 009 先隔离库 down/re-up 验证；正式恢复窗口优先使用备份，不在主库直接 down。
3. 保留新权限、导航和角色审计记录作为发布证据，不直接改写审计历史。
4. 冻结标签 `req-config-center-frozen-v1` 永不移动。

## 🚧 生产化边界

- 仍使用本地演示 Cookie 身份，不表述为生产 SSO/OIDC/MFA。
- 仍不连接真实钉钉、U9、ERP、DCS。
- 仍不调用真实外部模型。
- SQL Server Express 与单机部署不满足生产高可用。
