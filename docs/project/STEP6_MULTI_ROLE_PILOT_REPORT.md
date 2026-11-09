# AIDT 第二轮多岗位试点报告

> 状态：`PILOT_COMPLETED_WITH_ROLE_BLOCKER`
>
> 日期：2026-11-01
>
> 环境：`http://127.0.0.1:8088` / `req-config-center-final2`

## 🎯 结论

第二轮试点完成了研发、项目经理、质量工程师和平台管理员四类身份的实际验证。

- 员工事项发起：通过
- 流程配置创建与校验：通过
- 平台管理员发布：通过
- 研发工程师处理首节点：通过
- 质量工程师处理终节点：通过
- 普通岗位配置权限隔离：通过
- 项目经理以真实岗位身份审批：**失败**

由于 `project-manager` 角色缺少流程任务通过/退回权限，原计划“项目经理直接审批”的标准流程无法完成。

在不修改权限代码的前提下，本轮建立了一条“经理节点由平台管理员兼岗”的等价流程并完整走通。

## 👥 参与身份

| 岗位 | 展示身份 | 本轮职责 | 结果 |
|---|---|---|---|
| 研发工程师 | `demo.researcher` | 发起并处理测试申请 | 通过 |
| 项目经理 | `demo.pm` | 创建/校验配置、接收经理节点 | 配置通过；任务处理失败 |
| 质量工程师 | `demo.qa` | 最终数据判定 | 通过 |
| 平台管理员 | `demo.admin` | 发布配置、经理节点兼岗、流程发起 | 通过 |

## 🧪 试点 A：项目经理直接审批

### 配置

- 配置 ID：`4d4c5d19-e61e-4837-8bc1-9e990a3b6ef9`
- 编号：`CFG-ROLE-PILOT-20261101` v1
- 节点：
  1. 研发提交测试申请 → `demo.researcher`
  2. 项目经理审核 → `demo.pm`
  3. 质量工程师数据判定 → `demo.qa`

### 项目与实例

- 项目：`PRJ-CFG-PILOT-20261101-01`
- 项目 ID：`4bed28fb-2f9d-4336-8234-4bdc0f36584d`
- 流程实例：`f0b677ee-0bad-402e-9519-fe67d52eea9b`

### 结果

- 研发工程师成功通过 `TEST_REQUEST`。
- `MANAGER_REVIEW` 正确分配给项目经理。
- 项目经理点击“通过”时返回：`PERMISSION_DENIED`。
- 平台管理员尝试代为处理时返回：`TASK_ASSIGNEE_MISMATCH`。
- 流程停在 `MANAGER_REVIEW`，经理任务保持 `Pending`。

### 根因

`project-manager` 当前具备 `workflow-task:read`，但没有 `workflow-task:approve`、`workflow-task:return` 或 `workflow-task:terminate`。

这不属于流程配置本身错误，而是岗位权限种子与流程任务动作权限不匹配。

## ✅ 试点 B：管理员兼岗经理节点

### 配置

- 配置 ID：`153b9b53-f8af-4d17-bd97-b032632444e4`
- 编号：`CFG-ROLE-PILOT-20261101` v2
- 节点：
  1. 研发提交测试申请 → `demo.researcher`
  2. 经理复核（平台管理员兼岗）→ `demo.admin`
  3. 质量工程师数据判定 → `demo.qa`

### 项目与实例

- 项目：`PRJ-CFG-PILOT-20261101-02`
- 项目 ID：`7ded1f79-06b1-43fa-97c3-8042bb211fb4`
- 流程实例：`5e1f3e40-4e83-466f-ba61-402a12cd1b1d`

### 结果

| 节点 | 处理人 | 状态 |
|---|---|---|
| `TEST_REQUEST` | 研发工程师 | Approved |
| `MANAGER_REVIEW` | 平台管理员兼岗 | Approved |
| `QA_JUDGEMENT` | 质量工程师 | Approved |

- 流程实例：`Completed`
- 结束时间：`2026-11-01 03:04:57`
- 项目状态：`Active`
- 项目需要后续显式验收/关闭，流程完成不会自动代替项目经理关闭项目

### 审计

| 动作 | 实体 | 执行身份 | 结果 |
|---|---|---|---|
| Start | WorkflowInstance | 平台管理员 | Succeeded |
| Approve | TEST_REQUEST | 研发工程师 | Succeeded |
| Approve | MANAGER_REVIEW | 平台管理员兼岗 | Succeeded |
| Approve | QA_JUDGEMENT | 质量工程师 | Succeeded |

## 🔐 权限隔离

- 研发工程师访问 `/configurations`：`403`，被路由守卫拦截。
- 质量工程师访问 `/configurations`：`403`，被路由守卫拦截。
- 项目经理可以创建、编辑和校验配置。
- 项目经理页面不显示发布按钮。
- 平台管理员可以发布和回滚。

证据：

- `docs/project/test-artifacts/desktop-role-pilot-completed.png`
- `docs/project/test-artifacts/desktop-role-pilot-access-denied.png`

## 📊 本轮指标

| 指标 | 结果 |
|---|---:|
| 启动流程实例 | 2 |
| 完整完成实例 | 1 |
| 流程完成率 | 50% |
| 流程任务总数 | 5 |
| 已审批任务 | 4 |
| 任务完成率 | 80% |
| 真实项目经理成功审批 | 0/1 |
| 普通岗位越权进入配置中心 | 0/2 |
| 成功实例审批链路 | 3/3 |

## ⚠️ 新增问题

### P0：项目经理岗位缺少流程任务动作权限

- 影响：任何分配给项目经理的人工审批节点都无法由项目经理完成。
- 当前表现：页面能看到任务，但后端拒绝动作。
- 临时绕行：把经理节点分配给平台管理员兼岗。
- 建议修复：
  - 授予 `project-manager` 角色 `workflow-task:approve`、`workflow-task:return` 和 `workflow-task:terminate`；
  - 或建立明确的“部门经理”流程角色，与项目负责人角色解耦；
  - 补充“项目经理处理真实流程任务”的 SQL/API/E2E 回归。

### 观察项：流程完成后项目仍为 Active

- 本轮没有把流程完成自动映射为项目完成。
- 对多阶段项目而言这是合理行为，但前端后续应明确提示“流程已完成，项目等待验收/关闭”。

### P1：已完成流程任务仍出现在“我的待办”

- `QA_JUDGEMENT` 已完成并写入审计，但页面重新加载后仍显示在“我的待办”列表。
- 已完成任务仍显示“通过/退回/终止”按钮，存在重复点击和误操作风险。
- 根因方向：`/api/v1/workflow-tasks` 未传状态条件时返回全部任务，前端“我的待办”直接展示全部结果。
- 修复建议：
  - 前端默认只请求 `Pending/InProgress`；
  - 已处理任务放入历史页签；
  - 对非待办任务禁用动作按钮。

## 🧾 前序问题保留

按要求，本轮没有修复：

1. 事件状态未持久化
2. 项目状态未持久化
3. 附件未自动形成项目证据关系
4. 数字员工日期识别偏移一天

## 🎯 下一步

第 7 项可以基于本轮指标开展验证，但结论必须区分：

- 技术闭环可执行；
- 研发、质量和管理员兼岗岗位链路可执行；
- 真实项目经理审批链路当前被权限阻断。

在修复该权限问题前，不应宣称“多岗位流程已完全验收通过”。
