# AGT-003-A 数字员工身份与能力治理后端交付报告

状态：`COMPLETE_DEPLOYED_AND_FROZEN`

## 🎯 结论

数字员工身份已升级为具备不可变版本、业务编码、独立服务身份、部门与负责人、
A0–A4、风险/金额、数据/模型/工具白名单、永久人工边界、生命周期和令牌撤销的正式主数据。
停用会立即阻止新运行、新流程节点解析和新 MCP 服务令牌，并撤销已有服务令牌；历史运行和审计保留。

## ✅ 实际实现

- `Agent.AgentProfile` 作为当前有效快照，`Agent.AgentProfileVersion` 保存不可变历史。
- 草稿创建/编辑、校验、发布、停用、恢复、历史查询和回滚；已发布版本禁止原地修改。
- 回滚从历史快照创建新的下一版本，不切换或改写旧记录。
- 平台管理员、部门负责人、普通业务人员和服务身份后端边界。
- 高风险能力变化自动进入既有 `Identity.AccessChangeRequest` 双人审核，申请人不能自审。
- 永久人工边界服务端强制补全：付款、质量放行、配方变更、设备控制、安全联锁、
  外部合同承诺、客户重大赔付。
- AgentRun、工作流 Agent 节点和 MCP 服务令牌只解析已发布、有效身份。
- MCP 服务令牌显式关联 `AgentProfileId`，按数字员工能力白名单过滤工具与 Resources。
- 停用撤销服务令牌并保存撤销人、时间、原因；历史运行、回执和审计不删除。
- 403 越权请求由独立 DbContext 写 `Security.request.denied` 审计，避免污染或重放业务事务。
- 所有敏感身份变更写 Audit 和 Outbox，使用幂等键和显式 Revision/Version 并发控制。

## 🔌 API 与权限

| API | 用途 |
|---|---|
| `GET /api/v1/agents` | 当前身份可使用的有效数字员工 |
| `GET /api/v1/agents/available` | 按部门和搜索查询有效候选 |
| `GET /api/v1/agents/manage` | 治理分页列表 |
| `GET /api/v1/agents/{id}` | 身份详情 |
| `POST /api/v1/agents/drafts` | 创建初始草稿 |
| `POST /api/v1/agents/{id}/drafts` | 从当前发布版本创建后续草稿 |
| `PUT /api/v1/agents/{id}/drafts/{versionId}` | 更新草稿 |
| `POST /api/v1/agents/{id}/versions/{versionId}/actions/validate` | 校验 |
| `POST /api/v1/agents/{id}/versions/{versionId}/actions/publish` | 发布或提交高风险审核 |
| `POST /api/v1/agents/{id}/actions/disable` | 停用并撤销服务令牌 |
| `POST /api/v1/agents/{id}/actions/resume` | 恢复 |
| `GET /api/v1/agents/{id}/versions` | 不可变版本历史 |
| `POST /api/v1/agents/{id}/actions/rollback` | 从历史版本创建新发布版本 |
| `GET /api/v1/agents/change-requests` | 待审/历史高风险变更 |
| `POST /api/v1/agents/change-requests/{requestId}/actions/review` | 第二位平台管理员审核 |

权限：`agent:read/create/update/manage/approve`。部门负责人只能在授权部门、
自身权限及 A0–A2/Low/金额 0 上限内执行；更高中高风险能力由平台管理员双人审核。

## 🗄️ 迁移

- EF：`20261103004434_015_AgentProfileGovernance`
- 脚本：`database/migrations/015_AgentProfileGovernance.up.sql/down.sql`
- 新增：`Agent.AgentProfileVersion`
- 扩展：`AgentProfile`、`Mcp.Client`、`Mcp.AccessToken`、`AccessChangeRequest` 类型约束
- 隔离库：`AIDT_AGT003_A_TEST_20261103_084904`
- up/down/re-up：14/67 → 15/68 → 14/67 → 15/68
- 主库：15 迁移 / 68 表；3 个存量身份均生成 v1 发布快照

## 🧪 测试结果

- 后端/SQL/API：171/171，0 失败、0 跳过。
- MCP：11/11，0 失败、0 跳过。
- Vitest：18 个文件、35/35。
- OpenAPI：160 路径、181 operationId。
- 源码 Playwright：40 通过、1 个部署专用 MCP OAuth 用例跳过、0 失败。
- 覆盖生命周期、不可覆盖版本、历史/回滚、并发、幂等、部门与普通用户边界、
  服务身份治理拒绝、双人审核/自审拒绝、未发布/停用阻止新运行、
  停用撤销令牌、历史运行保留、工具/模型/数据/金额/风险/永久边界。

## 🚦 正式门禁

- [x] 功能分支实现
- [x] 主库前置备份与恢复校验
- [x] 015 隔离库 up/down/re-up
- [x] 源码全量测试与三端 E2E
- [x] 提交功能分支
- [x] 干净构建 `req-ui-step6a-final1`
- [x] 正式部署与正式 E2E 41/41
- [x] 应用回滚 `final1 → req-ui-step5-final1 → final1`
- [x] 最终数据库备份和独立恢复验证
- [x] 创建 `req-ui-step6a-frozen-v1`

## 📦 正式发布

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step6a-final1.zip`
- SHA256：`32E201A25804D9B5876771D197CEE38ABFD992FF9BFCFA313CC69C8931264456`
- 发布源码：`e2be3c863deee1716b7de48bf4d857cdf1851332`
- 部署版本：`req-ui-step6a-final1`
- 最终部署：`2026-11-03T10:17:16.5259368+08:00`
- 正式 E2E：41/41，0 失败、0 跳过、0 flaky。
- 部署前备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-step6a-final1_20261103_101707.bak`
- 备份 SHA256：`91C583FCC1D9D0D373A863F5B363A192BA9D35EA7E5264565102F8404596CF5F`
- 独立恢复库：`AIDT_AGT003_A_FINAL1_RESTORE_20261103_1017`
- 恢复验证：ONLINE、15 迁移、68 应用表、3 个有效发布身份、750 条历史 AgentRun、
  5 项 Agent 权限、`Mcp.AccessToken.AgentProfileId` 存在。
- 应用回滚后页面、API、Worker、AiGateway、MCP 与 capabilities 均返回 200；
  再部署 6A 后全部保持 200。

## 🔌 真实连接边界

- 钉钉、U9、ERP、DCS：未连接。
- 外部模型：未启用。
- AiGateway：继续 `RuleSimulator`，`ExternalModelCalled=false`。

## 🚧 未完成与风险

- 本阶段是后端治理能力；管理页面按目标文件范围留给 6B。
- MCP 服务身份的工具可见性按当前能力白名单过滤，但完整业务页面仍需后续 UI 验收。
- 当前部署为本地测试环境，不代表生产 SSO、生产 DBA 或正式上线。

