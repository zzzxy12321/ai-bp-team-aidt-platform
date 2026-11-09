# 009 前端导航与 IAM 权限数据模型

> 迁移：`20261031195740_009_FrontendNavigationIamWorkflowRoles`
>
> 状态：待主库应用 / 已隔离验证

## 🎯 目标

在不修改 001–008 的前提下，新增组织树、范围化角色分配、高风险授权审核、导航版本和个人偏好，并扩展流程节点处理人冻结证据。

## 🏢 Identity

### 扩展表

| 表 | 新增字段 | 用途 |
|---|---|---|
| `Identity.Department` | `ParentDepartmentId`、`HierarchyType`、`Path`、`SortOrder`、`Version` | 公司下中心/部门/班组树 |
| `Identity.User` | `InvitationStatus`、`InvitedAt`、`ActivatedAt`、`Version` | 本地受控邀请和开通 |
| `Identity.Role` | `RoleType`、`Description`、`IsSystem`、`IsActive`、`Version`、时间戳 | 系统/业务/Agent 服务角色分类 |

### 新增表

| 表 | 主键/唯一约束 | 用途 |
|---|---|---|
| `Identity.UserRoleAssignment` | `UX_UserRoleAssignment_User_Role_Scope` | 组织/部门范围化角色分配 |
| `Identity.AccessChangeRequest` | `UX_AccessChangeRequest_Requester_Idempotency` | 高风险授权双人审核接缝 |

## 🧭 Navigation

| 表 | 用途 |
|---|---|
| `Navigation.NavigationWorkspace` | 稳定工作区身份、关联部门和发布状态 |
| `Navigation.NavigationVersion` | Draft/Published/Retired 版本和 Revision |
| `Navigation.NavigationItem` | 入口、路由、排序、显隐、启停和权限绑定 |
| `Navigation.NavigationUserPreference` | 用户级收藏、隐藏和排序 |

导航不复制部门主数据；`DepartmentId` 直接引用 `Identity.Department`。

## 🔐 Workflow

### `Workflow.WorkflowNodePolicy`

- `AssigneeStrategy`
- `BusinessRoleId`
- `AllowTransfer`
- `TransferTargetStrategy`

### `Workflow.WorkflowTask`

- `AssigneeStrategy`
- `BusinessRoleId / BusinessRoleCode / BusinessRoleVersion`
- `CandidateUserIdsJson`
- `AssigneeSnapshotJson`
- `OriginalAssigneeId / BackupAssigneeId`
- `TransferredBy / TransferredAt / TransferReason`

任务创建时冻结实际解析出的候选人集合和角色版本；审批只使用冻结快照。

## 🌱 回填与种子

- 历史角色按编码回填为 `System / Business / AgentService`。
- 历史用户回填 `InvitationStatus=Active`。
- 历史部门回填 `HierarchyType=Department`、`Path=/CODE`。
- 历史任务回填 `SpecificUser` 和单候选人快照。
- 新建研发、经理审核、质量复核三个业务角色。
- 为研发工程师、项目经理、质量工程师写入部门范围业务角色。
- 创建系统导航工作区和保留入口。
- 增加 `demo.security` 作为第二名本地平台管理员，验证高风险双人复核。

## 🧪 隔离验证

- 基线库：`AIDT_FRONTEND_NAV_TEST_20261101_040523`
- 序列：8 迁移/51 表 → 009 up/9 迁移/57 表 → down/8/51 → re-up/9/57。
- Down 清理业务角色、权限、`demo.security`、新表和 `Navigation` Schema。
- up/re-up 后导航工作区 1、范围化角色分配 3、项目经理动作权限 3/3。
- 最终全量 SQL/API：104/104 通过。

## ↩️ 回滚

应用回滚使用备份恢复或 009 down；主库 down 仅用于受控恢复窗口，正式演练已在隔离库完成。
