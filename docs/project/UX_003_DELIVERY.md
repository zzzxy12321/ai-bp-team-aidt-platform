# UX-003 统一工作事项中心交付报告

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`  
> 功能：`UX-003` 统一工作事项中心  
> 发布：`req-ui-step1-final1`  
> 冻结标签：`req-ui-step1-frozen-v1`

## 🎯 结论

普通员工第一次打开“我的工作”时，可以在同一个业务语言界面看到流程待办、项目任务、
事项确认、会议行动项、智能体接入确认和数字员工运行确认。每个事项都说明当前阶段、
责任人、截止时间、风险、下一步和来源，并进入原有业务详情执行动作。

本批只扩展只读工作事项投影，不创建第二套业务状态，不改变审批、确认、审计、幂等或
真人边界。没有新增数据库迁移，没有修改 MCP 工具或外部系统接入。

## 🧭 产品结构

“我的工作”固定为六个层次：

1. **今日焦点**：统一排序后的第一项，给出最需要立即处理的事项和下一步。
2. **待我处理**：当前用户有权处理或作为冻结候选人的事项。
3. **我发起**：当前用户发起并仍需跟进的事项。
4. **我参与**：当前用户参与、但未必需要立即动作的事项。
5. **需要关注**：永久人工边界、逾期、今天到期或等待确认的高优先事项。
6. **最近来源证据**：有文件读取权限时的最近业务证据，正文仍受保密等级和扫描状态控制。

统一排序固定为：

```text
永久人工边界 → 已逾期 → 今天到期 → 待我确认 → 本周 → 其他
```

## 🔗 六类来源

| 来源 | 业务语言 | 详情入口 | 去重规则 |
|---|---|---|---|
| `WorkflowTask` | 流程待办 | `/workflows?instance=…&task=…` | 当前处理人或冻结候选人优先 |
| `ProjectTask` | 项目任务 | `/projects/{id}?task=…` | 关联当前用户可见流程任务时让位给流程待办 |
| `EventInbox` | 事项确认 | `/inbox?event=…` | 已生成项目任务的事件不再作为主事项重复展示 |
| `MinutesActionItem` | 会议行动项 | `/minutes?source=…&actionItem=…` | 仅展示待确认行动项 |
| `McpHumanConfirmation` | 智能体接入确认 | `/mcp-confirmations?confirmationId=…` | 仅本人、组织内、未过期且待确认 |
| `AgentRun` | 数字员工确认 | `/agents?run=…` | 仅本人发起且等待人工确认的运行 |

内部 ID 仅用于前端路由和后端定位，不显示在员工主卡片。页面不展示 TraceId、英文节点代码、
数据库表名或内部 revision。

## 🛡️ 权限与人工边界

- 每个来源先检查自身读取动作权限，再应用组织、部门和环境范围。
- 流程任务仍要求当前处理人或冻结候选人；聚合不扩大审批资格。
- 项目任务必须属于当前用户可见项目。
- 事件和听记必须属于授权部门；事件确认仍要求原确认权限。
- MCP 确认只对绑定用户、当前组织、`Pending` 且未过期的记录可见。
- 数字员工运行只对发起人和授权部门可见，确认动作仍要求原确认权限。
- 永久人工边界只展示为高优先事项，不能由聚合页直接代为确认。
- 所有实际动作继续进入原详情 API，由后端重新执行权限、审计和幂等。

## 🧱 实现边界

- 新增 `WorkItemViewDto` 与 `MyWorkSummaryDto`。
- `MyWorkDto` 提供统一事项和来源证据，不再返回旧的混合任务结构。
- 状态中文映射、风险标签、优先级桶、周界、排序和去重放入纯展示规则。
- SQL 服务只读聚合六类来源，无业务写入，无新表、字段或迁移。
- 前端新增统一事项卡，并重构“我的工作”的信息层级和 390px 布局。
- 事件、听记、MCP 和 Agent 详情页支持查询参数聚焦，不复制详情实现。
- OpenAPI 更新为 122 路径、142 operationId；MCP 冻结接口和工具没有改变。

## 🧪 验收结果

| 门禁 | 结果 |
|---|---|
| 后端构建 | 0 警告、0 错误 |
| SQL/API 集成 | 127/127 |
| MCP 测试 | 11/11 |
| 后端合计 | 138/138，0 失败、0 跳过 |
| 后端格式 | 通过 |
| NuGet 漏洞 | 无已知漏洞 |
| 前端类型 | 通过 |
| Vitest | 11 个文件、16/16 |
| OpenAPI | 122 路径、142 operationId |
| 生产构建 | 通过 |
| npm 生产依赖 | 0 漏洞 |
| 源码 Playwright | 22 通过、1 个部署专用用例跳过 |
| 正式部署 Playwright | 23/23，0 失败、0 跳过、0 flaky |

正式 E2E 覆盖：

- 六类待处理来源真实聚合。
- 事件与生成任务、项目任务与流程任务去重。
- 今日焦点、待我处理、我发起、我参与、需要关注和来源证据。
- 点击六类事项进入正确现有详情。
- 完成后移出待处理。
- 无权限范围不泄露事项。
- 刷新和重新登录结果一致。
- 1440、1280 和 390px 布局可用。
- MCP OAuth、作用域、撤销和真人确认不回归。

## 🚀 发布与部署

- 功能提交：`a7b2356`。
- 非快进合并：`cf9ad3a4f53008f6a122f55992bea04baa72818d`。
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step1-final1.zip`。
- SHA256：`B38C51ED44EC9E40719A3DC5B65A0212AC47F1AF3B8CE1D5F1C533E498834D4E`。
- manifest 源码：`cf9ad3a4f53008f6a122f55992bea04baa72818d`，不含 `-dirty`。
- 正式版本目录：`D:\Apps\AIDT-Test\versions\req-ui-step1-final1`。
- 页面、API live/ready、Worker、AiGateway、MCP live/ready、capabilities 全部 HTTP 200。

发布包从干净合并提交构建；本报告在部署验收后归档，不用于替代 manifest 的源码追溯。

## ↩️ 回滚与恢复

- 应用回滚：`req-ui-step1-final1 → req-mcp-final5 → req-ui-step1-final1`。
- 两段回滚均完成页面、API、Worker、AiGateway、MCP 和 capabilities 健康检查。
- 部署备份通过 `RESTORE VERIFYONLY`。
- 最终部署备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-step1-final1_20261102_003647.bak`。
- 最终备份 SHA256：`45C9F686EE7ABB25211D1F54F35022E144267458349A1A534A7CB164C664C899`。
- 独立恢复库：`AIDT_UI_STEP1_FINAL1_RESTORE_20261102_003700`。
- 恢复结果：ONLINE、10 条 EF 迁移、62 张应用表、5 张 MCP 表；
  六类事项所需来源表、项目经理流程任务权限均存在。

## 📌 保留边界

- 当前仍是本地演示身份，不是生产 SSO/MFA。
- 不连接真实钉钉、U9、ERP 或 DCS。
- AiGateway 仍为 RuleSimulator，不调用真实外部模型。
- 聚合页只提供导航，不替代原详情页动作和审计。
- 若后续改变事项来源、权限或去重语义，必须新建需求批次并重新执行全量门禁。

## 🧾 关键文件

- `backend/AIDT.Application/WorkItems/WorkItemPresentation.cs`
- `backend/AIDT.Infrastructure/Services/SqlEmployeeWorkspaceService.cs`
- `frontend/src/pages/MyWorkPage.vue`
- `frontend/src/features/work-items/components/UnifiedWorkItemCard.vue`
- `frontend/e2e/aidt.spec.ts`
- `docs/project/TEST_EVIDENCE.md`
- `docs/contracts/openapi/v1.json`
