# 前端导航与 IAM 权限批次收尾影响分析

> 状态：`ANALYZED`
>
> 基线：`main@628e1557f8ad0dc57cf29ce7168f7fcfc0f6c4e6`

## 🎯 变更目标

修复部门范围、IAM 授权边界、使用中角色统计、Agent 节点死链、配置选择器、高风险驳回、人员分页和前端动作权限，并补齐六类处理人策略回归。

## 📦 影响矩阵

| 范围 | 预期变更 | 上游调用方 | 下游影响 | 验证 |
|---|---|---|---|---|
| 业务角色解析 | department scope 收敛到任务部门 | 配置发布、任务创建、审批、转交 | 候选人集合、部门越权、历史快照 | Domain/Application/SQL/API |
| IAM 服务 | 范围校验、权限委派上限、legacy/scoped 统计 | IAM 页面、登录会话、导航 | 角色成员数、停用、授权、自提权 | SQL/API/组件 |
| Agent 执行 | Agent 节点执行、确认、阻断、幂等关联 | 配置、任务、AgentRun、GatewayCall、审计 | 任务状态、Worker、前端提示 | SQL/API/Worker/E2E |
| IAM 审核 | reject API 和必填意见 | IAM 审核页面、审计 | 高风险授权状态 | API/组件/E2E |
| 人员列表 | 服务端分页和搜索 | IAM 人员页、角色成员选择 | 大数据集可用性 | API/组件 |
| 前端权限 | `auth.can(...)` 控制动作 | IAM 页面、配置页、导航 | 按钮可见性与移动布局 | 组件/E2E |
| OpenAPI | reject 和其他新增路由/模型 | 静态契约、前端 client | 契约漂移 | contract check |
| 数据库 | 优先无结构变化；必要时新增 010 | 迁移、备份恢复 | 升级/回滚 | 隔离 up/down/re-up、恢复 |
| 发布 | final2、回滚与冻结 | Nginx、API、Worker、AiGateway | 部署与恢复证据 | 健康、Playwright、回滚 |

## 🔁 关键调用链

### 业务角色候选

```text
WorkflowNodePolicy(BusinessRole)
 → WorkflowAssignmentResolver
 → legacy UserRole + scoped UserRoleAssignment
 → department scope 必须等于 workflowDepartmentId
 → frozen candidates/role version/resolution snapshot
 → WorkflowTask
 → ProcessTask re-authorization
```

### IAM 角色治理

```text
CurrentUser
 → action permission
 → department management scope
 → target role type/scope
 → delegation ceiling/self-escalation/high-risk review
 → transaction + idempotency + audit
```

### Agent 节点

```text
WorkflowTask(Agent strategy)
 → AgentProfile validation
 → risk/skill/amount/human-boundary check
 → AgentRun + GatewayCall
 → auto-complete OR AwaitingHumanConfirmation OR Blocked
 → human confirmation
 → task action + audit + idempotency
```

## 🔐 权限影响

- 普通员工：继续看不到 IAM 管理入口，直接 API 返回 403。
- 项目经理/部门负责人：只维护授权部门；不能分配 organization scope，不能给自己分配角色，不能修改范围外成员。
- 平台管理员：保留跨部门治理能力。
- Agent 服务身份：不能冒充真人审批，不得自确认。
- 只读 IAM 用户：只能查询允许范围，不得写。

## 🗄️ 数据影响

- 角色候选按现有 `ScopeType/ScopeReference` 过滤，不需要复制部门主数据。
- 角色成员统计合并 legacy 与 scoped 关系，不改写历史关系。
- Agent 运行优先复用现有 `AgentRun/GatewayCall/WorkflowTask` 字段。
- 如需保存 Agent 节点确定状态或审核意见，新增 010；不得修改已执行迁移。

## 🧪 回归风险

| 风险 | 控制 |
|---|---|
| 历史任务因成员变化失去处理资格 | 审批只依赖冻结候选快照 |
| 部门收敛误伤 organization 范围成员 | 明确区分 organization 与 department 测试 |
| 全量替换删除范围外成员 | 服务端只替换授权部门集合并断言范围外不变 |
| legacy 角色停用统计不一致 | 两表并集、用户去重、停用用户排除 |
| Agent 服务身份绕过人工边界 | 永久人工边界硬拒绝 + 不得自确认 + 高风险测试 |
| 分页导致角色成员选择遗漏 | 服务端搜索 + 全量选择器测试 |
| 前端隐藏按钮被误当权限控制 | 所有负向 API 仍单独测试 403 |
| 新迁移破坏 009 数据 | 备份、隔离 up/down/re-up、恢复库核对 |

## 📚 文档同步

- 收尾变更计划和交付报告。
- 原变更计划、影响分析中的过期状态和 009/010/011 描述。
- 功能地图、权限矩阵、需求池。
- `task_plan.md`、`findings.md`、`progress.md`。
- OpenAPI、模块 README、迁移状态/DBA 审核（如新增 010）。

## ↩️ 回滚

- 代码回滚到 `req-navigation-iam-final1`。
- 如新增 010，使用部署前备份恢复主库；隔离库执行 010 down 和 re-up。
- 应用回滚 `final2 → final1 → final2`。
- 保留已产生审计，不修改历史标签。
