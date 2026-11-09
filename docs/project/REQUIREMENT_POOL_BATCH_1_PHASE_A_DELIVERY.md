# 需求池批次 1 阶段 A 交付报告

> 日期：2026-10-31
>
> 当前状态：`PHASE_A_COMPLETE_WAITING_SQL`
>
> 批次状态：`REQUIREMENT_POOL_BATCH_1_IN_PROGRESS`
>
> 整个 AIDT：未完成

## 🎯 本轮范围

只完成四个需求的阶段 A：

- `UX-001` 员工优先业务入口
- `WS-001` 部门工作区
- `DOC-001` 部门文件与附件中心
- `DATA-001` 演示数据与真实业务数据隔离

未实现钉钉听记、真实模型、SLA、转办、数字员工代理、PMO 项目模型或生产 SSO。

## ✅ 已完成

### 工程与契约

- 后端 9 个项目（含 AiGateway）编译通过。
- 新增部门/文件/环境领域、DTO、Application 端口、SQL 适配和 API Controller。
- OpenAPI 增加到 38 路径、46 operationId。
- 默认配置失败关闭：SQL `IsReady=false`、迁移 `authored_not_applied`、
  Workflow 未配置、Worker 后台关闭。

### 员工入口与权限分层

- 根路径 `/` 为“我的工作”。
- 新增 `/department`、`/documents`、`/management`。
- 员工导航与管理入口分离。
- 员工卡片只显示业务名称、当前步骤、负责人、状态和下一步。

### 部门与环境

- 新增 Department、UserDepartmentMembership、部门负责人和主部门。
- DataScope 新增 `department:<guid>`。
- 业务对象、流程实例、任务、附件带有部门、环境和来源字段。
- 普通员工按部门和当前环境过滤；管理员可显式跨环境。

### 文件中心

- 支持上传、列表、详情、版本、下载、预览和删除申请。
- 校验扩展名、MIME、文件签名、大小和 SHA256。
- PDF、图片和 UTF-8 文本可内嵌预览；Office 仅下载。
- 敏感/核心文件默认待人工扫描复核。
- 物理文件名 GUID，前端不接收物理存储路径。
- 版本历史保留，删除仅申请，不静默删除正文。

### 数据库源文件

- EF Migration：`003_DepartmentWorkspaceFilesAndEnvironment`
- 作者态应用表：38
- 幂等 up/down SQL、DBA 审核、数据模型说明和 DemoSeed 更新已编写。
- **未执行任何迁移。**

## 🧪 测试结果

| 门禁 | 结果 |
|---|---|
| 后端构建 | 0 警告、0 错误 |
| 后端测试 | 58 总数；48 通过、0 失败、10 显式跳过 |
| 后端格式 | `dotnet format --verify-no-changes` 通过 |
| NuGet 漏洞 | 无已知漏洞 |
| 前端类型检查 | 通过 |
| 前端测试 | 5 文件、7 测试通过 |
| OpenAPI 静态校验 | 38 路径、46 operationId |
| 前端生产构建 | 通过 |
| npm 生产漏洞 | 0 |
| Markdown 链接 | 通过 |
| `git diff --check` | 通过 |
| API 烟雾 | live 200；ready 503 blocked；未登录 my-work 401 |
| Worker 烟雾 | live 200；ready 503 blocked；后台 false |

10 个跳过项全部是 SQL Server 连接、SQL 约束或 SQL 登录态 API 集成。

### SQL 边界纠正记录

早期有一次全量测试在配置切回阶段 A 前误触发既有 SQL 登录集成，登录返回 HTTP 500。
发现后立即停止 SQL 路径，没有继续诊断服务/端口/连接，也没有执行迁移或写业务数据。
之后将 10 个 SQL 用例改为默认显式 Skip，全部后续验证都在阻断配置下完成。

## 🚨 SQL BLOCKED

- 原因：SQL Server 安装要求重启，错误码 3010
- 已检查：沿用历史服务、1433、sqlcmd 证据；本轮未重新探测或修复
- 未执行：迁移、SQL 集成测试、备份恢复、数据库端到端验证
- 继续条件：公司电脑重启后 SQL Server 服务可用，1433 监听，应用连接成功

## 🚧 未完成范围

- 003 在隔离测试库 up/down 和 38 表约束验证
- 部门/环境 SQL 过滤、文件上传下载、版本和越权审计集成测试
- 登录后桌面和 390px 浏览器 E2E
- WF-001 持久化回归
- 备份恢复、主库迁移、部署和回滚

## ⏭️ 阶段 B 恢复检查点

1. 检查 `MSSQL*` Windows 服务状态
2. 检查 TCP `1433`
3. 检查 SQL Server 版本、实例名
4. 使用应用连接串完成连接测试
5. 备份并验证备份
6. 隔离测试库执行 002→003→003 down
7. 通过后才执行主库迁移和集成/E2E

## 🔒 保密提醒

本轮未使用真实配方、客户、成本、报价、工艺参数或生产数据。

## 📄 实际修改文件

### 后端

- API：`backend/AIDT.Api/Controllers/{MyWork,Departments,Files,BusinessObjects,Workflow,Dashboard}Controller.cs`
- API 配置：`appsettings.json`、`Configuration/RuntimeOptionsValidation.cs`、`Middleware/GlobalExceptionMiddleware.cs`
- Application：`Abstractions/{Ports,EmployeeFilePorts}.cs`
- Application 契约：`Contracts/{CommonContracts,ContractMapping,EmployeeWorkspaceContracts,FileCenterContracts,DataEnvironmentOptions}.cs`
- Application 文件校验：`Documents/FileContentValidator.cs`
- Application 用例：`UseCases/{CreateBusinessObject,StartWorkflowInstance,ProcessWorkflowTask}Handler.cs`
- Domain：`BusinessObjects/BusinessObject.cs`、`Workflows/WorkflowModels.cs`
- Domain 新模型：`Identity/Department.cs`、`Documents/FileModels.cs`、`Common/DataEnvironment.cs`
- Domain 权限/错误：`Identity/UserPrincipal.cs`、`Common/{DomainChecks,ErrorCodes,AidtDomainException}.cs`
- Infrastructure：`DependencyInjection.cs`、`Configuration/AidtRuntimeOptions.cs`
- EF 实体与配置：`Persistence/{Entities,AidtDbContext,AidtModelConfigurations,AidtModelConfigurations.Workflow,AidtModelConfigurations.AuditIntegrationAgent,AidtModelConfigurations.DocumentLink}.cs`
- SQL 仓储：`Repositories/{SqlDepartmentRepository,SqlBusinessObjectRepository,SqlWorkflowRepository,SqlIdentityRepositories}.cs`
- 查询与服务：`Queries/SqlWorkspaceQueryService.cs`、`Services/{SqlEmployeeWorkspaceService,SqlFileCenterService,NotReadyRepositories,RuntimeServices}.cs`
- 文件存储：`Storage/LocalFileStorage.cs`
- 后端 README：`backend/README.md`、`backend/AIDT.{Domain,Application,Infrastructure}/README.md`

### 数据库

- `backend/AIDT.Infrastructure/Persistence/Migrations/20261030172128_003_DepartmentWorkspaceFilesAndEnvironment.cs`
- `backend/AIDT.Infrastructure/Persistence/Migrations/20261030172128_003_DepartmentWorkspaceFilesAndEnvironment.Designer.cs`
- `backend/AIDT.Infrastructure/Persistence/Migrations/AidtDbContextModelSnapshot.cs`
- `database/migrations/003_DepartmentWorkspaceFilesAndEnvironment.up.sql`
- `database/migrations/003_DepartmentWorkspaceFilesAndEnvironment.down.sql`
- `database/migrations/migration-state.json`
- `database/seed/001_DemoSeed.sql`
- `database/dba-review/003-DepartmentWorkspaceFilesAndEnvironment.md`
- `database/schema/003-DEPARTMENT-FILES-ENVIRONMENT.md`
- `database/README.md`、`database/migrations/README.md`、`database/schema/INITIAL_SCHEMA.md`

### 前端

- 路由/布局/权限：`frontend/src/{router/index.ts,layouts/AppLayout.vue,stores/auth.ts}`
- HTTP 与契约：`frontend/src/api/http.ts`、`api/modules/{workspaceApi,fileApi}.ts`、`api/types/contracts.ts`
- 页面：`frontend/src/pages/{MyWorkPage,DepartmentPage,DocumentsPage,DashboardPage,LoginPage,WorkflowPage}.vue`
- 组件：`frontend/src/components/base/EnvironmentTag.vue`、`frontend/src/features/workflow/components/WorkItemCard.vue`
- 组件测试：`EnvironmentTag.test.ts`、`WorkItemCard.test.ts`
- 契约脚本：`frontend/scripts/check-openapi.mjs`、`frontend/README.md`

### 测试与文档

- 后端新增：`tests/AIDT.UnitTests/{Domain/DepartmentAndFileTests,Application/FileContentValidatorTests,Infrastructure/LocalFileStorageTests,SqlIntegrationFactAttribute}.cs`
- 后端更新：API、Application、Domain、MigrationModel 和 SQL 测试文件
- OpenAPI/API：`docs/contracts/openapi/v1.json`、`docs/contracts/API_CONTRACT.md`
- 功能/权限：`docs/project/FEATURE_MAP.md`、`docs/project/PERMISSION_MATRIX.md`
- 变更/交付：`docs/project/CHANGE_PLAN_REQUIREMENT_POOL_BATCH_1.md`、本报告
- 运行/阻断：`docs/004-交付说明/{STARTUP,SQL_BLOCKED}.md`
- 规划恢复：`task_plan.md`、`findings.md`、`progress.md`
- 根说明：`README.md`
