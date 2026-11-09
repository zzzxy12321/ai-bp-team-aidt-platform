# 目标模式提示词：SQL 重启阻断下的分阶段框架搭建

你现在开始 AIDT 初期框架搭建。当前环境中的 SQL Server 2025 尚未完成安装，明确存在“必须重新启动计算机”的阻断。

## 目标模式执行协议

这是一项目标模式持续执行任务，不是方案咨询，也不是只输出计划。

- 你必须直接检查磁盘、代码、环境和规划文件，自主完成阶段 A。
- 开始后不要要求用户重新确认已冻结的需求、技术栈、目录或规范。
- 不要先写一大段计划就停止；计划写入文件后立即执行。
- 遇到不确定实现，先从文档、代码和测试中查证，采用符合冻结基线的最小合理假设，并记录到 `findings.md`。
- 每完成一个阶段，更新 `task_plan.md`、`findings.md`、`progress.md`。
- 上下文可能被压缩；恢复时以磁盘文件、Git 状态和实际测试结果为准，不凭记忆重做。
- 当前运行目标是完成阶段 A，不是完成整个 AIDT 项目。
- 只有达到已确认的 SQL 外部阻断边界，或阶段 A 已全部完成且有证据时，才可结束本轮。
- 不得因为 SQL Server 未就绪而停止不依赖数据库的工作。
- 不得把本轮结束表述为整个目标完成。
- 第二天重启后再次运行本提示词时，先恢复已有进度：阶段 A 未完成则继续补完；阶段 A 已完成则进入阶段 B。
- 同一错误最多进行三次不同尝试；第三次失败后保存证据并请求用户指导。

## 当前已知事实

- .NET SDK：`10.0.403` 可用。
- Node.js：`24.18.0` 可用。
- SQL Server 安装目录存在。
- SQL Server 安装日志报错：`3010`，提示“需要重新启动计算机”。
- 当前没有可用的 `MSSQL*` Windows 服务。
- TCP `1433` 未监听。
- `sqlcmd` 不在 PATH 中。
- 用户当前通过向日葵远程操作，不在公司电脑旁。

因此本任务采用两阶段执行：

1. **现在**：完成所有不依赖 SQL Server 运行环境的工程、契约、代码、单元测试和前端工作。
2. **公司电脑重启且 SQL Server 就绪后**：继续执行数据库迁移、SQL Server 集成测试、端到端验证和部署验证。

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
16. `coding/standards/10-构建部署回滚离线交付.md`
17. `coding/standards/13-Vibecoding工作流.md`
18. `coding/standards/14-完成定义与交付清单.md`
19. `coding/docs/project/FEATURE_MAP.md`
20. `coding/task_plan.md`
21. `coding/findings.md`
22. `coding/progress.md`

## 绝对禁止

- 不尝试重启当前电脑。
- 不尝试启动不存在的 SQL Server 服务。
- 不重新安装或修复 SQL Server。
- 不使用 SQLite、LocalDB、PostgreSQL、内存数据库或 Docker 替代 SQL Server。
- 不连接真实 U9、ERP、DCS、钉钉或外部模型。
- 不修改冻结技术栈。
- 不把未执行的 SQL Server 迁移、集成测试或部署说成已通过。
- 不生成与正式仓库不一致的临时后端结构。

## 阶段 A：现在可以完成

### A1. 环境与变更计划

- 更新 `task_plan.md`，登记 SQL Server 为外部阻断。
- 更新 `findings.md`，记录 `3010`、无服务、1433 未监听、sqlcmd 缺失。
- 在 `progress.md` 记录当前环境和阶段 A 范围。
- 生成本次变更计划和上下文包。

### A2. 正式后端工程骨架

在 `coding/backend/` 创建：

```text
AIDT.Api
AIDT.Application
AIDT.Domain
AIDT.Infrastructure
AIDT.Workflow
AIDT.Integrations
AIDT.Worker
```

要求：

- `net10.0`
- 固定项目引用方向
- 集中 NuGet 版本
- 全局配置和启动校验
- 统一响应
- 全局异常
- TraceId
- 健康检查端点
- OpenAPI
- 不依赖 SQL Server 即可编译

### A3. 前端工程骨架

在 `coding/frontend/` 创建 Vue 3 + TypeScript + Vite + Element Plus + Pinia + Vue Router。

要求：

- 页面、features、components、api、stores 分层
- 统一 HTTP client
- 统一错误状态
- 身份和权限路由骨架
- 工作台、流程、对象、审计和 Agent 页面骨架
- 不把 Mock 描述为真实后端

### A4. 领域与契约

完成不依赖数据库运行的：

- DTO
- 领域模型
- 状态枚举
- 错误码
- OpenAPI 初稿
- 身份权限模型接口
- 业务对象主对象接口
- 流程任务接口
- 审计接口
- Agent 运行接口
- 连接器 Outbox 契约

### A5. 数据库设计文件

可以完成：

- SQL Server Schema 设计
- 表结构设计
- 字段、主键、外键、索引和约束设计
- EF Core Migration 代码
- 数据库发布与回滚脚本

但：

- 不执行迁移。
- 不连接 SQL Server。
- 将迁移状态标记为 `authored_not_applied`。
- 不宣称数据库已创建或验证。

### A6. 不依赖 SQL Server 的测试

执行：

- Domain 单元测试
- Application 用例测试，使用接口 Mock
- DTO/验证测试
- 错误码测试
- 前端类型检查
- 前端构建
- 前端组件测试
- OpenAPI 静态校验

集成测试可以编写，但必须标记为等待 SQL Server，不能默认假装通过。

### A7. 文档

更新：

- 模块 README
- FEATURE_MAP
- API 契约说明
- 数据库设计说明
- 启动说明
- 当前阻断说明
- task_plan/findings/progress

## 阶段 A 的 SQL 边界

遇到以下任一操作时必须停止，不继续尝试：

- 启动 SQL Server
- 执行数据库迁移
- 打开 SQL Server 连接
- 运行依赖 SQL Server 的集成测试
- 生成需要真实数据库的性能结果
- 验证备份恢复
- 验证端到端持久化

停止时写入：

```markdown
## SQL BLOCKED

- 原因：SQL Server 安装要求重启，错误码 3010
- 已检查：服务、1433、sqlcmd
- 未执行：迁移、集成测试、数据库端到端验证
- 继续条件：公司电脑重启后 SQL Server 服务可用，1433 监听，应用连接成功
```

## 阶段 B：重启后继续

在公司电脑重启并具备 SQL Server 后，再次运行同一提示词。先执行：

1. 检查 `MSSQL*` 服务状态。
2. 检查 TCP 1433。
3. 检查 SQL Server 版本和实例名。
4. 使用应用连接字符串完成连接测试。
5. 如果连接失败，记录具体错误并停止。
6. 连接成功后执行数据库迁移。
7. 运行 SQL Server 集成测试。
8. 完成三条业务流程的持久化闭环。
9. 运行浏览器 E2E。
10. 验证备份恢复和部署脚本。

## 验收标准

### 阶段 A 完成

- 后端和前端工程可以编译/构建。
- 不依赖 SQL Server 的测试通过。
- 数据库迁移已编写但未执行。
- OpenAPI、领域模型和前端骨架一致。
- SQL 阻断已记录。
- 未使用替代数据库。

### 阶段 B 完成

- SQL Server 2025 可连接。
- 迁移成功。
- 集成测试通过。
- 三条流程至少一条完整端到端。
- 条件、退回、终止和审计可验证。
- 前端关键路径通过。
- 部署与回滚可验证。

## 最终输出

分别列出：

1. 当前已完成。
2. 当前未完成。
3. SQL Server 阻断证据。
4. 已执行测试和结果。
5. 等待重启后执行的事项。
6. 继续所需命令和检查点。
7. 修改文件。

输出时还必须明确：

- 本轮目标状态：`PHASE_A_COMPLETE_WAITING_SQL` 或 `PHASE_A_IN_PROGRESS` 或 `PHASE_B_COMPLETE`。
- 整个 AIDT 目标是否完成：在 SQL 验证完成前必须回答“未完成”。
- 下一次运行同一提示词时，应执行的第一项检查。

持续执行完成阶段 A。不要因为 SQL Server 不可用而停工，也不要越过 SQL 边界伪装完成。
