# AIDT 功能地图

> 版本：V3.6（UX-006 已部署并冻结）
>
> 状态：随功能开发持续更新  
> 用途：AI 定位功能、选择上下文、分析变更影响和选择回归测试

## 📖 使用规则

1. 每个开发任务先找到 Feature ID。
2. AI 只读取该功能登记的相关页面、模块、API、表和测试。
3. 任何功能、接口、表、权限或测试变化必须更新本文件。
4. 未在功能地图登记的功能不得直接进入正式开发。
5. 模块 ID 和 Feature ID 一旦发布不得随意改名。

## 🧱 平台能力

| Feature ID | 功能 | 状态 | 前端 | 后端模块 | API | 数据表 | 测试 | 风险 |
|---|---|---|---|---|---|---|---|---|
| PLT-001 | 登录与身份 | active | `pages/LoginPage.vue`、`stores/auth` | Identity | `/api/v1/auth/*` | `Identity.User` | Cookie/CSRF/本地身份 API 测试 | 仅本机 Demo；不是生产 SSO |
| PLT-002 | 菜单与动作权限 | active | `stores/auth`、`router` | Identity | `/api/v1/permissions/me` | `Identity.RolePermission`、`DataScope` | 401/403、数据范围、Core 脱敏测试 | 正式 SSO/OIDC 仍待甲方身份源 |
| PLT-003 | 工作台指标 | active | `pages/DashboardPage.vue` | Platform/Reporting | `/api/v1/dashboard` | 跨模块只读查询 | API 与桌面/移动 E2E | SQL 持久化指标 |
| UX-001 | 员工优先的业务入口与事项发起 | active | `pages/MyWorkPage.vue`、`layouts/AppLayout.vue` | EmployeeWorkspace/Document/PMO | `/api/v1/my-work`、`/api/v1/inbox`、`/api/v1/files` | 部门、流程、任务、文件只读聚合；事件与附件创建 | Vue、API、桌面/390px E2E | 员工卡片不暴露 UUID、版本、节点代码和 TraceId |
| DEPT-WF-001 | 部门优先工作空间、可视化流程设计器与节点动态表单 | implemented / frozen | `layouts/AppLayout.vue`、`pages/ConfigurationCenterPage.vue`、`pages/WorkflowPage.vue`、`features/workflow/*` | Identity/Navigation/Configuration/Workflow/BusinessObject/Document/Minutes/Agent/Audit | 导航当前树；配置模板/生命周期/字段；任务表单草稿、AI 规则审查和动作联动 | 复用 `ConfigurationVersion`、`WorkflowDefinition/Version/NodePolicy/Task`、`Navigation.*`、`Audit`、`Outbox`，不建第二套真源 | 018 up/down/re-up；后端/SQL/API 186、MCP 11、Vitest 47、OpenAPI 187/208；源码 E2E 52+1 skip；正式 E2E 53/53 | Demo 身份与规则模拟；付款、质量放行、配方、设备、联锁、合同承诺和重大赔付永久人工 |
| UX-003 | 统一工作事项中心 | implemented / frozen | `pages/MyWorkPage.vue`、`features/work-items/components/UnifiedWorkItemCard.vue` | EmployeeWorkspace/Workflow/PMO/Minutes/Agent/MCP | `GET /api/v1/my-work` | 跨模块只读聚合；不新增业务状态表 | 统一契约、排序/去重单元测试、SQL/API、Vitest、正式 23/23 E2E | 聚合结果必须执行原动作权限与部门/项目/环境范围 |
| UX-004-A | 业务可编辑的事件人工确认 | implemented / frozen | `pages/InboxPage.vue`、`features/event-confirmation/confirmationRules.ts` | PMO/Workflow/Project | `POST /api/v1/inbox/{id}/actions/confirm-and-start-flow` | `PMO.EventInbox`、`EventSuggestion`、`ProjectTask`、`ProjectRelation`、`WorkflowInstance`、`WorkflowTask` | 可编辑项目/负责人/期限/优先级/内容、已有/新流程、影响预览、上海时区、原子回滚、143 项后端、20 项 Vitest、正式 26/26 E2E | 单次确认单事务；负责人须为项目有效期成员；永久人工边界不放宽 |
| UX-004-B | 统一人工确认中心 | implemented / frozen | `pages/ConfirmationCenterPage.vue`、`pages/McpConfirmationPage.vue`、`pages/MinutesPage.vue` | PMO/Workflow/Project/Minutes/Agent/MCP | `/confirmations`、`/minutes/confirmation-users`、`/mcp/confirmation-views/me` | `MinutesActionItem`、`MinutesTask`、`McpHumanConfirmation`、`AgentRun`、`ProjectRelation` | 四类来源、深链、听记编辑与确认、MCP 业务视图、意见必填、过期/拒绝/冲突、源码 28+1、正式 29/29、三端覆盖 | 不建第二套确认状态；MCP 技术字段折叠；Token 永不展示 |
| UX-005 | PMS 风格侧边栏与工作台正文重构 | implemented / frozen | `layouts/AppLayout.vue`、`pages/MyWorkPage.vue`、`pages/MinutesPage.vue`、`components/workspace/*` | Navigation/Identity/EmployeeWorkspace/Minutes；C2 仅新增听记只读筛选与汇总 | `GET /minutes` 筛选参数、`GET /minutes/summary`；My Work 复用现有契约 | 复用导航、事项和听记表；无迁移 | 后端/SQL/API 187；MCP 11；Vitest 30 文件/67；源码 E2E 63/63；正式 8088 E2E 64/64；Chrome/Edge 响应式与缩放矩阵；回滚和恢复通过 | API 仅为只读；写接口、事务、权限和审计未变；主库累积自动化数据时需使用冻结基线库做全量验收 |
| UX-006 | 全站主栏目页面介绍与具体业务示例 | implemented / frozen | `components/workspace/MainColumnPageHeader.vue`、`WorkbenchPageHeader.vue`、`WorkbenchIntroDrawer.vue`、`features/workbench-intro/*`、17 个一级栏目页 | 仅前端帮助内容；原业务模块和权限不变 | 无新增 API；审计页复用既有 `entityType/entityId` 查询参数 | 无迁移；复用现有业务只读查询 | 17 栏目目录与示例、抽屉三种关闭方式、焦点返回、权限和刷新/重登；Vitest 72/72、源码 E2E 68 通过 + 2 skip、正式 8088 E2E 70/70、后端/MCP 198/198、OpenAPI 188/209 | 静态帮助不写审计；Mock/未连接/规则模拟必须如实说明；生产 SSO 和真实外部系统仍未实现 |
| WS-001 | 部门工作区 | active | `pages/DepartmentPage.vue` | Identity/EmployeeWorkspace | `/api/v1/departments`、`/api/v1/departments/{id}/workspace` | `Identity.Department`、`Identity.UserDepartmentMembership` | Domain、SQL、API、桌面/390px E2E | 任务按处理人主部门归属，流程保持对象归属 |
| WS-002 | 可配置岗位工作台模板 | implemented / frozen | `pages/DepartmentPage.vue`、`features/iam/components/IamWorkbenchPanel.vue` | Workspace/EmployeeWorkspace/PMO/Workflow/Document/Minutes/Agent | `/api/v1/departments/{id}/workbench`、`/api/v1/workbench-templates*` | `Workspace.WorkbenchTemplate`、`WorkbenchTemplateVersion`、`WorkbenchCard`、`WorkbenchAssignment` | 6 模板、优先级、回退、草稿/发布/停用/恢复、无权限过滤、149 项后端/MCP、28 项 Vitest、正式 37/37 E2E | 模板只组合现有能力；任意 SQL/脚本/HTML/JS 禁止；无匹配回退通用工作区 |
| DOC-001 | 部门文件与附件中心 | active | `pages/DocumentsPage.vue` | Document/Infrastructure | `/api/v1/files`、版本、下载、预览、删除申请 | `Document.Attachment`、`DocumentVersion`、`AttachmentLink` | 类型/签名/大小/路径、上传下载预览、版本、敏感拒绝、E2E | 敏感未扫描文件禁止普通员工下载；Office 无内嵌预览 |
| DATA-001 | 演示与真实数据隔离 | active | 各业务页环境标签 | BusinessObject/Workflow/Document | `includeTestData`、`environmentCode` 查询参数 | 对象、流程、任务和附件环境字段 | 对象/流程/文件 SQL/API 过滤与管理员跨环境测试 | 跨环境开关仅平台管理员生效 |
| MIN-001 | AI 听记与行动项 | active | `pages/MinutesPage.vue` | Minutes/Agent/Document | `/api/v1/minutes*` | `Minutes.MinutesSource`、`MinutesActionItem`、`MinutesTask` | 上传/文本、人工确认、幂等、来源追溯、E2E | 手动导入；钉钉 Disconnected；真实模型未启用 |
| WF-005 | 节点能力包与 SLA | active | `pages/WorkflowPage.vue` | Workflow | `/workflow-versions/{id}/node-policies`、`/workflow-sla/actions/run` | `Workflow.WorkflowNodePolicy`、`WorkflowNotification` | 策略版本、SLA、重复扫描、WF-001 回归 | 会签/或签仍不在范围 |
| AGT-002 | 超时升级与受限代理 | active | `pages/WorkflowPage.vue`、`pages/AgentPage.vue` | Workflow/Agent | `/workflow-tasks/{id}/actions/agent-proxy`、`/agents/runs/{id}/actions/confirm-proxy` | `WorkflowTask`、`WorkflowNotification`、`AgentRun` | 提醒、升级、低风险代理、高风险拒绝 | 高风险永久人工；代理不冒充真人 |
| AGT-003 | 数字员工身份中心与能力治理 | implemented / frozen | `pages/AgentPage.vue`、`features/agents/*`、`ConfigurationCenterPage`、`WorkflowPoliciesPage`、`McpCenterPage` | Agent/Identity/Workflow/MCP/Audit | `/api/v1/agents`、`/options`、`/{id}/context`、`/manage`、生命周期/版本/回滚/审核；MCP 关联身份与令牌状态 | `Agent.AgentProfile`、`AgentProfileVersion`、`Identity.AccessChangeRequest`、`Identity.RolePermission`、`Mcp.Client/AccessToken` | 后端/SQL/API 172、MCP 11、Vitest 41、OpenAPI 162/183、正式 E2E 45/45；网页生命周期、双人审核、停用撤令牌、流程/MCP/AgentRun 联动、三档响应式 | 未连接真实外部模型或外部系统；高风险发布必须第二位平台管理员 |
| SCN-001-A | 岗位业务场景目录与场景启动后端 | implemented / frozen | 7B 页面待建设；本批仅 TypeScript 契约 | Scenario/BusinessObject/Workflow/PMO/Document/Minutes/Agent/Audit/Integration | `/api/v1/scenarios`、`/manage`、生命周期、版本、preflight/start/launch、高风险审核 | `Scenario.ScenarioDefinition`、`ScenarioVersion`、`ScenarioLaunch`；复用对象、流程、任务、项目、文件、听记、Agent、审计和 Outbox | 6 类基础场景真实启动、版本冻结、部门权限、引用失效、材料校验、幂等、失败回滚、高风险双人审核；后端/SQL/API 181、MCP 11、源码 E2E 44/44+1 skip、OpenAPI 179/200 | 7B 前端未建；外部系统未连接 |
| SCN-001-B | 岗位业务场景中心前端与试用闭环 | implemented / frozen | `pages/ScenariosPage.vue`、`features/scenarios/*`、`ConfigurationCenterPage`、`DepartmentPage`、`MyWorkPage`、`AuditPage` | Scenario/EmployeeWorkspace/Workbench/Identity/Audit | `/scenarios`；7A 场景目录/版本/预检/启动/审核；新增 `GET /api/v1/scenarios/launches` 当前用户真源聚合 | 仅复用 017 三张场景表；启动列表实时联查 Workflow、ProjectTask、EventInbox、AgentRun、BusinessObject、User，不复制状态 | 六场景可见；七步真实发起；刷新/重登持久；材料/听记/人工边界；配置生命周期；深链与工作台统一入口；1440/1280/390；Vitest 45、OpenAPI 180/201、源码 E2E 49 pass + 1部署专用 skip、正式部署 E2E 50/50 | Demo 环境；不连钉钉/U9/ERP/DCS/外部模型；AI 不自动确认、审批、付款、放行或关闭项目 |
| AIG-002 | AgentRun 与 AiGateway 关联 | active | `pages/MinutesPage.vue`、`pages/AgentPage.vue` | Agent/AiGateway | `/agents/runs/{id}/actions/execute`、`gateway-calls` | `Agent.AgentGatewayCall` | 成功、超时、未授权模型、ExternalModelCalled=false | RuleSimulator；真实模型未启用 |
| PMO-001 | 项目主对象与生命周期 | verified_test | `pages/ProjectsPage.vue`、`pages/ProjectDetailPage.vue` | BusinessObject/PMO | `/api/v1/projects`、`/projects/{id}`、`/actions/change-status` | `BusinessObject.BusinessObject`、`PMO.ProjectProfile`、`BusinessObjectVersion` | 创建、状态/版本刷新、未完成任务阻断、Completed 100%、部门范围、SQL/API/E2E | 正式 SSO、资源成本和项目组合不在本批 |
| PMO-002 | 项目成员、里程碑与工作台 | verified_test | `pages/ProjectsPage.vue`、`pages/ProjectDetailPage.vue` | PMO/Workflow/Document/Minutes | `/projects/{id}/members`、`/milestones`、`/relations` | `PMO.ProjectMember`、`ProjectMilestone`、`ProjectRelation` | 成员/负责人、逾期、任务、流程/SLA、PROJECT 上传自动证据、文件/听记和审计 | 普通成员不能管理成员或里程碑 |
| PMO-003 | 项目操作闭环 | implemented / frozen | `pages/ProjectDetailPage.vue`、`features/projects/projectOperationRules.ts` | PMO/Workflow/Document/Minutes/BusinessObject | `PUT /projects/{id}`、`/members`、`POST /projects/{id}/tasks`、`/relations`、`/actions/change-status` | `PMO.ProjectProfile`、`ProjectMember`、`ProjectTask`、`ProjectRelation`、`BusinessObjectVersion`、`AuditEvent` | 编辑/换负责人、成员角色/移除、直接建任务、四类证据、进度重算、合法状态流转、刷新一致；后端/MCP 146/146、Vitest 27/27、正式三端 32/32 | 不提供任意进度覆盖、任意状态跳转或第二套任务/证据模型 |
| EVT-001 | 企业事件收集收件箱 | verified_test | `pages/InboxPage.vue` | PMO/Agent/Document/Minutes | `/api/v1/inbox*` | `PMO.EventInbox`、`EventSuggestion` | 手动、TXT/MD/JSON/DOCX、听记/行动项/通知/附件、高风险边界、状态重新读取、上海时区 | 真实钉钉/邮件监听和音频转写未启用 |
| TASK-002 | 事件行动项转项目任务 | verified_test | `pages/InboxPage.vue`、`pages/ProjectDetailPage.vue` | PMO/Agent | `/inbox/{id}/actions/confirm`、`/project-tasks/{id}/trace` | `PMO.ProjectTask`、事件和关系表 | 真人确认、同事务持久化、幂等任务、共享截止时间、完成更新进度、任务反查来源 | Agent 不得自动派发任务或发起流程 |
| OBJ-001 / META-001 | 业务对象建模与字段编辑 | implemented / frozen | `pages/BusinessModelingPage.vue`、`pages/BusinessObjectPage.vue` | Metadata/Application/Infrastructure/Identity/Audit | `/object-types*`、`/business-objects/{id}/versions/context` | `Metadata.ObjectType`、`FieldDefinition`、`ObjectTypeVersion` | 网页字段设计、预览、校验、版本、发布/停用/恢复、对象版本/关联、流程配置与岗位卡片联动；后端/MCP 174/174、正式 E2E 41/41 | 建模仅平台管理员；不提供任意脚本/SQL/HTML/JS |
| OBJ-002 | 业务对象实例 | implemented / frozen | `pages/BusinessObjectPage.vue` | Domain/Application/Infrastructure | `POST/GET/PUT /api/v1/business-objects` | `BusinessObject.BusinessObject`、`BusinessObjectVersion` | 九类动态字段、创建/详情/编辑、版本、项目/流程/文件/审计关联、并发、Core 脱敏 | 删除字段不物理清理历史 JSON；非管理员受到字段脱敏 |
| WF-001 | 客户样品到转产 | active | `pages/WorkflowPage.vue` | Workflow/BusinessObject | `/api/v1/workflow-instances` | `Workflow.WorkflowInstance` | 5 节点全角色 E2E，Completed | 配方与真实客户数据不得进入 Demo |
| WF-002 | 批次异常到 CAPA | active | `pages/WorkflowPage.vue` | Workflow/BusinessObject | `/api/v1/workflow-instances` | `Workflow.WorkflowInstance` | Completed + Returned 实例 | DCS 仅 Mock，不替代安全联锁 |
| WF-003 | 请购到付款 | active | `pages/WorkflowPage.vue` | Workflow/BusinessObject/Integration | `/api/v1/workflow-instances` | `Workflow.WorkflowInstance` | Completed；金额条件与人工审批 | 禁止真实付款 |
| WF-004 | 客诉到 CAPA | active | `pages/WorkflowPage.vue` | Workflow/BusinessObject | `/api/v1/workflow-instances` | `Workflow.WorkflowInstance` | Terminated 场景 | 不连接真实客户系统 |
| TASK-001 | 我的待办 | active | `pages/WorkflowPage.vue` | Workflow | `/api/v1/workflow-tasks` | `Workflow.WorkflowTask` | 分配、重复冲突、版本、E2E | 会签/委托未纳入首个 Demo |
| AUD-001 | 审计与证据 | active | `pages/AuditPage.vue` | Audit | `/api/v1/audit-events` | `Audit.AuditEvent` | 与事务同写、时间线、E2E | 独立审计详情页返回 501 |
| AGT-001 | 数字员工运行 | active | `pages/AgentPage.vue` | Agent | `/api/v1/agents/*` | `Agent.AgentRun` | 限额、人工确认、停止、自确认拒绝 | 规则模拟，不是真实 AI |
| AIG-001 | AiGateway 受控接缝 | mock | 无独立业务页面 | AiGateway | `127.0.0.1:5100/v1/chat/completions` | 日志摘要 | 白名单、规则模拟、超时测试 | 不调用外部模型 |
| OUT-001 | Outbox 与后台任务 | active | 连接器状态页 | Worker/Integration | `/api/v1/connectors/{id}/outbox` | `Integration.OutboxEvent` | Delivered、重试、死信结构；API 查询 | 外部回执为 Mock |
| INT-001 | 外部连接器 | mock/disconnected | `pages/AgentPage.vue` | Integrations | `/api/v1/connectors` | `Integration.Connector` | Mock receipt 与未连接测试 | 不产生外部副作用 |
| CFG-001 | 业务流程配置版本与发布 | active | `pages/ConfigurationCenterPage.vue` | Configuration/Workflow | `/api/v1/configurations`、`/actions/validate`、`/actions/publish`、`/actions/rollback` | `Configuration.ConfigurationVersion`、`Workflow.WorkflowDefinition`、`Workflow.WorkflowVersion` | 草稿、Revision 并发、结构/处理人校验、发布、回滚和可执行 WorkflowVersion 联动；SQL/API 97/97；正式 E2E 13/13 | 高级可视化画布、会签/或签和外部连接器动作仍待扩展 |
| NAV-001 | 可配置组织栏目与工作区导航 | active | `IamCenterPage.vue`、`features/iam/IamNavigationPanel.vue`、`layouts/AppLayout.vue` | Identity/Navigation/Configuration | `/api/v1/navigation/current`、`/workspaces`、`/actions/publish`、`/actions/retire`、`/preferences` | `Navigation.NavigationWorkspace`、`NavigationVersion`、`NavigationItem`、`NavigationUserPreference` | 默认模板、发布版本、部门范围、停用恢复、个人偏好、按钮权限和桌面/390px E2E | 系统入口不可删除；隐藏菜单不是 API 权限 |
| IAM-001 | 组织、人员、角色和权限配置中心 | active | `IamCenterPage.vue`、`IamOrganizationPanel.vue`、`IamPeoplePanel.vue`、`IamRolesPanel.vue` | Identity/Audit/Workflow | `/api/v1/iam/*` | `Identity.Department/User/Role/UserRole/UserRoleAssignment/DataScope/AccessChangeRequest` | 组织树、服务端分页、范围分配、legacy/scoped 成员统计、委派上限、驳回、自提权阻断、双人审核、SQL/API 112/112 | 本地受控开通，不是邮件/钉钉邀请或生产 SSO |
| WFROLE-001 | 流程业务角色与节点级授权 | active | `ConfigurationCenterPage.vue`、`WorkflowPage.vue` | Workflow/Identity/Configuration | `/api/v1/workflow-roles/*`、`/workflow-tasks/{id}/actions/transfer`、`/actions/escalate` | `Workflow.WorkflowNodePolicy`、`WorkflowTask`、`WorkflowTaskCandidate`、`AgentRun`、`AgentGatewayCall` | 六类策略、部门候选收敛、转交资格、Agent 自动/人工确认/阻断、真实项目经理审批、桌面 E2E | Agent 使用 RuleSimulator；永久人工边界不放宽 |
| MCP-001 | AIDT 标准 MCP 服务 | active | `McpCenterPage.vue`、`McpConfirmationPage.vue` | `AIDT.Mcp`、Application、Infrastructure | `/mcp`、OAuth 2.1、`/api/v1/mcp/*` | `Mcp.Client`、`AuthorizationRequest`、`AuthorizationCode`、`AccessToken`、`HumanConfirmation` | SDK 2.3.0、HTTP/stdio、PKCE、令牌轮换/撤销、工具/Resources、真人确认、调用审计 | 远程 HTTPS 由反向代理终止 |
| DEP-001 | 离线部署 | verified_test | `deploy/README.md` | Deployment | Nginx `127.0.0.1:8088` | N/A | 发布包、SHA256、A→B→A 回滚、离线构建 | 测试环境，不是生产上线 |

## 🗄️ 模块地图

| 模块 ID | 后端项目/模块 | SQL Schema | 允许依赖 | 禁止依赖 | README |
|---|---|---|---|---|---|
| MOD-IDENTITY | `AIDT.Domain.Identity` / `AIDT.Infrastructure` | `Identity` | Core abstractions | Workflow 私有表 | `backend/AIDT.Domain/README.md` |
| MOD-METADATA | `AIDT.Domain.BusinessObjects` / `AIDT.Infrastructure` | `Metadata` | Core abstractions | 业务模块私有表 | `backend/AIDT.Domain/README.md` |
| MOD-OBJECT | `AIDT.Application` / `AIDT.Domain.BusinessObjects` | `BusinessObject` | Metadata contracts | Workflow 私有表 | `backend/AIDT.Application/README.md` |
| MOD-WORKFLOW | `AIDT.Workflow` / `AIDT.Domain.Workflows` | `Workflow` | BusinessObject contracts | 外部系统私有表 | `backend/AIDT.Workflow/README.md` |
| MOD-AUDIT | `AIDT.Domain.Audit` / `AIDT.Infrastructure` | `Audit` | Core contracts | 业务写模型 | `backend/AIDT.Domain/README.md` |
| MOD-INTEGRATION | `AIDT.Integrations` | `Integration` | Workflow commands、Adapter contracts | 直接写其他模块表 | `backend/AIDT.Integrations/README.md` |
| MOD-AGENT | `AIDT.Domain.Agents` / `AIDT.Infrastructure` | `Agent` | Workflow contracts | 直接控制外部设备 | `backend/AIDT.Domain/README.md` |
| MOD-CONFIG | `AIDT.Infrastructure` | `Configuration` | Metadata、Workflow contracts | 绕过发布审计 | `backend/AIDT.Infrastructure/README.md` |
| MOD-DEPARTMENT | `AIDT.Domain.Identity` / `AIDT.Infrastructure` | `Identity` | Core abstractions | 直接跨部门查询 | `backend/AIDT.Domain/README.md` |
| MOD-DOCUMENT | `AIDT.Domain.Documents` / `AIDT.Infrastructure` | `Document` | BusinessObject、Workflow 公共关联 ID | 返回物理存储路径、跨部门下载 | `backend/AIDT.Infrastructure/README.md` |
| MOD-MINUTES | `AIDT.Domain.Minutes` / `AIDT.Infrastructure` | `Minutes` | Document、Agent、Workflow public IDs | 复制业务对象主字段 | `backend/AIDT.Infrastructure/README.md` |
| MOD-PMO | `AIDT.Domain.Projects` / `AIDT.Infrastructure.Services.SqlProjectService` | `PMO` + `BusinessObject` | BusinessObject、Workflow、Minutes、Document public IDs | 复制源对象主字段、独立项目数据库 | `backend/AIDT.Infrastructure/README.md` |
| MOD-INBOX | `AIDT.Infrastructure.Services.SqlEventInboxService` | `PMO` + `Agent` | PMO、Agent、Minutes、Document public contracts | Agent 自动确认或高风险流程自动发起 | `backend/AIDT.Infrastructure/README.md` |
| MOD-MCP | `AIDT.Mcp` / `AIDT.Infrastructure.Services.SqlMcp*` | `Mcp` + `Audit` | Application services、Identity、Document、Workflow、PMO public contracts | 绕过业务权限、返回物理路径、任意 SQL/Shell/HTTP | `backend/AIDT.Mcp/README.md` |
| MOD-WORKBENCH | `AIDT.Infrastructure.Services.SqlWorkbenchService` | `Workspace` + 各业务只读表 | EmployeeWorkspace、Project public contracts、Identity、Audit | 复制业务数据、任意查询脚本、前端决定权限 | `backend/AIDT.Infrastructure/README.md` |
| MOD-SCENARIO | `AIDT.Domain.Scenarios` / `SqlScenarioService` / `ScenariosController` | `Scenario` + 各业务公共 ID | BusinessObject、Workflow、PMO、Document、Minutes、Agent public services | 复制业务字段、流程/任务状态、审计账本；直接写私有业务表 | `backend/AIDT.Application/README.md` |

## 🔗 核心调用链

### DEPT-WF-001 部门模板到冻结任务表单

```text
AppLayout / Navigation API
 → 公司公共入口 + 部门工作空间 + 平台治理
 → ConfigurationCenterPage
 → ConfigurationVersion
 → WorkflowDefinition / WorkflowVersion / WorkflowNodePolicy
 → WorkflowInstance + WorkflowTask（冻结 FormDefinitionJson/SchemaVersion/Hash）
 → WorkflowFormRenderer + WorkflowDynamicField
 → Application 表单校验
 → WorkflowTask 表单值 + TaskAction + Audit + Outbox
 → AgentRun / aidt-rule-sim（仅建议，ExternalModelCalled=false）
```

跨模块追溯继续使用业务对象、流程实例、任务、附件、听记、AgentRun 和审计 ID；
不新增部门业务表，不复制对象字段。

### WF-001 客户样品到转产

```text
Vue WorkflowPage
 → workflowApi
 → WorkflowInstanceController
 → CreateWorkflowInstance
 → BusinessObject 查询/更新
 → Elsa Workflow
 → Audit
 → 回归测试
```

### WF-002 批次异常到 CAPA

```text
Mock DCS Alarm / Adapter
 → Workflow Event
 → BusinessObject.ProductionBatch
 → QualityDeviation
 → Elsa Workflow
 → Task/Audit
```

### WF-003 请购到付款

```text
Vue Purchase Workbench
 → Workflow Task
 → Approval Rules
 → Integration Outbox
 → U9/ERP Mock Adapter
 → Receipt/Reconciliation
```

### SCN-001-A 场景受控启动

```text
Vue 7B（待建设）/ API Contract
 → ScenariosController
 → SqlScenarioService
 → 场景版本冻结引用校验
 → 现有 BusinessObject / Workflow / EventInbox / ProjectTask / File / Minutes / Agent 服务
 → Scenario.ScenarioLaunch
 → Audit + Outbox
```

## 🧪 回归矩阵

| 修改范围 | 必须测试 |
|---|---|
| 流程定义 | 发起、条件、退回、终止、历史版本 |
| 业务对象 | 创建、版本、权限、关联流程 |
| API | 契约、错误码、权限、幂等 |
| 数据库迁移 | 新库、升级库、回滚、恢复 |
| 前端页面 | loading、empty、success、error、移动端 |
| 权限 | 未登录、无权限、数据范围、字段脱敏 |
| Agent | 范围、确认、停止、审计 |
| 部署 | 离线构建、健康检查、回滚 |

## 🕘 全阶段 Demo 历史调用链状态

- 历史运行曾连接本机 SQL Server；当前阶段 A 默认 `Database.IsReady=false`，003 未执行。
- 本地演示身份、Cookie、CSRF、RBAC、组织范围和 Core 字段脱敏已通过真实 API 测试。
- Elsa 3.9 SQL 运行时、Hangfire、Outbox Worker 和 Garnet 短期锁已运行。
- 三条主流程 Completed；另有一条 WF-002 Returned 和一条 WF-004 Terminated。
- 前端真实调用 API，桌面和 390px 移动端 Playwright E2E 已通过。
- 发布包、Nginx 唯一入口、备份恢复、迁移 down 和版本回滚已在测试环境验证。
- AiGateway 使用 `RuleSimulator`，明确 `ExternalModelCalled=false`。

## ✅ 需求池批次 1 状态

- `UX-001`、`WS-001`、`DOC-001`、`DATA-001` 已完成主库迁移、SQL 集成、权限、文件安全和浏览器 E2E。
- 迁移 003 建表和回填，迁移 004 校正任务责任部门。
- 文件上传、下载、预览、版本、SHA256、敏感文件门禁、跨部门拒绝和审计已验证。
- 整个 AIDT 需求池仍未完成；本状态只表示批次 1 完成。

## ✅ 需求池批次 2 状态

- `MIN-001`、`WF-005`、`AGT-002`、`AIG-002` 已完成 Demo 纵向切片。
- 听记分析必须经真人确认；行动项确认幂等并保留原文、发言人、时间戳。
- 节点策略随流程版本冻结，Worker 提醒/升级幂等。
- Agent 使用独立服务身份，低风险代理需真人确认；永久人工边界禁止代理。
- AiGateway 仍为 RuleSimulator，`ExternalModelCalled=false`。
- 真实钉钉与外部模型未完成，整个 AIDT 需求池未完成。

## 📝 变更记录

| 日期 | Feature ID | 变更 | 修改人 |
|---|---|---|---|
| 2026-10-30 | ALL | 创建首期功能地图 | Codex |
| 2026-10-30 | ALL | 建立阶段 A 后端、前端、契约、迁移和测试骨架；登记 SQL 阻断 | Codex |
| 2026-10-30 | ALL | 阶段 A 重验证：默认失败关闭、SQL 测试显式跳过、补齐 OpenAPI 和前端契约状态 | Codex |
| 2026-10-30 | ALL | 全阶段 Demo：真实 SQL、Elsa、Hangfire/Outbox/Garnet、权限脱敏、E2E、AiGateway、部署恢复 | Codex |
| 2026-10-31 | UX-001/WS-001/DOC-001/DATA-001 | 阶段 A：员工入口、部门、文件安全、环境过滤、迁移源文件和前端页面；SQL 验证待阶段 B | Codex |
| 2026-10-31 | UX-001/WS-001/DOC-001/DATA-001 | 批次 1 完成：主库 003/004、64 项 SQL/API 测试、5 项桌面/移动 E2E、部署回滚和离线构建 | Codex |
| 2026-10-31 | MIN-001/WF-005/AGT-002/AIG-002 | 批次 2 实现：005、听记确认、SLA、受限代理、Gateway 调用链 | Codex |
| 2026-11-01 | PMO-001/PMO-002/EVT-001/TASK-002 | 批次 3 实现：007、项目主对象/成员/里程碑、事件收件箱、真人确认任务和证据链 | Codex |
| 2026-11-01 | NAV-001/IAM-001/WFROLE-001 | 导航与 IAM 权限批次：009、组织树、范围化角色、导航版本、节点处理人快照和真实项目经理审批 | Codex |
| 2026-11-01 | NAV-001/IAM-001/WFROLE-001 | 收尾：部门收敛、IAM 范围/委派、legacy 统计、Agent 闭环、分页、驳回、动作权限和 112 项回归 | Codex |
| 2026-11-01 | PMO-001/PMO-002/EVT-001/TASK-002 | 批次 3 收尾：状态持久化、项目自动文件证据、上海时区和历史数据修护；115 项后端与 18 项 E2E | Codex |
| 2026-11-01 | MCP-001 | 官方 .NET MCP SDK、OAuth 2.1/PKCE、HTTP/stdio、受控工具/Resources、真人确认和调用审计 | Codex |
| 2026-11-02 | UX-003 | 统一工作事项中心：六类待处理来源只读聚合、事件/任务去重、优先级排序、详情深链和桌面/1280/390px E2E | Codex |
| 2026-11-02 | UX-004-A | 事件人工确认升级：可编辑业务上下文、已有/新流程选择、影响预览、单事务编排与失败回滚 | Codex |
| 2026-11-02 | UX-004 | 统一人工确认中心完成、部署并冻结：四类来源聚合、可编辑听记上下文、MCP 业务语言、迁移 011、正式 29/29、回滚与独立恢复 | Codex |
| 2026-11-02 | PMO-003 | 项目详情补齐编辑、换负责人、成员管理、直接建任务、文件/听记/流程/对象证据、进度重算与状态流转；无新迁移 | Codex |
| 2026-11-02 | WS-002 | 岗位工作台模板：Workspace 4 表、6 个系统模板、服务端匹配/卡片聚合、IAM 配置、源码 149 项后端/MCP 与 28 项 Vitest | Codex |
| 2026-11-02 | META-001-A | 对象类型生命周期、九类字段、不可变版本、建模权限、业务对象安全编辑、013 与新增 E2E | Codex |
| 2026-11-03 | META-001-B | 业务建模工作室、动态表单、可搜索人员/部门、对象版本与四类关联证据、流程配置/岗位卡片联动、014 更新授权 | Codex |
| 2026-11-03 | AGT-003 | 数字员工不可变版本、草稿到回滚生命周期、部门负责人边界、高风险双人审核、MCP 服务令牌关联与停用撤销、015 迁移 | Codex |
| 2026-11-03 | AGT-003 / 6B | `/agents` 身份中心、七段表单、运行/流程/MCP/审计证据、流程与策略业务选择器、三档响应式、016 只读授权 | Codex |
| 2026-11-03 | SCN-001 / 7B | `/scenarios` 首页、场景详情、七步发起向导、我的场景真源聚合、配置治理、岗位工作台/统一入口、1440/1280/390 与全量回归 | Codex |
| 2026-11-04 | UX-005 | 扩展登记 PMS 风格侧边栏与工作台正文重构：A 外壳/侧栏，B 统一页面头/统计/筛选/介绍抽屉，C 首先迁移我的工作与 AI 听记；默认无 API/数据库变更，待实施 | Codex |
| 2026-11-04 | UX-005 | A/B/C1/C2 实现：固定应用壳、浅色独立滚动侧栏和窄屏抽屉；共享页面头/介绍/统计/筛选；我的工作全宽筛选列表；听记服务端筛选与只读汇总、固定高度双栏；无迁移 | Codex |
| 2026-11-05 | UX-005 | 正式发布：`59f406d` 修复抽屉加载竞态，`7f03623` 合并，final2 部署 8088；正式 E2E 64/64，final2→final1→final2 回滚、最终备份与独立恢复通过；冻结 `req-ui-ux005-frozen-v1` | Codex |
| 2026-11-05 | UX-006 | 全站 17 个一级栏目统一页面头、集中介绍目录和唯一抽屉；补齐具体业务示例、权限与人工边界、响应式及可访问性；修复审计页只过滤最近 100 条的问题；源码 68 通过、2 个部署专用 skip | Codex |
| 2026-11-05 | UX-006 | 正式发布：`2d9ba09` 合并，`req-ui-ux006-final1` 部署 8088；正式 E2E 70/70，应用回滚 `ux006→ux005-final3→ux006`、最终备份与独立恢复通过；冻结 `req-ui-ux006-frozen-v1` | Codex |

## ✅ 批次 2.1 可用性补完

- `MIN-001`：`.docx/.txt/.md/.json` 导入；DOCX 读取正文段落，PDF 不解析。
- `WF-005`：`/workflow-policies` 支持版本/节点选择、草稿、发布、影响范围和版本状态。
- `AGT-002`：节点支持 `RemindOnly`、`HumanConfirmedProxy`、`PreAuthorizedAutoProxy`；
  自动代理使用独立服务身份和唯一幂等键。
- `AIG-002`：继续使用 AiGateway RuleSimulator，外部模型未启用。
- 默认流程：`RND_MATERIALS_CONFIRM`；默认节点：`MATERIALS_CONFIRM`。
- 永久人工边界包括付款、质量放行、配方、设备控制、安全联锁、合同和重大赔付。

## ✅ 需求池批次 3 业务 Demo

- `PMO-001`：`PROJECT` 是唯一业务主对象，公共标题/状态/创建人/部门/环境/版本在
  `BusinessObject.BusinessObject`，专属字段在 `PMO.ProjectProfile`。
- `PMO-002`：项目成员、主负责人、里程碑、逾期、项目进度、跨部门任务、关联流程/SLA、
  文件/听记/审计证据时间线已实现。
- `EVT-001`：支持手动事项、TXT/MD/JSON/DOCX、听记、行动项、流程通知和附件来源。
- `TASK-002`：Agent RuleSimulator 只生成建议；真人确认后幂等创建 `PMO.ProjectTask`，
  任务可反查事件、听记、文件、流程和审计。
- 高风险关键词命中付款、质量放行、配方、设备控制、安全联锁、合同或重大赔付时，
  只形成高风险待确认建议，不创建任务或流程。
- 真实钉钉/邮件监听、音频转写、真实大模型和正式 SSO 仍未接入。

## ✅ 需求池批次 3 收尾

- `EVT-001`：Analyze/Confirm 使用受跟踪事件，事件与 suggestion 同事务保存；刷新后
  `AwaitingConfirmation/Confirmed` 保持，已确认事件不再显示确认按钮。
- `PMO-001`：状态管理动作更新受跟踪主对象，同时递增 Version 并写版本快照；
  `Active → Blocked → Active → AwaitingAcceptance → Completed` 重新读取保持。
- `PMO-002`：PROJECT 文件上传在同一事务自动建立 `SupportingEvidence` 关系；
  项目详情刷新可见，重复关系为 0，非 PROJECT 上传行为不变。
- `TASK-002`：日期原文 `2025-08-15` 统一解释为 `2025-08-15 18:00 +08:00`；
  事件建议、确认任务和页面显示共享同一截止时间。
- 本批无新迁移，001–009 未修改；历史修护使用事务化、幂等、可审计 SQL。
- 后端/SQL/API：115/115；前端 Vitest 13/13；OpenAPI 114/134；源码 E2E 18/18。
- 交付报告：`docs/project/REQUIREMENT_POOL_BATCH_3_CLOSEOUT_DELIVERY.md`。

## ✅ 前端导航与 IAM 权限批次

- `NAV-001`：导航工作区、版本、默认模板、权限过滤、部门范围、停用恢复和个人偏好已持久化。
- `IAM-001`：组织树、人员邀请/开通、主兼职部门、数据范围、角色 CRUD、权限矩阵、范围分配、自提权和双人审核已接入。
- `WFROLE-001`：六类节点处理人策略在任务创建时冻结，审批同时校验动作权限、当前处理人、冻结资格和部门范围。
- 冻结 P0 已修复：`demo.pm` 可直接审批分配给自己的经理节点，不再依赖平台管理员兼岗。
- 迁移 009 已在隔离库完成 up/down/re-up，并应用到主 `AIDT`。
- 最终隔离 SQL/API：104/104。
- 源码桌面/390px Playwright：15/15。
- 正式部署、回滚、恢复和新冻结标签以交付报告为准。

## ✅ MCP-001 标准 MCP 服务

- `AIDT.Mcp` 使用官方 `ModelContextProtocol.AspNetCore` / `ModelContextProtocol` 2.3.0。
- 同一可执行文件支持 Streamable HTTP `/mcp` 与 `--stdio`。
- 浏览器授权使用 OAuth 2.1 授权码 + PKCE S256、动态客户端注册、Access/Refresh Token 轮换与家族撤销。
- 工具按业务语义设计，复用现有 Application Service，不暴露 SQL、Shell、任意文件系统或任意 HTTP。
- 审批、事件确认、流程转交、项目最终状态和敏感文件正文存在一次性真人确认边界。
- 每次工具调用和资源读取写 `Audit.AuditEvent`，可按客户端、用户、工具、资源和 TraceId 反查。

## ✅ UX-003 统一工作事项中心

- `MyWorkDto` 改为统一 `WorkItemViewDto` 投影，聚合 `WorkflowTask`、`ProjectTask`、
  `EventInbox`、`MinutesActionItem`、`McpHumanConfirmation` 和 `AgentRun`。
- 聚合为只读查询，不新增业务状态表，不新增数据库迁移。
- 排序固定为永久人工边界、已逾期、今天到期、等待确认、本周和其他。
- 事件已生成项目任务时事件让位；用户视图内关联流程任务的项目任务让位给流程待办。
- `MyWorkPage.vue` 提供今日焦点、待我处理、我发起、我参与、需要关注和最近来源证据。
- 详情深链分别指向流程、项目、事件确认、听记、MCP 确认和 Agent。
- 员工卡片不渲染 UUID、TraceId、原始节点代码、数据库字段名和内部 revision。
- 权限仍按来源动作、组织/部门/项目/环境范围在后端执行。

## ✅ UX-004-A 业务可编辑的事件人工确认

- `InboxPage.vue` 支持项目、负责人、截止、优先级、内容和任务开关，并可选择不启动流程、
  关联已有流程或发起新流程。
- 确认前展示影响预览和不会执行的高风险动作；缺负责人或截止时间无法最终确认。
- `ConfirmInboxEventAndStartWorkflowHandler` 在同一事务内编排流程、事件、任务、关系和审计。
- 事件状态条件抢占、项目任务唯一约束和幂等键共同防止重复派发。
- 正式部署 `req-ui-step2a-final1`，冻结标签 `req-ui-step2a-frozen-v1`，
  正式桌面/1280/390px E2E 26/26。

## ✅ UX-004-B 统一人工确认中心

- `/confirmations` 聚合事件、听记行动项、MCP 确认和 AgentRun，支持四类 `type + id` 深链。
- “我的工作”的四类待确认入口进入统一页；原详情页仍可直达。
- 听记行动项可编辑负责人、截止、优先级、项目、业务对象、流程版本、已有流程和内容。
- 信息不完整可保存为“待补充”，缺负责人或截止时服务端禁止确认建任务。
- 迁移 011 增加听记行动项/任务上下文列并注入系统导航项，不新增表。
- MCP 页面展示实际业务动作、资源/项目、将执行与不会执行内容；技术字段折叠，
  通过和拒绝均要求意见，Token 不返回、不展示。
- Agent/永久人工边界不自动确认。
- 合并提交 `931f953`，正式发布 `req-ui-step2-final1`，正式 E2E 29/29。
- 回滚 `final1 → req-ui-step2a-final1 → final1` 通过；独立恢复库为 11 条迁移、62 张表。
- 冻结标签 `req-ui-step2-frozen-v1`。

## ✅ PMO-003 项目操作闭环

- `ProjectDetailPage` 可按权限编辑项目、名称、目标、阶段、计划日期、保密等级和负责人。
- 新负责人成为唯一 Lead，原 Lead 转为 Manager；成员可新增、改角色和移除非负责人。
- `POST /api/v1/projects/{id}/tasks` 直接创建 `PMO.ProjectTask`，负责人必须是有效项目成员，
  截止时间按上海业务时区保存，并使用幂等键防止重复提交。
- 项目关系可选择文件、听记、流程实例和业务对象，证据摘要与关系在同一事务持久化。
- 项目进度继续由未取消任务与里程碑自动计算，前端不提供任意百分比覆盖。
- 状态选择器只展示领域允许的下一状态；完成项目仍阻断未完成任务。
- 索引：`pages/ProjectDetailPage.vue`、`features/projects/projectOperationRules.ts`、
  `ProjectsController`、`SqlProjectService`、`PMO.ProjectTask/ProjectRelation`、
  `ApiContractTests.ProjectOperations_EditMembersCreateTasksLinkEvidenceAndPersist`。
- 无新迁移；数据库保持 11 条迁移、62 张应用表。
