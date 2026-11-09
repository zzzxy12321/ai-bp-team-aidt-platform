# 010 MCP 标准服务 Schema

> 迁移：`20261101122556_010_McpStandardServer`
>
> Schema：`Mcp`

## 表

### `Mcp.Client`

OAuth 动态注册客户端。

- `ClientId` 唯一
- 公共客户端使用 PKCE，`TokenEndpointAuthMethod=none`
- 机密客户端仅保存客户端密钥 SHA-256 摘要
- 保存登记回调、授权类型、作用域、启用状态和最后使用时间

### `Mcp.AuthorizationRequest`

浏览器同意页的短期请求，绑定当前用户、客户端、redirect URI、PKCE challenge 和作用域。

### `Mcp.AuthorizationCode`

一次性授权码；数据库仅保存 SHA-256 摘要。有效期 5 分钟，消费后不可重放。

### `Mcp.AccessToken`

Access Token 与 Refresh Token 共用表，数据库仅保存 SHA-256 摘要。

- Access 有效期 1 小时
- Refresh 有效期 30 天
- 刷新时轮换
- `TokenFamilyId` 将同一授权产生的令牌串联
- 撤销任一令牌会撤销整个令牌家族

### `Mcp.HumanConfirmation`

高风险动作的一次性真人确认。

- 绑定 `UserId + ClientId + ToolName + ResourceType + ResourceId + RequestHash`
- 默认 10 分钟过期
- 状态仅允许 `Pending / Confirmed / Rejected / Consumed`
- 通过条件更新原子消费，不能跨用户或重放

## 约束

- 原始 Access/Refresh Token、授权码和客户端密钥不得落库或写日志。
- `Mcp` 只保存授权状态；项目、事件、任务、流程、文件、导航和业务审计仍由各自模块拥有。
- 调用审计写现有 `Audit.AuditEvent`，不另建第二套可修改审计。

## 回滚

迁移 010 down：

1. 删除 `MCP_CENTER` 导航入口。
2. 删除 `mcp-read`、`mcp-manage` 角色授权和权限。
3. 删除 `agent.mcp` 服务身份。
4. 删除 `Mcp` 五张表。
5. 删除空的 `Mcp` Schema。

不删除既有业务数据和审计。
