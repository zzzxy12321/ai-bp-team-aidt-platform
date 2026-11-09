# AIDT 全阶段 Demo 会话交接

## 当前目标

首个可交付 AIDT Demo 已完成；生产 AIDT 未完成。

## 已完成

- SQL Server 2025、两批 EF 迁移、35 张应用表。
- SQL 仓储、事务、并发、幂等、审计和 Outbox。
- 本地 Cookie 身份、CSRF、RBAC、组织范围和 Core 脱敏。
- Elsa 3.9、Hangfire、Outbox Worker 和 Garnet 锁。
- WF-001/002/003 Completed，退回和 WF-004 终止。
- Mock 连接器和 AiGateway RuleSimulator。
- Vue 真实 API、桌面和移动 E2E。
- Nginx、发布包、校验和、备份恢复、迁移 down、A→B→A 回滚和离线构建。

## 测试结果

- 后端：41/41。
- 前端：3/3。
- Playwright：2/2。
- OpenAPI：28 路径、33 operationId。
- NuGet/npm：无已知漏洞。

## 恢复点

- 最终部署前数据库：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_demo-final3_20261030_143102.bak`
- 最终部署前备份 SHA256：`31C8DB5A8DC098CF18F4FC31FF7EF4AE52F6216137605D4FD60AFCC81F911029`
- 部署状态：`D:\Apps\AIDT-Test\deployment-state.json`
- 部署历史：`D:\Apps\AIDT-Test\deployment-history.json`
- 当前 `demo-final3` 测试服务保持运行，浏览器入口 `http://127.0.0.1:8088`。

## 未完成

- 正式 SSO/OIDC、真实外部系统、生产监控/告警和高可用。
- 生产 DBA 审核、证书、SPN、Windows 服务运维账号。
- 会签、或签、委托和 SLA。

## 下一步

1. 依据甲方身份源完成正式 OIDC/SSO。
2. 在甲方测试网络确认 SQL 证书、SPN、账号和 DBA 维护窗口。
3. 将现有 Mock Adapter 替换为经授权的只读 Sandbox Adapter。
4. 做生产规模容量、SLA、RTO/RPO 和高可用设计。
