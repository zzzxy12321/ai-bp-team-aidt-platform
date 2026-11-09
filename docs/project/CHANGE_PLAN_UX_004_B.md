# UX-004-B 统一确认中心与剩余确认来源变更计划

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`
>
> 功能：`UX-004` 业务可编辑的人工确认中心（2B）
>
> 分支：`feature/ui-step2b-confirmation-center`
>
> 基线：`main@48a1a10`
>
> 合并提交：`931f9531f71d58b189fb83941e30195edd30f6ef`
>
> 正式发布：`req-ui-step2-final1`
>
> 冻结标签：`req-ui-step2-frozen-v1`

## 🎯 目标

在 2A 事件确认基础上，完成事件、听记、MCP、Agent 四类人工确认的统一入口，
让业务人员可以阅读、补充、修改并追溯人工决定。

## 🧱 实现范围

1. 新增 `/confirmations` 页面，聚合四类确认来源。
2. 支持 `type + id` 深链；员工工作台中的四类待确认入口改指向统一页面，
   原详情深链保持可用。
3. 听记行动项补齐负责人、截止、优先级、业务对象、项目、流程版本、
   已有流程和可编辑内容。
4. 听记草稿允许保存不完整信息；缺负责人或截止时禁止确认创建任务。
5. MCP 确认升级为业务标题、资源名称、项目上下文、执行/不执行内容和折叠技术信息。
6. MCP 通过和拒绝均必填意见；保持单条确认、无令牌展示。
7. Agent 只允许真人确认；永久人工边界继续阻断自动确认。
8. 统一页覆盖 Loading、Empty、Error、Denied、Expired、Pending、Confirmed、
   Rejected、NeedsInformation。

## 🗄️ 数据与接口

- 迁移 011 为 `MinutesActionItem` 增加 `ProjectId`、`BusinessObjectId`、
  `WorkflowInstanceId`；为 `MinutesTask` 增加 `ProjectId`。
- 不新增表；应用表从 62 张保持为 62 张。
- 听记更新/确认请求增加项目、业务对象和已有流程字段。
- 听记候选接口返回当前授权部门有效真人。
- MCP 自有确认读取增加业务视图 DTO；技术字段仅嵌套返回并由前端折叠。
- 统一确认页使用现有领域 API 组合，不建立第二套确认状态。

## 🔐 权限和原子性

- 听记保存要求 `minutes:update`，确认要求 `minutes:approve`。
- 项目、业务对象和流程必须属于听记部门与环境。
- 任务、行动项、来源、项目关系和审计在同一事务中保存。
- MCP 决策仅允许绑定真人、单条、Pending 且未过期记录。
- Agent 服务身份不能确认自身运行。

## 🧪 验收

- 修改听记负责人/截止后确认，刷新保持。
- 听记关联项目、业务对象和已有流程后在任务和项目证据中可反查。
- MCP 显示业务标题和资源名称，技术信息折叠，通过/拒绝均强制意见。
- 四类来源及深链、无权限、过期、重复确认和冲突有回归。
- 1440、1280、390px 均可用。
- 2A 后端 143/143、源码 25+1、正式 26/26 不回归。
- 全量门禁、迁移 up/down/re-up、发布、部署、回滚、恢复和冻结。

## 🚦 实施阶段

1. [x] 计划、需求池和迁移源文件
2. [x] 听记编辑/确认领域与接口
3. [x] MCP 业务化确认和技术信息折叠
4. [x] `/confirmations` 统一页面、深链和工作台入口
5. [x] 单元、SQL/API、Vitest、OpenAPI
6. [x] 1440/1280/390px E2E 与异常状态
7. [x] 全量门禁、发布、部署、回滚、恢复和冻结

## 🚀 最终结果

- 功能提交：`95ddeb3`、`f69cf15`、`8d3afbd`。
- 非快进合并：`931f9531f71d58b189fb83941e30195edd30f6ef`。
- 正式发布包 SHA256：
  `4832A05B0350D79E3C33E2438B89FE980A979D5D6A4508ED1F3D3AFD50F2C189`。
- 正式 E2E：29/29，0 失败、0 跳过、0 flaky。
- 应用回滚：`req-ui-step2-final1 → req-ui-step2a-final1 → req-ui-step2-final1`。
- 最终部署备份 SHA256：
  `0DB5198B0F3B5581FF128ADF5F56109E0E4E0DD5D6A05E6B620F2373B1728C4C`。
- 独立恢复库 `AIDT_UI_STEP2_FINAL1_RESTORE_20261102_1220`：
  ONLINE、11 条迁移、62 张应用表、`CONFIRMATIONS` 导航和听记上下文字段均存在。
- 冻结标签：`req-ui-step2-frozen-v1`。
