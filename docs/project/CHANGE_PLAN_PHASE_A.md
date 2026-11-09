# 阶段 A 变更计划

> 状态：complete  
> 日期：2026-10-30  
> 范围：SQL Server 阻断下可独立完成的正式工程基线

## 目标

在 `coding/` 建立可直接延续到甲方交付的 .NET 10 模块化单体与 Vue 3 前端基线，完成领域、契约、非 SQL 测试、迁移源文件和文档；不执行数据库操作。

## 实施顺序

1. 建立解决方案、`global.json`、集中 NuGet 版本和统一构建属性。
2. 创建 7 个后端项目，固定 `Api → Application → Domain`、`Infrastructure/Workflow/Integrations → 公开契约`、`Worker → Application/Infrastructure` 的引用方向。
3. 实现启动配置校验、统一响应、异常处理、TraceId、健康检查和静态 OpenAPI。
4. 建立 Vue 3 + TypeScript + Vite + Element Plus + Pinia + Router 骨架。
5. 编写领域状态转换、DTO、错误码、应用用例接口和显式未连接实现。
6. 编写 SQL Server 迁移、发布/回滚脚本，状态固定为 `authored_not_applied`。
7. 编写并执行 Domain、Application、契约和前端测试。
8. 完成架构、行为、交付三视角自审并更新全部索引文档。

## 验证

| 项目 | 命令/证据 | SQL 依赖 |
|---|---|---|
| 后端编译 | `dotnet build AIDT.slnx` | 否 |
| 后端测试 | `dotnet test AIDT.slnx --no-build` | 否 |
| 后端格式 | `dotnet format AIDT.slnx --verify-no-changes` | 否 |
| 前端类型 | `npm run typecheck` | 否 |
| 前端测试 | `npm run test` | 否 |
| 前端构建 | `npm run build` | 否 |
| OpenAPI | `npm run contract:check` | 否 |
| SQL 集成 | 仅保留待运行标记 | 是，阶段 B |

## 禁止项

- 不连接 SQL Server，不执行迁移。
- 不以内存库、SQLite、LocalDB、Docker、PostgreSQL 替代。
- 不将 Skeleton、Mock、未连接状态描述为真实业务完成。
- 不修改旧 `AIDT/LOCAL-MVP`。

## 回滚

本轮无外部状态变更。代码和文档按新增目录整体撤回；SQL 回滚脚本仅作为阶段 B 授权执行资产保留。
