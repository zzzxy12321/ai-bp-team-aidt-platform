# SCN-001-B 岗位业务场景中心前端与试用闭环交付报告

状态：`COMPLETE_DEPLOYED_AND_FROZEN`

## 🎯 结论

7B 已基于 7A 真实场景后端建设 `/scenarios`，覆盖场景发现、详情、七步发起、
“我的场景”、现有业务证据深链和配置治理。页面不维护第二套对象、流程、任务、
权限、文件、审计或 Agent 状态。

## 🧩 六类实际路径

| 场景 | 发起输入 | 真实业务链 |
|---|---|---|
| 研发样品到转产 | 研发部门、样品编号、客户/产品/需求 | 创建 `CUSTOMER_SAMPLE`，启动 `CUSTOMER_SAMPLE_TO_PRODUCTION`，生成真实首任务 |
| 采购请购到到货验收 | 采购对象、金额/用途、报价附件 | 创建 `PURCHASE_REQUEST`，进入 `PURCHASE_TO_PAYMENT`，付款保持人工 |
| 财务付款人工复核 | 选择已有采购对象、发票/预算/验收材料 | 启动 `FINANCE_PAYMENT_REVIEW`，等待授权财务真人 |
| 部门经理事项分派与审批 | 事项标题、原文、附件/听记 | 创建 `PMO.EventInbox`，等待经理确认负责人、期限和后续任务/流程 |
| 项目经理里程碑与风险 | 项目、负责人、任务内容、期限 | 在现有项目创建 `PMO.ProjectTask` 并保留来源证据 |
| 质量异常到 CAPA | 质量偏差对象、批次证据 | 创建 `QUALITY_DEVIATION`，启动 `BATCH_DEVIATION_CAPA`，质量放行保持人工 |

## 🖥️ 前端闭环

- `/scenarios` 首页：搜索、部门、岗位/角色、分类、推荐、发起历史、进行中、卡片和可用状态。
- 卡片业务语言：名称、适用岗位、说明、步骤数、材料、审批、数字员工、风险与 Demo 标识。
- 场景详情：业务目标、准备项、责任岗位、动作、完成条件、文件/听记、AI 能力与人工边界、完成后证据。
- 七步向导：场景确认 → 业务信息 → 文件与听记 → 审批路径 → 权限风险 → 确认发起 → 结果和下一步。
- 我的场景：当前步骤、等待人、截止、材料、缺失项、流程/任务状态、项目、Agent 建议、下一步和审计入口。
- 配置治理：草稿、校验、发布、停用、恢复、不可变版本和回滚；高风险发布进入双人审核。
- 入口联动：左侧主导航、我的工作、部门/岗位工作台和配置中心进入同一个 `/scenarios`。

## 🔌 后端接缝

- 新增 `GET /api/v1/scenarios/launches` 当前用户启动摘要。
- 聚合以 `ScenarioLaunch` 为索引，实时联查当前活动 `WorkflowTask`、`WorkflowInstance`、
  `ProjectTask`、`EventInbox`、`AgentRun`、`BusinessObject` 和 `User`。
- 启动本身继续调用 7A `POST /actions/start`，由现有对象、流程、事件和项目任务服务执行。
- 当前节点变化后“我的场景”会读取新的活动任务，不继续显示首次任务。

## 🗄️ 数据库

- 本批不新增迁移，继续使用 017 的 `Scenario` 3 表。
- 场景配置与启动记录写入 SQL Server；历史流程和任务仍由原模块持有。

## 🧪 源码门禁

- .NET 构建：0 警告、0 错误。
- `dotnet format --verify-no-changes`：通过。
- 隔离库后端/SQL/API：181/181。
- MCP：11/11。
- 前端类型检查：通过。
- Vitest：22 个文件、45/45。
- OpenAPI：180 路径、201 operationId。
- 生产构建：通过。
- 源码 E2E：49/49 通过；1 条部署专用 MCP OAuth 用例按设计跳过。

## 🚀 正式发布门禁

- 发布版本：`req-ui-step7-final1`
- 冻结标签：`req-ui-step7-frozen-v1`
- 正式部署：`D:\Apps\AIDT-Test\versions\req-ui-step7-final1`。
- 发布包：
  `D:\Apps\AIDT-Test\releases\AIDT-req-ui-step7-final1.zip`
- 发布包 SHA256：
  `19332AC8E38C5472D597E85DFE809C767CD458EC56EB7B9A97A3F26C219A8259`
- manifest 源提交：`da404f9f412d0f66845c0250f2061b86150f2818`，无 `-dirty`。
- 正式 Nginx 部署 E2E：50/50，0 失败、0 跳过、0 flaky。
- 应用回滚：`req-ui-step7-final1 → req-ui-step7a-final1 → req-ui-step7-final1`；
  回滚、重部署和场景专项 E2E 均通过，最终健康端点全部 200。
- 最终数据库备份：
  `C:\ProgramData\AIDT\Backups\AIDT_SCN_001_B_FINAL1_20261103_1700.bak`
- 备份 SHA256：
  `0E1E2274D0C2823273A59994A0DB5E3752D37A12D3CC268B134D83F314B9A22A`
- 独立恢复库：
  `AIDT_SCN_001_B_FINAL1_RESTORE_20261103_1700`，ONLINE、17 迁移、71 应用表、
  6 个基础场景、6 条启动映射、7 个发布版本、5 项场景权限、13 条场景审计。

## 🏷️ 冻结

- 冻结标签：`req-ui-step7-frozen-v1`
- 历史标签和发布包未移动、未覆盖。

## 🚧 风险与未完成

- 当前为本地受控 Demo；未接真实钉钉、U9、ERP、DCS 和外部模型。
- 本地 Cookie 演示身份不代表生产 SSO。
- 业务配置只能引用注册资源；仍需要甲方在真实试点前完成组织、流程和身份源联调。
- 真实配方、客户、成本和生产数据不得进入 Demo 或测试样例。

