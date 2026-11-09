# UX-004 业务可编辑的人工确认中心交付报告

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`
>
> 功能：`UX-004` 业务可编辑的人工确认中心
>
> 功能分支：`feature/ui-step2b-confirmation-center`
>
> 合并提交：`931f9531f71d58b189fb83941e30195edd30f6ef`
>
> 正式发布：`req-ui-step2-final1`
>
> 冻结标签：`req-ui-step2-frozen-v1`

## 🎯 交付结论

UX-004 已完成事件、听记、MCP 和 Agent 四类人工确认的统一业务入口：

```text
/confirmations?type=event&id=…
/confirmations?type=minutes&id=…
/confirmations?type=mcp&id=…
/confirmations?type=agent&id=…
```

用户可从“我的工作”直接进入统一确认页；原详情深链继续兼容。

## 🧱 实现

### 统一确认中心

- 新增 `/confirmations`，按来源和状态筛选事件、听记行动项、MCP 与 AgentRun。
- 页面覆盖 Loading、Empty、Error、Denied、Expired、Pending、Confirmed、
  Rejected、NeedsInformation。
- 统一页只组合现有领域 API，不建立第二套确认状态。
- 深链选中对应事项；外部来源可进入原详情执行动作。

### 听记行动项

- 可编辑负责人、截止时间、优先级、项目、业务对象、流程版本、已有流程和内容。
- 信息不完整可以保存；缺负责人或截止时间时前后端均禁止确认创建任务。
- 确认后保存负责人、截止、项目、对象、流程和任务关联。
- 项目和任务关系在同一事务写入；项目详情可反查行动项、任务和流程。
- 新增听记部门负责人候选接口，使用姓名和部门名称，不输入 UUID。

### MCP 确认

- 页面标题显示实际业务动作，并显示项目、事件或任务名称。
- 显示“确认将执行”和“不会执行”的业务影响。
- `clientId`、`resourceType`、`resourceId`、`requestHash` 收进“技术信息”折叠区。
- 通过和拒绝都强制填写意见；每次只能处理一条确认。
- 页面和 API 均不返回或展示 Access Token、Refresh Token、授权码或 Cookie。
- Expired、Pending、Confirmed、Rejected 状态可区分。

### Agent 与人工边界

- AgentRun 只提供真人确认入口，服务身份不能自确认。
- 付款、质量放行、配方变更、DCS/PLC、安全联锁、合同承诺和客户重大赔付
  继续作为永久人工边界，不允许自动确认。

## 🗄️ 数据与迁移

- 迁移 011：`20261102023118_011_UX004ConfirmationCenter`。
- `MinutesActionItem` 增加 `ProjectId`、`BusinessObjectId`、`WorkflowInstanceId`。
- `MinutesTask` 增加 `ProjectId`。
- 为现有已发布导航注入系统入口 `/confirmations`。
- 不新增表；隔离库 up/down/re-up 后保持 62 张应用表。

## 🛡️ 权限与拒绝

- 听记、事件、Agent 列表分别执行组织、部门、项目/环境和动作权限。
- 听记候选只返回当前听记部门的有效真人；服务身份不可作为负责人。
- MCP 自有确认只返回当前用户和组织范围。
- 无组织范围身份进入统一页时返回 Denied 状态，不显示其他部门事项。
- 过期、重复确认、缺负责人/截止、无权限均被后端拒绝。

## 🧪 源码验证

| 门禁 | 结果 |
|---|---|
| .NET 构建 | 0 警告、0 错误 |
| SQL/API | 134/134 |
| MCP | 11/11 |
| 后端合计 | 145/145，0 失败、0 跳过 |
| 前端类型 | 通过 |
| Vitest | 13 个文件、23/23 |
| OpenAPI | 125 路径、145 operationId |
| 生产构建 | 通过 |
| 源码 Playwright | 28 通过、1 个部署专用 MCP 用例跳过、0 失败、0 flaky |

新增 E2E 覆盖 1440、1280、390px，截图位于
`docs/project/test-artifacts/ux004-confirmation-center-*.png`。

## 🚀 正式发布、回滚与恢复

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step2-final1.zip`。
- 发布包 SHA256：
  `4832A05B0350D79E3C33E2438B89FE980A979D5D6A4508ED1F3D3AFD50F2C189`。
- manifest `sourceRevision`：
  `931f9531f71d58b189fb83941e30195edd30f6ef`，不含 `-dirty`。
- 正式部署目录：`D:\Apps\AIDT-Test\versions\req-ui-step2-final1`。
- 应用回滚：
  `req-ui-step2-final1 → req-ui-step2a-final1 → req-ui-step2-final1`，两段健康检查通过。
- 最终正式 E2E：29/29，0 失败、0 跳过、0 flaky；报告开始于
  `2026-11-02T12:16:51+08:00`，晚于最终重新部署 `12:16:41`。
- 最终部署备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-step2-final1_20261102_121633.bak`。
- 部署备份 SHA256：
  `0DB5198B0F3B5581FF128ADF5F56109E0E4E0DD5D6A05E6B620F2373B1728C4C`。
- 独立恢复库：`AIDT_UI_STEP2_FINAL1_RESTORE_20261102_1220`，ONLINE、
  11 条 EF 迁移、62 张应用表。
- 恢复库已核对 `CONFIRMATIONS /confirmations` 导航入口、4 个听记上下文列及 4 个外键。
- 最终部署页面、API live/ready、capabilities 均返回 HTTP 200；
  capabilities 返回 `UX-004=available`、`databaseMigrationState=applied_verified`。

## 📌 使用边界

- 当前身份仍为本地 Cookie + CSRF，不是生产 SSO。
- 不连接真实钉钉、U9、ERP、DCS，不调用真实外部模型。
- 这是经过部署、回滚和恢复验证的测试环境最终包，不代表生产 SSO、真实外部系统或
  高可用生产环境已经上线。
