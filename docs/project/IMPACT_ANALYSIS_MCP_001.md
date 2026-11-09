# MCP-001 变更影响分析

## 变更目标

新增标准 MCP 接入层，以业务工具和 Resources 暴露当前用户已授权数据及受控动作，并保持现有权限、人工边界与审计为最终真源。

## 上游调用方

- Codex 和其他 MCP 客户端。
- AIDT Vue 管理页面与浏览器授权页。
- 既有 AIDT Cookie 会话。
- 受控数字员工/服务身份（令牌继承明确授权）。

## 下游依赖

- Identity：用户、角色、权限、部门。
- PMO：项目、事件、任务。
- Workflow：实例、任务、决定和转交。
- Document：文件元数据和正文。
- Navigation：当前导航。
- Audit：追加式调用审计。
- SQL Server：MCP 授权状态持久化。

## API 变化

- 新增 MCP 管理 REST 路径和 OpenAPI operationId。
- 新增 OAuth metadata/authorize/register/token/revoke 端点；这些是 MCP 授权协议端点，不是业务 REST 包装。
- 新增 MCP Streamable HTTP `/mcp`。
- 现有 114 路径和 134 operationId 不删除、不改语义。

## 数据库变化

- 新增迁移 010 和 `Mcp` Schema。
- 新增 5 张表及索引/检查约束。
- 新增 MCP 权限和导航种子。
- 不修改现有业务表字段。

## 权限变化

- `mcp:read`：查看工具目录、客户端、令牌和调用审计。
- `mcp:manage`：撤销客户端/令牌。
- Token scope 与现有业务权限双门禁。
- 管理员角色不覆盖 token 部门选择。

## 审计变化

- 工具调用、资源读取、授权、拒绝、签发、刷新、撤销和真人确认写 `Audit.AuditEvent`。
- 敏感原文、Prompt、Cookie、CSRF、原始令牌和密钥不写审计或日志。

## 前端变化

- 新 `/mcp-center` 管理页。
- 显示工具目录、授权客户端、令牌、真人确认和调用审计。
- 浏览器授权同意页由 MCP 服务输出，使用同一 AIDT 会话与视觉语言。
- 路由和管理入口受 `mcp:read/manage` 控制。

## 部署变化

- 新 `AIDT.Mcp.exe` 进程/端口。
- Nginx `/mcp`、`/oauth/*`、`.well-known` MCP 路由。
- 发布 manifest 增加 MCP 版本和授权模式。
- build/deploy/rollback 脚本新增 MCP 进程、健康检查和回滚。

## 测试范围

- Domain：作用域、令牌状态、确认状态。
- Application/Infrastructure：OAuth、PKCE、摘要、撤销、确认。
- API：管理端点、Cookie 会话、CSRF。
- MCP：stdio/HTTP 初始化、tools/list、tools/call、resources/list/read。
- SQL：迁移、唯一键、过期/撤销、确认一次性。
- 安全：跨部门/项目/环境、无权限、失效 token、敏感字段与路径。
- E2E：登录、MCP 页面、授权、确认、审计追溯。
- 部署：健康、撤销、重启持久化、回滚和恢复。

## 回归风险

- Data Protection 配置调整可能影响现有 Cookie 登录。
- 新增 MCP 项目可能改变发布体积和启动顺序。
- 管理导航种子可能影响现有导航版本。
- 审计量增加可能影响查询性能，必须有索引和分页上限。

## 回滚方式

- 应用回到 `req-batch3-final3`。
- 停止 MCP 进程并移除 Nginx 路由。
- 必要时执行迁移 010 down。
- 不删除既有业务数据、审计或需求池更新。
