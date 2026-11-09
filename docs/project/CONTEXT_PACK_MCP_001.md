# MCP-001 上下文包

## 🎯 当前目标

实现 AIDT 标准 MCP 服务，支持浏览器授权、工具/Resources 发现、受控业务查询与动作、明确拒绝、高风险真人确认和完整调用审计。

## 📌 冻结基线

- `main@509e62f`
- `req-batch3-final3`
- `req-batch3-closeout-frozen-v1`
- 114 REST 路径 / 134 operationId
- 9 条 EF 迁移 / 57 张应用表
- 当前无 MCP Server、令牌或 MCP 审计

## 🔗 关联功能

- `MCP-001`
- `PLT-001/P LT-002`：身份、权限和数据范围
- `IAM-001/WFROLE-001`：组织、角色、部门和任务授权
- `PMO-001/PMO-002`：项目、成员、里程碑和项目任务
- `EVT-001/TASK-002`：事件、Agent 分析和真人确认
- `WF-001..005/TASK-001`：流程实例、待办和动作
- `DOC-001`：文件、保密等级和下载审计
- `AUD-001`：追加式审计
- `NAV-001`：当前导航树

## 🧩 后端入口

- `AIDT.Api/Program.cs`
- `HttpContextCurrentUserAccessor`
- `IProjectService`
- `IEventInboxService`
- `IWorkspaceQueryService`
- `IFileCenterService`
- `INavigationService`
- `ProcessWorkflowTaskHandler`
- `IWorkflowTaskAssignmentService`
- `IUserRepository`
- `IAuditRepository`

## 🗄️ 数据

现有所有业务表不改字段。新增 `Mcp` Schema，不复制项目、事件、任务、流程、文件或审计主数据。

## 🔐 安全约束

- 每次调用同时校验 MCP token scope、AIDT 用户权限、数据范围和资源范围。
- 令牌只存摘要；原始令牌不落日志、不写 localStorage。
- 平台管理员 MCP token 仍受其选择的部门范围限制。
- 高风险动作必须绑定当前真人用户的一次性确认。
- 不返回物理文件路径、密码、连接串、Cookie、CSRF 或密钥。

## 🧪 必测

- OAuth 授权码、PKCE、令牌刷新、撤销和过期。
- stdio `initialize`、`tools/list`、允许的 `tools/call`。
- HTTP `initialize`、工具发现、Resources。
- 跨部门、跨项目、跨环境、无权限、停用用户和撤销令牌。
- 高风险缺少/伪造/跨用户/重放确认。
- 审计可按客户端、用户、工具、资源、TraceId 反查。
- 迁移 010 up/down/re-up。
- 正式部署 OAuth、MCP 和管理页。

## 🚫 明确排除

- SQL/Shell/任意 HTTP/任意文件系统。
- 外部付款、质量放行、配方修改、DCS 控制。
- 生产 SSO/OIDC/MFA。
- Node.js MCP Server。
