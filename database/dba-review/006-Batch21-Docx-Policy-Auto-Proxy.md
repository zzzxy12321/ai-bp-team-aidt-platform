# 006 批次 2.1 DBA 审核

> 状态：隔离库已验证；主库已应用
>
> 日期：2026-10-31

## 变更

- `Workflow.WorkflowNodePolicy` 增加代理模式、自动代理分钟和模式约束。
- `Agent.AgentRun` 增加代理触发、原责任人、补偿状态和唯一幂等键。
- 增加过滤唯一索引 `UX_AgentRun_ProxyIdempotency`。
- 幂等 Seed 增加专用低风险流程 `RND_MATERIALS_CONFIRM` 和节点策略 `MATERIALS_CONFIRM`。

## 安全边界

- 默认策略风险为 Low、A2、金额上限 0、永久人工边界 false。
- 自动代理使用独立服务身份，不写真人确认。
- 财务、质量放行、配方、设备控制、安全联锁、合同和重大赔付不得使用本策略。
- 006 不修改 001—005，不删除既有业务数据。

## 验证

- 备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_1_PRE_006_20261031_121903.bak`
- `RESTORE VERIFYONLY`：通过。
- 隔离库：`AIDT_REQ_BATCH2_1_TEST_20261031_121903`。
- 序列：005 → 006 up → 006 down → 006 re-up。
- 结果：6 条 EF 迁移、44 张应用表、默认策略唯一。

## 回滚

- 先停止批次 2.1 应用和 Worker。
- 执行 006 down 前确认没有仍在运行且依赖自动代理字段的任务。
- 应用版本回滚和数据库结构回滚分别验证，不以应用回滚代替数据库恢复。
