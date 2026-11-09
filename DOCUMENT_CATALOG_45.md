# AIDT 45-Document Catalog

> 本索引对应 `AIDT-AI开发规范目录说明.xlsx`，并映射到完整版仓库中的当前实际路径。

| # | Type | Document | Current path | Catalog path | Purpose |
|---|---|---|---|---|---|
| 1 | 入口 | AI 开发总入口 | `AGENTS.md` | `coding/AGENTS.md` | 规定所有 AI 窗口的阅读顺序、冻结技术栈、仓库结构、强制工作流、硬边界和完成条件。 |
| 2 | 入口 | Claude 入口 | `CLAUDE.md` | `coding/CLAUDE.md` | 将 Claude 窗口转发到 AGENTS.md，避免两套规则冲突。 |
| 3 | 入口 | 项目文档总索引 | `README.md` | `coding/README.md` | 列出项目基线、规范、模板、提示词、功能地图和项目控制文件的阅读顺序。 |
| 4 | 项目基线 | 项目目标 | `docs/001-产品PRD/00-项目目标.md` | `coding/00-项目目标.md` | 定义代码必须适合甲方复制、部署、接手和长期维护，并给出目标检查问题。 |
| 5 | 项目基线 | 需求基线 | `docs/001-产品PRD/01-需求基线.md` | `coding/01-需求.md` | 定义产品范围、业务对象、流程、数据边界、非功能和验收口径。 |
| 6 | 项目基线 | 冻结技术栈与架构 | `docs/001-产品PRD/02-技术栈与架构.md` | `coding/02-技术栈与架构.md` | 冻结 .NET 10、SQL Server 2025、Vue 3、Elsa 等组合，并说明模块、数据与部署架构。 |
| 7 | 项目基线 | 实施计划 | `docs/001-产品PRD/03-计划方案.md` | `coding/03-计划方案.md` | 定义阶段、90 天里程碑、团队配置、目录规划、规范先行门槛、风险和 DoD。 |
| 8 | 项目控制 | 研究发现 | `findings.md` | `coding/findings.md` | 记录调研事实、风险、规范覆盖结论和待验证事项。 |
| 9 | 项目控制 | 进度日志 | `progress.md` | `coding/progress.md` | 记录已完成操作、测试、验证证据和下一阶段。 |
| 10 | 项目控制 | 任务计划 | `task_plan.md` | `coding/task_plan.md` | 记录当前阶段、任务状态、关键决策、错误和验证结果。 |
| 11 | 功能索引 | AIDT 功能地图 | `docs/project/FEATURE_MAP.md` | `coding/docs/project/FEATURE_MAP.md` | 把功能映射到页面、后端模块、API、数据表、权限、审计和测试。 |
| 12 | 开发规范 | AI 工程宪法 | `standards/00-AI工程宪法.md` | `coding/standards/00-AI工程宪法.md` | 定义 AI 不可违反的原则、任务阅读要求、决策顺序、上下文和修改范围。 |
| 13 | 开发规范 | 上下文阅读与恢复 | `standards/01-上下文阅读与恢复.md` | `coding/standards/01-上下文阅读与恢复.md` | 定义四级阅读、检索策略、上下文预算、中断恢复、三次失败和会话交接。 |
| 14 | 开发规范 | 架构与模块边界 | `standards/02-架构与模块边界.md` | `coding/standards/02-架构与模块边界.md` | 定义 .NET 分层、模块、SQL Schema 所有权、前端分层、Elsa 边界和 ADR 触发条件。 |
| 15 | 开发规范 | 后端开发规范 | `standards/03-后端开发规范.md` | `coding/standards/03-后端开发规范.md` | 规定 C# 命名、Controller/Application/Domain 职责、异常、日志、事务、并发和后台任务。 |
| 16 | 开发规范 | 前端开发规范 | `standards/04-前端开发规范.md` | `coding/standards/04-前端开发规范.md` | 规定 Vue 3 目录、组件、API、Pinia、路由、加载/错误状态、可访问性和测试。 |
| 17 | 开发规范 | 数据库与迁移规范 | `standards/05-数据库与迁移规范.md` | `coding/standards/05-数据库与迁移规范.md` | 规定 SQL Server Schema、命名、类型、约束、JSON、索引、版本和迁移发布。 |
| 18 | 开发规范 | API 与契约规范 | `standards/06-API与契约规范.md` | `coding/standards/06-API与契约规范.md` | 规定 REST、版本、响应、错误码、分页、幂等、并发和 OpenAPI 同步。 |
| 19 | 开发规范 | 安全权限审计配置事务 | `standards/07-安全权限审计配置事务.md` | `coding/standards/07-安全权限审计配置事务.md` | 覆盖身份、RBAC/ABAC、数据分级、审计、日志、密钥、事务、外部写回和数字员工边界。 |
| 20 | 开发规范 | 测试与回归规范 | `standards/08-测试与回归规范.md` | `coding/standards/08-测试与回归规范.md` | 定义单元、集成、API、流程、前端、E2E、离线构建、权限和回滚测试。 |
| 21 | 开发规范 | Git 评审提交规范 | `standards/09-Git评审提交规范.md` | `coding/standards/09-Git评审提交规范.md` | 定义分支、提交信息、PR、评审维度、合并门槛和 AI-assisted 标记。 |
| 22 | 开发规范 | 构建部署回滚离线交付 | `standards/10-构建部署回滚离线交付.md` | `coding/standards/10-构建部署回滚离线交付.md` | 定义版本包、Windows 服务、SQL 迁移、Nginx、离线依赖、健康检查和回滚。 |
| 23 | 开发规范 | 功能地图与影响分析 | `standards/11-功能地图与影响分析.md` | `coding/standards/11-功能地图与影响分析.md` | 定义功能 ID、调用链、模块 README、变更影响和 AI 上下文选择。 |
| 24 | 开发规范 | Bug 修复与重构规范 | `standards/12-Bug修复与重构规范.md` | `coding/standards/12-Bug修复与重构规范.md` | 要求先复现、写失败测试、最小修复、回归邻近功能，并限制重构边界。 |
| 25 | 开发规范 | Vibecoding 工作流 | `standards/13-Vibecoding工作流.md` | `coding/standards/13-Vibecoding工作流.md` | 定义从接任务到上下文、影响分析、编码、测试、自审、文档和交接的标准流程。 |
| 26 | 开发规范 | 完成定义与交付清单 | `standards/14-完成定义与交付清单.md` | `coding/standards/14-完成定义与交付清单.md` | 定义功能、工程、安全、测试、文档、发布和甲方交接完成条件。 |
| 27 | 开发规范 | 规范总索引 | `standards/README.md` | `coding/standards/README.md` | 列出六层规范结构、全部专项规范、12 类企业规范映射和最小启动门槛。 |
| 28 | 模板 | Bug 报告模板 | `templates/BUG_REPORT_TEMPLATE.md` | `coding/templates/BUG_REPORT_TEMPLATE.md` | 记录现象、复现、影响、TraceId、根因、修复和回归。 |
| 29 | 模板 | 变更计划模板 | `templates/CHANGE_PLAN_TEMPLATE.md` | `coding/templates/CHANGE_PLAN_TEMPLATE.md` | 记录目标、非目标、上下文包、预期文件、契约影响、测试和回滚。 |
| 30 | 模板 | AI 上下文包模板 | `templates/CONTEXT_PACK_TEMPLATE.md` | `coding/templates/CONTEXT_PACK_TEMPLATE.md` | 限定 AI 本次只读哪些功能、模块、API、表、测试和禁止内容。 |
| 31 | 模板 | 功能地图条目模板 | `templates/FEATURE_ENTRY_TEMPLATE.md` | `coding/templates/FEATURE_ENTRY_TEMPLATE.md` | 标准化功能 ID、页面、模块、API、表、权限、审计和测试登记。 |
| 32 | 模板 | 模块 README 模板 | `templates/MODULE_README_TEMPLATE.md` | `coding/templates/MODULE_README_TEMPLATE.md` | 为后端或前端模块记录目的、依赖、数据表、API、测试和限制。 |
| 33 | 模板 | PR 检查表 | `templates/PR_CHECKLIST_TEMPLATE.md` | `coding/templates/PR_CHECKLIST_TEMPLATE.md` | 提供架构、API、数据库、安全、测试、文档和证据检查点。 |
| 34 | 模板 | 会话交接模板 | `templates/SESSION_HANDOFF_TEMPLATE.md` | `coding/templates/SESSION_HANDOFF_TEMPLATE.md` | 记录目标、修改、测试、风险、下一步和禁止重复操作。 |
| 35 | 提示词 | 初期框架搭建 Demo | `prompts/00-初期框架搭建Demo.md` | `coding/prompts/00-初期框架搭建Demo.md` | 从零建立正式仓库结构的 AIDT 生产形态 Demo，包含代码、数据库、测试和部署。 |
| 36 | 提示词 | 新功能开发 | `prompts/01-新功能开发.md` | `coding/prompts/01-新功能开发.md` | 要求 AI 先查功能地图、做影响分析，再实现契约、后端、前端和测试。 |
| 37 | 提示词 | Bug 修复 | `prompts/02-Bug修复.md` | `coding/prompts/02-Bug修复.md` | 要求复现、定位根因、写失败测试、最小修复和邻近回归。 |
| 38 | 提示词 | 数据库变更 | `prompts/03-数据库变更.md` | `coding/prompts/03-数据库变更.md` | 要求设计表、约束、索引、迁移、回滚、测试和 DBA 审核。 |
| 39 | 提示词 | 重构 | `prompts/04-重构.md` | `coding/prompts/04-重构.md` | 要求基线测试、单一边界、小步修改、行为等价和 ADR。 |
| 40 | 提示词 | 发布前审查 | `prompts/05-发布前审查.md` | `coding/prompts/05-发布前审查.md` | 要求审查代码、契约、安全、测试、离线构建、部署和回滚，输出 Go/No-Go。 |
| 41 | 提示词 | 会话恢复与交接 | `prompts/06-会话恢复与交接.md` | `coding/prompts/06-会话恢复与交接.md` | 要求从磁盘计划和 Git 实际状态恢复，禁止凭记忆重做。 |
| 42 | 提示词 | 提示词索引 | `prompts/README.md` | `coding/prompts/README.md` | 列出框架搭建、新功能、Bug、数据库、重构、发布和会话恢复提示词。 |
| 43 | 提示词 | SQL 重启阻断下的分阶段搭建 | `prompts/07-SQL重启阻断下的分阶段搭建.md` | `coding/prompts/07-SQL重启阻断下的分阶段搭建.md` | SQL Server 安装等待重启时，先完成可编译、可测试的工程与契约；明确 SQL 边界，重启后继续迁移、集成测试和端到端验证。 |
| 44 | 提示词 | 阶段 B - SQL 恢复后继续 | `prompts/08-阶段B-SQL恢复后继续.md` | `coding/prompts/08-阶段B-SQL恢复后继续.md` | 阶段 A 完成后，先验证 SQL Server 服务、1433 与应用连接，再执行迁移、SQL 集成测试、Elsa/Hangfire/Garnet 接缝、业务持久化闭环和浏览器 E2E。 |
| 45 | 提示词 | AIDT 全阶段目标模式 | `prompts/09-AIDT全阶段目标模式.md` | `coding/prompts/09-AIDT全阶段目标模式.md` | SQL 就绪后，用一个持续执行的目标模式任务书完成迁移、持久化、权限、Elsa、业务流程、Hangfire/Garnet、前端 E2E、部署和交付。 |

**Total: 45 documents. All mapped paths were verified in this repository.**
