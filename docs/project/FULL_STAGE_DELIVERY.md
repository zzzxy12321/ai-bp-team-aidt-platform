# AIDT 全阶段 Demo 交付报告

> 目标状态：`FULL_STAGE_COMPLETE_DEMO_READY`  
> 日期：2026-10-30  
> 定位：Windows 单机内网首个可交付 Demo  
> 生产 AIDT：未完成

## 🎯 一句话结论

AIDT 首个全阶段 Demo 已完成：真实 SQL Server 持久化、三种已完成业务流程、退回和终止、Elsa、Hangfire/Outbox/Garnet、本地演示身份与权限、真实 Vue API、桌面/移动 E2E、Nginx、发布回滚和离线构建均有可复查证据。

## 🧱 系统组成

| 组件 | 测试地址/位置 | 状态 |
|---|---|---|
| Vue 前端 | `127.0.0.1:5173` 开发；Nginx `127.0.0.1:8088` 测试交付 | 通过 |
| AIDT API | `127.0.0.1:5080` | 通过 |
| AIDT Worker | `127.0.0.1:5081` | 通过 |
| AiGateway | `127.0.0.1:5100`，RuleSimulator | 通过 |
| SQL Server 2025 | `MSSQLSERVER`，1433，命名管道 | 通过 |
| Elsa 3.9 | SQL Schema `Elsa` | 通过 |
| Hangfire | SQL Schema `HangFire` | 通过 |
| Garnet 2.2.5 | `127.0.0.1:6380` | 通过 |
| Nginx 1.28.0 | `127.0.0.1:8088` 唯一测试入口 | 通过 |

## 🗄️ 数据库与恢复点

- SQL Server 版本：`17.0.1000.7 Express Edition (64-bit)`。
- 应用 Schema：9；应用表：35。
- EF 迁移：`InitialSchema`、`WorkflowPersistenceAndSecurity`。
- Elsa 迁移：15；Hangfire 表：11。
- 全阶段修改前备份：`C:\ProgramData\AIDT\Backups\AIDT_FULLSTAGE_20261030_092845.bak`。
- 备份 SHA256：`18C2637590B0E3CFDF6948BC6E26B9CE997B5FA50B9B8C058B1FC89F52EDC3AE`。
- `RESTORE VERIFYONLY` 和临时库恢复均已通过。

## 🔄 业务流程

| 流程 | 持久化结果 | 关键证据 |
|---|---|---|
| `WF-001` 客户样品到转产 | Completed | 5 节点、跨 5 个角色 E2E；对象 Draft→Active/Version 2 |
| `WF-002` 批次异常到 CAPA | Completed + Returned | 条件分支、CAPA、退回原因和 Suspended 对象 |
| `WF-003` 请购到付款 | Completed | 金额 >10000 条件、财务人工审批，无真实付款 |
| `WF-004` 客诉到 CAPA | Terminated | 终止决策、Cancelled 任务、Suspended 对象 |

对象、流程实例、任务、任务动作、审计和 Outbox 均通过 SQL 外键/关联 ID 与 TraceId 追溯。

## 🧪 验证摘要

- 后端测试：41/41 通过，0 跳过。
- 前端组件/状态测试：3/3 通过。
- Playwright：桌面 WF-001 全角色闭环 + 390px 移动端，2/2 通过。
- OpenAPI：28 路径、33 operationId，静态校验通过。
- NuGet/npm：无已知漏洞。
- 迁移测试：001 up 34 表 → 002 up 35 表 → 002 down 34 表 → 001 down 0 表。
- 并发/幂等：版本守卫更新仅一个赢家；同幂等键重放返回原对象，异内容返回 409。
- 部署：demo-a→demo-b→rollback demo-a，Nginx、API、Worker、AiGateway 健康检查通过。
- 离线：233 个 NuGet 包与 npm 缓存离线 restore/install/build 通过。

## 🚫 明确边界

- 本地演示身份不是生产 SSO。
- U9/ERP/DCS/钉钉均为 Mock 或 Disconnected。
- AiGateway 不调用真实模型。
- 不产生真实付款、质量放行、配方修改、设备控制或外部消息。
- 当前是 Windows 单机 Demo，不是高可用生产环境。
- 配置版本发布/回滚页面未纳入首个 Demo；部署脚本回滚已实现。
- 会签、或签、委托和 SLA 属于后续 Elsa 扩展。

## 📁 接手入口

- [README](../../README.md)
- [启动说明](../004-交付说明/STARTUP.md)
- [权限矩阵](./PERMISSION_MATRIX.md)
- [Mock 边界](./MOCK_BOUNDARIES.md)
- [测试证据](./TEST_EVIDENCE.md)
- [部署说明](../../deploy/README.md)
- [功能地图](./FEATURE_MAP.md)
- [变更文件清单](./FULL_STAGE_CHANGE_MANIFEST.md)

## 📦 最终测试发布包

- 版本：`demo-final3`
- 路径：`D:\Apps\AIDT-Test\releases\AIDT-demo-final3.zip`
- SHA256：`684EE630E912AF3B55CF32C3B8464A9850A3F15F540CFE08A04366F4E882643E`
- 当前版本目录：`D:\Apps\AIDT-Test\versions\demo-final3`
- 当前浏览器入口：`http://127.0.0.1:8088`

## 🔙 回滚点

应用回滚可使用 `D:\Apps\AIDT-Test` 的上一版本目录；数据库恢复点使用
`deployment-history.json` 中的预部署备份。生产回滚仍需甲方审批和正式数据库维护窗口。

- 最终部署前数据库备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_demo-final3_20261030_143102.bak`
- 备份 SHA256：`31C8DB5A8DC098CF18F4FC31FF7EF4AE52F6216137605D4FD60AFCC81F911029`
- 上一应用版本：`D:\Apps\AIDT-Test\versions\demo-final2`
