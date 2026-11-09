# DEPT-WF-001 交付报告

状态：`complete_deployed_and_frozen`

## 🎯 结论

部门优先工作空间、部门流程负责人自助维护普通模板、节点级动态表单、任务版本冻结、
可视化流程设计器和四部门试点已在源码与隔离库中闭环。流程、身份、任务、文件、审计和
数字员工继续复用既有真源，没有建立第二套系统。

主库 018、正式部署、正式 E2E、应用回滚、独立恢复和冻结标签均已完成。

## 🧭 8A 部门导航与测试隔离

- 导航改为公司公共入口、部门工作空间、平台治理三层。
- 公共入口固定为“我的工作、统一确认、待办、事件收件箱”，不在部门下重复。
- 导航正文只来自服务端 `Navigation.Workspace/Version/Item`，前端不再硬编码业务入口。
- 路由、资源和动作通过 `NavigationRouteCatalog` 白名单校验，不接受任意 URL、SQL、
  脚本、HTML 或 JavaScript。
- 32 个 `NAV-E2E-*` 测试工作区已标记 `IsTestData=1` 并退役；历史版本、入口和审计保留。
- 319 个测试配置版本已标记 `IsTestData=1`；无测试配置保持发布态。
- 6 个现有部门与 `DELIVERY` 交付中心（Demo）各有且仅有一个有效默认工作区。
- 支持收藏、排序、隐藏和默认部门展开；当前部门由活动成员关系确定。
- Cookie 增加授权指纹，`/api/v1/auth/session` 返回 `Current/Stale/Anonymous`；
  权限变化明确提示“权限已更新，请重新登录”，不再把 403 或旧权限显示成空数组。

## 🧩 8B 流程模板、节点与动态表单

- 流程模板继续使用 `Configuration.ConfigurationVersion`，发布后生成现有
  `Workflow.WorkflowDefinition/WorkflowVersion`；未增加第二套流程引擎。
- 同一部门、同一对象类型支持多个模板和一个默认模板。
- 生命周期覆盖草稿、校验、试运行、发布、停用、恢复、版本历史和回滚。
- 节点覆盖处理人、业务说明、SLA、交付物、相关角色、附件要求、完成条件、节点动作、
  数字员工、受控 AI 审查、通知升级和永久人工边界。
- 17 类字段包括文本、数字、金额/百分比、选择、多选、人员、部门、日期、日期时间、
  日期范围、附件、布尔、只读系统字段和业务对象字段绑定。
- 字段来源严格区分 `BusinessObjectReference`、`TaskInput`、`EvidenceReference`；
  业务对象只引用字段编码，不复制正式字段值。
- 任务创建时冻结 `FormDefinitionJson`、`FormSchemaVersion` 和
  `FormDefinitionHash`。新模板发布不改变历史任务及历史表单。
- 表单草稿和节点动作在服务端重新校验字段类型、必填、角色、人员/部门范围、附件、
  敏感等级、版本哈希、并发和幂等；表单、任务动作、审计与 Outbox 在同一事务内。
- 新增接口：
  - `GET /api/v1/workflow-tasks/{id}/form`
  - `PUT /api/v1/workflow-tasks/{id}/form-draft`
  - `POST /api/v1/workflow-tasks/{id}/actions/ai-review`
- AI 审查只调用现有 `aidt-rule-sim`，响应固定 `ExternalModelCalled=false`，
  只产生建议或草稿，并关联 AgentRun、GatewayCall、任务和业务对象。

## 🎨 8C 可视化工作台与员工运行页

- `/configurations` 升级为左中右工作台：字段/能力库、流程节点和实时表单预览、
  节点属性与人工边界。
- 员工 `/workflows` 以业务节点名为主标题，展示冻结表单、期限、附件、AI 建议和
  永久人工边界；引擎 ID、节点 Code、TraceId 等只放在管理员折叠区。
- 页面覆盖 Loading、Empty、Error、Denied、Disabled、MissingTemplate、MissingObject、
  MissingAttachment、MissingAgent、ValidationFailed、Conflict 和重复提交。
- 保持 Vue 3、TypeScript、Vite、Element Plus、Pinia，没有新增 UI 库或流程引擎。

## 👥 权限与永久人工边界

- 新增范围化角色 `department-flow-owner`，只按真实 `department` 角色分配判定。
- 部门流程负责人只能维护授权部门的普通模板；跨部门、付款、质量放行、配方变更、
  设备控制、安全联锁、外部合同承诺和客户重大赔付继续由平台管理员审核。
- 普通员工只使用本部门已发布模板；服务身份不能创建、发布、审批或启动流程。
- 所有越权、发布、停用、恢复、回滚、表单提交、AI 审查和边界拒绝写追加式审计。

## 🧪 四部门试点路径

| 部门 | 试点路径 | 已验证结果 |
|---|---|---|
| 交付中心（Demo） | 部门导航 → 流程配置 → 交付验收任务 → 表单/附件 → 审计 | 390px 可访问，部门负责人可进入配置 |
| 研发 | 样品测试模板 → 人员/日期/附件节点 → 发起任务 → 审批/退回 → 历史版本 | 冻结表单、规则模拟 AI 审查、历史不变 |
| 供应链 | 请购/到货模板 → 部门负责人创建 → 校验发布 → 发起与任务处理 | 草稿、校验、发布和跨部门拒绝通过 |
| 财务 | 人工复核模板 → 高风险付款边界 → 平台审核 | 部门负责人不能自行发布高风险付款模板 |

试点数据和测试账号均明确标记 `Demo/Test`，不连接真实 PMS、钉钉、U9、ERP、DCS、
WMS、LIMS 或外部大模型。

## 🗄️ 数据库与契约

- 唯一新增迁移：`20261103153650_018DepartmentWorkspaceDynamicForms`。
- 001–017 未修改。
- 018 扩展现有导航、配置、流程定义、节点策略和任务表，没有新增第二套业务真源。
- JSON 列使用 `ISJSON` 约束；任务冻结字段、测试标记、部门默认模板增加约束和索引。
- 正式脚本：
  - `database/migrations/018_DepartmentWorkspaceDynamicForms.up.sql`
  - `database/migrations/018_DepartmentWorkspaceDynamicForms.down.sql`
- OpenAPI：187 路径、208 operationId。

## ✅ 源码测试证据

- .NET 构建：通过，0 警告、0 错误。
- `dotnet format --verify-no-changes`：通过。
- 后端、SQL、API：186/186。
- MCP：11/11。
- 前端 Typecheck：通过。
- Vitest：24 个文件、47/47。
- OpenAPI/TypeScript 契约：187 路径、208 operationId。
- 前端生产构建：通过；生产依赖审计 0 漏洞。
- 源码 Playwright：52 通过、1 项部署专用 MCP OAuth 跳过、0 失败。
- 018 纯 017 还原库：18/17/18 迁移，应用表 71；7 个部门默认工作区；
  32 个测试导航退役；319 个测试配置、0 个测试配置发布中；
  `department-flow-owner` 角色与 26 项权限存在。

## ✅ 主库应用

- 迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_DEPT_WF_001_PRE_018_20261104_0145.bak`
- SHA256：`0CE44730E6C5DDBEE529AEF1076E3658D85F3410504AF93E507744E69961391C`
- `RESTORE VERIFYONLY`：通过。
- `AIDT`：18 迁移、71 张应用表；7 个部门默认工作区；32 个测试导航退役；
  319 个测试配置标记、测试发布态 0；交付中心与部门流程负责人权限存在。

## 🚀 正式部署、回滚与恢复

- 正式部署：`req-ui-step8-final3`。
- 发布包：
  `D:\Apps\AIDT-Test\releases\AIDT-req-ui-step8-final3.zip`
- SHA256：`81B5FC5CB1EAAC66D3E321B0270037FD6E342D2D77004926CA4A28BF48F61C2A`
- manifest sourceRevision：`a86cccd7a36c35f19f750a794969da90135a86b2`，无 `-dirty`。
- 正式全量 E2E：53/53，0 失败、0 跳过、0 flaky。
- 覆盖正式 Nginx、页面、API、Worker、AiGateway、MCP OAuth、四部门试点和
  1440/1280/390px。
- 应用回滚：`req-ui-step8-final3 → req-ui-step7-final1 → req-ui-step8-final3`。
- 回滚与重部署后页面、API、Worker、AiGateway、MCP 健康端点均为 200。
- 重部署后部门与场景专项：8/8。
- 最终备份：
  `C:\ProgramData\AIDT\Backups\AIDT_DEPT_WF_001_FINAL3_FROZEN_20261104_0250.bak`
- SHA256：`9641532E40CF78CFB92676D2698090584C7775A57A4C303999419009242F8071`
- 独立恢复库：`AIDT_DEPT_WF_001_FINAL3_FROZEN_RESTORE_20261104_0250`；
  ONLINE、18 迁移、71 表、7 个部门默认工作区、发布中测试默认入口 0、
  26 项部门流程负责人权限、无效任务表单 0。

### 发布版本说明

- `req-ui-step8-final1` 首次正式登录暴露 Nginx 上游响应头缓冲导致 502，未冻结。
- `req-ui-step8-final2` 修复会话 Cookie 体积后通过一轮全量 E2E，
  但发现测试导航可替换财务正式默认工作区，未冻结。
- `req-ui-step8-final3` 修复测试工作区隔离并通过全部发布门禁，作为最终版本。

## ⚠️ 风险与边界

- 当前身份仍是本机 Demo Cookie，不是生产 SSO。
- AI 仅为规则模拟，不代表真实大模型结论。
- 会签/或签、真实外部连接和外部大模型不在本批范围。
- 正式 E2E 截图位于
  `C:\ProgramData\AIDT\Evidence\req-ui-step8-final3`，不写入发布源码。
