# AIDT 数据库资产

> 数据库：SQL Server 2025  
> ORM：EF Core 10.0.14  
> 当前批次状态：**DEPT_WF_001_COMPLETE_DEPLOYED_AND_FROZEN**
>
> 主 `AIDT` 已应用 018：18 迁移、71 张应用表；正式部署与恢复已验证

## 📁 目录

| 路径 | 内容 |
|---|---|
| `migrations/` | EF Core 迁移源码、幂等发布 SQL、回滚 SQL、迁移状态 |
| `repairs/` | 一次性、事务化、幂等的受控历史数据修护脚本 |
| `dba-review/` | DBA 审核检查点 |
| `seed/` | 演示种子数据脚本；本轮不执行 |
| `backup-notes/` | 备份与恢复说明，当前未执行恢复演练 |
| `schema/003-DEPARTMENT-FILES-ENVIRONMENT.md` | 部门、文件、版本和环境隔离数据模型 |
| `schema/007-BATCH3-PROJECT-INBOX.md` | 项目、成员、里程碑、事件收件箱和项目任务 |
| `schema/010-MCP-STANDARD-SERVER.md` | OAuth 客户端、授权码、Access/Refresh Token 和真人确认 |
| `schema/017-SCENARIO-CATALOG.md` | 岗位场景目录、不可变版本和真实业务链启动映射 |
| `schema/018-DEPARTMENT-WORKSPACE-DYNAMIC-FORMS.md` | 部门默认导航、测试隔离、模板元数据和任务冻结表单 |

## 🕘 历史 SQL 验证（不等于本轮）

- SQL Server 2025 Express `17.0.1000.7`，服务 `MSSQLSERVER` Running，1433 监听。
- 9 个应用 Schema、35 张应用表、2 条 EF 迁移、15 条 Elsa 迁移、11 张 Hangfire 表。
- 迁移测试库已验证 001→002→002 down→001 down：34→35→34→0 表。
- 部署前全量备份、`RESTORE VERIFYONLY` 与临时库恢复验证通过。
- 以上证据只覆盖 001/002 的历史运行。

## 🧾 执行规则

- 应用启动时自动迁移关闭。
- 测试部署由 `deploy/deploy.ps1` 在备份后执行。
- 正式生产执行前仍需 DBA 审核、维护窗口和正式证书/SPN。
- 种子脚本只包含 Demo 身份、样例对象和 Mock 连接器，不含真实客户、配方或成本数据。

## 🛠️ 阶段 B 执行顺序

1. 确认 `MSSQL*` 服务状态。
2. 确认 TCP `1433` 监听和实例版本。
3. 使用应用最小权限连接串进行连接测试。
4. 备份并记录回滚点。
5. 在测试库执行幂等升级脚本。
6. 校验 Schema、表、索引、约束和迁移历史。
7. 运行集成测试。
8. 验证应用回滚、数据库回滚与备份恢复。

禁止在应用启动时自动迁移，禁止手工跳过迁移直接修改表结构。

## 🧾 迁移清单

| 迁移 | 内容 | 阶段 A 状态 |
|---|---|---|
| `20261029170022_InitialSchema` | 初始 9 Schema、34 张应用表 | applied_verified |
| `20261030004100_WorkflowPersistenceAndSecurity` | 组织范围字段与幂等记录，应用表增至 35 | applied_verified |
| `20261030172128_003_DepartmentWorkspaceFilesAndEnvironment` | 部门、成员、文件关联、环境字段和回填，应用表增至 38 | applied_verified |
| `20261030174644_004_WorkflowTaskDepartmentBackfillCorrection` | 将历史任务责任部门校正为处理人主部门 | applied_verified |
| `20261031014600_005_Batch2MinutesSlaAgentGateway` | 听记、节点策略、SLA、通知和 Agent 网关，应用表增至 44 | applied_verified |
| `20261031044811_006_Batch2_1DocxPolicyAutoProxy` | 策略代理模式、自动代理幂等字段和默认低风险策略 | applied_verified |
| `20261031165645_Batch007ProjectInboxTasks` | `PMO` Schema、7 张新表、项目主对象扩展和批次 3 种子，应用表增至 51 | applied_verified |
| `20261031183454_Batch008ConfigurationCenter` | 流程配置草稿、版本、校验和发布，应用表保持 51 | applied_verified |
| `20261031195740_009_FrontendNavigationIamWorkflowRoles` | Navigation Schema、导航工作区/版本/入口/偏好及范围化角色分配，应用表增至 57 | applied_verified |
| `20261101122556_010_McpStandardServer` | `Mcp` Schema、5 张授权状态表、MCP 权限/服务身份/导航，应用表增至 62 | applied_verified |
| `20261103153650_018DepartmentWorkspaceDynamicForms` | 部门默认导航、测试标记、模板元数据、节点动态字段和任务冻结表单；应用表保持 71 | applied_verified |

## 🧭 DEPT-WF-001 数据变更

- 唯一新增迁移为 018；001–017 未修改。
- 导航与配置版本新增 `IsTestData`，测试导航退役但不删除历史版本和入口。
- `WorkflowDefinition` 增加部门默认模板、适用岗位、风险级别和测试标记。
- `WorkflowNodePolicy` 增加节点名称、业务说明、SLA、交付物、完成条件、
  动态字段 Schema、动作、AI 审查、通知和永久人工边界。
- `WorkflowTask` 增加冻结表单定义、内容哈希、Schema 版本、任务填写值和节点名称。
- JSON 列均有 `ISJSON` 约束；未建立新任务表单值表，也未复制业务对象正式字段。

### 018 隔离验证

- 还原库：`AIDT_DEPT_WF_001_MIGRATION_20261104_0100`。
- 基线：17 迁移、71 张应用表、33 个工作区、320 个配置版本。
- up：18 迁移、71 张应用表、7 个部门默认工作区。
- down：17 迁移、71 张应用表；交付中心部门和用户保留停用态。
- re-up：18 迁移、71 张应用表；7 个部门默认工作区、26 项部门流程负责人权限。
- 测试导航 32 个全部 `IsTestData=1 + Retired`；
  319 个测试配置已标记，测试配置发布中为 0；无效节点/任务 JSON 为 0。

### 主库应用

- 迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_DEPT_WF_001_PRE_018_20261104_0145.bak`
- SHA256：`0CE44730E6C5DDBEE529AEF1076E3658D85F3410504AF93E507744E69961391C`
- `RESTORE VERIFYONLY`：通过。
- 主库 `AIDT`：17/71 → 18/71。
- 7 个部门默认工作区、32 个测试导航退役、319 个测试配置标记、
  测试配置发布中 0、部门流程负责人 26 项权限、交付中心存在。
- 正式 E2E 后仍为 7 个部门默认工作区，发布中的测试默认入口为 0。

## 🧰 批次 3 收尾数据修护

脚本：`repairs/BATCH3_CLOSEOUT_DATA_REPAIR_20261101.sql`

执行前置条件：

1. 已完成全量备份并通过 `RESTORE VERIFYONLY`。
2. 已在隔离库执行前测、事务修护、修护后复测和幂等重跑。
3. 只在明确维护窗口对主库执行。

修护原则：

- 事件只从 `EventSuggestion.Status` 推导可证明状态。
- 项目只接受最新成功 `project.status_changed` 审计的 Before 与当前状态一致且 After 合法。
- PROJECT 附件只按现有 `Attachment.BusinessObjectId` 幂等补建 `SupportingEvidence`。
- 不修改或删除历史审计；有实际修护时只新增一条修护审计。
- 本脚本无 Schema 变更，不替代或修改迁移 001–009。

## 🧭 DEPT-WF-001 正式发布恢复

- 最终部署：`req-ui-step8-final3`。
- 最终数据库备份：
  `C:\ProgramData\AIDT\Backups\AIDT_DEPT_WF_001_FINAL3_FROZEN_20261104_0250.bak`
- SHA256：`9641532E40CF78CFB92676D2698090584C7775A57A4C303999419009242F8071`
- 独立恢复库：`AIDT_DEPT_WF_001_FINAL3_FROZEN_RESTORE_20261104_0250`。
- 恢复结果：ONLINE、18 迁移、71 应用表、7 个部门默认工作区、
  0 个发布中测试默认入口、26 项部门流程负责人权限、无效任务表单 0。
