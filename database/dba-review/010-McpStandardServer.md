# DBA 审核：010 MCP 标准服务

## 变更范围

- 新增 Schema：`Mcp`
- 新增表：5
- 新增索引：12
- 新增权限：2
- 新增服务身份：`agent.mcp`
- 新增导航入口：`MCP_CENTER`
- 不修改既有业务表字段

## 安全审查

- Token、AuthorizationCode、ClientSecret 只保存 SHA-256，不保存明文。
- TokenHash、AuthorizationCodeHash、ClientId 唯一索引防止碰撞和重复。
- 确认记录用 `UserId + Status + ExpiresAt` 和请求复合索引支持一次性消费。
- 令牌家族索引支持同一 OAuth 授权整体撤销。
- SQL 迁移包含 `SET QUOTED_IDENTIFIER ON`，兼容 Identity 过滤索引。
- Up/Down 不修改或删除既有业务审计；Down 只删除本迁移创建的授权状态和种子。

## 执行与回滚

- 只执行 `database/migrations/010_McpStandardServer.up.sql`。
- 迁移前必须完成 `COPY_ONLY, CHECKSUM` 备份和 `RESTORE VERIFYONLY`。
- 隔离验证顺序：`009 → 010 up → 010 down → 010 re-up`。
- Down 后应回到 57 张应用表、9 条迁移、无 `Mcp` Schema。

## 数据量预期

MCP 表只保存短期授权状态。Token/Code 可按到期时间定期清理；业务调用审计保留在 `Audit` 模块，不在本迁移中删除。
