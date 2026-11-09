# 前端导航与 IAM 权限批次收尾上下文包

> 状态：`READY`
>
> 分支：`fix/navigation-iam-closeout`
>
> 基线：`main@628e1557f8ad0dc57cf29ce7168f7fcfc0f6c4e6`

## 🎯 本轮唯一目标

关闭 `NAV-001 / IAM-001 / WFROLE-001` 在冻结版本中的已知遗漏，完成全量回归、正式部署、回滚恢复和新冻结标签。

## 📚 已读取权威资料

- `AGENTS.md`
- `docs/001-产品PRD/00-项目目标.md`
- `docs/001-产品PRD/01-需求基线.md`
- `docs/001-产品PRD/02-技术栈与架构.md`
- `docs/001-产品PRD/03-计划方案.md`
- `docs/001-产品PRD/04-需求池.md` 中的三项需求
- 原批次变更计划、上下文包、影响分析和交付报告
- `FEATURE_MAP.md`、`PERMISSION_MATRIX.md`
- `task_plan.md`、`findings.md`、`progress.md`
- 身份、权限、API、数据库、前端、测试、审计、发布和回滚规范
- 导航、IAM、工作流角色、工作流任务、Agent、前端页面和集成测试关键代码

## 🧱 当前系统事实

| 项目 | 当前状态 |
|---|---|
| 分支基线 | `main@628e155`，工作区干净 |
| 旧冻结标签 | `req-navigation-iam-frozen-v1` |
| 当前部署 | `req-navigation-iam-final1` |
| 源码发布提交 | `0bd8547e1b0cda2818f76f1ac3751b9ba201b410` |
| 主库迁移 | 9 条，已应用到 009 |
| 应用表 | 57 张 |
| 身份 | 本地 Cookie + CSRF，仅演示 |
| 外部集成 | 钉钉/U9/ERP/DCS 未连接，外部模型关闭 |

## 🔎 关键代码上下文

### IAM 与权限

- `backend/AIDT.Infrastructure/Services/SqlIdentityConfigurationService.cs`
- `backend/AIDT.Infrastructure/Persistence/Entities.cs`
- `backend/AIDT.Infrastructure/Persistence/AidtModelConfigurations.cs`
- `backend/AIDT.Api/Controllers/IamController.cs`
- `backend/AIDT.Api/Identity/HttpContextCurrentUserAccessor.cs`

### 工作流角色与任务

- `backend/AIDT.Infrastructure/Services/SqlWorkflowRoleService.cs`
- `backend/AIDT.Infrastructure/Services/SqlWorkflowTaskAssignmentService.cs`
- `backend/AIDT.Application/UseCases/ProcessWorkflowTaskHandler.cs`
- `backend/AIDT.Application/UseCases/StartWorkflowInstanceHandler.cs`
- `backend/AIDT.Workflow/WorkflowEngineAdapter.cs`
- `backend/AIDT.Api/Controllers/WorkflowRolesController.cs`

### Agent

- `backend/AIDT.Domain/Agents/*`
- `backend/AIDT.Infrastructure/Services/SqlAgentService.cs`
- `backend/AIDT.Worker/*`
- `backend/AIDT.AiGateway/*`
- `backend/AIDT.Api/Controllers/AgentsController.cs`

### 前端

- `frontend/src/pages/IamCenterPage.vue`
- `frontend/src/features/iam/components/IamNavigationPanel.vue`
- `frontend/src/features/iam/components/IamRolesPanel.vue`
- `frontend/src/features/iam/components/IamPeoplePanel.vue`
- `frontend/src/pages/ConfigurationCenterPage.vue`
- `frontend/src/pages/WorkflowPage.vue`
- `frontend/src/stores/auth.ts`
- `frontend/e2e/aidt.spec.ts`

### 测试

- `tests/AIDT.UnitTests/Api/IamNavigationWorkflowRoleApiTests.cs`
- SQL 集成测试项目
- Vue 组件测试
- 正式部署 Playwright 报告

## 🚨 已确认缺陷

1. `BusinessRole` 解析未将 department 范围限定到任务部门。
2. 项目经理范围管理仍存在 organization scope、全量替换和权限委派上限缺口。
3. 角色列表和停用检查可能漏算 legacy `UserRole`。
4. Agent 节点分配给服务身份，但现有真人处理链拒绝服务身份，存在死链。
5. 配置页仍要求手工输入 Agent Profile GUID。
6. 高风险审核缺少 Controller reject 路由和页面驳回。
7. 人员页面只加载默认 50 人，无服务端分页。
8. IAM 页面按钮未完全按动作权限隐藏/禁用。
9. 六类策略与转交资格测试覆盖不足。

## 🔐 硬边界

- 不移动、覆盖或删除历史冻结标签。
- 不覆盖 `req-navigation-iam-final1` 发布包。
- 不修改迁移 001–009。
- 如需结构变化，只新增 010。
- 不引入第二套身份、权限、流程、数据库或 UI 框架。
- 不连接真实外部系统或启用真实模型。
- 不以菜单隐藏代替 API 权限。
- 不以 Mock、前端状态或文档描述冒充已实现。

## 🧾 保持原状的旧问题

- 事件状态持久化。
- 项目状态持久化。
- 文件自动形成项目证据关系。
- 数字员工截止日期偏移。
- 已完成任务仍显示在“我的待办”。

## ✅ 完成判定

只有代码、迁移、API、前端、权限、审计、测试、部署、回滚、恢复、文档和新冻结标签全部有证据，才可判定本批完全收口。
