# 阶段 A 实际变更清单

> 日期：2026-10-30  
> 状态：`PHASE_A_COMPLETE_WAITING_SQL`  
> 说明：`coding/` 不是 Git 仓库，本清单依据实际磁盘操作和文件时间整理；不把后续运行的历史 SQL 证据算作本轮变更。

## 🧱 后端运行边界

- `backend/AIDT.Api/appsettings.json`
- `backend/AIDT.Infrastructure/Configuration/AidtRuntimeOptions.cs`
- `backend/AIDT.Infrastructure/Services/RuntimeServices.cs`
- `backend/AIDT.Workflow/DependencyInjection.cs`
- `backend/AIDT.Workflow/AIDT.Workflow.csproj`
- `backend/AIDT.Worker/Program.cs`
- `backend/AIDT.Worker/appsettings.json`

关键变化：

- API 与 Worker 默认 `Database.IsReady=false`。
- Workflow 默认不注册 Elsa SQL 运行时。
- Worker 默认不注册 Hangfire、Outbox、Garnet 或心跳任务。
- Elsa 传递依赖 Pkcs 强制升级到 `10.0.14`。

## 🌐 前端

- `frontend/scripts/check-openapi.mjs`
- `frontend/src/pages/DashboardPage.vue`
- `frontend/src/pages/BusinessObjectPage.vue`
- `frontend/src/pages/WorkflowPage.vue`
- `frontend/src/pages/AuditPage.vue`
- `frontend/src/pages/AgentPage.vue`
- `frontend/src/pages/LoginPage.vue`
- `frontend/README.md`

关键变化：移除“真实 SQL 已接通”“SQL AVAILABLE”“治理生效”等超范围声明，统一为阶段 A 契约或失败关闭文案。

## 🧪 测试

- `tests/AIDT.UnitTests/Api/ApiContractTests.cs`
- `tests/AIDT.UnitTests/Integration/SqlServerConnectionProbeTests.cs`
- `tests/README.md`

关键变化：

- API 测试使用隔离的阶段 A 配置。
- SQL 连接探针与 SQL 登录读取测试显式 `Skipped`。
- 不再用直接 `return` 把未执行测试伪装为通过。

## 🗄️ 数据库源文件与状态

- `database/migrations/migration-state.json`
- `database/migrations/README.md`
- `database/README.md`
- `database/schema/INITIAL_SCHEMA.md`
- `database/dba-review/002-WorkflowPersistenceAndSecurity.md`
- `database/backup-notes/README.md`

关键变化：

- 本轮迁移状态为 `authored_not_applied`。
- 两批迁移与回滚脚本保留 SHA256。
- 后续运行的历史执行证据保留在 `historicalExecution`，不混入本轮结论。
- 新增迁移 002 的 DBA 审核检查点。

## 📚 契约与交付文档

- `docs/contracts/openapi/v1.json`
- `docs/contracts/API_CONTRACT.md`
- `docs/004-交付说明/STARTUP.md`
- `docs/004-交付说明/SQL_BLOCKED.md`
- `docs/project/FEATURE_MAP.md`
- `docs/project/CHANGE_PLAN_PHASE_A.md`
- `docs/project/PHASE_A_DELIVERY.md`
- `docs/project/SESSION_HANDOFF_PHASE_A.md`
- `docs/project/PHASE_A_CHANGE_MANIFEST.md`
- `README.md`
- `backend/README.md`
- `backend/AIDT.Infrastructure/README.md`
- `backend/AIDT.Workflow/README.md`
- `backend/AIDT.Worker/README.md`

关键变化：OpenAPI 补齐前端实际使用路径，SQL 阻断、恢复检查点、能力状态和测试证据统一。

## 🧹 机械格式统一

`dotnet format` 对既有 C# 文件统一了编码、行尾和格式，涉及以下目录中的既有文件：

- `backend/AIDT.Api/Controllers/`
- `backend/AIDT.Api/Identity/`
- `backend/AIDT.Api/Middleware/`
- `backend/AIDT.Application/UseCases/`
- `backend/AIDT.Infrastructure/Persistence/`
- `backend/AIDT.Infrastructure/Queries/`
- `backend/AIDT.Worker/Jobs/`
- `backend/AIDT.Workflow/`
- `tests/AIDT.UnitTests/`

格式统一没有改变业务语义。

## 📋 规划与记忆

- `task_plan.md`
- `findings.md`
- `progress.md`

以上文件已记录：阶段 A 完成、整个 AIDT 项目未完成、SQL 未执行、历史运行痕迹、测试结果和阶段 B 第一检查点。
