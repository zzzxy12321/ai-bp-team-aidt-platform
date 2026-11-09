# AIDT.Mcp

> Feature：`MCP-001`
>
> 协议 SDK：`ModelContextProtocol.AspNetCore` / `ModelContextProtocol` 2.3.0

## 职责

`AIDT.Mcp` 是 AIDT 的标准智能体接入层，不是第二套业务系统。

- Streamable HTTP：`/mcp`
- stdio：`AIDT.Mcp.exe --stdio`
- OAuth 2.1：授权码 + PKCE S256
- 工具：项目、事件、项目任务、流程、文件、导航、审计、Agent 目录
- Resources：`aidt://projects/{id}`、`aidt://events/{id}`、`aidt://project-tasks/{id}`、
  `aidt://workflow-instances/{id}`、`aidt://files/{id}`、`aidt://navigation/current`
- 高风险操作：审批、转交、事件确认、项目最终状态、敏感文件正文必须二次真人确认

## 安全边界

- 只继承当前 AIDT 用户或显式服务身份的有效权限。
- MCP 令牌作用域不能扩大 AIDT 业务权限。
- 令牌部门的交集同时生效，平台管理员通过 MCP 也不能绕过所选部门。
- 不暴露 SQL、Shell、任意文件系统、任意 HTTP、连接串或文件物理路径。
- Access Token 1 小时，Refresh Token 30 天并轮换；撤销任一令牌会撤销整个令牌家族。
- 每次工具调用和资源读取写 `Audit.AuditEvent`，`Module=MCP`。
- 服务身份令牌必须关联已发布、有效 `AgentProfileId`；工具和 Resources 会再按
  数字员工工具白名单过滤，停用后既有令牌立即失效且不能新签发。

## 配置

主要配置位于 `AIDT:Mcp`：

- `PublicBaseUrl`：浏览器 OAuth 和确认 URL 的公开入口
- `SystemAuditUserId`：无效令牌等无法归属用户的拒绝审计身份
- `MaxRequestsPerMinute`
- `MaxRequestBytes`
- `MaxResponseBytes`
- `AllowedOrigins`：仅浏览器跨域 MCP 客户端需要

远程部署必须在 Nginx/反向代理终止 HTTPS。测试环境可使用 `http://127.0.0.1:8088`。

## Codex 连接

推荐使用远程 Streamable HTTP + 浏览器 OAuth：

```toml
[mcp_servers.aidt]
url = "http://127.0.0.1:8088/mcp"
```

生产环境将 URL 改为经批准的 HTTPS 内网地址。客户端首次连接按 MCP OAuth 发现流程打开浏览器，
登录 AIDT 后选择部门和工具作用域。

stdio 模式用于本机受控进程：

```powershell
$env:AIDT_MCP_TOKEN = '<通过 OAuth 取得的短期令牌>'
.\AIDT.Mcp.exe --stdio
```

不得把原始令牌写入仓库、日志、聊天、前端 localStorage 或长期脚本。

## 测试

```powershell
dotnet test tests/AIDT.McpTests/AIDT.McpTests.csproj
```

SQL 集成：

```powershell
$env:AIDT_RUN_SQL_INTEGRATION_TESTS = '1'
$env:AIDT_SQL_CONNECTION = 'Server=np:\\.\pipe\sql\query;Database=<隔离库>;Integrated Security=True;TrustServerCertificate=True'
$env:ConnectionStrings__Aidt = $env:AIDT_SQL_CONNECTION
dotnet test tests/AIDT.McpTests/AIDT.McpTests.csproj --no-build
```
