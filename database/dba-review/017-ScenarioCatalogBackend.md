# 017 ScenarioCatalogBackend DBA 审核

## 🎯 变更范围

- 新增 `Scenario` Schema：`ScenarioDefinition`、`ScenarioVersion`、`ScenarioLaunch`。
- 扩展 `Document.AttachmentLink.EntityType`，允许 `ProjectTask`、`EventInbox`、`ScenarioLaunch`。
- 扩展 `Identity.AccessChangeRequest.RequestType`，允许 `ScenarioPublish` 双人审核。
- 新增 5 项场景权限、6 个基础场景和 `FINANCE_PAYMENT_REVIEW` 单节点人工流程。

## 🔐 数据与约束

- 场景编码组织内唯一，场景版本号场景内唯一。
- 所有场景 JSON 字段均有 `ISJSON` 约束。
- 场景启动存在组织、用户、幂等键唯一约束。
- 场景停用和版本状态有检查约束。
- 已发布版本不可原地覆盖，由应用服务和数据库版本唯一约束共同保证。

## ⚠️ 锁与回滚

- 新增表和索引不修改历史业务数据，预计锁风险低。
- 两个既有检查约束为收紧后的受控扩展，历史数据均满足扩展值。
- Down 删除场景表，并清理 017 新增的场景权限、角色授权和财务流程；
  001–016 不改写。

## 🧪 验证

- 最终隔离库完成 16→17 up、17 down、17 re-up。
- 结构：68→71→68→71 张应用表。
- 6 个场景、6 个发布版本、5 项权限、16 条角色授权、1 个人工流程。
- 场景和引用快照 JSON 无效数量 0。
