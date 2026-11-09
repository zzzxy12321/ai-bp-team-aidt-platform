# AIDT.Infrastructure

## 📌 职责

- EF Core SQL Server 模型与迁移
- 运行时配置和启动能力状态
- 未连接状态下的显式 NotReady 仓储
- 系统时钟和平台能力查询

## 🗄️ 数据库状态

- Provider：`Microsoft.EntityFrameworkCore.SqlServer`
- Migration：001–018（主库已应用 018）
- 作者态应用表：71
- Schema：Identity、Metadata、BusinessObject、Workflow、Audit、Integration、Agent、Document、Configuration、Minutes、PMO、Navigation、Workspace、Scenario
- 018 状态：`applied_verified`

应用不在启动时自动迁移。010 已先在隔离库完成 up/down/re-up，再应用到主 `AIDT`。

## ✅ 批次 1 验证

- 003 up/down/re-up 和 004 up 已通过
- 部门/环境过滤和文件 API 集成测试通过
- 文件上传、下载、预览、版本、敏感文件拒绝和越权审计通过
- SQL Server 集成、备份恢复和端到端持久化通过
- 生产 DBA 复核和正式连接证书仍待甲方环境

## 🧭 导航与 IAM

- `SqlIdentityConfigurationService`：组织树、人员、角色、权限、数据范围和双人审核。
- `SqlNavigationService`：导航版本、发布、停用恢复和用户偏好。
- `SqlNavigationService` 当前导航按公司公共入口、部门、平台治理分层，过滤无效测试导航，
  并对路由/资源/动作执行白名单校验。
- `SqlWorkflowConfigurationService` 管理部门模板、默认模板、生命周期、节点字段、
  部门流程负责人权限和高风险平台审核。
- `SqlWorkflowTaskFormService` / `SqlWorkflowTaskAiReviewService` 处理冻结表单草稿、
  Schema/角色/范围/附件校验与规则模拟审查。
- `SqlWorkflowRoleService`：流程角色解析、部门范围收敛和任务候选快照。
- `SqlWorkflowTaskAssignmentService`：转交/升级目标部门资格和业务角色冻结候选校验。
- `SqlBatch2Service`：Agent 节点 RuleSimulator 执行、人工确认、永久人工边界阻断及 Gateway 证据。
- 角色有效成员数合并 legacy `UserRole` 与 scoped `UserRoleAssignment`。

## 🧪 收尾验证

- 隔离库全量后端/SQL/API：112/112，0 跳过。
- 009 down/re-up：8 迁移/51 表 → 9 迁移/57 表。
- 本轮不需要迁移 010，未修改 001–009。

## 🧭 UX-003 统一工作事项聚合

- `SqlEmployeeWorkspaceQueryService` 以只读查询聚合流程任务、项目任务、待确认事件、
  听记行动项、MCP 真人确认和 Agent 待确认运行。
- 每类来源先执行原动作权限与组织/部门/项目/环境范围，再进入展示投影。
- 事件已有项目任务时不再显示事件；项目任务关联的流程待办已在当前用户视图出现时，
  项目任务让位。
- 本批不新增迁移，数据库仍为 010 / 62 张应用表。

## 🤖 AGT-003-A 数字员工身份治理

- `SqlAgentGovernanceService` 实现身份草稿/校验/发布/停用/恢复/历史/回滚和双人审核。
- `Agent.AgentProfileVersion` 保存不可变能力快照；`AgentProfile` 保存当前有效快照与指针。
- 停用事务同时撤销关联 MCP 服务令牌并写 Audit/Outbox，历史运行不改写。
- `SqlSecurityAuditService` 使用独立 DI 作用域记录 403 拒绝，避免污染回滚中的业务事务。
- `SqlScenarioService` 管理场景目录、不可变版本、部门边界、发布/停用/恢复/回滚和受控启动；
  启动只生成真实业务链映射，不复制业务状态。
- `SqlMcpOAuthService` 要求服务令牌关联已发布有效数字员工，并按能力白名单过滤工具与 Resources。
- 迁移 015 后数据库为 15 条 EF 迁移、68 张应用表。
- 迁移 017 新增 `Scenario` Schema 3 张表；应用表 71 张，六类基础场景和 5 项场景权限已验证。
- 迁移 018 复用导航、配置、流程和任务表；纯 017 还原库完成
  17/71 → 18/71 → 17/71 → 18/71，应用表保持 71。
- 正式 E2E 后测试导航不会替换部门正式默认工作区；
  如历史验收留下错误默认位，可执行
  `database/repairs/DEPT_WF_001_NAV_TEST_DEFAULT_REPAIR_20261104.sql`。

## ✅ UX-004-A 事件确认与流程发起

- `SqlEventInboxService` 在事件所属部门内校验项目、负责人、期限和可选流程。
- 事件状态使用条件更新抢占；确认、项目任务、项目关系、审计和幂等在同一事务内完成。
- `StartWorkflowInstanceHandler` 提供事务内暂存入口，新流程失败时连同事件确认整体回滚。
- 无新增迁移，数据库仍为 010 / 62 张应用表。

## ✅ UX-004-B 统一人工确认中心

- 听记行动项和任务保存项目、业务对象与已有流程上下文，确认和项目关系同事务写入。
- 听记负责人候选限定为来源部门有效真人，不返回服务身份。
- MCP 自有确认增加业务视图，技术标识折叠，通过/拒绝意见在服务端必填。
- 迁移 011 增加上下文列和统一确认系统导航；表数保持 62。

## ✅ PMO-003 项目操作闭环

- 项目编辑复用 `BusinessObject + PMO.ProjectProfile`，负责人变更在同一操作中收敛唯一 Lead。
- `CreateTaskAsync` 复用 `PMO.ProjectTask`，校验有效项目成员、上海时区期限、优先级和幂等键。
- 文件、听记、流程实例和业务对象继续通过 `PMO.ProjectRelation` 关联，不复制业务内容。
- 任务创建、完成和里程碑变化继续统一重算项目进度。
- 无新增迁移，数据库保持 11 条迁移、62 张应用表。

## ✅ WS-002 岗位工作台模板

- `SqlWorkbenchService` 负责已发布模板匹配、用户/角色/部门/组织优先级、卡片权限过滤和回退。
- `Workspace` Schema 只保存版本、卡片和匹配配置，不复制业务对象、项目、流程或文件数据。
- 六类系统模板共 31 张卡片，全部引用现有服务或明确显示尚未接入。
- 卡片查询再次执行原业务权限、组织/部门/项目/环境范围和永久人工边界。
- 迁移 012 在隔离库完成 up/down/re-up；模板权限随 down 清理、re-up 恢复。
