# AIDT Demo 权限矩阵

## 🧭 DEPT-WF-001 部门工作空间与流程模板

| 动作 | 普通员工 | 部门流程负责人 | 平台管理员 | 服务身份 |
|---|---|---|---|---|
| 查看导航 | 仅授权部门与平台入口 | 授权部门与平台入口 | 全组织、含测试治理入口 | 禁止登录 |
| 使用已发布模板 | 本部门、非测试、已发布 | 授权部门 | 全组织 | 不能创建/发布/审批/启动 |
| 创建/编辑/校验/试运行普通模板 | 否 | 仅授权部门 | 是 | 否 |
| 发布低风险单部门模板 | 否 | 是 | 是 | 否 |
| 跨部门或高风险模板发布 | 否 | 否 | 平台审核 | 否 |
| 财务付款、质量放行、配方变更、设备控制、安全联锁、外部合同承诺、客户重大赔付 | 永久人工 | 永久人工 | 永久人工 | 永久禁止 |
| 查看任务冻结表单 | 当前处理人/冻结候选人 | 授权部门 | 全组织 | 仅已授权 Agent 运行范围 |
| 保存表单草稿、通过、退回、终止 | 当前处理人 | 当前处理人 | 当前处理人；不得绕过任务归属 | 否 |
| AI 审查/建议 | 只读建议 | 只读建议 | 只读建议 | `aidt-rule-sim` 规则模拟 |

- `department-flow-owner` 必须同时具有 `department` scope；角色名称本身不构成授权。
- 跨部门人员、对象、流程、附件或 Agent 由后端按真实 ID 和范围再次拒绝。
- 发布、停用、恢复、回滚、表单提交、AI 审查和拒绝均写追加式审计。
- 测试导航使用 `IsTestData=1` + `Retired`；测试配置使用 `IsTestData=1`，
  不允许重新污染普通用户主视图。

## 🧭 SCN-001-A 岗位业务场景权限

| 权限/动作 | 普通业务人员 | 部门负责人 | 平台管理员 | 服务身份 |
|---|---|---|---|---|
| `scenario:read` | 是，已发布且部门可用 | 是，授权部门 | 是 | 否 |
| `scenario:create/update/manage` | 否 | 是，授权部门且不得超权限上限 | 是 | 否 |
| `scenario:approve` | 否 | 是，仅普通场景 | 是 | 否 |
| 高风险场景发布/回滚 | 否 | 否 | 第二位平台管理员审核后应用 | 否 |
| 启动已发布场景 | 是，需对象/流程/任务/文件/听记/Agent 原权限 | 是 | 是 | 否 |
| 查看草稿/停用场景 | 否 | 授权部门 | 全组织 | 否 |
| 查看启动映射 | 发起人、部门负责人、平台管理员 | 授权部门 | 全组织 | 否 |

- 场景不扩大任何对象、流程、任务、文件、听记、项目或 Agent 原权限。
- 采购、财务和质量场景的发布进入 `Identity.AccessChangeRequest` 的 `ScenarioPublish` 双人审核。
- 场景停用只阻止新启动，不删除历史版本、启动映射或真实业务链。
- 付款、质量放行、配方变更、设备控制、安全联锁、外部合同承诺和客户重大赔付继续永久人工。

> 身份来源：SQL Server `Identity` Schema 中的本地演示数据  
> 生产 SSO：未接入
>
> 当前批次状态：`SCN_001_B_COMPLETE_DEPLOYED_AND_FROZEN`

### 7B 前端与试用闭环补充

| 页面/动作 | 普通业务人员 | 部门负责人 | 平台管理员 | 服务身份 |
|---|---|---|---|---|
| 场景搜索、部门/岗位/分类筛选 | 仅后端返回的可见场景 | 授权部门 | 全组织已发布场景 | 禁止 |
| 场景卡片当前可发起 | 已发布、部门/角色范围适用 | 是 | 是 | 禁止 |
| 七步发起与预检 | 需对象、流程、任务、文件、听记和 Agent 原权限 | 是 | 是 | 禁止 |
| 我的场景 | 仅本人发起的启动记录 | 本人发起；治理页可管理部门 | 本人发起；治理页全组织 | 禁止 |
| 配置中心场景入口 | 不可见且 API 403 | 授权部门 | 全组织 | 禁止 |
| 草稿、校验、发布、停用、恢复、版本、回滚 | 否 | 在授权部门和能力上限内 | 是 | 否 |
| 高风险采购/财务/质量发布 | 否 | 否 | 第二位平台管理员审核后应用 | 否 |

- “我的场景”摘要由 `ScenarioLaunch` 联查原流程任务、项目任务、事件、用户和对象，
  前端不计算或维护第二套状态。
- 普通员工页面不显示 WorkflowDefinition ID、节点代码、TraceId、权限编码、Token 或物理文件路径。
- 场景启动后的任务继续出现在既有 `/workflows`、`/my-work` 和项目任务中，不复制任务。

## 🖥️ AGT-003-B 数字员工身份中心

| 页面/动作 | 平台管理员 | 部门负责人 | 普通业务人员 | 服务身份 |
|---|---|---|---|---|
| `/agents` 搜索、部门/状态筛选 | 全组织 | 授权部门 | 授权部门，仅有效已发布 | 禁止登录 |
| 新建、复制、编辑草稿 | 是 | 是，部门与权限上限内 | 否 | 否 |
| 校验、发布、停用、恢复、回滚 | 是 | 是，授权部门且不超上限 | 否 | 否 |
| 高风险双人审核 | 是，第二位管理员 | 否 | 否 | 否 |
| 查看版本、运行、流程和 Gateway 证据 | 是 | 是，授权部门 | 仅有效身份摘要；敏感证据裁剪 | 否 |
| 查看 MCP 客户端/令牌与审计时间线 | 是 | 是，授权部门 | 后端返回不可见标志 | 否 |
| 流程配置/节点策略选择数字员工 | 是 | 授权部门且已发布有效 | 无配置权限时只读 | 否 |
| MCP 服务令牌签发 | 是 | 否 | 否 | 不得自行签发 |

- 016 只向供应链、财务、生产三个既有业务角色增加 `agent-read`，不增加写、发布、
  审核、停用、回滚或令牌能力。
- 前端按真实权限隐藏动作，但所有读写仍由 API 最终校验；隐藏按钮不是授权控制。
- `/agents?run=...` 自动进入运行记录页签，停用身份的历史运行仍可追溯，但不可新建运行。
- MCP 页面只显示有效已发布身份用于服务令牌；停用后立即显示不可用并禁止新签发。
- 页面和 API 不返回明文令牌、TokenHash、密钥、物理路径或完整敏感材料。

## 🤖 AGT-003-A 数字员工身份治理

| 权限/动作 | 平台管理员 | 部门负责人 | 普通业务人员 | 服务身份 |
|---|---|---|---|---|
| 查看有效数字员工 | 是，全组织 | 是，授权部门 | 是，授权部门且仅 Published/Active | 不登录治理页面 |
| 查询治理详情与版本 | 是 | 是，授权部门 | 否，草稿/停用不可见 | 否 |
| `agent:create/update` 草稿 | 是 | 是，授权部门和有效上限内 | 否 | 否 |
| `agent:approve` 发布 | 是 | 是，A0–A2/Low/金额 0；更高能力转平台审核 | 否 | 否 |
| `agent:manage` 停用/恢复/回滚 | 是 | 是，授权部门 | 否 | 否 |
| 高风险变更审核 | 是，且必须是第二位平台管理员 | 否 | 否 | 否 |

- 部门负责人必须在授权部门内，且工具所需权限、数据范围不得超过自身有效权限。
- 自提权、跨部门、超上限和高风险自审均由后端拒绝并写 `Security.request.denied` 审计。
- 发布版本不可覆盖；修改已发布身份必须创建新草稿；回滚创建新版本，不改写历史快照。
- 服务身份不能登录治理页面，不能审批自己的身份，也不能完成真人确认。
- 停用立即阻止新运行、新流程 Agent 节点解析和新 MCP 服务令牌签发，并撤销已有服务令牌。
- MCP 服务令牌必须关联已发布有效 `AgentProfileId`；工具和 Resource 调用继续按能力白名单过滤。

## 🧱 META-001-B 建模与动态表单权限

| 权限/页面 | 普通业务员工 | 平台管理员 | 最终判定 |
|---|---|---|---|
| 业务建模工作室 | 否 | 是 | 路由、按钮和服务端均要求 `object-model:read` + platform-admin |
| 对象类型创建/编辑 | 否 | 是 | `object-model:create/update` |
| 校验/停用/恢复 | 否 | 是 | `object-model:manage` |
| 发布 | 否 | 是 | `object-model:approve` |
| 已发布类型读取 | 是，仅发布快照 | 是 | 普通用户列表不返回草稿/停用类型 |
| 创建业务对象 | 原有业务角色 | 是 | `business-object:create` + 部门/组织/环境 |
| 编辑业务对象 | 业务角色，授权部门内 | 是 | 014 授权 `business-object:update`；仍校验 `expectedVersion` |
| 查看对象版本/关联 | 原对象读取权限 | 是 | 版本、上下文接口再次校验对象访问范围 |
| User/Department 字段候选 | 授权部门内 | 组织范围 | 不返回服务身份、停用用户或无权部门 |

- Core 字段在对象列表、详情、编辑和历史版本中继续返回 `***`。
- 前端隐藏按钮不替代 API 403；冲突以服务端 409 显示并要求重新读取。
- 建模不授予任意 HTML、JavaScript、SQL 或数据源表达式。

## 🧱 META-001-A 业务对象建模

| 权限 | 普通员工 | 平台管理员 | 数据/行为边界 |
|---|---|---|---|
| `object-model:read` | 否 | 是 | 读取对象类型版本历史；5B 管理入口可用 |
| `object-model:create` | 否 | 是 | 新建 Draft 对象类型 |
| `object-model:update` | 否 | 是 | 保存新草稿版本，不改写已发布快照 |
| `object-model:manage` | 否 | 是 | 校验、停用、恢复 |
| `object-model:approve` | 否 | 是 | 发布版本 |
| `business-object:update` | 否 | 是，及原有授权角色 | 校验发布字段、`expectedVersion`、部门和环境 |

- 对象建模写接口还显式要求 `platform-admin`，出现新授权角色时不会仅凭权限码自动获得建模能力。
- 对象类型草稿、对象实例和业务对象快照均写追加式审计。
- Core 字段对非平台管理员继续返回 `***`；字段从草稿删除后，历史 Core 值仍按已发布快照遮蔽。
- 工作流继续通过 `objectTypeCode` 引用，不把字段复制进流程实例；PROJECT 与 PMO 保持兼容。

## 🧩 WS-002 可配置岗位工作台模板

| 权限 | 普通员工 | 部门负责人 | 平台管理员 |
|---|---|---|---|
| `workbench-template:read` | 是，仅匹配模板和原权限内卡片 | 是，授权部门 | 是，组织范围 |
| `workbench-template:manage` | 否 | 是，授权部门的业务模板 | 是，含系统模板 |
| `workbench-template:publish` | 否 | 是，授权部门的业务模板 | 是，含系统模板 |

- 系统模板可复制，不可删除；发布版本不可原地覆盖。
- 同一部门按用户、角色、部门、组织默认顺序匹配，只返回一个已发布模板。
- 卡片再次执行原业务权限、组织/部门/项目/环境范围和永久人工边界。
- 模板配置不会扩大任何业务数据范围；前端隐藏按钮不替代 API 403。
- 创建、修改、复制、发布、停用、恢复和分配写追加式审计。
- 正式部署 `req-ui-step4-final1` 的 37/37 E2E 已覆盖跨部门拒绝、部门负责人复制边界、
  用户级优先、草稿不生效、发布/停用/恢复和无权限卡片不返回。

## ✅ UX-004-A 事件确认与流程发起

`POST /api/v1/inbox/{id}/actions/confirm-and-start-flow` 使用 Application Handler
在一个事务内完成事件确认、项目任务、项目关系、流程和审计。

| 校验层 | 规则 |
|---|---|
| 事件确认 | 必须具备 `event-inbox:confirm`，且事件属于授权部门与环境 |
| 项目管理 | 必须能管理所选项目；跨部门项目直接拒绝 |
| 负责人 | 必须是当前组织有效真人用户，且是所选项目的有效成员 |
| 截止时间 | `DueAt` 必填，按客户端完整 `date-time` 保存；页面固定按 `Asia/Shanghai` 解释 |
| 流程提交 | 发起新流程时继续校验 `workflow-instance:submit`、对象状态、版本和部门范围 |
| 已有流程 | 必须属于当前项目或允许显式关联的同部门流程 |
| 幂等与并发 | 幂等键绑定完整编辑内容；事件状态条件更新和任务唯一约束阻止重复派发 |
| 审计 | 记录项目、负责人、截止、优先级、任务、流程和不会执行的高风险动作 |

权限边界：

- 页面按钮隐藏或禁用不替代后端权限；缺负责人/截止时间返回 400，越权返回 403。
- `project-manager` 具备事件确认和项目任务能力，但发起流程仍要求其拥有
  `workflow-instance:submit`；没有该动作权限时不得通过确认接口绕过。
- Agent、MCP 和服务身份不能调用该真人确认动作替代本人确认。
- 高风险事项只形成真人跟进任务，不自动付款、放行、改配方、控制设备、解除联锁、
  承诺合同或赔付。

## ✅ UX-004-B 统一人工确认中心

| 来源/动作 | 权限和数据范围 |
|---|---|
| 统一确认列表 | 登录 + `dashboard:read`；各来源独立返回并独立拒绝 |
| 事件确认 | `event-inbox:read/confirm` + 组织/部门/环境 |
| 听记行动项 | `minutes:read/update/approve` + 组织/部门/环境 |
| 听记负责人候选 | 听记部门有效真人成员；服务身份不可选 |
| MCP 业务视图 | 当前真人 + 组织；只返回该用户自己的确认 |
| MCP 通过/拒绝 | 单条、绑定本人、`Pending`、未过期；意见必填 |
| AgentRun | `agent:read/approve` + 组织/部门范围；服务身份不能自确认 |
| 项目/对象/流程候选 | 选择结果必须与听记部门和环境一致 |

- 统一页没有批量 MCP 决策入口；每个确认必须单独填写意见。
- MCP 响应不含 Access/Refresh Token、授权码、Cookie 或 CSRF。
- 永久人工边界继续阻止 Agent 自动付款、质量放行、配方、设备、联锁、合同和重大赔付。
- 正式部署 `req-ui-step2-final1` 的 29/29 E2E 已覆盖无组织范围拒绝、深链权限和
  四类来源独立拒绝；应用回滚及独立数据库恢复继续使用相同权限规则。

## 🧭 UX-003 统一工作事项权限

`GET /api/v1/my-work` 是只读聚合，不扩大任何来源权限。每个来源分别执行：

| 来源 | 动作权限 | 数据范围 | 可见条件 |
|---|---|---|---|
| WorkflowTask | `workflow-task:read` | 组织 + 部门 | 当前处理人或冻结候选人；发起人显示“我发起” |
| ProjectTask | `project-task:read` | 组织 + 部门 + 项目 | 当前负责人或创建人；如关联流程任务则优先展示流程待办 |
| EventInbox | `event-inbox:read/confirm` | 组织 + 部门 + 环境 | `AwaitingConfirmation`；已有事件生成任务时不再展示事件 |
| MinutesActionItem | `minutes:read/approve` | 组织 + 部门 + 环境 | 来源创建人或指定/建议执行人；仅 `Draft` |
| McpHumanConfirmation | 当前真人确认 | 当前用户 + 组织 | 仅本人、`Pending`、未过期 |
| AgentRun | `agent-run:approve` | 组织 + 部门 + 环境 | 本人发起且 `AwaitingConfirmation` |

聚合接口只返回显示模型；点击后仍进入原详情并接受原 API 权限、幂等和审计校验。
永久人工边界只影响风险展示和排序，不会被摘要入口弱化。

## 👤 本地演示身份

| 用户 | 角色 | 主要能力 | 数据范围 |
|---|---|---|---|
| `demo.admin` | platform-admin | 全部权限、跨部门、显式跨环境、部门维护 | Demo 组织 + 研发主部门 |
| `demo.researcher` | rd-engineer | 对象创建、流程、审计、部门文件 | Demo 组织 + 研发 |
| `demo.qa` | quality-engineer | 质量审批、退回/终止、部门文件 | Demo 组织 + 质量 |
| `demo.supply` | supply-specialist | 对象创建、采购审批、连接器、部门文件 | Demo 组织 + 供应链 |
| `demo.finance` | finance-approver | 财务审批、流程、部门文件 | Demo 组织 + 财务 |
| `demo.production` | production-team | 生产任务、部门文件 | Demo 组织 + 生产 |
| `demo.noscope` | rd-engineer | 仅有角色权限、无组织范围，用于拒绝测试 | 无 |
| `demo.pm` | project-manager | 建项目、成员、里程碑、事件确认和项目任务 | Demo 组织 + 研发部门 |
| `demo.security` | platform-admin | 高风险授权第二审核人、跨部门平台治理 | Demo 组织 + 研发部门 |

## 🛂 权限层

| 层 | Demo 实现 | 验证 |
|---|---|---|
| 登录 | SQL 用户 + HttpOnly Cookie | 登录和未登录 401 |
| CSRF | Cookie + `X-CSRF-TOKEN` | 写请求中间件 |
| RBAC | 角色/资源/动作 | 有菜单无 API 权限返回 403 |
| 组织范围 | `DataScope=organization` | 无组织范围返回 `DATA_SCOPE_DENIED` |
| 部门范围 | `DataScope=department:<guid>` + `UserDepartmentMembership` | 跨部门返回 `DEPARTMENT_SCOPE_DENIED`，SQL/API/E2E 已验证 |
| 环境范围 | 默认当前环境；管理员可显式 `includeTestData` | 对象、流程和文件环境过滤已验证 |
| 字段脱敏 | Core 字段对非平台管理员显示 `***` | 真实 API 创建/读取测试 |
| 高风险动作 | Agent 人工确认和停止 | 服务身份自确认拒绝 |
| 审计 | 登录后业务动作写追加式审计 | 事务、时间线和 E2E |
| 文件访问 | 部门 + 动作 + 环境 + 保密等级 + 扫描状态 | 上传、下载、预览、版本和越权审计已验证 |
| AI 听记 | `minutes:read/create/update/approve` + 部门/环境 | 上传、提取、人工确认、任务追溯和幂等已验证 |
| 节点策略 | 员工只读，平台管理员发布 | 策略随 WorkflowVersion 冻结到任务 |
| Agent 代理 | 节点显式授权 + 真人确认 + 独立服务身份 | 低风险通过；永久人工边界拒绝 |
| AiGateway | AgentRun 调用 ID、Run ID、模型和摘要 | `RuleSimulator`，外部模型关闭 |
| 项目访问 | `project:read` + 组织/部门/成员/负责人范围 | 非项目成员直接 ID 返回 `PROJECT_SCOPE_DENIED` 并写审计 |
| 项目管理 | `project:update`、`project-member:manage`、`milestone:manage` | 平台管理员、项目负责人或授权部门负责人；状态、版本、快照和审计一致 |
| 项目文件证据 | `file:create` + 项目/部门/环境/保密等级 | PROJECT 上传自动建 SupportingEvidence；不要求二次手工关联 |
| 项目任务 | `project-task:create/read/update` | 项目经理创建；任务负责人或项目经理完成 |
| 事件收件箱 | `event-inbox:read/create/update/confirm` | Agent 只能建议；确认必须真人权限；刷新后状态保持 |
| MCP 治理 | `mcp:read/manage` | 平台管理员查看工具、客户端、令牌、确认和调用审计；仅管理权限可撤销客户端/令牌 |

## 📁 文件权限

| 权限 | 平台管理员 | 业务角色 |
|---|---|---|
| `file:read` | 是 | 是 |
| `file:create` | 是 | 是 |
| `file:update` | 是 | 是 |
| `file:delete` | 是 | 是，但只创建删除申请 |
| 下载敏感未扫描文件 | 是 | 否 |
| 查看历史版本 | 是 | 是 |
| 查看物理路径 | 否 | 否 |

## 🏢 部门维护

| 权限 | 平台管理员 | 业务角色 |
|---|---|---|
| `department:read` | 是 | 是，仅授权部门 |
| `department:create` | 是 | 否 |
| `department:update` | 是 | 否 |

## 🎙️ 批次 2 权限

| 权限 | 平台管理员 | 业务角色 |
|---|---|---|
| `minutes:read` | 是 | 是，授权部门 |
| `minutes:create/update/approve` | 是 | 是，授权部门 |
| `workflow-policy:read` | 是 | 是 |
| `workflow-policy:update` | 是 | 否 |
| `workflow-sla:update` | 是 | 否；测试触发仅管理员 |
| `agent-run:update` | 是 | 是，仅执行本人可见 AgentRun |

## 🔐 字段敏感等级

- `Internal`：具备对应资源读取 API 权限时可读。
- `Sensitive`：仍受 API 权限与组织范围保护；首个 Demo 未实现所有角色的细粒度字段差异。
- `Core`：非 `platform-admin` 返回 `***`；已有 `InternalFormulaReference` 自动测试。

## 🚫 生产限制

- 不提供正式企业 SSO/OIDC。
- 不声称本地 Cookie 身份达到生产认证标准。
- 生产需要真实组织主数据、MFA、会话策略、密钥管理和权限审批。

## 🧩 批次 2.1 策略权限

- `/workflow-policies` 仅平台管理员可见。
- `workflow-policy:update` 只授予平台管理员；普通员工直接调用保存/发布接口返回 403。
- SLA 超时模拟接口仅平台管理员可用。
- 自动代理运行使用 Agent 独立服务身份，不继承真人确认身份。

## 🏗️ 批次 3 项目与事件权限

| 权限 | 平台管理员 | 项目经理 | 普通项目成员 |
|---|---|---|---|
| `project:read` | 是 | 是 | 是，仅参与/授权项目 |
| `project:create` | 是 | 是 | 否 |
| `project:update` | 是 | 是，负责或授权项目 | 否 |
| `project-member:read` | 是 | 是 | 是 |
| `project-member:manage` | 是 | 是 | 否 |
| `milestone:read` | 是 | 是 | 是 |
| `milestone:manage` | 是 | 是 | 否 |
| `event-inbox:read` | 是 | 是 | 是，部门/项目范围 |
| `event-inbox:confirm` | 是 | 是 | 否 |
| `project-task:read` | 是 | 是 | 是 |
| `project-task:create` | 是 | 是 | 否 |
| `project-task:update` | 是 | 是 | 是，仅本人任务 |
| `project-audit:read` | 是 | 是 | 否 |

## 🔐 项目数据范围

- 平台管理员可跨部门和跨环境（显式请求测试环境）。
- 项目经理可访问授权部门项目，并管理本人负责或部门负责人可见的项目。
- 普通成员只能读取自己参与的项目和任务。
- 部门负责人可读取授权部门项目。
- 跨部门非项目成员不能通过直接 ID 读取。
- 项目成员角色不作为唯一安全边界；每次请求仍校验 API 权限与项目/部门范围。

## 🧾 批次 3 收尾一致性

- 本批不新增权限码，也不放大现有角色能力。
- PROJECT 文件自动证据只在上传目标确实是 PROJECT 业务对象时建立，仍先执行原文件权限、
  部门、环境和保密等级校验。
- 项目状态持久化修复继续复用 `project:update` 与项目范围判定，不允许普通成员绕过。
- 事件确认继续要求真人 `event-inbox:confirm`；Agent 服务身份不能自确认。
- 上海时区仅改变业务日期解释，不改变身份、权限或数据范围。
- 项目经理能够收到并审批经理业务角色节点，沿用前端导航与 IAM 收尾已补齐的
  `workflow-task:approve/return/terminate`；仍需本人任务、冻结候选人和部门范围同时满足。

## 🤖 事件 Agent 身份

- 独立服务身份：`agent.project`。
- 角色：`project-event-agent`。
- AgentProfile：`项目事件建议数字员工`，A2、Low、金额 0。
- 只允许事件审阅和项目任务草稿；不能确认事件、派发任务、发起流程或关闭高风险事项。

## 🧭 导航和 IAM 权限

| 权限 | 平台管理员 | 项目经理/授权部门负责人 | 普通员工 |
|---|---|---|---|
| `navigation:read` | 是 | 是 | 是 |
| `navigation:manage` | 是 | 是，仅授权部门 | 否 |
| `navigation:approve`（发布） | 是 | 是，仅授权部门 | 否 |
| `identity-admin:read` | 是 | 是 | 否 |
| `identity-admin:manage` | 是 | 否 | 否 |
| `role:read` | 是 | 是 | 否 |
| `role:create/update/manage` | 是 | 否 | 否 |
| `permission:read/assign` | 是 | 是，仅授权范围 | 否 |
| `data-scope:update` | 是 | 否 | 否 |
| `workflow-role:read` | 是 | 是 | 否 |
| `workflow-role:manage` | 是 | 是，仅授权部门 | 否 |
| `workflow-task:update`（转交/升级） | 是 | 是 | 否 |

## 🧩 IAM 收尾边界

- 项目经理只有 `permission:assign` 不足以修改全局角色权限；还必须具备 `role:manage`。
- 项目经理不能分配 organization scope 角色，也不能修改授权部门以外的成员。
- 全量替换成员只替换操作者授权部门集合，范围外成员保持不变。
- 角色有效成员数合并 legacy `UserRole` 与 scoped `UserRoleAssignment`，停用用户不计。
- 高风险授权通过和驳回都必须填写审核意见，申请人不能自审。
- 普通员工不显示 IAM 管理入口；页面按钮隐藏或禁用不替代后端 403。

## 👥 流程业务角色

| 用户 | 业务角色 | 部门范围 | 用途 |
|---|---|---|---|
| `demo.researcher` | `business-rd-engineer` | 研发 | 研发节点 |
| `demo.pm` | `business-manager-reviewer` | 研发 | 经理审核节点 |
| `demo.qa` | `business-quality-reviewer` | 质量 | 质量复核节点 |

## 🔐 节点审批最终判定

审批接口必须同时满足：

1. `workflow-task:approve/return/terminate` 动作权限。
2. 当前任务 `AssigneeId` 为当前用户。
3. 当前用户存在于任务创建时冻结的候选人集合。
4. 当前用户具备任务部门数据范围。
5. 任务版本和幂等键有效。

`project-manager` 已补齐动作权限，但不会因此获得任意流程任务代审权。

业务角色 department scope 必须与任务部门一致；organization scope 仍可作为组织级候选。转交目标必须具有任务部门资格，业务角色目标还必须位于冻结候选集合。

## 🤖 Agent 节点状态

| 状态 | 条件 | 结果 |
|---|---|---|
| 待执行 | Agent 任务已创建、Worker 尚未处理 | 保留任务并等待 Worker |
| 已代理完成 | Low 风险、A2+、未命中永久人工边界 | AgentRun/GatewayCall/动作/审计关联后自动推进 |
| 待人工确认 | A0/A1 或 Profile 要求确认 | 结果生成后转真人确认，服务身份不能自确认 |
| 已阻断 | 永久人类边界、Profile 缺失、网关失败 | 明确标记并转人工，不得静默停留 |

## 🚦 高风险授权

- 系统排序角色、管理型权限等高风险变更进入 `Identity.AccessChangeRequest`。
- 申请人不能审核自己的申请。
- 第二名平台管理员审核通过后才应用。
- 本机演示提供 `demo.security` 只用于验证双人复核接缝，不代表生产双人审批制度已完成。

## 🔒 身份边界

- 当前身份仍为本地开发 Cookie + CSRF。
- IAM 页面不提供密码、OIDC、MFA 或企业 SSO 管理。
- 用户“邀请”为受控本地开通状态，不是真实邮件/钉钉邀请。

## 🔌 MCP 权限与令牌

- MCP 不创建第二套业务权限。每次调用同时校验 MCP token scope、当前 AIDT 用户权限和资源数据范围。
- MCP token 的部门范围与用户部门范围取交集；平台管理员通过 MCP 时仍不能绕过所选部门。
- `aidt:read`、`aidt:write`、`aidt:files`、`aidt:approve`、`aidt:manage` 只控制工具发现和调用，不替代业务权限。
- 审批、事件确认、流程转交、项目最终状态和敏感文件正文必须由当前真人用户在 AIDT 浏览器确认。
- Access Token 有效 1 小时，Refresh Token 有效 30 天并轮换；撤销任一令牌会撤销同一令牌家族。
- 服务身份令牌只能由 `mcp:manage` 管理员基于有效且有业务权限的服务身份签发，最长 168 小时。
- 原始令牌、授权码、客户端密钥、Cookie、CSRF、数据库连接和文件物理路径不得出现在 MCP 响应、日志或前端存储。
