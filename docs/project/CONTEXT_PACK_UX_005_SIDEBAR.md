# UX-005 PMS 风格侧边栏与工作台正文重构上下文包

## 🎯 任务

参考 PMS 侧栏和会议模块正文，分阶段完成 AIDT 的应用外壳、侧栏、统一页面框架，
并首批落地“我的工作”和“AI 听记”。

## 🏷️ 任务类型

前端布局、视觉、信息架构和交互重构。默认不改变 API、数据库、权限或业务事务。

## ✅ 验收标准

### UX-005-A

- 桌面侧栏固定，正文和导航分别滚动。
- 导航可滚动到底部，底部操作可达。
- `820px` 以下使用抽屉。

### UX-005-B

- 页面头包含标题、范围、操作和“介绍”。
- 正文不再固定为 `1260px`。
- 运营页统一为统计、筛选、分类、主体顺序。
- 介绍抽屉包含用途、场景、1～2 个例子和安全边界。

### UX-005-C

- 我的工作具备 4 个统计、搜索、筛选、分类和全宽事项列表。
- AI 听记具备 4 个统计、搜索、筛选和固定高度左右分栏。
- 现有分析、保存、确认和事项打开能力不回归。

## 📚 必读文件

1. `AGENTS.md`
2. `docs/001-产品PRD/00-项目目标.md`
3. `docs/001-产品PRD/01-需求基线.md`
4. `docs/001-产品PRD/02-技术栈与架构.md`
5. `docs/001-产品PRD/03-计划方案.md`
6. `docs/001-产品PRD/04-需求池.md` 中 `IDEA-UX-005`
7. `standards/00-AI工程宪法.md`
8. `standards/01-上下文阅读与恢复.md`
9. `standards/04-前端开发规范.md`
10. `standards/06-API与契约规范.md`
11. `standards/08-测试与回归规范.md`
12. `task_plan.md`、`findings.md`、`progress.md`
13. `docs/project/FEATURE_MAP.md`
14. `docs/project/CHANGE_PLAN_UX_005_SIDEBAR.md`
15. `docs/project/IMPACT_ANALYSIS_UX_005_SIDEBAR.md`
16. `prompts/14-PMS风格侧边栏重构目标模式.md`

## 🧩 涉及功能

- `UX-005`
- `UX-001` 我的工作
- `MIN-001` AI 听记
- `NAV-001` 导航数据
- `PLT-002` 权限过滤

## 🖥️ 相关页面和组件

- `frontend/src/layouts/AppLayout.vue`
- `frontend/src/pages/MyWorkPage.vue`
- `frontend/src/pages/MinutesPage.vue`
- `frontend/src/components/workspace/*`（如决定抽取）
- `frontend/src/styles/tokens.css`
- `frontend/src/style.css`

## 🌐 相关 API

- `GET /api/v1/navigation/current`
- `GET/PUT /api/v1/navigation/preferences`
- `GET /api/v1/my-work`
- `/api/v1/minutes*`
- `GET /api/v1/departments`

默认只消费现有接口；C2 实际新增了最小只读听记汇总与筛选契约，
详见 `docs/project/UX_005_DELIVERY.md`。

## 🗄️ 相关数据

- 导航树、导航偏好、用户权限范围；
- 统一工作事项投影；
- 听记来源、分析运行和行动项；
- 部门列表。

不复制业务对象、事项或任务状态。

## 🧪 相关测试

- `frontend/e2e/aidt.spec.ts`
- `frontend/e2e/department-workspace.spec.ts`
- 新增 `frontend/e2e/workspace-layout.spec.ts`
- 新增 AppLayout、介绍抽屉、我的工作、AI 听记专项 Vitest

## 🚫 明确禁止

- 不引入 Ant Design、PMS 源码或第二套组件库。
- 不修改导航权限和业务状态写入逻辑。
- 不把所有页面一次重构完。
- 不按当前页条数伪造全量统计。
- 不用前端隐藏代替后端权限。
- 不将风险边界从操作位置完全删除。
- 不把长说明从一个正文位置简单挪到另一个正文位置；必须进入介绍抽屉或上下文帮助。

## 🔍 当前已知事实

- AIDT `.main-content` 固定为 `1260px`；在 `2304px` 视口下仅占可用正文约 `62%`。
- PMS 会议页使用接近全部可用正文宽度。
- 我的工作已有 4 个统计和 4 类页签，但无搜索和组合筛选。
- AI 听记无统计和筛选，左右栏宽度约 `289px / 851px`，大量记录使页面超过 `13000px`。
- 我的工作右侧 `337px` 说明栏压缩主事项列表。
- 侧栏仍存在 `100vh + overflow:hidden`、`sticky` 失效和窄屏超长问题。

## ❓ 实施时决策

- 基础组件是否抽取及命名；
- 我的工作主体采用表格还是紧凑列表；
- AI 听记列表使用分页、游标加载或虚拟滚动；
- 统计字段能否由现有 API 准确提供；
- 运营页最大宽度采用 `1680px` 还是完全自适应。

决策以真实数据、低耦合和可验证性为准。

## 📤 输出要求

- 按 A/B/C 阶段实施并分别提供测试证据。
- 保持业务动作、权限和审计不变。
- 提供桌面、Codex 侧栏、移动端截图。
- 更新变更文档、功能地图、测试和规划文件。

## ✅ 当前结果

- 源码实现、Vitest、Playwright、生产构建与文档同步已完成。
- 已非快进合并到 `main`，最终发布 `req-ui-ux005-final2` 部署到 8088。
- 8088 正式全量 Playwright `64/64`；主库切回后的 UX-005 烟测 `10/10`。
- 应用回滚、最终数据库备份和独立恢复通过。
- 交付证据：`docs/project/UX_005_DELIVERY.md`；冻结标签 `req-ui-ux005-frozen-v1`。
