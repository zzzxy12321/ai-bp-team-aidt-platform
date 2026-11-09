# UX-005 PMS 风格侧边栏与工作台正文重构交付报告

状态：`complete_deployed_and_frozen`

## 🎯 交付结论

UX-005-A、B、C1、C2 已完成源码、自动化测试、视觉证据和文档同步，
已通过非快进合并交付到 `main`，并部署到 8088。

- 功能分支最终提交：`0bc7053`
- 非快进合并提交：`7f03623`
- 发布修复提交：`59f406d`（窄屏导航加载完成时保留已经打开的抽屉）
- 发布验收证据提交：`0654008`
- 最终发布：`req-ui-ux005-final2`
- 最终部署：8088，`2026-11-05 10:44:03 +08:00`
- 冻结标签：`req-ui-ux005-frozen-v1`

## 🧱 根因与布局策略

1. 旧侧栏使用 `100vh + overflow:hidden`，导航没有独立滚动，外加外层横向
   overflow 上下文，导致导航裁切、正文滚动后侧栏离开视口。
2. 修复为 `100dvh` 固定两列应用壳；主列拆成固定顶栏和独立滚动正文，
   侧栏内部只让中间导航滚动。
3. 宽表此前会把 `.page-shell` 的网格最小内容宽度撑出正文容器，由
   `overflow-x:clip` 裁掉操作列。现统一 `min-width:0/max-width:100%`，
   表格在自身滚动容器内横向滚动，页面本身不产生横向溢出。
4. 运营页移除全局 `1260px` 限制；详情、表单和长文仍可独立限制阅读宽度。

## 🚦 阶段边界

| 阶段 | 提交 | 范围 |
|---|---|---|
| 文档登记 | `809a9fb` | UX-005 变更三件套、目标提示词、功能地图和规划 |
| A | `9dcefe2` | 固定应用壳、浅色侧栏、导航独立滚动、单开部门、窄屏抽屉 |
| B | `ed3c6c9` | 页面头、介绍抽屉、统计条、筛选栏基础组件 |
| C1 | `c60b176` | 我的工作真实统计、全宽列表、搜索、组合筛选、分类、移动卡片 |
| C2 | 单一阶段提交（见 `git log`） | AI 听记只读汇总/筛选、固定高度双栏、测试与最终文档 |
| 发布修复 | `59f406d` | 拆分路由与导航 watcher，修复导航加载完成关闭已打开抽屉的竞态 |
| 发布证据 | `0654008` | 8088 全量验收截图、报告和发布过程证据 |

## 🌐 API 与数据

- 新增 `GET /api/v1/minutes/summary`。
- `GET /api/v1/minutes` 新增只读组合筛选参数。
- 继续复用 `minutes:Read` 和原组织/部门/环境范围过滤。
- 未修改写接口、业务事务、权限语义或审计规则。
- 无数据库迁移，最大迁移仍为 `018`。

## 🧪 正式评审门禁结果

- `npm run typecheck`：通过。
- `dotnet build AIDT.slnx --no-restore`：0 警告、0 错误。
- `dotnet format AIDT.slnx --verify-no-changes --no-restore`：通过。
- 后端/SQL/API：187/187 通过。
- MCP：11/11 通过。
- `npm run test`：30 个文件、67/67 通过（新增抽屉加载竞态回归）。
- `npm run build`：通过。
- `npm run contract:check`：188 路径、209 operationId。
- 源码 Playwright：63/63 通过；修复后发布矩阵与正式 8088 全量回归再次通过。
- 正式 8088 Playwright：64/64 通过，0 失败、0 跳过、0 flaky。
- 最终主库切回后的 UX-005 关键烟测：10/10 通过。
- `npm audit --omit=dev`：0 漏洞。
- NuGet 全项目依赖审计：无已知漏洞。

最终浏览器矩阵覆盖 `1920×1080`、`1440×900`、`1366×768`、`1280×720`、
`1024×768`、`820×900`、`600×518`、`390×844`。Chrome 与 Edge 均执行
`1366×768` 的 100%/125%/150% 和 `1920×1080` 的 100%/125% 缩放矩阵，
验证桌面侧栏固定、导航可滚到底部、部门单开、当前菜单可见、移动抽屉和焦点返回、
完整正文宽度、表格容器横滚、我的工作组合筛选和事项打开、听记筛选与左右独立滚动、
无页面横向溢出。

## 🖼️ 视觉证据

- `docs/project/test-artifacts/ux005-a-desktop-1920-sidebar.png`
- `docs/project/test-artifacts/ux005-a-desktop-1920-scrolled.png`
- `docs/project/test-artifacts/ux005-a-1280x720.png`
- `docs/project/test-artifacts/ux005-a-600x518-codex-drawer.png`
- `docs/project/test-artifacts/ux005-a-390x844-mobile-drawer.png`
- `docs/project/test-artifacts/ux005-c1-desktop-1920.png`
- `docs/project/test-artifacts/ux005-c1-combined-filter-1920.png`
- `docs/project/test-artifacts/ux005-c1-filtered-1920.png`
- `docs/project/test-artifacts/ux005-c1-intro-1920.png`
- `docs/project/test-artifacts/ux005-c1-600x518-codex.png`
- `docs/project/test-artifacts/ux005-c1-390x844-mobile.png`
- `docs/project/test-artifacts/ux005-c2-desktop-1920.png`
- `docs/project/test-artifacts/ux005-c2-filtered-1920.png`
- `docs/project/test-artifacts/ux005-c2-intro-1920.png`
- `docs/project/test-artifacts/ux005-c2-600x518-codex.png`
- `docs/project/test-artifacts/ux005-c2-390x844-mobile.png`

## 🚀 正式发布、回滚与恢复

- 分支 `feature/ux-005-pms-workspace` 以非快进方式合并到 `main`，
  `req-ui-step8-frozen-v1` 保持指向 `7e580f9`，未被移动。
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-ux005-final2.zip`。
- SHA256：`0B13C8B595E549DECC9D566D723A06DF6FDA4931A9CEFEB9F1E5E6F78EF6AD93`。
- manifest `sourceRevision`：`06540088c3f8fae71235231534ea53a2db93b15a`，
  无 `-dirty`，18 条迁移、RuleSimulator、Mock/Disconnected 边界正确。
- final1 已完成首次部署并产生 64/64 全量证据；最终重部署后的关键烟测发现
  导航加载完成会关闭已打开抽屉的竞态，因此不覆盖 final1，改为修复后构建 final2。
- 正式 8088 全量回归为避免主库累计 E2E 数据导致长事务 SQL 取消，临时将
  8088 后端连接到由 `AIDT_DEPT_WF_001_FINAL3_FROZEN_20261104_0250.bak`
  恢复的 `AIDT_UX005_FINAL2_E2E_20261105`；Nginx 入口仍为 `127.0.0.1:8088`，
  主库未被覆盖。完成 64/64 后已切回主库。
- 应用回滚：`req-ui-ux005-final2 → req-ui-ux005-final1 → req-ui-ux005-final2`
  通过；每次页面、API、Worker、AiGateway、MCP 健康检查均为 200。
- 最终部署前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-ux005-final2_20261105_104353.bak`，
  SHA256 `D87DCA1FA8B7A57E16E1A1A6A8FB8A82E4EB77070DF7F0EC66888D92A2D02505`。
- 最终主库备份：
  `C:\ProgramData\AIDT\Backups\AIDT_UX005_FINAL2_FROZEN_20261105_1045.bak`，
  SHA256 `E482EF7FB8F47CD49DAC1B98C503B3D3D6575110D85D9CCE2B9628AADDA7ADE1`；
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库：
  `AIDT_UX005_FINAL2_FROZEN_RESTORE_20261105_1045`，ONLINE、18 条迁移、
  71 张应用表、7 个部门默认工作区。

### 视觉证据

- 1920 桌面侧栏与滚动固定：
  `docs/project/test-artifacts/ux005-a-desktop-1920-sidebar.png`、
  `ux005-a-desktop-1920-scrolled.png`
- 1366 的 100/125/150%、1280、820、600×518、390×844：
  `docs/project/test-artifacts/ux005-release-edge-*.png` 与
  `ux005-release-chrome-*.png`
- 我的工作：`ux005-c1-desktop-1920.png`、`ux005-c1-combined-filter-1920.png`、
  `ux005-c1-intro-1920.png`
- AI 听记：`ux005-c2-desktop-1920.png`、`ux005-c2-filtered-1920.png`、
  `ux005-c2-intro-1920.png`
- 无横向溢出和表格内部滚动由 `release-matrix.spec.ts` 自动断言并保留 JSON/HTML 报告。

## ⚠️ 剩余风险

- 听记当前仍为手动导入和 RuleSimulator；“钉钉断连、真实模型未启用”已明确标注。
- 主库长期累积自动化历史数据后，一次全量回归可能触发本地 SQL Server 瞬时取消；
  本次通过恢复冻结基线库隔离全量 E2E，并保留该运维风险，不做数据删除。
- 前端构建保留既有的单入口 chunk 超过 500 kB 提示；本次未做路由级拆包。
- 本地 Cookie + CSRF 仍是 Demo 身份，不代表生产 SSO；如需服务器试点，需确认
  网络、身份源、备份和告警边界。

## ✅ 评审建议

已达到可进入服务器试点评审的前端标准；服务器试点仍受 Demo 身份和本地回环部署限制。
后续评审重点：

1. 侧栏抽屉和焦点管理；
2. 我的工作筛选语义与分页；
3. 听记只读汇总口径；
4. 固定高度双栏在真实数据量下的体验；
5. 后续是否将相同页面头/介绍/筛选组件推广到其他运营页。
