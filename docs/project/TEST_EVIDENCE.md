# AIDT 全阶段测试证据

> 日期：2026-10-30

## 🧪 UX-006 全站主栏目介绍（2026-11-05）

### 正式发布与 8088

- 功能提交 `7fc1be7`；非快进合并提交 `2d9ba09`；manifest 无 `-dirty`。
- 发布：`req-ui-ux006-final1`。
- 发布包 SHA256：
  `8025047A96557AD7C8792BB94ACFBDF4F03A1C0F6ED198B1639BF1EA16349324`。
- 部署时间：`2026-11-05T13:47:14+08:00`。
- 正式 8088 全量 Playwright：`70/70`，0 失败、0 跳过、0 flaky；
  报告开始 `2026-11-05T06:12:18Z`，时长 887,062 ms。
- 正式全量使用冻结基线库 `AIDT_UX006_FORMAL_FINAL_20261105_1435`，
  避免主库长期累积的自动化数据导致“我的工作/听记”长查询超时；测试结束后已切回主库。
- 应用回滚 `ux006 → ux005-final3 → ux006` 通过，所有健康端点 200。
- 最终主库备份 SHA256：
  `338CAF74469E8FD6D7A12F88F1DF77D251D4CF511C9C6630023E607D3E19786C`，
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库 `AIDT_UX006_FINAL1_RESTORE_20261105_1428`：ONLINE、
  18 条迁移、71 张应用表。
- 冻结标签：`req-ui-ux006-frozen-v1`。

### 隔离环境

- SQL/API/MCP：`AIDT_UX006_BACKEND_20261105_1218`。
- 完整源码 Playwright：`AIDT_UX006_E2EFINAL2_20261105_1322`。
- 两个库均由 `AIDT_PRE_DEPLOY_req-ui-ux005-final3_20261105_105603.bak`
  或 `AIDT_DEPT_WF_001_FINAL3_FROZEN_20261104_0250.bak` 独立恢复，不修改主库。
- 通过 `tests/e2e/Run-FullE2e.ps1` 临时创建并清理低风险/高风险数字员工服务身份。

### 门禁结果

- `npm run typecheck`：通过。
- `npm run test`：31 个文件、72/72 通过。
- `npm run build`：通过；仅保留既有大 chunk 提示。
- `npm run contract:check`：188 路径、209 operationId。
- `dotnet build AIDT.slnx --no-restore`：0 警告、0 错误。
- `dotnet format AIDT.slnx --verify-no-changes --no-restore`：通过。
- SQL/API + MCP：198/198，0 失败、0 跳过。
- npm 生产依赖审计：0 漏洞。
- NuGet 全项目漏洞审计：无已知漏洞。
- 源码 Playwright：68 通过、0 失败、2 个部署专用用例按设计跳过；
  正式 8088 全量补齐后为 70/70。
- UX-006 专项：5/5 通过，覆盖 17 个栏目、刷新/重登、无权限、320—1920 响应式、
  抽屉滚动和侧栏截图。

### 视觉证据

- `docs/project/test-artifacts/ux006-intro-*-1920.png`：17 个栏目介绍抽屉。
- `docs/project/test-artifacts/ux006-responsive-*.png`：1920、1366 的三档、
  1280、820、600×518、390、320。
- `docs/project/test-artifacts/ux006-shell-1920-sidebar.png`：完整侧栏。
- `docs/project/test-artifacts/ux006-shell-1920-nav-bottom.png`：导航滚动到底部。

## 🧪 UX-005 正式部署冻结（2026-11-05）

### 最终发布与正式 8088

- 发布：`req-ui-ux005-final2`；源码 `0654008`；manifest 无 `-dirty`。
- SHA256：`0B13C8B595E549DECC9D566D723A06DF6FDA4931A9CEFEB9F1E5E6F78EF6AD93`。
- 8088 正式全量 Playwright：`64/64`，0 失败、0 跳过、0 flaky；
  报告开始 `2026-11-05T02:32:27Z`，时长 568,332 ms。
- 正式全量使用 8088 Nginx 入口；后端临时连接由冻结 step8 备份恢复的
  `AIDT_UX005_FINAL2_E2E_20261105`，以避免主库累积自动化数据造成瞬时 SQL 取消。
  主库未被覆盖，测试结束后已切回。
- 切回主库后的 UX-005 关键烟测：`10/10`，覆盖桌面、1280、600×518、390×844、
  我的工作和 AI 听记。
- 修复后的 Vitest：30 个文件、67/67；新增“导航完成加载不关闭已打开抽屉”。
- 应用回滚：`final2 → final1 → final2`，各阶段健康检查全部 200。
- 最终备份 `AIDT_UX005_FINAL2_FROZEN_20261105_1045.bak`：
  SHA256 `E482EF7FB8F47CD49DAC1B98C503B3D3D6575110D85D9CCE2B9628AADDA7ADE1`，
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库 `AIDT_UX005_FINAL2_FROZEN_RESTORE_20261105_1045`：
  ONLINE、18 迁移、71 应用表、7 个部门默认工作区。

### 源码评审门禁（final2 前）

### 隔离测试环境

- 隔离库：`AIDT_UX005_RELEASE_20261105`，由 `req-ui-step8-final3` 干净基线备份恢复。
- 主库 `AIDT` 在源码门禁期间未修改。
- 测试服务账号仅存在于隔离库，用于数字员工 E2E 夹具。

### 后端与契约

- `dotnet build AIDT.slnx --no-restore`：0 警告、0 错误。
- `dotnet format AIDT.slnx --verify-no-changes --no-restore`：通过。
- `dotnet test tests/AIDT.UnitTests/AIDT.UnitTests.csproj --no-build`：
  187/187 通过，0 失败、0 跳过。
- `dotnet test tests/AIDT.McpTests/AIDT.McpTests.csproj --no-build`：
  11/11 通过，0 失败、0 跳过。
- NuGet 全项目漏洞审计：无已知漏洞。
- OpenAPI：188 路径、209 operationId。

### 前端

- `npm run typecheck`：通过。
- `npm run test`：30 个文件、66/66 通过；发布修复后复跑 30 个文件、67/67。
- `npm run build`：通过。
- `npm run contract:check`：通过。
- `npm audit --omit=dev --audit-level=high`：0 漏洞。

### 源码 Playwright

- 结果：63/63 通过，0 失败、0 跳过、0 flaky。
- 原有 1440、1280、390px 导航、我的工作、AI 听记、项目、文件、流程、IAM、
  数字员工、场景和工作台流程全部回归。
- 发布矩阵在 Chrome 和 Edge 中覆盖：
  `1920×1080`、`1920×1080@125%`、`1440×900`、`1366×768@100%`、
  `1366×768@125%`、`1366×768@150%`、`1280×720`、`1024×768`、
  `820×900`、`600×518`、`390×844`。
- 缩放验证使用等效 CSS 视口和对应 deviceScaleFactor，截图保持目标物理像素尺寸。
- 验证侧栏固定、导航独立滚动、品牌/底部固定、部门单开、当前菜单可见、
  `820px` 抽屉、Esc 和焦点返回、页面无横向滚动、表格仅内部横滚。

### 截图

- `docs/project/test-artifacts/ux005-release-edge-1366x768-zoom100-100.png`
- `docs/project/test-artifacts/ux005-release-edge-1366x768-zoom125-125.png`
- `docs/project/test-artifacts/ux005-release-edge-1366x768-zoom150-150.png`
- `docs/project/test-artifacts/ux005-release-edge-1280x720-100.png`
- `docs/project/test-artifacts/ux005-release-edge-820x900-100.png`
- `docs/project/test-artifacts/ux005-release-edge-600x518-100.png`
- `docs/project/test-artifacts/ux005-release-edge-390x844-100.png`
- `docs/project/test-artifacts/ux005-release-chrome-1366x768-zoom100-100.png`
- `docs/project/test-artifacts/ux005-release-chrome-1366x768-zoom125-125.png`
- `docs/project/test-artifacts/ux005-release-chrome-1366x768-zoom150-150.png`
- `docs/project/test-artifacts/ux005-c1-desktop-1920.png`
- `docs/project/test-artifacts/ux005-c2-desktop-1920.png`

## 🧪 DEPT-WF-001 部门工作空间与动态表单（2026-11-04）

### 备份与隔离

- 8A 备份：
  `C:\ProgramData\AIDT\Backups\AIDT_DEPT_WF_001_PRE_8A_20261103_232808.bak`
- SHA256：`E301473F67F7D8F66CAAEA57D9DFF1DE60F706C07BB0CA36287E46BF3B12F45F`
- `RESTORE VERIFYONLY WITH CHECKSUM`：通过。
- 迁移库：`AIDT_DEPT_WF_001_MIGRATION_20261104_0100`。
- 全量后端库：`AIDT_DEPT_WF_001_FINAL_20261104_0100`。
- 全量 E2E 库：`AIDT_DEPT_WF_001_E2E_FINAL2_20261104_0100`。

### 018 迁移

- 纯 017 基线：17 迁移、71 张应用表。
- up：18 迁移、71 张应用表。
- down：17 迁移、71 张应用表。
- re-up：18 迁移、71 张应用表。
- 7 个部门默认工作区；32 个测试导航全部退役；
  319 个测试配置已标记，测试配置发布中 0；部门流程负责人 26 项权限。
- 节点表单和任务表单无效 JSON：0。

### 源码门禁

- `.NET` 构建：0 警告、0 错误。
- `dotnet format --verify-no-changes`：通过。
- 后端/SQL/API：186/186，0 失败、0 跳过。
- MCP：11/11，0 失败、0 跳过。
- 前端 Typecheck：通过。
- Vitest：24 个文件、47/47。
- OpenAPI/TypeScript 契约：187 路径、208 operationId。
- 前端生产构建：通过。
- 生产依赖审计：0 漏洞。
- 源码 Playwright：52 通过、1 项部署专用 MCP OAuth 跳过、0 失败、0 flaky。
- E2E 使用独立还原库，服务身份夹具在测试后停用，避免重复运行依赖历史空账号。

### 专项覆盖

- 三层导航、公共入口去重、单部门默认工作区、收藏/排序/隐藏。
- 旧会话授权指纹 `Stale` 和重新登录提示。
- 测试导航/配置退役且普通用户主视图不出现。
- 部门流程负责人建模板、校验、发布、试运行、停用、恢复和回滚。
- 17 类字段、人员/日期/附件、业务对象字段引用和证据引用。
- 任务冻结表单、草稿、并发、幂等、缺必填拒绝和有效提交。
- 规则模拟 AI 审查、AgentRun 关联、人工确认和永久人工边界。
- 交付中心、研发、供应链、财务路径；跨部门拒绝和历史表单不变。
- 1440、1280、390px 响应式。

### 正式发布门禁

- 首次 `req-ui-step8-final1` 登录在 Nginx 返回 502；直连 API 为 200，
  根因为权限 Cookie 达约 4.5 KB，超过默认上游响应头缓冲。
- 修复后压缩范围角色声明、去重角色 Claim，并将 `/api/` 缓冲设为 16 KB；
  `demo.researcher` 经 Nginx 登录恢复为 200，Cookie 长度降至 2,952 字节。
- `req-ui-step8-final2` 全量 E2E 后暴露测试导航可替换财务正式默认工作区；
  代码改为测试导航永不进入当前导航，也不得占用非测试部门默认位。
- 导航状态修复脚本恢复 7 个部门默认工作区；后端全量回归 186/186、MCP 11/11。
- `req-ui-step8-final3` 正式全量 E2E：53/53，0 失败、0 跳过、0 flaky。
- 应用回滚 `final3 → req-ui-step7-final1 → final3` 通过。
- 重部署后部门与场景专项：8/8；页面/API/Worker/AiGateway/MCP 健康端点全部 200。
- 正式包 SHA256：`81B5FC5CB1EAAC66D3E321B0270037FD6E342D2D77004926CA4A28BF48F61C2A`。
- 最终备份 SHA256：`9641532E40CF78CFB92676D2698090584C7775A57A4C303999419009242F8071`。
- 独立恢复库：`AIDT_DEPT_WF_001_FINAL3_FROZEN_RESTORE_20261104_0250`，
  ONLINE、18 迁移、71 表、7 个部门默认工作区、发布中测试默认入口 0。

## 🧪 SCN-001-B 岗位业务场景中心前端与试用闭环（2026-11-03）

### 隔离与备份

- 主库测试前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_SCN_001_B_PRE_TEST_20261103_145500.bak`
- SHA256：`025FE780001933B4EC6011FD1248B98F366670AD71A8448D4F5C4741E82BB473`
- `RESTORE VERIFYONLY`：通过。
- 后端隔离库：`AIDT_SCN_001_B_TEST_20261103_145500`，17 迁移、6 场景。
- 源码 E2E 隔离库：`AIDT_SCN_001_B_E2E4_20261103_163000`，由同一备份独立恢复。

### 源码门禁

- `.NET` 构建：0 警告、0 错误。
- `dotnet format --verify-no-changes`：通过。
- 隔离库后端/SQL/API：181/181，0 失败、0 跳过。
- MCP：11/11，0 失败、0 跳过。
- 前端类型检查：通过。
- Vitest：22 个文件、45/45。
- OpenAPI：180 路径、201 operationId。
- 生产构建：通过。
- 源码 Playwright：49/49 通过；1 条部署专用 MCP OAuth 用例按设计跳过；
  0 失败、0 flaky。

### 正式部署、回滚与恢复

- 发布包 `AIDT-req-ui-step7-final1.zip`：
  SHA256 `19332AC8E38C5472D597E85DFE809C767CD458EC56EB7B9A97A3F26C219A8259`。
- manifest sourceRevision：`da404f9f412d0f66845c0250f2061b86150f2818`，无 `-dirty`。
- 正式 Nginx `http://127.0.0.1:8088` Playwright：50/50，0 失败、0 跳过、0 flaky。
- 正式 E2E 报告：`docs/project/test-artifacts/playwright-report.json`，开始时间
  `2026-11-03T08:47:14Z`，晚于最终部署。
- 应用回滚：`req-ui-step7-final1 → req-ui-step7a-final1 → req-ui-step7-final1`。
- 回滚后和重部署后页面/API/Worker/AiGateway/MCP 健康端点均为 200。
- 重新部署后场景专项 E2E：5/5，覆盖真实发起、配置生命周期和 1280/390px。
- 最终数据库备份：
  `C:\ProgramData\AIDT\Backups\AIDT_SCN_001_B_FINAL1_20261103_1700.bak`
- 备份 SHA256：
  `0E1E2274D0C2823273A59994A0DB5E3752D37A12D3CC268B134D83F314B9A22A`
- `RESTORE VERIFYONLY` 与独立恢复：通过。
- 恢复库：`AIDT_SCN_001_B_FINAL1_RESTORE_20261103_1700`，ONLINE、17 迁移、
  71 应用表、6 个基础场景、6 条启动映射、7 个发布版本、5 项场景权限、
  13 条场景审计。

### 专项覆盖

- 六类场景真实可见、预检和启动。
- 新启动摘要接口从当前活动流程任务读取当前步骤，并在任务推进后变化。
- 刷新和重新登录后启动记录一致。
- 七步向导真实调用文件、听记、预检和启动 API。
- 桌面 1440 与 1280 场景目录、详情和业务卡片。
- 移动 390px 可完成真实场景发起并查看下一步。
- 配置中心可完成草稿、校验、发布、停用、恢复，并实际写 SQL Server。
- 场景停用后隐藏于普通员工目录，历史映射不删除。
- 高风险发布、自审拒绝、部门边界、永久人工边界继续由既有 7A SQL/API 测试覆盖。
- 原 6 个场景目录和原 45 项正式 E2E 纳入全量回归。

说明：全量源码首轮曾暴露三个测试环境问题：项目闭环桌面用例接近原 90 秒门限、
低风险自动代理扫描在 Worker 抢先处理后可能返回空摘要、数字员工服务账号下拉
在异步加载时误选首个账号。分别以业务合理超时、空摘要安全归零和等待真实选项
后按显示名选择修复；最终在全新 E2E4 隔离库一次通过 49 项，MCP 部署专用项按设计跳过。

## ✅ SCN-001-A 岗位场景后端源码与迁移验证（2026-11-03）

### 基线与备份

- `main@ae64f50`，6A/6B 冻结，迁移最大 016，应用表 68，部署 6-Final1。
- 主库 017 前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_SCN_001_A_PRE_017_20261103_1239.bak`
- SHA256：`80B6AF181764800F38749E49EA16F828441B6DC491EFBDDD372E51BA239807DF`
- `RESTORE VERIFYONLY`：通过。

### 迁移

- 最终隔离库：`AIDT_SCN_001_A_FINAL_20261103_1250`。
- up/down/re-up：16/68 → 17/71 → 16/68 → 17/71。
- 3 张 Scenario 表、6 个场景、6 个版本、5 项权限、16 条角色授权、1 个财务人工流程。
- 无效场景 JSON 和引用快照 JSON：0。
- 主库 `AIDT`：17 条迁移、3 张场景表、6 个场景、6 个版本、5 项权限、1 个财务流程。

### 源码测试

- 后端/SQL/API：181/181，0 失败、0 跳过。
- MCP：11/11，0 失败、0 跳过。
- Vitest：20 个文件、41/41。
- OpenAPI：179 路径、200 operationId。
- 生产构建：通过。
- npm 生产依赖审计：0 漏洞。
- NuGet 依赖审计：无已知漏洞。
- 源码 Playwright：44/44 通过；1 条部署专用 MCP 用例跳过。

### 关键行为

- 六类基础场景分别启动真实对象+流程、项目任务和事件收件箱。
- 采购、财务和质量场景真实写入并返回业务链入口。
- 草稿、发布、停用、恢复、V2 发布和 V1 回滚通过。
- 已发布版本原地修改返回 409。
- 同幂等键同内容重放返回原启动记录，同键异内容返回冲突。
- 重复业务对象失败后，对象、流程和启动映射无半成品。
- 高风险质量场景首次发布进入 Pending，第二位平台管理员审核后发布。
- 申请人自审返回 403。

### 正式部署、回滚与恢复

- 发布包 `AIDT-req-ui-step7a-final1.zip`：
  SHA256 `5BD498BA71395137AB48FE3FDE2C24222D1E1BD824F18EA930FB32568858A3D7`。
- manifest sourceRevision：`6e772ba292736c100daaac35debca7dc3d05810d`，无 `-dirty`。
- 正式 Nginx `http://127.0.0.1:8088` Playwright：45/45。
- 部署环境场景烟测：管理员可见 6 个场景；研发工程师启动样品到转产场景，
  返回真实 BusinessObject、WorkflowInstance、WorkflowTask，幂等重放一致。
- 应用回滚：`req-ui-step7a-final1 → req-ui-step6-final1 → req-ui-step7a-final1`。
- 最终备份 SHA256：
  `A883E99E195562BC8E5A4372636006AA3537910E26D7F73889586BF5C8536558`。
- 独立恢复库：17 迁移、71 应用表、6 场景、6 个有效快照、1 条场景启动映射、
  5 项权限、1 个财务人工流程。

## ✅ AGT-003-B 数字员工身份中心源码验证（2026-11-03）

### 数据库迁移

- 主库迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_AGT003_B_PRE_MAIN_016_20261103_1205.bak`。
- SHA256：`F507534041F5E4B0D6AD36C54DA0C43ADA07AC7225327544B531B6EF8886821B`。
- `RESTORE VERIFYONLY`：通过。
- 隔离库：`AIDT_AGT003_B_TEST_20261103_104741`。
- 016 up/down/re-up：15/68 → 16/68 → 15/68 → 16/68。
- 主库已应用 016：16 迁移、68 应用表、3 项新增 `agent-read`、
  6 个未绑定数字员工服务账号。

### 源码门禁

- `dotnet build AIDT.slnx --no-restore`：0 警告、0 错误。
- `dotnet format AIDT.slnx --verify-no-changes --no-restore`：通过。
- 隔离库后端/SQL/API：172/172，0 失败、0 跳过。
- MCP：11/11，0 失败、0 跳过。
- 前端类型检查：通过。
- Vitest：20 个文件、41/41。
- OpenAPI：162 路径、183 operationId，静态校验通过。
- 生产构建：通过；生产依赖审计 0 漏洞；NuGet 无已知漏洞。
- 源码 Playwright：44 通过、1 个部署专用 MCP OAuth 跳过、0 失败、0 flaky。

### 新增覆盖

- 网页新建、编辑、校验、发布、刷新、重新登录、普通用户只读和部门范围。
- 网页查看版本、创建新草稿、发布、回滚，并验证回滚后当前名称和版本。
- 高风险变更第二位平台管理员审核，申请人自审按钮和后端均拒绝。
- `/agents?run=...` 深链自动打开运行记录并反查身份、流程、任务、MCP、Gateway 和审计。
- 停用身份后 MCP 服务令牌立即撤销，新令牌选择列表不再出现该身份；历史运行仍可打开。
- 流程配置与节点策略只显示有效发布身份，页面无 AgentProfile GUID 输入。
- 1280、390 身份中心和编辑表单可用，无横向溢出或不可操作遮挡。
- 原有 WF-001、项目、事件、流程策略、IAM、工作台和 MCP 正式能力无回归。

### 正式部署、回滚与恢复

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step6-final1.zip`。
- 发布包 SHA256：
  `282BDAE5DD0D9D3C2EEF8D240FBD03BC74F2C3F7792F6AA64E2CE29064FC937F`。
- manifest sourceRevision `fb20bfb00030ba9974ed2508974af6a465fd415b`，无 `-dirty`；
  包含 16 条迁移和 016 脚本。
- 正式 E2E：45/45，0 失败、0 跳过、0 flaky，包含部署专用 MCP OAuth。
- 应用回滚：`final1 → req-ui-step6a-final1 → final1` 通过；
  页面、API ready、Worker、AiGateway、MCP live/ready 和 capabilities 均 HTTP 200。
- 部署记录备份 SHA256：
  `CE4EE1D4C35DDDC72D673464B8440E96D8904AEFEA04F7A1CFCAF4D6A8126A41`。
- 最终备份：
  `C:\ProgramData\AIDT\Backups\AIDT_AGT003_B_FINAL1_20261103_1214.bak`。
- 最终备份 SHA256：
  `8B6B4C505CC359CB9D04A2E970FC20D0F1C068A5F55F33D6312A0471B2379F85`。
- 独立恢复库：`AIDT_AGT003_B_FINAL1_RESTORE_20261103_1214`；
  ONLINE、16 迁移、68 表、5 个有效发布身份、790 条 AgentRun、
  3 项新增授权、6 个服务账号。

## ✅ AGT-003-A 数字员工身份与能力治理源码验证（2026-11-03）

### 数据库迁移

- 迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_AGT003_A_PRE_015_20261103_084904.bak`。
- SHA256：`12AA239934AFA563C61DAAC4EE2F9AF07BD21A52AE9660AE6937C52D233256B6`。
- `RESTORE VERIFYONLY`：通过。
- 隔离库：`AIDT_AGT003_A_TEST_20261103_084904`。
- 015 up/down/re-up：14 迁移/67 表 → 15/68 → 14/67 → 15/68。
- 存量 3 个数字员工全部回填编码、部门、负责人、能力边界和 v1 发布快照。
- 重复编码 0、重复发布版本 0。
- 主库已应用 015，保持 15 迁移/68 表。

### 源码门禁

```powershell
dotnet build AIDT.slnx --no-restore
$env:AIDT_RUN_SQL_INTEGRATION_TESTS='1'
$env:AIDT_SQL_CONNECTION='Server=np:\\.\pipe\sql\query;Database=AIDT_AGT003_A_TEST_20261103_084904;Integrated Security=True;TrustServerCertificate=True'
$env:ConnectionStrings__Aidt=$env:AIDT_SQL_CONNECTION
dotnet test tests/AIDT.UnitTests/AIDT.UnitTests.csproj --no-build
dotnet test tests/AIDT.McpTests/AIDT.McpTests.csproj --no-build
```

- 后端/SQL/API：171/171，0 失败、0 跳过。
- MCP：11/11，0 失败、0 跳过。
- Vitest：18 个文件、35/35。
- OpenAPI：160 路径、181 operationId，静态校验通过。
- 前端类型检查和生产构建通过。
- 源码 Playwright：40 通过、1 个部署专用 MCP OAuth 跳过、0 失败、0 flaky。
- 正式 Playwright：41/41，0 失败、0 跳过、0 flaky。

### 新增覆盖

- 草稿创建/编辑/校验/发布；不可变发布版本；后续草稿；版本历史；回滚生成新版本。
- 并发 Revision 冲突、创建/发布/停用/恢复/审核幂等。
- 部门负责人授权部门边界、普通用户只读、服务身份登录治理页面拒绝。
- 高风险发布进入既有双人审核；申请人自审返回 403 并写安全审计。
- 未发布/停用身份不能新运行；停用后已有服务令牌失效、新服务令牌拒绝签发。
- 历史 AgentRun 可继续查询。
- 工具白名单、模型范围、金额/风险/自主等级和永久人工边界。
- 工作流 Agent 节点和 MCP 工具/Resource 只接受有效发布身份。
- 既有 AgentRun、工作流、MCP、IAM、项目与确认链路回归。

### 正式部署、回滚与恢复

- 发布包 SHA256：
  `32E201A25804D9B5876771D197CEE38ABFD992FF9BFCFA313CC69C8931264456`。
- manifest sourceRevision `e2be3c863deee1716b7de48bf4d857cdf1851332`，无 `-dirty`。
- 正式 E2E：41/41，0 失败、0 跳过、0 flaky。
- 应用回滚：`final1 → req-ui-step5-final1 → final1` 通过。
- 最终部署备份 SHA256：
  `91C583FCC1D9D0D373A863F5B363A192BA9D35EA7E5264565102F8404596CF5F`。
- 恢复库 `AIDT_AGT003_A_FINAL1_RESTORE_20261103_1017`：
  ONLINE、15 迁移、68 应用表、3 个有效发布身份、750 条历史 AgentRun。

## ✅ META-001-B 建模工作室与动态表单源码验证（2026-11-03）

### 数据库迁移

- 014 迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_META_001_B_PRE_014_20261102_235841.bak`。
- SHA256：`18E4413B6BEE9EF638969E1799C08F2FD0C73216C2AADDFE8299A7ECF305107E`。
- `RESTORE VERIFYONLY`：通过。
- 隔离库：`AIDT_META_001_B_TEST_20261102_235841`。
- 014 up/down/re-up：13→14→13→14；六个业务角色更新授权 `0→6→0→6`。
- 应用表保持 67，主库已应用 014。

### 源码门禁

- 后端/SQL/API：163/163，0 失败、0 跳过。
- MCP：11/11，合计 174/174。
- Vitest：18 个文件、35/35。
- OpenAPI：146 路径、167 operationId，静态校验通过。
- 源码 E2E：40 通过、1 个部署专用 MCP OAuth 用例按设计跳过、0 失败、0 flaky。
- 新增覆盖：网页新建对象类型、字段设计/排序/必填/长度/敏感等级/选项、预览、
  本地校验失败与修正、服务端校验、发布、版本历史、普通用户动态录入、
  User/Department 搜索选择、并发 409、刷新后编辑、对象版本、
  项目/流程/文件/审计关联、草稿隐藏、无权限拒绝。
- 流程配置只显示已发布类型；岗位工作台业务对象卡片可选择发布类型编码。
- 1440、1280、390 均无横向溢出。

### 正式部署、回滚与恢复

- 发布包 SHA256：
  `384291D1F053BCDAD5B831F82A3EEC5C03F019E9C55E0F8BFE40D29660721941`。
- manifest sourceRevision `9875ad2abc71dacf6caab13268c4b0b806ce9b3b`，
  14 条迁移，无 `-dirty`。
- 最终部署：`2026-11-03T00:30:41.2525647+08:00`。
- 正式 Playwright：41/41，0 失败、0 跳过、0 flaky；开始时间
  `2026-11-03T00:30:44.488+08:00`。
- 应用回滚：`final1 → req-ui-step5a-final1 → final1` 通过，页面、API、Worker、
  AiGateway、MCP 健康检查均 200。
- 部署前备份 SHA256：
  `47D8DAFD1866FB5451A0A0F7142E25D48B007B1974BD8934C080C0F4517D18F0`。
- 最终备份 SHA256：
  `0D3762398F19C6FBBAA940692EC42FA729FDA15AC56C500350E91B8D1FCC2114`。
- 恢复库 `AIDT_META_001_B_FINAL1_RESTORE_20261103_003623`：
  ONLINE、14 迁移、67 表、6 个业务角色更新授权、4 个动态模型版本。

## ✅ META-001-A 对象建模与字段编辑源码验证（2026-11-02）

### 数据库与迁移

- 迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_META_001_A_PRE_013_20261102_180708.bak`。
- SHA256：`1B83CF51B33D447A04FDBE9A0CF70EFC163EFDBF996F6D22417EE05DDFD30426`。
- `RESTORE VERIFYONLY`：通过。
- 迁移探针库：`AIDT_META_001_A_TEST2_20261102_180708`。
- 013 up/down/re-up：12/66 → 13/67 → 12/66 → 13/67。
- 现有 5 种对象类型全部回填为 Published，生成 5 条 v1 版本快照和 5 项对象模型权限。
- 主库 `AIDT` 已应用 013：13 条迁移、67 张应用表。

### 后端、SQL/API 与 MCP

```powershell
dotnet build AIDT.slnx --no-restore
$env:AIDT_RUN_SQL_INTEGRATION_TESTS='1'
$env:AIDT_SQL_CONNECTION='Server=np:\\.\pipe\sql\query;Database=AIDT_META_001_A_BACKEND_20261102_185349;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5'
$env:ConnectionStrings__Aidt=$env:AIDT_SQL_CONNECTION
dotnet test tests/AIDT.UnitTests/AIDT.UnitTests.csproj --no-build
dotnet test tests/AIDT.McpTests/AIDT.McpTests.csproj --no-build
```

- 后端/SQL/API：163/163，0 失败、0 跳过。
- MCP：11/11，0 失败、0 跳过。
- 合计：174/174。
- 构建：0 警告、0 错误；格式复验通过；NuGet 无已知漏洞。
- 覆盖：对象类型新建、编辑、校验、发布、停用、恢复、版本历史、幂等、
  不兼容类型拒绝、required/Select/长度规则、业务对象创建编辑、版本和审计、
  `expectedVersion` 冲突、无权限拒绝、Core 脱敏、现有五类对象兼容。

### 前端静态与源码 E2E

```powershell
npm run typecheck
npm run test
npm run contract:check
npm run build
npm audit --omit=dev
```

- Vitest：15 个文件、28/28。
- OpenAPI：142 路径、163 operationId，静态校验通过。
- 生产构建：通过；生产依赖：0 漏洞。
- 源码 E2E 使用独立干净库 `AIDT_META_001_A_E2E_20261102_185349`：
  37 通过、1 个部署专用 MCP OAuth 用例按设计跳过、0 失败、0 flaky。
- 新增 META-001-A E2E 覆盖完整对象生命周期和业务对象编辑持久化。
- 1440、1280、390px 原回归均通过。

### 正式部署、回滚与恢复

- 正式发布包 SHA256：
  `8F66CF0090C9F8C43AFDB14455B391E9438CDAD189024242C3E697D743C3DB06`。
- manifest sourceRevision：`f3d71c6d51913fba07d2134ecb53b6e57d21cbd8`，
  13 条迁移，不含 `-dirty`。
- 首次 `final1` 试部署因 Windows 动态端口排除覆盖 6380 而未形成有效证据，
  原包保留为 `AIDT-req-ui-step5a-final1-predeploy.zip`。
- 正式部署使用 `-GarnetPort 6480`；默认 6380 仍保留，Worker 缓存连接串与部署状态同步。
- 最终部署：`2026-11-02T22:53:19.4628230+08:00`。
- 正式 Playwright：38/38，0 失败、0 跳过、0 flaky；开始时间
  `2026-11-02T22:53:27.898+08:00`。
- 应用回滚：`final1 → req-ui-step4-final1 → final1` 通过，页面、API、Worker、
  AiGateway、MCP 健康检查均 200。
- 最终部署前备份 SHA256：
  `8BC9164F465AA1836CB9DECA64DC1C8C33D6DC8C63B6E15CB92FF9280AB706E7`。
- 最终备份 SHA256：
  `673AA522C4C2F2BCA65EA7C2D9006F1456E3B48935EFA839D485C7A1DAD7C2EB`，
  `RESTORE VERIFYONLY` 通过。
- 恢复库 `AIDT_META_001_A_FINAL1_RESTORE_20261102_225845`：
  ONLINE、13 迁移、67 张应用表、5 个现有类型、11 个版本快照、5 项建模权限。

## ✅ WS-002 岗位工作台模板源码验证（2026-11-02）

### 数据库迁移

- 迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_WS002_PRE_012_20261102_140411.bak`。
- SHA256：`B9A768B11015B62EA0A1AA1AABC03FA362A883AF7FE8C51C647C11AAEF409FB5`。
- `RESTORE VERIFYONLY`：通过。
- 隔离库：`AIDT_WS002_MIGRATION_PROBE_20261102_1405`。
- 012 up：11 → 12 迁移，6 个模板、31 张卡片、3 个模板权限。
- 012 down：模板权限 0；re-up：12 迁移、6 模板、31 卡片恢复。

### 后端与前端

```powershell
dotnet build backend/AIDT.Api/AIDT.Api.csproj --no-restore
dotnet build backend/AIDT.Mcp/AIDT.Mcp.csproj --no-restore
dotnet test tests/AIDT.UnitTests/AIDT.UnitTests.csproj --no-build
dotnet test tests/AIDT.McpTests/AIDT.McpTests.csproj --no-build
npm run typecheck
npm run test
npm run contract:check
```

- 后端/SQL/API：138/138，0 失败、0 跳过。
- MCP：11/11。
- 合计：149/149。
- Vitest：15 个文件、28/28。
- OpenAPI：136 路径、157 operationId，静态校验通过。
- 新增 Workbench API 用例：3/3，覆盖 6 模板、优先级、草稿/发布/停用/恢复、无权限卡片、跨部门拒绝和管理员复制边界。

### 源码 Playwright

- 岗位工作台专项：5/5，覆盖 1440、1280、390px。
- 六岗位首屏差异、无匹配回退、IAM 模板配置页、390px 无横向溢出均通过。
- 在从 012 前备份重建的干净最终库 `AIDT_WS002_FINAL_20261102_1438` 上执行全量源码 E2E：
  36 通过、1 个部署专用 MCP OAuth 用例按设计跳过、0 失败、0 flaky。
- 全量执行显式设置 `AIDT_E2E_MCP_PORT=5085`，避免源码 MCP 用例误连 8088。
- 首次 `final1` 包 manifest 漏列 012；运行包含迁移但发布元数据不一致。
  无标签包保留为 `AIDT-req-ui-step4-final1-premanifest.zip`，未覆盖删除。
- 截图：
  - `docs/project/test-artifacts/ws002-manager-1440.png`
  - `docs/project/test-artifacts/ws002-research-390.png`

### 正式部署与回滚

- 发布包 SHA256：
  `26F3E66B844709F8DFA152970E32F621019585CBA067532A229F72C483613435`。
- manifest sourceRevision：
  `efc473226faabcf90304d3018c6ff527ed4f8609`，12 条迁移，不含 `-dirty`。
- 最终部署：`2026-11-02T15:28:59.7140665+08:00`。
- 正式 Playwright：37/37，0 失败、0 跳过、0 flaky；开始时间
  `2026-11-02T15:29:06.497+08:00`。
- 应用回滚：
  `final1 → req-ui-step3-final1 → final1` 通过。
- 最终部署备份 SHA256：
  `993163DFB93976D1E92CAB3E5AEC33BBF0DD1122846B22330A771756484409D0`。
- 独立恢复库 `AIDT_UI_STEP4_FINAL1_RESTORE_20261102_1536`：
  12 迁移、66 表、6 模板、31 卡片、3 权限。
- 页面、API、Worker、AiGateway、MCP 健康检查均 200。

## ✅ UX-004-A 事件确认与流程发起（2026-11-02）

### 源码与隔离库

```powershell
dotnet build AIDT.slnx --no-restore
$env:AIDT_RUN_SQL_INTEGRATION_TESTS='1'
$env:AIDT_SQL_CONNECTION='Server=np:\\.\pipe\sql\query;Database=AIDT_UX004_A_FINAL_20261102_090200;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10'
$env:ConnectionStrings__Aidt=$env:AIDT_SQL_CONNECTION
dotnet test AIDT.slnx --no-build --no-restore
dotnet format AIDT.slnx --verify-no-changes --no-restore
dotnet list AIDT.slnx package --vulnerable --include-transitive
```

- Build：0 警告、0 错误。
- SQL/API：132/132；MCP：11/11；合计 143/143，0 失败、0 跳过。
- 新增覆盖：编辑负责人/截止/内容刷新保持；创建任务并发起新流程；
  关联已有流程；流程启动后确认失败整体回滚；缺负责人/截止、跨项目、跨部门和无权限拒绝。
- Format：通过；NuGet：无已知漏洞。
- 隔离库：10 条 EF 迁移、62 张应用表，无新增迁移。

### 前端静态门禁

```powershell
npm run typecheck
npm run test
npm run contract:check
npm run build
npm audit --omit=dev
```

- Typecheck：通过。
- Vitest：12 个文件、20/20 通过。
- OpenAPI：123 个路径、143 个 operationId。
- 生产构建：通过；npm 生产依赖：0 漏洞。
- 上海时区规则：`2025-08-15T10:00:00Z → 2025-08-15 18:00 +08:00`；
  缺负责人或截止时间时最终确认门禁为 false。

### 源码 Playwright

- 25 通过、1 个部署专用 MCP OAuth 用例跳过、0 失败、0 flaky。
- 新增用例在 1440、1280、390px 均通过，覆盖编辑确认、影响预览、新流程发起、
  刷新后事件/任务/流程/项目反查。
- 截图：
  - `docs/project/test-artifacts/ux004-confirmation-1440.png`
  - `docs/project/test-artifacts/ux004-confirmation-1280.png`
  - `docs/project/test-artifacts/ux004-confirmation-390.png`
- 原始报告：`docs/project/test-artifacts/playwright-report.json`。

### 正式部署 Playwright

```powershell
$env:PLAYWRIGHT_USE_EXISTING_DEPLOY='1'
$env:PLAYWRIGHT_BASE_URL='http://127.0.0.1:8088'
$env:AIDT_DEPLOY_ORIGIN='http://127.0.0.1:8088'
npm run test:e2e
```

- 正式 Nginx 部署：26/26 通过，0 失败、0 跳过、0 flaky。
- 报告时间 `2026-11-02T09:37:09+08:00`，晚于最终重新部署 `09:33:32`。
- 覆盖新增编辑确认与新流程原子发起，以及原 MCP OAuth、统一工作中心、文件、
  听记、项目、流程、IAM/导航等回归。

### 发布、回滚与恢复

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step2a-final1.zip`。
- SHA256：`C0179C5618C67F95BABB49A0254F0F9FD69A1D1DF2617AB4DDC5252C020CB553`。
- manifest 源码：`ed78d4813d5842d78e41646dda707a6ba150f9eb`，不含 `-dirty`。
- 正式版本目录：`D:\Apps\AIDT-Test\versions\req-ui-step2a-final1`。
- 应用回滚：`req-ui-step2a-final1 → req-ui-step1-final1 → req-ui-step2a-final1` 通过。
- 最终部署前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-step2a-final1_20261102_093323.bak`。
- 备份 SHA256：`3DA905C968B42974B351FBF97E3DB517E455F303671F1F686A339658C104A0E1`。
- 独立恢复库：`AIDT_UX004_A_FINAL1_RESTORE_20261102_093323`；10 条迁移、62 张应用表。

## ✅ UX-004 统一人工确认中心（2026-11-02）

### 隔离迁移

- 备份：`C:\ProgramData\AIDT\Backups\AIDT_UX004_B_PRE_TEST_20261102_103712.bak`。
- 备份 SHA256：`BD83AADE4DF731C7D81FA54D0C6B38A13F09968D34C5151B1DAE11CF7DFF3E07`。
- 迁移 011：`20261102023118_011_UX004ConfirmationCenter`。
- 隔离库 `AIDT_UX004_B_FINAL_20261102_113000` 完成 up/down/re-up。
- 结果：11 条迁移、62 张应用表；新增 3 个行动项上下文列、1 个任务项目列及系统导航项。

### 源码门禁

- `.NET` 构建：0 警告、0 错误。
- SQL/API：134/134；MCP：11/11；合计 145/145，0 失败、0 跳过。
- 前端类型：通过；Vitest：13 个文件、23/23。
- OpenAPI：125 路径、145 operationId。
- 生产构建：通过。
- 源码 Playwright：28 通过、1 个部署专用 MCP OAuth 用例跳过、0 失败、0 flaky。
- 新用例覆盖事件/听记/MCP/Agent 四类入口、1440/1280/390、听记编辑确认、
  MCP 通过/拒绝、技术信息折叠、无权限与深链兼容。

### 正式部署 E2E

```powershell
$env:PLAYWRIGHT_USE_EXISTING_DEPLOY='1'
$env:PLAYWRIGHT_BASE_URL='http://127.0.0.1:8088'
$env:AIDT_DEPLOY_ORIGIN='http://127.0.0.1:8088'
npm run test:e2e
```

- 正式 Nginx 部署：29/29 通过，0 失败、0 跳过、0 flaky。
- 最终报告开始时间 `2026-11-02T12:16:51+08:00`，晚于最终重新部署
  `2026-11-02T12:16:41+08:00`；耗时 200.867 秒。
- MCP OAuth 部署专用用例已实际执行并通过，未使用跳过作为通过条件。
- 报告 SHA256：`66C714BFF3462EA9607C81C8DDC9B36B70441461AB42CA06B0188C55C83B8D9E`。
- 原始报告：[playwright-report.json](./test-artifacts/playwright-report.json)。

### 发布、回滚与恢复

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step2-final1.zip`。
- 发布包 SHA256：
  `4832A05B0350D79E3C33E2438B89FE980A979D5D6A4508ED1F3D3AFD50F2C189`。
- manifest `sourceRevision`：
  `931f9531f71d58b189fb83941e30195edd30f6ef`，不含 `-dirty`。
- 应用回滚：
  `req-ui-step2-final1 → req-ui-step2a-final1 → req-ui-step2-final1` 通过；
  各阶段页面、API/Worker/AiGateway/MCP 健康检查通过。
- 最终部署备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-step2-final1_20261102_121633.bak`。
- 备份 SHA256：
  `0DB5198B0F3B5581FF128ADF5F56109E0E4E0DD5D6A05E6B620F2373B1728C4C`；
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库：`AIDT_UI_STEP2_FINAL1_RESTORE_20261102_1220`。
- 恢复核对：ONLINE、11 条 EF 迁移、62 张应用表、18 条
  `CONFIRMATIONS` 导航记录（全部指向 `/confirmations`）、听记 4 个新增上下文列、
  4 个迁移 011 外键。

## ✅ UX-003 统一工作事项中心（2026-11-02）

### 后端、SQL/API 与 MCP

```powershell
dotnet build AIDT.slnx --no-restore
$env:AIDT_RUN_SQL_INTEGRATION_TESTS='1'
$env:AIDT_SQL_CONNECTION='Server=np:\\.\pipe\sql\query;Database=AIDT;Integrated Security=True;TrustServerCertificate=True'
$env:ConnectionStrings__Aidt=$env:AIDT_SQL_CONNECTION
dotnet test AIDT.slnx --no-restore
dotnet format AIDT.slnx --verify-no-changes --no-restore
dotnet list AIDT.slnx package --vulnerable --include-transitive
```

- Build：0 警告、0 错误。
- SQL/API：127/127 通过；MCP：11/11 通过；合计 138/138，0 失败、0 跳过。
- 覆盖六类来源聚合、状态中文映射、排序、去重、组织/部门/环境过滤、无权限事项隐藏、
  详情深链、完成后移出待处理和刷新/重新登录一致性。
- Format：通过。NuGet：无已知漏洞。

### 前端静态门禁

```powershell
npm run typecheck
npm run test
npm run contract:check
npm run build
npm audit --omit=dev
```

- Typecheck：通过。
- Vitest：11 个文件、16/16 通过。
- OpenAPI：122 路径、142 operationId。
- Production build：通过。
- npm audit：0 漏洞。

### 源码 Playwright

```powershell
npm run test:e2e
```

- 结果：22 通过、1 跳过、0 失败、0 flaky。
- 跳过项：只在正式 Nginx 部署运行的 MCP OAuth 端到端用例。
- 覆盖“我的工作”今日焦点、待我处理、我发起、我参与、需要关注、最近来源证据，
  六类事项与正确详情深链，刷新/重新登录一致性，无权限事项不可见，完成后移出，
  1440、1280 和 390px 可用性。
- 证据：[Playwright JSON](./test-artifacts/playwright-report.json)、
  [Playwright HTML](./test-artifacts/playwright-html/index.html)。

### 正式部署 E2E

```powershell
$env:PLAYWRIGHT_USE_EXISTING_DEPLOY='1'
$env:PLAYWRIGHT_BASE_URL='http://127.0.0.1:8088'
npm run test:e2e
```

- 发布：`req-ui-step1-final1`。
- 结果：23/23 通过，0 失败、0 跳过、0 flaky。
- 覆盖源码全部用例，并额外执行正式 Nginx 部署专用 MCP OAuth 授权码、PKCE、工具发现、
  作用域、撤销与真人确认链。
- 正式健康检查：页面、API live/ready、Worker ready、AiGateway ready、MCP live/ready、
  capabilities 全部 HTTP 200。

### 发布、回滚与恢复

- 发布包 SHA256：`B38C51ED44EC9E40719A3DC5B65A0212AC47F1AF3B8CE1D5F1C533E498834D4E`。
- manifest `sourceRevision`：`cf9ad3a4f53008f6a122f55992bea04baa72818d`，不含 `-dirty`。
- 应用回滚：`req-ui-step1-final1 → req-mcp-final5 → req-ui-step1-final1`，两段健康检查通过。
- 部署备份 `RESTORE VERIFYONLY`：通过。
- 独立恢复库：`AIDT_UI_STEP1_FINAL1_RESTORE_20261102_003700`。
- 恢复库：ONLINE、10 条 EF 迁移、62 张应用表、5 张 MCP 表；工作事项六类来源表和
  项目经理流程任务权限均存在。

## 🧪 后端

```powershell
dotnet build AIDT.slnx --no-restore
dotnet test AIDT.slnx
dotnet format AIDT.slnx --verify-no-changes --no-restore
dotnet list AIDT.slnx package --vulnerable --include-transitive
```

- Build：0 警告、0 错误。
- Tests：41/41 通过，0 跳过。
- 覆盖：Domain、Application、SQL 连接、API、权限、数据范围、Core 脱敏、幂等、约束、事务回滚、并发。
- Format：通过。
- NuGet：无已知漏洞。

## 🌐 前端

```powershell
npm ci
npm run typecheck
npm run test
npm run contract:check
npm run build
npm audit --omit=dev
```

- Typecheck：通过。
- Vitest：3/3 通过。
- OpenAPI：28 路径、33 operationId。
- Production build：通过。
- npm audit：0 漏洞。

## 🖥️ 浏览器 E2E

```powershell
npm run test:e2e
```

- `desktop-edge`：创建对象、发起 WF-001、5 个角色审批、对象/流程/审计/Agent 操作通过。
- `mobile-edge-390`：5 个关键页面在 390px 宽度可操作且无页面级横向溢出。
- 最终次数：对 Nginx 测试部署 `http://127.0.0.1:8088` 运行，2/2 通过，0 skipped，0 flaky。

截图：

- [桌面工作台](./test-artifacts/desktop-dashboard.png)
- [桌面流程完成](./test-artifacts/desktop-workflow-completed.png)
- [桌面 Agent](./test-artifacts/desktop-agent.png)
- [移动端 390px](./test-artifacts/mobile-390-agents.png)

JSON/HTML：

- [Playwright JSON](./test-artifacts/playwright-report.json)
- [Playwright HTML](./test-artifacts/playwright-html/index.html)

## 🗄️ 迁移与恢复

- 001 up：34 张应用表。
- 002 up：35 张应用表、`OrganizationId`、`IdempotencyRecord`。
- 002 down：34 张应用表，幂等表移除。
- 001 down：0 张应用表。
- 备份临时库恢复：35 表、13 对象、59 审计、51 Outbox。

## 🚀 部署与离线

- Nginx 8088 ：root/live/ready/OpenAPI 均 200。
- Deploy：demo-a 成功，升级 demo-b 成功，rollback demo-a 成功。
- 最终发布：`AIDT-demo-final3.zip`，SHA256 `684EE630E912AF3B55CF32C3B8464A9850A3F15F540CFE08A04366F4E882643E`。
- 内部服务仅监听 `127.0.0.1`。
- 离线 NuGet：233 包；离线 .NET build 通过。
- 离线 npm：0 漏洞；`npm ci --offline` 与 Vite build 通过。

## ⚠️ 已知未覆盖

- 生产 SSO/MFA 和真实企业组织同步。
- 真实 U9/ERP/DCS/WMS/LIMS/钉钉。
- 高可用、多组织和高并发容量。
- 生产级证书、SPN 和 DBA 维护窗口。
- 会签、或签、委托和 SLA。

## 📦 需求池批次 1（2026-10-31）

### SQL/API 集成

```powershell
$env:AIDT_RUN_SQL_INTEGRATION_TESTS='1'
$env:AIDT_SQL_CONNECTION='Server=np:\\.\pipe\sql\query;Database=AIDT_REQ_BATCH1_TEST_20261031_013442;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5'
dotnet test AIDT.slnx --no-restore
```

- 结果：64/64 通过，0 失败、0 跳过。
- 覆盖部门持久化/成员关系、跨部门对象隐藏、对象/流程/文件环境过滤、
  文件上传下载预览、版本、通用关联、敏感文件拒绝、跨部门拒绝与审计。

### 前端与浏览器

- `npm run typecheck`：通过。
- `npm run test`：5 个文件、7/7 通过。
- `npm run contract:check`：38 路径、46 operationId。
- `npm run build`：通过。
- `npm run test:e2e`：5/5 通过，0 skipped/flaky。
- 覆盖：员工入口、管理导航分层、部门工作区、拖拽/点击上传、预览、版本、
  删除申请、敏感文件拒绝、WF-001 回归、390px 全部关键页面。

### 迁移、恢复和回滚

- 003：隔离库 002→003 up（38 表）→003 down（35 表）→003 up。
- 004：任务责任部门校正，主库不一致数 0。
- 备份：迁移前备份 `RESTORE VERIFYONLY` 通过。
- 恢复：临时库恢复 38 表、4 条迁移，验证后删除临时库。
- 部署回滚：`req-batch1-b → req-batch1-a`，首页与 ready 均 200。
- 最终部署：`req-batch1-final5`；root/API/Worker/AiGateway/Nginx ready 均通过。
- 最终发布包 SHA256：`6DEFDDEB879C2F3CCC73BBD786E4CFDAA1FD3FA3E37E428D6D978B6B6A99353B`。
- 离线：233 NuGet 包、约 45.6 MB npm 缓存，restore/install/build 通过。

### 批次 1 干净发布冻结（`req-batch1-final6`）

- 批次提交：`e37c55d`、`d385c5e`、`4cb4738`、`ed210b7`。
- `main` 非快进合并：`7aa50459acc0f1ec66ba70caa16d82cc574415af`。
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch1-final6.zip`。
- 发布包 SHA256：`008711843CDED2B6F0D0814959FE9B36F432C73FF4277E69E93D5164CE8E4895`。
- manifest：`version=req-batch1-final6`、`sourceRevision=7aa50459acc0f1ec66ba70caa16d82cc574415af`，不含 `-dirty`。
- 部署前备份：`AIDT_PRE_DEPLOY_req-batch1-final6_20261031_092357.bak`；
  SHA256 `F73950AF218642E5F218DA1091452104437C3D9C2215A1CC86CFAB407146F40A`；`RESTORE VERIFYONLY` 通过。
- 健康检查：root/API/Worker/AiGateway/Nginx capabilities 均 200。
- 能力状态：`REQUIREMENT_POOL_BATCH_1_COMPLETE`；`UX-001`、`WS-001`、`DOC-001`、`DATA-001` 均 `available`。
- SQL：4 条 EF 迁移、38 张应用表、6 个部门、任务主部门不一致 0。
- 部署环境 Playwright：5/5 通过，0 failure、0 skipped、0 flaky。

### 批次 2（2026-10-31）

- 隔离库 SQL/API：70/70 通过，0 失败、0 跳过。
- 迁移 005：up/down/re-up；5 条迁移、44 张应用表。
- 前端：6 个测试文件、9/9 通过。
- OpenAPI：53 路径、62 operationId。
- 生产构建：通过。
- 源码与正式部署 Playwright：6/6 通过。
- 覆盖：听记两份以上行动项、人工确认前零任务、幂等确认、任务反查、
  节点策略、SLA 提醒/升级幂等、低风险代理、高风险拒绝、AgentGateway 和 390px。

### 批次 2 最终部署

- 版本：`req-batch2-final2`
- 发布包 SHA256：`339AC0BBF400F378FA0E570960F06EF85B0C97DEA5B3EFA1267B066A4301888E`
- manifest sourceRevision：`875204ed5d7fbc7cce7054959eab8c4fead255b8`
- root/API/Worker/AiGateway：均 200
- `MIN-001`、`WF-005`、`AGT-002`、`AIG-002`：均 `available`
- 应用回滚 `final2 → final1 → final2`：通过
- 最终 Playwright：6/6，0 failure、0 skipped、0 flaky

### 批次 2.1（截至发布前）

- 后端默认：55 通过、21 个 SQL 用例按预期跳过。
- 隔离 SQL/API：76/76 通过；覆盖 DOCX 单元路径、策略发布权限、SLA 提醒/升级、
  预授权自动代理一次、重复扫描幂等、独立服务身份、通知、审计和人工边界拒绝。
- 迁移 006：隔离库 up→down→re-up；6 条迁移、44 张应用表。
- 默认策略：`MATERIALS_CONFIRM`，120/30/60/60，Low/A2，永久人工边界 false。
- 前端：类型检查通过，Vitest 9/9，OpenAPI 56 路径/65 operationId，生产构建通过。
- NuGet/npm：无已知漏洞；`dotnet format --verify-no-changes` 通过。
- final3 正式部署 Playwright：8/8，0 failure、0 skipped、0 flaky。
- 应用回滚：`final3 → final2 → final3`，最终 root/API/Worker/AiGateway 均 200。
- 数据库恢复库：`AIDT_REQ_BATCH2_1_RESTORE_20261031_130335`，6 迁移、44 应用表。

### 批次 3（2026-11-01）

- 默认后端：68 通过、25 个 SQL 用例按预期跳过、0 失败。
- 最终隔离库：`AIDT_REQ_BATCH3_FINAL_20261101_010853`。
- SQL/API、Domain、Contract 全量：96/96 通过，0 失败、0 跳过。
- 覆盖：项目主对象、状态流转、成员/主负责人、里程碑逾期、项目经理数据范围、
  跨部门直接 ID 拒绝、平台管理员跨部门、事件多来源接口、Agent 建议零副作用、
  真人确认、幂等任务、任务完成进度、高风险人工边界、任务来源反查。
- 迁移 007：006 → 007 up（6→7 迁移、44→51 表）→ down（7→6、51→44、
  无 PMO Schema/种子）→ re-up。
- 主库 `AIDT`：007 已应用，数据库 ONLINE、7 条迁移、51 张应用表。
- 历史主对象 `Title` 和 `CreatedByUserId` 独立回填：缺失数均为 0。
- 前端：类型检查通过；Vitest 9/9；OpenAPI 73 路径/85 operationId；生产构建通过；
  `npm audit --omit=dev` 0 漏洞。
- 源码开发环境 E2E：桌面 + 390px 共 11/11 通过，0 failure、0 skipped、0 flaky；
  覆盖项目经理创建项目/成员/里程碑、事件分析零副作用、真人确认任务、高风险边界和移动端。
- `final1` 正式部署复验发现管理员登录 Cookie 过大并经 Nginx 返回 502；
  权限和数据范围改为压缩声明并保留旧 Cookie 兼容，新增 2 项身份解析测试。
- `final1` 包和部署目录保留但未打标签；修复版顺延为 `req-batch3-final2`。
- final2 正式部署 E2E：11/11，0 failure、0 skipped、0 flaky。
- 应用回滚 `final2 → req-batch2-final3 → final2` 成功，最终健康检查全部 200。
- 部署前备份恢复：临时库 `AIDT_REQ_BATCH3_FINAL_RESTORE_20261101_013149`
  为 7 迁移、51 应用表，PROJECT/项目经理/事件 Agent 均存在。

### 批次 3 收尾（2026-11-01）

#### 后端与隔离库

```powershell
$env:AIDT_RUN_SQL_INTEGRATION_TESTS='1'
$env:AIDT_SQL_CONNECTION='Server=np:\\.\pipe\sql\query;Database=AIDT_BATCH3_CLOSEOUT_FINAL_20261101_185000;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=10'
$env:ConnectionStrings__Aidt=$env:AIDT_SQL_CONNECTION
dotnet test AIDT.slnx --no-build --no-restore --logger "console;verbosity=minimal"
```

- 最终隔离库：`AIDT_BATCH3_CLOSEOUT_FINAL_20261101_185000`。
- 结果：115/115 通过，0 失败、0 跳过。
- 构建：0 警告、0 错误；`dotnet format --verify-no-changes` 通过。
- NuGet：所有项目无已知漏洞。
- 新增持久化测试：
  - `Inbox_StatusPersistsAcrossFreshReadsAndSharedDeadline`
  - `ProjectStatusTransitions_PersistVersionProgressAndCompletionGuards`
  - `ProjectFileUpload_AutoLinksProjectEvidenceAndPreservesOtherObjects`
  - `TryExtractDeadline_ParsesExplicitDate`

#### 前端

- `npm run typecheck`：通过。
- `npm run test`：9 个文件、13/13 通过。
- `npm run contract:check`：114 路径、134 operationId。
- `npm run build`：生产构建通过。
- `npm audit --omit=dev`：0 漏洞。

#### 源码 Playwright

- 桌面 + 390px：18/18 通过，0 失败、0 跳过、0 flaky。
- 新增刷新闭环同时验证项目状态、PROJECT 文件证据、事件状态、上海截止时间、
  真人确认按钮消失、项目任务完成和最终进度 100%。

#### 历史数据修护

- 脚本：`database/repairs/BATCH3_CLOSEOUT_DATA_REPAIR_20261101.sql`。
- 隔离库前值：事件异常 24、项目状态审计异常 1、附件关系缺失 0。
- 自动修护：事件 24、项目 1、附件关系 0；修护后异常均为 0。
- 12 条 `Received + HighRisk` → `AwaitingConfirmation`。
- 12 条 `Received + Confirmed` → `Confirmed`。
- `PRJ-3157C-20250808`：`Active v2` → `Completed v3`，进度 100。
- 修护审计 1 条；幂等重跑后仍 1 条，重复附件关系 0。
- 主库最终修护前备份 SHA256：
  `08AF9BE74009DD137ACBDBBB7AB963EF751066920329CE8E525A8A3BA0E368BE`。
- 主库修护后事件/项目/附件关系异常均为 0；幂等复跑重复关系 0。

#### final3 正式发布与恢复

- 发布包 SHA256：`EAD2F5D6135EE5BFB525EEE46B2F57191C528F8FAE38EB1023517D5C86FAB4F6`。
- manifest sourceRevision：`d9ee050df2de473ca68433f1d8dd00b28d06c0a0`，不含 `-dirty`。
- 部署前备份 SHA256：`6091CEEEF32DCAB51DB6F2A649E38C34AE70AB33A2F6686289A8AC56DF665C1B`。
- 正式部署：`req-batch3-final3`，`2026-11-01T18:35:47.702+08:00`。
- 正式 Playwright：18/18，开始于 `2026-11-01T18:35:57.774+08:00`，
  0 failure、0 skipped、0 flaky。
- 页面、API live/ready、Worker、AiGateway、capabilities 均 HTTP 200。
- 应用回滚 `final3 → req-navigation-iam-final2 → final3` 通过。
- 迁移探针库 `AIDT_REQ_BATCH3_CLOSEOUT_MIGRATION_PROBE_20261101_183922`：
  009 down/re-up 由 9/57 → 8/51 → 9/57；`project-manager` 三项审批权限恢复为 3/3。
- 独立恢复库 `AIDT_REQ_BATCH3_CLOSEOUT_FINAL3_RESTORE_20261101_183818`：
  ONLINE、9 迁移、57 表、异常 0、`PRJ-3157C-20250808` 为 `Completed v3 / 100%`。
