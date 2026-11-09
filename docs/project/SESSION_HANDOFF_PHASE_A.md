# 会话交接：AIDT 阶段 A

## 当前目标

完成 SQL Server 阻断下的阶段 A 框架搭建；阶段 B 等待公司电脑重启和 SQL Server 可用。

## 已完成

- 正式 .NET 10 七项目后端骨架、领域/应用契约、API/Worker。
- Vue 3 前端工作台、流程、对象、审计和 Agent 骨架。
- SQL Server 初始 Schema、EF Core Migration、升级和回滚 SQL。
- 后端 27 项通过 + 2 项 SQL 测试显式跳过、前端 3 项测试、28 路径/33 operationId OpenAPI 静态校验和构建。
- 模块 README、功能地图、API、数据库、启动和阻断文档。

## 修改文件

完整分类清单见 `docs/project/PHASE_A_DELIVERY.md` 的“实际文件清单”。核心入口：

- 解决方案：`AIDT.slnx`
- 后端：`backend/**`
- 前端：`frontend/**`
- 数据库：`database/**`
- 测试：`tests/**`
- 契约：`docs/contracts/**`
- 规划：`task_plan.md`、`findings.md`、`progress.md`

## 未提交变更

`coding/` 当前不是 Git 仓库，无法提供 `git status` 和提交记录。所有变更均在磁盘工作区，未执行外部提交。

## 关键决策

- SQL Server 未就绪时不连接、不迁移，不以任何替代数据库兜底。
- API 存活检查不依赖数据库；就绪检查明确返回 blocked。
- 持久化端点返回 `PERSISTENCE_NOT_READY`，不提供内存持久化假象。
- Elsa、Hangfire、Garnet、真实身份和外部连接器只保留接缝。
- 前端权限使用显式 `contract-preview`，界面不得冒充正式权限控制。

## 测试命令和结果

```powershell
dotnet build AIDT.slnx --no-restore
dotnet test AIDT.slnx --no-build
dotnet format AIDT.slnx --verify-no-changes --no-restore

Set-Location frontend
npm ci
npm run typecheck
npm run test
npm run contract:check
npm run build
```

- 后端：29 总数；27 通过、0 失败、2 跳过；0 警告。
- 前端：3 通过、0 失败；类型检查、契约校验和生产构建通过。
- 运行态：API live `200`、ready `503`；Worker live `200`、ready `503`；测试端口已释放。
- NuGet/npm 生产依赖：未发现已知漏洞。

## 未完成事项

- SQL Server 连接和迁移执行。
- 持久化仓储、Elsa 运行时、Hangfire/Garnet。
- 正式 SSO、权限过滤、数据范围。
- SQL 集成测试、业务链闭环、浏览器 E2E。
- 备份恢复、部署和回滚演练。

## 已知风险

- 所有数据库行为仍只有静态模型/脚本证据。
- 认证和权限只有骨架与单元规则，不是生产认证。
- 真实工作流推进和外部连接器未实现。
- 当前不是 Git 仓库，交接依赖磁盘清单和测试输出。

## 下一个最小步骤

检查 `MSSQL*` Windows 服务状态。服务不存在时停止；服务存在后检查 1433、实例版本和应用连接。

## 禁止重复的操作

- 不要重新生成或修改已完成的迁移，除非模型确实变化并走新迁移。
- 不要在服务不存在时反复尝试连接或执行迁移。
- 不要用 SQLite、LocalDB、内存库、Docker 或 PostgreSQL 替代 SQL Server。
- 不要把阶段 A 描述为整个 AIDT 项目完成。
