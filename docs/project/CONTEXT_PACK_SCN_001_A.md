# SCN-001-A 上下文包

## 🎯 当前目标

在 `feature/ui-step7a-scenario-catalog-backend` 实现岗位场景目录、
不可变版本生命周期和受控启动后端，最终发布 `req-ui-step7a-final1`
并冻结 `req-ui-step7a-frozen-v1`。

## 🧱 已确认基线

- `main` 为 `ae64f50`，6A/6B 已冻结。
- 当前最大迁移为 016，68 张应用表。
- 当前部署版本为 `req-ui-step6-final1`。
- 现有真实流程：`CUSTOMER_SAMPLE_TO_PRODUCTION`、`BATCH_DEVIATION_CAPA`、
  `PURCHASE_TO_PAYMENT`、`CUSTOMER_COMPLAINT_CAPA`。
- 现有主对象：`CUSTOMER_SAMPLE`、`PURCHASE_REQUEST`、`QUALITY_DEVIATION`、
  `CUSTOMER_COMPLAINT`、`PROJECT`。
- 现有项目任务、事件收件箱、文件关联、听记来源、AgentProfileVersion、
  Audit 和 Outbox 均已有真实服务。

## 📐 架构决策

- 场景只保存配置和启动映射，不成为业务状态权威。
- 发布版本冻结对象类型编码/版本、流程定义/版本、AgentProfile 编码/版本。
- 启动通过现有业务对象、流程、事件收件箱和项目任务服务。
- 同一场景启动由外层事务包裹，内层业务服务采用事务内暂存入口。
- 场景停用只阻止新启动，历史记录和真实业务链不删除。

## 🚫 禁止

- 直接插入 WorkflowInstance、WorkflowTask 或 BusinessObject 绕过领域服务。
- 复制对象字段、流程状态、任务状态或审计账本。
- 通过超时、Agent 或场景能力绕过永久人工边界。
- 修改、移动或覆盖历史标签与发布包。

## 🧪 完成门禁

- 六类场景真实可启动。
- 生命周期、版本、权限、引用失效、材料、幂等、回滚和人工边界通过。
- 迁移 017 up/down/re-up 与主库应用通过。
- 后端、SQL/API、MCP、OpenAPI、前端类型、Vitest 和全量 E2E 通过。
- 正式部署、应用回滚和独立数据库恢复通过。
