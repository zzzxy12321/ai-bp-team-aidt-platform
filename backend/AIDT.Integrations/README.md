# AIDT.Integrations

## 📌 当前状态

定义连接器 Adapter、命令信封和回执契约，提供 Mock 和未连接 Adapter。所有 Mock 均返回明确回执，不发起外部网络调用。

## 🔁 目标流程

```text
业务命令 → 本地事务 → Outbox → Connector Adapter → 回执/重试/死信/对账
```

## 🚫 边界

- 不直接写 U9、ERP、DCS、WMS 或 LIMS 数据库。
- 写回必须有幂等键、审批证据、回执和补偿。
- 未授权连接器只能显示 `Disconnected`；Mock 回执不得标记为真实外部成功。
- 全局外部系统模式为 `Mock`，`AllowWrites=false`。
