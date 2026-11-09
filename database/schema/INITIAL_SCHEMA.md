# AIDT 初始 SQL Server Schema

> 版本：InitialSchema + WorkflowPersistenceAndSecurity  
> 应用表：35  
> 历史状态：applied_verified（001/002，不等于 003 已执行）
>
> 当前批次：003 `authored_not_applied`

## 🗂️ Schema 与表

| Schema | 表 |
|---|---|
| `Identity` | Organization、User、Role、Permission、UserRole、RolePermission、DataScope |
| `Metadata` | ObjectType、FieldDefinition、ObjectRelation |
| `BusinessObject` | BusinessObject、BusinessObjectVersion、BusinessObjectRelation |
| `Workflow` | WorkflowDefinition、WorkflowVersion、WorkflowInstance、WorkflowTask、WorkflowTaskCandidate、WorkflowTaskAction |
| `Audit` | AuditEvent、EvidenceAsset |
| `Integration` | Connector、ConnectorCommand、ConnectorReceipt、OutboxEvent、DeadLetter、Reconciliation、IdempotencyRecord |
| `Agent` | AgentProfile、AgentRun、AgentConfirmation |
| `Document` | Attachment、DocumentVersion、Signature |
| `Configuration` | ConfigurationVersion |

## 🔑 关键约束

- 业务编号在同一 `ObjectTypeId` 下唯一。
- 同一业务对象的版本号唯一。
- 流程实例固定 `WorkflowDefinitionId`、`WorkflowVersionId` 和 `BusinessObjectId`。
- 外部命令幂等键按连接器唯一。
- Outbox 幂等键全局唯一。
- Agent 金额和成本不得为负。
- JSON 字段通过 `ISJSON` 检查。
- 金额使用 `decimal(18,4)`，Agent 成本使用 `decimal(18,6)`。
- 时间统一转换为 UTC 后写入 `datetime2(3)`。
- `BusinessObject.RowVersion` 和 `WorkflowTask.RowVersion` 用于并发控制。
- 业务对象保存组织范围；可重放写命令使用 `Integration.IdempotencyRecord`。

## 🔍 主要索引

- 业务对象：`(ObjectTypeId, ObjectCode)` 唯一；`(Status, UpdatedAt, Id)` 分页。
- 任务：`(AssigneeId, Status, CreatedAt)`；`(WorkflowInstanceId, CreatedAt)`。
- 审计：`(EntityType, EntityId, OccurredAt)`；`(TraceId, OccurredAt)`。
- Outbox：`(Status, NextAttemptAt, CreatedAt)`。
- Agent：`(AgentProfileId, CreatedAt)`；`(Status, CreatedAt)`。

## ⚠️ 已验证

- SQL Server 2025 实际表、外键、索引和检查约束。
- JSON 检查与中文 Demo 数据。
- 行版本并发冲突和事务回滚。
- 备份恢复。
- 002 升级与 down 回滚。

生产排序规则、容量和大表执行计划仍需甲方 DBA 环境复核。
