# AGT-003-A 上下文包

## 当前基线

- `main@dc0c945`、工作区干净。
- 前序冻结：`req-ui-step5a-frozen-v1`、`req-ui-step5-frozen-v1`。
- 当前部署：`req-ui-step5-final1`。
- 当前最大 EF 迁移：014；应用表 67 张。
- 技术栈：.NET 10、EF Core SQL Server、Vue 3、Element Plus、Elsa、Hangfire、Garnet、Nginx。

## 现有数字员工

- `AgentProfile` 当前仅有姓名、服务用户、A0–A4、金额、风险、工具 JSON、激活标志。
- `AgentRun` 已关联业务对象、流程、任务、听记、GatewayCall、代理和原始责任人。
- `AgentConfirmation` 已拒绝服务身份自确认。
- Worker 和流程角色已有只读 AgentProfile 解析，但只检查 `IsActive`。
- AiGateway 继续 RuleSimulator；`ExternalModelCalled=false`。

## 现有 IAM / MCP / 审计

- `UserPrincipal` 提供组织、角色、权限、部门范围，服务身份由用户主数据标识。
- `Identity.AccessChangeRequest` 已支持申请人与第二位管理员审核。
- MCP OAuth 复用现有客户、授权码、Access/Refresh Token、撤销和审计。
- 服务身份 MCP 令牌当前只绑定服务用户，不显式绑定 AgentProfile。
- 审计表追加式；Outbox 与业务事务同提交。

## 可复用组件

- `IAuditRepository`、`IOutboxRepository`、`IIdempotencyStore`、`IUnitOfWork`、`IClock`。
- `SqlWorkflowConfigurationService` 的 Revision/幂等/发布事务模式。
- `SqlObjectModelService` 的版本/校验/发布/停用模式。
- `Identity.AccessChangeRequest` 的高风险双人审核。
- `McpToolCatalog` 的受控工具目录与 scope 检查。

## 设计约束

- 不创建第二套身份/权限/令牌系统。
- 不直接写其他模块私有表；Infrastructure 治理服务可在同一事务中协调 Agent、Identity、
  MCP、Audit、Outbox，但通过明确服务边界封装。
- 历史数据不可改写；发布创建不可变快照。
- 所有敏感动作写 Audit 与 Outbox。
- 外部模型和外部系统保持未连接。

## 关键实现决策

- `AgentProfile` 保留当前有效快照以兼容现有运行查询；`AgentProfileVersion`保存不可变历史。
- 发布时先验证版本，再在同一事务切换当前发布指针、更新主表快照并写审计/Outbox。
- 停用时按 `AgentProfileId` 及服务用户关联撤销 MCP 服务令牌，不触碰真人交互令牌。
- 高风险发布自动生成既有 `AccessChangeRequest`，由第二位平台管理员审核后应用。
- 永久人工边界由服务端固定集合强制写入，任何草稿删除或缩小都会被拒绝。

## 必读代码入口

- `backend/AIDT.Domain/Agents/AgentModels.cs`
- `backend/AIDT.Application/Contracts/CommonContracts.cs`
- `backend/AIDT.Api/Controllers/AgentsController.cs`
- `backend/AIDT.Infrastructure/Persistence/Entities.cs`
- `backend/AIDT.Infrastructure/Persistence/AidtModelConfigurations.AuditIntegrationAgent.cs`
- `backend/AIDT.Infrastructure/Persistence/Repositories/SqlAgentRepository.cs`
- `backend/AIDT.Infrastructure/Services/SqlBatch2Service.cs`
- `backend/AIDT.Infrastructure/Services/SqlWorkflowRoleService.cs`
- `backend/AIDT.Infrastructure/Services/SqlMcpOAuthService.cs`
- `backend/AIDT.Mcp/AidtBusinessTools.cs`
- `backend/AIDT.Workflow/WorkflowEngineAdapter.cs`
- `frontend/src/api/types/contracts.ts`

## 验证环境

- 源码库与隔离 SQL 测试库。
- 正式部署入口：`http://127.0.0.1:8088`。
- 发布、回滚、备份恢复沿用既有 `deploy/` 和 `database/` 脚本流程。

