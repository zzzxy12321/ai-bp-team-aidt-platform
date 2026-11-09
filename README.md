# AIDT 项目参考文档

> **公开发行版**：本仓库是 AI-BP 团队的 AIDT 完整正式系统源码快照，供成员浏览、克隆、下载后在各自环境中部署、测试和研究。  
> 公开仓库的 `main` 分支由仓库所有者维护；其他成员不能直接写入，只能在自己的副本或 Fork 中修改。

> 建立日期：2026-10-29  
> 文档性质：需求、技术栈和实施方案基线  
> 使用原则：需求、生产技术栈和实施计划均已冻结，后续开发在冻结基线内执行。

## 📚 文档目录

| 文档 | 用途 |
|---|---|
| [AGENTS.md](./AGENTS.md) | 所有 AI 开发窗口的唯一主入口 |
| [CLAUDE.md](./CLAUDE.md) | Claude 入口，指向 AGENTS.md |
| [产品规划总入口](./docs/001-产品PRD/README.md) | 项目目标、需求、技术栈、计划与需求池的统一入口 |
| [00-项目目标.md](./docs/001-产品PRD/00-项目目标.md) | 最高优先级目标、仓库定位和交付决策原则 |
| [01-需求基线.md](./docs/001-产品PRD/01-需求基线.md) | 产品目标、业务边界、核心能力、验收口径 |
| [02-技术栈与架构.md](./docs/001-产品PRD/02-技术栈与架构.md) | 已冻结生产技术栈、系统设计、离线交付与技术边界 |
| [03-计划方案.md](./docs/001-产品PRD/03-计划方案.md) | 分阶段实施、里程碑、交付物、风险与验收门槛 |
| [04-需求池.md](./docs/001-产品PRD/04-需求池.md) | 持续收集尚未正式立项的下一阶段产品需求 |
| [standards/README.md](./standards/README.md) | 前后端、数据库、API、安全、测试、Git、部署和 AI 协作规范 |
| [templates/](./templates/) | 功能、变更、Bug、PR 和会话交接模板 |
| [prompts/](./prompts/) | 框架搭建、新功能、Bug、数据库、重构、发布和恢复提示词 |
| [docs/project/FEATURE_MAP.md](./docs/project/FEATURE_MAP.md) | 功能、页面、模块、API、数据表和测试的索引 |
| [批次 1 交付报告](./docs/project/REQUIREMENT_POOL_BATCH_1_DELIVERY.md) | 四个需求、迁移、权限、文件安全、E2E、部署和回滚证据 |
| [MCP-001 交付报告](./docs/project/MCP_001_DELIVERY.md) | 标准 MCP、OAuth、工具/Resources、机器身份、审计、部署与冻结证据 |
| [task_plan.md](./task_plan.md) | 当前任务阶段和未完成事项 |
| [findings.md](./findings.md) | 研究事实、风险和错误 |
| [progress.md](./progress.md) | 已完成操作和测试证据 |

## 🧱 当前工程产物

| 路径 | 内容 |
|---|---|
| [backend/](./backend/) | .NET 10 模块化单体、8 个生产项目、API/Worker/MCP、领域与契约 |
| [frontend/](./frontend/) | Vue 3 + TypeScript + Vite + Element Plus + Pinia + Router |
| [database/](./database/) | SQL Server Schema 设计、EF Core Migration、升级与回滚 SQL |
| [tests/](./tests/) | 领域、应用、契约、迁移模型和 API 冒烟测试 |
| [docs/contracts/](./docs/contracts/) | OpenAPI 与 API 契约说明 |
| [docs/004-交付说明/](./docs/004-交付说明/) | 启动说明和 SQL 阻断证据 |

## ✅ 当前状态

```text
MCP_001_COMPLETE_DEPLOYED_AND_FROZEN
```

- 主库应用 10 条迁移、62 张应用表，包含 `Mcp` 授权状态 Schema。
- 后端/SQL/API 116/116；MCP 单元、OAuth、协议与 stdio 11/11。
- 前端类型、Vitest 14/14、OpenAPI 122 路径/142 operationId、生产构建通过。
- 正式部署桌面/390px + MCP OAuth E2E 19/19。
- 应用回滚、部署备份恢复和部署后 stdio 服务身份调用通过。
- 本结论只表示 MCP-001 完成，整个 AIDT 生产项目仍未完成。

最终测试发布包：

```text
D:\Apps\AIDT-Test\releases\AIDT-req-mcp-final5.zip
SHA256 B949F1D218A7539F8BBA9FC53372B14A92382FD29ADA229D0C1E14F0F1F68FD5
```

## 🛠️ 快速验证

```powershell
dotnet build AIDT.slnx --no-restore
dotnet test AIDT.slnx --no-restore --filter "Category!=RequiresSqlServer"

Set-Location frontend
npm ci
npm run typecheck
npm run test
npm run contract:check
npm run build
```

详细启动说明见 [STARTUP.md](./docs/004-交付说明/STARTUP.md)，全阶段交付见 [FULL_STAGE_DELIVERY.md](./docs/project/FULL_STAGE_DELIVERY.md)。

## 🧭 阅读顺序

1. AI 首先读取根目录 `AGENTS.md`。
2. 再阅读项目目标，确认所有产出必须是甲方可直接复制和维护的正式仓库。
3. 再阅读需求，确认“要做什么、不做什么”。
4. 阅读技术栈与架构，按冻结技术组合实施，偏离必须写 ADR。
5. 阅读规范总索引和当前任务对应的专项规范。
6. 读取功能地图、当前模块 README、计划和进度。
7. 最后阅读计划方案，确认阶段和验收门槛。

## ✅ 最高优先级目标

> **我的代码必须适合直接作为甲方便于复制、部署、接手和长期维护的正式代码仓库结构。**

AIDT Demo 必须是正式仓库的可运行纵向切片，不得被设计成一次性演示目录或按角色拼接的临时资料集合。

## 🎯 当前总体判断

- 产品需求可以落地，核心业务方向成立。
- 现有 MVP 已跑通对象、流程、任务、审批、条件、审计和数字员工确认。
- 当前 `Node.js 原生 HTTP + SQLite + 原生前端` 是验证栈，不能直接作为成熟生产网站。
- AIDT 生产技术栈已冻结为：`.NET 10 + SQL Server 2025 + Vue 3 + Element Plus + Elsa Workflows + Hangfire + Garnet + Nginx + AiGateway + 离线部署包`。
- 需求基线、技术栈与实施计划均已标记为 `FROZEN`，变更必须走 ADR 或变更单。
- 架构采用 Windows 单机内网部署、ASP.NET Core 模块化单体、Vue 3 前端和独立 Adapter，不上微服务、消息队列或 Docker。
- 第一阶段只建议承诺平台底座和三条跨部门样板流程，不建议直接铺满 13 个工作台。

## 🚦 进入真实业务试点的最低门槛

1. 正式 SSO 与 API 级权限校验。
2. 组织、项目、字段和动作级数据范围。
3. 流程定义版本冻结。
4. 不可静默删除的关键审计。
5. SQL Server 2025 生产数据库、版本化迁移和恢复验证。
6. 备份、恢复演练、监控和告警。
7. 外部连接器的幂等、重试、死信、对账和补偿。
8. 敏感数据分级、密钥管理和模型路由规则。

## 🔒 公开使用与生产数据边界

本仓库按公开发行方式提供，允许访问者下载和在自己的副本中修改。真实配方、客户、成本、报价、工艺参数、接口密钥和生产数据仍属于敏感信息，不得提交到本仓库或任何其他公开仓库。
