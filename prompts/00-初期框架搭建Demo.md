# 目标模式提示词：AIDT 生产形态初期框架 Demo

> 当前执行入口提示：如果 SQL Server 安装仍要求重启，或 `MSSQL*` 服务、1433 或数据库连接不可用，请改用 `prompts/07-SQL重启阻断下的分阶段搭建.md`。不要把 SQLite、LocalDB 或 Docker 当替代。

你现在开始为 AIDT 企业业务操作系统搭建“生产形态初期框架 Demo”。该 Demo 必须成为后续正式仓库的可运行纵向切片，不允许建立一次性演示工程或按角色拼接的临时目录。

## 工作目录

项目根目录为当前工作区中的 `coding/`。

不得修改或继续扩展旧的 `AIDT/LOCAL-MVP`。旧 MVP 只作为业务行为和测试参考。

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
20. `coding/task_plan.md`、`findings.md`、`progress.md`

## 目标

在 `coding/` 下建立以下正式结构：

```text
backend/
frontend/
database/
tests/
deploy/
docs/
standards/
templates/
prompts/
```

保留现有文档，不覆盖用户文件。

## 冻结技术栈

- .NET 10 LTS
- ASP.NET Core
- SQL Server 2025
- Vue 3 + TypeScript + Vite + Element Plus + Pinia + Vue Router
- Elsa Workflows
- Hangfire
- Garnet
- Nginx
- AiGateway
- 离线部署包

禁止使用 Java、PostgreSQL、React、Docker、RabbitMQ 或第二套前端/后端框架。

## Demo 范围

### 必须完成

1. ASP.NET Core 模块化单体骨架。
2. API、Application、Domain、Infrastructure、Workflow、Integrations、Worker 项目。
3. Vue 3 前端骨架和统一布局。
4. SQL Server 数据模型与版本化迁移。
5. 身份与权限接口骨架。
6. 业务对象主对象与版本模型。
7. 流程实例、版本、任务、通过、退回和终止。
8. 至少一个条件跳转。
9. 审计事件。
10. 一个数字员工的独立身份、范围、人工确认和运行记录。
11. 一个 Mock 外部 Adapter 和 Outbox 骨架。
12. OpenAPI 契约。
13. 统一错误响应和 TraceId。
14. 核心 API 自动测试。
15. 前端关键路径浏览器测试。
16. `README.md`、启动、迁移和部署说明。

### 首期业务链

优先实现：

1. 客户样品到研发转产。
2. 批次异常到 CAPA。
3. 请购到付款。

如果工作量超出单次目标模式执行能力，先完成一条端到端流程，并把另外两条登记为未完成，不得用假页面冒充完成。

## Elsa 要求

- 先完成最小 PoC。
- Elsa 运行时和 API 可用。
- 流程实例绑定流程版本。
- 不使用 Elsa Studio 作为业务前端。
- 不在同一任务中同时自研第二套工作流引擎。

如果 Elsa 与 .NET 10 或 SQL Server 2025 不兼容：

1. 记录具体错误和证据。
2. 停止继续堆代码。
3. 提供替代方案 ADR 草案。
4. 请求架构确认。

## 数据与安全

- 使用虚构脱敏数据。
- 不连接真实 U9、ERP、DCS、WMS 或钉钉。
- 不调用外部模型。
- 不产生付款、放行、配方修改或真实消息。
- 所有高风险动作只到人工确认。
- Mock、未连接和真实状态必须清晰。

## 执行顺序

### 阶段 1：环境与基线

- 检查 .NET、Node、SQL Server、Nginx、Garnet 可用性。
- 检查端口和目录。
- 建立或更新 `task_plan.md`、`findings.md`、`progress.md`。
- 输出风险和环境缺口。

缺少 SQL Server 2025 时，不得用 SQLite 替代并宣称完成；可以继续生成代码和迁移，但必须明确未完成运行验证。

### 阶段 2：工程骨架

- 建立解决方案、项目引用、集中包版本。
- 建立 Vue 工程。
- 建立统一响应、异常、日志、配置和健康检查。
- 建立数据库项目结构。

### 阶段 3：平台内核

- 身份与权限。
- 对象元模型。
- 业务对象。
- Elsa 工作流。
- 任务动作。
- 审计。
- Outbox 和 Hangfire。

### 阶段 4：业务流程

按 FEATURE_MAP 实现三条流程，优先保证一条完整闭环。

### 阶段 5：前端

- 登录/身份外壳。
- 工作台。
- 待办。
- 流程详情。
- 对象详情。
- 审计。
- Agent。
- 桌面和 390px 移动端。

### 阶段 6：验证

必须运行：

- 后端编译
- 单元测试
- 集成测试
- API 测试
- 前端类型检查
- 前端构建
- 浏览器测试
- 数据库迁移
- 健康检查
- 部署或部署脚本验证

### 阶段 7：文档

更新：

- `FEATURE_MAP.md`
- 模块 README
- OpenAPI
- 数据库说明
- `task_plan.md`
- `findings.md`
- `progress.md`

## Definition of Done

- 一条以上流程完整闭环。
- 条件、退回、终止和审计可用。
- 权限拒绝有测试。
- 数据库迁移可执行。
- 前端关键路径通过。
- 工程可重建。
- Mock 和真实边界清晰。
- 所有未完成项明确记录。

## 最终输出

1. 一句话结论。
2. 启动命令。
3. 已完成范围。
4. 未完成范围。
5. 测试命令和结果。
6. 数据库和迁移说明。
7. 风险。
8. 文件清单。

不要先写长篇计划后停止。持续执行，直到完成可验证 Demo 或遇到真实环境阻塞。
