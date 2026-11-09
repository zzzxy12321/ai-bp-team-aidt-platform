# Mock 与真实边界

## ✅ 真实组件

- SQL Server 2025 业务持久化、Elsa SQL 运行时、Hangfire SQL 存储。
- Garnet 短期分布式锁。
- AIDT API、Worker、Vue 前端、Nginx。

## 🧪 Mock/未连接组件

| 组件 | 状态 | 行为 |
|---|---|---|
| U9 | `Mock` | 返回明确 `Mocked` 回执；不写真实财务 |
| ERP | `Mock` | 不连接真实订单/库存 |
| DCS | `Mock` | 仅报警摘要语义，不连接 PLC/联锁 |
| 钉钉 | `Disconnected` | 不发送真实消息 |
| WMS/LIMS | 未接入 | 无真实连接 |
| 外部模型 | 未接入 | AiGateway `RuleSimulator` |
| 钉钉听记 | `Disconnected` | 仅手动粘贴或上传 `.txt/.md/.json`；无音频转写 |
| 节点代理 | 规则模拟 | 独立服务身份，低风险且需真人确认；高风险禁止 |
| 企业事件来源 | 手动/Disconnected | 手动粘贴或选择已有听记、行动项、通知、附件；无邮箱、聊天或会议自动监听 |
| 事件 Agent 建议 | `RuleSimulator` | 只生成建议；真实模型未启用；高风险只进入人工边界 |

## 🤖 AiGateway

- 白名单模型：`aidt-rule-sim`。
- `ExternalModelEnabled=false`。
- 响应含 `ExternalModelCalled=false`、输入/输出 SHA256 和超时信息。
- 日志不记录消息正文，只记录模型、消息数和输入摘要。
- AgentRun 保存 GatewayCall 关联，必须返回 `ExternalModelCalled=false`。

## 🚫 不得误报

- Mock 回执不能当作真实外部成功。
- 规则模拟不能当作真实 AI 结论。
- 本地演示身份不能当作生产 SSO。
- Demo 不能当作生产上线或生产授权已满足。

## 🧩 批次 2.1 边界

- DOCX 仅解析正文段落，不解析图片、批注、修订、宏或 OCR。
- PDF 不在 AI 听记接口解析；原文件仍可保存在文件中心。
- 自动代理仍使用 RuleSimulator 语义和独立服务身份，不调用真实外部模型。
- 真实钉钉、音频转写、正式 SSO 和外部系统写入均未启用。

## 🏗️ 批次 3 边界

- `demo.pm` 和 `agent.project` 均为本地演示身份，不是企业 SSO。
- 项目、成员、里程碑、事件、建议和项目任务均真实写入 SQL Server。
- 事件上传只解析 TXT/MD/JSON/DOCX 文本；其他附件可登记后作为来源，不做 OCR。
- Agent 分析零业务副作用：不创建项目任务、不发起流程、不修改项目状态。
- 真人确认后才可幂等创建项目任务；流程关联只引用既有流程实例，不自动发起。
- 财务付款、质量放行、配方、DCS/PLC、安全联锁、合同、客户重大赔付保持永久人工边界。
