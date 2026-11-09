# AIDT 阶段 A 交付报告

> 当前状态：`PHASE_A_COMPLETE_WAITING_SQL`  
> 整个 AIDT 项目：**未完成**  
> SQL Server：本轮未连接、未迁移；历史后续证据已隔离保留

逐文件变更见 [PHASE_A_CHANGE_MANIFEST.md](./PHASE_A_CHANGE_MANIFEST.md)。

## ✅ 已完成

- 正式 .NET 10 七项目模块化单体骨架
- Domain、Application、API、Infrastructure、Workflow、Integrations、Worker 分层
- 统一响应、全局异常、TraceId、健康检查、OpenAPI
- Vue 3 + TypeScript + Vite + Element Plus + Pinia + Vue Router 前端骨架
- 业务对象、流程任务、审计、身份权限、Agent 和 Outbox 契约
- SQL Server 9 Schema、35 应用表、两批 EF 迁移、升级和回滚 SQL
- 27 项非 SQL 后端测试通过、2 项 SQL 测试显式跳过；3 项前端测试、OpenAPI 静态校验
- 模块、API、Schema、启动、DBA、阻断和恢复文档

## 📁 实际文件清单

### 根级与构建

- `AIDT.slnx`
- `global.json`
- `Directory.Build.props`
- `Directory.Packages.props`
- `nuget.config`
- `.editorconfig`
- `.gitignore`
- `dotnet-tools.json`
- `README.md`
- `task_plan.md`
- `findings.md`
- `progress.md`

### 后端

- `backend/README.md`
- `backend/AIDT.Api/**`
- `backend/AIDT.Application/**`
- `backend/AIDT.Domain/**`
- `backend/AIDT.Infrastructure/**`
- `backend/AIDT.Workflow/**`
- `backend/AIDT.Integrations/**`
- `backend/AIDT.Worker/**`

### 前端

- `frontend/package.json`
- `frontend/package-lock.json`
- `frontend/vite.config.ts`
- `frontend/tsconfig*.json`
- `frontend/.env.example`
- `frontend/README.md`
- `frontend/scripts/check-openapi.mjs`
- `frontend/src/**`

### 数据库

- `database/README.md`
- `database/schema/INITIAL_SCHEMA.md`
- `database/migrations/README.md`
- `database/migrations/migration-state.json`
- `database/migrations/001_InitialSchema.up.sql`
- `database/migrations/001_InitialSchema.down.sql`
- `database/migrations/002_WorkflowPersistenceAndSecurity.up.sql`
- `database/migrations/002_WorkflowPersistenceAndSecurity.down.sql`
- `database/dba-review/001-InitialSchema.md`
- `database/backup-notes/README.md`

### 测试

- `tests/README.md`
- `tests/AIDT.UnitTests/AIDT.UnitTests.csproj`
- `tests/AIDT.UnitTests/TestUsers.cs`
- `tests/AIDT.UnitTests/Domain/*.cs`
- `tests/AIDT.UnitTests/Application/*.cs`
- `tests/AIDT.UnitTests/Contracts/*.cs`
- `tests/AIDT.UnitTests/Api/*.cs`

### 文档

- `docs/project/FEATURE_MAP.md`
- `docs/project/CHANGE_PLAN_PHASE_A.md`
- `docs/project/CONTEXT_PACK_PHASE_A.md`
- `docs/project/PHASE_A_DELIVERY.md`
- `docs/project/SESSION_HANDOFF_PHASE_A.md`
- `docs/contracts/API_CONTRACT.md`
- `docs/contracts/openapi/v1.json`
- `docs/004-交付说明/STARTUP.md`
- `docs/004-交付说明/SQL_BLOCKED.md`

## 🧪 最终测试证据

| 命令 | 结果 |
|---|---|
| `dotnet restore AIDT.slnx` | 成功 |
| `dotnet build AIDT.slnx --no-restore` | 0 警告、0 错误 |
| `dotnet test AIDT.slnx --no-build` | 29 总数；27 通过、0 失败、2 显式跳过 |
| `dotnet format AIDT.slnx --verify-no-changes --no-restore` | 通过 |
| `dotnet list AIDT.slnx package --vulnerable --include-transitive` | 无已知漏洞 |
| `npm ci` | 成功 |
| `npm run typecheck` | 通过 |
| `npm run test` | 3 通过、0 失败 |
| `npm run contract:check` | 28 路径、33 operationId 通过 |
| `npm run build` | 成功，无大包警告 |
| `npm audit --omit=dev` | 0 漏洞 |
| 运行态烟雾 | API live `200` / ready `503`；Worker live `200` / ready `503` |
| SQL 集成探针 | 显式 Skipped，未连接数据库 |

## 🚧 未完成

- 阶段 B 的 SQL Server 重新检查、迁移执行和数据库验证
- 正式 SQL 仓储与 UnitOfWork
- Elsa/Hangfire/Garnet 版本 PoC 与实际集成
- 正式 SSO、API 权限过滤、数据范围和字段脱敏
- SQL Server 集成测试、备份恢复
- 完整业务链持久化、浏览器 E2E
- 离线部署包、部署和回滚演练

## 🔎 SQL 阻断证据

- SQL Server 安装日志：`ErrorCode: 3010`
- 原始检查未发现 `MSSQL*` Windows 服务
- 原始检查 TCP `1433` 未监听
- 原始检查 `sqlcmd` 不在 PATH
- 本轮迁移状态：`authored_not_applied`
- 后续运行留下过 `InitialSchema` 历史执行记录，已保留在 `migration-state.json.historicalExecution`，不计入本轮
- 未使用 SQLite、LocalDB、内存数据库、Docker 或 PostgreSQL 替代

## ⏭️ 阶段 B 恢复检查点

下一次运行同一提示词时，第一项只检查：

```powershell
Get-Service -Name 'MSSQL*' -ErrorAction SilentlyContinue
```

如果服务不存在，立即停止并保留 `SQL BLOCKED`。只有服务可用后再检查 TCP 1433、实例版本和应用连接；连接成功后仍需确认已获准进入阶段 B，之后才允许执行迁移。
