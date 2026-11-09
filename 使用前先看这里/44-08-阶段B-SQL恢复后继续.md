# 目标模式提示词：阶段 B - SQL Server 恢复后继续

你现在进入 AIDT 阶段 B 的目标模式执行。阶段 A 已完成；当前任务不是重做阶段 A，而是在 SQL Server 恢复后继续完成数据库验证和可运行业务闭环。

## 目标模式执行协议

- 这是持续执行任务，不是方案咨询，也不是只输出计划。
- 直接读取磁盘文件、检查环境、恢复上下文并开始执行。
- 不要求用户重复确认冻结需求、技术栈、目录和规范。
- 先验证 SQL Server；不通过时记录阻断并结束本轮，不尝试绕过。
- SQL Server 通过后，自主持续推进代码、迁移、测试和文档，直到阶段 B 完成或遇到新的真实外部阻断。
- 每完成一个阶段都更新 `task_plan.md`、`findings.md`、`progress.md` 和功能地图。
- 上下文可能被压缩；恢复时以磁盘文件和实际测试结果为准。
- 不把阶段 B 完成等同于整个 AIDT 生产系统完成。
- 同一错误最多三次不同尝试；三次失败后保存证据并请求用户指导。

## 阶段 A 已完成事实

项目当前状态：`SQL_READY_FOR_PHASE_B`

已完成：

- .NET 10 七项目模块化单体骨架。
- Vue 3 前端骨架。
- Domain、Application、API、Infrastructure、Workflow、Integrations、Worker 分层。
- 业务对象、流程任务、权限、审计、Agent 和 Outbox 契约。
- SQL Server 9 Schema、34 张应用表、EF Core Migration、升级和回滚 SQL。
- 后端 28 项测试通过。
- 前端 3 项测试、类型检查、契约检查和构建通过。
- OpenAPI 22 路径、26 operationId 静态校验通过。
- 迁移状态：`authored_not_applied`。
- SQL Server 已安装并连接成功，但 `AIDT` 业务数据库尚未创建。

不得重复创建或随意重写上述成果。先读取和复用，只有发现明确缺陷时才修改。

## 工作目录

项目根目录：当前工作区的 `coding/`。

不得修改旧的 `AIDT/LOCAL-MVP`。

## 开始前必须读取

按顺序完整读取：

1. `coding/AGENTS.md`
2. `coding/docs/001-产品PRD/00-项目目标.md`
3. `coding/docs/001-产品PRD/01-需求基线.md`
4. `coding/docs/001-产品PRD/02-技术栈与架构.md`
5. `coding/docs/001-产品PRD/03-计划方案.md`
6. `coding/standards/00-AI工程宪法.md`
7. `coding/standards/01-上下文阅读与恢复.md`
8. `coding/standards/02-架构与模块边界.md`
9. `coding/standards/03-后端开发规范.md`
10. `coding/standards/04-前端开发规范.md`
11. `coding/standards/05-数据库与迁移规范.md`
12. `coding/standards/06-API与契约规范.md`
13. `coding/standards/07-安全权限审计配置事务.md`
14. `coding/standards/08-测试与回归规范.md`
15. `coding/standards/10-构建部署回滚离线交付.md`
16. `coding/standards/11-功能地图与影响分析.md`
17. `coding/standards/13-Vibecoding工作流.md`
18. `coding/standards/14-完成定义与交付清单.md`
19. `coding/docs/project/FEATURE_MAP.md`
20. `coding/docs/project/PHASE_A_DELIVERY.md`
21. `coding/docs/project/SESSION_HANDOFF_PHASE_A.md`
22. `coding/docs/004-交付说明/SQL_BLOCKED.md`
23. `coding/docs/004-交付说明/STARTUP.md`
24. `coding/database/migrations/README.md`
25. `coding/database/migrations/migration-state.json`
26. `coding/task_plan.md`
27. `coding/findings.md`
28. `coding/progress.md`

## SQL Server 第一检查门

本轮第一项环境操作只能做只读检查：

```powershell
Get-Service -Name 'MSSQL*' -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 1433 -State Listen -ErrorAction SilentlyContinue
```

### 如果服务不存在或没有监听

1. 不尝试连接数据库。
2. 不尝试启动或修复服务。
3. 不重启电脑。
4. 不使用 SQLite、LocalDB、内存库、Docker 或 PostgreSQL 替代。
5. 将当前时间和检查结果写入 `findings.md`、`progress.md`、`task_plan.md` 和 `SQL_BLOCKED.md`。
6. 输出状态 `PHASE_B_BLOCKED_WAITING_SQL`。
7. 立即结束本轮，不做无关代码修改。

### 如果服务存在且 1433 正在监听

继续执行：

1. 确认 SQL Server 实例名和版本。
2. 确认应用连接字符串来源。
3. 使用应用实际使用的 SQL Server 客户端完成连接测试。
4. 确认目标数据库不存在或状态符合预期。
5. 记录连接成功证据。

`sqlcmd` 不存在本身不是阻断条件；可以使用项目实际依赖的 SQL Server 客户端或应用连接测试。

## 阶段 B 执行范围

### B1. 迁移执行

- 读取并审查 `database/migrations/001_InitialSchema.up.sql`。
- 核对 EF Core Migration、SQL 脚本和 Schema 文档一致。
- 若目标数据库已有数据，先备份并记录恢复点。
- 按 `database/migrations/README.md` 规定的路径执行迁移。
- 校验 9 个 Schema、34 张表、主键、外键、索引和版本表。
- 将迁移状态从 `authored_not_applied` 更新为实际执行状态。
- 失败时立即停止并保存数据库错误、执行位置和恢复方式。

### B2. 持久化基础设施

实现或完善：

- EF Core SQL Server DbContext 实际配置。
- Repository。
- UnitOfWork。
- 事务边界。
- 乐观并发。
- 幂等记录。
- 审计写入。
- Outbox 写入。

禁止：

- 在 Controller 直接操作 DbContext。
- 跨模块直接写表。
- 在事务中调用外部服务。
- 用内存仓储冒充生产持久化。

### B3. SQL Server 集成测试

至少覆盖：

- 空库迁移。
- 业务对象创建和版本。
- 流程实例版本引用。
- 任务通过、退回、终止。
- 并发处理冲突。
- 幂等键冲突和重放。
- 审计增长。
- Outbox 与业务同事务。
- 外键、唯一键和索引约束。

集成测试必须连接真实 SQL Server 测试库。

### B4. Elsa Workflows

- 完成 Elsa 与 .NET 10、SQL Server 2025 的 PoC。
- 验证流程版本、任务、条件、退回、终止和定时接缝。
- 不使用 Elsa Studio 作为业务前端。
- 如果 Elsa 不兼容，记录证据并写 ADR 草案，不能默默换成第二套引擎。

### B5. 业务流程

按功能地图实施，先完成一条完整闭环，再扩展：

1. `WF-001` 客户样品到研发转产。
2. `WF-002` 批次异常到 CAPA。
3. `WF-003` 请购到付款。

第一条流程必须真实经过：

```text
业务对象 → 流程实例 → 待办 → 通过/退回/终止
→ 条件跳转 → 审计 → 数据库持久化
```

不得用静态页面或假 API 冒充完成。

### B6. Hangfire 与 Garnet

- Hangfire 连接 SQL Server 持久化。
- 完成任务重试、死信/失败处理和对账任务接缝。
- Garnet 验证缓存、限流或短期锁中的一个明确场景。
- 缓存不得成为业务主存储。

### B7. 权限与审计

- 完成首期身份、角色和 API 权限过滤。
- 至少验证未登录、无权限、数据范围和字段脱敏。
- 高风险动作需要人工确认。
- 审计写入不能由普通 API 修改或删除。

本阶段不要求生产 SSO 全部上线，但不能继续只保留无约束演示身份。

### B8. 前端与浏览器 E2E

- 前端接入真实后端 API。
- 完成 loading、empty、success、error、permission denied 状态。
- 完成工作台、待办、流程详情、对象详情和审计验证。
- 桌面与 390px 移动端测试。
- 至少一条流程完成浏览器端到端。

### B9. 部署与恢复

- 执行数据库备份和恢复到测试库。
- 验证 `deploy.ps1` 或当前部署脚本。
- 验证健康检查、失败回滚和迁移失败停止。
- 不进行真实生产部署。

### B10. 文档与交接

更新：

- 功能地图。
- 模块 README。
- OpenAPI。
- 数据库 Schema 和迁移状态。
- 启动、部署、备份和恢复说明。
- `task_plan.md`、`findings.md`、`progress.md`。
- 阶段 B 交付报告和会话交接。

## 外部系统边界

- U9、ERP、DCS、WMS、LIMS、钉钉继续使用 mock 或只读沙箱。
- 不产生真实付款、质量放行、配方修改、DCS 控制或外部消息。
- 不连接真实生产数据库。

## 阶段 B 完成条件

- SQL Server 2025 连接成功。
- 迁移成功且结构校验通过。
- SQL Server 集成测试通过。
- 至少一条业务流程完成持久化闭环。
- 条件、退回、终止和审计可验证。
- 前端真实接入并通过桌面和移动端 E2E。
- Outbox、Hangfire、Garnet 至少有可验证接缝。
- 权限拒绝和审计验证通过。
- 备份恢复、部署和回滚至少完成测试环境验证。
- 所有未完成项有明确记录。

## 最终输出

按以下顺序输出：

1. 一句话结论。
2. 本轮目标状态：`PHASE_B_BLOCKED_WAITING_SQL`、`PHASE_B_IN_PROGRESS` 或 `PHASE_B_COMPLETE`。
3. SQL Server 版本、服务和连接证据。
4. 已完成内容。
5. 修改文件。
6. 测试命令和结果。
7. 未完成内容。
8. 风险和回滚点。
9. 整个 AIDT 项目是否完成。

在 SQL Server 未连接、迁移未执行、集成测试未通过前，必须明确回答：

> 整个 AIDT 项目仍未完成。

现在开始检查 SQL Server。若服务或监听不存在，按阻断流程记录后结束；只有 SQL Server 可用时才进入迁移和阶段 B 实施。
