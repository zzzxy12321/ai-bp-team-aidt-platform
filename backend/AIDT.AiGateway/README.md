# AIDT.AiGateway

## 📌 当前状态

Demo 级受控 AI 网关接缝，监听 `127.0.0.1:5100`。

- 只允许 `aidt-rule-sim` 白名单模型。
- 使用本地规则模拟结果。
- `ExternalModelEnabled` 必须为 `false`。
- 请求有超时上限。
- 日志只记录模型、消息数和输入摘要，不记录消息正文。
- 响应明确标识 `ExternalModelCalled=false`。

## 🔌 端点

- `GET /health/live`
- `GET /health/ready`
- `GET /v1/models`
- `POST /v1/chat/completions`

## 🚫 边界

不连接 Ollama、OpenAI 或其他真实模型。接入真实模型前必须另行完成安全、数据分级、密钥和 ADR。
