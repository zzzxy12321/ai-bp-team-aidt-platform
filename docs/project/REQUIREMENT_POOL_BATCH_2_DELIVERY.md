# AIDT 需求池批次 2 交付报告

> 状态：`REQUIREMENT_POOL_BATCH_2_DEMO_COMPLETE`
>
> 日期：2026-10-31
>
> 范围：`MIN-001`、`WF-005`、`AGT-002`、`AIG-002`
>
> 整个 AIDT 需求池与生产系统：未完成

## 🎯 一句话结论

批次 2 已完成手动听记、规则行动项、人工确认任务、版本冻结节点策略、幂等 SLA
提醒/升级、低风险受限代理和 AgentRun-AiGateway 调用链；真实钉钉与外部模型未接入。

## ✅ 四个 Feature

| Feature ID | 结果 | 主要证据 |
|---|---|---|
| `MIN-001` | 完成 | 文本/文件登记、规则提取多条草稿、人工校对、确认前零任务、任务反查 |
| `WF-005` | 完成 | 节点策略版本、SLA/提醒/升级、任务策略冻结 |
| `AGT-002` | 完成 | 重复扫描幂等、低风险代理真人确认、独立 Agent 身份、高风险拒绝 |
| `AIG-002` | 完成 | AgentRun 调 AiGateway、GatewayCall、摘要、超时/未授权和 ExternalModelCalled=false |

## 🗄️ 数据与迁移

- 005：`20261031014600_005_Batch2MinutesSlaAgentGateway`
- 新增 Minutes 3 表、WorkflowNodePolicy、WorkflowNotification、AgentGatewayCall。
- 扩展 AgentProfile、AgentRun、WorkflowTask。
- 隔离库完成 005 up/down/re-up；主库已应用。
- 主库：5 条 EF 迁移、44 张应用表。

## 🔌 新增 API 与页面

- `/api/v1/minutes*`
- `/api/v1/workflow-versions/{id}/node-policies`
- `/api/v1/workflow-sla/actions/run`
- `/api/v1/workflow-tasks/{id}/actions/agent-proxy`
- `/api/v1/agents/runs/{id}/actions/execute`
- `/api/v1/agents/runs/{id}/gateway-calls`
- `/api/v1/agents/runs/{id}/actions/confirm-proxy`
- 页面 `/minutes`；流程页显示 SLA/代理并查看节点策略；Agent 页可执行网关。
- OpenAPI：53 路径、62 operationId。

## 🎙️ 听记到任务的演示路径

1. 粘贴或上传 UTF-8 文字稿，建立 `MinutesSource`。
2. 创建待真人确认的 AgentRun。
3. 真人确认后调用 AiGateway RuleSimulator。
4. 规则解析生成至少两条行动项草稿。
5. 人工修改并确认后创建 `MinutesTask`。
6. 通过任务反查 MinutesSource、发言人、时间戳和原文片段。
7. 相同确认幂等键返回同一任务。

## ⏱️ SLA 与代理路径

1. 平台管理员为流程版本节点保存并发布策略。
2. 新任务冻结 NodePolicyId 和 DueAt。
3. 测试将任务时间回拨后触发同一 Worker 扫描逻辑。
4. 30 分钟提醒、60 分钟升级各执行一次；重复扫描不重复。
5. 低风险节点请求受限代理，需真人确认。
6. Agent 使用独立服务身份完成动作，通知原责任人并写 `task.agent_proxied`。
7. 永久人工边界策略禁止代理。

## 🧪 测试

- 隔离 SQL/API：70/70，0 失败、0 跳过。
- 前端：6 个文件、9/9。
- OpenAPI：53 路径、62 operationId。
- Playwright：正式部署 6/6，0 失败、0 skipped、0 flaky。
- 迁移：005 up/down/re-up。
- 依赖与格式：通过。

## 🤖 边界

- 钉钉：`Disconnected`。
- 音频转写：未实现。
- 外部模型：未启用。
- AiGateway：`RuleSimulator`。
- 代理：`ExternalModelCalled=false`、低风险、真人确认。

## 🚀 最终发布

- 版本：`req-batch2-final2`
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch2-final2.zip`
- SHA256：`339AC0BBF400F378FA0E570960F06EF85B0C97DEA5B3EFA1267B066A4301888E`
- manifest sourceRevision：`875204ed5d7fbc7cce7054959eab8c4fead255b8`
- 部署目录：`D:\Apps\AIDT-Test\versions\req-batch2-final2`
- 应用回滚：`final2 → final1 → final2`，健康 200
- 部署前备份：`AIDT_PRE_DEPLOY_req-batch2-final2_20261031_101159.bak`
- 备份 SHA256：`111C41C8DC6F9429638B46E4B812117008EB93E9CA25D5C0F05035F9D86302E8`

## 🚧 未完成

- 真实钉钉 API、音频转写、真实企业身份与模型接入。
- 完整转办、委托、会签、或签和 PMO 领域模型。
- 生产 DBA、高可用、灾备和正式上线。
- 整个 AIDT 需求池未完成。
