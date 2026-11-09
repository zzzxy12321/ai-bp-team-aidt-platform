# 数据库迁移

当前代码最大迁移：`018_DepartmentWorkspaceDynamicForms`。
主库当前已应用 018；独立还原库 up/down/re-up、主库应用、正式部署和最终恢复均已验证。
001–017 为冻结历史，不得修改。

# InitialSchema 迁移

## 📦 文件

- EF Core：`backend/AIDT.Infrastructure/Persistence/Migrations/`
- 幂等升级：`001_InitialSchema.up.sql`
- 回滚：`001_InitialSchema.down.sql`
- 增量升级：`002_WorkflowPersistenceAndSecurity.up.sql`
- 增量回滚：`002_WorkflowPersistenceAndSecurity.down.sql`
- 部门、文件与环境隔离升级：`003_DepartmentWorkspaceFilesAndEnvironment.up.sql`
- 部门、文件与环境隔离回滚：`003_DepartmentWorkspaceFilesAndEnvironment.down.sql`
- 任务责任部门校正：`004_WorkflowTaskDepartmentBackfillCorrection.up.sql`
- 任务责任部门校正回滚：`004_WorkflowTaskDepartmentBackfillCorrection.down.sql`
- 批次 2 听记、SLA、代理与网关升级：`005_Batch2MinutesSlaAgentGateway.up.sql`
- 批次 2 回滚：`005_Batch2MinutesSlaAgentGateway.down.sql`
- 批次 2.1 策略与自动代理升级：`006_Batch2_1DocxPolicyAutoProxy.up.sql`
- 批次 2.1 回滚：`006_Batch2_1DocxPolicyAutoProxy.down.sql`
- 批次 3 项目、收件箱与任务升级：`007_Batch3ProjectInboxTasks.up.sql`
- 批次 3 回滚：`007_Batch3ProjectInboxTasks.down.sql`
- 配置中心与可执行流程发布：`008_ConfigurationCenter.up.sql`
- 配置中心回滚：`008_ConfigurationCenter.down.sql`
- 导航、IAM 与流程角色升级：`009_FrontendNavigationIamWorkflowRoles.up.sql`
- 导航、IAM 与流程角色回滚：`009_FrontendNavigationIamWorkflowRoles.down.sql`
- MCP 标准服务升级：`010_McpStandardServer.up.sql`
- MCP 标准服务回滚：`010_McpStandardServer.down.sql`
- 岗位工作台模板升级：`012_WS002RoleWorkbenchTemplates.up.sql`
- 岗位工作台模板回滚：`012_WS002RoleWorkbenchTemplates.down.sql`
- 岗位场景、不可变版本与启动映射升级：`017_ScenarioCatalogBackend.up.sql`
- 岗位场景回滚：`017_ScenarioCatalogBackend.down.sql`
- 部门工作空间与动态表单升级：`018_DepartmentWorkspaceDynamicForms.up.sql`
- 部门工作空间与动态表单回滚：`018_DepartmentWorkspaceDynamicForms.down.sql`
- 静态状态：`migration-state.json`

## 📊 结构规模

- 应用 Schema：15
- 作者态迁移应用表：71
- 幂等脚本另含：`__EFMigrationsHistory`
- 业务对象、流程、任务、审计、Outbox、Agent、文档和配置均按模块独立 Schema。

## ✅ 本轮已完成

- 从 EF Core 模型生成迁移代码。
- 从迁移代码静态生成 SQL Server 升级和回滚脚本。
- 检查设计包含 9 个 Schema 和 35 张应用表。
- 001、002 的执行记录是 2026-10-30 的历史证据。
- 003 已编写 EF Core Migration、幂等升级 SQL、回滚 SQL 和 DBA 审核记录。
- 003 已在隔离测试库完成 up/down/re-up，并成功应用到主 `AIDT`。
- 004 将历史任务责任部门回填为处理人主部门，支持跨部门流程。
- 005 增加听记、SLA、节点策略和 Agent 网关记录。
- 006 增加代理模式、自动代理幂等字段和默认低风险节点策略。
- 007 增加 PMO 主对象扩展、成员、里程碑、关系、收件箱、建议和项目任务。
- 008 扩展配置版本元数据、Revision 并发、权限，并将发布动作与可执行 WorkflowVersion 绑定。
- 009 增加组织树、范围化角色分配、高风险授权、导航版本/偏好和流程任务处理人快照。
- 009 补齐 `project-manager` 的流程任务审批、退回和终止权限。
- 010 增加 `Mcp` Schema、OAuth 客户端/授权码/令牌/真人确认、相关权限和导航入口。
- 012 增加 `Workspace` Schema、4 张模板表、6 个系统模板、31 张卡片和 3 项模板权限。
- 017 增加 `Scenario` Schema 3 张表、6 个基础场景、5 项场景权限、
  财务付款人工复核流程，以及 AttachmentLink 和 AccessChangeRequest 约束扩展。
- 018 复用导航、配置和流程表，增加测试隔离、部门默认模板、节点动态字段、
  任务冻结表单、表单值、约束与索引。

## ⚠️ 环境说明

- 003 已执行；004 为不可逆语义数据校正，down 只移除迁移记录，不回写旧错误部门。
- 正式生产迁移仍需 DBA 审核和维护窗口。

## ✅ 本批执行结果

- 迁移前备份和 `RESTORE VERIFYONLY` 通过。
- 隔离测试库完成 003 up/down/re-up 和 004 up。
- 主库完成 003 up 和 004 up。
- 应用表 38，任务责任部门与处理人主部门不一致数量为 0。
- 009 在隔离库完成 up/down/re-up，主库已应用，应用表 57。
- 010 在隔离库完成 up/down/re-up：62→57→62 表、10→9→10 迁移，主库已应用。
- 012 在隔离库完成 up/down/re-up：11→12→11→12 迁移；主库已应用。
- 013 在隔离库完成 up/down/re-up：12/66→13/67→12/66→13/67 迁移；主库已应用。
- 014 仅授权既有业务角色更新业务对象；隔离库 13→14→13→14，主库已应用。
- 017 在最终隔离库完成 16→17→16→17；68→71→68→71 张应用表，
  场景版本引用快照和 JSON 约束全部验证通过。
- 018 在纯 017 还原库完成 17→18→17→18，应用表保持 71；
  7 个部门默认工作区、32 个测试导航退役、319 个测试配置标记。
- 主库 `AIDT` 已应用 018：18 迁移、71 应用表；迁移前备份与 `RESTORE VERIFYONLY` 通过。
