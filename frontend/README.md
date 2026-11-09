# AIDT 前端

> 技术栈：Vue 3 + TypeScript + Vite + Element Plus + Pinia + Vue Router  
> 状态：DEPT-WF-001 已完成、部署并冻结

## 🧱 目录

```text
src/
├─ api/            统一 HTTP client、契约类型和模块 API
├─ components/     base 与 business 可复用组件
├─ features/       平台、流程等业务组件
├─ layouts/        页面布局
├─ pages/          路由页面
├─ router/         路由与权限守卫
├─ stores/         身份和平台状态
├─ styles/         设计 token
└─ test/           测试初始化
```

## 🛠️ 命令

```powershell
npm ci
npm run typecheck
npm run test
npm run contract:check
npm run build
```

开发服务：

```powershell
npm run dev
```

默认 `http://127.0.0.1:5173`，代理 `/api`、`/health` 和 `/openapi` 到 `127.0.0.1:5080`。
可通过 `VITE_API_PROXY_TARGET` 覆盖开发代理目标。

## 🔐 身份状态

- `.env.example` 默认 `VITE_AUTH_ENFORCEMENT=contract-preview`，用于查看路由骨架，不代表正式认证。
- 正式部署接入 SSO 后必须改为 `enforced`，并从会话 API 装载用户权限。
- 页面顶部始终显示“权限契约预览”；不得把当前状态描述为正式权限控制。

## 🚫 当前限制

- 默认运行配置连接本机 SQL Server；就绪检查和持久化页面正常。
- 本地演示身份来自 SQL 种子数据，不是生产 SSO。
- 外部连接器显示 `Disconnected`，不生成 Mock 成功记录。
- 员工优先入口 `/`、部门 `/department`、文件 `/documents` 和管理 `/management`
  已通过真实 SQL 浏览器 E2E。
- 员工 `/` 支持真实创建事项和附件关联。
- 员工 `/` 将流程任务、项目任务、事件确认、听记行动项、MCP 确认和 Agent 确认聚合为统一工作事项；
  主卡片不展示 UUID、TraceId、节点英文代码或内部 revision。
- 工作事项按永久人工边界、已逾期、今天到期、待我确认、本周和其他排序；
  事件已有生成任务时不再重复展示事件。
- 工作事项深链进入现有流程、项目、收件箱、听记、MCP 确认和 Agent 页面。
- 流程配置中心 `/configurations` 支持草稿、校验、发布和回滚。
- IAM 与导航中心 `/iam` 支持组织树、人员、角色权限矩阵、数据范围、导航发布和高风险审核。
- MCP 接入治理 `/mcp-center` 支持工具目录、客户端、令牌、服务身份令牌、真人确认和调用审计。
- MCP 高风险确认 `/mcp-confirmations` 只允许当前真人确认绑定请求。
- 人员列表使用服务端分页和搜索；角色成员候选加载全部授权用户。
- IAM 管理按钮按动作权限显示或禁用，后端 403 仍是最终真源。
- 高风险审核支持填写意见后通过或驳回。
- 左侧导航由 `/api/v1/navigation/current` 返回，支持个人排序、收藏和隐藏。
- 左侧导航按公司公共入口、部门工作空间、平台治理三层显示；每部门只显示一个有效默认
  工作区，测试导航不会进入普通用户主视图。
- 流程配置的 Agent 节点使用可搜索数字员工选择器，不要求输入 GUID。
- Agent 任务显示待执行、待人工确认、已代理完成或已阻断状态。
- 390px 响应式已经在所有关键页面验证无页面级横向溢出。
- 项目详情状态选择器使用稳定测试标识；刷新闭环覆盖状态、文件证据和完成进度。
- 项目详情支持编辑项目与更换负责人、成员角色/移除、直接创建并分配任务、
  关联文件/听记/流程/业务对象证据，以及合法状态流转和自动进度重算。
- PMO-003 已在 1440、1280、390px 完成完整操作闭环 E2E。
- 事件收件箱刷新后保持 `AwaitingConfirmation/Confirmed`，已确认事件不再显示确认按钮。
- 上海截止日期页面显示为原文日期 18:00，不出现次日凌晨偏移。
- 部门页 `/department` 按服务端岗位模板渲染 6 类真实首屏；无匹配时回退通用部门工作区。
- IAM `/iam` 的“工作台模板”页签支持卡片、匹配范围、预览、草稿、校验、发布、停用、恢复和版本历史。
- 工作台卡片支持 Ready、Empty、Error、Denied、NotConnected；无权限卡片不在响应中返回。
- `/scenarios` 岗位业务场景中心按业务目标展示授权场景、准备项、责任岗位、
  审批路径、数字员工和永久人工边界。
- 七步启动向导真实调用 7A 预检/启动接口、文件中心和听记服务；页面不构造流程实例或任务。
- “我的场景”读取 `ScenarioLaunch` 及当前活动流程/项目任务/事件真源，刷新和重新登录后一致。
- 场景配置在 `/scenarios?mode=manage` 完成草稿、校验、发布、停用、恢复、版本和回滚；
  配置中心提供同一入口。
- `/configurations` 使用左中右可视化工作台，支持字段组件、节点动作、人员/部门/日期/
  附件字段、数字员工、AI 规则审查、SLA、通知和永久人工边界。
- `/workflows` 按任务冻结版本渲染动态表单，保存草稿并通过、退回或终止；
  技术 ID、节点 Code 和哈希只在管理员折叠区显示。
- 源码 E2E 默认从 `AIDT_E2E_API_PORT` 同步 Vite 代理目标，并使用隔离库与可重复服务身份夹具。

## 🧾 文件中心

- 上传支持 PDF、DOCX、XLSX、PPTX、PNG、JPG/JPEG、TXT、CSV。
- PDF、图片和文本内嵌预览；Office 明确提示下载查看。
- 版本历史、SHA256、扫描状态、保密等级和删除申请均通过契约表达。
- 前端不接收物理路径，不暴露 UUID 作为员工主视觉。
