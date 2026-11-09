# AGT-003-B 数字员工身份中心交付报告

状态：`COMPLETE_DEPLOYED_AND_FROZEN`

## 🎯 结论

`/agents` 已从运行记录页升级为业务人员可理解、可操作、可追溯的数字员工身份中心。
新建、编辑、校验、发布、停用、恢复、版本和回滚均由网页真实调用 6A API，
数据来自 SQL Server；刷新和重新登录后保持一致。

本交付证明页面没有使用静态身份列表、Pinia 内存或 localStorage 伪造持久化。
权限最终由后端执行，前端按钮仅做体验层收敛。

## 🖥️ 实际页面与操作

- 身份中心：搜索、部门筛选、生命周期筛选、身份卡片、有效/草稿/停用概览。
- 新建与编辑：七段表单，覆盖基本信息、组织负责人、服务身份、自主限额、
  工具能力、人工确认与熔断、数据与模型。
- 生命周期：创建草稿、复制身份、保存、校验、发布、停用、恢复、版本历史、
  回滚和高风险复核。
- 证据链：近期运行、流程节点、WorkflowTask、MCP 客户端/令牌、GatewayCall、
  审计时间线。
- 业务语言：流程配置和节点策略只显示名称、说明、等级、风险、工具和稳定编码；
  不要求普通用户输入 GUID，不显示 AgentProfile 裸 ID。
- 状态覆盖：Loading、Empty、Error、Denied、Conflict、NotPublished、Inactive、
  PendingReview 和 ValidationFailed。
- `/agents?run=...` 自动打开运行记录页签；历史运行在身份停用后仍可追溯，
  新运行按钮不可用并解释原因。

## 🔌 真实联动证据

- `GET /api/v1/agents/options` 返回当前操作者可用的部门、真人负责人、
  未绑定服务账号、注册能力、永久边界、确认规则、熔断和模型范围。
- `GET /api/v1/agents/{id}/context` 聚合身份、运行、流程节点、Task、MCP、
  Gateway 和审计证据，并按权限裁剪 MCP 与审计可见性。
- Agent 概要增加 `lastRunAt`、`recentRunCount`；MCP 客户端/令牌增加关联身份名称、
  身份 ID 和当前可用状态。
- 流程配置和流程策略只消费有效已发布身份；停用后立即从新节点选择中消失。
- MCP 服务令牌必须关联有效已发布 `AgentProfile`；停用撤销已有令牌，
  新签发列表不再出现该身份。
- 页面、日志和网络响应不返回明文令牌、TokenHash、密钥、物理路径或敏感材料。

## 🔐 权限边界

- 平台管理员可管理全组织身份并审核高风险变更。
- 部门负责人只能管理授权部门且不超过自身权限上限的身份。
- 普通业务人员只能查看授权部门中的有效已发布身份，页面不出现编辑/发布动作。
- 数字员工服务身份不能登录治理页面、审核自己或完成真人确认。
- 高风险能力升级必须由第二位平台管理员审核，申请人不能自审。
- 016 只向供应链、财务、生产三个既有业务角色增加 `agent-read`，
  不增加写、发布、审核、停用、回滚或令牌能力。
- 付款、质量放行、配方变更、设备控制、安全联锁、外部合同承诺和客户重大赔付
  继续由后端永久强制人工边界。

## 🗄️ 数据库迁移

- EF：`20261103024548_016_AgentIdentityViewPermissions`
- 脚本：`database/migrations/016_AgentIdentityViewPermissions.up.sql/down.sql`
- up SHA256：`7BCE0A8AD1D536EC076E9F0B756278F36CC30170DA7B339E6ACD5F763B02583F`
- down SHA256：`39A7F396E10EC30E77339C69069DBA42BCDC12F7A4729B016339610E4FC87E1C`
- 不新增表；16 迁移后仍为 68 张应用表。
- 隔离库：`AIDT_AGT003_B_TEST_20261103_104741`
- up/down/re-up：15/68 → 16/68 → 15/68 → 16/68
- 主库已应用：3 项新增 `agent-read`、6 个未绑定数字员工服务账号。
- 主库迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_AGT003_B_PRE_MAIN_016_20261103_1205.bak`
- 备份 SHA256：
  `F507534041F5E4B0D6AD36C54DA0C43ADA07AC7225327544B531B6EF8886821B`

## 🧪 测试结果

| 门禁 | 结果 |
|---|---|
| 后端构建 | 通过，0 警告、0 错误 |
| 后端格式 | 通过 |
| 后端/SQL/API | 172/172，0 失败、0 跳过 |
| MCP | 11/11，0 失败、0 跳过 |
| 前端类型检查 | 通过 |
| Vitest | 20 个文件、41/41 |
| OpenAPI | 162 路径、183 operationId |
| 生产构建 | 通过 |
| npm 生产依赖审计 | 0 漏洞 |
| NuGet 依赖审计 | 无已知漏洞 |
| 源码 Playwright | 44 通过 + 1 个部署专用用例跳过 |
| 正式 Playwright | 45/45，0 失败、0 跳过、0 flaky |

源码和正式 E2E 覆盖：创建并刷新、编辑并重新登录、业务语言校验、发布后可选、
停用后从流程和令牌选项消失、恢复、版本查看和回滚、部门范围、普通只读、
高风险双人审核、MCP 关联和撤销、AgentRun/流程/任务/Gateway/审计反查、
`?run=` 深链、1440/1280/390 三档页面和原有功能回归。

## 📦 正式发布

- 功能分支：`feature/ui-step6b-agent-identity-ui`
- 功能提交：`fb20bfb00030ba9974ed2508974af6a465fd415b`
- 发布版本：`req-ui-step6-final1`
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step6-final1.zip`
- SHA256：`282BDAE5DD0D9D3C2EEF8D240FBD03BC74F2C3F7792F6AA64E2CE29064FC937F`
- manifest：16 条迁移、`sourceRevision=fb20bfb...`、不含 `-dirty`
- 首次部署：`2026-11-03T12:04:19.1491890+08:00`
- 回滚后重新部署：`2026-11-03T12:13:34.4174775+08:00`
- 冻结标签：`req-ui-step6-frozen-v1`

## ↩️ 回滚与恢复

- 应用回滚：`req-ui-step6-final1 → req-ui-step6a-final1 → req-ui-step6-final1`。
- 回滚和重新部署后页面、API ready、Worker、AiGateway、MCP live/ready、
  capabilities 全部 HTTP 200。
- 重新部署记录备份 SHA256：
  `CE4EE1D4C35DDDC72D673464B8440E96D8904AEFEA04F7A1CFCAF4D6A8126A41`。
- 最终主库备份：
  `C:\ProgramData\AIDT\Backups\AIDT_AGT003_B_FINAL1_20261103_1214.bak`
- 最终备份 SHA256：
  `8B6B4C505CC359CB9D04A2E970FC20D0F1C068A5F55F33D6312A0471B2379F85`
- 独立恢复库：`AIDT_AGT003_B_FINAL1_RESTORE_20261103_1214`
- 恢复验证：ONLINE、16 迁移、68 应用表、5 个有效发布身份、790 条 AgentRun、
  3 项新增授权、6 个服务账号。

## 🔌 真实连接边界

- 钉钉、U9、ERP、DCS：未连接。
- 外部大模型：未启用。
- AiGateway：继续 `RuleSimulator`，`ExternalModelCalled=false`。
- 本交付不创建第二套身份、权限、MCP、OAuth、审计或令牌系统。

## 🚧 未完成事项与风险

- 当前为本地测试环境交付，不是生产 SSO、生产 DBA 或正式生产上线。
- 前端仓库仍未配置 ESLint 脚本或依赖；本批实际执行的是类型检查、Vitest、
  OpenAPI、生产构建、依赖审计和浏览器 E2E，不能宣称 lint 已通过。
- 真实企业组织同步、外部系统回执、生产证书与密钥托管仍需甲方环境接入。
- 页面已证明业务人员能够完成已授权身份治理；生产双人审批制度、岗位职责和
  组织主数据仍需甲方流程确认。

## ✅ 阶段目标判断

在本地受控环境中，已达到“业务人员可自行治理数字员工身份”的阶段目标：
授权用户可通过网页完成草稿、发布、停用、恢复和回滚，并看到运行、
流程、MCP 与审计证据。生产推广仍需 SSO、正式审批制度和企业主数据接入。
