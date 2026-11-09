# 研究发现

## 2026-11-05 UX-006 故障恢复与当前门禁审计

- 原目标会话在暂停时正轮询长测试命令；恢复后同一 `write_stdin` 工具调用缺少匹配输出，
  导致 `No tool output found`。工作树、37 个文件修改和测试结果均未丢失。
- 当前分支为 `feature/ux-006-column-introductions`，基线 `main@d22e7d0`；
  8088 当前部署 `req-ui-ux005-final3`，源码提交与基线一致。
- 17 个一级栏目均已接入 `MainColumnPageHeader` 和集中目录
  `frontend/src/features/workbench-intro/introCatalog.ts`；原有我的工作、听记页面内重复抽屉已删除。
- 目录测试已覆盖 17 项完整性、每页 3—5 步、至少两个不重复例子、业务语言和合法链接。
- 当前 Playwright 报告为 59 通过、11 失败；报告生成于 `aidt.spec.ts` 最后修改之前，
  其中两个标题断言已随后修正，不能把旧报告当作当前代码结论，必须重跑。
- 共享页面头已显示当前“演示环境”；介绍抽屉已为“适用岗位”提供独立分区。
- 规划文件此前未登记 UX-006；本次已补齐需求池、功能地图、交付报告和测试证据。
- `planning-with-files` 的 `session-catchup.py` 在当前 Windows 环境静默退出码 1；
  已改用 Git 状态、目录测试、最近 Playwright 报告和规划文件完成恢复。
- 完整源码回归首轮暴露审计页仅取最近 100 条后在前端过滤，历史审计较多时会错误显示
  0 条；后端已支持 `entityType/entityId` 参数，前端改为直接查询后 WF-001 审计场景通过。
- 使用历史累计库时数字员工服务账号已被绑定、我的工作查询也受大量历史数据影响；
  改用 step8 冻结基线库并由 `Run-FullE2e.ps1` 临时创建专用服务身份后，完整源码
  Playwright 达到 68 通过、0 失败、2 个部署专用跳过。
- 审计、MCP 等若干精确文本断言会同时命中导航或表头；已把身份中心标题断言收敛到
  `h1`，未以隐藏元素或删除测试规避。
- 正式 8088 首轮在主库累计数据上为 65/70，失败集中在历史数据导致的长查询超时；
  临时把 8088 后端切换到 step8 冻结基线恢复库后，正式全量 `70/70` 通过，
  随后已验证切回主库且全部健康端点 200。
- 应用回滚 `ux006 → ux005-final3 → ux006` 通过；最终主库备份
  SHA256 `338CAF...`，独立恢复库 ONLINE、18 迁移、71 应用表。
- 发布包 SHA256 `8025047A...`，manifest 源码 `2d9ba09` 且无 `-dirty`。

## 2026-11-05 UX-005 正式发布最终结论

- 无快进合并提交 `7f03623`；功能分支最终提交 `0bc7053`。
- 最终 8088 关键烟测发现导航加载完成会再次关闭已打开抽屉；根因是同一个 watcher
  同时监听路由、当前部门和导航分组，并在任意变化时设置 `sidebarOpen=false`。
  已拆分为“路由变化关闭抽屉”和“导航数据刷新只同步部门/滚动”两个 watcher，
  提交 `59f406d`，并补 Vitest 67/67。
- 最终发布 `req-ui-ux005-final2`，源码 `0654008`，
  SHA256 `0B13C8B595E549DECC9D566D723A06DF6FDA4931A9CEFEB9F1E5E6F78EF6AD93`；
  manifest 无 `-dirty`，18 条迁移、RuleSimulator、Mock/Disconnected 边界正确。
- final2 部署时间 `2026-11-05T10:44:03+08:00`，8088 页面/API/Worker/AiGateway/MCP
  健康检查全部 200。
- 8088 正式全量 E2E `64/64`，0 失败、0 跳过、0 flaky；报告开始
  `2026-11-05T02:32:27Z`。为避免主库累计测试数据触发本地 SQL Server 瞬时取消，
  E2E 使用由 frozen step8 备份恢复的 `AIDT_UX005_FINAL2_E2E_20261105`，
  8088 Nginx 入口不变，主库未覆盖；结束后已切回主库并完成关键烟测 10/10。
- 应用回滚 `final2 → final1 → final2` 通过；最终主库备份 SHA256
  `E482EF7FB8F47CD49DAC1B98C503B3D3D6575110D85D9CCE2B9628AADDA7ADE1`，
  独立恢复库 ONLINE、18 迁移、71 应用表、7 个部门默认工作区。
- 核心 UX 面最终通过：固定外壳、独立正文滚动、独立导航滚动、部门单开、
  当前菜单定位、820px 抽屉/遮罩/Esc/焦点返回、全宽正文、真实统计与筛选、
  AI 听记固定高度双栏、页面无横向溢出。

## 2026-11-04 UX-005 侧栏与正文问题复现及 PMS 参考

## 2026-11-05 UX-005 正式发布启动核对

- 当前分支 `feature/ux-005-pms-workspace`，HEAD `4caa29f`，工作树干净。
- `main` 仍为 `7e580f9`；8088 当前版本 `req-ui-step8-final3`，
  manifest sourceRevision 为 `a86cccd`，18 条迁移，AI 为 RuleSimulator。
- Chrome 位于 `%LOCALAPPDATA%\Google\Chrome\Application\chrome.exe`，
  Edge 位于 `C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe`。
- 现有 Playwright 配置只固定使用 Edge；正式发布前必须增加 Chrome 项目，
  并显式覆盖 1366、1024、820 和缩放等价视口。
- 部署脚本会在切换前备份 `AIDT` 并执行 `RESTORE VERIFYONLY`；
  8088 回滚点需保持 `req-ui-step8-final3` 可用。
- 发布必须使用 GarnetPort `6480`，不能使用 README 的默认 6380 示例。

### 正式评审结论（2026-11-05）

- 固定应用壳、侧栏独立滚动、部门单开、当前菜单自动定位、820px 抽屉和
  完整正文宽度均通过 Chrome/Edge 双浏览器验证。
- 新增 `release-matrix.spec.ts`，覆盖 1920、1440、1366、1280、1024、820、
  600×518、390×844，以及 1366 的 100/125/150% 和 1920 的 100/125%。
- 放大后页面本身不横滚；业务宽表只在 Element Plus 表格容器内横滚。
- 听记汇总/筛选 API 已覆盖权限、组织、部门、环境、匿名访问，以及
  “全量汇总不是当前页条数”的断言。
- 正式回归中修正了旧测试在长时间运行后对最新审计条目和搜索命中的错误假设，
  未删除、隐藏或跳过业务用例。

- 当前基线 `main@7e580f9`，工作区在文档登记前干净，当前部署为 `req-ui-step8-final3`。
- `127.0.0.1:8088/documents` 在约 `1920×1080` 下展开一个部门后，导航高度约
  `1917px`，侧栏高度仅 `1080px`，底部约 `837px` 被裁切。
- 正文滚动约 `1680px` 后，侧栏顶部为 `-1680px`；`position: sticky` 未按预期吸附。
- Codex 内置浏览器约 `600×518` 时，侧栏整体高度约 `2031px`，
  `overflow-y` 为 `visible`，完整导航成为普通页面内容。
- 当前根因是 `.sidebar` 的 `100vh + overflow:hidden`、`nav` 无独立滚动，
  以及 `.app-layout` 的 `overflow-x:hidden` 形成错误滚动上下文；
  `820px` 以下仅改为 `static`，没有抽屉。
- PMS 侧栏实测约 `236px` 宽，导航独立 `overflow-y:auto`，菜单约 `38px` 高，
  分组使用浅色背景和左侧 `3px` 色条，`760px` 以下切换为带遮罩抽屉。
- 设计决策：借 PMS 的布局、折叠、独立滚动和窄屏抽屉，不复制其品牌、源码和蓝色主题；
  AIDT 保留绿色品牌和 `248px` 宽度。
- AIDT `.main-content` 固定最大宽度 `1260px`；在约 `2304px` 总视口中，
  可用正文宽约 `2033px`，内容只占约 `62%`。
- PMS 会议页正文基本使用完整可用宽度，顺序为页面头、范围提示、4 个统计、
  搜索筛选栏和全宽会议表。
- “我的工作”已有 4 个统计和 4 个分类页签，但没有关键词和组合筛选；
  右侧约 `337px` 的说明栏持续压缩主事项列表。
- “AI 听记”没有统计和筛选，左右栏约 `289px / 851px`；
  来源列表一次性展开，当前页面高度约 `13648px`。
- 设计决策调整：UX-005 扩展为 A 应用外壳/侧栏、B 统一页面头/统计/筛选/介绍抽屉、
  C 首批我的工作与 AI 听记；运营页使用完整可用宽度，说明文字进入介绍抽屉。
- 新需求登记为 `IDEA-UX-005 / UX-005`，P1，approved；本轮只更新文档，
  未修改前端源码、API、数据库、权限或部署。

### 实施基线核对（2026-11-04）

- 已创建独立分支 `feature/ux-005-pms-workspace`，基线仍为 `main@7e580f9`。
- 当前 `AppLayout.vue` 的主要错误滚动上下文是 `.app-layout { overflow-x:hidden }`、
  `.sidebar { height:100vh; overflow:hidden }` 和没有 `min-height:0` 的 `nav`；
  窄屏只是把 side bar 改为 `static`，完整导航继续进入正文流。
- `.main-content { width:min(1260px,100%) }` 是所有运营页窄化的直接来源。
- `MyWorkDto.Summary` 已由服务端对完整待办集计算四项指标；
  `pendingItems/initiatedByMe/participating/needsAttention` 也是完整聚合结果，
  因此 C1 可在前端准确搜索和组合筛选，不需要 API 变更。
- 听记列表当前只返回最多 100 条分页来源，行动项只能按选中来源查询；
  现有响应无法准确取得全量“待确认行动项”，也无法由服务端按标题/参会人/文字稿、
  分析状态、来源和会议时间组合筛选。C2 需要最小只读扩展。
- 听记来源实体没有独立参会人字段；参会人可通过文字稿与行动项发言人检索，
  不新增重复参会人主数据。

### UX-005-A 结论（2026-11-04）

- 固定外壳不需要 `position: sticky`：`.app-layout` 固定 `100dvh`，主列使用
  `auto + minmax(0,1fr)` 两行，正文成为唯一业务滚动容器；侧栏保持固定列。
- 导航使用 `flex:1; min-height:0; overflow-y:auto`，品牌区和底部操作均为
  `flex:0`，因此导航滚到底部时底部操作仍可达。
- 当前部门由受控状态单开；`toggle` 事件切换目标部门，避免多个 `<details>` 同时展开。
  当前路由变化后按 `data-nav-key` 查找当前项并 `scrollIntoView({block:'nearest'})`。
- `820px` 以下侧栏改为固定定位抽屉；遮罩、Esc 关闭、菜单按钮焦点返回和
  `aria-current/aria-expanded` 均已覆盖。
- A 阶段验证：TypeScript 通过；Vitest 25 文件、50/50；生产构建通过；
  Playwright A 专项 4/4（1920、1280、600×518、390×844）。

### UX-005-B 结论（2026-11-04）

- 新增四个基础组件：`WorkbenchPageHeader`、`WorkbenchIntroDrawer`、
  `WorkbenchStats`、`WorkbenchFilterBar`；组件只负责结构、可访问性和布局。
- 页面头把“介绍”作为固定辅助操作；介绍抽屉从右打开、约 420px，按用途、
  使用时机、业务例子、权限/数据/人工边界分区，并支持 Esc 和初始焦点。
- 统计条接受任意 1～5 项业务指标，不强制所有页面固定四张卡。
- 筛选栏桌面常驻，760px 以下保留搜索并把其余筛选收进底部筛选面板；
  重置、结果数量由页面提供的动作槽和文字决定。
- 全局正文已在 A 阶段解除 `1260px` 限制；详情和长文仍可由各自页面设置阅读宽度。
- B 阶段验证：基础组件 Vitest 4/4，TypeScript 通过。

### UX-005-C1 结论（2026-11-04）

- “我的工作”四项统计直接使用 `MyWorkDto.Summary` 的服务端全量结果；
  不使用当前分页或当前筛选后的条数。
- 搜索覆盖事项标题、项目/业务对象上下文、来源名称、部门和详情目标编号；
  部门、来源、状态、优先级、截止时间可组合，重置一次清空全部条件。
- 分类为“全部 + 待我处理 + 我发起 + 我参与 + 需要关注”；“全部”按来源和 ID
  去重合并四类，避免同一事项跨分类重复计数。
- 桌面七列表格与手机卡片共用同一投影；单页 50 条，避免一次渲染数百行。
- 今日焦点保留为高优先级横条；快速开始进入“发起事项”下拉，最近证据进入筛选栏入口；
  右侧长期说明栏已删除。
- 永久人工边界和待确认提示直接出现在行操作旁，不因移入介绍抽屉而消失。
- C1 专项验证：过滤/表格/页面 Vitest 9/9；Playwright 1920、600×518、390×844
  共 3/3；页面宽度在 1920 下大于 1260px，无横向溢出。

### UX-005-C2 结论（2026-11-04）

- 现有听记接口无法准确计算全量“待确认行动项”，也不支持标题/参会人/文字稿、
  分析状态、行动项状态、来源类型和会议时间组合筛选，因此 C2 增加只读
  `MinutesSummaryDto` 与 `MinutesSourceQuery`。
- 汇总和筛选继续复用 `ApplySourceScope`，没有扩大组织、部门、环境数据范围；
  `Draft` 行动项只统计可访问听记来源下的全量记录。
- 听记页面在桌面固定为约 `320px` 来源栏加剩余全宽详情栏，两侧独立滚动；
  每页 20 条来源。移动端改为两个有界高度面板，避免整页被全部来源拉长。
- 现有分析运行、行动项编辑/保存、统一确认和真人确认链路未改动；
  专项 E2E 继续通过。
- C2 专项 Playwright 1920、600×518、390×844 共 3/3；新增只读 API 集成测试 1/1。

### UX-005 最终门禁（2026-11-04）

- 独立测试库：`AIDT_UX005_FINAL_20261104`；主库未修改。
- 前端类型检查、30 个 Vitest 文件/66 用例、生产构建、OpenAPI 静态校验通过。
- 源码 E2E 62/62；另在部署入口单独验证 MCP OAuth 1/1，合计 63/63。
- 后端默认测试 118 通过、69 个 SQL 条件用例按设计跳过；新增听记只读集成用例
  在独立库 1/1 通过。
- 全量回归中发现并修复：移动抽屉遮罩层级、宽表在独立滚动正文中的最小宽度上下文、
  以及旧测试对全量列表首屏的假设。未删除测试、未跳过非部署专用用例。

## 2026-11-03 DEPT-WF-001 / 批次 8 启动核对

- 固定仓库为 `D:/desktop/工作区合区/Vibecoding-目标模式/coding`。
- `main@3e94f3a` 工作区干净；`req-ui-step7-frozen-v1`、
  `req-ui-step7a-frozen-v1` 存在，尚无 step8 标签。
- 现有系统已具备版本化导航、配置、Elsa 流程版本、节点策略、对象、任务、文件、
  听记、Agent、场景、审计和 Outbox，可复用，不应另建第二套真源。
- 配置中心当前已管理可执行流程结构，但节点配置仍偏流程引擎字段；
  `WorkflowPage` 尚无节点冻结动态表单。
- 导航目前同时存在服务端数据驱动和 `AppLayout.vue` 硬编码业务入口，
  需要统一为后端已注册资源。
- 会话中的旧权限 Cookie 可能让场景显示为空；必须区分未授权、权限变更和真实空数据。
- 8A 清场只能退役/标记测试数据，不得物理删除审计、历史流程、任务或证据。

## 2026-11-04 DEPT-WF-001 实现与回归发现

- 导航测试残留 32 个、配置测试版本 319 个；导航采用测试标记并退役，
  配置采用测试标记且不改变历史流程版本解释。
- 公司公共入口只从系统工作区抽取并去重；每部门只选择一个有效默认工作区，
  因而测试工作区可发布但不会进入普通用户主视图。
- 旧 Cookie 缺授权指纹会明确进入 `Stale`，前端清除权限并要求重新登录。
- 任务表单必须冻结定义 JSON、Schema 版本和哈希；节点新版本不改变历史任务。
- Playwright 源码服务必须显式使用 Development 和同一 API 代理端口；
  否则会误连当前 5080 部署并把部署数据当成源码结果。
- 数字员工 E2E 原依赖历史未绑定服务账号，重复运行会耗尽；现增加隔离数据库夹具，
  每次生成低/高风险服务账号并在测试后停用。
- 任务技术信息折叠后旧测试不能再依赖隐藏文本来定位；使用数据属性定位，
  同时保持普通员工页面不显示节点 Code、任务 ID 或哈希。
- 配置中心默认“已发布”筛选会让新建草稿在创建后立即消失；现创建成功后自动切到全部状态，
  而重新进入页面仍默认已发布，避免测试/草稿污染主视图。E2E 发布步骤显式切换到“已校验”。
- 首次正式包构建前 manifest 仍显示 `isolated_verified_pending_main`，因此该包只保留为
  预构建追溯，不作为发布包；必须先把迁移状态更新为 `applied_verified` 再重新构建。
- final1 经 Nginx 登录 502、直连 API 200，根因是 Scoped Role Claim 与角色 Claim
  重复导致认证 Cookie 达约 4.5 KB；压缩角色范围并配置 16 KB 上游缓冲后降至 2,952 字节。
- final2 的 IAM 导航 E2E 会把测试工作区设为部门默认，旧逻辑随即取消非测试正式默认；
  现测试导航不参与当前导航、创建时也不占默认位，并增加非测试默认保护测试。
- 测试截图默认写仓库会令发布 manifest 出现 `-dirty`；正式 E2E 改用
  `AIDT_EVIDENCE_DIR` 写入运维证据目录，发布源码保持干净。

## 2026-11-03 SCN-001 / 7B 启动核对

- `main@1b33a9f` 工作区干净，6-final1 与 7A 冻结标签均存在。
- 当前部署 `req-ui-step7a-final1`，页面、API、Worker、AiGateway 和 MCP 健康。
- 最大迁移 017，`Scenario` Schema 3 表，六类基础场景已发布。
- 7A 已提供 17 个场景端点，但缺少“我发起过/最近进行中”的当前用户启动列表。
- `ScenarioLaunch` 保存真实对象、流程实例/任务、项目任务、事件、附件、听记和
  AgentRun 的 ID，可作为只读聚合索引，不应再复制状态。
- 前端已有 `Scenario*` 契约，但没有 `scenarioApi` 模块和 `/scenarios` 页面。
- 种子场景由迁移 017 创建，启动记录默认 `EnvironmentCode=demo`、`IsTestData=true`；
  页面必须明确标识 Demo，不能冒充生产事项。
- 保留 7A 的永久人工边界和双人审核，不在 7B 放大权限。
- 当前用户启动列表必须按 `WorkflowInstanceId` 查活动任务；仅读 `ScenarioLaunch.WorkflowTaskId`
  会停留在首次任务，无法反映真实进度。实现已改为优先读取 Pending/InProgress 当前任务。
- 场景详情关闭动画曾异步清空共享 `detail`，导致移动端启动向导丢失上下文；
  已移除关闭回调清理并通过移动端完整七步 E2E。
- 配置草稿创建需要完整业务形状（问题、触发、部门、岗位、步骤、权限），
  因此新建对话框补齐必要业务输入，不把不完整 payload 伪装成可发布草稿。
- 源码 E2E 必须在 `ASPNETCORE_ENVIRONMENT=Development` 下启动，否则本地 Demo 登录
  会按设计拒绝；数字员工服务账号必须按实际业务显示名选择，不能假设统一占位文本。
- 正式部署后完整 E2E 50/50 通过；回滚到 7A 和重新部署 final1
  不改变数据库结构，历史启动映射和场景版本可读取。
- 最终部署备份恢复到独立数据库后：17 迁移、71 应用表、6 基础场景、
  6 条启动映射、7 个发布版本、5 项权限、13 条场景审计。

## 2026-11-03 SCN-001 / 7A 启动核对

- `main` 位于 `ae64f50`，6A/6B 已合并并冻结。
- 当前冻结标签包括 `req-ui-step6a-frozen-v1`、`req-ui-step6-frozen-v1`；
  当前部署为 `req-ui-step6-final1`。
- 当前最大迁移为 016，应用表 68 张，Schema 14 个。
- 现有四类流程、项目任务、事件收件箱、文件关联、听记、AgentProfileVersion、
  Audit 和 Outbox 都有真实服务，可直接作为场景能力底座。
- `ConfigurationVersion` 可保存配置版本，但不能独立表达稳定场景编码、
  当前发布指针、部门适用范围和启动映射；若只用它，会迫使名称/JSON
  承担主键与状态职责。
- 决策：迁移 017 新增 `Scenario` Schema 3 张表，只保存场景版本和真实业务链 ID。
- 场景发布将冻结引用稳定编码和版本；不依赖当前最大版本重新解释历史启动。

### 实现与测试结论

- 场景采用 `ScenarioDefinition`、`ScenarioVersion`、`ScenarioLaunch` 3 张表；
  启动映射只保存真实业务链 ID。
- 六类基础场景均能在隔离库启动真实对象+流程、事件收件箱或项目任务。
- 发布后修改返回 `SCENARIO_VERSION_CONFLICT`；回滚生成下一新版本。
- 采购/财务/质量高风险发布进入 `AccessChangeRequest(ScenarioPublish)`，
  第二位平台管理员审核后应用，自审返回 403。
- 质量角色补齐 `business-object:create` 和 `workflow-instance:submit`，
  仍受原部门、字段、流程节点和永久人工边界约束。
- 源码 E2E 的端口问题：Vite 代理固定 API `5080`，MCP 测试默认 `8085`/`8088`
  需显式配套；备用端口会造成前端读旧库、MCP 写新库。最终用默认端口全绿。
- 最新结果：后端/SQL/API 181/181，MCP 11/11，Vitest 41/41，
  OpenAPI 179/200，源码 E2E 44/44 + 1 个部署专用用例跳过。
- 正式部署 45/45；部署环境场景烟测返回真实对象、流程和任务，幂等重放一致。
- 应用回滚与重新部署、数据库最终备份和独立恢复均通过。

## 2026-11-03 AGT-003 / 6B 启动

- `main@337dd51` 工作区干净；5-final 和 6A 冻结标签存在；当前部署
  `req-ui-step6a-final1`，最大迁移 015、68 表。
- 当前 `/agents` 只显示三张身份卡和运行表格，没有搜索、筛选、生命周期治理或证据聚合。
- 6A 已有完整生命周期 API，但缺少前端所需的人员/部门/服务账号/能力元数据端点，
  也缺少按身份聚合运行、流程节点、MCP、Gateway 和审计的 Context API。
- 配置中心和流程策略已调用有效候选 `/api/v1/agents`，但辅助文字仍暴露 UUID 片段；
  MCP 中心缺少关联身份名称、不可用状态和撤销原因。
- 供应、财务、生产三个既有业务角色尚无 `agent-read`，需要通过 016 增量授权，
  否则“普通业务人员可查看授权有效身份”的目标不成立。
- 页面实现将使用 6A 真实 API，无静态身份列表；运行、流程、MCP 和审计证据均从 SQL 查询。
- 身份编辑器首次实现对 Vue 响应式对象调用 `structuredClone`，Chrome 抛出
  `DataCloneError`；已改为显式字段复制并增加响应式对象回归测试。
- `/agents?run=...` 需自动切换到运行记录页签；已保留旧 `.agent-card`、运行确认/停止
  测试标识，并更新旧 E2E 到新业务文案。
- 016 静态脚本必须在事务前设置 `ANSI_NULLS/QUOTED_IDENTIFIER`，并在跨 `GO` 时
  避免依赖临时变量；最终隔离库与主库均成功应用。
- 全量源码 E2E 首次暴露项目状态提示早于详情刷新，移动端连续变更会提交旧版本；
  已将提示移到 `load()` 之后。流程配置处理人策略增加稳定测试标识。

## 2026-11-03 AGT-003 / 6A 启动

- `main@dc0c945` 工作区干净；`req-ui-step5a-frozen-v1`、`req-ui-step5-frozen-v1`
  均存在；当前部署为 `req-ui-step5-final1`。
- 当前最大迁移为 014，模型快照为 67 张应用表；6A 只能新增 015。
- 现有 `AgentProfile` 仅保存名称、服务用户、自主等级、金额、风险、白名单 JSON 和
  `IsActive`，没有版本、编码、部门、负责人、生命周期、数据/模型范围、确认或熔断规则。
- `AgentRun`、流程 Agent 节点、听记/事件分析和 MCP 服务身份均直接读取当前
  `AgentProfile`；需保留主表当前快照以降低兼容风险，并用独立版本表保存不可变历史。
- MCP 服务令牌目前绑定服务用户但不绑定 AgentProfile；停用治理需要新增显式关联，
  同时避免撤销真人交互式 OAuth 令牌。
- 高风险双人审核可复用 `Identity.AccessChangeRequest`；审计、Outbox、幂等、事务
  均可复用现有端口和模式。
- `demo-login` 当前允许服务身份进入浏览器会话，需要由 6A 明确拒绝服务身份登录治理页面。
- 5B 变更计划状态与证据复选框此前未同步，已按真实交付证据修正。
- 015 已在隔离库完成 `14/67 → 15/68 → 14/67 → 15/68`；3 个存量身份均回填 v1 发布快照。
- 首次 DBA 脚本执行暴露 SQL Server 批编译看不到同批新增列，以及过滤索引要求
  `QUOTED_IDENTIFIER ON`；已增加批分隔 `GO` 和 SET 选项并通过复跑。
- 首次 6A 全量回归暴露 IAM 旧测试仍尝试用服务身份登录；已改为验证治理登录 403。
- 安全拒绝审计首版复用失败事务的 DbContext，会重放回滚实体；已改为独立 DI 作用域。
- 全量门禁最终结果：后端/SQL/API 171/171，MCP 11/11，Vitest 35/35，
  OpenAPI 160/181，源码 Playwright 40 通过、1 个部署专用用例跳过。
- 正式 E2E 的前两次失败分别来自 SLA 扫描瞬时空返回、收件箱加载未完成和提示层遮挡；
  已加入收件箱加载禁用门禁、实际 POST 2xx 断言，并在干净基线重跑 41/41。
- 发布包 SHA256 `32E201A25804D9B5876771D197CEE38ABFD992FF9BFCFA313CC69C8931264456`，
  sourceRevision `e2be3c8`，无 `-dirty`。
- 应用回滚 `final1 → req-ui-step5-final1 → final1` 通过；最终所有健康端点 200。
- 恢复库 `AIDT_AGT003_A_FINAL1_RESTORE_20261103_1017` 为 ONLINE、15 迁移、68 表、
  3 个有效发布身份、750 条历史 AgentRun。
- 6B 最终源码结果：后端/SQL/API 172/172、MCP 11/11、Vitest 41/41、
  OpenAPI 162/183、源码 E2E 44 通过 + 1 个部署专用用例跳过。
- 6B 主库迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_AGT003_B_PRE_MAIN_016_20261103_1205.bak`，
  SHA256 `F507534041F5E4B0D6AD36C54DA0C43ADA07AC7225327544B531B6EF8886821B`，
  `RESTORE VERIFYONLY` 通过。
- 发布包 `AIDT-req-ui-step6-final1.zip` 从 `fb20bfb` 干净构建，
  SHA256 `282BDAE5DD0D9D3C2EEF8D240FBD03BC74F2C3F7792F6AA64E2CE29064FC937F`。
- 正式 Nginx E2E 45/45，包含部署专用 MCP OAuth；0 失败、0 跳过、0 flaky。
- 应用回滚 `final1 → req-ui-step6a-final1 → final1` 通过，最终所有健康端点 200。
- 最终恢复库为 16 迁移、68 表、5 个有效发布身份、790 条 AgentRun；
  6 个新增服务账号中 4 个仍未被 E2E 身份绑定。

## 2026-11-02 META-001-B 启动

- `main@fccadfc` 工作区干净；`req-ui-step5a-frozen-v1` 指向前述提交。
- 当前部署 `req-ui-step5a-final1`，13 条迁移、67 张应用表。
- 现有业务对象页只能把全部字段渲染成 `el-input`，缺少详情、编辑、版本和关联证据。
- 5A 已提供完整建模 API，但没有业务对象版本/上下文读取端点。
- 普通业务角色当前仍可读取全部对象类型，需在服务端限制为已发布快照。
- 配置中心对象类型仍为文本输入；岗位卡片已有 `objectCodes` 过滤能力但 UI 未暴露。
- 本批优先只新增只读端点，不新增迁移。
- 013 回填历史快照的 `options` 是 JSON 字符串而非数组；已增加统一兼容解析器。
- 普通业务角色原本没有 `business-object:update`，新增 014 授权六个既有业务角色。
- 流程配置使用已发布类型读取端点，允许具备业务对象读取权限的角色获取发布快照。
- 源码 E2E 需用 `--no-launch-profile` 并避开系统保留端口，才能与正式部署并行运行。
- Windows 6082 端口曾被系统拒绝绑定，源码 E2E 改用已验证可绑定的 6182。
- 正式发布包 SHA256 `384291D1F053BCDAD5B831F82A3EEC5C03F019E9C55E0F8BFE40D29660721941`，
  sourceRevision `9875ad2`，14 条迁移且无 `-dirty`。
- 正式 E2E 41/41；应用回滚 `final1 → req-ui-step5a-final1 → final1` 通过。
- 独立恢复库为 14 迁移、67 表、6 个业务更新授权、4 个动态模型版本。

## 2026-11-02 META-001-A 启动

- `main@9b425a3` 工作区干净；`req-ui-step4-frozen-v1` 指向 `756ae40`。
- 当前部署 `req-ui-step4-final1`，12 条迁移、66 张应用表。
- 对象类型与字段已有主表，但缺少生命周期、扩展字段属性、版本快照和真正写 API。
- 业务对象已有版本快照表，但更新 API 为 501，仓储快照写入未绑定操作者。
- 现有五类对象字段使用 `string/text/decimal`，需兼容映射到 Text/LongText/Number。
- 新迁移只能使用 013；001–012 保持不变。
- 013 已在隔离库完成 `12/66 → 13/67 → 12/66 → 13/67`，主库已应用 13/67。
- 后端/SQL/API 163/163、MCP 11/11；干净源码库 E2E 37 通过、1 个部署专用用例跳过。
- 源码验证中修复 Worker 启动依赖：对象模型服务改为显式传入用户身份，不绑定 HTTP 上下文。
- 源码 E2E 首轮误用默认 5080 代理；正确设置 `VITE_API_PROXY_TARGET` 并重建干净库后通过。
- 现有类型及字段兼容通过；对象编辑保留已删除字段旧值，Core 历史字段继续脱敏。
- 正式发布包 SHA256 `8F66CF0090C9F8C43AFDB14455B391E9438CDAD189024242C3E697D743C3DB06`，
  sourceRevision `f3d71c6`，13 条迁移且无 `-dirty`。
- 正式 E2E 38/38，0 失败、0 跳过、0 flaky；开始时间晚于最终部署。
- 应用回滚 `final1 → req-ui-step4-final1 → final1` 通过。
- 最终恢复库 `AIDT_META_001_A_FINAL1_RESTORE_20261102_225845` 为
  ONLINE、13 迁移、67 表、11 个对象版本快照。
- Windows 动态端口排除覆盖 6380；部署脚本增加 `-GarnetPort` 参数并同步 Worker
  缓存连接串，默认仍 6380，本机正式部署使用 6480。

## 2026-11-02 WS-002 可配置岗位工作台模板

### 已确认基线

- `main@67fa2a8` 工作区干净，分支从最新 `main` 创建。
- `req-ui-step3-frozen-v1` 可访问，指向 `c97a7e8`。
- 当前部署 `req-ui-step3-final1`，发布源码 `e8db2b2`，11 条迁移、62 张应用表。
- 既有正式 E2E 32/32，后端/SQL/API/MCP 146/146，Vitest 27/27。

### 可复用能力

- `DepartmentPage.vue` 已有部门选择、跨环境开关、待办/对象/文件展示。
- `SqlEmployeeWorkspaceService` 已具备部门权限、任务、流程、对象和文件真实查询。
- `SqlNavigationService` 已有版本化、发布、停用、恢复、范围和审计模式，可参考但不直接复用业务语义。
- IAM 页面已有 `IamNavigationPanel`，可在同级增加“工作台模板”。
- `UserPrincipal` 同时包含角色、角色分配范围和组织/部门范围。

### WS-002 技术决策

| 决策 | 理由 |
|---|---|
| 新建独立 `Workspace` Schema | 与导航分离，避免把首屏布局混入导航版本 |
| 模板版本与卡片分离 | 草稿可编辑、发布版本不可变，支持历史 |
| 服务端解析并聚合卡片 | 前端不能拿全部模板自行判权 |
| 卡片类型白名单 + 结构化过滤 | 禁止任意 SQL/脚本/HTML/JS |
| 无匹配走既有部门工作区 | 保持兼容并避免白屏 |
| 新迁移仅 012 | 保护历史迁移 |

### 待验证风险

- 部门负责人判定需兼容 `department.ManagerUserId` 与 `membership.IsManager`。
- 角色级匹配必须按角色分配范围收敛到当前部门。
- 六类业务卡片需区分真实空数据与能力尚未接入。

### 已验证结果

- 012 隔离库 up/down/re-up：11 → 12 → 11 → 12 迁移；6 模板、31 卡片、3 权限可重建。
- 后端/SQL/API 138/138，MCP 11/11，合计 149/149。
- Vitest 15 文件、28/28；OpenAPI 136 路径、157 operationId。
- 岗位工作台源码 E2E 5/5，三尺寸通过。
- 干净最终库 `AIDT_WS002_FINAL_20261102_1438` 上的源码全量 E2E：
  36 通过、1 个部署专用 MCP 用例按设计跳过、0 失败。
- 默认匹配已验证：研发 `WS-RND`、质量 `WS-QA`、供应链 `WS-SCM`、财务 `WS-FIN`、
  项目经理 `WS-PM`、平台管理员 `WS-MGR`，生产部门回退通用工作区。
- 用户级模板发布后覆盖部门模板；草稿不生效；停用后恢复系统模板；恢复后重新生效。
- 模板卡片要求不存在权限资源时，该卡片不返回。
- 部门负责人可复制本部门系统模板；普通员工复制返回 403。
- 正式发布 `req-ui-step4-final1`：37/37 E2E，应用回滚与独立恢复库验证通过。
- 第一次构建的包运行代码正确但 manifest 漏列 012；无标签 premanifest 包保留，
  修正 `migration-state.json` 后从 `efc4732` 重建正式包。

### WS-002 错误记录

| 错误 | 处理 |
|---|---|
| SQL Server Express 不支持带 `COMPRESSION` 的备份 | 改为 `COPY_ONLY, CHECKSUM`，`RESTORE VERIFYONLY` 通过 |
| `dotnet ef` 设计工厂固定连接 `AIDT`，环境变量未切换探针库 | 后续命令显式传 `--connection` |
| 迁移首次报告 `description/sortOrder` 不明确 | 所有种子别名完全限定 |
| SQL 清理测试误用不存在的 `AggregateType` | 改为实际列 `ResponseType` |
| 源码 E2E 未设置 MCP 端口，MCP 用例误连 8088 | 设置 `AIDT_E2E_MCP_PORT=5085` 后 3/3 通过 |

## 2026-11-02 PMO-003 项目操作闭环

- 当前 `main@fa1a6d4` 干净；数据库为 11 条迁移、62 张应用表，本批无需新增迁移。
- `PMO.ProjectTask`、`ProjectRelation`、`ProjectMember`、`ProjectProfile` 和审计表已能承载
  项目编辑、负责人、成员、任务与证据操作，不需要第二套模型。
- 既有后端已有项目编辑、成员、关系和状态端口，但缺少“直接创建项目任务”API；
  前端详情页缺少编辑、成员角色/移除、任务创建和分类证据选择入口。
- 项目编辑此前只递增主对象版本，未写 `BusinessObjectVersion` 快照；本批已补齐。
- 项目任务创建复用 `project-task:create` 权限；负责人必须是有效项目成员，
  同一幂等键同内容返回原任务、异内容返回 409。
- 项目进度继续由未取消任务和里程碑计算；任务创建后立即重算，任务完成后再次重算。
- 证据选择器只展示项目所属部门的数据，前端不作最终授权判断，后端继续校验目标范围。
- 隔离测试库 `AIDT_PMO_003_TEST_20261102_124500` 从测试前备份恢复，保持 11 迁移/62 表。
- 隔离库后端/SQL/API/MCP：146/146，0 失败、0 跳过。
- 前端 Vitest：14 个文件、27/27；OpenAPI：126 路径、146 operationId。
- PMO-003 桌面/1280/390px E2E：3/3 通过，覆盖编辑、负责人、成员、任务、四类证据、
  自动进度与状态刷新。
- 首次全量源码回归有 4 条 MCP 相关失败，均由隔离库与默认部署库不一致导致；
  E2E 本地源码模式已改为使用隔离 MCP 源服务。最终完整回归 31 通过、
  1 条部署专用 MCP 用例按设计跳过、0 失败。
- 发布包从干净合并提交 `e8db2b2` 构建，manifest 无 `-dirty`；
  两次正式部署 E2E 均为 32/32。
- 应用回滚 `final1 → req-ui-step2-final1 → final1` 通过；最终所有服务健康检查 200。
- 最终部署备份恢复库为 11 迁移、62 表；`project.task_created` 审计与 9 类项目关系可恢复。

## 2026-11-02 UX-004-B 统一确认中心

- 基线 `main@48a1a10`，2A 冻结标签存在，当前部署 `req-ui-step2a-final1`。
- 统一工作中心已能把四类待确认来源投影为 `WorkItemView`，可作为统一确认页的索引；
  页面不应再建立第二套确认状态。
- 听记行动项目前可编辑负责人、截止、优先级、对象类型和流程版本，但不能保存项目、
  具体业务对象或已有流程；需要迁移 011 补齐持久化。
- MCP 当前 DTO 只暴露 ToolName/Client/Resource/RequestHash，需要新增业务视图，
  但 Token、授权码和物理资源信息继续不得返回。
- MCP 后端当前允许空意见，必须在服务端强制通过和拒绝都填写意见。
- 迁移 011 已在隔离库完成 up/down/re-up，仍为 62 张应用表；系统导航新增 `/confirmations`。
- 统一确认页中的无权限测试暴露并修复了听记来源与 AgentRun 列表缺失组织范围校验的问题。
- 听记确认要求负责人和截止时间；不完整草稿可保存，但服务端拒绝创建任务。
- MCP 自有确认视图只返回用户自己的记录，业务标题/资源/项目可见，技术标识嵌套折叠。
- 源码验收集：后端/MCP 145/145，前端 Vitest 23/23，OpenAPI 125/145，
  源码 Playwright 28 通过、1 个部署专用 MCP 用例跳过。
- 正式发布包从干净合并提交 `931f953` 构建，manifest 无 `-dirty`；首次正式
  Playwright 29/29 后执行应用回滚，验证 `final1 → req-ui-step2a-final1 → final1`。
- 恢复 final1 后再次执行正式 Playwright，结果仍为 29/29、0 跳过、0 flaky；
  最终报告开始时间晚于最终重新部署时间。
- 最终部署备份 `RESTORE VERIFYONLY` 通过，独立恢复库为 11 迁移、62 应用表；
  `CONFIRMATIONS` 导航入口、3 个行动项上下文列、1 个任务项目列和 4 个外键均存在。
- 发布包、历史标签和历史备份未覆盖或移动；冻结标签 `req-ui-step2-frozen-v1`
  指向最终文档提交。

## 2026-11-02 UX-004-A 事件确认与流程发起

- 基线 `main@62052fb`，工作区干净；前序冻结 `req-ui-step1-frozen-v1`、部署
  `req-ui-step1-final1` 均沿用。
- 事件确认请求已经承载 `ProjectId/AssigneeId/DueAt/Priority/Content/WorkflowInstanceId`，
  可扩展新流程定义和版本，无需新表。
- 当前 `ConfirmAsync` 能在自身事务内创建项目任务、项目和来源关系、审计及幂等记录，
  但不能与现有 `StartWorkflowInstanceHandler` 共用事务。
- 当前流程启动 Handler 已包含流程定义/版本、业务对象、部门范围、处理人解析、任务、
  Outbox 和审计规则，应提取事务内入口复用，不能复制。
- 当前页面只选择项目和优先级，负责人和截止时间不可编辑，也没有流程选择和影响预览。
- 项目任务与流程实例可分别通过项目关系反查；本段需补事件来源、任务和流程三类关系的
  同事务写入。
- 当前数据库为 10 条迁移、62 张应用表；本段预期不新增迁移。
- 事件确认采用“状态条件抢占 + 同事务写入”防止重复派发；新流程失败会连同流程、
  任务、关系、审计和幂等记录一起回滚。
- 流程启动需要 `workflow-instance:submit`。项目经理具备事件确认和项目任务能力，
  但若没有流程提交权限，不能通过新接口绕过该动作权限。
- 源码验收集：后端 143/143，前端 Vitest 20/20，OpenAPI 123/143；
  源码 Playwright 25 通过、1 个部署专用 MCP 用例跳过，覆盖 1440、1280、390。
- 正式 Nginx 部署 26/26 通过；新确认用例在桌面、1280 和 390px 均完成编辑、
  影响预览、新流程发起与刷新反查。
- 跨部门事件确认已由后端显式拒绝，前端确认选择器只展示事件所属部门项目。
- 发布包从干净合并提交 `ed78d48` 构建，manifest 无 `-dirty`。

## 2026-11-01 UX-003 统一工作事项中心启动

- 基线 `main@6a378f5` 工作区干净；前序 `req-mcp-frozen-v1` 存在；已创建独立分支
  `feature/ui-step1-unified-work-center`。
- 现有 `/api/v1/my-work` 仅聚合流程任务、流程实例和最近文件；`DepartmentWorkspaceDto`
  仍使用旧 `WorkItemDto`，不应与新员工工作事项模型混用。
- `WorkflowTask`、`ProjectTask`、`EventInbox`、`MinutesActionItem`、
  `McpHumanConfirmation`、`AgentRun` 均已有持久化模型和独立详情接口。
- 事件确认生成项目任务时，`ProjectTask.EventInboxId` 唯一；可直接用该关联排除重复主事项。
- 项目任务如关联 `WorkflowTaskId`，应优先展示流程待办，避免同一业务动作重复。
- 当前聚合为只读查询，无需新增业务状态；预计不需要数据库迁移。
- 当前 `AgentRun`、`McpHumanConfirmation` 和部分详情页没有查询参数聚焦能力，需要补前端深链定位。
- 现有 MyWork 页面仍显示旧技术化标题/节点代码来源，必须改为统一业务卡片。

### UX-003 源码验收发现（2026-11-02）

- `/api/v1/my-work` 已能只读聚合六类来源，同时保留原动作权限、组织、部门、项目和
  环境过滤；未新增数据库迁移或第二套业务状态。
- 事件已生成项目任务时事件让位；项目任务关联当前用户可见流程任务时项目任务让位，
  避免同一业务动作重复展示。
- 统一排序、风险标签、中文状态、去重和周界判断已抽为纯规则，并由单元测试锁定。
- 六类详情深链均进入现有业务页；MCP、听记、事件和 Agent 页面新增查询参数聚焦，
  不会复制详情或绕过后端动作校验。
- 员工主卡片不显示 UUID、TraceId、节点英文代码、表名或 revision；内部定位 ID 只用于
  前端深链和后端契约。
- 源码门禁首次失败仅为两个新增 C# 文件的行尾和编码不符合仓库规则；机械格式化后
  格式复验通过，不是业务逻辑失败。
- 源码 Playwright 为 22 通过、1 跳过；跳过项是只在 Nginx 正式部署中运行的 MCP OAuth，
  正式部署必须达到 23/23。
- 正式 Nginx 部署 E2E 达到 23/23，MCP OAuth、工具发现、作用域、撤销和真人确认均通过。
- 发布构建首次由 Windows PowerShell 5 外层启动时，将无 BOM UTF-8 中文脚本读成乱码；
  改用当前 PowerShell 7 直接执行后成功。该问题只影响本地调用方式，未进入编译或部署。
- 应用回滚 `final1 → req-mcp-final5 → final1` 通过；两段均完成 API、Worker、AiGateway、
  MCP 和页面健康检查。
- 部署备份 `RESTORE VERIFYONLY` 通过；独立恢复库为 ONLINE、10 条迁移、62 张应用表、
  5 张 MCP 表，六类工作事项所需来源表均存在。

## 2026-11-01 AIDT 标准 MCP 服务启动

- 当前 `main@509e62f`，工作区仅有 `IDEA-MCP-001 / MCP-001` 需求池未提交更新，必须保留。
- 当前部署 `req-batch3-final3`，冻结标签 `req-batch3-closeout-frozen-v1`。
- 现有 REST 为 114 路径、134 operationId，身份为 Cookie + CSRF；不能把浏览器会话直接交给长期 MCP 客户端。
- 现有 Application Service 已覆盖项目、事件、任务、流程、文件、导航和审计，MCP 应复用这些服务及数据范围规则。
- 官方 NuGet 当前稳定版：`ModelContextProtocol` 与 `ModelContextProtocol.AspNetCore` `2.3.0`，支持 .NET 10、stdio、Streamable HTTP、Resources、请求过滤器与 OAuth protected-resource metadata。
- 采用独立 `AIDT.Mcp`；HTTP 模式使用官方 ASP.NET Core MCP 集成，stdio 模式在同一可执行文件中切换 Host。
- OAuth 使用授权码 + PKCE S256、动态客户端注册、短期 Access Token、轮换 Refresh Token 和 RFC 7009 撤销；令牌只存 SHA-256 摘要。
- MCP 调用者数据范围必须同时受“用户现有权限/数据范围”和“本次 MCP 令牌选择部门”约束；平台管理员通过 MCP 时不能因角色绕过令牌部门范围。
- 高风险动作和敏感文件正文采用二次真人确认挑战；Agent 只能获得 challenge ID，不能自己确认。
- MCP 调用审计写入现有 `Audit.AuditEvent`，`Module=MCP`，记录客户端、令牌、工具/资源、用户、作用域、TraceId、幂等键、结果和耗时摘要。
- 不新增 Node.js MCP 后端，不暴露 SQL、Shell、任意路径或任意 HTTP 代理工具。
- 迁移 010 已在隔离库完成 `62→57→62` 表、`10→9→10` 迁移的 up/down/re-up，并应用到主库。
- `MCP-001` 工具目录包含 26 个业务语义工具；不包含 SQL、Shell、HTTP 或文件系统原语。
- `AIDT.Mcp` HTTP 与 stdio 均通过官方 SDK 完成 initialize、tools/list、tools/call。
- OAuth Access Token 1 小时、Refresh Token 30 天并轮换；`TokenFamilyId` 支持撤销整族令牌。
- MCP token 选择部门形成硬限制；资源读取和写入入口同时做用户权限与 token 部门双重校验。
- 服务身份令牌只能由 `mcp:manage` 管理员基于既有服务身份签发，最长 168 小时，不自动获得平台管理员权限。
- 高风险确认一次性绑定用户、客户端、工具、资源和请求摘要；重复消费和跨用户确认均被拒绝。
- 源码桌面/390px E2E：18/18 通过；部署专用 MCP/OAuth 用例在源码模式按设计跳过。
- 首次正式部署冒烟发现 Nginx `$host` 丢弃非标准端口，导致 MCP challenge 的
  `resource_metadata` 缺少 `:8088`。已改为 `$http_host` 并转发 `X-Forwarded-Host`；
  该问题只影响发现地址，不改变令牌或业务权限。
- 正式 OAUTH E2E 发现动态客户端注册的标准 snake_case 字段未映射到 C# 记录；
  已为注册请求增加 `JsonPropertyName`，避免把非空标准请求误判为空客户端名称。
- 从全新 worktree 构建时发现 `build-release.ps1` 未检查原生命令退出码，会在
  `dotnet publish --no-restore` 缺资产时继续生成不完整 ZIP；已为四个发布项目和
  npm 命令增加显式失败门禁。错误 `req-mcp-final3` 包保留但不使用、不打标签。
- `req-mcp-final2` 的浏览器 OAuth 已成功完成 consent 和回调，暴露出标准字段映射问题；
  修复后正式 OAuth、工具发现、`aidt_whoami`、人工确认、MCP 管理页和撤销均通过。
- 最终正式 Playwright：19/19，0 失败、0 跳过、0 flaky。
- 应用回滚 `final5 → final4 → final5` 通过。
- `final5` 部署备份恢复库：10 迁移、62 应用表、5 张 Mcp 表、2 项 MCP 权限、
  `agent.mcp` 和 MCP 导航入口均存在。

## 来自参考图片的规范范围

用户参考图片列出的 12 类企业级开发规范：

1. 编码规范
2. 分层架构规范
3. 接口设计规范
4. 数据库设计规范
5. 异常处理规范
6. 日志规范
7. 安全规范
8. 配置规范
9. 事务规范
10. 测试规范
11. Git 提交规范
12. 代码审查规范

这些是必要的工程基础，但对 AI 协作仍不充分。还缺少：

- 目标与范围优先级
- 模块地图和功能地图
- 上下文读取协议
- 变更影响分析
- Bug 修复闭环
- 会话恢复和交接
- 文档与代码一致性校验
- 离线构建和甲方复制交付
- Definition of Done

## 对 AIDT 的适用判断

前端、后端和数据库都需要规范，但应采用分层治理：

1. 开发前必须有最小编译、架构、数据、安全、测试和 AI 工作协议。
2. 具体模块文档在实现对应模块前完成。
3. 所有规则必须尽量由模板、测试、分析器、CI 和代码审查固化。
4. 长文档不是越多越好，必须有明确阅读入口和作用域。

## 冻结技术栈

`.NET 10 + SQL Server 2025 + Vue 3 + Element Plus + Elsa Workflows + Hangfire + Garnet + Nginx + AiGateway + 离线部署包`

## 当前项目目标

代码必须适合直接作为甲方便于复制、部署、接手和长期维护的正式代码仓库结构。

## 风险

| 风险 | 结果 | 控制 |
|---|---|---|
| 规范过多且分散 | AI 不读或读错 | 使用 AGENTS.md 单一入口和按需阅读表 |
| 规范与代码脱节 | 文档变成历史材料 | 变更任务必须同步更新功能地图和契约 |
| AI 一次加载全仓库 | 上下文耗尽 | 模块索引 + 精准检索 + 任务上下文包 |
| 修 Bug 只改现象 | 回归风险 | 复现测试 + 影响分析 + 邻近测试 |
| 所有规则只靠提示词 | 无法强制 | CI、分析器、测试、PR 检查表 |

## V1 验证结论

- 规范体系共覆盖 34 个入口、规范、模板、提示词和项目索引文件。
- 相对 Markdown 链接全部有效。
- `AGENTS.md` 是唯一主入口，`CLAUDE.md` 只做转发。
- 功能地图已建立首期 13 个功能和 8 个后端模块索引。
- 新功能、Bug、数据库、重构、发布和会话恢复均有独立提示词。
- 规范先行门槛已写入实施计划。
- 该体系仍需在首批代码落地后，通过分析器、CI、测试和模块 README 进一步固化。

## 2026-10-30 SQL Server 环境检查

- SQL Server 安装目录存在：`C:\Program Files\Microsoft SQL Server\170`。
- 安装日志 `Setup Bootstrap\Log\Summary.txt` 明确报错：
  - `ErrorCode: 3010`
  - “需要重新启动计算机。必须重新启动计算机才能安装 SQL Server。”
- 当前未发现 `MSSQL*` Windows 服务。
- TCP `1433` 未监听。
- `sqlcmd` 不在 PATH 中。
- 当前通过向日葵远程操作，不能现场重启公司电脑。
- 处理策略：先完成不依赖 SQL Server 的工程、契约、代码和单元测试；迁移、集成测试和数据库端到端验证等待公司电脑重启后继续。

## 2026-10-30 阶段 A 基线

### 环境事实

- `.NET SDK`：`10.0.403`。
- ASP.NET Core Runtime：`10.0.14`。
- Node.js：`24.18.0`；npm：`11.17.0`。
- `coding/` 当前不是 Git 仓库，无法用 Git 状态提供变更证据；以磁盘文件、构建和测试输出为准。
- `coding/` 原有内容只有文档、规范、模板和提示词，尚无正式后端、前端、数据库迁移和测试代码。

### 实现假设

1. 阶段 A 的后端项目按冻结目录固定为 `Api/Application/Domain/Infrastructure/Workflow/Integrations/Worker`，不提前拆成部门微服务。
2. Elsa、Hangfire、Garnet 只建立受控集成边界和配置项；具体版本与 SQL 持久化在阶段 B PoC 后锁定，不伪造已集成状态。
3. 为满足“不依赖 SQL Server 即可编译”，API 的存活检查不访问数据库；就绪检查读取静态部署能力状态并明确返回数据库未就绪，不尝试连接 1433。
4. 持久化接口不在阶段 A 用内存库/SQLite 代替。API 若调用写用例但没有 SQL 实现，应返回稳定的 `SERVICE_UNAVAILABLE` 状态，而不是制造持久化假象。
5. OpenAPI 使用仓库内可静态校验的机器可读契约，避免额外引入 Swagger UI 包或第二套 API 描述体系。
6. 数据库迁移状态固定为 `authored_not_applied`。迁移代码与发布 SQL 只表示结构设计产物，不表示数据库已创建或验证。

### 依赖策略

- .NET 包版本集中在根级 `Directory.Packages.props`。
- 前端版本由 `package.json` + `package-lock.json` 锁定，使用 `npm ci`。
- 只引入实现冻结技术栈与测试所需的依赖；不以临时包绕开架构。

### 风险

| 风险 | 控制 |
|---|---|
| 无 Git 仓库导致难以查看历史差异 | 输出实际文件清单并以构建/测试证据复核 |
| Elsa/Hangfire/Garnet 具体版本未锁定 | 阶段 A 仅保留边界文档和配置，不宣称已集成 |
| 迁移只静态编写，未在 SQL Server 验证 | 明确标记 `authored_not_applied`，阶段 B 执行并修正 |
| API 骨架可能被误认为完整业务系统 | 前端和 README 明确显示 `Skeleton / SQL Blocked` 状态 |

## 阶段 A 上下文包

- 当前目标：`prompts/07-SQL重启阻断下的分阶段搭建.md` 的阶段 A。
- 相关功能：`PLT-001`、`PLT-002`、`OBJ-001/002`、`WF-001` 至 `WF-004`、`TASK-001`、`AUD-001`、`AGT-001`、`INT-001`、`DEP-001`。
- 后端模块：Identity、Metadata、BusinessObject、Workflow、Audit、Integration、Agent、Configuration 的公开契约与模块边界。
- 前端：工作台、流程、业务对象、审计、Agent。
- 数据库：Identity、Metadata、BusinessObject、Workflow、Audit、Integration、Agent。
- 测试：Domain 状态规则、Application 权限/冲突、DTO/错误码、OpenAPI 静态契约、Vue 组件、类型检查与生产构建。
- 明确排除：SQL Server 连接、迁移执行、数据库集成、真实外部系统、真实模型、付款/质量放行/DCS 控制。

## 2026-10-30 阶段 A 实施结论

### 已确认产物

- 正式后端：`.NET 10`、7 个项目、严格引用方向、集中 NuGet 版本。
- 正式前端：Vue 3、TypeScript、Vite、Element Plus、Pinia、Vue Router。
- 领域契约：业务对象、流程实例/任务、身份权限、审计、Agent 和 Outbox。
- HTTP：统一响应、全局异常、TraceId、健康检查、静态 OpenAPI。
- 数据库：EF Core `InitialSchema`、9 个应用 Schema、34 张应用表、幂等升级/回滚 SQL。
- 测试：Domain、Application、DTO/验证、错误码、迁移模型、API 冒烟和前端组件/状态。
- 文档：模块 README、API、Schema、启动、DBA 审核、SQL 阻断和会话交接。

### 关键技术决策

1. NuGet 传递依赖 `System.Security.Cryptography.Pkcs 9.0.11` 被漏洞审计阻断，显式锁定到 `10.0.14`。
2. EF Core 使用 `ValueConverter` 将 `DateTimeOffset` 统一存为 UTC `datetime2(3)`，满足 SQL Server 提供程序类型映射。
3. API 的 `IsReady` 来自配置而非数据库探测；阶段 A 启动时不会尝试打开 SQL Server 连接。
4. 未就绪仓储直接抛出 `PERSISTENCE_NOT_READY`，不提供内存持久化假象。
5. 前端 `contract-preview` 只展示路由/权限骨架，并在界面明确标注；生产接入身份后必须切换 `enforced`。
6. 生成迁移时 EF 快照曾落入错误的嵌套目录，已移回正式 `Persistence/Migrations`。

### 最终验证

- 后端：0 警告、0 错误；28/28 测试通过。
- 前端：`npm ci`、类型检查、3/3 测试、OpenAPI 校验和 Vite 生产构建通过。
- 安全：NuGet 与 npm 生产依赖审计均无已知漏洞。
- 文档：Markdown 相对链接全部有效。
- 迁移：`authored_not_applied`，未连接 SQL Server、未执行迁移、未做集成或恢复验证。

## 2026-10-30 阶段 B 未完成范围

- SQL Server 服务、TCP 1433、实例版本和连接可用性验证。
- 正式 SQL 仓储、EF UnitOfWork、事务并发和幂等重放。
- Elsa 具体版本 PoC、流程推进、退回/终止和 SQL 持久化。
- Hangfire/Garnet 实际集成。
- 真实 SSO、API 权限过滤器、字段脱敏和数据范围。
- SQL Server 集成测试、迁移验证、备份恢复。
- 单条完整业务链和浏览器 E2E。
- 部署、离线依赖包、部署脚本和回滚演练。

## 2026-10-30 阶段 B 续跑核对

- 最新阶段 A 目标模式对话最终状态为 `PHASE_A_COMPLETE_WAITING_SQL`。
- 阶段 A 报告确认后端 28 项、前端 3 项测试通过。
- 当前再次检查仍无 `MSSQL*` Windows 服务，1433 没有 Listen 状态。
- `sqlcmd` 仍不在 PATH；阶段 B 不把 sqlcmd 缺失本身作为阻断，优先使用应用实际 SQL 客户端。
- 已新增 `prompts/08-阶段B-SQL恢复后继续.md`，其第一检查门是 SQL 服务和监听。
- 已新增 `prompts/09-AIDT全阶段目标模式.md`，覆盖 SQL、迁移、持久化、权限、Elsa、业务流程、Hangfire/Garnet、前端 E2E、部署和交接。

## 2026-10-30 SQL Server 未恢复根因

- 计算机已于 2026-10-30 08:04 重启。
- 三个常用 Pending Reboot 注册表标记均已不存在，重启条件已解除。
- 重启后没有新的 SQL Server Setup 日志，最新日志仍是 2026-10-29 22:00 的失败记录。
- 没有 setup 或 sqlservr 进程。
- 没有 `MSSQL*` 服务，没有 SQL 实例注册，1433 没有 Listen。
- 安装目录 `C:\Program Files\Microsoft SQL Server\170` 只有 Setup Bootstrap 和规则组件，没有数据库引擎。
- C 盘剩余约 100.6GB，不构成安装阻断。
- 已解压安装入口存在：`C:\SQL2025\Express_ENU\SETUP.EXE`。
- 判断：安装程序在 reboot check 失败后不会自动续跑，需要重启后重新运行安装程序。
- 检测到的安装介质是 SQL Server 2025 Express；可用于 Demo，不能直接视为生产版授权选型。

## 2026-10-30 SQL Server 已恢复

- 已通过 `C:\SQL2025\Express_ENU\SETUP.EXE` 完成静默安装，Exit Code `0`。
- `MSSQLSERVER` 服务正在运行，启动类型 Automatic。
- 默认实例注册为 `MSSQL17.MSSQLSERVER`。
- 产品版本 `17.0.1000.7`，Edition 为 Express Edition (64-bit)。
- TCP `1433` 在 IPv4 和 IPv6 均处于 Listen。
- 使用 Windows 身份验证连接 `master` 成功。
- 当前系统数据库存在，`AIDT` 业务数据库尚不存在。
- 下一步不再检查安装，而是进入阶段 B：创建 `AIDT` 数据库并执行迁移。

## 2026-10-30 全阶段目标恢复

- 已完整读取全阶段提示词和指定的全部基线、规范、交付与迁移文档。
- SQL 第一检查门通过：`MSSQLSERVER` Running，TCP 1433 在 IPv4/IPv6 均 Listen，PID `14868`。
- 目标不是生产上线，而是首个可交付 Demo。
- `AIDT` 业务库目前尚未创建，`InitialSchema` 仍为 `authored_not_applied`。
- 本轮必须避免把本地测试身份说成生产 SSO，不得把 Mock 连接器说成真实外部集成。

## 2026-10-30 SQL 连接与 InitialSchema 实施

- TCP 1433 监听正常，但当前 Windows 身份通过 TCP 使用集成认证返回 SQL 错误 `18452`，原因为当前身份被实例视为“不受信任的域”。
- 同一身份使用本机命名管道 `np:\\.\pipe\sql\query` 连接成功，SQL Server 实例、登录和权限可用。
- 首次 `dotnet ef database update` 使用 TCP 失败；第二次使用命名管道因 EF CLI 连接串反斜杠转义后报 error 87，未执行任何迁移。
- 改用已有的 SQLCMD ODBC 18 客户端和同一命名管道，创建 `AIDT` 数据库并执行 `database/migrations/001_InitialSchema.up.sql`。
- 迁移执行成功。
- 数据库校验：
  - Schema：9
  - 应用表：34
  - 外键：31
  - 非主键索引：54
  - 检查约束：11
  - 迁移历史：`20261029170022_InitialSchema`
- Demo 本机应用连接采用命名管道；生产部署仍应修复 SPN/域信任后使用受控 TCP 连接。

## 2026-10-30 阶段 A 重验证审计

### 发现

- 工作区已有后续运行留下的 SQL 仓储、Elsa、Hangfire、Outbox 和真实 API 页面代码，但这些不能算作本轮阶段 A 的 SQL 验证成果。
- 默认 API/Worker 配置曾被改成 `FULL_STAGE_IN_PROGRESS`、数据库就绪和迁移已验证，违反本轮“默认失败关闭”的要求。
- SQL 集成探针在未设置环境变量时直接 `return`，会被测试框架报告为通过；这属于伪通过，已改为显式 `Skipped`。
- API 契约测试曾依赖全阶段状态和 SQL 登录种子，已改为隔离的阶段 A 配置。
- 前端多个页面曾声称“真实 SQL 数据”“Elsa 持久化证据”和“治理生效”，已改为明确的阶段 A 契约/阻断文案。
- OpenAPI 缺少前端实际使用的 dashboard、CSRF、本地演示登录、退出、流程定义、流程时间线和 Agent 创建端点，已补齐。
- 迁移历史事实不能删除：`migration-state.json` 顶部恢复为本轮 `authored_not_applied`，后续历史执行证据保留在 `historicalExecution`。

### 阶段 A 当前结论

- 默认运行配置不连接 SQL Server，Worker 不启动后台处理或 Garnet 连接。
- 非 SQL 测试可独立通过；SQL 集成测试和 SQL 登录读取测试显式跳过。
- 数据库拥有两批迁移源文件，应用表设计为 35 张，但本轮不执行任何迁移。
- 格式统一、OpenAPI/前端复验和三视角自审已完成。

## 2026-10-30 阶段 A 最终验证

- 后端构建：0 警告、0 错误。
- 后端测试：29 总数；27 通过、0 失败、2 显式跳过。
- 前端：`npm ci`、类型检查、3/3 Vitest、OpenAPI 静态校验、生产构建全部通过。
- OpenAPI：28 个路径、33 个 operationId，operationId 无重复，前端实际使用路径已登记。
- 依赖安全：NuGet 全部项目无已知漏洞；npm 生产依赖 0 漏洞。
- `System.Security.Cryptography.Pkcs` 曾由 Elsa 传递引入 `9.0.4` 高危版本，已在 `AIDT.Workflow` 直接引用集中锁定的 `10.0.14` 修复。
- API 运行态：live `200`；ready `503 blocked`，`isReady=false`，`authored_not_applied`。
- Worker 运行态：live `200`；ready `503 blocked`；`backgroundExecution=false`，未连接 SQL/Garnet。
- 两个烟雾测试进程已停止，端口 5082/5083 已释放。
- Markdown 相对链接检查全部通过。
- SQL 迁移、SQL 集成测试、浏览器持久化 E2E、备份恢复和部署验证均未执行。

## 2026-10-30 全阶段恢复与数据库基准确认

- 当前 SQL Server：`17.0.1000.7 Express Edition (64-bit)`，服务 `MSSQLSERVER` Running，1433 在 IPv4/IPv6 Listen。
- 命名管道连接需要 SQLCMD 显式 `-C` 信任本机证书；仅用于本地 Demo，不代表生产证书策略。
- `AIDT` 数据库已存在，9 个应用 Schema、35 张应用表。
- EF 迁移历史包含 `InitialSchema` 和 `WorkflowPersistenceAndSecurity`。
- Elsa Schema 有 15 条迁移，Hangfire Schema 有 11 张表。
- 当前数据规模：7 用户、4 对象类型、5 主对象、5 AIDT 流程实例、37 Elsa 工作流实例、15 任务、28 审计、25 Outbox 全 Delivered、1 Agent 运行。
- 三条主流程已完成：`WF-001`、`WF-002`、`WF-003`；另有 WF-002 退回和 WF-004 终止实例。
- 发现历史对象 `SAMPLE-DEMO-002` 的流程为 Completed，但对象仍为 Draft/Version 1。该对象可能产生于后续缺陷修复前，不能作为当前对象状态联动证据；后续 E2E 必须新建对象并重新证明。
- 已生成并恢复验证全阶段备份：`C:\ProgramData\AIDT\Backups\AIDT_FULLSTAGE_20261030_092845.bak`。
- 备份 SHA256：`18C2637590B0E3CFDF6948BC6E26B9CE997B5FA50B9B8C058B1FC89F52EDC3AE`。
- 全阶段配置已恢复：数据库 Ready、Workflow Configured、Worker BackgroundProcessing 均开启；29/29 测试通过。
- 当前缺失：AiGateway、浏览器 E2E、权限/并发/迁移/恢复专项测试、Nginx、发布包、部署和回滚脚本、功能地图与交接文档全量更新。

## 2026-10-30 全阶段实施结论

- AiGateway 已实现为独立 .NET RuleSimulator，无外部模型调用。
- Mock 连接器返回明确 `Mocked` 回执，真实外部写始终关闭。
- 真实 API 权限测试确认：无组织范围返回 `DATA_SCOPE_DENIED`，无接口权限返回 `PERMISSION_DENIED`，Core 字段显示 `***`。
- API 幂等测试确认：同键同内容返回原对象，同键异内容返回 409。
- 数据库并发测试确认：两个版本守卫更新只有一个成功。
- 浏览器 E2E 确认：WF-001 可从 UI 发起并跨研发、质量、生产、管理员完成；对象状态 Active；审计和 Agent 操作可用。
- Nginx 测试入口为 `127.0.0.1:8088`，内部服务端口只监听回环。
- 最终 E2E 移动端 390px 无页面级横向溢出。
- 离线构建第一次发现 Rolldown Windows 原生绑定未进入 npm 缓存，已通过显式锁定 `@rolldown/binding-win32-x64-msvc@1.2.16` 修复。
- 发布/回滚第一次暴露 Nginx 路径、temp 目录和 worker 清理缺陷，均已修复并重新验证。

## 2026-10-30 G12 最终审计

- 最终部署版本：`demo-final3`。
- 最终发布包 SHA256：`684EE630E912AF3B55CF32C3B8464A9850A3F15F540CFE08A04366F4E882643E`。
- Nginx `8088`、API `5080`、Worker `5081`、AiGateway `5100`、Garnet `6380` 均只监听本机回环。
- 最终 Nginx 部署环境 Playwright：2/2 通过，0 skipped/flaky。
- 最终数据库计数：23 个业务对象、12 个 AIDT 流程实例、38 个任务、85 条审计、71 条 Outbox（Pending=0）、83 个 Elsa 实例、5 个 Agent 运行。
- 当前有效流程：Completed 8、Returned 1、Terminated 1；另有失败演练留下的 Running 2，不影响最终成功闭环和验收证据。
- Worker 心跳 `PublishedAt=2026-10-30 06:22:04.593 UTC`，证实多次版本重启后后台任务继续运行。
- Demo 全部验收条件已满足；生产 SSO、真实外部系统、生产 DBA 签认和高可用仍未完成。

## 2026-10-30 岗位场景运行时演示

- 用户明确本轮不修改前后端代码，只在现有网页中创建真实业务对象和流程实例。
- 已通过网页表单新建 4 个业务对象，对象编号统一使用 `SCN-` 前缀和日期后缀，便于后续筛选。
- 已用 5 个现有演示身份实际处理任务：研发工程师、质量工程师、供应链专员、生产班组、财务审批人；平台管理员承担经理/项目经理复核职责。
- 4 条流程均完成：采购到付款 5 节点、客户样品到转产 5 节点、批次异常到 CAPA 4 节点、客诉到 CAPA 4 节点。
- SQL 核对结果：4 个场景对象、4 个流程实例、18 个任务，全部为 Completed/Approved；关联审计事件 26 条。
- Outbox 当前 105 条均为 Delivered，无 Pending。
- 证据截图：`docs/project/test-artifacts/scenario-center-proof-full.png`、`docs/project/test-artifacts/scenario-audit-proof.png`。
- 当前系统没有独立“项目经理”演示身份；项目经理能力暂映射到平台管理员。增加正式岗位身份属于后续权限配置或开发工作，本轮未修改。
- 当前 Agent 页面可以运行、确认和停止规则模拟，但现有网页创建 Agent 运行时不能选择业务对象或流程实例，因此本轮没有把 Agent 运行伪装成已绑定场景。后续若要“智能体自动收集企业事项”，需要继续设计事件入口、Agent 与对象/流程的关联、确认和证据链。

## 2026-10-31 产品规划文档结构

- 产品目标、需求、技术栈和计划方案已统一迁入 `docs/001-产品PRD/`，不再保留仓库根目录副本。
- `01-需求.md` 已更名为 `01-需求基线.md`，正文需求和冻结状态未改变。
- 新增总入口 `docs/001-产品PRD/README.md`，明确基线文档与需求池的职责和流转顺序。
- 新增 `docs/001-产品PRD/04-需求池.md`，用于持续收集尚未正式立项的产品想法。
- 需求池已登记员工工作台、部门工作区、文件附件、钉钉 AI 听记、流程节点能力包、超时代理、企业事件收集、Agent/AiGateway 串联和项目经理岗位等候选需求。
- `AGENTS.md`、仓库 README、AI 工程宪法、上下文阅读规范和相关提示词已切换到新路径。

## 2026-10-31 批次 2 目标模式任务书

- 批次 1 已冻结为 `req-batch1-final6`，批次 2 分支基线为 `48ea1a4`。
- 批次 2 锁定四项需求：AI 听记转任务、节点能力包与 SLA、超时提醒/升级/数字员工受限代理、AgentRun 与 AiGateway 关联。
- 已创建 `prompts/12-需求池批次2目标模式.md` 并加入提示词索引。
- 任务书明确真实钉钉、音频转写和外部模型不默认启用；无授权时必须使用明确标识的 Mock/Adapter。
- 高风险节点即使超时也不得由数字员工自动通过。

## 2026-10-31 需求池冲突审查与批次 1

- 需求池文件存在且作为候选需求活文档，不直接覆盖冻结基线。
- 当前代码有 Document 三张表但没有文件上传、下载和预览 API。
- 当前身份只有 Organization 和按组织的数据范围，没有 Department 主数据。
- 当前 WorkflowTask 没有 DueAt、SLA、转办和委托。
- 当前 AgentRun 没有 BusinessObjectId、WorkflowInstanceId 或 AiGateway 调用链。
- 当前 AiGateway 强制 RuleSimulator，外部模型调用关闭。
- 当前没有 Project、里程碑、阶段门和项目风险领域对象。
- 因此需求池不能一次性全部实施，应拆成三个批次。
- 批次 1 定为 UX-001、WS-001、DOC-001、DATA-001。
- 已生成 `prompts/10-需求池批次1目标模式.md`。

## 2026-10-31 批次 1 基线与恢复点

- Git 已初始化并完成基线提交 `8aff291`。
- 批次分支：`feature/requirement-pool-batch-1`。
- 迁移前版本：`20261030004100_WorkflowPersistenceAndSecurity`。
- 迁移前应用表：35。
- 数据库备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH1_PRE_20261031_010423.bak`。
- 备份 SHA256：`ED12133E0F3B4545E7AF45ADE9D2DA2D9548A24D5A5DD6CB458994D2B51403DE`。
- `RESTORE VERIFYONLY`：通过。
- 现有 Attachment/DocumentVersion 只有基础字段；没有通用关联、DepartmentId 和环境字段。
- 现有 Department 不存在，UserDepartmentMembership 不存在。

## 2026-10-31 需求池批次 1 阶段 A 收口

### SQL BLOCKED

- 原因：SQL Server 安装要求重启，错误码 3010
- 已检查：沿用历史记录中的服务、1433、sqlcmd 证据；本轮不重新探测或尝试修复
- 未执行：迁移、集成测试、数据库端到端验证
- 继续条件：公司电脑重启后 SQL Server 服务可用，1433 监听，应用连接成功

### 编译与契约

- 初次恢复时有 16 个编译错误，根因是 `DomainChecks` 可见性、DTO 类型不一致、
  实体配置落点、业务对象 Restore 参数错位、缺少仓储命名空间和区域格式。
- 上述错误已修复，后端恢复为 0 警告、0 错误。
- DepartmentWorkspace 最近流程统一使用员工可读的 `WorkItemDto`。
- 部门、对象、流程、任务和文件查询均在服务端应用部门与当前环境过滤。
- 平台管理员显式 `includeTestData` 才可跨环境；普通员工不能通过 query 绕过。
- 审计列表按组织用户过滤；普通员工仅能看本人或授权部门实体的审计。
- 幂等作用域加入用户 ID，防止不同用户复用同一幂等键读取他人结果。

### SQL 边界事故与纠正

- 早期一次全量测试在配置尚未切回阶段 A 时误触发既有 SQL 登录式 API 集成用例，
  登录返回 HTTP 500。
- 发现后立即停止 SQL 路径，未继续诊断服务、端口、连接或安装，也未执行写业务数据。
- 随后将默认配置切回 `Database.IsReady=false`、`authored_not_applied`，
  并新增 `SqlIntegrationFact` 使 10 个 SQL 用例默认显式跳过。
- 后续所有测试与烟雾检查均在 SQL 阻断配置下完成。

### 文件安全

- 扩展名、MIME、文件签名、大小和 SHA256 同时校验。
- 空文件、未知扩展名、MIME 不匹配、伪造 PDF/PNG/JPEG/Office 签名被拒绝。
- Office 文件校验 ZIP 内对应目录，不支持内嵌预览。
- 敏感/核心文件默认 `PendingManualReview`，普通员工在未 Clean 前禁止下载或预览。
- 上传正文采用 GUID 物理文件名，路径穿越被拒绝，数据库不向前端返回存储路径。
- 文件旧版本保留；删除仅创建申请并写审计，不物理删除。

### 迁移

- 生成 EF Migration `20261030172128_003_DepartmentWorkspaceFilesAndEnvironment`。
- 作者态应用表从 35 增至 38。
- 迁移包含现有对象、流程、任务和附件的部门与环境回填。
- 增加默认部门、成员/主部门、负责人、department DataScope 和部门/文件权限种子。
- 更新 DemoSeed，保证新库在 003 之后执行种子时也能满足新外键和检查约束。
- 幂等 up/down SQL 已生成，状态固定 `authored_not_applied`。

### 验证

- `dotnet test AIDT.slnx --no-restore`：58 总数，48 通过，0 失败，10 显式跳过。
- 跳过项均为 SQL Server 连接、SQL 约束或登录态 SQL API 集成。
- 前端：5 个测试文件、7 个测试通过；类型检查和生产构建通过。
- OpenAPI：38 路径、46 operationId 静态校验通过。
- Dotnet format 与依赖漏洞审计通过。
- API live `200`、ready `503 blocked`、未登录我的工作 `401`。
- Worker live `200`、ready `503 blocked`、`backgroundExecution=false`。
- 测试进程已停止，5098/5099 无监听残留。

### 未验证

- 003 up/down、回填、外键、唯一约束和检查约束。
- 部门/环境 SQL 过滤、文件持久化、并发和越权审计。
- 浏览器登录、上传、预览、版本、敏感文件拒绝和 390px 回归。
- 备份恢复、部署和回滚。

## 2026-10-31 需求池批次 1 最终结论

- 状态：`REQUIREMENT_POOL_BATCH_1_COMPLETE`。
- 003/004 已在隔离库和主 `AIDT` 应用：38 应用表、45 外键、26 检查约束。
- SQL/API 集成 64/64；Sensitive/跨部门文件拒绝与审计、部门/对象/流程/文件环境隔离均通过。
- 前端 7/7、OpenAPI 38 路径/46 operationId、Playwright 5/5。
- Playwright 覆盖员工入口、管理部门分层、部门工作区、拖拽/点击上传、预览、版本、
  删除申请、敏感文件拒绝、WF-001 和 390px。
- 应用回滚 `req-batch1-b → req-batch1-a` 后健康 200；最终 `req-batch1-final` 部署健康。
- 备份恢复临时库为 38 表、4 条迁移；测试库已删除。
- 离线：233 NuGet 包、约 45.6 MB npm 缓存，restore/build 通过。
- 最终发布包 SHA256：`6DEFDDEB879C2F3CCC73BBD786E4CFDAA1FD3FA3E37E428D6D978B6B6A99353B`。
- 整个 AIDT 需求池未完成，本状态只表示批次 1 完成。

## 2026-10-31 批次 1 发布冻结审查

- 当前分支：`feature/requirement-pool-batch-1`。
- 批次 1 功能、SQL/API 测试、前端测试和 Playwright 均通过。
- `req-batch1-final5` 已部署并运行，健康检查 200。
- 批次 1 的源码、迁移、测试和文档仍未提交，Git 工作区包含大量修改和未跟踪文件。
- 现有发布包 manifest 的 `sourceRevision` 为 `8aff29131d3c6204d88aea47c5c61920581b3f0a-dirty`，不能从干净提交复现。
- 结论：功能完成，版本治理未闭环；暂不进入批次 2 功能开发。
- 已生成版本冻结提示词，要求提交、合并、干净重建、部署验证、标签和创建批次 2 分支。

## 2026-10-31 批次 1 阶段 B 启动

- SQL 第一检查门通过：`MSSQLSERVER` Running，1433 IPv4/IPv6 Listen，版本 `17.0.1000.7`。
- 新迁移前备份成功，`RESTORE VERIFYONLY` 通过：
  `C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH1_PRE_003_20261031_013442.bak`。
- 备份 SHA256：`7C46A4C5BFAECB1FD0EBE76EB4D8D0380A526F02F6DAA6AA1A60C38F093CB784`。
- 隔离库：`AIDT_REQ_BATCH1_TEST_20261031_013442`。
- 第一次 003 up 失败：回填 UPDATE 与新增列处于同一 SQL 批次，旧元数据不可见。
- 第二次 003 up 失败：筛选索引要求 `QUOTED_IDENTIFIER ON`。
- 调整 EF 回填列限定方式，并在正式幂等发布脚本增加会话设置与回填前 `GO` 分批。
- 当前隔离库验证结果：003 up 38 表、45 外键、26 检查约束；6 部门、8 成员、8 部门 DataScope、7 部门/文件权限；无空部门或非法环境数据。
- 003 down 恢复 35 表，移除 003 历史、部门表、部门权限和 department DataScope；随后重新 up 通过。

## 2026-10-31 批次 1 版本冻结工作区审计

- 当前分支：`feature/requirement-pool-batch-1`；HEAD 为基线 `8aff29131d3c6204d88aea47c5c61920581b3f0a`。
- 本地分支只有 `main` 与批次 1 分支；未配置 Git 远程，不会执行推送。
- 工作区有 89 个已跟踪修改、46 个未跟踪文件，合计 135 个状态条目。
- 改动范围与批次 1 的 `UX-001`、`WS-001`、`DOC-001`、`DATA-001`、迁移 003/004、对应测试、部署修正和交付文档一致。
- 未发现不属于批次 1 的业务源码、用户目录或来源不明的未跟踪文件。
- 未跟踪文件中没有数据库、备份、上传正文、日志、密钥、`bin`、`obj`、`node_modules`、`dist` 或临时压缩包。
- 密钥特征扫描未发现私钥、云访问密钥、Token、密码或共享密钥。
- 默认连接串沿用基线中的本机 Windows 集成认证命名管道，不含明文密码；仅用于本机 Demo。
- `.tmp/`、`bin/`、`obj/`、`node_modules/`、`dist/` 已被 Git 忽略。
- 发现 Playwright 瞬时输出目录 `docs/project/test-artifacts/playwright-results/` 未被忽略，且基线已跟踪其中的 `.last-run.json`。
- 处理决定：加入忽略规则，并从索引移除该瞬时文件但保留磁盘内容；正式 JSON/HTML 报告和截图作为测试证据继续保留。

## 2026-10-31 批次 1 提交前门禁复验

- 后端构建：0 警告、0 错误。
- 默认后端测试：64 总数，48 通过、16 个 SQL/API 用例显式跳过，0 失败。
- 隔离测试库 `AIDT_REQ_BATCH1_TEST_20261031_013442` 的 SQL/API 全量测试：64/64 通过，0 跳过、0 失败。
- `dotnet format --verify-no-changes`：通过。
- NuGet 直接与传递依赖漏洞检查：全部项目无已知漏洞。
- `npm ci`：262 个包安装成功；完整依赖树有 5 个中危开发依赖，未自动升级或强制修复。
- 前端类型检查、生产构建通过。
- 前端测试：5 个文件、7/7 通过。
- OpenAPI：38 路径、46 operationId 通过。
- `npm audit --omit=dev`：0 漏洞。
- 现有 `req-batch1-final5` 部署：root/API/Worker/AiGateway/capabilities 全部 200。
- 部署环境 Playwright：5/5 通过，0 失败、0 跳过、0 flaky。
- 当前发布包仍为 `sourceRevision=8aff291...-dirty`，只作为提交前验证基线，不能打最终标签。

## 2026-10-31 批次 1 干净发布结论

- 批次 1 已拆为 4 个源码/测试/文档提交，并以 `--no-ff` 合并到 `main`。
- 合并提交：`7aa50459acc0f1ec66ba70caa16d82cc574415af`。
- `req-batch1-final6` 从该干净 `main` 构建，manifest 的 sourceRevision 为完整提交号且不含 `-dirty`。
- 发布包 SHA256：`008711843CDED2B6F0D0814959FE9B36F432C73FF4277E69E93D5164CE8E4895`。
- 注释标签 `req-batch1-final6` 指向 `7aa50459acc0f1ec66ba70caa16d82cc574415af`。
- 最终部署健康检查全部 200；批次 1 四项能力均为 `available`。
- 主库 38 张应用表、4 条 EF 迁移、任务主部门不一致 0。
- 部署前备份 `RESTORE VERIFYONLY` 通过，SHA256 为 `F73950AF218642E5F218DA1091452104437C3D9C2215A1CC86CFAB407146F40A`。
- 部署环境 Playwright 5/5 通过，0 failed、0 skipped、0 flaky。
- 批次 2 只创建分支基线，不实现任何批次 2 功能。
- `main` 发布记录提交：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`。
- `feature/requirement-pool-batch-2` 已从上述干净 `main` 创建，起始基线为该提交。
- 合并基线与 `main` 一致，标签 `req-batch1-final6` 在批次 2 分支可访问。
- 批次 2 当前没有代码、迁移、API 或页面变更。

## 2026-10-31 批次 2 启动审计

- 已完整读取任务书、全部 30 项指定基线、规范和三个规划文件。
- 当前分支 `feature/requirement-pool-batch-2`，HEAD `691252d63654e3d691865cda0d0bbf6c33486108`。
- 合并基线与 `main` 均为 `48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`；
  批次 1 标签 `req-batch1-final6` 指向 `7aa50459acc0f1ec66ba70caa16d82cc574415af`。
- 现有未提交内容为批次 2 提示词、提示词索引、规划文件和多张批次 1 E2E 刷新证据，均属于有效工作，不丢弃。
- SQL Server 2025 Express `17.0.1000.7`，`MSSQLSERVER` Running，1433 IPv4/IPv6 Listen，`AIDT` ONLINE。
- 功能地图不存在 `MIN-001`、`WF-005`、`AGT-002`、`AIG-002` 编号冲突。
- 当前 AgentRun 只有动作、金额、风险、状态和人工确认；没有业务关联和 GatewayCall。
- 当前 WorkflowTask 没有 DueAt、SLA、提醒、升级、DecisionMode 或 AgentRunId。
- 当前 AiGateway 是 RuleSimulator，外部模型关闭。
- 当前钉钉连接器 `Disconnected`，无真实消息能力。
- 批次 2 采用结构化新增表，不用不可查询 JSON 承载节点策略。
- 主库新备份已通过 `RESTORE VERIFYONLY`：
  `C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_PRE_005_20261031_094122.bak`。
- 备份 SHA256：`4F67C0A6CFF8CEC733CD863D5A8CDABF48B16B42A3F668F087DA1AB75160EA8F`。
- 隔离测试库 `AIDT_REQ_BATCH2_TEST_20261031_094122` 已从该备份恢复，
  验证为 4 条迁移、38 张应用表。
- 005 首次 up 再次触发 SQL Server 同批次新列可见性问题，事务中止；改用动态 SQL 后，
  从空恢复点重新执行，避免修补失败库造成假通过。
- 005 down 首次 re-up 暴露代理 Agent 数据未在 down 清理，唯一索引冲突；Down 已增加
  代理 Profile、服务用户和批次 2 权限清理。
- 最终隔离序列：4 迁移/38 表 → 005 up 5 迁移/44 表 → 005 down 4/38 → 005 up 5/44。
- 隔离库 SQL/API 测试最终 70/70，通过新增听记、SLA、代理、Gateway 和错误模型测试。
- 前端测试 9/9，OpenAPI 53 路径、62 operationId，生产构建通过。
- 主库 005 已应用：5 条 EF 迁移、44 张应用表、3 张 Minutes 表、
  2 个 AgentProfile、8 项新权限；历史 AgentRun 组织/部门空值 0。
- 第一次 final1 部署的健康和能力检查通过，但重复 E2E 发现 AgentProfile
  按名称排序会受中英文排序影响，测试点击“第一个”代理时不能稳定命中业务 Agent。
- 修复为按稳定 AgentProfile ID 排序并让 E2E 明确选择业务协同 Agent。
- final1 发布包保留但被 final2 顺延取代；没有覆盖 final1 或批次 1 发布包。
- final2 已部署、回滚到 final1 后恢复、最终 E2E 6/6。

## 2026-10-31 批次 2 独立复核

- 当前 `main` 工作区干净，`req-batch2-final2` 标签指向 `875204e`，当前部署版本为 `req-batch2-final2`。
- 运行中能力和部署清单一致：5 条 EF 迁移、44 张应用表、AI 听记/节点 SLA/Agent 代理/AiGateway 四项能力均返回 `available`。
- 隔离测试库重新执行 SQL/API 测试：70/70 通过，0 失败、0 跳过。
- 前端类型检查通过，组件测试 9/9 通过，OpenAPI 53 路径/62 operationId 通过，生产构建通过。
- 对正式部署重跑 Playwright：6/6 通过，0 failure、0 skipped、0 flaky。
- 实测听记页面已完成：听记登记、AgentRun 确认、规则模拟执行、行动项人工确认和任务创建。
- `NodePolicySlaAndAgentProxy_AreVersionFrozenAndHumanAuthorized` 覆盖节点策略冻结、SLA 提醒/升级、重复扫描幂等、低风险代理真人确认和高风险策略拒绝。
- `AgentGateway_UnapprovedModelFailsWithoutCreatingTasks` 覆盖 AiGateway 未授权模型失败且不产生业务任务。
- 当前主库没有预置 `WorkflowNodePolicy`，因此默认流程页面显示“该版本尚无节点策略”，现有任务显示“未配置 SLA”。这是演示数据未预置，不是节点能力缺失。
- 真实钉钉接口、音频转写和真实外部模型仍未接入；当前听记为手动导入/RuleSimulator。

## 2026-10-31 需求池批次 2.1 启动

- 已完整读取 2.1 目标和全部 31 项指定基线。
- 当前 `main` HEAD `a6d270f`；批次 2 标签 `req-batch2-final2` 指向 `875204e`。
- 当前部署 `req-batch2-final2`，页面/API/Worker/AiGateway ready 均 200。
- 工作区保留 6 张有效 E2E 刷新证据和 `batch2-minutes-verification.png`，不丢弃。
- 已创建分支 `fix/requirement-pool-batch-2-1`。
- DOCX 使用 .NET 内置 ZIP/XML，不新增 NuGet。
- 当前主库 `WorkflowNodePolicy` 仍为 0；2.1 通过迁移 006 幂等预置专用低风险策略。
- 2.1 主库备份通过恢复校验：
  `C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_1_PRE_006_20261031_121903.bak`；
  SHA256 `E5F17A4E50B9FF845704A4A3198E9A1ED28A5BF7A81458EAA65563CDCB5470F2`。
- 隔离库 `AIDT_REQ_BATCH2_1_TEST_20261031_121903` 已恢复，5 迁移、44 应用表。

## 2026-11-01 批次 2.1 收尾审计

- 当前 `main` HEAD 为 `c68a799`，标签 `req-batch2-final3` 为注释标签，解引用提交为 `5382fe7738eedb6a6cf80cac74d7371a042fdd4e`。
- 工作区初始仅存在 6 个 E2E 证据文件变化，业务源码、迁移、配置和项目文档均无未提交改动。
- 最新 `playwright-report.json` 记录 8 个测试全部通过，失败 0、跳过 0、flaky 0；报告开始时间为 `2026-10-31T16:36:16.580Z`。
- 当前部署状态仍为 `req-batch2-final3`，部署时间为 `2026-10-31T13:03:41+08:00`；manifest 源码提交为 `5382fe7738eedb6a6cf80cac74d7371a042fdd4e`，不含 `-dirty`，迁移状态为 `applied_verified`。
- 最新 E2E 报告晚于最终部署，且保留 8 个用例的新执行耗时、时间戳和截图，属于有效、可追溯的正式复验证据，应单独提交而非丢弃。
- 页面、`/health/live`、`/health/ready`、`/api/v1/platform/capabilities` 均返回 HTTP 200；能力接口项目状态为 `REQUIREMENT_POOL_BATCH_2_1_READY`。
- SQL Server 服务运行且 1433 监听；`AIDT` 为 ONLINE，6 条 EF 迁移，应用 Schema 共 44 张业务表；`dbo`、`Elsa`、`HangFire` 不计入应用表。
- 标签 `req-batch2-final3` 为注释标签，解引用目标仍为 `c68a799`；当前部署和 `current-version.txt` 仍为 `req-batch2-final3`。
- 已在 `chore/closeout-batch-2-1` 分支以提交 `97646e1` 单独提交上述 6 个证据文件。
- task_plan 的旧版 `REQUIREMENT_POOL_BATCH_2_1_IN_PROGRESS` 段落已明确标记为历史快照；顶部保留唯一当前权威状态，并明确批次 3 是最后一个业务 Demo 批次，不是生产项目最终完成。

## 2026-11-01 批次 3 启动与恢复点

- 批次 3 分支 `feature/requirement-pool-batch-3` 已从 `main` 合并提交 `2fd0177` 创建。
- Feature ID 无冲突：`PMO-001`、`PMO-002`、`EVT-001`、`TASK-002`。
- SQL Server 默认数据目录为 `C:\Program Files\Microsoft SQL Server\MSSQL17.MSSQLSERVER\MSSQL\DATA\`，主库文件也位于该目录。
- SQL Server Express 不支持 `BACKUP DATABASE ... WITH COMPRESSION`，首次备份未生成可用文件。
- 改用不压缩的 `COPY_ONLY, CHECKSUM` 后备份成功，`RESTORE VERIFYONLY` 通过。
- 备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH3_PRE_007_20261101_004945.bak`。
- 备份 SHA256：`B872EE36A940F338C91A3B15082F92E8D7905BA55C94C28C218F5EC0CB1C274E`。
- 首次恢复误用了不存在的 `C:\ProgramData\AIDT\Data` 路径；按实际 SQL 默认数据目录重试。
- 隔离库：`AIDT_REQ_BATCH3_TEST_20261101_004955`，ONLINE、6 条迁移、44 张应用表。

### 批次 3 实现与验证发现

- `PROJECT` 作为唯一业务主对象，项目公共 Title/Status/CreatedBy/Department/Environment/Version 保存在
  `BusinessObject`，专属字段只进入 `PMO.ProjectProfile`。
- 新增 `PMO` 7 表：项目档案、成员、里程碑、关系、项目任务、事件收件箱和事件建议。
- `demo.pm` 使用现有部门/角色体系；`agent.project` 为独立服务身份，事件建议 Agent 为 A2/Low。
- SQL Server 延迟名称解析导致首次自定义种子引用新列失败，已移除同批回填；历史标题和创建人通过独立批次回填，缺失均为 0。
- 筛选索引要求 `QUOTED_IDENTIFIER ON`，迁移首部已显式设置，最终脚本稳定。
- 固定部门 ID 不能假设由早期种子生成；007 改为按组织和 `RND` 代码查询实际部门 ID。
- Element Plus 为按需手动注册，新增项目页需要 `ElProgress`、`ElDatePicker`、Radio 和 Timeline 组件。
- E2E 首轮暴露新建项目按钮在页面加载完成前可点击，现由 loading 禁用，保证部门候选已加载。
- `req-batch3-final1` 正式部署后，管理员登录返回 HTTP 502；根因是每个权限/数据范围单独写入
  Cookie claims，管理员权限增长后 Set-Cookie/响应头超出限制。项目经理权限较少，因此未触发。
- 会话改为单个压缩权限声明和单个压缩数据范围声明，访问器同时兼容旧的多声明 Cookie；
  新增 packed/legacy 两类身份解析单元测试。`final1` 包保留但不得作为最终标签。

### 批次 3 最终发布

- `req-batch3-final2` 从干净 `main` 合并提交 `9b9b7fe` 构建，manifest 不含 `-dirty`。
- 发布包 SHA256：`C4875993E77D183EBA8EEE1F2D857AB35A11C815C2B00BE5DED85D5C9DB7E435`。
- 正式部署 E2E：11/11，0 failure、0 skipped、0 flaky。
- 应用回滚 `final2 → req-batch2-final3 → final2` 成功，最终健康检查全部 200。
- 最终部署前备份恢复为独立库：7 迁移、51 应用表、PROJECT/项目经理/事件 Agent 均存在。
- 标签 `req-batch3-final2` 指向 `9b9b7fe`；没有移动或覆盖旧标签。
- 批次 3 业务 Demo 完成，整个 AIDT 生产项目仍未完成。

### 批次 2.1 实施发现

- DOCX 不需要新增 NuGet；内置 ZIP/XML 足够读取正文，2 MiB 上限延续听记接口。
- 自动代理必须只读取任务冻结的 `NodePolicyId`，不能读取该节点最新策略。
- 独立服务身份使用 `AgentProfile.ServiceUserId`；原责任人保存在 AgentRun，
  任务明确标记 `DecisionMode=AgentProxy`。
- `sla-auto-proxy:{taskId}` 过滤唯一索引保证同一任务只有一个代理运行。
- 防御性 Worker 即使遇到数据库中错误配置的“永久人工边界 + 自动代理”，也必须拒绝。
- EF CLI 环境变量未覆盖设计时连接，006 曾提前应用到主库 `AIDT`；迁移为增量、
  未删除历史数据，主库已核验默认流程和策略。后续手工隔离脚本使用 SQLCMD 显式库名，
  不再回滚已应用主库迁移。
- 生成 SQL 的自定义 Seed 与新增列位于同一批时会在 SQL Server 编译阶段报列不存在；
  手工脚本已在索引创建后增加批分隔 `GO`。
- final3 部署后实际观察到两条 `MATERIALS_CONFIRM` 任务各只有 1 个
  `WorkflowTaskProxy` AgentRun，触发模式为 `SlaTimeout`，幂等键与任务 ID 一致。
- 应用回滚 `final3 → final2 → final3` 完成，最终 root/API/Worker/AiGateway 均 200。
- 部署备份恢复到独立库后为 6 条迁移、44 张应用表，默认策略 seed 可恢复。

## 2026-11-01 3157C 真实流程试点发现

### 已证明可用的链路

- 使用现有 API/网页成功创建项目 `PRJ-3157C-20250808`，关联研发、项目经理和质量工程师。
- 四张原始 PNG 以 `Sensitive / Active` 上传并最终在项目详情中显示。
- 事件可关联附件、保存完整原文、调用项目事件建议数字员工并生成项目、负责人、截止日期、风险和缺失信息建议。
- 真人确认后成功创建 P1 项目任务，质量工程师可完成该任务，项目进度和里程碑进度可以刷新。
- 项目详情能够反查事件来源、任务、附件关系、业务对象和追加式审计时间线。

### 需要修复的问题

1. `SqlEventInboxService.LoadAccessibleEventAsync` 通过 `ApplyEventScope` 的 `AsNoTracking` 查询读取事件；`AnalyzeAsync` 和 `ConfirmAsync` 后续修改 `inbox.Status`、`AnalysisRunId`，但这些修改没有显式附着到 EF 跟踪，因此事件状态一直保持 `Received`。
2. `SqlProjectService.ChangeStatusAsync` 使用的项目实体来自 `LoadProjectEntityAsync` 的非跟踪查询；修改 `Status/Version` 后调用 `SaveChanges` 不会持久化项目主对象。结果出现“提示成功、审计记录 Completed、页面仍为 Active”的矛盾。
3. 文件上传接口保存了 `BusinessObjectId`，但项目详情读取文件时主要依赖 `ProjectRelations` 和 `AttachmentLinks`；仅传业务对象 ID 不会自动出现在项目文件区，需要额外创建附件关系。
4. `ProjectRules.TryExtractDeadline` 对“任务截止日期：2025-08-15”生成 `2025-08-16 02:00:00 +08:00`，存在一天偏移；本次在人工确认任务时覆盖为正确值。

### 对前端设计的影响

- 配置端和员工端必须以持久化后的服务端状态为准，不能用接口响应直接假定成功。
- 事件、项目、附件等对象需要统一的“关联一次、各视图一致”规则，避免用户手工补关系。
- 前端应禁用已完成事件的确认按钮，并将“分析完成/确认完成/任务已创建”拆成明确状态。

### 试点后的配置化产品结论

- 企业各岗位不需要分别建设孤立数据库；应采用统一组织、业务对象、流程实例、任务、文件和审计账本。
- 岗位差异应通过菜单、数据范围、动作权限、字段视图、流程模板、节点能力包、提醒和报表体现。
- 配置中心只能组合已注册、可验证、可审计的标准能力，不能在前端生成任意后端代码。
- 配置必须支持草稿、测试、影响分析、审批、发布、版本冻结和回滚。
- 普通员工使用业务语言发起事项；授权流程负责人配置部门流程；平台团队控制对象模型、连接器、安全和高风险发布。

## 2026-11-01 配置中心后端实现结论

- 现有 `Configuration.ConfigurationVersion` 足以承载版本记录，无需创建第二套配置表。
- 配置发布直接生成 `Workflow.WorkflowDefinition / WorkflowVersion`，避免“前端配置成功但流程引擎不可执行”。
- 配置版本使用 `Revision` 做乐观并发控制，发布与回滚使用幂等键及审计。
- 配置仅允许引用已注册对象类型和有效真人处理人；服务身份不能作为流程节点处理人。
- 配置节点须无循环、全部可达且至少有一个完成节点。
- 配置结构发布和现有节点策略配置是两个互补层次：前者管理流程结构，后者管理 SLA、附件和数字员工策略。
- 008 的幂等 SQL 脚本再次暴露 SQL Server 同批次新列不可见问题，已在列新增与数据回填之间增加 `GO`；EF 迁移本身按独立命令执行不受影响。

## 2026-11-01 配置中心前端联调发现

- 页面初次加载时，如果部门和用户候选尚未返回，用户仍可点击“发起事项”或“新建配置”，导致弹窗中的部门为空。已在按钮上增加加载门禁。
- 配置回滚恢复 WorkflowVersion 状态后，WorkflowDefinition 名称仍保留新版本名称。回滚逻辑已同步更新 Definition 名称，并加入 SQL/API 回归断言。
- 当前正式部署 Playwright 13/13 通过，新配置中心和员工事项入口已进入实际部署。

## 2026-11-01 第二轮多岗位试点发现

- 研发工程师能够处理配置生成的第一个流程任务。
- 项目经理能够看到分配给自己的 `MANAGER_REVIEW` 任务，但角色缺少 `workflow-task:approve/return/terminate`，无法处理。
- 平台管理员虽然拥有全部审批权限，但流程任务要求当前处理人，管理员代审会返回 `TASK_ASSIGNEE_MISMATCH`。
- 将经理节点映射给平台管理员兼岗后，研发、经理兼岗、质量三个节点完整通过，WorkflowInstance 达到 `Completed`。
- 流程实例完成后项目仍为 `Active`，项目验收/关闭需要显式动作，不应由任意流程实例自动代项目关闭。
- 真实项目经理审批是当前多岗位推广的 P0 阻断，不能把管理员兼岗试点说成项目经理岗位已验收。
- 前端 `/workflows` 的“我的待办”加载全部任务，没有按 `Pending/InProgress` 过滤；已 Approved 的任务仍显示且保留操作按钮。

## 2026-11-01 第 7 项验证结论

- 两轮多岗位试点的整体流程完成率为 50%，有效兼岗路径完成率为 100%。
- 流程任务整体完成率为 80%；失败实例恰好缺少真实项目经理审批这一环。
- `3157C` 的证据对象数量完整，但状态与证据引用存在不一致，不能把“有证据”与“状态正确”混为一谈。
- 自动关联覆盖率 80%，唯一失败点是文件上传后未自动形成项目证据关系。
- 缺少改造前人工工时、录入次数和错误率基线，因此当前不能给出可信的效率提升百分比。
- 生产化结论：Demo 可用、受控试点有条件可用、全员生产上线不可用。

## 2026-11-01 前端导航与 IAM 权限批次

- 冻结标签仍指向 `788963c` 的发布记录，实际发布源码证据为 `00a8098`；当前 `main@40c30d5` 在该基线后仅新增本批任务定义。
- 组织树复用现有 `Identity.Department`，没有建立第二套部门主数据。
- 导航采用工作区/版本/入口/个人偏好四层模型；公司根由现有 Organization 提供。
- 用户旧版 `UserRole` 保持兼容；新增 `UserRoleAssignment` 承载组织/部门范围。
- 范围字符串在 SQL 种子中为大写 GUID、请求中为小写 GUID；使用大小写敏感 C# 比较曾造成唯一索引冲突，现已统一为 OrdinalIgnoreCase 匹配。
- SQL Server 生成脚本中新增列回填需要跨 `GO` 批次；直接同批次会导致列不可见。正式脚本已显式分批。
- `project-manager` 补齐动作权限后，审批仍必须通过任务处理人、冻结候选资格和部门范围，不产生任意代审能力。
- 历史任务在角色成员移除后仍可由冻结候选完成，已由 SQL/API 用例验证。
- 系统“管理概览”入口改为 `identity-admin:Manage`，避免普通角色因已有 `platform:Read` 看到管理入口。
- 390px 导航曾因 flex 内容撑开文档宽度；通过限制 grid/sidebar/nav 宽度并保留内部横向滚动修复。
- 最终隔离库 104/104，源码 Playwright 15/15；事件/项目状态、文件自动关系、日期偏移和待办过滤继续保留。
- 干净合并提交 `0bd8547` 构建 `req-navigation-iam-final1`，manifest 无 `-dirty`。
- 正式部署 Playwright 15/15；应用回滚到 `req-config-center-final3` 并重新部署 final1 均健康。
- 部署前备份 `RESTORE VERIFYONLY` 与独立库恢复通过：9 迁移、57 应用表。

## 2026-11-01 前端导航与 IAM 收尾修复

- 业务角色候选 bug：department scope 曾接受组织内所有部门；已收敛到任务 `workflowDepartmentId`，organization scope 保持可用。
- IAM 越权 bug：项目经理曾可分配 organization scope、全量替换时撤销范围外成员，并可通过 `permission:assign` 单独修改角色权限。
- 修复后：成员查询、增加、移除和替换均校验部门；范围外成员保持不变；全局权限修改必须同时具备 `role:manage` 和 `permission:assign`，且不得超出操作者有效权限。
- legacy `UserRole` 曾不计入角色 memberCount 和停用检查；现与 scoped assignment 合并、用户去重、停用用户排除。
- Agent 节点曾把任务交给服务身份后无人可处理；现由 Worker 区分自动完成、人工确认和明确阻断，并关联 AgentRun/GatewayCall/任务动作/审计。
- Agent 服务身份自确认被拒绝；A0/A1 结果需真人确认；永久人工边界在 Worker 和确认接口双重阻断。
- 配置页原手工输入 AgentProfile GUID，现改为 `/api/v1/agents` 可搜索选择器。
- 高风险审核原无 reject 路由且意见可空；现通过/驳回均必填意见，申请人不可自审。
- 人员列表原只处理前 50 人；现服务端分页、跨全量搜索，55 人回归通过。
- 测试串行化是本批 SQL 集成稳定性的必要条件：多个治理用例会变更共享部门/角色状态，禁止 xUnit 并行执行避免测试互扰。
- 无结构变化，未创建 010；009 在恢复探针库完成 down/re-up，8/51 → 9/57。
- 全量隔离 SQL/API：112/112；前端 13/13；OpenAPI 114 路径/134 operationId；生产构建通过。
- 事件状态、项目状态、文件自动关系、日期偏移和已完成待办过滤仍保持原状。
- final2 发布包从干净合并提交 `d53fba3` 构建，manifest sourceRevision 为 `d53fba365b4c760b71e7212972eb3372649f2e81`，不含 `-dirty`。
- 正式部署 17/17 E2E；应用回滚 final2 → final1 → final2 通过。
- 部署备份恢复库验证 9 迁移、57 应用表及项目经理三动作权限。

## 2026-11-01 需求池批次 3 收尾

- 事件状态丢失根因是 `ApplyEventScope` 全量 `AsNoTracking`；现仅管理动作使用 tracking，列表/GET 保持只读优化。
- 项目状态丢失根因是管理读取先走 `AsNoTracking`；现复用原权限/范围判定后读取受跟踪主对象，并补 `BusinessObjectVersion`。
- 项目文件不显示根因是上传只写 `AttachmentLink`；现 PROJECT 上传同事务补 `ProjectRelation(Attachment, SupportingEvidence)`。
- 业务时区偏移根因是使用 `now.Offset`；现集中配置 IANA `Asia/Shanghai` 和 18 点截止小时。
- E2E 首次失败是 Element Plus 只读 input 被内部展示层拦截；增加 `project-status-select` 稳定测试标识，不改变实际交互。
- 隔离库 `AIDT_BATCH3_CLOSEOUT_FINAL_20261101_185000`：后端/SQL/API 115/115，0 跳过。
- 前端类型、13/13 Vitest、OpenAPI 114/134、生产构建和依赖审计通过。
- 源码桌面 + 390px E2E：18/18，0 failure、0 skipped、0 flaky。
- 修护脚本在隔离库将 24 条事件和 1 个项目状态异常归零；修护审计 1 条，幂等重跑稳定，重复附件关系 0。
- 本批未新增迁移；001–009 未修改，历史修护不覆盖审计。
- `project-manager` 的流程审批能力沿用导航/IAM 收尾结果，仍受本人任务、冻结候选和部门范围约束。
- 主库最终修护前备份 SHA256 `08AF9BE74009DD137ACBDBBB7AB963EF751066920329CE8E525A8A3BA0E368BE`，RESTORE VERIFYONLY 通过。
- 主库修护后事件、项目状态和附件关系异常均为 0；幂等复跑修护审计仍 1、重复关系 0。
- final3 发布包从干净合并提交 `d9ee050` 构建，SHA256 `EAD2F5D6135EE5BFB525EEE46B2F57191C528F8FAE38EB1023517D5C86FAB4F6`，manifest 无 `-dirty`。
- 正式部署 18/18 E2E；开始时间晚于部署时间；应用回滚 final3 → navigation-IAM final2 → final3 通过。
- 恢复库 `AIDT_REQ_BATCH3_CLOSEOUT_FINAL3_RESTORE_20261101_183818` 为 9 迁移、57 表、异常 0，试点项目为 Completed v3 / 100%。
- 构建时开发依赖有 5 个 moderate npm audit 提示，生产依赖为 0；未在本批升级测试/构建依赖。
