# AGT-003-A 影响分析

## 变更目标

把主要用于规则模拟的 `AgentProfile` 升级为有版本、有生命周期、有组织边界和可撤销令牌的
数字员工身份主数据，同时保持现有 AgentRun、Workflow、MCP 和审计链路兼容。

## 涉及功能 ID

- `AGT-003`：数字员工身份生命周期与能力治理
- `AGT-001`：数字员工运行
- `AGT-002`：受限代理
- `AIG-002`：AgentRun 与 AiGateway
- `WFROLE-001`：流程 Agent 节点
- `MCP-001`：MCP 服务身份与令牌
- `IAM-001`：组织范围和高风险双人审核
- `AUD-001`：追加式审计与 Outbox

## 预期修改文件

- Domain：`AgentModels.cs`、新增数字员工能力/永久边界规则。
- Application：Agent 治理契约和端口、DTO 映射、请求校验。
- Infrastructure：EF 实体/配置、迁移 015、SQL Agent 治理服务、查询服务、MCP 服务令牌。
- API：扩展 `AgentsController`，增加治理端点。
- Frontend：同步 TypeScript AgentProfile/版本/请求 DTO；保持现有候选选择器兼容。
- OpenAPI：新增路径、Schema、幂等/并发/权限说明。
- Tests：Agent Domain、Agent API/SQL、MCP 令牌撤销、迁移模型与全量回归。
- Docs：AGT-003 计划/上下文/影响/交付/测试证据、功能地图、权限矩阵、规划文件。

## 上游调用方

- Agent 页面启动、确认、停止和代理运行。
- 听记分析、事件建议和流程 Agent 节点选择 AgentProfile。
- 工作流节点策略保存 AgentProfileId。
- MCP 管理员签发服务身份令牌。
- 部门负责人和平台管理员治理入口。

## 下游依赖

- AgentRun 需要原子消费当前已发布版本的能力上限。
- Workflow 节点解析只接受有效已发布身份。
- MCP 服务令牌需要显式关联 AgentProfile，停用时撤令牌。
- Audit/Outbox 需要记录发布、停用、恢复、审核、回滚和撤令牌证据。

## API 变化

- 保留现有 `GET /agents`、`GET /agents/runs` 与运行动作路径。
- 扩展 `AgentProfileDto`，只新增字段，保持旧字段。
- 新增治理列表、详情、有效候选、草稿、校验、发布、停用、恢复、版本、回滚和审核端点。
- 错误码：身份未发布、无效、停用、自审、高风险待审核、并发冲突、能力不在白名单。

## 数据库变化

- 仅新增 015。
- 新增 `Agent.AgentProfileVersion`。
- 扩展 `Agent.AgentProfile`、`Mcp.Client`、`Mcp.AccessToken`。
- 增加唯一和过滤索引；增加 JSON 与状态检查约束。
- 存量身份一次性回填为已发布 v1，保持现有运行可用。

## 权限变化

- `agent:read/create/update/manage/approve`。
- 平台管理员全平台；部门负责人按授权部门和编辑/发布上限；
  普通用户仅读有效候选；服务身份不得访问治理。
- 高风险发布复用 `Identity.AccessChangeRequest` 双人审核。

## 审计变化

- 新增 `agent.profile.*` 与 `mcp.token.revoked_by_agent` 事件。
- 敏感变更写既有 Audit 和 Outbox；不写令牌明文。

## 前端状态变化

- 旧页面继续使用有效候选列表，不因新增字段破坏。
- 治理端点 TypeScript 契约完整，支持 denied/conflict/notPublished/idempotency replay。

## 测试范围

- 全生命周期、非法状态、版本不可覆盖、回滚生成新版本。
- 部门负责人跨部门拒绝、普通用户只读、服务身份治理/自审拒绝。
- 高风险差值和双人审核、申请人不得自审。
- 工具/模型/数据范围/自主等级/金额/风险/永久边界。
- 未发布/停用不能新运行、新流程解析或签发新服务令牌。
- 停用撤销已有 MCP 服务令牌，历史运行/回执/审计仍可查。
- 并发、幂等、审计和 Outbox、MCP、流程节点与既有 AgentRun 回归。

## 回归风险

- 存量 `AgentProfile` 字段被现有服务和测试直接读取，迁移必须保留并回填。
- MCP 交互式用户令牌没有 AgentProfile 链路，不能被数字员工停用误伤。
- 工作流历史任务已经冻结策略，停用身份只能阻止新解析，不得改写历史任务。
- 普通候选列表数量变化可能影响按名称选择 Agent 的页面，需确保种子兼容。

## 回滚方式

应用回滚到 `req-ui-step5-final1`，数据库恢复 015 前备份；隔离库验证 015 down。
历史 AgentRun、Audit 和 Outbox 不做删除或改写。

