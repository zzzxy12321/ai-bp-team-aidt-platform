# AIDT.Worker

## 📌 当前状态

全阶段 Demo 后台宿主。`AIDT:Worker:EnableBackgroundProcessing=true`，运行：

- Hangfire SQL Server 存储和分钟心跳任务
- Outbox Worker、指数重试和死信记录
- Garnet 短期分布式锁

`/health/live` 和 `/health/ready` 均已通过。

## ⏭️ 阶段 B

- 服务重启后的任务恢复和生产监控告警
- 对账和人工补偿工作流
- 生产 Windows 服务托管参数
