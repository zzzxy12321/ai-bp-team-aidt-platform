# 进度日志

## 会话：2026-11-05 UX-006 故障恢复与发布门禁

### 阶段 0：工作树恢复

- **状态：** complete
- 从原会话悬空 `write_stdin` 故障中切换到新会话，保留全部未提交修改。
- 当前分支 `feature/ux-006-column-introductions`，HEAD `d22e7d0`。
- 已读取目标提示词、L0 产品基线、前端/测试规范、功能地图、UX-005
  文档和 UX-006 变更三件套。
- 已核对 17 个栏目接入集中介绍目录、共享页面头和 `ux006-intro.spec.ts`。
- 已修复共享页面头环境语义、适用岗位内容标签和文档登记缺口。
- 完整源码回归发现并修复审计页按实体过滤只覆盖最近 100 条的问题；改为使用既有
  后端查询参数，不新增 API 或迁移。
- frontend typecheck、Vitest 72/72、构建、OpenAPI 188/209 通过。
- backend build/format、SQL/API + MCP 198/198 通过；npm 生产依赖和 NuGet 均无漏洞。
- 全量源码 Playwright：68 通过、0 失败、2 个部署专用用例按设计跳过。
- UX-006 专项 5/5 通过，17 个栏目及 320—1920 响应式截图已生成。

### 当前下一步

无。UX-006 已完成发布、回滚、恢复和冻结。

### 阶段 1：合并与源码发布

- **状态：** complete
- 功能提交 `7fc1be7` 非快进合并到 `main`，合并提交 `2d9ba09`。
- 第一次 `npm ci` 被遗留源码 Vite 进程占用 Rolldown 二进制阻断；
  仅停止对应源码测试进程后重试，发布构建通过。
- 发布包 `AIDT-req-ui-ux006-final1.zip`，SHA256
  `8025047A96557AD7C8792BB94ACFBDF4F03A1C0F6ED198B1639BF1EA16349324`。

### 阶段 2：8088 正式验收

- **状态：** complete_deployed_and_frozen
- 部署时间 `2026-11-05T13:47:14+08:00`，部署前备份和恢复校验通过。
- 主库累计数据首轮正式 E2E 为 65/70；按既定方案切换到冻结基线恢复库后
  正式全量 `70/70`，0 失败、0 跳过、0 flaky。
- 测试结束后切回主库，页面、API、Worker、AiGateway 和 MCP 均为 200。
- 应用回滚 `ux006 → ux005-final3 → ux006` 通过。
- 最终主库备份 SHA256
  `338CAF74469E8FD6D7A12F88F1DF77D251D4CF511C9C6630023E607D3E19786C`；
  独立恢复库 ONLINE、18 迁移、71 应用表。

## 会话：2026-11-05 UX-005 正式发布

### 阶段 2：正式发布、回滚与恢复

- **状态：** complete_deployed_and_frozen
- 从 `main@7f03623` 的干净源码构建 final1；部署后发现窄屏抽屉与导航加载完成竞态，
  修复并提交 `59f406d`，证据提交 `0654008`。
- final2 发布包 SHA256：
  `0B13C8B595E549DECC9D566D723A06DF6FDA4931A9CEFEB9F1E5E6F78EF6AD93`，
  manifest sourceRevision `0654008`，无 `-dirty`。
- 8088 正式全量 Playwright：64/64，0 失败、0 跳过、0 flaky；
  报告开始 `2026-11-05T02:32:27Z`。
- 最终主库切回后的 UX-005 关键烟测：10/10。
- final2 → final1 → final2 应用回滚通过；所有健康端点 200。
- 最终主库备份 SHA256
  `E482EF7FB8F47CD49DAC1B98C503B3D3D6575110D85D9CCE2B9628AADDA7ADE1`；
  独立恢复库 ONLINE、18 迁移、71 应用表、7 个部门默认工作区。

### 阶段 0：基线恢复

- **状态：** complete
- 已复核分支 `feature/ux-005-pms-workspace`、HEAD `4caa29f`、干净工作树。
- 已确认 `main@7e580f9`，8088 部署 `req-ui-step8-final3`，
  源码 `a86cccd`，最大迁移 `018`。
- 已完整读取产品/规范/UX-005 文档、目标提示词、部署与回滚脚本。
- 已确认 Chrome 与 Edge 可执行文件位置；现有 Playwright 仅配置 Edge。

### 当前下一步

非快进合并到 `main`，构建并部署 `req-ui-ux005-final1`。

### 阶段 1：正式评审与源码门禁

- **状态：** complete
- 新增 Chrome/Edge 双浏览器发布矩阵，覆盖 1920、1440、1366、1280、1024、
  820、600×518、390×844，以及 1366 的 100/125/150% 和 1920 的 100/125%。
- 听记汇总/筛选测试补齐权限、组织、部门、环境和匿名访问范围。
- backend build：0 警告、0 错误；`dotnet format --verify-no-changes`：通过。
- 后端/SQL/API：187/187；MCP：11/11。
- 前端 typecheck：通过；Vitest：30 文件、66/66；OpenAPI 188/209；生产构建通过。
- 源码 Playwright：63/63，0 失败、0 跳过、0 flaky。
- npm 生产依赖：0 漏洞；NuGet：无已知漏洞。

## 会话：2026-11-04 UX-005 目标模式实施

### 阶段 0：基线与恢复

- **状态：** complete
- 已读取 UX-005 目标提示词、变更计划、影响分析、上下文包、功能地图、
  L0 产品/规范基线、规划文件和现有前端实现。
- 已确认基线 `main@7e580f9`、部署 `req-ui-step8-final3`、最大迁移 `018`。
- 已创建 `feature/ux-005-pms-workspace`，保留设计登记阶段未提交文档。
- `planning-with-files` 的会话恢复脚本在当前 Windows 环境静默退出，退出码 1；
  已改用 Git 状态、规划文件和最近日志完成恢复核对。

### 当前下一步

进入 UX-005-B，抽取页面头、介绍抽屉、统计条和筛选栏基础组件。

### 阶段 B：统一工作台正文框架

- **状态：** complete
- 新增 `WorkbenchPageHeader`：标题、范围、主要操作和右上角“介绍”。
- 新增 `WorkbenchIntroDrawer`：约 420px 右侧抽屉，包含用途、使用时机、
  1～2 个业务例子、权限/数据/人工边界，并支持 Esc 与初始焦点。
- 新增 `WorkbenchStats`：接受实际提供的 1～5 项指标，不机械固定四卡。
- 新增 `WorkbenchFilterBar`：桌面常驻，移动端首行保留搜索、其余筛选进入面板。
- 全局正文宽度已由 A 阶段解除 `1260px`，B 不增加第二套页面宽度容器。
- 验证：基础组件 Vitest 4/4；TypeScript 通过。

### 阶段 C1：我的工作

- **状态：** complete
- 页面头改为“我的工作 / 当前部门范围 / 发起事项 / 介绍”。
- 四项统计使用服务端真实汇总：待我处理、今天到期、待我确认、人工边界。
- 增加关键词、部门、来源、状态、优先级、截止时间和重置；筛选消费完整聚合集。
- 分类为全部、待我处理、我发起、我参与、需要关注，分类数量随当前筛选变化。
- 桌面使用七列紧凑事项表；760px 以下自动切换为事项卡。
- 单页 50 条，保留今日焦点横条；快速入口进入“发起事项”下拉，
  最近证据进入筛选栏；长期右侧说明栏已删除。
- 人工边界、质量放行、付款、配方和设备控制提示保留在事项操作附近。
- 新增 `myWorkFilters`、`WorkItemTable`、`MyWorkPage` 测试，并更新原事项发起 E2E
  以适配下拉入口。
- 验证：相关 Vitest 9/9；C1 Playwright 3/3；原 1440/1280/390 流程待最终全量回归。

### 阶段 C2：AI 听记

- **状态：** complete
- 页面头改为“AI 听记 / 当前部门与环境 / 导入听记 / 介绍”。
- 四项统计来自新增只读汇总：听记总数、已分析、待确认行动项、本周新增。
- 增加标题/参会人/文字稿、部门、分析状态、行动项状态、来源类型和会议时间筛选；
  筛选与分页由服务端执行。
- 主体为来源栏 + 全宽详情栏，两侧固定高度并独立滚动；每页 20 条来源。
- 钉钉断连和真实模型未启用说明移入介绍抽屉与状态标签。
- 分析、行动项编辑、保存、统一确认和真人确认行为保持原路径。
- 新增 `MinutesSummaryDto`、`MinutesSourceQuery`、`GET /api/v1/minutes/summary`；
  未修改写接口、事务、权限、审计或数据库迁移。
- C2 专项 Playwright 3/3；只读 API 集成测试 1/1。

### 最终门禁

- **状态：** complete_pending_review
- `npm run typecheck` 通过。
- `npm run test`：30 文件、66/66 通过。
- `npm run build` 通过；仅有既有的大 chunk 提示，不是失败。
- `npm run contract:check`：188 路径、209 operationId。
- 源码 Playwright：62/62 通过。
- 部署入口 MCP OAuth：1/1 通过。
- 后端默认测试：118 通过、69 个 SQL 条件用例按设计跳过。
- 新增听记 API 集成测试：独立库 1/1 通过。
- 交付报告：`docs/project/UX_005_DELIVERY.md`。
- 以上为正式发布前的历史状态；后续已合并、部署 final2、回滚、恢复并冻结，
  续见本日志顶部“阶段 2”记录。

### 阶段 A：应用外壳与侧栏

- **状态：** complete
- `.app-layout` 固定为 `100dvh`；主列独立滚动，侧栏不再随正文离开视口。
- 侧栏改为浅色 AIDT 绿色工作台风格，保留约 `248px` 宽度、`38px` 菜单密度、
  分组色条和二级层级线。
- 品牌与底部操作固定，导航 `flex:1/min-height:0/overflow-y:auto`。
- 当前部门单开，当前菜单变化后自动滚入可视区域。
- `820px` 以下使用约 `280px` 左抽屉，支持遮罩、Esc 和焦点返回。
- 新增 `AppLayout.test.ts` 覆盖单开、当前菜单和抽屉焦点。
- 新增 `workspace-layout.spec.ts`，验证 1920/1280/600×518/390×844、
  正文滚动后侧栏位置、导航滚动、底部可达和无横向溢出。
- 验证：`npm run typecheck` 通过；`npm run test` 25 文件/50 用例通过；
  `npm run build` 通过；A 专项 Playwright 4/4。

## 会话：2026-11-04 UX-005 PMS 风格侧边栏与工作台正文设计整合

### 阶段 1：问题复现与参考检查

- **状态：** complete
- 已用 Computer Use 检查 AIDT 部门文件页和 Codex 内置浏览器侧栏。
- 已记录侧栏裁切、滚动后消失、窄屏超长导航和错误滚动上下文的实际尺寸证据。
- 已检查用户 PMS 侧边栏，确认独立滚动、紧凑菜单、分组色条和窄屏抽屉设计。
- 已对照 PMS 会议页正文、AIDT 我的工作和 AI 听记，确认页面宽度、说明文字、
  统计、搜索、筛选、分类和主内容布局差距。

### 阶段 2：需求与实施文档

- **状态：** complete
- 已登记 `IDEA-UX-005 / UX-005`，P1，approved。
- 已创建变更计划、影响分析和上下文包。
- 已更新功能地图和提示词索引。
- 已创建 `prompts/14-PMS风格侧边栏重构目标模式.md`。
- 已将 UX-005 扩展为 A 应用外壳/侧栏、B 统一工作台正文框架、
  C 首批我的工作与 AI 听记三个阶段。
- 本轮未修改前端源码、API、数据库、权限、审计或部署配置。

### 当前下一步

按整合后的目标模式提示词在独立功能分支实施；先补基线复现测试，再依次完成 A/B/C。

## 会话：2026-11-03 DEPT-WF-001 / 批次 8 部门工作空间与可视化流程设计器

### 阶段 1：基线与登记

- **状态：** complete
- 已核实仓库、`main@3e94f3a`、工作区干净、7/7A 冻结标签和 step8 标签缺失。
- 已确认当前部署基线 `req-ui-step7-final1`、最大迁移 017。
- 已读取产品目标、需求基线、技术架构、计划、需求池、全部规范、功能地图、
  权限矩阵、Mock 边界、测试证据和 5A—7B 关键交付。
- 已创建 `feature/ui-step8a-department-navigation`。
- 已登记 `IDEA-DEPT-WF-001 / DEPT-WF-001`，并建立变更计划、影响分析和上下文包。

### 阶段 2：实现与源码回归

- **状态：** complete
- 8A：测试导航 32 个退役、测试配置 319 个标记；7 个部门唯一默认工作区；
  会话授权指纹和旧权限重登提示完成。
- 8B：18 个节点/表单字段能力；任务冻结定义、Schema 版本和哈希；
  表单草稿、审批联动、受控 AI 规则审查完成。
- 8C：`/configurations` 左中右设计器、`/workflows` 冻结表单运行页完成。
- 8D：交付中心、研发、供应链、财务四部门路径和跨部门/人工边界通过。
- 018 纯 017 还原库完成 up/down/re-up：18/17/18 迁移，应用表保持 71。
- 后端/SQL/API 186/186；MCP 11/11；Vitest 47/47；
  OpenAPI 187/208；源码 E2E 52 通过、1 项部署专用跳过。

### 当前下一步

主库 018 已应用并核验；重新构建携带 `applied_verified` 状态的正式包并部署。

### 阶段 3：主库应用

- **状态：** complete
- 主库迁移前备份及 `RESTORE VERIFYONLY` 通过。
- 主库由 17/71 应用到 18/71。
- 7 个部门默认工作区、32 个测试导航退役、319 个测试配置标记。
- 测试配置发布态 0，交付中心和部门流程负责人 26 项权限存在。

### 阶段 4：正式发布、回滚与恢复

- **状态：** complete
- 首次 final1 登录经 Nginx 返回 502；修复权限 Cookie 体积和 Nginx 缓冲。
- final2 全量 E2E 后暴露测试导航替换正式财务默认工作区；代码和状态修复完成。
- final3 正式 E2E 53/53，0 失败、0 跳过、0 flaky。
- 应用回滚 `final3 → req-ui-step7-final1 → final3` 通过。
- 重部署后部门与场景专项 8/8，全部健康端点 200。
- 最终备份与独立恢复验证通过：18 迁移、71 表、7 个部门默认工作区、
  发布中测试默认入口 0。
- 正式包 SHA256 `81B5FC5CB1EAAC66D3E321B0270037FD6E342D2D77004926CA4A28BF48F61C2A`。
- 最终备份 SHA256 `9641532E40CF78CFB92676D2698090584C7775A57A4C303999419009242F8071`。

### 当前下一步

无。创建 `req-ui-step8-frozen-v1` 后工作区保持干净。

## 会话：2026-11-03 SCN-001 / 7B 岗位业务场景中心前端与试用闭环

### 阶段 1：前置门禁与登记

- **状态：** complete
- 已核对 `main@1b33a9f`、干净工作区、6-final1/7A 冻结标签。
- 已核对当前部署 `req-ui-step7a-final1`、健康端点、017、71 表和六场景后端。
- 已发现并登记“缺少当前用户启动列表”这一 7B 必要接缝。
- 已创建 `feature/ui-step7b-scenario-center-ui`。
- 已创建 7B 变更计划、影响分析和上下文包。

### 当前下一步

完成最终全量源码 E2E、提交合并、干净发布、正式部署、回滚和独立恢复。

### 阶段 2：前端与真实聚合

- **状态：** complete
- 新增 `GET /api/v1/scenarios/launches`，实时读取当前活动流程任务、项目任务、
  事件、对象、项目和 AgentRun。
- 新增 `/scenarios` 首页、搜索/部门/岗位/分类筛选、推荐、我的场景和业务卡片。
- 新增七步启动向导，真实上传文件、导入/选择听记、调用预检和启动 API。
- 新增场景配置治理，支持草稿、校验、发布、停用、恢复、版本和回滚。
- 配置中心、岗位工作台、我的工作和审计深链接入统一场景中心。
- 桌面 1440/1280 与移动 390px 专项 E2E 5/5。
- 配置中心到场景治理的生命周期 E2E 通过，结束状态主动停用以避免污染目录。

### 阶段 3：源码回归

- **状态：** complete
- 后端/SQL/API 181/181；MCP 11/11。
- Vitest 22 文件、45/45。
- OpenAPI 180 路径、201 operationId。
- 生产构建通过。
- 源码全量 E2E 49/49 通过，1 条部署专用 MCP OAuth 按设计跳过。
- 干净构建 `req-ui-step7-final1`，SHA256
  `19332AC8E38C5472D597E85DFE809C767CD458EC56EB7B9A97A3F26C219A8259`。
- 正式部署 E2E 50/50，0 失败、0 跳过、0 flaky。
- 应用回滚 `final1 → 7A → final1` 通过，所有健康端点 200，重部署场景专项 5/5。
- 最终备份 SHA256
  `0E1E2274D0C2823273A59994A0DB5E3752D37A12D3CC268B134D83F314B9A22A`。
- 独立恢复库为 ONLINE、17 迁移、71 应用表、6 基础场景、6 启动映射、
  7 发布版本、5 场景权限、13 场景审计。

## 会话：2026-11-03 SCN-001 / 7A 岗位场景后端

### 阶段 1：基线与登记

- 已读取目标文件、项目目标、需求基线、技术架构、计划、需求池、
  AI 工程宪法、上下文规范、现有规划、功能地图、权限矩阵和模块 README。
- 已确认 main、6A/6B 冻结标签、当前部署 6-Final1、最大迁移 016。
- 已从 main 创建 `feature/ui-step7a-scenario-catalog-backend`。
- 已在需求池登记 `IDEA-SCN-001` / `SCN-001`，P0，approved。
- 已建立 SCN-001-A 变更计划、影响分析和上下文包。
- 已实现 3 张场景表、完整生命周期、六类基础场景、启动编排、权限、
  幂等、高风险双人审核、OpenAPI 和前端 TypeScript 契约。
- 迁移 017 在最终隔离库完成 16→17→16→17；68→71→68→71。
- 主库已应用 017：17 条迁移、71 张表、6 个场景、5 项权限、1 个财务人工流程。
- 后端/SQL/API 181/181；MCP 11/11；Vitest 41/41；OpenAPI 179/200；
  生产构建、依赖审计和源码 E2E 44/44 + 1 skip 通过。
- 功能提交 `90c5046`，非快进合并 `6e772ba`；从干净 main 构建并部署
  `req-ui-step7a-final1`。
- 发布包 SHA256 `5BD498BA71395137AB48FE3FDE2C24222D1E1BD824F18EA930FB32568858A3D7`。
- 正式 Nginx Playwright 45/45；管理员可见 6 个场景，部署环境场景启动和幂等重放通过。
- 应用回滚 final1 → req-ui-step6-final1 → final1 通过。
- 最终备份 SHA256 `A883E99E195562BC8E5A4372636006AA3537910E26D7F73889586BF5C8536558`；
  独立恢复库验证 17 迁移、71 表、6 场景、6 个有效快照。
- 已创建 `req-ui-step7a-frozen-v1`，7A 完成。

## 会话：2026-11-03 AGT-003 / 6B 数字员工身份中心前端与业务联动

### 阶段 1：前置门禁与上下文

- **状态：** complete
- 已核对干净 `main@337dd51`、5-final/6A 冻结、当前部署和 015 迁移。
- 已读取 6A 交付、现有 Agent 页面、配置中心、流程策略、MCP 中心和前端契约。
- 已创建 `feature/ui-step6b-agent-identity-ui`。
- 已创建 6B 变更计划、上下文包和影响分析。

### 阶段 2：后端聚合与前端实现

- **状态：** complete
- 已新增 Agent options/context 契约，补充最近运行和 MCP 身份可用状态。
- 已完成身份中心、七段编辑表单、业务语言映射、运行证据和 MCP 联动。
- 016 在隔离库完成 15/68 → 16/68 → 15/68 → 16/68，并在主库应用。
- 后端/SQL/API 172/172、MCP 11/11、Vitest 41/41、OpenAPI 162/183。
- 源码 Playwright 44 通过、1 个部署专用 MCP OAuth 跳过、0 失败。

### 阶段 3：发布与正式验收

- **状态：** complete
- 主库 016 已应用：16 迁移、68 表、3 项新增 `agent-read`、6 个未绑定服务账号。
- 功能提交 `fb20bfb`；发布包从干净提交构建，manifest 无 `-dirty`。
- 正式部署 `req-ui-step6-final1`，正式 E2E 45/45。
- 应用回滚 `final1 → req-ui-step6a-final1 → final1` 通过，所有健康检查 200。
- 最终备份 SHA256 `8B6B4C505CC359CB9D04A2E970FC20D0F1C068A5F55F33D6312A0471B2379F85`。
- 独立恢复库 `AIDT_AGT003_B_FINAL1_RESTORE_20261103_1214`：ONLINE、16 迁移、
  68 表、5 个发布身份、790 条 AgentRun、3 项新增授权、6 个服务账号。
- 已创建冻结标签 `req-ui-step6-frozen-v1`。

## 会话：2026-11-03 AGT-003 / 6A 数字员工身份治理后端

### 阶段 1：基线与上下文

- **状态：** complete
- 已读取目标文件、AGENTS、五份产品基线、工程宪法、上下文恢复规范和
  后端/数据库/API/安全/测试/Git/部署/功能地图/完成定义规范。
- 已核对 `main@dc0c945`、工作区干净、5A/5 冻结标签和
  `req-ui-step5-final1` 部署基线。
- 已核对最大迁移 014、67 张应用表，以及 AgentProfile、AgentRun、
  Workflow Agent 节点、MCP 令牌、IAM 双人审核和 Audit/Outbox 实现。
- 已创建功能分支 `feature/ui-step6a-agent-profile-backend`。
- 已创建 AGT-003 变更计划、影响分析和上下文包。
- 已修正 `CHANGE_PLAN_META_001_B.md` 状态及完成证据。

### 阶段 2：实现与源码验证

- **状态：** complete
- 已实现身份主数据、不可变版本、完整生命周期、治理权限、双人审核、幂等/并发。
- 已打通 AgentRun、工作流节点、MCP 服务令牌、能力白名单和安全拒绝审计。
- 新增 015；隔离库完成 up/down/re-up，主库已应用 15/68。
- 后端/SQL/API 171/171；MCP 11/11；Vitest 35/35；OpenAPI 160/181。
- 源码 Playwright 40 通过、1 个部署专用 MCP OAuth 跳过、0 失败。

### 阶段 3：提交、构建、部署与冻结

- **状态：** complete
- 功能分支已提交；发布包从干净提交构建，manifest 无 `-dirty`。
- 正式 E2E 41/41，0 失败、0 跳过、0 flaky。
- 发布包 SHA256 `32E201A25804D9B5876771D197CEE38ABFD992FF9BFCFA313CC69C8931264456`。
- 应用回滚 `final1 → req-ui-step5-final1 → final1` 通过，所有健康检查 200。
- 最终备份 SHA256 `91C583FCC1D9D0D373A863F5B363A192BA9D35EA7E5264565102F8404596CF5F`。
- 独立恢复库 `AIDT_AGT003_A_FINAL1_RESTORE_20261103_1017`：ONLINE、15 迁移、
  68 表、3 个有效发布身份、750 条历史 AgentRun。
- 已创建冻结标签 `req-ui-step6a-frozen-v1`。

## 会话：2026-11-02 META-001-B 业务对象建模与动态表单前端

### 阶段 1：基线与上下文
- **状态：** complete
- 已核对干净 `main@fccadfc`、5A 冻结标签和当前部署。
- 已创建 `feature/ui-step5b-object-modeling-ui`。
- 已登记需求，创建变更计划、上下文包和影响分析。
- 已确认 5A API、现有对象页、流程配置和岗位卡片复用边界。

### 阶段 2：实现与源码验证
- **状态：** complete
- 已实现建模工作室、字段设计器、动态九类表单、对象详情/编辑/版本/四类关联。
- 已打通流程配置和岗位工作台发布类型选择。
- 新增 014 普通业务角色对象更新授权，隔离库 up/down/re-up 通过。
- 后端/SQL/API 163/163、MCP 11/11、Vitest 35/35。
- 源码 E2E 40 通过、1 个部署专用 MCP OAuth 跳过、0 失败。

### 阶段 3：合并、发布与正式验收
- **状态：** complete
- 功能分支已非快进合并；干净构建 `req-ui-step5-final1`。
- 发布包 SHA256 `384291D1F053BCDAD5B831F82A3EEC5C03F019E9C55E0F8BFE40D29660721941`。
- 正式 E2E 41/41，0 失败、0 跳过、0 flaky。
- 应用回滚 `final1 → req-ui-step5a-final1 → final1` 通过。
- 最终备份 SHA256 `0D3762398F19C6FBBAA940692EC42FA729FDA15AC56C500350E91B8D1FCC2114`。
- 独立恢复库 14 迁移、67 表、6 个业务更新授权、4 个动态模型版本。
- 已冻结 `req-ui-step5-frozen-v1`。

## 会话：2026-11-02 META-001-A 业务对象建模与字段编辑后端

### 阶段 1：基线与上下文
- **状态：** complete
- 已核对干净 `main@9b425a3`、前序冻结 `req-ui-step4-frozen-v1` 和当前部署。
- 已创建 `feature/ui-step5a-object-modeling-backend`。
- 已登记 `IDEA-META-001 / META-001-A`。
- 已创建变更计划、上下文包、影响分析并更新规划文件。
- 已精读对象控制器、领域实体、上下文映射、仓储、查询、权限、契约与现有测试。

### 阶段 2：实现与源码验证
- **状态：** complete
- 已实现对象类型生命周期、九类字段属性、版本快照、建模权限和业务对象编辑。
- 新增 013；隔离库 up/down/re-up 和主库应用通过。
- 后端/SQL/API 163/163，MCP 11/11，合计 174/174。
- Vitest 28/28；OpenAPI 142/163；生产构建和源码 E2E 通过。
- 源码 E2E：37 通过、1 个部署专用 MCP OAuth 用例跳过、0 失败。

### 阶段 3：合并、发布与正式验收
- **状态：** complete
- 非快进合并功能分支；干净构建 `req-ui-step5a-final1`。
- 发布包 SHA256 `8F66CF0090C9F8C43AFDB14455B391E9438CDAD189024242C3E697D743C3DB06`。
- 正式 E2E 38/38，0 失败、0 跳过、0 flaky。
- 应用回滚 `final1 → req-ui-step4-final1 → final1` 通过。
- 最终备份 SHA256 `673AA522C4C2F2BCA65EA7C2D9006F1456E3B48935EFA839D485C7A1DAD7C2EB`，
  独立恢复库 13 迁移、67 表、11 个版本快照。
- 交付完成后在本证据提交创建 `req-ui-step5a-frozen-v1`。

## 会话：2026-11-02 WS-002 可配置岗位工作台模板

### 阶段 1：基线与上下文
- **状态：** complete
- 已核对 `main@67fa2a8`、干净工作区、前序冻结和当前部署。
- 已创建 `feature/ui-step4-role-workbench-templates`。
- 已登记需求池、变更计划、上下文包、影响分析和权限矩阵 WS-002 初始口径。
- 正在精读部门工作区、导航、IAM、项目与来源权限实现。

### 阶段 2：实现与源码验证
- **状态：** complete
- 新增 Workspace 4 表、012 迁移、6 个系统模板、31 张真实卡片和 3 个模板权限。
- 实现服务端模板匹配、卡片聚合、草稿/发布/停用/恢复、IAM 配置和部门工作台重构。
- 隔离库 11 → 12 → 11 → 12 迁移通过。
- 后端/SQL/API 138/138、MCP 11/11；Vitest 15 文件/28 用例。
- OpenAPI 136 路径/157 operationId；岗位工作台源码 E2E 5/5。
- 干净最终库源码全量 E2E：36 通过、1 个部署专用 MCP 用例跳过、0 失败。
- 提交 `5e8a040`，非快进合并 `31c189c`。
- 首次 `final1` manifest 漏列 012；无标签包已保留为 premanifest 证据，修正迁移状态后重建正式包。
- 正式包 SHA256 `26F3E66B...613435`，manifest 12 条迁移且不含 `-dirty`。
- 正式 E2E 37/37，0 失败、0 跳过、0 flaky。
- 应用回滚 `final1 → req-ui-step3-final1 → final1` 通过。
- 部署备份恢复到 `AIDT_UI_STEP4_FINAL1_RESTORE_20261102_1536`：
  12 迁移、66 表、6 模板、31 卡片、3 权限。
- 页面、API、Worker、AiGateway、MCP 健康检查均 200。

### 计划阶段

1. 数据模型与 012 迁移
2. 模板解析与卡片聚合 API
3. 模板配置/版本/发布/停用/恢复
4. 部门工作区与 IAM 页面
5. 单元、SQL/API、Vitest、E2E
6. 隔离库迁移、主库应用、构建部署
7. 回滚、恢复、交付文档和冻结

## 会话：2026-11-02 PMO-003 项目操作闭环

### 基线与计划

- 已确认 `main@fa1a6d4`、工作区干净、前序冻结 `req-ui-step2-frozen-v1` 可访问。
- 当前部署 `req-ui-step2-final1`；数据库 11 条迁移、62 张应用表。
- 已创建 `feature/ui-step3-project-operations`。
- 已建立 `CHANGE_PLAN_PMO_003.md`、`CONTEXT_PACK_PMO_003.md`、
  `IMPACT_ANALYSIS_PMO_003.md`。

### 实现与源码验证

- 新增 `POST /api/v1/projects/{id}/tasks`、`CreateProjectTaskRequest` 和幂等项目任务写入。
- 项目编辑补齐 `BusinessObjectVersion` 快照；负责人变更继续收敛唯一 Lead。
- 项目详情增加编辑、负责人、成员角色/移除、任务创建和文件/听记/流程/对象证据操作。
- 状态选择器按领域规则展示合法流转；完成项目继续阻断未完成任务。
- 测试前备份 `AIDT_PMO_003_PRE_TEST_20261102_124500.bak`，
  SHA256 `2D7B373F8DF40FBF81F8A5194F66EB17EAB8B47EB3A5EEDD5764A059701B3B6A`。
- 隔离库 `AIDT_PMO_003_TEST_20261102_124500` 恢复验证为 11 迁移、62 应用表。
- 后端/SQL/API 与 MCP：146/146，0 失败、0 跳过。
- 前端类型检查、Vitest 14 文件/27 用例、OpenAPI 126/146 通过。
- PMO-003 Playwright 桌面/1280/390px：3/3 通过。
- 全量源码回归发现既有 MCP 用例误连默认部署库；本地源码模式已改为使用隔离 MCP
  源服务；最终完整回归 31 通过、1 条部署专用用例按设计跳过、0 失败。
- 功能地图、需求池、前后端 README 和交付报告已更新。
- 功能提交 `cd81f0e`、`2f85e7c`、`01df3dd`；非快进合并 `e8db2b2`。
- 从干净合并提交构建 `req-ui-step3-final1`，发布包 SHA256
  `B50F0A19AF78EC2EAE4EAE4C811E6F482B92B80445E565F4CCB7F2502DA04541`。
- 首次和最终正式部署 E2E 均为 32/32，0 失败、0 跳过、0 flaky。
- 应用回滚 `final1 → req-ui-step2-final1 → final1` 通过。
- 最终部署备份 SHA256
  `C964476E04205F814967B9CA0DB2967F5039BBCF8291127CAF0CC5AA0601FEFD`；
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库 `AIDT_UI_STEP3_FINAL1_RESTORE_20261102_1335` 为 ONLINE、
  11 迁移、62 应用表，任务创建审计和项目关系可恢复。
- 创建冻结标签 `req-ui-step3-frozen-v1`；PMO-003 完成部署与冻结。

## 会话：2026-11-02 UX-004-B 统一确认中心

### 基线与计划

- 已确认 `main@48a1a10`、工作区干净、`req-ui-step2a-frozen-v1` 可访问。
- 当前部署仍为 `req-ui-step2a-final1`。
- 已创建 `feature/ui-step2b-confirmation-center`。
- 已建立 `CHANGE_PLAN_UX_004_B.md`。
- 当前实现边界：统一页组合现有来源；听记增加项目/对象/流程持久化；MCP 增加业务视图。
- 已完成迁移 011、听记上下文与候选接口、MCP 业务视图及统一确认页。
- 已完成“我的工作”四类待确认入口改造和原详情页兼容。
- 隔离库迁移 up/down/re-up：11 迁移、62 表。
- 后端/SQL/API 134/134；MCP 11/11；合计 145/145。
- 前端类型、Vitest 23/23、OpenAPI 125/145、生产构建通过。
- 源码 Playwright：28 通过、1 个部署专用 MCP 用例跳过、0 失败、0 flaky。
- 功能提交 `95ddeb3`、`f69cf15`、`8d3afbd`；非快进合并 `931f953`。
- 从干净合并提交构建 `req-ui-step2-final1`，发布包 SHA256
  `4832A05B0350D79E3C33E2438B89FE980A979D5D6A4508ED1F3D3AFD50F2C189`。
- 首次正式部署 E2E：29/29，0 失败、0 跳过、0 flaky。
- 应用回滚 `final1 → req-ui-step2a-final1 → final1` 通过；重新部署后再次
  正式 E2E 29/29，最终报告晚于最终部署时间。
- 最终部署备份 SHA256
  `0DB5198B0F3B5581FF128ADF5F56109E0E4E0DD5D6A05E6B620F2373B1728C4C`；
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库 `AIDT_UI_STEP2_FINAL1_RESTORE_20261102_1220` 为 ONLINE、
  11 迁移、62 应用表，导航入口与听记上下文字段完整。
- 创建冻结标签 `req-ui-step2-frozen-v1`；UX-004 完成部署与冻结。

## 会话：2026-11-02 UX-004-A 事件确认与流程发起

### 基线与计划

- 已确认工作区干净，`main`、HEAD 和 `62052fb` 指向同一提交。
- 已确认前序标签 `req-ui-step1-frozen-v1` 存在。
- 已将 `CHANGE_PLAN_UX_003.md` 状态修正为 `COMPLETE_DEPLOYED_AND_FROZEN`。
- 已在需求池登记 `IDEA-UX-004 / UX-004`，明确 2A 只覆盖事件确认与流程发起。
- 已创建 `feature/ui-step2a-event-confirmation`。
- 已读取本段指定上下文并建立 `CHANGE_PLAN_UX_004_A.md`。
- 当前实现边界：无新表；Application Handler 统一事务；复用流程启动和事件确认规则。
- 已完成后端可编辑确认、已有/新流程选择、影响摘要审计、状态条件抢占和单事务回滚。
- 已完成前端表单、人员/项目/流程业务名称、上海时区和确认前影响预览。
- 后端 SQL/API 与 MCP：143/143，0 失败、0 跳过。
- 前端类型、Vitest 20/20、OpenAPI 123/143 和生产构建通过。
- 源码 Playwright：25 通过、1 个部署专用 MCP 用例跳过、0 失败、0 flaky。
- 功能分支提交 `a57848e`、`24fc249`、`561e8cb`；非快进合并 `ed78d48`。
- 从干净合并提交构建 `req-ui-step2a-final1`，包 SHA256
  `C0179C5618C67F95BABB49A0254F0F9FD69A1D1DF2617AB4DDC5252C020CB553`。
- 正式部署 Playwright：26/26，0 失败、0 跳过、0 flaky。
- 应用回滚 `final1 → req-ui-step1-final1 → final1` 通过。
- 最终备份 SHA256 `3DA905C968B42974B351FBF97E3DB517E455F303671F1F686A339658C104A0E1`；
  恢复库 `AIDT_UX004_A_FINAL1_RESTORE_20261102_093323` 为 10 迁移、62 表。
- 创建冻结标签 `req-ui-step2a-frozen-v1`。

## 会话：2026-11-01 UX-003 统一工作事项中心

### 启动与基线

- 已核对 `git status` 干净、当前 `main@6a378f5`，前序冻结标签存在。
- 已切回 `main` 并创建 `feature/ui-step1-unified-work-center`。
- 已读取 AGENTS、产品目标、需求基线、技术栈、计划、需求池、功能地图、权限矩阵、
  MCP 交付和相关 standards。
- 已在需求池登记 `IDEA-UX-003 / UX-003`。

### 当前执行

- 已完成统一只读 `WorkItemView`、六类来源聚合、详情深链和三端响应式页面。
- 已完成单元、SQL/API、MCP、前端类型、Vitest、OpenAPI、生产构建和源码 Playwright。

### 当前下一步

- 提交功能分支，非快进合并到 `main`。
- 从干净合并提交构建 `req-ui-step1-final1`，完成正式部署 E2E、应用回滚和数据库恢复验证。

### 源码验收结果（2026-11-02）

- 后端构建：0 警告、0 错误。
- SQL/API：127/127；MCP：11/11；0 失败、0 跳过。
- 后端格式：通过；NuGet 漏洞审计：无已知漏洞。
- 前端类型检查：通过；Vitest：11 个文件、16/16。
- OpenAPI：122 路径、142 operationId；生产构建：通过；npm 生产依赖：0 漏洞。
- 源码 Playwright：22 通过、1 跳过、0 失败、0 flaky；跳过项为部署专用 MCP OAuth。
- 桌面 1440、紧凑桌面 1280、移动 390 均纳入本轮验收。

### 正式交付（2026-11-02）

- 功能分支提交 `a7b2356`，非快进合并提交 `cf9ad3a`，`main` 构建前干净。
- 发布包 `AIDT-req-ui-step1-final1.zip` SHA256
  `B38C51ED44EC9E40719A3DC5B65A0212AC47F1AF3B8CE1D5F1C533E498834D4E`，
  manifest `sourceRevision=cf9ad3a4f53008f6a122f55992bea04baa72818d`，无 `-dirty`。
- 正式部署 E2E：23/23，0 失败、0 跳过、0 flaky。
- 页面、API live/ready、Worker、AiGateway、MCP live/ready、capabilities 全部 200。
- 应用回滚 `final1 → req-mcp-final5 → final1` 通过，最终版本为 `req-ui-step1-final1`。
- 部署备份 `RESTORE VERIFYONLY` 通过，并恢复到独立库
  `AIDT_UI_STEP1_FINAL1_RESTORE_20261102_003700`：ONLINE、10 迁移、62 应用表、5 张 MCP 表。
- 创建冻结标签 `req-ui-step1-frozen-v1`；历史标签和发布包未移动。

## 会话：2026-11-01 AIDT 标准 MCP 服务

### 恢复与基线

- 已读取 AGENTS、产品目标、需求基线、冻结技术栈、实施计划、MCP 需求、功能地图、权限矩阵、交付基线、API/OpenAPI、规划文件和相关规范。
- 已确认 `main@509e62f`、部署 `req-batch3-final3`、标签 `req-batch3-closeout-frozen-v1`。
- 已确认需求池仅有 `IDEA-MCP-001 / MCP-001` 未提交新增内容。
- 已检查 Agent、IAM、项目、事件、流程、文件、导航、审计代码入口。
- 已确认官方 MCP C# SDK 稳定版 `2.3.0`，支持 stdio、Streamable HTTP、Resources 和认证元数据。

### 当前执行

- 已完成 MCP 架构、OAuth 2.1/PKCE、工具作用域、服务身份令牌、真人确认和审计。
- 已完成 ADR、变更计划、上下文包、影响分析、Schema、DBA 审核和 API/功能地图更新。
- 新增 `AIDT.Mcp`：官方 SDK 2.3.0、Streamable HTTP、stdio、26 个业务工具和 6 类 Resources。
- 迁移 010 已在隔离库完成 62→57→62 表、10→9→10 迁移的 up/down/re-up，并应用到主库。
- 后端/SQL/API 116/116；MCP 协议/stdio/权限/审计 11/11；均 0 跳过。
- 前端类型检查、10 个文件 14/14、OpenAPI 122 路径/142 operationId、生产构建通过。
- 源码桌面 + 390px Playwright：18/18 通过，部署专用 MCP OAuth 用例 1 个按设计跳过。
- NuGet 与 npm 生产依赖无已知漏洞；MCP SDK `2.3.0` 已集中锁版本。
- `req-mcp-final1` 首次部署后健康检查均通过，但 MCP challenge 发现端口丢失；
  已修复 Nginx Host/X-Forwarded-Host，需重建 `final2` 后重新冒烟。
- `req-mcp-final2` 正式 OAUTH E2E 的客户端注册返回 400；根因为标准 snake_case
  注册字段未绑定。已补 JSON 字段映射并需要重新构建发布包。
- 全新 worktree 首次构建 `final3` 因缺少 restore 资产失败，同时暴露发布脚本未检查
  原生命令退出码；该包不完整且不得使用。已增加 publish/npm 失败门禁。

### 下一步

- 无；`MCP-001` 已完成、部署并冻结。

### 最终交付

- 正式发布：`req-mcp-final5`，包 SHA256 `B949F1D218A7539F8BBA9FC53372B14A92382FD29ADA229D0C1E14F0F1F68FD5`。
- manifest sourceRevision：`d0d816d38cc6a1ef8ccc27a3de3761c2a6c83594`，不含 `-dirty`。
- 正式 Playwright：19/19，0 失败、0 跳过、0 flaky。
- 部署后 stdio 服务身份调用：initialize、tools/list、whoami 全部通过。
- 应用回滚：`final5 → final4 → final5` 通过。
- 数据库恢复库：`AIDT_MCP_FINAL5_RESTORE_20261101_211843`，10 迁移、62 表、Mcp 5 表。
- 最终冻结标签：`req-mcp-frozen-v1`。

## 会话：2026-10-30 规范体系生成

### 已完成

- 读取参考图片并提取 12 类企业开发规范。
- 应用 `planning-with-files-zh` 的磁盘工作记忆模式。
- 确认用户核心痛点是 AI 上下文有限、注意力分散、修 Bug 回归和维护困难。
- 确认技术栈已冻结。
- 确认项目最高目标是甲方可直接复制、部署、接手和长期维护。

### 正在执行

- 无；V1 规范体系已完成。

### 下一步

1. 生成规范主索引和 AI 工程宪法。
2. 生成架构、后端、前端、数据库、API 和安全规范。
3. 生成测试、Git、评审、发布和 Bug 修复流程。
4. 生成 AGENTS、模板和提示词。
5. 同步更新一级 README 与计划方案。

以上 1—5 已完成，进入最终完整性校验。

### 验证结果

- 预期文件：34 个。
- 实际文件：34 个。
- Markdown 相对链接：全部通过。
- 冻结技术栈冲突检查：仅存在“禁止使用”说明，无实际技术路线冲突。
- 参考图片中的 12 类规范：全部映射。
- 额外规范与工作流：功能地图、上下文恢复、Bug 修复、重构、离线部署、DoD。

### V1 交付

- 根入口：`AGENTS.md`、`CLAUDE.md`
- 规范：`standards/00`—`14`
- 模板：`templates/`
- 提示词：`prompts/00`—`06`
- 项目索引：`docs/project/FEATURE_MAP.md`
- 运行时计划：`task_plan.md`、`findings.md`、`progress.md`

## 2026-10-30 冻结与 SQL 分批准备

- 需求文档冻结为 V1.1。
- 技术栈与架构文档保持冻结 V2.0。
- 实施计划冻结为 V1.2。
- 已检查 SQL Server 环境：安装日志错误码 3010，要求重启；无 MSSQL 服务、1433 未监听、sqlcmd 未找到。
- 已生成 `prompts/07-SQL重启阻断下的分阶段搭建.md`。
- 提示词已补充目标模式执行协议：直接执行、不等待重复确认、不把阶段 A 当成整个目标完成、重启后复跑同一提示词继续阶段 B。
- 阶段 A 可继续构建工程、契约、代码、单元测试和前端。
- 阶段 B 等待公司电脑重启和 SQL Server 可用后继续。

## 2026-10-30 阶段 A 执行

### 已完成

- 完整读取 `AGENTS.md`、项目目标、冻结需求、冻结技术栈、冻结计划。
- 完整读取任务书要求的规范目录、架构/后端/前端/数据库/API/安全/测试/部署/工作流/完成定义。
- 读取功能地图、`task_plan.md`、`findings.md`、`progress.md`。
- 检查环境：`.NET SDK 10.0.403`、ASP.NET Core Runtime `10.0.14`、Node `24.18.0`、npm `11.17.0`。
- 确认 `coding/` 不是 Git 仓库，当前没有正式代码骨架。
- 建立阶段 A 范围、影响分析、上下文包和 SQL 阻断记录。

### 正在执行

- 无；阶段 A 已完成。

### 阶段完成记录

#### A1 环境与计划

- 完成全部指定基线材料读取。
- 创建变更计划和上下文包。
- 登记 SQL Server 3010 外部阻断。

#### A2 后端工程

- 创建 `AIDT.Api`、`Application`、`Domain`、`Infrastructure`、`Workflow`、`Integrations`、`Worker`。
- 建立 `global.json`、集中 NuGet 版本、统一构建属性、EditorConfig 和解决方案。
- 实现配置校验、统一响应、全局异常、TraceId、健康检查和 OpenAPI 服务。

#### A3 前端工程

- 创建 Vue 3 + TypeScript + Vite + Element Plus + Pinia + Vue Router。
- 完成工作台、流程、业务对象、审计、Agent 页面骨架。
- 建立统一 HTTP client、平台状态 store、身份/权限路由接缝和错误状态。

#### A4 领域与契约

- 完成业务对象、流程任务、审计、身份权限、Agent 和 Outbox 模型。
- 完成三类 Application 用例及端口。
- 完成 OpenAPI 初稿和前后端 DTO 契约。

#### A5 数据库

- 编写 EF Core `InitialSchema` 和 ModelSnapshot。
- 生成幂等升级 SQL 与回滚 SQL。
- 覆盖 9 个 Schema、34 张应用表。
- 状态固定为 `authored_not_applied`。
- 未连接 SQL Server、未执行迁移。

#### A6 测试与构建

- `dotnet build AIDT.slnx --no-restore`：成功，0 警告、0 错误。
- `dotnet test AIDT.slnx --no-build`：28 通过、0 失败、0 跳过。
- `dotnet format AIDT.slnx --verify-no-changes --no-restore`：通过。
- `dotnet list AIDT.slnx package --vulnerable --include-transitive`：无已知漏洞。
- `npm ci`：成功，`npm audit --omit=dev` 无漏洞。
- `npm run typecheck`：通过。
- `npm run test`：3 通过、0 失败。
- `npm run contract:check`：22 路径、26 operationId 通过。
- `npm run build`：生产构建成功，主 JS 约 155.26 kB，无大包警告。
- Markdown 相对链接检查：全部有效。

#### A7 文档与自审

- 更新 7 个后端模块 README、前端 README、测试 README。
- 增加 API 契约、Schema、迁移、DBA、备份、启动和 SQL 阻断文档。
- 更新功能地图至 V0.2。
- 完成架构、行为、交付三视角自审。

### 当前状态

`PHASE_A_COMPLETE_WAITING_SQL`

整个 AIDT 项目：未完成。

## 2026-10-31 需求池批次 1 阶段 B

### SQL 第一检查与恢复点

- `MSSQLSERVER` Running / Automatic；TCP 1433 IPv4/IPv6 Listen。
- SQL Server 2025 Express `17.0.1000.7`；SQLCMD ODBC 18 可用。
- 主库 `AIDT` 为 35 张应用表，迁移历史停在 002。
- 新备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH1_PRE_003_20261031_013442.bak`。
- SHA256：`7C46A4C5BFAECB1FD0EBE76EB4D8D0380A526F02F6DAA6AA1A60C38F093CB784`。
- 备份 `RESTORE VERIFYONLY` 通过。
- 已安全停止 `demo-final3` 的 API、Worker、AiGateway 和 Nginx。

### 隔离迁移验证

- 从新备份恢复 `AIDT_REQ_BATCH1_TEST_20261031_013442`。
- 第 1 次 up 暴露新增列与回填同批次编译问题；事务回滚，测试库保持 35 表。
- 第 2 次 up 暴露筛选索引 `QUOTED_IDENTIFIER` 问题；事务回滚。
- 修复发布脚本后第 3 次 up 成功：38 表、45 外键、26 检查约束。
- 回填校验：6 部门、8 成员、8 部门 DataScope、7 部门/文件权限、0 非法对象/流程/任务。
- 003 down 成功恢复 35 表并清理 003 历史、部门/文件权限和 department DataScope。
- 已重新执行 003 up，准备集成测试。

### 下一步

下一次运行同一提示词时，第一项检查为公司电脑 `MSSQL*` Windows 服务状态；服务、1433 和应用连接均通过后才进入阶段 B。

## 补充：SQL BLOCKED

- 原因：SQL Server 安装要求重启，错误码 3010。
- 已检查：服务、1433、sqlcmd。
- 未执行：迁移、集成测试、数据库端到端验证。
- 继续条件：公司电脑重启后 SQL Server 服务可用，1433 监听，应用连接成功。

## 2026-10-30 阶段 B 续跑提示词

- 已核对最新目标模式阶段 A 对话：状态为 `PHASE_A_COMPLETE_WAITING_SQL`，并有完整测试和交付证据。
- 已核对较早的同名对话：只是未开启目标模式的误跑，未形成阶段 A 成果，可忽略。
- 当前仍未发现 `MSSQL*` 服务，1433 没有真实 SQL Server Listen。
- 已生成 `prompts/08-阶段B-SQL恢复后继续.md`。
- 阶段 B 提示词要求先检查 SQL 服务与监听，未通过时只记录阻断，不修改无关代码。
- 已生成 `prompts/09-AIDT全阶段目标模式.md`，提供单个目标模式提示词直接执行到首个可交付 Demo。

## 2026-10-30 SQL Server 未恢复诊断

- 系统已于 08:04 重启，Pending Reboot 标记均已清除。
- 重启后没有 SQL Server 安装进程或新增安装日志。
- 当前没有实例服务、实例注册和 1433 Listen。
- 根因不是服务启动失败，而是安装程序没有在重启后自动继续。
- 已找到解压后的安装入口 `C:\SQL2025\Express_ENU\SETUP.EXE`。
- 已把恢复步骤、检查命令和 Express 版本限制写入 `docs/004-交付说明/SQL_BLOCKED.md`。

## 2026-10-30 SQL Server 恢复

- 使用 `C:\SQL2025\Express_ENU\SETUP.EXE` 执行静默安装。
- 安装退出码：`0`。
- 服务 `MSSQLSERVER`：Running，Automatic。
- 默认实例：`MSSQLSERVER`。
- 版本：`17.0.1000.7`，SQL Server 2025 Express。
- TCP `1433`：正在监听。
- Windows 身份验证连接 `master`：成功。
- `AIDT` 业务数据库尚未创建。
- 项目状态更新为 `SQL_READY_FOR_PHASE_B`。

## 2026-10-30 AIDT 全阶段目标模式启动

- 完整读取 `prompts/09-AIDT全阶段目标模式.md`。
- 重新读取全部冻结基线、14 份开发规范、阶段 A 交付、SQL 恢复记录、启动和迁移状态。
- SQL 第一检查门通过：
  - `MSSQLSERVER`：Running / Automatic
  - TCP `1433`：IPv4、IPv6 Listen
- 写入全阶段 12 个工作包、非目标、影响分析、风险和验收门。
- 当前开始 G1：创建 `AIDT` 数据库并执行 `InitialSchema`。

### G1 数据库创建与迁移

- SQL 第一检查门通过。
- TCP 集成认证首次尝试失败：SQL `18452`，不受信任的域。
- 使用同一 Windows 身份的命名管道连接成功。
- EF CLI 命名管道连接串转义失败，未执行迁移。
- 使用 SQLCMD ODBC 18 通过命名管道创建 `AIDT` 数据库。
- 执行 `001_InitialSchema.up.sql` 成功。
- 验证结果：9 Schema、34 表、31 外键、54 非主键索引、11 检查约束。
- `__EFMigrationsHistory`：`20261029170022_InitialSchema`，EF ProductVersion `10.0.14`。

## 2026-10-30 阶段 A 重验证

### 本轮边界

- 重新完整读取任务书、AGENTS、冻结需求/技术栈/计划、指定开发规范、功能地图和规划文件。
- 只做磁盘审计与非 SQL 操作；不连接、不启动、不修复 SQL Server。
- 不回滚或删除后续运行留下的数据库和代码，只把本轮状态重新收口到阶段 A。

### 已完成

- 默认 API 与 Worker 配置切回 `PHASE_A_COMPLETE_WAITING_SQL`、`IsReady=false`、`authored_not_applied`。
- Elsa 在 `IsConfigured=false` 时只保留接缝，不注册 SQL 持久化运行时。
- Worker 在 `EnableBackgroundProcessing=false` 时只返回骨架健康状态，不注册 Hangfire、Outbox、Garnet 或心跳任务。
- SQL 集成探针改为显式 `Skipped`；SQL 登录读取测试改为显式 `Skipped`。
- API 契约测试使用隔离的阶段 A 配置，验证 `401`、`503 blocked`、能力状态和 OpenAPI。
- 前端页面移除“真实 SQL 数据”“SQL AVAILABLE”“治理生效”等超范围表述。
- 补齐 OpenAPI 的前端实际调用端点并增加静态检查必选路径。
- 迁移状态文件顶部恢复为 `authored_not_applied`，保留后续历史执行证据。
- 新增迁移 002 的 DBA 审核检查点。

### 首轮复验

- `dotnet build AIDT.slnx --no-restore`：成功，0 警告、0 错误。
- `dotnet test AIDT.slnx --no-build`：29 总数，27 通过，0 失败，2 跳过。
- `npm run typecheck`：通过。
- `npm run test`：3/3 通过。
- `npm run contract:check`：首次旧契约通过；补齐端点后待复验。
- `npm run build`：生产构建通过。
- `dotnet format --verify-no-changes`：发现工作区既有的行尾/编码不一致，待机械统一后复验。

### 下一步

无；阶段 A 已完成，下一步只在阶段 B 恢复时执行 SQL 第一检查门。

## 2026-10-30 阶段 A 最终完成

### 最终测试

- `dotnet build AIDT.slnx --no-restore`：0 警告、0 错误。
- `dotnet test AIDT.slnx --no-build`：29 总数；27 通过、0 失败、2 显式跳过。
- `dotnet format AIDT.slnx --verify-no-changes --no-restore`：通过。
- `dotnet list AIDT.slnx package --vulnerable --include-transitive`：无已知漏洞。
- `npm ci`：成功；0 漏洞。
- `npm run typecheck`：通过。
- `npm run test`：3/3 通过。
- `npm run contract:check`：28 路径、33 operationId 通过。
- `npm run build`：生产构建成功。
- `npm audit --omit=dev`：0 漏洞。
- Markdown 相对链接：全部有效。

### 运行态烟雾

- API `/health/live`：200。
- API `/health/ready`：503 blocked，数据库 `isReady=false`、`authored_not_applied`。
- Worker `/health/live`：200，`phase_a_skeleton`。
- Worker `/health/ready`：503 blocked，后台执行 false。
- 相关进程已停止，测试端口已释放。

### 安全修复

- 发现 Elsa 传递依赖 `System.Security.Cryptography.Pkcs 9.0.4` 含高危公告。
- 在 `AIDT.Workflow` 直接锁定集中版本 `10.0.14` 后，NuGet 审计恢复为全部项目无已知漏洞。

### 当前状态

`PHASE_A_COMPLETE_WAITING_SQL`

整个 AIDT 项目：未完成。

### 阶段 B 第一检查

重新检查 `MSSQL*` 服务状态；服务不存在时立即停止并保持 SQL BLOCKED，不在本轮主动启动或修复 SQL Server。

## 2026-10-30 全阶段目标续跑

### 基线恢复

- 完整读取 `prompts/09-AIDT全阶段目标模式.md`。
- SQL 第一检查门通过：`MSSQLSERVER` Running，1433 IPv4/IPv6 Listen。
- 使用命名管道和 SQLCMD `-C` 核对实例：SQL Server 2025 Express，版本 `17.0.1000.7`。
- `AIDT` 数据库存在；35 张应用表、2 条 EF 迁移、15 条 Elsa 迁移、11 张 Hangfire 表。
- 恢复全阶段运行配置：数据库 Ready、Workflow Configured、Worker BackgroundProcessing 开启。

### 恢复点

- 新备份：`C:\ProgramData\AIDT\Backups\AIDT_FULLSTAGE_20261030_092845.bak`
- `RESTORE VERIFYONLY`：通过
- SHA256：`18C2637590B0E3CFDF6948BC6E26B9CE997B5FA50B9B8C058B1FC89F52EDC3AE`

### 测试

- `dotnet test AIDT.slnx`：29/29 通过，0 跳过。
- SQL 命名管道集成探针通过。
- 本地演示身份登录和真实 SQL 对象/连接器读取通过。

### 发现

- 历史示例 `SAMPLE-DEMO-002` 的流程 Completed，但业务对象仍为 Draft/Version 1；不得把该历史记录作为当前对象状态联动证据，后续需用新对象重新闭环验证。

## 2026-10-30 G7-G11 实施与验证

### G7 连接器与 AiGateway

- 新增 `backend/AIDT.AiGateway`，监听 `127.0.0.1:5100`。
- 实现模型白名单、超时、输入/输出摘要、规则模拟和明确 `ExternalModelCalled=false`。
- 新增 `MockConnectorAdapter`，返回 `Mocked` 回执且 `AllowsWrites=false`。
- Connector Outbox API 由固定 503 改为真实 SQL 查询。
- 平台外部系统模式改为 `Mock`，保持 `AllowWrites=false`。

### G8 前端与 E2E

- 前端恢复真实 SQL 数据文案，新增对象、流程、任务、审计互跳入口。
- 修复工作台权限误配：`platform:read` → `dashboard:read`。
- 修复 Element Plus `v-loading` 指令未注册。
- Playwright 桌面用例：创建对象、发起 WF-001、跨 5 个角色完成审批、对象 Active、审计、Agent 确认/停止全部通过。
- Playwright 移动端用例：5 个关键页面在 390px 可操作且无页面级横向溢出。
- 最终 E2E：2/2 通过。

### G9 测试

- 后端测试增至 41/41，0 跳过。
- 新增 SQL 唯一约束、检查约束、事务回滚、幂等唯一键和版本并发测试。
- 新增 API 幂等重放、同键异内容冲突、无组织范围拒绝、无接口权限拒绝、Core 字段脱敏和 Outbox 查询。

### G10 迁移、恢复与部署

- 迁移测试库：001 up 34 表 → 002 up 35 表 → 002 down 34 表 → 001 down 0 表。
- 部署前备份恢复：35 表、13 对象、59 审计、51 Outbox；临时恢复库已删除。
- 发布包 `demo-a`、`demo-b` 生成并校验。
- Nginx 1.28.0 在 `127.0.0.1:8088` 提供唯一入口；API/Worker/AiGateway 仅监听本机端口。
- 测试部署 A、升级 B、回滚 A 均通过健康检查。
- 离线导出：233 个 NuGet 包、约 45.6 MB npm 缓存；离线 restore/build/install/frontend build 全部通过。

### 关键缺陷修复

- E2E 发现业务角色工作台权限错误。
- E2E 发现 `v-loading` 未注册。
- 发布脚本修复 Nginx mime 路径和 temp 目录。
- 回滚脚本修复 Nginx worker 残留和状态/manifest 不同步。
- 离线构建修复 Rolldown Windows 原生绑定锁定。

## 2026-10-30 G12 最终完成

- 最终发布包：`D:\Apps\AIDT-Test\releases\AIDT-demo-final3.zip`
- SHA256：`684EE630E912AF3B55CF32C3B8464A9850A3F15F540CFE08A04366F4E882643E`
- 当前测试部署：`D:\Apps\AIDT-Test\versions\demo-final3`
- 浏览器入口：`http://127.0.0.1:8088`
- Nginx 部署环境 Playwright：2/2 通过，0 skipped，0 flaky。
- API/Worker/AiGateway/Nginx 健康检查全部 200。
- 数据库 Outbox Pending=0；Worker 心跳在最终重启后更新。
- 最终状态：`FULL_STAGE_COMPLETE_DEMO_READY`。
- 生产 AIDT 项目未完成：正式 SSO、真实外部系统、生产 DBA、高可用和运维监控仍待甲方环境。

## 2026-10-30 岗位场景运行时演示

- 遵守用户约束：未修改后端、前端、数据库迁移或部署脚本。
- 通过 `http://127.0.0.1:8088` 网页创建并完成 4 个岗位场景。
- 场景 01 `SCN-01-PUR-EXPEDITE-20261030`：采购员预算审核 → 财务大额复核 → 采购执行 → 生产验收 → 财务模拟付款；流程 Completed，5/5 任务 Approved。
- 场景 02 `SCN-02-SAMPLE-ANTIWEATHER-20261030`：研发立项 → 质量判定 → 生产接收 → 试产复核 → 转产发布；流程 Completed，5/5 任务 Approved。
- 场景 03 `SCN-03-DEV-TEMP-20261030`：生产现场确认 → 质量评审 → 经理重大偏差复核 → 质量 CAPA；流程 Completed，4/4 任务 Approved。
- 场景 04 `SCN-04-COMPLAINT-VISCOSITY-20261030`：质量受理 → 生产批次追溯 → 质量联合调查 → 经理客户处置回访；流程 Completed，4/4 任务 Approved。
- SQL 核对：4 个业务对象、4 个流程实例、18 个任务、26 条场景关联审计事件。
- 证据截图：
  - `docs/project/test-artifacts/scenario-center-proof-full.png`
  - `docs/project/test-artifacts/scenario-audit-proof.png`

## 2026-10-31 产品规划文档统一归档

- 建立 `coding/docs/001-产品PRD/`。
- 将原根目录四份文档迁移到产品规划目录：
  - `00-项目目标.md`
  - `01-需求基线.md`
  - `02-技术栈与架构.md`
  - `03-计划方案.md`
- 新增 `README.md`，作为产品规划统一入口和阅读顺序。
- 新增 `04-需求池.md`，登记下一阶段候选需求、状态、优先级、验收和安全边界。
- 更新 `AGENTS.md`、`README.md`、`standards/00`、`standards/01`、`standards/README.md` 和相关阶段提示词中的路径。
- 校验结果：根目录旧文件已不存在；新文件全部存在；旧 `01-需求.md` 路径无残留；项目内 Markdown 相对链接全部有效。
- 本轮未修改后端、前端、数据库迁移或部署代码。

## 2026-10-31 批次 2 目标模式提示词

- 已确认批次 1 标签 `req-batch1-final6` 和批次 2 分支 `feature/requirement-pool-batch-2` 可用。
- 已生成 `prompts/12-需求池批次2目标模式.md`。
- 已更新 `prompts/README.md`。
- 本批次任务书覆盖 AI 听记导入与行动项确认、节点能力包与 SLA、超时升级与数字员工受限代理、AgentRun/AiGateway 关联。
- 当前只完成提示词，未开始批次 2 功能实施。

## 2026-10-31 需求池批次 1 提示词

- 已审查需求池、冻结基线、功能地图、权限矩阵、Mock 边界和全阶段交付。
- 已确认需求池不能直接整体一次性实施。
- 已选择批次 1：员工优先入口、部门工作区、部门文件中心和演示/真实数据隔离。
- 已生成 `prompts/10-需求池批次1目标模式.md`。
- 听记、真实模型、SLA/代理和 PMO 项目模型明确排除在批次 1。

## 2026-10-31 需求池批次 1 启动

- 完整读取批次任务书、冻结 PRD、技术栈、计划、需求池、功能地图、权限矩阵、Mock 边界、全阶段交付和相关规范。
- Git 基线提交：`8aff291`。
- 批次分支：`feature/requirement-pool-batch-1`。
- SQL 迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH1_PRE_20261031_010423.bak`。
- 备份 SHA256：`ED12133E0F3B4545E7AF45ADE9D2DA2D9548A24D5A5DD6CB458994D2B51403DE`。
- `RESTORE VERIFYONLY`：通过。
- 已建立影响分析和变更计划：`docs/project/CHANGE_PLAN_REQUIREMENT_POOL_BATCH_1.md`。

## 2026-10-31 需求池批次 1 阶段 A 执行

### 阅读与恢复

- 完整读取 `AGENTS.md`、提示词 `07`、冻结目标/需求/技术栈/计划、开发规范、
  功能地图、需求池和 `task_plan/findings/progress`。
- 恢复 Git 分支 `feature/requirement-pool-batch-1` 和既有未提交改动。
- 将本轮定义为批次 1 的阶段 A；没有把历史全阶段 Demo 当作本轮 SQL 证据。

### 后端

- 修复 16 个初始编译错误并恢复 0 警告、0 错误。
- 完成部门、成员、主部门、部门负责人和 department DataScope。
- 完成我的工作、部门工作区、文件列表、上传、详情、版本、下载、预览和删除申请 API。
- 完成文件类型/MIME/签名/大小/SHA256 校验、敏感文件扫描门禁和越权审计。
- 完成对象、流程、任务、文件与审计的部门/环境过滤代码。
- 完成用户隔离幂等作用域，防止跨用户复用键读取他人结果。
- 默认 API/Worker 配置失败关闭：数据库未就绪、Workflow 未配置、后台不启动。

### SQL 边界纠正

- 一次早期全量测试在阶段 A 配置恢复前误触发既有 SQL 登录集成，返回 HTTP 500。
- 已立即停止 SQL 路径，没有继续连接诊断或执行迁移/写业务数据。
- 之后新增显式 SQL 跳过属性，将 10 个 SQL 用例改为默认 Skip，并在阻断配置下完成后续测试。

### 前端

- `/` 改为“我的工作”，新增 `/department`、`/documents`、`/management`。
- 员工导航与平台管理导航分离；员工卡片不显示 UUID、版本、节点代码和 TraceId。
- 文件页支持上传、关联事项、预览、下载、版本历史和删除申请。
- PDF/图片/文本预览；Office 明确提示下载；敏感文件状态可见。
- 环境标签和管理员跨环境开关已编写；390px 响应式样式已补齐。

### 数据库源文件

- 作者态表：38。
- 新增 `DepartmentWorkspaceFilesAndEnvironment` EF Migration。
- 新增幂等 up/down SQL、DBA 审核和数据模型说明。
- 更新 DemoSeed，使新库种子满足部门、环境、文件扫描和检查约束。
- 迁移状态固定 `authored_not_applied`。

### 验证结果

- `dotnet test AIDT.slnx --no-restore`：48 通过、0 失败、10 显式跳过、总计 58。
- `dotnet format AIDT.slnx --verify-no-changes --no-restore`：通过。
- `dotnet list AIDT.slnx package --vulnerable --include-transitive`：无已知漏洞。
- `npm run typecheck`：通过。
- `npm run test`：5 个文件、7 个测试通过。
- `npm run contract:check`：38 路径、46 operationId。
- `npm run build`：生产构建成功。
- `npm audit --omit=dev`：0 漏洞。
- Markdown 本地链接：通过；`git diff --check`：通过。
- API live `200`；ready `503 blocked`；未登录 `/my-work` 返回 `401`。
- Worker live `200`；ready `503 blocked`；后台执行 false。
- 烟雾测试进程已停止，端口无残留。

### SQL BLOCKED

- 原因：SQL Server 安装要求重启，错误码 3010。
- 已检查：沿用历史服务、1433、sqlcmd 证据；本轮未重新探测或修复。
- 未执行：迁移、SQL 集成测试、备份恢复、数据库端到端和浏览器持久化 E2E。
- 继续条件：阶段 B 通过服务、TCP 1433、实例版本和应用连接四项检查。

### 当前状态

`PHASE_A_COMPLETE_WAITING_SQL` / `REQUIREMENT_POOL_BATCH_1_IN_PROGRESS`

整个 AIDT 项目：未完成。

## 2026-10-31 需求池批次 1 完成

- SQL 第一检查门、迁移前备份和 `RESTORE VERIFYONLY` 通过。
- 隔离库完成 003 up/down/re-up、004 up；主库完成 003/004。
- SQL/API 集成 64/64，覆盖部门持久化、跨部门拒绝、环境隔离、文件安全与审计。
- 前端 7/7、OpenAPI 46 操作、生产构建通过。
- Playwright 5/5：员工入口、部门、文件拖拽/点击上传、预览/版本/删除申请、
  敏感文件拒绝、WF-001 回归和 390px。
- 备份恢复验证 38 表/4 迁移；应用 A→B→A 回滚及最终部署健康通过。
- 离线 233 NuGet 包、npm 缓存安装与构建通过。
- 最终版本：`req-batch1-final5`。
- 最终发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch1-final5.zip`。
- SHA256：`6DEFDDEB879C2F3CCC73BBD786E4CFDAA1FD3FA3E37E428D6D978B6B6A99353B`。
- Worker 依赖恢复后 ready 200，Garnet 6380、pendingOutbox=0。
- 当前状态：`REQUIREMENT_POOL_BATCH_1_COMPLETE`。

## 2026-10-31 批次 1 完整性复核

- 重新执行默认后端测试：48 通过、16 个 SQL 用例显式跳过。
- 重新执行前端类型检查、7/7 组件测试、OpenAPI 38 路径/46 operationId。
- 重新执行部署环境 Playwright：5/5 通过。
- 当前服务返回 `REQUIREMENT_POOL_BATCH_1_COMPLETE`，迁移 `applied_verified`。
- 主库迁移 003/004 已应用，应用表 38 张，部门 6 个，文件 17 个。
- 发现批次 1 改动尚未提交，现有 `req-batch1-final5` 的 sourceRevision 为 `8aff291...-dirty`。
- 因此功能通过，但版本治理尚未冻结。
- 已生成 `prompts/11-批次1版本冻结与批次2分支目标模式.md`。
- 整个 AIDT 需求池：未完成。

## 2026-10-31 批次 1 版本冻结启动

- 完整读取 `prompts/11-批次1版本冻结与批次2分支目标模式.md`。
- 完整读取 `AGENTS.md`、项目目标、需求基线、技术栈、计划、需求池、Git/部署/完成定义规范、批次 1 交付、测试证据、功能地图和三个规划文件。
- Git 状态：分支 `feature/requirement-pool-batch-1`，HEAD `8aff29131d3c6204d88aea47c5c61920581b3f0a`。
- 识别 89 个已跟踪修改和 46 个未跟踪文件；内容均属于批次 1。
- 未发现来源不明的用户改动、真实密钥、数据库文件、上传正文、日志或缓存进入候选提交。
- 发现 Playwright 瞬时结果目录未被忽略，已开始处理 `.gitignore` 与索引清理。
- 下一步：逐组审查代码差异和迁移，然后运行全部发布前门禁。

### 提交前验证

- `dotnet build AIDT.slnx --no-restore`：成功，0 警告、0 错误。
- `dotnet test AIDT.slnx`：64 总数，48 通过、16 个 SQL 用例显式跳过、0 失败。
- 隔离库 SQL/API 测试：64/64 通过，0 跳过、0 失败。
- `dotnet format`：通过。
- NuGet 漏洞检查：全部项目无已知漏洞。
- `npm ci`：成功安装 262 个包。
- `npm run typecheck`：通过。
- `npm run test`：5 个文件、7/7 通过。
- `npm run contract:check`：38 路径、46 operationId。
- `npm run build`：通过。
- `npm audit --omit=dev`：0 漏洞。
- `final5` 部署健康检查：root/API/Worker/AiGateway/capabilities 全部 200。
- `final5` Playwright：5/5 通过，0 skipped、0 flaky。
- 下一步：按逻辑拆分提交并确认工作区干净。

## 2026-10-31 批次 1 提交、合并与 final6 发布

- 批次 1 提交：`e37c55d`、`d385c5e`、`4cb4738`、`ed210b7`。
- `main` 非快进合并：`7aa50459acc0f1ec66ba70caa16d82cc574415af`。
- 干净 `main` 构建：`req-batch1-final6`，未覆盖 `final5`。
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch1-final6.zip`。
- SHA256：`008711843CDED2B6F0D0814959FE9B36F432C73FF4277E69E93D5164CE8E4895`。
- manifest sourceRevision：`7aa50459acc0f1ec66ba70caa16d82cc574415af`，不含 `-dirty`。
- 部署前备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-batch1-final6_20261031_092357.bak`。
- 备份 SHA256：`F73950AF218642E5F218DA1091452104437C3D9C2215A1CC86CFAB407146F40A`；`RESTORE VERIFYONLY` 通过。
- 部署：`D:\Apps\AIDT-Test\versions\req-batch1-final6`，上一版本 `req-batch1-final5`。
- 健康检查：root/API/Worker/AiGateway/Nginx capabilities 全部 200。
- SQL：4 条 EF 迁移、38 张应用表、6 个部门、任务主部门不一致 0。
- 部署环境 Playwright：5/5 通过，0 failed、0 skipped、0 flaky。
- 注释标签 `req-batch1-final6` 已创建，指向 `7aa50459acc0f1ec66ba70caa16d82cc574415af`。
- 发布记录提交：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`。
- 已从干净 `main` 创建并切换 `feature/requirement-pool-batch-2`。
- 批次 2 起始基线：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`。
- 合并基线与 `main` 一致；标签 `req-batch1-final6` 可访问。
- 本轮未实现任何批次 2 功能。
- 完成状态：`BATCH1_RELEASE_FROZEN_AND_BATCH2_BRANCH_READY`。

## 2026-10-31 需求池批次 2 启动

- 完整读取 `prompts/12-需求池批次2目标模式.md` 和全部指定基线。
- 当前分支 `feature/requirement-pool-batch-2`，基线 `48ea1a4`。
- 批次 1 标签 `req-batch1-final6` 可访问；工作区既有批次 2 提示词和批次 1 测试证据均保留。
- SQL 第一检查门通过：`MSSQLSERVER` Running，1433 Listen，`AIDT` ONLINE。
- 冻结正式 Feature ID：`MIN-001`、`WF-005`、`AGT-002`、`AIG-002`。
- 已创建变更计划和上下文包：
  `docs/project/CHANGE_PLAN_REQUIREMENT_POOL_BATCH_2.md`
  `docs/project/CONTEXT_PACK_REQUIREMENT_POOL_BATCH_2.md`
- 下一步：备份主库、校验备份并恢复批次 2 隔离测试库。

### 批次 2 数据库恢复点

- 主库备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_PRE_005_20261031_094122.bak`。
- SHA256：`4F67C0A6CFF8CEC733CD863D5A8CDABF48B16B42A3F668F087DA1AB75160EA8F`。
- `RESTORE VERIFYONLY`：通过。
- 隔离测试库：`AIDT_REQ_BATCH2_TEST_20261031_094122`。
- 隔离库验证：4 条 EF 迁移、38 张应用表。
- 主库未执行 005 迁移，未删除或覆盖旧备份。

### 批次 2 实施推进

- 新增 `MinutesSource`、`MinutesActionItem`、`MinutesTask`、`WorkflowNodePolicy`、
  `WorkflowNotification`、`AgentGatewayCall` 和 AgentRun/WorkflowTask 扩展。
- 005 隔离库完成 up/down/re-up，最终 5 迁移、44 应用表。
- 听记文本登记、规则提取、人工确认、幂等任务和来源反查 API 完成。
- 节点策略版本冻结、SLA、Worker 扫描、提醒、升级和 Agent 辅助提案完成。
- 低风险代理提供真人确认、独立 Agent 身份、通知和审计；高风险策略被拒绝。
- AgentRun 调用 AiGateway RuleSimulator 并保存 GatewayCall，外部模型仍未启用。
- 隔离库后端测试 70/70；前端 9/9；OpenAPI 53 路径/62 operationId；构建通过。
- 主库 005 已应用：5 条 EF 迁移、44 张应用表；AgentRun 作用域空值 0；
  AgentProfile 2 个；批次 2 权限 8 项。
- 第一次 final1 部署后重复 E2E 暴露 AgentProfile 选择不稳定；修复并顺延 final2。
- final2 发布包 SHA256：`339AC0BBF400F378FA0E570960F06EF85B0C97DEA5B3EFA1267B066A4301888E`。
- final2 manifest sourceRevision：`875204ed5d7fbc7cce7054959eab8c4fead255b8`，不含 `-dirty`。
- 应用回滚 `final2 → final1 → final2` 通过，恢复部署健康。
- 最终部署前备份：`AIDT_PRE_DEPLOY_req-batch2-final2_20261031_101159.bak`；
  SHA256 `111C41C8DC6F9429638B46E4B812117008EB93E9CA25D5C0F05035F9D86302E8`；恢复校验通过。
- 最终 Playwright：6/6，0 failure、0 skipped、0 flaky。

## 2026-10-31 批次 2 独立复核

- 当前分支 `main`；批次 2 标签为 `req-batch2-final2`；部署状态和 current-version 均为 `req-batch2-final2`。
- 健康检查：页面、API、Worker、AiGateway 全部正常；平台能力接口返回 `MIN-001`、`WF-005`、`AGT-002`、`AIG-002` 均为 `available`。
- 数据库：5 条迁移、44 张应用表；主库现有 7 条听记、21 条行动项、7 条听记任务和 7 条 GatewayCall。
- SQL/API 隔离库重跑：70/70 通过。
- 前端：类型检查通过；Vitest 6 个文件、9/9 通过；OpenAPI 53 路径、62 operationId；生产构建通过。
- 正式部署 Playwright：6/6 通过，包含听记人工确认、文件、WF-001 和 390px 移动端。
- 实测说明：主库尚未预置节点策略，当前流程页面显示“该版本尚无节点策略”；SLA/代理能力由隔离集成测试证明，不是默认演示数据。

## 2026-10-31 需求池批次 2.1 启动

- 完整读取目标文件和全部指定基线。
- 当前 `main` 基线 `a6d270f`，批次 2 标签和 `final2` 部署可用。
- 保留所有未提交 E2E 证据和独立复核记录。
- 创建分支 `fix/requirement-pool-batch-2-1`。
- 已建立 2.1 变更计划和上下文包。
- 下一步：备份主库、创建隔离库，然后实现 DOCX 与迁移 006。

### 批次 2.1 数据库恢复点

- 主库备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_1_PRE_006_20261031_121903.bak`。
- SHA256：`E5F17A4E50B9FF845704A4A3198E9A1ED28A5BF7A81458EAA65563CDCB5470F2`。
- `RESTORE VERIFYONLY`：通过。
- 隔离库：`AIDT_REQ_BATCH2_1_TEST_20261031_121903`，5 迁移、44 表。

## 2026-10-31 批次 2.1 可用性补完

- 修复 `MinutesDocumentParser` 的 `CA1822`，后端恢复 0 警告、0 错误。
- DOCX 只读取正文段落，支持正常、多段落、空文档、损坏 ZIP 和缺正文测试；
  PDF 继续只保存原文件并提示粘贴或转 DOCX。
- 新增 `/workflow-policies` 策略管理页、策略用户/节点候选 API 和受控超时模拟 API。
- 006 增加代理模式、自动代理时间、运行来源、原责任人、补偿状态和唯一幂等键。
- 默认 `RND_MATERIALS_CONFIRM` / `MATERIALS_CONFIRM` 策略：120/30/60/60、
  Low、A2、预授权自动代理、永久人工边界 false。
- 隔离库完成 006 up/down/re-up：6 迁移、44 应用表。
- SQL/API 76/76 覆盖预授权自动代理一次、重复扫描、独立服务身份、原责任人通知、
  完整审计、人工边界防深度校验、普通员工 403、金额/风险/等级超限拒绝。
- 前端已增加策略页和专用 E2E 用例；类型检查、9/9 单测、OpenAPI 56/65、构建通过。
- OpenAPI 和 DBA 审核已同步。
- 下一步：正式部署 Playwright、回滚/恢复验证、提交、合并和 final3 发布。
- 批次提交：`7e6e3f9`、`f54fa86`、`386ebd6`；非快进合并 `5382fe7`。
- final3 发布包 SHA256：`31524319F5BCD543CA6343C62C50C012DAF9C7EA76FB6E5A765140DA09B3B188`。
- manifest sourceRevision：`5382fe7738eedb6a6cf80cac74d7371a042fdd4e`，不含 `-dirty`。
- 正式部署 Playwright 8/8；应用回滚 final3→final2→final3 通过。
- 部署前备份恢复库验证：6 迁移、44 应用表、默认策略存在。
- 完成状态：`REQUIREMENT_POOL_BATCH_2_1_READY`。

## 2026-11-01 批次 2.1 收尾门禁

### 证据核验

- `git status --short --branch` 初始仅显示 6 个 `docs/project/test-artifacts/` 文件变化。
- `playwright-report.json`：8 expected、0 unexpected、0 skipped、0 flaky、0 errors。
- 报告运行时间晚于 `req-batch2-final3` 最终部署；部署清单源码为 `5382fe7`，迁移 `applied_verified`。
- 页面、API live/ready 和 capabilities 均 HTTP 200；能力接口仍返回 `REQUIREMENT_POOL_BATCH_2_1_READY`。
- SQL Server：`MSSQLSERVER` Running，TCP 1433 IPv4/IPv6 监听；`AIDT` ONLINE，6 条 EF 迁移、44 张应用表。
- `req-batch2-final3` 标签仍指向 `c68a799`，当前部署和 `current-version.txt` 未变化。
- 创建分支 `chore/closeout-batch-2-1`。
- 单独提交测试证据：`97646e1 test(batch2.1): refresh final deployment evidence`。

### 规划清理

- 将旧版 `REQUIREMENT_POOL_BATCH_2_1_IN_PROGRESS` 段落明确标记为历史快照。
- 文件顶部保留唯一当前权威状态 `REQUIREMENT_POOL_BATCH_2_1_READY`。
- 明确记录批次 3 是最后一个业务 Demo 批次，不是整个生产项目最终完成。

### 命令错误记录

- 首次健康检查组合命令因 PowerShell `foreach` 输出直接接管道导致 ParserError；改用 `$results = foreach (...)` 收集结果后重试，页面、live、ready、capabilities 均返回 200。

## 2026-11-01 需求池批次 3 启动

- 阶段 A 门禁通过后创建 `feature/requirement-pool-batch-3`。
- 完成 `CHANGE_PLAN_REQUIREMENT_POOL_BATCH_3.md` 和 `CONTEXT_PACK_REQUIREMENT_POOL_BATCH_3.md`。
- 冻结 Feature ID：`PMO-001`、`PMO-002`、`EVT-001`、`TASK-002`。
- 首次备份因 Express 不支持压缩失败，改为未压缩备份后成功。
- 主库备份 `RESTORE VERIFYONLY` 通过，SHA256 已记录。
- 首次恢复因目标数据目录不存在失败；查询实例默认目录后恢复成功。
- 隔离库 `AIDT_REQ_BATCH3_TEST_20261101_004955`：ONLINE、6 迁移、44 应用表。

### 批次 3 实现与测试

- 完成 `PROJECT` 主对象扩展、PMO 项目档案、成员、里程碑、关系、事件收件箱、建议和项目任务。
- 完成 `demo.pm`、`project-manager`、`agent.project`、`project-event-agent` 和 15 项 PMO 权限。
- 完成 `/projects`、`/projects/:id`、`/inbox` 页面、前端 API 类型和 390px 响应式。
- OpenAPI 更新为 73 路径、85 operationId；前后端契约、权限矩阵、功能地图和 Mock 边界同步。
- 最终隔离库 `AIDT_REQ_BATCH3_FINAL_20261101_010853` 完成 006→007 up、down、re-up：
  44→51→44→51 表。
- 主库 `AIDT` 已应用 007：7 条迁移、51 张应用表；历史 Title/CreatedBy 缺失均为 0。
- 默认后端 68 通过、25 SQL 跳过；最终隔离库 94/94 通过，0 跳过。
- 前端类型、9/9 测试、OpenAPI、构建和依赖审计通过。
- 源码 E2E 11/11 通过，覆盖桌面和 390px，0 failure、0 skipped、0 flaky。
- `req-batch3-final1` 已生成并部署，但正式 E2E 暴露管理员登录 Cookie 超限并返回 502；
  15 个新增管理权限使管理员权限 claims 超过安全响应头限制。
- 修复为压缩权限/数据范围声明并兼容旧 Cookie，新增身份访问器测试；最终后端 96/96 通过。
- `final1` 包保留且未打标签；应用回滚到 `req-batch2-final3` 恢复稳定回滚点。
- 从干净 `main` 构建并部署 `req-batch3-final2`，sourceRevision `9b9b7fe`。
- final2 正式 E2E 11/11，0 failure、0 skipped、0 flaky。
- 应用回滚 `final2 → final3 → final2`；最终 root/live/ready/capabilities 均 200。
- 最终部署备份恢复库验证：7 迁移、51 应用表、PROJECT/项目经理/事件 Agent 均存在。
- 创建标签 `req-batch3-final2`，未覆盖 final1 或前序发布包。
- 完成状态：`REQUIREMENT_POOL_BATCH_3_DEMO_COMPLETE`。

## 2026-11-01 3157C 真实流程试点

### 启动

- 用户确认先执行七项计划第 1 项：用现有网站和现有接口走通 `3157C` 真实测试流程。
- 冻结边界：不修改前后端源码、数据库结构、迁移和部署配置；只创建业务测试数据。
- 试点数据：不连接真实钉钉、不处理离职人员、不补齐截图缺失值，按 `Sensitive` 管理。
- 当前状态：第 1 项 `in_progress`，第 2–7 项 `pending`。
- 首次写请求因登录后仍使用登录前 CSRF 令牌被安全门禁拒绝，未写入数据。
- 登录后重新获取 CSRF 令牌并成功创建项目 `PRJ-3157C-20250808`，项目 ID `f507fb2e-cdf0-4841-a988-f3088fccdcbe`。
- 已添加研发工程师、质量工程师为 Contributor；项目经理为 Lead。
- 已创建 4 个里程碑：发起申请、核对转交、录入数据、复核异常。
- 项目详情读取结果：3 名有效成员、4 个里程碑、0 任务、0 文件、7 条审计。
- 首次附件上传因 PNG 被普通表单声明为通用 MIME，被文件签名/MIME 校验拒绝；通过显式 `image/png` 后成功，未产生脏数据。
- 已上传四张流程原始截图并关联项目，均为 `Sensitive / Active`；文件 ID 为 `9063e769-166a-4572-b804-26c3abeb9a22`、`56573e71-6bef-40af-82b5-42e5e31c176c`、`e12b10d1-06b6-4f95-9e48-550392f71c76`、`978f77c7-ca3d-4e7a-a29e-a112de850ded`。
- 已创建事件 `465ee426-c0d6-46b5-accc-bd4299c42005`，标题“3157C 负极胶小试测试申请与检测流程”，原文 721 字，附件关联第一张测试申请截图。
- 使用“项目事件建议数字员工”分析成功，建议 ID `7d4143a4-e39c-49dd-9d3f-862bdc21d691`。
- 分析结果识别到正确项目，风险 Low、优先级 P1，建议负责人为项目经理；缺失信息为“是否发起流程待人工判断”。
- 事件接口在已有建议后仍返回状态 `Received`，与预期 `AwaitingConfirmation` 不一致；列入试点体验问题。
- 事件真人确认后成功创建项目任务 `2f48608d-aa6d-4acf-aa33-347e98210c46`，负责人为质量工程师，截止 `2025-08-15 18:00:00`，优先级 P1。
- 质量工程师成功完成任务，任务状态 `Completed`，任务版本 2；项目进度刷新至 100%。
- 通过现有项目关系接口补录 4 条 `Attachment / SupportingEvidence` 关系，项目详情文件区现显示 4 张原始截图。
- 四个里程碑全部完成，项目统计为里程碑 4/4、任务 1/1、文件 4、项目关系 6、项目经理可见审计 19 条。
- 尝试将项目状态改为 `Completed`：接口和网页均提示更新成功，审计已写入 `project.status_changed`，但重新读取仍为 `Active`。
- 事件已有建议和已创建任务后，网页仍显示 `Received` 和“真人确认并创建项目任务”按钮；事件自身的 `AnalysisRunId` 也未落入读取结果。
- 数字员工把原文截止日期 `2025-08-15` 识别为 `2025-08-16 02:00:00`；任务确认时已人工修正。
- 已保存项目详情和收件箱视觉证据：`docs/project/test-artifacts/3157c-pilot-project-detail.png`、`docs/project/test-artifacts/3157c-pilot-inbox.png`。
- 第 1 项结论：核心业务数据链已真实落库并在网页可见；事件/项目状态持久化和附件自动关联仍有待修复，第 2 项可以基于本次结果开始整理，但不能把本场景标记为“完全通过”。

## 2026-11-01 3157C 试点与配置化需求清单

- 第 2 项完成：新增 `docs/project/3157C_PILOT_AND_CONFIGURATION_REQUIREMENTS.md`。
- 文档记录了真实项目、成员、里程碑、任务、附件、事件、Agent 建议、真人确认和状态问题。
- 已将 4 个问题区分为 2 个 P0、2 个 P1，并给出影响、疑似根因和修复要求。
- 明确不建立财务、采购、经理、研发等割裂数据库；使用统一业务对象和账本，通过角色视图、权限、流程模板和字段视图体现岗位差异。
- 明确可配置范围：流程模板、节点能力、人员权限、表单字段、文件证据、SLA/Agent、项目任务、配置发布治理。
- 明确 L1–L4 配置分级和审批责任。
- 明确员工工作台、部门/项目工作区、流程配置中心、平台治理中心四类前端页面。
- 明确配置前端前必须先补齐工作流模板、版本、节点 Schema、沙箱、发布和回滚等后端能力。
- 形成 `IDEA-CFG-002`、`IDEA-UX-002`、`IDEA-EVT-003`、`IDEA-DOC-002`、`IDEA-OBS-001` 五条候选需求。
- 七项计划当前状态：第 1–2 项完成；第 3–7 项待执行。

## 2026-11-01 AIDT 业务用户界面原型

- 第 3 项完成：新增独立交互原型 `docs/project/prototypes/AIDT_BUSINESS_UX_PROTOTYPE.html`。
- 原型包含员工“我的工作”、项目工作区、流程配置中心、平台治理四个页面。
- “发起事项”弹窗支持事项类型、项目、标题、截止时间、优先级、说明和附件入口演示。
- 流程配置展示发起、审核转交、数据录入、异常条件、数字员工代理、完成归档等业务节点。
- 节点属性包含责任人、退回责任人、SLA、提醒、升级、数字员工和永久人工边界。
- 项目页按 100% 进度、4/4 里程碑、1/1 任务、4 份附件和 19 条审计展示试点结果，并明确标注状态待修复。
- 已通过本机临时静态服务完成浏览器验证：页面切换、事项弹窗、节点选择正常，控制台无 warning/error。
- 已保存 6 张视觉证据：员工端、项目端、配置端、治理端、发起事项弹窗和窄屏员工端。
- 未修改正式前端、后端、数据库迁移和部署配置。
- 七项计划当前状态：第 1–3 项完成；第 4–7 项待执行。

## 2026-11-01 配置中心最小可执行后端

- 第 4 项完成：实现 `CFG-001` 配置草稿、Revision 并发、校验、发布和回滚。
- 新增 Application 配置契约和服务接口，新增 `SqlWorkflowConfigurationService` 和 NotReady 实现。
- `ConfigurationsController` 从空壳查询升级为 7 个真实 API 操作。
- 发布配置会生成可执行 WorkflowDefinition/WorkflowVersion；隔离库已用回滚后的版本实际发起流程。
- 新增配置权限：读取、创建、更新、发布、回滚；平台管理员负责发布和回滚。
- 008 迁移前备份：`AIDT_PRE_BATCH4_008_20261101_023726.bak`，SHA256 `38E0AB14005613AEB0C3B861C0A4B363E1932F9D1AB2AEAE177E7CD5B9B06919`。
- 隔离库 `AIDT_CFG_BATCH4_TEST_20261101_023700` 完成 008 up/down/re-up；主库 `AIDT` 已应用 008，现有 8 条 EF 迁移、51 张应用表。
- 全量隔离 SQL/API 测试 97/97 通过，0 失败、0 跳过。
- `.NET` 构建 0 警告、0 错误；格式、EF 模型快照和 NuGet 漏洞检查通过。
- 前端类型检查、9/9 测试、OpenAPI 76 路径/90 operationId、生产构建通过。
- 能力接口和功能地图已更新 `CFG-001`。
- 交付文档：`docs/project/CONFIGURATION_CENTER_BACKEND_DELIVERY.md`。
- 用户明确要求前序问题先记录、后统一决定；本轮未修复事件状态、项目状态、附件自动关联和日期偏移问题。
- 七项计划当前状态：第 1–4 项完成；第 5–7 项待执行。

## 2026-11-01 配置中心前端与员工工作台

- 第 5 项完成：员工工作台接入真实事项创建和附件关联。
- 新增正式流程配置中心 `/configurations`，支持创建草稿、复制新版本、节点编辑、Revision 保存、校验、发布和回滚。
- 新增 `configurationApi`、配置前端类型和配置人员候选接口。
- 开发联调中发现并修复两个新问题：页面加载前早点击导致空部门；配置回滚后流程定义名称未更新。
- 配置中心专项 E2E：员工事项提交、经理创建/校验、管理员发布通过。
- 正式部署 Playwright：13/13，0 failure；移动端包含配置中心且无横向溢出。
- 前端类型检查、10/10 Vitest、OpenAPI 77/91、生产构建通过。
- 隔离库 SQL/API：97/97，0 失败、0 跳过。
- final1 部署后补齐版本号、导航文案和交付文档，重新构建 `req-config-center-final2` 并部署到 `http://127.0.0.1:8088`。
- final2 发布包 SHA256：`5E513F0820279B189F4611A54A60CAFC97268DB2E932018CFF608B0B09952E06`。
- final2 部署备份：`AIDT_PRE_DEPLOY_req-config-center-final2_20261101_025759.bak`，SHA256 `3FC621DB7E6777988290A8127392718A19543BE88AE9EF913805B4CDED9EC110`。
- 前序事件状态、项目状态、附件自动关联和日期偏移问题未修复，继续保留。
- 七项计划当前状态：第 1–5 项完成；第 6–7 项待执行。

## 2026-11-01 第二轮多岗位试点

- 第 6 项执行完成，结论为 `PILOT_COMPLETED_WITH_ROLE_BLOCKER`。
- 创建项目经理直审流程及项目 `PRJ-CFG-PILOT-20261101-01`。
- 研发工程师成功处理 `TEST_REQUEST`。
- 项目经理能收到 `MANAGER_REVIEW`，但审批返回 `PERMISSION_DENIED`。
- 平台管理员也无法处理他人任务，返回 `TASK_ASSIGNEE_MISMATCH`。
- 创建兼岗等价流程及项目 `PRJ-CFG-PILOT-20261101-02`。
- 研发工程师、平台管理员兼经理、质量工程师顺次通过三个节点。
- 成功实例 `5e1f3e40-4e83-466f-ba61-402a12cd1b1d` 状态 `Completed`，三个任务全部 `Approved`。
- 审计包含 WorkflowInstance Start 和三次 WorkflowTask Approve。
- 研发、质量岗位访问配置中心均为 403。
- 新增 P0 问题：项目经理角色缺少流程任务审批、退回和终止权限。
- 新增 P1 问题：流程任务列表未按待办状态过滤，已 Approved 的任务仍显示在当前待办并保留动作按钮。
- 试点报告：`docs/project/STEP6_MULTI_ROLE_PILOT_REPORT.md`。
- 七项计划当前状态：第 1–6 项完成；第 7 项待执行。

## 2026-11-01 第 7 项指标与生产化验证

- 第 7 项完成：`KPI_VALIDATION_COMPLETED_NOT_PRODUCTION_READY`。
- 两条多岗位流程实例：1 个 Completed、1 个 Running；整体完成率 50%。
- 真实项目经理路径完成率 0/1；管理员兼岗等价路径完成率 1/1。
- 流程任务整体完成率 4/5（80%）；成功实例任务完成率 3/3（100%）。
- `3157C` 证据对象完整性 10/10，但事件状态、AnalysisRunId 和项目状态不一致。
- 成功多岗位流程证据链 6/6，审计包含 Start 和三次 Approve。
- 自动关联覆盖 4/5（80%）；文件上传自动形成项目证据关系仍失败。
- 没有历史人工工时和错误率基线，因此未虚构效率提升比例。
- 问题总计 3 个 P0、3 个 P1。
- 验证报告：`docs/project/STEP7_KPI_VALIDATION_REPORT.md`。
- 七项计划全部执行完成；生产化门槛未通过。

## 2026-11-01 配置中心试点基线冻结

- 冻结提交组：`40d1156`、`e62b252`、`74b89a2`，非快进合并提交 `00a8098`。
- 工作区在构建前干净，发布包 manifest `sourceRevision=00a809837c2ecd6c0386814c28bb7732474f6fc8`，不含 `-dirty`。
- 冻结发布版本：`req-config-center-final3`。
- 发布包 SHA256：`743C7433C872625DC2E074C68228BA254C98956821B3F5D26775F676384DA6A6`。
- 部署备份：`AIDT_PRE_DEPLOY_req-config-center-final3_20261101_032156.bak`。
- 部署备份 SHA256：`80FB98B36D894473EBD818882AC9F485F453443A74CC846C1B6B13FB58E58C48`。
- 新版本已部署到 `http://127.0.0.1:8088`；页面、live、ready、capabilities 均为 HTTP 200。
- 配置 API 使用项目经理身份返回 200，可见配置总数 8。
- 冻结状态：`CONFIG_CENTER_PILOT_FROZEN_NOT_PRODUCTION_READY`。
- 已知 P0/P1 不修复，留待后续修复批次统一处理。

## 2026-11-01 前端导航与 IAM 权限批次

- 从干净 `main@40c30d5` 创建 `feature/frontend-navigation-iam`。
- 完成变更计划、上下文包和影响分析。
- 实现 IAM 组织树、人员邀请/开通、角色 CRUD、权限矩阵、数据范围、范围角色分配和双人审核。
- 实现导航工作区、版本、默认模板、发布、停用、恢复、当前导航树和个人偏好。
- 实现六类流程处理人策略、任务候选人/角色版本快照、转交、升级和真实项目经理审批。
- `project-manager` 补齐 `workflow-task:approve/return/terminate`；审批仍受任务处理人和节点资格约束。
- 新迁移 009：6 张新表、57 张应用表；001–008 未修改。
- 主库迁移前备份：`AIDT_FRONTEND_NAV_009_PRE_20261101_040523.bak`。
- 备份 SHA256：`112246EBB6F19AE5695B0F0BC694C1A92B54D8A312710F6F2FD7CFE125FDF235`。
- 隔离库 `AIDT_FRONTEND_NAV_TEST_20261101_040523` 完成 009 up/down/re-up。
- 最终干净隔离库 `AIDT_FRONTEND_NAV_FINAL_20261101_041947`：SQL/API 104/104。
- 主库已应用 009：9 条迁移、57 张应用表；项目经理动作权限 3/3。
- 前端：类型检查、11/11 Vitest、113 路径/133 operationId、生产构建通过。
- 源码桌面 + 390px Playwright：15/15，0 失败、0 跳过、0 flaky。
- 待执行：提交、合并、干净构建、正式部署、正式 E2E、回滚/恢复和冻结标签。
- 功能提交：`c529db1`、`e22a6d5`、`6929828`；非快进合并 `0bd8547`。
- 发布包 `req-navigation-iam-final1`：SHA256 `C5DA0827D1D00D1804234958439AB49EFD92BAACCFBD554D082D4EA364F68DEF`。
- manifest sourceRevision `0bd8547...`，不含 `-dirty`。
- 正式部署 Playwright：15/15。
- 应用回滚 final1 → req-config-center-final3 → final1 通过。
- 部署备份恢复验证：`AIDT_NAV_IAM_RESTORE_20261101_042649`，9 迁移、57 应用表。
- 创建冻结标签 `req-navigation-iam-frozen-v1`。

## 2026-11-01 前端导航与 IAM 收尾验证

- 从干净 `main@628e155` 创建 `fix/navigation-iam-closeout`。
- 完成收尾变更计划、上下文包和影响分析。
- 先补失败测试，确认跨部门候选、legacy 成员数、organization scope 越权、Agent 死链和 reject 路由缺失。
- 修复 IAM 部门范围、委派上限、范围外成员保护、legacy/scoped 成员统计和停用判断。
- 完成 Agent 节点 Worker 闭环、人工确认、永久人工边界阻断和配置页 Profile 选择器。
- 完成高风险审核驳回、服务端人员分页、前端动作权限和六类策略测试。
- 主库验证前备份 SHA256：`7D4D5436D63EDA715AE0826688E7E2810303647C3654901B29F91F7BC8DF9589`。
- 全量隔离库 `AIDT_REQ_NAV_IAM_FINAL_20261101_171000`：后端/SQL/API 112/112，0 跳过。
- 恢复探针库 `AIDT_REQ_NAV_IAM_MIGRATION_20261101_164000`：009 down/re-up 通过，8/51 → 9/57。
- 前端类型检查、13/13 Vitest、114 路径/134 operationId、生产构建通过。
- 源码桌面 + 390px Playwright：17/17，0 失败、0 跳过、0 flaky。
- 无新增迁移；001–009 未修改。
- 功能分支提交 `7ba4ee8`，非快进合并 `d53fba3`。
- 从干净合并提交构建 `req-navigation-iam-final2`，SHA256 `7B538AC01AE4671C83B42266B942339B9A74724797CD14DFB11C0C1E921A8B5B`，manifest 不含 `-dirty`。
- 部署前备份 SHA256 `63F6B97C60214ADDEC8138677027EA05D1E4073E712A580EF7AD845BC5304887`。
- 正式部署桌面/390px Playwright 17/17；页面、API、Worker、AiGateway、capabilities 全部 200。
- 应用回滚 final2 → final1 → final2 通过。
- 部署备份恢复到 `AIDT_NAV_IAM_FINAL2_RESTORE_20261101_151859`：ONLINE、9 迁移、57 表、项目经理三动作权限 3/3。
- 创建收尾冻结标签 `req-navigation-iam-closeout-frozen-v1`。

## 2026-11-01 需求池批次 3 收尾验证

- 从干净 `main@93c4cd4` 创建 `fix/requirement-pool-batch-3-closeout`。
- 完成收尾变更计划；补齐独立上下文包和影响分析。
- 修复事件分析/确认使用未跟踪实体导致刷新后回退的问题。
- 修复项目状态修改未更新真实主对象、版本和版本快照的问题。
- 修复 PROJECT 文件上传缺少项目 SupportingEvidence 关系的问题。
- 修复截止日期依赖服务器偏移的问题，集中使用上海时区 18:00。
- 新增 API 重新读取、独立 SQL 上下文、文件幂等/非项目回归和多段项目状态测试。
- 新增桌面刷新 E2E，覆盖项目状态、自动文件证据、事件状态、上海时间和最终进度。
- 隔离库 `AIDT_BATCH3_CLOSEOUT_FINAL_20261101_185000` 后端/SQL/API 115/115，0 跳过。
- 前端类型、13/13 Vitest、OpenAPI 114/134、生产构建和依赖审计通过。
- 源码桌面 + 390px Playwright 18/18，0 failure、0 skipped、0 flaky。
- 隔离库历史修护：事件异常 24 → 0、项目状态异常 1 → 0、附件关系缺失 0 → 0。
- 已更新功能地图、权限矩阵、需求池、测试证据、模块 README、迁移状态和规划文件。
- 功能分支提交 `4cc0ba6`，非快进合并 `d9ee050`。
- 主库最终修护前备份 SHA256 `08AF9BE74009DD137ACBDBBB7AB963EF751066920329CE8E525A8A3BA0E368BE`，RESTORE VERIFYONLY 通过。
- 主库修护后事件、项目和附件关系异常均为 0；幂等复跑稳定。
- 从干净合并提交构建 `req-batch3-final3`，SHA256 `EAD2F5D6135EE5BFB525EEE46B2F57191C528F8FAE38EB1023517D5C86FAB4F6`。
- final3 部署时间 `18:35:47`；正式 Playwright 开始时间 `18:35:57`，18/18 通过。
- 应用回滚 `final3 → req-navigation-iam-final2 → final3` 通过，最终所有健康检查 200。
- 迁移探针 `AIDT_REQ_BATCH3_CLOSEOUT_MIGRATION_PROBE_20261101_183922`：009 down/re-up
  为 9/57 → 8/51 → 9/57，项目经理三项审批权限 3/3。
- 部署备份恢复库 `AIDT_REQ_BATCH3_CLOSEOUT_FINAL3_RESTORE_20261101_183818`：9 迁移、57 表、异常 0。
- 创建 `req-batch3-final3` 与 `req-batch3-closeout-frozen-v1` 两个标签，完成冻结。
