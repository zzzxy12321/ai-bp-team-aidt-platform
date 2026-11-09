# SQL Server 阻断与恢复记录

## 当前状态：RESOLVED_FOR_BATCH_1

SQL Server 第一检查门已通过，003/004 已在隔离测试库和主 `AIDT` 执行。
以下 SQL BLOCKED 内容保留为阶段 A 历史证据。

## SQL BLOCKED（历史阶段 A）

- 原因：SQL Server 安装要求重启，错误码 3010
- 已检查：服务、1433、sqlcmd
- 未执行：迁移、集成测试、数据库端到端验证
- 继续条件：公司电脑重启后 SQL Server 服务可用，1433 监听，应用连接成功

> 当前阶段 A 的阻断不是重新尝试安装或修复 SQL Server，而是执行请求明确要求：
> 一旦触及服务、端口、连接、迁移或集成测试边界，立即停止并保持 SQL BLOCKED。
> 下面的恢复记录只作为历史证据，不授权本轮连接数据库。

## 当前状态

历史运行曾完成 SQL Server 2025 安装和 001/002 迁移验证。当前需求池批次 1
阶段 A 固定为 `authored_not_applied`，不连接、不迁移、不运行 SQL 集成测试。

## 原始阶段 A 边界

```markdown
## SQL BLOCKED

- 原因：本次执行严格限定为阶段 A，并要求在数据库边界立即停止
- 已检查：使用历史记录中的服务、1433、sqlcmd 和安装日志作为原始阻断证据
- 未执行：数据库连接、迁移执行、SQL Server 集成测试、备份恢复、数据库端到端验证
- 继续条件：进入阶段 B 后重新检查 MSSQL* 服务、TCP 1433、实例版本和应用连接；全部通过后另行获准迁移
```

以下记录说明 SQL Server 如何从安装阻断恢复到当前可用状态。

- 原始原因：SQL Server 安装程序在规则检查阶段要求重启，错误码 `3010`
- 恢复时间：2026-10-30 08:22 左右
- 当前状态：**RESOLVED**
- SQL Server 安装：成功，Exit Code `0`
- 服务：`MSSQLSERVER` Running，启动类型 Automatic
- 实例：`MSSQLSERVER`
- 版本：`17.0.1000.7`
- 版本类型：SQL Server 2025 Express
- TCP：`1433` 正在监听
- 实际连接：Windows 身份验证连接 `master` 成功
- 业务库：后续历史运行曾创建 `AIDT`；本轮未访问
- 迁移状态：本轮固定为 `authored_not_applied`
- 下一步：阶段 B 重新通过 SQL 第一门及授权检查后，再决定迁移

## 🔎 阻断证据

| 检查项 | 结果 |
|---|---|
| SQL Server 安装目录 | 存在：`C:\Program Files\Microsoft SQL Server\170` |
| 安装日志 | `ErrorCode: 3010`，要求重新启动计算机 |
| Windows 服务 | 未发现可用 `MSSQL*` 服务 |
| TCP `1433` | 未监听 |
| `sqlcmd` | 不在 PATH |
| 系统最近重启 | 2026-10-30 08:04 |
| 待重启标记 | 三个常用 Pending Reboot 标记均不存在 |
| 重启后安装日志 | 没有新增安装日志，最新日志仍为 2026-10-29 22:00 |
| SQL 实例注册 | `Instance Names\SQL` 不存在 |
| 安装进程 | 当前没有 setup/sqlservr 进程 |
| 磁盘空间 | C: 剩余约 100.6 GB |

## 🧭 根因判断

1. 2026-10-29 22:00 的安装尝试在 `RebootRequiredCheck` 规则处被 `3010` 中止，尚未进入 SQL Server 引擎实际安装阶段。
2. 2026-10-30 08:04 系统已经重启，本轮重启条件已经解除。
3. 重启后没有出现新的 SQL Server Setup 日志，也没有安装进程。
4. 当前既没有 `MSSQL*` 服务，也没有实例注册和 1433 Listen。

结论：**不是 SQL Server 服务无法启动，而是安装程序没有在重启后自动继续，当前仍需完成安装。**

## 🛠️ 已执行的现场恢复

1. 已使用本机管理员账号 `PSD-邹晓阅\PSD-ZXY` 执行静默安装。
2. 安装入口：

```text
C:\SQL2025\Express_ENU\SETUP.EXE
```

3. 安装数据库引擎并创建默认实例 `MSSQLSERVER`。
4. 启用 Windows 身份验证，并保留当前管理员账号为 SQL Server 管理员。
5. 启用 TCP/IP，端口设为 `1433`。
6. 安装器返回 Exit Code `0`。
7. 已完成以下验证：

```powershell
Get-Service -Name 'MSSQL*'
Get-NetTCPConnection -LocalPort 1433 -State Listen
```

8. 服务启动、1433 监听和实例连接全部成功后，才运行阶段 B 目标模式。

## 📌 安装版本提醒

当前安装介质为 `SQLEXPR_x64_CHS.exe`，即 SQL Server 2025 Express 版本。该版本可用于单机 Demo，但生产交付前必须确认数据库容量、内存、CPU、并发和授权是否满足甲方要求。不得把 Express Demo 直接表述为正式生产版选型。

## 🚧 仍待阶段 B 完成

- 创建 `AIDT` 业务数据库
- 执行数据库迁移
- 运行 SQL Server 集成测试
- 验证备份恢复
- 验证三条业务流程的持久化闭环
- 验证数据库端到端和浏览器 E2E

## ✅ 阶段 A 不越界证据

- 没有使用 SQLite、LocalDB、内存数据库、Docker 或 PostgreSQL。
- 没有修改旧 `AIDT/LOCAL-MVP`。
- 没有执行 `dotnet ef database update`。
- 没有执行升级或回滚 SQL。
- API 启动时不会调用 `Database.Migrate()` 或打开连接。
- 未连接任何真实外部系统或模型。

## ⏭️ 下一次运行

下一次运行先恢复任务书，再检查 `MSSQL*` 服务、TCP 1433 和应用连接。只有三项
全部通过并明确进入阶段 B 后，才可备份并执行 003 迁移；第一项数据库操作不得是
直接迁移。
