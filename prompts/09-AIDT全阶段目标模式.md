# 目标模式提示词：AIDT 全阶段 Demo 一次执行

你现在进入 AIDT 全阶段目标模式。你的任务不是写方案，也不是只完成一个阶段，而是在现有阶段 A 成果基础上，持续执行到首个可交付 Demo 满足全部验收条件。

## 一、目标模式执行协议

- 这是持续自主执行任务，不是方案咨询。
- 直接读取磁盘文件、检查环境、恢复上下文并开始执行。
- 不要求用户重复确认已经冻结的需求、技术栈、目录、规范和计划。
- 不先写长篇计划后停止；计划写入文件后立即实施。
- 不要重复搭建阶段 A 已有工程，不要无故重写已有迁移、前端和测试。
- 遇到问题先从文档、代码、测试和环境证据中查证，采用符合冻结基线的最小合理假设，并记录到 `findings.md`。
- 每完成一个工作包，更新 `task_plan.md`、`findings.md`、`progress.md` 和功能地图。
- 上下文可能被压缩；恢复时以磁盘文件、Git 状态和实际测试结果为准，不凭记忆重做。
- 同一错误最多进行三次不同尝试；三次后保存具体错误和尝试记录并请求用户指导。
- 只有全部验收条件满足，或遇到不可自行解决的真实外部阻断时，才允许结束本轮。
- 不得把“代码已写完”“API 返回 200”或“页面能打开”表述为 Demo 完成。

## 二、当前项目状态

当前状态：`SQL_READY_FOR_PHASE_B`

阶段 A 已完成：

- .NET 10 七项目模块化单体。
- Vue 3 + TypeScript + Element Plus 前端。
- 领域、应用、API、Infrastructure、Workflow、Integrations、Worker 骨架。
- 业务对象、流程任务、审计、权限、Agent 和 Outbox 契约。
- SQL Server 9 Schema、34 张应用表、EF Core Migration、升级和回滚 SQL。
- 后端 28 项测试通过。
- 前端 3 项测试、类型检查、契约检查和构建通过。
- OpenAPI 22 路径、26 operationId。
- 迁移状态：`authored_not_applied`。
- SQL Server 2025 已安装、服务运行、1433 监听、实例连接成功。
- `AIDT` 业务数据库尚未创建。

整个 AIDT 项目仍未完成。

## 三、工作目录

项目根目录：当前工作区的 `coding/`。

不得修改旧的 `AIDT/LOCAL-MVP`。

## 四、开始前必须读取

按顺序完整读取：

1. `coding/AGENTS.md`
2. `coding/docs/001-产品PRD/00-项目目标.md`
3. `coding/docs/001-产品PRD/01-需求基线.md`
4. `coding/docs/001-产品PRD/02-技术栈与架构.md`
5. `coding/docs/001-产品PRD/03-计划方案.md`
6. `coding/standards/README.md`
7. `coding/standards/00-AI工程宪法.md`
8. `coding/standards/01-上下文阅读与恢复.md`
9. `coding/standards/02-架构与模块边界.md`
10. `coding/standards/03-后端开发规范.md`
11. `coding/standards/04-前端开发规范.md`
12. `coding/standards/05-数据库与迁移规范.md`
13. `coding/standards/06-API与契约规范.md`
14. `coding/standards/07-安全权限审计配置事务.md`
15. `coding/standards/08-测试与回归规范.md`
16. `coding/standards/09-Git评审提交规范.md`
17. `coding/standards/10-构建部署回滚离线交付.md`
18. `coding/standards/11-功能地图与影响分析.md`
19. `coding/standards/12-Bug修复与重构规范.md`
20. `coding/standards/13-Vibecoding工作流.md`
21. `coding/standards/14-完成定义与交付清单.md`
22. `coding/docs/project/FEATURE_MAP.md`
23. `coding/docs/project/PHASE_A_DELIVERY.md`
24. `coding/docs/project/SESSION_HANDOFF_PHASE_A.md`
25. `coding/docs/004-交付说明/SQL_BLOCKED.md`
26. `coding/docs/004-交付说明/STARTUP.md`
27. `coding/database/migrations/README.md`
28. `coding/database/migrations/migration-state.json`
29. `coding/task_plan.md`
30. `coding/findings.md`
31. `coding/progress.md`

## 五、全阶段目标范围

本轮“全阶段”定义为：从当前阶段 A 成果继续，完成首个可交付 AIDT Demo。

不包含：

- 13 个部门工作台全部上线。
- 真实 U9、ERP、DCS、WMS、LIMS、钉钉生产接入。
- 真实付款、质量放行、配方修改或 DCS 控制。
- 正式生产上线。
- 全部数字员工与真实外部大模型。

完成后应具备：

1. 真实 SQL Server 持久化。
2. 身份、权限和审计边界。
3. 四条标准业务流程中的至少三条完整闭环，条件允许时完成四条。
4. 对象、流程、任务、审计和数字员工证据互相追溯。
5. Elsa Workflows 可运行。
6. Hangfire 后台任务可运行。
7. Garnet 至少承担一个明确缓存/锁/限流场景。
8. Nginx 统一入口。
9. AiGateway 受控接缝。
10. 桌面和移动端浏览器 E2E。
11. 数据库迁移、备份恢复、部署和回滚在测试环境验证。
12. 完整交付文档和甲方接手指南。

## 六、SQL 第一检查门

开始任何数据库或业务实现前，只读检查：

```powershell
Get-Service -Name 'MSSQL*' -ErrorAction SilentlyContinue
Get-NetTCPConnection -LocalPort 1433 -State Listen -ErrorAction SilentlyContinue
```

### 服务不存在或没有 Listen

1. 不尝试连接。
2. 不尝试启动或修复服务。
3. 不重启电脑。
4. 不使用 SQLite、LocalDB、内存数据库、Docker 或 PostgreSQL 替代。
5. 更新：
   - `task_plan.md`
   - `findings.md`
   - `progress.md`
   - `docs/004-交付说明/SQL_BLOCKED.md`
6. 输出：
   - 本轮目标状态：`FULL_STAGE_BLOCKED_WAITING_SQL`
   - 整个 AIDT 项目：未完成
7. 结束本轮。

### 服务存在且 1433 Listen

继续：

1. 确认 SQL Server 实例名和版本。
2. 确认应用连接串。
3. 使用应用实际 SQL 客户端连接。
4. 校验目标数据库和当前迁移状态。
5. 记录可复查的连接证据。

`sqlcmd` 不在 PATH 不是独立阻断条件，可使用项目实际 SQL 客户端。

## 七、执行阶段

### 阶段 1：恢复基线与影响分析

- 读取全部必需文件。
- 检查工作区、依赖锁定、迁移状态和当前测试状态。
- 更新计划文件，写清本轮完整目标、非目标和风险。
- 建立功能地图上下文包。
- 不重新执行已完成的阶段 A 工作。

### 阶段 2：SQL Server 与迁移

- 连接真实 SQL Server 2025。
- 如目标库已有数据，先备份并记录恢复点。
- 按数据库迁移规范执行 `InitialSchema`。
- 校验 Schema、表、主键、外键、索引、约束和迁移版本。
- 更新迁移状态。
- 记录精确执行命令、结果和数据库版本。
- 迁移失败立即停止后续启动，不得带病继续。

### 阶段 3：持久化与事务

- 完成 EF Core SQL Server DbContext。
- 完成 Repository。
- 完成 UnitOfWork。
- 完成并发控制。
- 完成幂等记录。
- 完成审计和 Outbox 同事务写入。
- Controller 不直接访问 DbContext。
- 不在事务中调用外部服务。
- 不用内存实现冒充持久化。

### 阶段 4：身份、权限和审计

- 完成首期登录和可信身份边界。
- 完成角色、接口权限、组织范围、数据范围和字段脱敏。
- 未登录、无菜单、无 API 权限和无数据范围都必须拒绝。
- 外发令牌、Cookie、CSRF、安全头按安全规范实现。
- 审计记录只追加。
- 高风险动作保留人工确认。
- 若真实企业 SSO 凭据未提供，实现经明确标注的 OIDC 适配接缝和本地测试身份，不宣称生产 SSO 已打通。

### 阶段 5：Elsa Workflows

- 完成 Elsa 与 .NET 10、SQL Server 2025 的 PoC。
- 流程实例绑定冻结的流程版本。
- 验证条件、通过、退回、终止、任务生成和时间线。
- 验证流程历史实例不受新版本定义影响。
- 不使用 Elsa Studio 作为业务前端。
- 如需业务化流程配置，使用 Vue 调用 AIDT 契约。
- 兼容性失败时记录证据并写 ADR，不能偷偷更换第二套引擎。

### 阶段 6：业务流程闭环

按功能地图实现：

1. `WF-001` 客户样品到研发转产。
2. `WF-002` 批次异常到 CAPA。
3. `WF-003` 请购到付款。
4. `WF-004` 客诉到 CAPA。

最低完成三条；若剩余时间与依赖允许，完成第四条。

每条流程必须具备：

- 业务对象唯一主记录。
- 流程实例引用对象。
- 自动生成待办。
- 通过、退回和终止。
- 至少一个条件分支。
- 对象状态和版本变化。
- 审计证据。
- SQL Server 持久化。
- 任务、对象、流程和审计互相跳转。

禁止用静态页面、固定 JSON 或纯内存状态冒充完成。

### 阶段 7：Hangfire、Outbox 与 Garnet

- Hangfire 使用 SQL Server 持久化。
- 实现至少一个幂等后台任务。
- 实现 Outbox Worker、重试和失败记录。
- Garnet 至少用于一个明确场景：缓存、短期锁或限流。
- 缓存不得成为主存储。
- 验证服务重启后任务和 Outbox 状态可恢复。

### 阶段 8：连接器与 AiGateway

- U9、ERP、DCS、钉钉、WMS/LIMS 使用 Mock。
- Mock 与真实连接必须在 API、UI 和日志中区分。
- 外部写命令采用 Outbox、幂等键、回执和补偿结构。
- AiGateway 至少提供模型白名单、超时、审计和不可用降级接缝。
- 不调用真实外部模型。
- 数字员工使用规则化模拟结果，不冒充真实 AI 结论。

### 阶段 9：前端真实接入

- 工作台展示真实汇总。
- 我的待办读取真实任务。
- 流程、对象、审计和 Agent 页面调用真实 API。
- 完成 loading、empty、success、error、permission denied 状态。
- 完成字段权限和脱敏展示。
- 高风险操作二次确认。
- 桌面和 390px 移动端均可操作。
- 错误信息显示 TraceId。
- 前端必须明确标识 Mock、未连接和真实状态。

### 阶段 10：完整测试

执行：

- Domain 单元测试。
- Application 用例测试。
- SQL Server 集成测试。
- API 契约测试。
- 权限越界测试。
- 并发与幂等测试。
- 迁移升级和回滚测试。
- 备份恢复测试。
- 前端类型检查。
- 前端组件测试。
- 浏览器 E2E。
- 桌面与移动端截图证据。
- 离线依赖和构建验证。

不得修改测试来掩盖错误。

### 阶段 11：Nginx 与部署

- 验证 Nginx 作为唯一入口。
- 内部服务只监听 `127.0.0.1`。
- 验证健康检查。
- 验证版本目录。
- 验证数据库备份和迁移执行顺序。
- 验证启动失败停止和回滚。
- 生成可复现发布包和校验和。
- 只在测试环境验证，不部署真实生产。

### 阶段 12：文档和交接

更新：

- 功能地图。
- 模块 README。
- OpenAPI。
- 数据库 Schema 和迁移状态。
- 启动、部署、备份和恢复说明。
- 权限矩阵。
- Mock/真实边界。
- 测试证据。
- 已知限制。
- `task_plan.md`
- `findings.md`
- `progress.md`
- 全阶段交付报告。
- 会话交接。

## 八、硬边界

- 技术栈保持冻结。
- 不引入第二前端、第二后端、第二数据库或第二流程引擎。
- 不直接写其他模块表。
- 不绕过权限、审计、事务、幂等和数据库迁移。
- 不连接真实外部业务系统。
- 不产生付款、放行、配方变更、设备控制或真实对外消息。
- 不把 Mock 说成真实集成。
- 不把本地测试身份说成正式 SSO。
- 不把阶段完成说成整个生产系统完成。

## 九、缺失可选组件时的处理

- 如果 Nginx、Garnet 或 Hangfire 因环境缺少可执行文件而无法完整验证，先完成代码、配置和测试接缝，并记录精确缺口。
- 核心验收依赖 SQL Server 时，SQL Server 缺失是硬阻断。
- 非核心组件缺失时，记录 `integration_pending_environment`，继续完成其他工作，不得伪造运行成功。

## 十、全阶段完成条件

只有以下全部满足，才能输出 `FULL_STAGE_COMPLETE_DEMO_READY`：

- SQL Server 连接和迁移通过。
- 34 张表结构校验通过。
- 持久化、事务、并发、幂等和审计通过。
- 至少三条业务流程完成持久化闭环。
- 条件、退回、终止可验证。
- Elsa 流程运行并可回放。
- Hangfire、Outbox 和 Garnet 场景验证通过，或其环境缺口已明确记录且不影响 Demo 核心验收。
- 身份、API 权限、数据范围和字段脱敏通过。
- 前端接入真实 API。
- 桌面和移动端 E2E 通过。
- 备份恢复、部署和回滚在测试环境通过。
- 文档和交接完整。
- 没有未说明的 Mock。

如果只完成部分阶段，状态只能是：

- `FULL_STAGE_IN_PROGRESS`
- `FULL_STAGE_BLOCKED_WAITING_SQL`
- `FULL_STAGE_PARTIAL`

## 十一、最终输出

按以下顺序：

1. 一句话结论。
2. 本轮目标状态。
3. 已完成阶段。
4. SQL Server 与迁移证据。
5. 已实现业务流程。
6. 测试命令和结果。
7. 浏览器 E2E 结果。
8. 部署和恢复结果。
9. 修改文件。
10. 未完成项。
11. 风险和回滚点。
12. 整个 AIDT 项目是否完成。
13. 甲方接手入口。

除非所有完成条件满足，否则必须明确回答“整个 AIDT 项目未完成”。

现在开始执行。先做 SQL Server 第一检查门；通过后不得再次停下等待普通确认，应持续完成全部可执行阶段。
