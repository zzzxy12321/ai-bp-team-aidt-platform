# MCP-001 变更计划：AIDT 标准 MCP 服务

> 基线：`main@509e62f`
>
> 当前部署：`req-batch3-final3`
>
> 目标发布：`req-mcp-final1`
>
> 目标冻结标签：`req-mcp-frozen-v1`

## 🎯 目标

实现可供 Codex 和其他标准 MCP 客户端使用的 AIDT MCP 服务。客户端通过浏览器授权后，只能发现和调用明确授权的业务工具；所有数据访问继续受 AIDT 用户、组织、部门、项目、环境、字段和动作权限约束。

## 📦 范围

### 新增

- `backend/AIDT.Mcp`：官方 .NET MCP SDK、Streamable HTTP、stdio。
- `AIDT.Mcp` Domain/Application ports/Infrastructure persistence。
- SQL `Mcp` Schema、迁移 010、客户端/授权码/令牌/真人确认表。
- OAuth 2.1/PKCE、动态客户端注册、令牌刷新与撤销。
- 项目、事件、项目任务、流程、文件、导航、审计工具与 Resources。
- MCP 管理 API、Vue 管理页、真人确认页。
- MCP 权限、导航项、Nginx/MCP Windows 进程、发布与回滚。
- 单元、SQL/API、MCP 协议、stdio 和浏览器测试。

### 不新增

- Node.js MCP 后端。
- SQL、Shell、任意文件系统或任意 HTTP 工具。
- 114 个 REST 路由的一对一包装。
- 绕过现有权限的机器管理员。
- 外部付款、质量放行、配方、设备控制或大规模跨部门查询工具。

## 🧱 数据

迁移 010 新增：

- `Mcp.Client`
- `Mcp.AuthorizationCode`
- `Mcp.AccessToken`
- `Mcp.HumanConfirmation`
- `Mcp.AuthorizationRequest`（短期同意请求，防篡改）

原始令牌、授权码和客户端密钥均只保存 SHA-256 摘要。历史审计只追加，不随令牌撤销删除。

## 🔐 工具作用域

| Scope | 能力 |
|---|---|
| `aidt:read` | 当前用户、导航、项目、事件、项目任务、流程、审计只读 |
| `aidt:write` | 创建项目/事项、分析事件、成员、里程碑、任务完成、项目状态 |
| `aidt:files` | 文件元数据和受控上传/下载 |
| `aidt:approve` | 流程决定、转交、事件真人确认等高风险动作 |
| `aidt:manage` | MCP 管理目录、客户端、令牌和调用审计 |

## 👤 真人确认边界

以下动作第一次调用返回 `HUMAN_CONFIRMATION_REQUIRED` 和确认 URL：

- 事件确认与任务派发；
- 流程通过、退回、终止和转交；
- 项目 Completed/Terminated 等最终状态；
- 敏感/Core 文件正文上传或下载。

调用者必须由当前真人用户在 AIDT 浏览器中确认同一请求摘要，再把 challenge ID 回传给 MCP。服务端一次性消费 challenge，绑定用户、工具、资源和参数摘要。

## ✅ 验收映射

| 需求 | 验证 |
|---|---|
| stdio 初始化与工具发现 | 独立 stdio 集成测试 |
| HTTP 初始化和鉴权 | TestServer + 标准 MCP SDK 客户端 |
| 浏览器授权 | OAuth 授权码/PKCE API 测试 + Playwright |
| 项目、待办、事件查询 | 工具调用测试和正式部署冒烟 |
| 创建事件和 Agent 分析 | 写作用域、现有服务和审计验证 |
| 高风险真人确认 | 无 challenge 拒绝、有 challenge 成功、跨用户/重放拒绝 |
| 无权限/跨部门/跨项目/跨环境/失效令牌 | 正向与负向 API/MCP/SQL 测试 |
| 调用审计 | 按客户端、用户、工具、资源和 TraceId 查询 |
| 重启与撤销 | SQL 持久化、撤销后下一调用立即 401/拒绝 |
| 敏感数据边界 | 响应不含 Cookie、令牌、物理路径、连接串；文件正文二次确认 |

## 🚀 交付门禁

1. `dotnet format`、构建、NuGet 漏洞检查。
2. 非 SQL 单元与 MCP 协议测试。
3. 隔离库迁移 010 up/down/re-up。
4. SQL/API 全量测试。
5. 前端类型、Vitest、OpenAPI、生产构建。
6. 源码桌面/390px Playwright。
7. 发布包从干净提交构建。
8. 部署后 MCP 协议、stdio、OAuth、权限、审计和 E2E。
9. 应用回滚与数据库恢复验证。
10. 冻结标签和交付文档。

## 🔄 回滚

- 应用回到 `req-batch3-final3`。
- 停止并移除 `mcp` 进程与 Nginx 路由。
- 必要时执行迁移 010 down；令牌立即失效。
- 业务数据和既有审计不删除。
