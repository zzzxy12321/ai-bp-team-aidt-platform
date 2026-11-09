# ADR-MCP-001：引入官方 .NET MCP SDK 并建设 AIDT 标准 MCP 服务

> 状态：Accepted
>
> 日期：2026-11-01
>
> 需求：`MCP-001` / `IDEA-MCP-001`

## 背景

AIDT 已提供 REST、流程、项目、事件、文件、IAM 和审计能力，但 Codex、其他标准 MCP 客户端和受控数字员工缺少稳定的工具发现与调用入口。直接把浏览器 Cookie/CSRF 暴露给智能体、让智能体直连 SQL 或逐一包装 114 个 REST 路径都会绕过现有权限、数据范围和高风险人工边界。

## 决策

1. 新增独立 `backend/AIDT.Mcp`，以业务语义工具和 Resources 提供受控能力。
2. 使用官方 NuGet：
   - `ModelContextProtocol.AspNetCore` `2.3.0`
   - `ModelContextProtocol` `2.3.0`
3. 不手写 MCP JSON-RPC；官方 SDK 负责初始化、能力协商、工具/资源发现、调用、取消和 Streamable HTTP。
4. HTTP 模式使用 Streamable HTTP；同一可执行文件以 `--stdio` 启动 stdio 模式。
5. 浏览器授权采用 OAuth 2.1 授权码 + PKCE S256、动态客户端注册、Access/Refresh Token 轮换和撤销。
6. 令牌、授权码、客户端和真人确认持久化到 SQL Server 新 `Mcp` Schema；原始令牌只返回一次，数据库仅保存 SHA-256 摘要。
7. MCP 只复用现有 Application Service 和领域规则，不复制业务逻辑。
8. 每次工具调用和资源读取写现有 `Audit.AuditEvent`；同时审计客户端注册、授权、令牌签发、刷新、撤销和拒绝。
9. 审批、事件真人确认、流程转交、项目最终状态和高敏文件正文使用一次性真人确认挑战。
10. 工具作用域与现有 RBAC/ABAC 双重生效；令牌选择的部门范围不能因 MCP 调用者是平台管理员而绕过。

## 影响

- 新后端项目 `AIDT.Mcp`、新端口、Nginx `/mcp` 反代和发布包组件。
- 新迁移 010、`Mcp` Schema 和 5 张应用表。
- 新增 `mcp:read`、`mcp:manage` 权限及平台管理入口。
- 新增 OpenAPI 管理接口和 MCP 工具/资源清单。
- Cookie 和 MCP 同意页共享 ASP.NET Data Protection 密钥环，保持同源登录体验。
- Node.js 不新增 MCP 后端；Vue 只承担管理页面和真人确认。

## 回滚

- 应用可回滚到 `req-batch3-final3`。
- 迁移 010 down 删除 `Mcp` Schema 及其表；已签发令牌随之失效。
- 回滚应用不会删除业务项目、事件、任务、流程、文件或现有审计。
- 若只禁用 MCP，可停止 `AIDT.Mcp` 并从 Nginx 移除 `/mcp` 路由，现有 AIDT 主业务不受影响。

## 被否决方案

- 直接暴露 REST 路由：工具语义差、调用面过大、易绕过人工确认。
- 允许 SQL/Shell/任意 HTTP 工具：违反 AIDT 硬边界。
- Node.js 单独实现 MCP：引入第二套后端和认证/权限逻辑。
- 使用长期 Cookie 作为 MCP 令牌：无法独立撤销、轮换和按工具授权。
