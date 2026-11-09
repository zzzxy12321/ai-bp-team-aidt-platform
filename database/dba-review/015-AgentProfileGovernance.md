# DBA 审核：015 AgentProfile Governance

## 🎯 变更目的

把 `Agent.AgentProfile` 从规则模拟配置升级为可版本化、可发布、可停用和可审计的数字员工身份主数据；
同时把 MCP 服务令牌显式关联数字员工，并在停用时撤销服务令牌。

## 🗄️ 影响对象

| 对象 | 变更 |
|---|---|
| `Agent.AgentProfile` | 增加编码、说明、部门、负责人、生命周期、当前发布版本、数据/模型/工具/确认/熔断规则及审计列 |
| `Agent.AgentProfileVersion` | 新增不可变版本快照表 |
| `Mcp.Client` | 增加可空 `AgentProfileId` 和索引/外键 |
| `Mcp.AccessToken` | 增加 `AgentProfileId`、`RevokedBy`、`RevocationReason` |
| `Identity.AccessChangeRequest` | 检查约束扩展 `AgentProfilePublish` |
| `Identity.*` | 增量权限、部门负责人角色、Agent 服务角色和演练用户 |

当前主库影响行：`AgentProfile` 存量 3 行；新增版本 3 行；身份用户/角色/权限均为小表增量。

## ⚠️ 锁与执行

- 没有大表重写；主要操作是加可空列、扩展小表、创建 1 张新表和过滤索引。
- `AgentProfile` 与 `Mcp.*` 当前为小表，预计锁表秒级。
- 不适合与正在发布数字员工或签发 MCP 令牌的写入并发执行。

## 🧪 已执行验证

- 基线：14 条迁移 / 67 张应用表。
- up：15 条迁移 / 68 张应用表；3 个既有身份全部生成发布快照。
- up 幂等复跑：通过，无重复身份或版本。
- down：恢复 14 条迁移 / 67 张应用表，新增用户、权限、角色与列清理完成。
- re-up：再次恢复 15/68，身份 3、发布版本 3、重复编码 0、重复发布版本 0。
- 自动化：后端/SQL/API 171/171，MCP 11/11。

## 🧾 脚本

- 升级：`database/migrations/015_AgentProfileGovernance.up.sql`
- SHA256：`D8C489B1F7FCBA44A27F57CE207B3C4B2AAD90DB03199C0F66FFF161D5378E13`
- 回滚：`database/migrations/015_AgentProfileGovernance.down.sql`
- SHA256：`442E38A2D28DE7D9F0440A18F494F66DB4C41F8A4B9CC75018EA4AD76018E40D`

脚本含 `SET ANSI_NULLS ON`、`SET QUOTED_IDENTIFIER ON` 和批分隔 `GO`，以支持过滤索引及新增列在后续批次中编译。

## ↩️ 回滚

优先恢复迁移前全量备份。隔离环境可执行 down；应用默认回滚到 `req-ui-step5-final1`。
down 会删除 015 新增版本表和新列，不会删除历史 `AgentRun`、任务动作或审计记录。

## ✅ 结论

建议在维护窗口执行；脚本及约束需保持当前 SHA256。该迁移仅用于测试环境本轮发布。

