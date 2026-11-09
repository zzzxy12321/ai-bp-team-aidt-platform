# 前端导航与 IAM 权限批次影响分析

> 状态：`IMPACT_ANALYZED`
>
> 基于 `main@40c30d5`

## 🎯 变更目标

将组织、人员、角色、权限、数据范围、导航和流程节点处理人从固定种子/代码配置升级为受控、版本化、可审计的配置能力，并修复项目经理能收到任务但无法审批的 P0。

## 📦 影响矩阵

| 范围 | 变更 | 上游调用方 | 下游影响 | 验证 |
|---|---|---|---|---|
| Identity 模型 | 部门树、角色类型、用户邀请、范围化角色分配、高风险审核 | Auth、部门工作区、项目、流程 | 会话角色/权限、数据范围、管理页面 | Domain、SQL、API、E2E |
| Navigation | 新增 Schema、版本、导航项、个人偏好 | AppLayout、路由守卫 | 左侧栏、390px、直达路由 | Vue、API、桌面/移动 E2E |
| Configuration | 节点增加策略/业务角色字段 | 配置页面、发布服务、WorkflowEngine | 新发布流程版本、历史配置兼容 | 反序列化兼容、发布/回滚测试 |
| Workflow | 任务冻结处理人快照、转交/升级、动作授权 | 待办、审批、SLA、Agent | 任务创建、审批、历史回放 | Domain/Application/SQL/API/E2E |
| Permission seeds | 新 IAM/导航/流程角色权限，补齐 project-manager 动作 | Cookie 主身份、路由守卫、API | 权限体积、管理员会话 | Cookie 超限回归、403/allow 测试 |
| Database | 最终合并为单一 009 | 应用启动不自动迁移 | 旧数据回填、外键、索引 | 隔离 up/down/re-up、恢复 |
| Frontend | 新 IAM/导航页面、动态侧栏、配置节点策略 | 用户、平台管理员、项目经理 | 页面信息架构、移动布局 | 类型/组件/OpenAPI/build/E2E |
| Deploy | 新版本与回滚 | Nginx 测试部署 | 发布包、manifest、数据库备份 | 健康、回滚、恢复 |

## 🔁 调用链影响

### 当前用户导航

```text
AppLayout
 → navigationApi.getCurrent()
 → NavigationController
 → NavigationQueryService
 → NavigationVersion/Item + Identity.RolePermission + Department
 → permission-filtered tree
```

后端过滤后，前端不再自行猜测菜单。前端路由守卫用于 UX；对应 API 仍独立执行权限。

### 登录与会话

```text
DemoLogin
 → SqlUserRepository
 → legacy UserRole + scoped UserRoleAssignment
 → permissions/roles/data scopes
 → compact Cookie claims
```

需要保持旧 Cookie 解析和压缩声明的兼容性，新增权限不能导致管理员 Set-Cookie 超限。

### 流程节点解析

```text
ConfigurationNode(strategy, role)
 → publish validation
 → WorkflowVersion.DefinitionJson
 → WorkflowEngineAdapter
 → IWorkflowAssignmentResolver
 → frozen candidate snapshot
 → WorkflowTask
 → ProcessTask authorization
 → process/deny audit
```

处理人解析结果写入任务快照。后续角色成员变化不会重新解释历史任务。

### 审批动作

```text
CurrentUser
 → workflow-task action permission
 → department scope
 → task assignee
 → frozen business-role/candidate membership
 → expected version/idempotency
 → task/instance/audit/outbox transaction
```

`project-manager` 的动作权限必须与后三项共同满足，不能形成全局任意审批。

## 🗄️ 数据库影响

### 兼容策略

- 只扩展现有 `Department/User/Role/WorkflowNodePolicy/WorkflowTask`，不重建表。
- 保留现有 `UserRole` 作为旧全局角色关系；新范围化关系进入 `UserRoleAssignment`。
- 现有 8 个角色按编码回填类型和系统保留标记。
- 现有已发布 `WorkflowVersion.DefinitionJson` 缺少策略字段时默认 `SpecificUser`，使用原 `assigneeUserName`。
- 现有 `WorkflowTask` 无快照时在读取 DTO 时标记 `LegacySpecificUser`，不修改历史责任。
- 导航迁移为现有组织创建系统导航工作区并生成保留入口。
- 新表均使用外键、检查约束和查询索引；审计不删除。

### 回滚风险

- 导航为新增 Schema，down 可整体移除。
- IAM 扩展字段和范围化角色分配为新增结构；down 必须清理本批权限、角色分配和新字段。
- 流程任务新增字段在 down 后历史任务仍保留原有核心字段；发布配置的 DefinitionJson 若引用新策略，回滚应用前需恢复 008 版本或应用兼容旧代码。
- 主库 down 只能在恢复窗口按备份进行，不直接在主库首次演练。

## 🔐 权限影响

新增管理权限，不将 `identity-admin:manage` 授予所有部门负责人。

平台管理员：

- 全部组织/人员/角色/权限/导航权限。
- 跨部门数据范围。

项目经理/部门负责人：

- 可看授权部门 IAM 基础信息。
- 可维护授权部门导航。
- 可按被授予的能力分配限定范围业务角色。
- 不能给自己新增角色或权限。

普通员工：

- 只使用被分配角色对应的菜单和 API。
- 无管理权限返回 403 并写拒绝审计。

高风险：

- 授予系统角色或管理型权限进入待审批状态。
- 审批人与申请人必须不同。
- 无人可自审自己的提权申请。

## 🧪 测试影响

### 必跑回归

- 登录、Cookie 压缩、CSRF、未登录 401。
- 原 8 角色权限和部门数据范围。
- WF-001/WF-002/WF-003/WF-004 现有流程。
- 配置草稿、Revision、校验、发布、回滚。
- SLA/Agent 策略和永久人工边界。
- 项目、事件、文件、审计现有 API。

### 新增专项

- IAM：组织树、邀请/开通、批量导入、角色 CRUD、权限矩阵、角色停用阻断、自提权拒绝、双人审核。
- 导航：默认模板、发布、隐藏、停用、恢复、部门范围、403、偏好。
- WFROLE：六类策略、发布可解析校验、任务冻结、历史成员变更、转交/升级、项目经理真实审批。
- 并发：导航 Revision、角色版本、角色分配重复写入、任务动作并发。
- E2E：正式部署桌面和 390px。

## 📚 文档影响

需要同步更新：

- `docs/project/FEATURE_MAP.md`
- `docs/project/PERMISSION_MATRIX.md`
- `docs/001-产品PRD/04-需求池.md`
- `docs/contracts/openapi/v1.json`
- `database/schema/009...`
- `database/dba-review/009...`
- 新交付报告
- `task_plan.md`、`findings.md`、`progress.md`
- 后端/前端模块 README

## ⚠️ 回归风险

| 风险 | 控制 |
|---|---|
| Cookie 权限声明再次超限 | 保持压缩声明；增加管理员登录/会话回归 |
| 旧流程配置无法反序列化 | 字符串字段可空 + 默认策略；LegacySpecificUser 兼容 |
| 历史任务因角色变化失去处理资格 | 解析结果快照写入任务，审批只读冻结证据 |
| 部门树影响现有 department scope | 改字段不改变原 ID；旧 ScopeReference 继续有效 |
| 导航菜单与直接路由不一致 | 后端菜单过滤 + API 403 + 前端守卫三层验证 |
| 用户角色范围导致权限泄漏 | 范围化分配兼作部门资格；高风险审核；多部门 E2E |
| 迁移 down 丢数据 | 仅测试库执行 down；生产恢复使用备份并保留审计 |
| 本批过大 | 结构最终合并为单一迁移 009，按 IAM → 导航 → WFROLE 测试工作包推进 |

## ↩️ 代码回滚

- 代码：合并前可用单个功能分支回滚；发布后回滚应用目录到 `req-config-center-final3`。
- 数据：优先使用部署前备份恢复到新库或恢复窗口；测试库执行 009 down/re-up。
- 标签：只新增新冻结标签，旧标签保持不动。

## ✅ 实施顺序

1. 写契约与迁移，生成 EF 模型快照。
2. 实现 IAM 后端与角色范围、双人审核。
3. 实现导航后端与当前用户树。
4. 扩展流程配置、处理人解析、任务冻结和审批资格。
5. 实现前端 IAM、导航和节点策略。
6. 补 Domain/Application/SQL/API/Vue/E2E 测试。
7. 隔离库迁移、备份恢复、全量门禁。
8. 提交、非快进合并、干净构建、部署、回滚验证和冻结标签。
