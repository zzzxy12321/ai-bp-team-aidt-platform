# UX-004-A 事件确认与流程发起交付报告

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`
>
> 功能：`UX-004` 业务可编辑的人工确认中心，第 2A 段
>
> 分支：`feature/ui-step2a-event-confirmation`
>
> 目标发布：`req-ui-step2a-final1`
>
> 目标冻结标签：`req-ui-step2a-frozen-v1`

## 🎯 交付结论

事件收件箱已从“只能接受或拒绝建议”升级为可编辑的真人确认：

```text
选择项目
 → 选择项目成员作为负责人
 → 按 Asia/Shanghai 设置截止时间
 → 选择 P0/P1/P2
 → 编辑任务内容
 → 决定是否创建任务
 → 不启动流程 / 关联已有流程 / 发起新流程
 → 查看影响与不会执行的动作
 → 一次原子提交
```

页面不再要求输入 UUID；项目、人员、流程均以业务名称展示。

## 🧱 实现

### 服务端

- 新增 `POST /api/v1/inbox/{id}/actions/confirm-and-start-flow`。
- 新增 `ConfirmInboxEventAndStartWorkflowHandler`，统一事务编排：
  - 启动新流程
  - 确认事件
  - 创建项目任务
  - 写项目来源/任务/流程关系
  - 写审计和幂等
- 流程启动规则提取为事务内入口，继续复用原 `StartWorkflowInstanceHandler`：
  权限、定义/版本、业务对象、处理人解析、首任务、Outbox 和审计。
- 事件确认规则提取为事务内入口，继续复用 `SqlEventInboxService`：
  部门/项目范围、项目成员负责人、项目任务、关系、审计和幂等。
- 事件状态通过条件更新抢占；同一事件不能重复确认或重复派发。
- 流程失败、负责人校验失败或后续写入失败时，事件、任务、流程、关系、审计和幂等
  全部回滚。
- 高风险永久人工边界保持不变。

### 前端

- 事件卡提供“编辑并确认业务去向”。
- 确认弹窗包含项目、负责人、截止时间、优先级、可编辑内容、创建任务开关、
  已有流程和新流程选择。
- 影响预览明确显示关联项目、负责人、截止、任务、流程、通知对象和不会执行的动作。
- 缺负责人、截止或必要流程时最终确认按钮禁用；后端同时返回 400 防止绕过。
- 截止时间固定按 `Asia/Shanghai` 转换和显示，避免浏览器时区导致次日偏移。

### 数据与接口

- 不新增表、字段或迁移；继续使用 10 条迁移、62 张应用表。
- 确认请求复用 `ProjectId / AssigneeId / DueAt / Priority / Content / WorkflowInstanceId`，
  新增可选 `WorkflowDefinitionId / WorkflowVersionId` 用于发起新流程。
- 确认结果新增可选流程实例，事件、任务、项目与流程可互相反查。
- OpenAPI 更新为 123 个路径、143 个 operationId。

## 🔐 权限与边界

| 场景 | 结果 |
|---|---|
| 缺负责人 | HTTP 400，禁止最终确认 |
| 缺截止时间 | HTTP 400，禁止最终确认 |
| 非项目成员负责人 | HTTP 403 |
| 跨部门项目 | HTTP 403 |
| 缺事件确认权限 | HTTP 403 |
| 缺流程提交权限 | HTTP 403，不绕过原动作权限 |
| 已确认事件再次派发 | HTTP 409 |
| 新流程启动后确认失败 | 事务整体回滚，无任务/流程/关系残留 |

不会执行：付款、质量放行、配方变更、DCS/PLC 控制、安全联锁、合同承诺、
客户重大赔付。

## 🧪 源码验证

| 门禁 | 结果 |
|---|---|
| .NET 构建 | 0 警告、0 错误 |
| SQL/API | 132/132 |
| MCP | 11/11 |
| 后端合计 | 143/143，0 失败、0 跳过 |
| 后端格式 | 通过 |
| NuGet 漏洞 | 无已知漏洞 |
| 前端类型 | 通过 |
| Vitest | 12 个文件、20/20 |
| OpenAPI | 123 路径、143 operationId |
| 生产构建 | 通过 |
| npm 生产依赖 | 0 漏洞 |
| 源码 Playwright | 25 通过、1 个部署专用 MCP 用例跳过、0 失败、0 flaky |

新增 E2E 在 1440、1280、390px 运行，截图见
`docs/project/test-artifacts/ux004-confirmation-*.png`。

## 📌 当前边界

- 本段只完成事件确认来源；听记、MCP、Agent 等其他人工确认来源不在本段伪造为完成。
- 当前身份仍为本地 Cookie + CSRF，不是生产 SSO。
- 不连接真实钉钉、U9、ERP、DCS，不调用真实外部模型。
- 本段完成仅代表事件确认入口；听记、MCP、Agent 等其他确认来源后续另行实施。

## 🚀 正式交付

- 功能提交：`a57848e`、`24fc249`、`561e8cb`。
- 非快进合并：`ed78d4813d5842d78e41646dda707a6ba150f9eb`。
- 目标发布：`req-ui-step2a-final1`。
- 发布包：
  `D:\Apps\AIDT-Test\releases\AIDT-req-ui-step2a-final1.zip`。
- 发布包 SHA256：
  `C0179C5618C67F95BABB49A0254F0F9FD69A1D1DF2617AB4DDC5252C020CB553`。
- manifest 源码：`ed78d4813d5842d78e41646dda707a6ba150f9eb`，不含 `-dirty`。
- 部署版本：`D:\Apps\AIDT-Test\versions\req-ui-step2a-final1`。
- 正式 Playwright：26/26，0 失败、0 跳过、0 flaky。
- 应用回滚：
  `req-ui-step2a-final1 → req-ui-step1-final1 → req-ui-step2a-final1`。
- 最终部署前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-step2a-final1_20261102_093323.bak`。
- 备份 SHA256：
  `3DA905C968B42974B351FBF97E3DB517E455F303671F1F686A339658C104A0E1`。
- 独立恢复库：`AIDT_UX004_A_FINAL1_RESTORE_20261102_093323`，ONLINE；
  10 条迁移、62 张应用表。
- 目标冻结标签：`req-ui-step2a-frozen-v1`。
