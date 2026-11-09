# AIDT 后端工程

> 状态：DEPT-WF-001 已完成、部署并冻结
>
> 目标框架：.NET 10
>
> SQL Server：主 `AIDT` 已应用 001–018；018 up/down/re-up 和主库应用均通过

## 🧱 项目边界

| 项目 | 责任 | 允许依赖 |
|---|---|---|
| `AIDT.Api` | HTTP、配置、统一响应、异常、TraceId、健康检查、OpenAPI | Application、Domain、Infrastructure、Workflow、Integrations |
| `AIDT.AiGateway` | OpenAI 兼容受控网关、模型白名单、规则模拟和摘要审计 | 不调用外部模型 |
| `AIDT.Application` | 用例、事务边界、DTO、端口和业务编排 | Domain |
| `AIDT.Domain` | 聚合、状态机、权限主体、审计、Agent 和 Outbox 规则 | 无 |
| `AIDT.Infrastructure` | EF Core 模型、SQL Server 迁移、运行时配置和未就绪实现 | Application、Domain |
| `AIDT.Workflow` | Elsa 集成边界 | Application、Domain |
| `AIDT.Integrations` | 连接器 Adapter 与 Outbox 契约 | Application、Domain |
| `AIDT.Mcp` | 官方 .NET MCP SDK、Streamable HTTP、stdio、OAuth 2.1、工具与 Resources | Application、Domain、Infrastructure、Workflow |
| `AIDT.Worker` | 后台任务宿主骨架 | Application、Domain、Infrastructure、Workflow、Integrations |

禁止反向依赖，禁止 Controller 直接访问 DbContext，禁止 Infrastructure 反向进入 API。

## 🛠️ 本地验证

```powershell
dotnet restore AIDT.slnx
dotnet build AIDT.slnx --no-restore
dotnet test AIDT.slnx --no-build
dotnet format AIDT.slnx --verify-no-changes --no-restore
```

启动 API：

```powershell
dotnet run --project backend/AIDT.Api --launch-profile http
```

`/health/live` 和 `/health/ready` 均返回 `200`。MCP 本地协议验证：

```powershell
dotnet test tests/AIDT.McpTests/AIDT.McpTests.csproj
```

Domain、Application、契约、SQL/API、MCP HTTP/stdio 和工作流回归通过后才可发布。

## 🔒 当前限制

- 真实 SSO 尚未接入；本地 Development 演示身份和 Cookie 只用于 Demo，不能表述为生产 SSO。
- 部门/环境过滤、文件持久化、敏感文件门禁和跨部门任务流转已验证。
- IAM、导航和流程业务角色处理人解析已接入 SQL Server、权限和审计。
- 事件和项目状态使用真实跟踪事务持久化；PROJECT 文件上传自动形成项目证据关系。
- 业务日期集中使用 `Asia/Shanghai` 18:00，不依赖服务器默认时区。
- 场景启动摘要以 `ScenarioLaunch` 为索引，实时联查当前活动 `WorkflowTask`、
  `WorkflowInstance`、`ProjectTask`、`EventInbox`、`AgentRun` 和业务对象，不复制状态。
- `GET /api/v1/scenarios/launches` 只返回当前用户发起记录，并保留真实证据深链。
- 部门导航、流程模板、节点动态表单和任务冻结版本复用现有 Navigation/Configuration/
  Workflow/Audit/Outbox 真源；018 是唯一新增迁移。
- `department-flow-owner` 仅在部门 scope 生效；部门负责人可维护低风险单部门模板，
  高风险和跨部门动作仍由平台审核。
- MCP 新增迁移 010 和 `Mcp` Schema；001–009 不改写。
- 当前后端/SQL/API 与 MCP 测试结果以本批交付报告为准。
- AiGateway 只执行规则模拟，不调用外部模型。
- 不连接 U9、ERP、DCS、钉钉或模型网关。
- 当前源码回归：后端/SQL/API 185、MCP 11、Vitest 47、OpenAPI 187/208；
  源码 E2E 52 通过、1 项部署专用 MCP 跳过。
