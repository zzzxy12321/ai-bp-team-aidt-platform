# 阶段 A 上下文包

## 当前目标

完成 AIDT 初期框架阶段 A：正式后端与前端骨架、领域契约、非 SQL 测试、迁移源文件和文档全部落地；数据库验证留给重启后的阶段 B。

## 验收标准

- 7 个后端项目在 `.NET 10` 下编译，项目引用方向符合冻结架构。
- Vue 3 前端可类型检查、组件测试和生产构建。
- Domain、Application、DTO、错误码和 OpenAPI 契约一致。
- 迁移与回滚脚本已编写，状态为 `authored_not_applied`，未执行。
- SQL Server 阻断证据与恢复检查点已记录。
- 未使用任何替代数据库。

## 相关功能

`PLT-001`、`PLT-002`、`OBJ-001`、`OBJ-002`、`WF-001` 至 `WF-004`、`TASK-001`、`AUD-001`、`AGT-001`、`INT-001`、`CFG-001`、`DEP-001`。

## 相关契约

- `/api/v1/auth/session`
- `/api/v1/permissions/me`
- `/api/v1/object-types`
- `/api/v1/business-objects`
- `/api/v1/workflow-instances`
- `/api/v1/workflow-tasks`
- `/api/v1/audit-events`
- `/api/v1/agents`
- `/api/v1/connectors`
- `/api/v1/configurations`
- `/health/live`
- `/health/ready`
- `/openapi/v1.json`

## 明确排除

SQL Server 连接、迁移执行、数据库集成测试、真实 SSO、真实连接器、真实模型、外部系统写回、付款/质量放行/DCS 控制、旧 `AIDT/LOCAL-MVP` 修改。
