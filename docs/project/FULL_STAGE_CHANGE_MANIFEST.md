# AIDT 全阶段实际变更清单

> 日期：2026-10-30  
> 状态：`FULL_STAGE_COMPLETE_DEMO_READY`  
> 说明：`coding/` 不是 Git 仓库，无法生成 Git diff；以下为磁盘上的实际核心修改。

## 🧱 解决方案与后端

- `AIDT.slnx`
- `backend/AIDT.AiGateway/**`
- `backend/AIDT.Api/appsettings.json`
- `backend/AIDT.Api/Controllers/ConnectorsController.cs`
- `backend/AIDT.Application/Abstractions/Ports.cs`
- `backend/AIDT.Infrastructure/Configuration/AidtRuntimeOptions.cs`
- `backend/AIDT.Infrastructure/Queries/SqlWorkspaceQueryService.cs`
- `backend/AIDT.Infrastructure/Services/RuntimeServices.cs`
- `backend/AIDT.Infrastructure/Services/NotReadyRepositories.cs`
- `backend/AIDT.Integrations/ConnectorContracts.cs`
- `backend/AIDT.Worker/appsettings.json`
- `backend/AIDT.Worker/Program.cs`
- `backend/AIDT.Workflow/AIDT.Workflow.csproj`
- `backend/AIDT.Workflow/DependencyInjection.cs`
- `backend/README.md`
- 各后端模块 README

## 🧪 测试与浏览器 E2E

- `tests/AIDT.UnitTests/AIDT.UnitTests.csproj`
- `tests/AIDT.UnitTests/Api/ApiContractTests.cs`
- `tests/AIDT.UnitTests/AiGateway/AiGatewayContractTests.cs`
- `tests/AIDT.UnitTests/Integrations/MockConnectorAdapterTests.cs`
- `tests/AIDT.UnitTests/Integration/SqlServerConnectionProbeTests.cs`
- `tests/AIDT.UnitTests/Integration/SqlServerPersistenceInvariantTests.cs`
- `frontend/playwright.config.ts`
- `frontend/e2e/aidt.spec.ts`
- `frontend/package.json`
- `frontend/package-lock.json`
- `docs/project/test-artifacts/**`

## 🌐 前端

- `frontend/vite.config.ts`
- `frontend/src/main.ts`
- `frontend/src/router/index.ts`
- `frontend/src/layouts/AppLayout.vue`
- `frontend/src/pages/LoginPage.vue`
- `frontend/src/pages/DashboardPage.vue`
- `frontend/src/pages/BusinessObjectPage.vue`
- `frontend/src/pages/WorkflowPage.vue`
- `frontend/src/pages/AuditPage.vue`
- `frontend/src/pages/AgentPage.vue`
- `frontend/README.md`

## 🗄️ 数据库与种子

- `database/seed/001_DemoSeed.sql`
- `database/migrations/migration-state.json`
- `database/migrations/README.md`
- `database/README.md`
- `database/schema/INITIAL_SCHEMA.md`
- `database/backup-notes/README.md`
- `database/dba-review/001-InitialSchema.md`
- `database/dba-review/002-WorkflowPersistenceAndSecurity.md`

## 🚀 部署与离线

- `deploy/build-release.ps1`
- `deploy/deploy.ps1`
- `deploy/rollback.ps1`
- `deploy/stop-test-deployment.ps1`
- `deploy/nginx/nginx.conf.template`
- `deploy/offline/export-and-verify.ps1`
- `deploy/offline/README.md`
- `deploy/README.md`

最终包在：

```text
D:\Apps\AIDT-Test\releases\AIDT-demo-final3.zip
```

## 📚 文档与规划

- `README.md`
- `task_plan.md`
- `findings.md`
- `progress.md`
- `docs/contracts/API_CONTRACT.md`
- `docs/contracts/openapi/v1.json`
- `docs/004-交付说明/STARTUP.md`
- `docs/004-交付说明/SQL_BLOCKED.md`
- `docs/project/FEATURE_MAP.md`
- `docs/project/FULL_STAGE_DELIVERY.md`
- `docs/project/PERMISSION_MATRIX.md`
- `docs/project/MOCK_BOUNDARIES.md`
- `docs/project/TEST_EVIDENCE.md`
- `docs/project/SESSION_HANDOFF_FULL_STAGE.md`
- `docs/project/FULL_STAGE_CHANGE_MANIFEST.md`

## 🧹 机械格式

`dotnet format` 统一了后端和测试 C# 文件编码、行尾和格式，没有改变业务语义。
