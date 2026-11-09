# 任务计划：AIDT Vibecoding 成熟开发规范体系

## 当前权威状态：UX-006 全站主栏目页面介绍与具体业务示例（2026-11-05）

- 状态：`complete_deployed_and_frozen`
- 分支：`feature/ux-006-column-introductions`
- 基线：`main@d22e7d0`
- 当前部署：`req-ui-ux005-final3`
- 前置冻结：`req-ui-ux005-frozen-v1`、`req-ui-step8-frozen-v1`
- 需求：`IDEA-UX-006` / `UX-006`，P1，`approved`
- 变更计划：`docs/project/CHANGE_PLAN_UX_006.md`
- 影响分析：`docs/project/IMPACT_ANALYSIS_UX_006.md`
- 上下文包：`docs/project/CONTEXT_PACK_UX_006.md`

### 阶段门禁

1. [x] 从故障会话恢复当前工作树，保留已有修改与测试证据
2. [x] 核对 17 个主栏目均接入集中介绍目录和共享页面头
3. [x] 核对目录包含用途、岗位时机、3—5 步路径、两个例子、数据/权限/风险/下一步
4. [x] 修复页面头环境语义、适用岗位标签和审计页只过滤最近 100 条的问题
5. [x] frontend typecheck、Vitest 72/72、生产构建和 OpenAPI 检查通过
6. [x] 当前代码全量 Playwright：68 通过、0 失败、2 个部署专用跳过
7. [x] backend build/format、SQL/API 与 MCP 全量 198/198 通过
8. [x] npm、NuGet 依赖审计通过
9. [x] 更新需求池、功能地图、交付报告和测试证据
10. [x] 提交并非快进合并到 `main`（`7fc1be7` → `2d9ba09`）
11. [x] 从干净合并提交构建 `req-ui-ux006-final1`
12. [x] 部署 8088、正式全量 E2E 70/70 与视觉矩阵通过
13. [x] 应用回滚 `ux006 → ux005-final3 → ux006`、备份与独立恢复验证通过
14. [x] 创建 `req-ui-ux006-frozen-v1`，历史标签保持不变

### 当前下一步

无。UX-006 已完成正式部署、回滚、恢复和冻结。

## 当前权威状态：UX-005 PMS 风格侧边栏与工作台正文重构（2026-11-05）

- 状态：`complete_deployed_and_frozen`
- 分支：`main`
- 基线：`main@7e580f9`
- 当前部署：`req-ui-ux005-final2`
- 发布源码：`main@0654008`
- 冻结标签：`req-ui-ux005-frozen-v1`
- 需求：`IDEA-UX-005` / `UX-005`，P1，`approved`
- 变更范围：前端应用壳、侧栏、统一工作台正文框架及“我的工作 / AI 听记”样板；
  默认无 API、数据库、权限或审计变更
- 变更计划：`docs/project/CHANGE_PLAN_UX_005_SIDEBAR.md`
- 影响分析：`docs/project/IMPACT_ANALYSIS_UX_005_SIDEBAR.md`
- 上下文包：`docs/project/CONTEXT_PACK_UX_005_SIDEBAR.md`
- 目标模式提示词：`prompts/14-PMS风格侧边栏重构目标模式.md`

### 阶段门禁

1. [x] 使用 Computer Use 复现桌面、Codex 窄侧栏和正文固定窄宽问题
2. [x] 对比 PMS 侧栏、会议统计、搜索筛选和全宽主体布局
3. [x] 登记扩展后的需求、变更计划、影响分析和上下文包
4. [x] 整合编制 A/B/C 三阶段目标模式提示词
5. [x] A：固定应用壳、导航独立滚动、当前部门单开和窄屏抽屉
6. [x] B：页面头、介绍抽屉、统计条、筛选栏和全宽运营页基础组件
7. [x] C1：改造“我的工作”，加入真实统计、搜索、组合筛选和分类
8. [x] C2：改造“AI 听记”，加入真实统计、搜索、筛选和独立滚动双栏
9. [x] 完成 1920、1440、1280、600×518、390px 验证
10. [x] 更新功能地图、规划文件和交付证据

### 当前下一步

无。UX-005 已完成发布、回滚验证、数据库恢复验证和冻结。

### 正式发布门禁（2026-11-05）

1. [x] 复核 UX-005 源码、API 权限/范围和现有测试
2. [x] 补齐 1366、1024、820 与缩放矩阵，Chrome/Edge 双浏览器
3. [x] backend build、format、后端单测、SQL/API、MCP、OpenAPI
4. [x] 前端 typecheck、Vitest、构建、全量 E2E 与 UX-005 专项
5. [x] npm 生产依赖和 NuGet 漏洞审计
6. [x] 非快进合并至 `main`（`7f03623`），发布前抽屉竞态修复（`59f406d`）
7. [x] 从干净提交 `0654008` 构建 `req-ui-ux005-final2`
8. [x] 部署 8088，正式 E2E 64/64，应用回滚 `final2 → final1 → final2`
9. [x] 最终数据库备份、`RESTORE VERIFYONLY` 和独立恢复
10. [x] 文档证据、部署记录和冻结标签 `req-ui-ux005-frozen-v1`

### 实施决策

- 应用壳使用 `100dvh` 固定两列网格，主列内部再分为固定顶栏和独立滚动正文；
  侧栏不依赖 sticky，而由固定高度的网格列和内部 `nav` 滚动共同保证位置。
- 侧栏改为 AIDT 绿色品牌的浅色工作台面板；菜单密度、分组色条和二级层级线
  只借鉴 PMS 的信息结构。
- B 只抽取页面头、介绍抽屉、统计条和筛选栏四类基础组件，不把两页压成同一模板。
- “我的工作”四项统计直接使用现有服务端全量汇总；组合筛选消费完整聚合集。
- “AI 听记”当前契约不能准确汇总待确认行动项，也不支持服务端筛选；C2 增加
  最小只读汇总/筛选能力，不改变任何写接口、业务事务、权限或审计语义。

## 当前权威状态：DEPT-WF-001 / 批次 8 部门工作空间与可视化流程设计器（2026-11-03）

- 状态：`complete_deployed_and_frozen`
- 分支：`main`（8A/8B/8C/8D 四个连续逻辑分支已非快进合并）
- 基线：`main@3e94f3a`
- 当前部署：`req-ui-step8-final3`
- 代码最大迁移：018；主库已应用 018
- 正式发布：`req-ui-step8-final3`（`final1`/`final2` 未通过门禁，保留追溯）
- 目标冻结标签：`req-ui-step8-frozen-v1`
- 需求：`IDEA-DEPT-WF-001` / `DEPT-WF-001`，P0，`approved`

### 阶段门禁

1. [x] 核实固定仓库、干净 main、7/7A 标签、当前部署和 017 基线
2. [x] 完整读取产品基线、规范、规划、功能地图和关键交付
3. [x] 创建 8A 分支、变更三件套并登记需求
4. [x] 8A：只读数据盘点、备份、测试隔离、部门导航和会话权限刷新
5. [x] 8B：流程模板、节点模型、动态表单、任务冻结版本和迁移 018
6. [x] 8C：可视化流程设计器、运行时表单和三档响应式
7. [x] 8D 源码侧：四部门试点、全量回归
8. [x] 四段连续提交、非快进合并主库并应用、备份主库 018
9. [x] 8D 发布侧：部署、正式 E2E、回滚、独立恢复和冻结

### 当前下一步

无。DEPT-WF-001 已完成、部署、回滚验证、恢复验证并冻结。

## 当前权威状态：SCN-001 / 7B 岗位业务场景中心前端与试用闭环（2026-11-03）

- 状态：`complete_deployed_and_frozen`
- 分支：`feature/ui-step7b-scenario-center-ui`
- 基线：`main@1b33a9f`
- 目标发布：`req-ui-step7-final1`
- 目标冻结标签：`req-ui-step7-frozen-v1`
- 前置冻结：`req-ui-step6-frozen-v1`、`req-ui-step7a-frozen-v1`
- 当前部署：`req-ui-step7a-final1`
- 最大迁移：017

### 阶段门禁

1. [x] 核对标签、干净工作区、部署健康、017 和六场景真实后端
2. [x] 创建功能分支、变更计划、影响分析和上下文包
3. [x] 新增当前用户场景启动只读聚合 API
4. [x] 实现 `/scenarios` 首页、详情、七步启动向导和我的场景
5. [x] 配置中心、岗位工作台、统一待办和证据深链联动
6. [x] 覆盖状态、错误、Demo、数字员工和永久人工边界
7. [x] 类型检查、Vitest、后端/SQL/API、MCP、OpenAPI、源码 E2E
8. [x] 干净构建、正式部署、50 项正式 E2E
9. [x] 应用回滚、数据库备份和独立恢复
10. [x] 更新交付、测试证据、功能地图、权限矩阵、需求池并冻结

### 当前下一步

无。SCN-001-B 已部署并冻结。

## 当前权威状态：SCN-001 / 7A 岗位业务场景目录与场景启动后端（2026-11-03）

- 状态：`complete_deployed_and_frozen`
- 分支：`feature/ui-step7a-scenario-catalog-backend`
- 目标发布：`req-ui-step7a-final1`
- 冻结标签：`req-ui-step7a-frozen-v1`
- 需求：`IDEA-SCN-001` / `SCN-001`，P0，`approved`

### 阶段门禁

- [x] 读取 L0/L1 规划、规范、功能地图、权限矩阵和模块 README
- [x] 核对 main、6A/6B 冻结标签、当前部署和最大迁移 016
- [x] 建立变更计划、影响分析和上下文包
- [x] 场景 Domain、DTO、端口和 SQL 服务
- [x] 场景 API、权限、迁移 017 和六类真实模板
- [x] 单元、EF、SQL/API、MCP、前端、OpenAPI 和源码 E2E
- [x] 提交、合并、干净构建和正式部署
- [x] 正式 E2E、应用回滚、数据库备份恢复和冻结

### 当前下一步

SCN-001-A 已完成并可进入 7B；下一阶段建设场景中心、启动向导和场景管理页面。

## 当前权威状态：AGT-003 / 6B 数字员工身份中心前端与业务联动（2026-11-03）

- 状态：`AGT_003_B_COMPLETE_DEPLOYED_AND_FROZEN`
- 功能：`AGT-003 / 6B`
- 功能分支：`feature/ui-step6b-agent-identity-ui`
- 基线：`main@337dd51`
- 前置冻结：`req-ui-step5-frozen-v1`、`req-ui-step6a-frozen-v1`
- 当前部署：`req-ui-step6-final1`
- 目标发布：`req-ui-step6-final1`
- 目标冻结标签：`req-ui-step6-frozen-v1`
- 变更计划：`docs/project/CHANGE_PLAN_AGT_003_B.md`
- 上下文包：`docs/project/CONTEXT_PACK_AGT_003_B.md`
- 影响分析：`docs/project/IMPACT_ANALYSIS_AGT_003_B.md`

### 阶段门禁

1. [x] 核对 5-final、6A 冻结、干净工作区、6A 交付和最大迁移
2. [x] 审查现有 Agent 页面、配置中心、MCP、API 与前端契约
3. [x] 创建独立功能分支和变更三件套
4. [x] 补齐 Agent options/context 只读 API 和最近运行/MCP 身份信息
5. [x] 重写 `/agents` 身份中心、七段表单、生命周期和高风险审核交互
6. [x] 打通流程配置、MCP、AgentRun、深链和三档响应式
7. [x] 新增 016 权限迁移、单元/Vitest/OpenAPI 和源码 E2E
8. [x] 后端/SQL/API、MCP 全量回归和干净构建
9. [x] 正式部署 41+ 项 E2E、应用回滚、备份与恢复
10. [x] 交付文档、功能地图和冻结标签

### 当前下一步

无。AGT-003-B 已部署并冻结。

## 当前权威状态：AGT-003 / 6A 数字员工身份治理后端（2026-11-03）

- 状态：`AGT_003_A_COMPLETE_DEPLOYED_AND_FROZEN`
- 功能：`IDEA-AGENT-003 / AGT-003 / 6A`
- 功能分支：`feature/ui-step6a-agent-profile-backend`
- 基线：`main@dc0c945`
- 前序冻结：`req-ui-step5a-frozen-v1`、`req-ui-step5-frozen-v1`
- 当前部署：`req-ui-step5-final1`
- 目标发布：`req-ui-step6a-final1`
- 目标冻结标签：`req-ui-step6a-frozen-v1`
- 变更计划：`docs/project/CHANGE_PLAN_AGT_003_A.md`
- 上下文包：`docs/project/CONTEXT_PACK_AGT_003_A.md`
- 影响分析：`docs/project/IMPACT_ANALYSIS_AGT_003_A.md`

### 阶段门禁

1. [x] 读取产品、规范、规划、功能地图、权限矩阵和前序交付
2. [x] 核对分支、标签、部署、014 和现有 Agent/MCP/IAM/Audit 实现
3. [x] 修正 META-001-B 完成状态与证据
4. [x] 实现领域身份主数据、不可变版本和生命周期规则
5. [x] 实现治理 API、权限、双人审核、幂等和并发
6. [x] 新增 015、数据约束、存量回填和 DBA 审核
7. [x] 打通运行、流程、MCP 令牌、停用撤销和审计/Outbox
8. [x] 单元、SQL/API、MCP、OpenAPI、前端契约和全量回归
9. [x] 迁移 up/down/re-up、干净构建、部署、正式 E2E
10. [x] 应用回滚、数据库备份恢复、交付文档和冻结

### 当前下一步

无。AGT-003-A 已完成、部署并冻结；6B 需按目标文件另行开工。

## 当前权威状态：META-001-B 业务对象建模与动态表单前端（2026-11-02）

- 状态：`META_001_B_COMPLETE_DEPLOYED_AND_FROZEN`
- 功能：`IDEA-META-001 / META-001-B`
- 功能分支：`feature/ui-step5b-object-modeling-ui`
- 基线：`main@fccadfc`
- 前序冻结：`req-ui-step5a-frozen-v1`（`fccadfc`）
- 当前部署：`req-ui-step5a-final1`
- 目标发布：`req-ui-step5-final1`
- 目标冻结标签：`req-ui-step5-frozen-v1`
- 变更计划：`docs/project/CHANGE_PLAN_META_001_B.md`
- 上下文包：`docs/project/CONTEXT_PACK_META_001_B.md`
- 影响分析：`docs/project/IMPACT_ANALYSIS_META_001_B.md`

### 阶段门禁

1. [x] 核对干净 `main@fccadfc`、5A 冻结和当前部署
2. [x] 创建 `feature/ui-step5b-object-modeling-ui`
3. [x] 登记需求并创建计划、上下文包、影响分析
4. [x] 补齐业务对象版本/上下文/用户候选读取能力
5. [x] 实现业务建模工作室和字段设计器
6. [x] 升级动态对象创建、详情、编辑、版本和关联
7. [x] 打通流程配置、岗位卡片和草稿隔离
8. [x] 单元、Vitest、SQL/API、OpenAPI 和源码 E2E
9. [x] 干净构建、部署、正式 E2E、回滚和恢复
10. [x] 交付文档和冻结

### 断点规则

- 上下文耗尽前提交完整改动，并把断点同步到 `findings.md`、`progress.md`。
- 未完成正式门禁时不得宣称发布或冻结。

## 当前权威状态：META-001-A 业务对象建模与字段编辑后端（2026-11-02）

- 状态：`META_001_A_COMPLETE_DEPLOYED_AND_FROZEN`
- 功能：`IDEA-META-001 / META-001-A`
- 功能分支：`feature/ui-step5a-object-modeling-backend`
- 基线：`main@9b425a3`
- 前序冻结：`req-ui-step4-frozen-v1`（`756ae40`）
- 当前部署：`req-ui-step4-final1`
- 目标发布：`req-ui-step5a-final1`
- 目标冻结标签：`req-ui-step5a-frozen-v1`

### 阶段门禁

1. [x] 核对干净 `main@9b425a3`、前序冻结和当前部署
2. [x] 从最新 `main` 创建功能分支
3. [x] 登记需求池并创建计划、上下文包、影响分析
4. [x] 实现对象模型生命周期、字段定义和业务对象更新
5. [x] 新增并验证 013
6. [x] 单元、SQL/API、MCP、OpenAPI 和源码 E2E
7. [x] 干净构建、部署、正式 E2E
8. [x] 应用回滚、数据库恢复、交付文档和冻结

### 断点规则

- 上下文耗尽前提交完整改动，并把断点同步到 `findings.md`、`progress.md`。
- 未完成正式门禁时不得宣称发布或冻结。

## 当前权威状态：WS-002 可配置岗位工作台模板（2026-11-02）

- 状态：`WS_002_COMPLETE_DEPLOYED_AND_FROZEN`
- 功能：`WS-002` 可配置岗位工作台模板
- 功能分支：`feature/ui-step4-role-workbench-templates`
- 基线：`main@67fa2a8`
- 前序冻结：`req-ui-step3-frozen-v1`（`c97a7e8`）
- 当前部署：`req-ui-step3-final1`
- 目标发布：`req-ui-step4-final1`
- 目标冻结标签：`req-ui-step4-frozen-v1`
- 变更计划：`docs/project/CHANGE_PLAN_WS_002.md`
- 上下文包：`docs/project/CONTEXT_PACK_WS_002.md`
- 影响分析：`docs/project/IMPACT_ANALYSIS_WS_002.md`

### WS-002 阶段门禁

1. [x] 核对干净 `main@67fa2a8`、前序冻结和当前部署
2. [x] 从最新 `main` 创建独立功能分支
3. [x] 登记需求池并完成计划、上下文包、影响分析
4. [x] 新增 012 迁移、模板/版本/卡片/分配模型与 6 个系统模板
5. [x] 实现服务端模板解析、权限过滤、真实卡片聚合和配置/发布/恢复 API
6. [x] 重构部门工作区为岗位卡片首页，并在 IAM 增加模板配置
7. [x] 完成单元、SQL/API、Vitest、OpenAPI、源码专项 E2E
8. [x] 隔离库 up/down/re-up、主库应用和恢复验证
9. [x] 非快进合并、干净构建 `req-ui-step4-final1`、部署、正式 E2E 37/37
10. [x] 应用回滚、部署备份恢复、交付文档和 `req-ui-step4-frozen-v1`

### 当前下一步

无。WS-002 已完成、部署并冻结；后续增强必须新建需求批次。

### 断点恢复规则

- 上下文耗尽前必须提交完整改动，并把断点同步到 `task_plan.md`、`findings.md`、`progress.md`。
- 未完成正式门禁时不得宣称发布或冻结。

## 当前权威状态：PMO-003 项目操作闭环（2026-11-02）

- 状态：`PMO_003_COMPLETE_DEPLOYED_AND_FROZEN`
- 功能：`PMO-003` 项目操作闭环
- 功能分支：`feature/ui-step3-project-operations`
- 基线：`main@fa1a6d4`
- 前序冻结：`req-ui-step2-frozen-v1`
- 当前部署：`req-ui-step2-final1`
- 目标发布：`req-ui-step3-final1`
- 目标冻结标签：`req-ui-step3-frozen-v1`
- 变更计划：`docs/project/CHANGE_PLAN_PMO_003.md`
- 交付报告：`docs/project/PMO_003_DELIVERY.md`

### 本段范围

1. [x] 核对干净 `main@fa1a6d4`、前序冻结和独立分支
2. [x] 登记 PMO-003 变更计划、上下文包和影响分析
3. [x] 项目编辑、负责人更换、成员角色/移除
4. [x] 直接创建项目任务、责任人和上海期限
5. [x] 文件、听记、流程实例和业务对象证据关联
6. [x] 进度重算、合法状态流转和刷新持久化
7. [x] 隔离库后端/SQL/API/MCP 146/146
8. [x] Vitest 27/27、OpenAPI 126/146、三端闭环 E2E
9. [x] 提交、非快进合并、干净构建和正式部署
10. [x] 正式 E2E、应用回滚、数据库恢复和冻结标签

### 当前下一步

无。PMO-003 已完成、部署并冻结；后续增强必须新建需求批次。

### 正式交付结果

- 功能提交：`cd81f0e`、`2f85e7c`、`01df3dd`；非快进合并 `e8db2b2`。
- 发布包 SHA256：
  `B50F0A19AF78EC2EAE4EAE4C811E6F482B92B80445E565F4CCB7F2502DA04541`。
- 正式 E2E：32/32，0 失败、0 跳过、0 flaky。
- 应用回滚：`final1 → req-ui-step2-final1 → final1` 通过。
- 最终部署备份 SHA256：
  `C964476E04205F814967B9CA0DB2967F5039BBCF8291127CAF0CC5AA0601FEFD`。
- 独立恢复库：`AIDT_UI_STEP3_FINAL1_RESTORE_20261102_1335`，11 迁移、62 表。

## 当前权威状态：UX-004-B 统一确认中心（2026-11-02）

- 状态：`UX_004_B_COMPLETE_DEPLOYED_AND_FROZEN`
- 功能分支：`feature/ui-step2b-confirmation-center`
- 合并提交：`main@931f9531f71d58b189fb83941e30195edd30f6ef`
- 基线：`main@48a1a10669d711952ec1dd8c373f054fdee3d986`
- 前序冻结：`req-ui-step2a-frozen-v1`
- 当前部署：`req-ui-step2-final1`
- 正式发布：`req-ui-step2-final1`
- 冻结标签：`req-ui-step2-frozen-v1`
- 变更计划：`docs/project/CHANGE_PLAN_UX_004_B.md`
- UX-004 已标记 `implemented / frozen`。

### 本段范围

1. [x] 核对最新 `main`、2A 冻结标签并创建独立分支
2. [x] 听记行动项的负责人、期限、对象、项目和流程持久化
3. [x] MCP 业务化确认、意见必填和技术信息折叠
4. [x] 四类来源统一确认页与工作台深链
5. [x] 单元、SQL/API、Vitest、OpenAPI、三端 E2E
6. [x] 迁移、发布、部署、回滚、恢复和冻结

### 当前下一步

无。UX-004-B 已完成、部署并冻结；后续增强必须新建需求批次。

### 正式交付结果

- 功能提交：`95ddeb3`、`f69cf15`、`8d3afbd`；非快进合并 `931f953`。
- 发布包 SHA256：
  `4832A05B0350D79E3C33E2438B89FE980A979D5D6A4508ED1F3D3AFD50F2C189`。
- 正式 E2E：29/29，0 失败、0 跳过、0 flaky。
- 应用回滚：`final1 → req-ui-step2a-final1 → final1` 通过。
- 最终部署备份 SHA256：
  `0DB5198B0F3B5581FF128ADF5F56109E0E4E0DD5D6A05E6B620F2373B1728C4C`。
- 独立恢复库：`AIDT_UI_STEP2_FINAL1_RESTORE_20261102_1220`，11 迁移、62 表。
- 冻结标签：`req-ui-step2-frozen-v1`。

## 当前权威状态：UX-004-A 事件确认与流程发起（2026-11-02）

- 状态：`UX_004_A_COMPLETE_DEPLOYED_AND_FROZEN`
- 分支：`feature/ui-step2a-event-confirmation`
- 基线：`main@62052fb8f422550f503d4383f72f5d58759ac2a6`
- 前序冻结：`req-ui-step1-frozen-v1`
- 当前部署：`req-ui-step1-final1`
- 正式发布：`req-ui-step2a-final1`
- 目标冻结标签：`req-ui-step2a-frozen-v1`
- 变更计划：`docs/project/CHANGE_PLAN_UX_004_A.md`
- 本段只完成事件确认与流程发起，不代表 UX-004 其他人工确认来源完成。

### 本段范围

1. [x] 核对干净 `main@62052fb`、前序冻结标签和独立分支
2. [x] 将 `CHANGE_PLAN_UX_003.md` 状态修正为完成、部署并冻结
3. [x] 在需求池登记 `IDEA-UX-004 / UX-004`
4. [x] 服务端原子编排与权限/审计
5. [x] 前端可编辑确认、候选名称和影响预览
6. [x] 单元、SQL/API、Vitest、OpenAPI、桌面/移动 E2E
7. [x] 全量门禁、发布、部署、回滚、恢复和冻结

### 当前下一步

无。UX-004-A 已完成、部署并冻结；后续人工确认来源另开分段。

### 正式交付结果

- 合并提交：`ed78d4813d5842d78e41646dda707a6ba150f9eb`。
- 发布包 SHA256：`C0179C5618C67F95BABB49A0254F0F9FD69A1D1DF2617AB4DDC5252C020CB553`。
- 正式 E2E：26/26。
- 应用回滚、数据库恢复均通过。
- 交付报告：`docs/project/UX_004_A_DELIVERY.md`。

## 历史权威状态：UX-003 统一工作事项中心（2026-11-01）

## 当前权威状态：UX-003 统一工作事项中心（2026-11-01）

- 状态：`UX_003_COMPLETE_DEPLOYED_AND_FROZEN`
- 分支：`feature/ui-step1-unified-work-center`
- 基线：`main@6a378f5`
- 前序冻结：`req-mcp-frozen-v1`、`req-batch3-closeout-frozen-v1` 等标签存在
- 目标发布：`req-ui-step1-final1`
- 目标冻结标签：`req-ui-step1-frozen-v1`
- 设计参考：`design/aidt-frontend-product-design`，只参考，不照搬。

### 本批范围

1. [x] 核对 Git 基线、前序标签和独立分支
2. [x] 登记 `UX-003` 到需求池
3. [x] 建立统一只读 `WorkItemView` 契约、状态映射、去重和排序规则
4. [x] 扩展 `/api/v1/my-work` 聚合六类待处理来源并执行权限/部门/环境过滤
5. [x] 重构“我的工作”为今日焦点、待我处理、我发起、我参与、需要关注和最近来源证据
6. [x] 打通六类事项的现有详情深链
7. [x] 补单元、SQL/API、Vitest、OpenAPI 和桌面/390px E2E
8. [x] 全量门禁、隔离部署、正式部署、回滚、恢复和交付文档
9. [x] 合并 `main`、创建 `req-ui-step1-frozen-v1`

### 变更影响分析

| 范围 | 预期变更 | 验证 |
|---|---|---|
| Application 契约 | 新增 `WorkItemViewDto`、`MyWorkSummaryDto`，调整 `MyWorkDto` | 编译、DTO 测试、OpenAPI |
| Application 规则 | 新增纯函数状态映射、排序桶、去重规则 | 单元测试 |
| Infrastructure | `/my-work` 聚合 WorkflowTask、ProjectTask、EventInbox、MinutesActionItem、McpHumanConfirmation、AgentRun | SQL/API 集成测试 |
| 权限 | 按 dashboard + 各来源读取/确认权限、组织/部门/项目/环境范围过滤 | 无权限与跨部门测试 |
| 前端 | 新统一事项卡；重构 MyWork；Inbox/Minutes/Agents 支持查询深链 | 类型、Vitest、桌面/390px E2E |
| API | 扩展 `GET /api/v1/my-work` 响应；无新增写接口 | OpenAPI、契约测试 |
| 数据库 | 只读聚合现有表，预计无迁移；若发现必要索引先评估新的 011 | 迁移检查、SQL 查询 |
| 审计 | 本批聚合为只读，不写业务状态；详情页原动作审计继续生效 | 回归 |

### 明确不修改

- 不修改外部 U9、ERP、DCS、钉钉或真实模型连接。
- 不改变既有事件确认、项目任务、听记确认、MCP 确认和 Agent 确认的业务语义。
- 不直接修改历史迁移、标签或发布包。
- 不扩展与 `UX-003` 无关的页面重构。

### 源码验收结果

- 后端构建：0 警告、0 错误。
- SQL/API 集成：127/127；MCP：11/11；全部 0 失败、0 跳过。
- 后端格式复验：通过；NuGet 依赖：无已知漏洞。
- 前端类型检查：通过；Vitest：11 个文件、16/16。
- OpenAPI：122 路径、142 operationId；生产构建：通过。
- npm 生产依赖审计：0 漏洞。
- 源码 Playwright：22 通过、1 个部署专用 MCP OAuth 用例跳过、0 失败、0 flaky；
  覆盖 1440、1280 和 390px。

### 当前下一步

无。`UX-003` 已完成、部署并冻结；后续增强必须新建需求批次。

### 正式交付结果

- 功能提交：`a7b2356`。
- 非快进合并：`cf9ad3a4f53008f6a122f55992bea04baa72818d`。
- 正式发布：`req-ui-step1-final1`。
- 发布包 SHA256：`B38C51ED44EC9E40719A3DC5B65A0212AC47F1AF3B8CE1D5F1C533E498834D4E`。
- 正式部署 Playwright：23/23，0 失败、0 跳过、0 flaky。
- 应用回滚：`req-ui-step1-final1 → req-mcp-final5 → req-ui-step1-final1` 通过。
- 独立恢复库：`AIDT_UI_STEP1_FINAL1_RESTORE_20261102_003700`，
  ONLINE、10 迁移、62 张应用表、5 张 MCP 表。
- 冻结标签：`req-ui-step1-frozen-v1`。
- 交付报告：`docs/project/UX_003_DELIVERY.md`。

## 当前权威状态：AIDT 标准 MCP 服务（2026-11-01）

- 状态：`MCP_001_COMPLETE_DEPLOYED_AND_FROZEN`
- 基线：`main@509e62f`
- 分支：`feature/mcp-001-standard-server`
- 当前部署：`req-batch3-final3`
- 当前冻结标签：`req-batch3-closeout-frozen-v1`
- 正式需求：`MCP-001` / `IDEA-MCP-001`
- 用户已有未提交需求池更新：保留，并作为本任务正式来源。
- 最终发布：`req-mcp-final5`
- 目标冻结标签：`req-mcp-frozen-v1`
- 协议 SDK：`ModelContextProtocol.AspNetCore` / `ModelContextProtocol` `2.3.0`
- 架构决策：`docs/adr/ADR-MCP-001-standard-mcp-server.md`
- 变更计划：`docs/project/CHANGE_PLAN_MCP_001.md`
- 上下文包：`docs/project/CONTEXT_PACK_MCP_001.md`
- 影响分析：`docs/project/IMPACT_ANALYSIS_MCP_001.md`
- 强制边界：不暴露 SQL、Shell、任意文件系统或任意 HTTP；不把 114 个 REST 路径逐一包装。

### MCP 工作包状态

1. [x] 冻结协议、OAuth、工具目录与 ADR
2. [x] 新增 MCP Schema、迁移 010、权限与导航种子
3. [x] 实现客户端、授权码、令牌、撤销、真人确认和调用审计
4. [x] 实现 `AIDT.Mcp` HTTP/stdio、工具与 Resources
5. [x] 实现 MCP 管理 API、服务身份令牌与 Vue 管理页面
6. [x] 完成单元、SQL/API、协议、权限和源码浏览器回归
7. [x] 构建 `req-mcp-final5`、部署、E2E、应用回滚和数据库恢复
8. [x] 更新契约、功能地图、交付报告并创建 `req-mcp-frozen-v1`

### MCP 当前下一步

全部完成；后续新增 MCP 能力必须新建需求批次和 ADR，不得修改冻结基线。

## 当前权威状态：需求池批次 3 收尾

- 状态：`REQUIREMENT_POOL_BATCH_3_CLOSEOUT_COMPLETE_DEPLOYED_AND_FROZEN`
- 分支：`fix/requirement-pool-batch-3-closeout`
- 基线：`main@93c4cd41129cf92033c4e0a109bee967a21a5552`
- 历史标签：`req-batch3-final2`，不得移动。
- 当前部署：`req-navigation-iam-final2`
- 目标发布：`req-batch3-final3`
- 目标冻结标签：`req-batch3-closeout-frozen-v1`
- 收尾计划：`docs/project/CHANGE_PLAN_REQUIREMENT_POOL_BATCH_3_CLOSEOUT.md`
- 收尾上下文包：`docs/project/CONTEXT_PACK_REQUIREMENT_POOL_BATCH_3_CLOSEOUT.md`
- 收尾影响分析：`docs/project/IMPACT_ANALYSIS_REQUIREMENT_POOL_BATCH_3_CLOSEOUT.md`
- 当前历史异常：事件状态不一致 24 条、项目状态审计不一致 1 条、PROJECT 附件关系缺失 0 条。
- 已完成：四项修复、隔离库历史修护、后端 115/115、前端类型/13 组件/OpenAPI/构建、
  源码桌面与 390px E2E 18/18。
- 最终发布：`req-batch3-final3`，sourceRevision `d9ee050df2de473ca68433f1d8dd00b28d06c0a0`。
- 发布包 SHA256：`EAD2F5D6135EE5BFB525EEE46B2F57191C528F8FAE38EB1023517D5C86FAB4F6`。
- 主库修护：事件 24→0、项目状态 1→0、附件关系缺失 0；`PRJ-3157C-20250808`
  为 Completed v3 / 100%。
- 正式部署 E2E：18/18；应用回滚 `final3 → req-navigation-iam-final2 → final3`；
  恢复库 `AIDT_REQ_BATCH3_CLOSEOUT_FINAL3_RESTORE_20261101_183818` 为 9 迁移/57 表。
- 新冻结标签：`req-batch3-closeout-frozen-v1`。

## 当前权威状态：前端导航与 IAM 权限批次收尾

- 状态：`FRONTEND_NAVIGATION_IAM_CLOSEOUT_COMPLETE_DEPLOYED_AND_FROZEN`
- 分支：`fix/navigation-iam-closeout`
- 基线：`main@628e1557f8ad0dc57cf29ce7168f7fcfc0f6c4e6`
- 上一冻结标签：`req-navigation-iam-frozen-v1`
- 当前部署：`req-navigation-iam-final1`
- 目标发布：`req-navigation-iam-final2`
- 目标冻结标签：`req-navigation-iam-closeout-frozen-v1`
- 收尾变更计划：`docs/project/CHANGE_PLAN_FRONTEND_NAVIGATION_IAM_CLOSEOUT.md`
- 收尾上下文包：`docs/project/CONTEXT_PACK_FRONTEND_NAVIGATION_IAM_CLOSEOUT.md`
- 收尾影响分析：`docs/project/IMPACT_ANALYSIS_FRONTEND_NAVIGATION_IAM_CLOSEOUT.md`
- 已完成：失败回归、部门/IAM/Agent/分页/驳回/动作权限修复、112/112 后端全量、13/13 前端组件、OpenAPI 114/134、生产构建、源码 17/17 E2E、009 down/re-up、恢复探针。
- 最终发布：`req-navigation-iam-final2`
- 发布包 SHA256：`7B538AC01AE4671C83B42266B942339B9A74724797CD14DFB11C0C1E921A8B5B`
- 正式 Playwright：17/17
- 应用回滚：`final2 → final1 → final2` 通过
- 数据库恢复：`AIDT_NAV_IAM_FINAL2_RESTORE_20261101_151859`，9 迁移/57 表。
- 新冻结标签：`req-navigation-iam-closeout-frozen-v1`
- 历史 `FRONTEND_NAVIGATION_IAM_DEPLOYED_AND_FROZEN` 仅代表 final1 基线，不代表本次收尾完成。

## 当前权威状态：前端导航与 IAM 权限批次

- 状态：`FRONTEND_NAVIGATION_IAM_DEPLOYED_AND_FROZEN`
- 分支：`main`
- 功能分支：`feature/frontend-navigation-iam`
- 合并提交：`0bd8547`
- 发布版本：`req-navigation-iam-final1`
- 发布包 SHA256：`C5DA0827D1D00D1804234958439AB49EFD92BAACCFBD554D082D4EA364F68DEF`
- 新冻结标签：`req-navigation-iam-frozen-v1`
- 基线：`req-config-center-frozen-v1` / 当前 `main@40c30d5`
- 功能：`NAV-001`、`IAM-001`、`WFROLE-001`
- 变更计划：`docs/project/CHANGE_PLAN_FRONTEND_NAVIGATION_IAM.md`
- 上下文包：`docs/project/CONTEXT_PACK_FRONTEND_NAVIGATION_IAM.md`
- 影响分析：`docs/project/IMPACT_ANALYSIS_FRONTEND_NAVIGATION_IAM.md`
- 新迁移：009；001–008 未修改。
- 主库：009 已应用，9 条迁移、57 张应用表。
- 隔离库：up/down/re-up 通过；最终 SQL/API 104/104。
- 前端：类型、11/11 组件、113 路径/133 operationId、生产构建通过。
- 源码 Playwright：桌面 + 390px 15/15。
- P0 已修复：项目经理可审批本人经理节点。
- 已完成：提交、非快进合并、干净构建、正式部署、正式 E2E、应用回滚、数据库恢复验证和新冻结标签。

## 当前权威状态：配置中心试点基线冻结

- 状态：`CONFIG_CENTER_PILOT_FROZEN_NOT_PRODUCTION_READY`
- 分支：`main`
- 冻结标签：`req-config-center-frozen-v1`
- 冻结发布版本：`req-config-center-final3`
- 冻结发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-config-center-final3.zip`
- 冻结发布包 SHA256：`743C7433C872625DC2E074C68228BA254C98956821B3F5D26775F676384DA6A6`
- 冻结源码提交：`00a809837c2ecd6c0386814c28bb7732474f6fc8`
- 冻结部署备份 SHA256：`80FB98B36D894473EBD818882AC9F485F453443A74CC846C1B6B13FB58E58C48`
- 冻结清单：`docs/project/REQ_CONFIG_CENTER_FREEZE_MANIFEST.md`
- 迁移：8 条 EF、51 张应用表；008 已完成隔离库 up/down/re-up 并应用到主库。
- 后端隔离 SQL/API：97/97；前端 Vitest：10/10；OpenAPI：77 路径/91 operationId。
- 正式部署 Playwright：13/13，覆盖桌面与 390px。
- 已部署员工事项发起、附件关联、流程配置草稿/校验/发布/回滚。
- `req-config-center-final3` 已从干净源码构建、部署并通过健康检查。
- 当前工作区干净，冻结标签指向发布证据提交。
- 第二轮试点完成研发、项目经理、质量工程师和平台管理员身份验证。
- 研发、管理员兼岗经理和质量三节点流程已完整完成，3/3 任务 Approved。
- `project-manager` 缺少流程任务审批权限，真实经理审批链路被 P0 阻断。
- 已完成流程任务仍出现在“我的待办”并可点击动作，记为 P1。
- 第 7 项完成流程完成率、任务完成率、证据完整性和自动化关联覆盖率验证。
- 七项计划已全部执行完成，但生产化门禁未通过，现冻结为受控试点基线。
- 前序 4 个问题保留未修：事件状态、项目状态、附件自动关联、日期偏移。
- 整个 AIDT 项目仍未达到生产化条件。

## 历史快照：需求池批次 3 业务 Demo 完成

- 状态：`REQUIREMENT_POOL_BATCH_3_DEMO_COMPLETE`
- 分支：`main`
- 发布源码：`9b9b7fe10819e8201011909028204d533625f627`
- 最终标签：`req-batch3-final2`
- 最终发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch3-final2.zip`
- 发布包 SHA256：`C4875993E77D183EBA8EEE1F2D857AB35A11C815C2B00BE5DED85D5C9DB7E435`
- 当前部署：`req-batch3-final2`；上一稳定版本：`req-batch2-final3`
- 后端和 SQL/API：96/96；正式 E2E：11/11。
- 迁移：7 条 EF、51 张应用表；隔离 up/down/re-up 和正式部署备份恢复通过。
- `req-batch3-final1` 因管理员 Cookie 超限保留但不打标签；final2 为最终交付。
- 批次 3 是最后一个业务 Demo 批次；整个 AIDT 生产项目仍未完成。

## 历史快照：需求池批次 2.1 收尾门禁通过

- 状态：`REQUIREMENT_POOL_BATCH_2_1_READY`
- 阶段：批次 2.1 收尾门禁通过，待非快进合并；批次 3 尚未启动。
- 已完成任务书：`prompts/12-需求池批次2目标模式.md`。
- 收尾分支：`chore/closeout-batch-2-1`。
- 测试证据提交：`97646e1`，仅刷新 `req-batch2-final3` 正式部署的 6 个 E2E 证据文件。
- 最新正式部署 E2E：8/8 通过，0 failure、0 skipped、0 flaky；报告时间晚于 final3 部署。
- 最新健康检查：页面、live、ready、capabilities 均返回 HTTP 200。
- 标签 `req-batch2-final3` 仍为注释标签，指向 `c68a799`，未移动。
- 主库 `AIDT` ONLINE，6 条 EF 迁移、44 张应用表（不含 dbo/Elsa/HangFire）。
- 批次 2 起始基线：`48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`。
- 批次 2 基线：已完成并发布 `req-batch2-final2`。
- SQL 第一检查门：通过。`MSSQLSERVER` Running，1433 双栈 Listen，SQL Server `17.0.1000.7`。
- 批次 2 迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_PRE_005_20261031_094122.bak`
- 备份 SHA256：`4F67C0A6CFF8CEC733CD863D5A8CDABF48B16B42A3F668F087DA1AB75160EA8F`；`RESTORE VERIFYONLY` 通过。
- 隔离测试库：`AIDT_REQ_BATCH2_TEST_20261031_094122`。
- 005 up/down/re-up：通过；主库 44 表、5 条 EF 迁移。
- 整个 AIDT 项目：未完成。
- 当前工作包：将收尾分支以 `--no-ff` 合并到 `main`，再从干净 `main` 创建批次 3 分支。
- 批次 2.1 目标文档：Goal objective attachment `60673446-867b-49a4-a5d7-9f48f4be0f99`。
- 批次 3 是最后一个业务 Demo 批次，不是整个生产项目最终完成。
- 本文件后续“历史快照”中的未勾选项只记录当时中断状态，不得覆盖本节唯一权威状态。

## 需求池批次 3 当前状态

- 状态：`REQUIREMENT_POOL_BATCH_3_DEMO_COMPLETE`
- 分支：`feature/requirement-pool-batch-3`，起始 `main` 合并提交 `2fd0177`。
- 正式功能：`PMO-001`、`PMO-002`、`EVT-001`、`TASK-002`。
- 变更计划：`docs/project/CHANGE_PLAN_REQUIREMENT_POOL_BATCH_3.md`。
- 上下文包：`docs/project/CONTEXT_PACK_REQUIREMENT_POOL_BATCH_3.md`。
- 主库迁移前备份：`C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH3_PRE_007_20261101_004945.bak`。
- 备份 SHA256：`B872EE36A940F338C91A3B15082F92E8D7905BA55C94C28C218F5EC0CB1C274E`；`RESTORE VERIFYONLY` 通过。
- 隔离测试库：`AIDT_REQ_BATCH3_TEST_20261101_004955`，6 条迁移、44 张应用表。
- 最终隔离库：`AIDT_REQ_BATCH3_FINAL_20261101_010853`，007 up/down/re-up 通过。
- 主库 `AIDT`：007 已应用，7 条迁移、51 张应用表。
- 后端 SQL/API 全量：96/96 通过，0 失败、0 跳过。
- 前端：类型检查、9/9 Vitest、73 路径/85 operationId、生产构建和依赖审计通过。
- 源码 E2E：11/11 通过，0 failure、0 skipped、0 flaky。
- `req-batch3-final1` 已构建并部署，但正式 E2E 暴露管理员 Cookie claims 超限；该包保留，不覆盖、不打最终标签。
- 当前修复：压缩 Cookie 权限/数据范围声明，兼容旧 Cookie；全量后端 96/96 通过。
- 已完成：提交修复并合并，构建 `req-batch3-final2`，完成正式 E2E、回滚和恢复验证。
- 下一步：仅在甲方提供真实身份、外部系统 Sandbox、生产 DBA/高可用环境后进入生产化。

## 目标

为 AIDT 建立一套适合长期 AI 协作的开发治理体系。规范必须解决：

- 上下文有限时的阅读顺序和上下文恢复
- 前后端、数据库和接口边界
- 功能地图、模块地图和影响分析
- 新功能、Bug 修复、重构、提交和部署工作流
- 测试、回归、代码审查和 Definition of Done
- 甲方复制、接手、离线构建和长期维护

## 当前阶段

批次 1 已完成四个需求、迁移、SQL/API 集成、权限、文件、桌面/移动 E2E、
部署回滚、备份恢复和离线构建。整个 AIDT 项目未完成。

## 阶段

### 阶段 1：确认需求与限制

- [x] 识别图片中的 12 类企业开发规范
- [x] 确认用户希望同时约束人和 AI
- [x] 确认技术栈已冻结
- [x] 确认目标仓库必须适合甲方复制和维护
- **状态：** complete

### 阶段 2：设计规范金字塔

- [x] 定义目标、架构、开发、测试、交付和 AI 工作流六层结构
- [x] 设计核心文档、模板和提示词目录
- **状态：** complete

### 阶段 3：生成规范文档

- [x] 生成 AI 工程宪法与阅读协议
- [x] 生成架构和模块边界规范
- [x] 生成后端、前端、数据库和 API 规范
- [x] 生成安全、权限、审计、日志、配置和事务规范
- [x] 生成测试、回归、Git、评审、发布和回滚规范
- [x] 生成功能地图、Bug 修复、重构和上下文恢复工作流
- [x] 生成完成定义和交付检查表
- **状态：** complete

### 阶段 4：生成 AI 工作模板与提示词

- [x] 生成 AGENTS.md 和 CLAUDE.md
- [x] 生成新功能、修 Bug、数据库变更、重构、发布提示词模板
- [x] 生成功能影响分析和会话交接模板
- **状态：** complete

### 阶段 5：集成到现有计划

- [x] 更新 coding/README.md 的阅读顺序
- [x] 更新 `docs/001-产品PRD/03-计划方案.md` 的规范先行门槛
- [x] 校验所有文档之间的技术栈和目录结构一致
- **状态：** complete

### 阶段 6：验证

- [x] 检查所有规范文件均存在
- [x] 检查冻结技术栈无冲突
- [x] 检查阅读顺序可从 AGENTS.md 到达
- [x] 检查每类 AI 工作均有执行模板
- [x] 检查 Markdown 相对链接
- **状态：** complete

## 验证结果

- 34 个预期文件全部存在。
- 全部 Markdown 相对链接通过检查。
- 冻结技术栈在入口、项目目标、技术栈、计划、规范和提示词中保持一致。
- 已覆盖图片中的 12 类企业开发规范。
- 已额外覆盖功能地图、上下文恢复、Bug 修复、重构、部署回滚和 AI 工作流。

## 关键决策

| 决策 | 理由 |
|---|---|
| 采用“最小启动规范 + 模块规范增量补充” | 避免在未开始开发前投入过多文档工作 |
| AGENTS.md 只做入口，完整规则分文件 | 降低上下文占用，减少规则冲突 |
| 功能地图与模块地图作为必读索引 | 控制大型代码库中的上下文范围 |
| 每次 Bug 修复必须补回归测试 | 防止修一处坏一处 |
| 文档规则必须有自动化和 CI 校验 | 纯提示词无法保证长期执行 |
| 技术栈和仓库目标保持单一来源 | 防止多个文档出现冲突 |

## 遇到的问题

| 错误 | 尝试次数 | 解决方案 |
|---|---:|---|
| 工作区没有独立“目标”文件 | 1 | 已新增 `docs/001-产品PRD/00-项目目标.md` 并纳入一级阅读顺序 |
| 原技术栈文档包含与冻结栈冲突的 Java/React/PostgreSQL 内容 | 1 | 已更新为冻结技术栈并删除冲突路线 |
| 规范文件之间可能断链 | 1 | 运行相对链接检查，全部通过 |

## 备注

- 所有文档使用中文，目录和代码标识使用稳定英文。
- 规范不替代代码、测试和 ADR。
- 真实敏感数据不进入公共模型、Demo 或仓库。

### 阶段 7：SQL 阻断下的初期框架搭建

- [x] 使用阶段 A 提示词完成基线与规范读取
- [x] 建立阶段 A 变更计划、上下文包和影响分析
- [x] A2：创建正式 .NET 10 后端工程骨架
- [x] A3：创建正式 Vue 3 前端工程骨架
- [x] A4：完成领域模型、DTO、错误码、接口与 OpenAPI
- [x] A5：编写 SQL Server Schema、EF Core 迁移和发布/回滚脚本（未执行）
- [x] A6：执行不依赖 SQL Server 的单元测试、前端构建与契约静态校验
- [x] A7：更新模块 README、功能地图、启动说明和 SQL 阻断说明
- [x] 完成架构、行为、交付三视角自审
- [x] 整理阶段 A 交付证据和恢复检查点
- **状态：** complete
- **提示词：** `prompts/07-SQL重启阻断下的分阶段搭建.md`
- **完成状态：** `PHASE_A_COMPLETE_WAITING_SQL`

### 阶段 8：SQL 恢复后的阶段 B

- **历史状态：** 后续运行记录，不属于本轮阶段 A 当前状态
- [x] 检查 `MSSQL*` 服务状态
- [x] 检查 TCP 1433 与 SQL Server 实例版本
- [x] 使用应用连接串完成连接测试
- [ ] 执行并验证 `InitialSchema` 迁移
- [ ] 运行 SQL Server 集成测试
- [ ] 完成至少一条业务链持久化端到端闭环
- [ ] 运行浏览器 E2E
- [ ] 验证备份恢复、部署与回滚
- **历史状态：** `SQL_READY_FOR_PHASE_B`（后续运行记录；本轮不据此进入阶段 B）
- **专项阶段 B 提示词：** `prompts/08-阶段B-SQL恢复后继续.md`
- **单目标全阶段提示词：** `prompts/09-AIDT全阶段目标模式.md`

## 阶段 A 范围与边界

### 本轮目标

完成所有不依赖 SQL Server 运行环境的正式仓库骨架、领域与契约代码、可运行前端骨架、测试、迁移源文件和文档。最终目标状态只能是：

- 全部完成：`PHASE_A_COMPLETE_WAITING_SQL`
- 被发现非 SQL 问题时：`PHASE_A_IN_PROGRESS`

### 外部阻断

| 项目 | 状态 | 证据 |
|---|---|---|
| SQL Server 2025 | BLOCKED | 安装日志 `3010`，要求重启计算机 |
| Windows 服务 | 不存在 | 未发现 `MSSQL*` 服务 |
| TCP 1433 | 未监听 | 已记录于 `findings.md` |
| sqlcmd | 不在 PATH | 已记录于 `findings.md` |
| 迁移执行 | 禁止 | 阶段 A 不连接数据库、不执行迁移 |

### 本轮必须交付

- `backend/`：7 个正式项目、集中版本、严格项目引用、启动配置校验、统一响应/异常/TraceId、健康检查和 OpenAPI。
- `frontend/`：Vue 3 + TypeScript + Vite + Element Plus + Pinia + Router，包含工作台、流程、对象、审计和 Agent 骨架。
- `database/`：SQL Server Schema 设计、EF Core Migration 代码、发布与回滚脚本，状态 `authored_not_applied`。
- `tests/`：Domain、Application、DTO/错误码、OpenAPI 静态校验；SQL Server 集成测试仅编写并明确跳过/阻断。
- 文档：模块 README、功能地图、API/数据库说明、启动说明、阻断说明和规划文件。

### 明确不做

- 不启动或修复 SQL Server。
- 不打开 SQL Server 连接，不执行迁移或集成测试。
- 不使用 SQLite、LocalDB、内存数据库、Docker 或 PostgreSQL 替代。
- 不连接 U9、ERP、DCS、钉钉或外部模型。
- 不修改旧 `AIDT/LOCAL-MVP`。
- 不把阶段 A 表述为整个 AIDT 项目完成。

## 变更影响分析

| 范围 | 预期变更 | 调用方/影响 | 验证 |
|---|---|---|---|
| 后端工程 | 新增 7 个项目和测试项目 | 后续所有后端开发 | `dotnet build` / `dotnet test` |
| 分层契约 | Domain/Application 接口、DTO、错误码 | API、测试、未来 SQL 实现 | 单元测试、引用方向检查 |
| HTTP 边界 | 统一响应、异常、TraceId、健康检查、静态 OpenAPI | Vue API client、部署探针 | API 非 SQL 测试、OpenAPI 校验 |
| 前端 | Vue 工程、路由、权限守卫、状态与 5 个页面 | 后续业务页面 | `vue-tsc`、Vitest、Vite build |
| 数据库 | 初始 Schema、EF 迁移、up/down SQL | 阶段 B 迁移与持久化 | 静态审查；禁止执行 |
| 文档 | 模块说明、契约、数据库、启动和阻断 | 甲方接手、AI 恢复 | 路径与契约核对 |

### 明确不修改

- `AIDT/LOCAL-MVP` 全部文件。
- 冻结需求、技术栈和计划的业务口径。
- 与阶段 A 无关的规范内容。

### 回滚方式

- 本阶段只新增工程、迁移源文件和文档，不改变外部系统状态。
- 代码回滚可按新增目录整体撤回；数据库回滚脚本供阶段 B 在授权环境使用，本轮不执行。

## 阶段 A 验证结果

| 门禁 | 结果 |
|---|---|
| `.NET` 项目 | 7 个后端项目 + 1 个测试项目 |
| 后端编译 | 成功，0 警告、0 错误 |
| 后端测试 | 29 总数；27 通过、0 失败、2 显式跳过 |
| 后端格式 | 通过 `dotnet format --verify-no-changes` |
| NuGet 漏洞检查 | 全部项目无已知易受攻击包 |
| 前端锁定安装 | `npm ci` 成功，0 漏洞 |
| 前端类型检查 | 通过 |
| 前端组件/状态测试 | 3 通过、0 失败 |
| OpenAPI 静态校验 | 28 路径、33 operationId 通过 |
| 前端生产构建 | 成功，无大包警告 |
| 数据库迁移 | 已编写，未执行 |
| SQL 集成/E2E | 按边界未执行 |

## 三视角自审

### 架构

- 7 个项目引用方向固定，Domain 无基础设施依赖。
- SQL 迁移归 Infrastructure，API 只做 HTTP 适配。
- Elsa、Hangfire、Garnet 和真实外部系统只保留受控接缝，没有伪造已集成。

### 行为

- 业务对象版本、流程重复处理、退回原因、Agent 限额和人工确认均有测试。
- 无 SQL 时 API 存活正常，依赖持久化端点返回稳定 `503`。
- 未使用内存库、SQLite、LocalDB、Docker 或 PostgreSQL。

### 交付

- 后端、前端、迁移、测试、契约和恢复文档齐全。
- 已知限制已明确写入模块 README、功能地图和 SQL 阻断说明。
- 项目不是 Git 仓库，本轮无法提供 Git diff；使用磁盘文件、构建和测试输出作为证据。

## 下一步

下一次执行同一提示词时，先检查 `MSSQL*` 服务状态；只有服务、TCP 1433 和应用连接都正常后，才进入阶段 B。

### 阶段 9：AIDT 全阶段可交付 Demo

- [x] 完整读取 `prompts/09-AIDT全阶段目标模式.md` 与全部指定基线
- [x] SQL 第一检查门：`MSSQLSERVER` Running，TCP 1433 Listen
- [x] G1：创建 `AIDT` 数据库并执行 `InitialSchema`
- [x] G2：完成 SQL 仓储、UnitOfWork、并发和幂等持久化
- [x] G3：完成本地测试身份、RBAC/ABAC、字段脱敏和审计基础实现
- [x] G4：完成 Elsa Workflows 3.9 与 SQL Server 持久化 PoC
- [x] G5：完成 `WF-001`、`WF-002`、`WF-003` 三条闭环，并补齐 `WF-004` 终止场景
- [x] G6：完成 Hangfire、Outbox Worker、死信字段与 Garnet 锁基础场景
- [x] G7：完成 Mock 连接器和 AiGateway 受控接缝
- [x] G8：前端接入真实 API，完成桌面/移动端状态和操作
- [x] G9：完成领域、应用、SQL 集成、权限、并发、迁移、API、前端和 E2E 测试
- [x] G10：完成备份恢复、Nginx、部署和回滚演练
- [x] G11：完成文档、功能地图、权限矩阵、测试证据和甲方交接
- [x] G12：执行全阶段验收审计
- **状态：** complete_demo_ready
- **提示词：** `prompts/09-AIDT全阶段目标模式.md`

## 全阶段本轮目标

从阶段 A 代码继续，交付首个真实 SQL Server 持久化的 AIDT Demo：

- 真实数据库和 34 张应用表
- 事务、并发、幂等、审计和 Outbox
- 本地测试身份 + 明确 OIDC 接缝，绝不冒充生产 SSO
- Elsa 流程运行与版本冻结
- 至少三条业务闭环及条件、退回、终止
- Hangfire、Outbox、Garnet 验证或明确环境缺口
- Mock 外部连接器 + AiGateway 接缝
- Vue 真实 API、桌面和 390px 移动 E2E
- 备份恢复、部署与回滚证据
- 完整交接文档

## 全阶段非目标

- 13 个部门工作台全部上线
- 真实 U9、ERP、DCS、WMS、LIMS、钉钉生产接入
- 真实付款、质量放行、配方修改、DCS 控制
- 真实外部大模型和生产 SSO
- 真实生产上线

## 变更影响分析

| 范围 | 预期变更 | 验证 |
|---|---|---|
| SQL Server | 创建 `AIDT` 库、执行迁移、校验 9 Schema/34 表 | SQL 查询、迁移历史、约束检查 |
| Infrastructure | SQL 仓储、UnitOfWork、Outbox、Agent、审计、身份读写 | 集成测试、事务和并发测试 |
| Identity | 本地演示身份/Cookie、RBAC+数据范围、OIDC 接缝 | 未登录、越权、数据范围、脱敏测试 |
| Workflow | Elsa 版本锁定、定义/版本/实例/任务/动作 | 条件、退回、终止、历史版本测试 |
| Business | 对象类型、三条样板流程、对象状态和证据链 | API + SQL + E2E |
| Worker | Hangfire SQL 存储、Outbox 幂等任务、重试/死信 | 重启恢复与重复投递测试 |
| Cache | Garnet 缓存/锁/限流至少一个场景 | 运行验证；缺口则记录 environment pending |
| Frontend | 真实工作台、待办、对象、审计和 Agent | 类型、组件、桌面/移动 E2E |
| Deploy | Nginx 入口、版本目录、备份、部署和回滚 | 测试环境脚本与健康检查 |
| Docs | 功能地图、权限矩阵、Mock 边界、测试证据和交接 | 文档链接和验收审计 |

## 明确不修改

- 旧 `AIDT/LOCAL-MVP`
- 冻结技术栈与需求口径
- 已执行前的 `InitialSchema` 结构；如需变更必须新增迁移

## 当前外部风险

| 风险 | 处理 |
|---|---|
| SQL Server 为 Express 2025 | Demo 可用，不宣称生产授权已满足 |
| Nginx/Garnet 可能未安装 | 先完成配置与可运行接缝，明确记录环境缺口 |
| 无企业 SSO 凭据 | 本地测试身份，保留 OIDC 接缝，不宣称生产 SSO |
| 无真实外部系统 | 只使用明确 Mock，不产生真实外部副作用 |

## 下一步

首个可交付 Demo 已完成。下一阶段进入甲方生产化：正式 SSO、真实 Sandbox 连接器、生产 DBA/证书/监控和高可用。

## 本轮最终验证（2026-10-30 全阶段）

| 门禁 | 结果 |
|---|---|
| 后端测试 | 41/41 通过，0 跳过 |
| 前端类型检查 | 通过 |
| 前端测试 | 3/3 通过 |
| OpenAPI | 28 路径、33 operationId |
| 前端生产构建 | 通过 |
| NuGet/npm 漏洞 | 0 已知漏洞 |
| Playwright | Nginx 部署环境 2/2，通过 |
| API/Worker/AiGateway/Nginx | 所有健康检查 200 |
| SQL | 35 张应用表；迁移 applied_verified |
| Outbox | Pending=0 |
| 备份恢复 | 临时库恢复验证通过 |
| 迁移回滚 | 001→002→002 down→001 down 通过 |
| 部署回滚 | demo-a→demo-b→demo-a 通过 |
| 最终发布 | demo-final3 部署成功 |
| 离线构建 | NuGet/npm 离线 restore/install/build 通过 |

## G12 全阶段验收审计

| 完成条件 | 权威证据 | 结果 |
|---|---|---|
| SQL 连接和迁移 | SQL Server 17.0.1000.7；2 条 EF 迁移；35 表 | 通过 |
| 事务、并发、幂等、审计 | 41 项后端测试；并发单一赢家；审计同事务 | 通过 |
| 至少三条流程闭环 | WF-001/002/003 Completed；E2E 重新完成 WF-001 | 通过 |
| 条件、退回、终止 | 采购金额条件、试产条件、Returned/Terminated | 通过 |
| Elsa | 3.9 SQL 迁移；83 个实例；执行日志 | 通过 |
| Hangfire/Outbox/Garnet | 心跳已刷新；Pending=0；Garnet 6380 | 通过 |
| 身份/API/数据范围/脱敏 | Cookie/CSRF；401/403；Core=`***` | 通过 |
| 前端真实 API | Vue 页面真实读取/写入 | 通过 |
| 桌面/移动 E2E | Nginx 8088 部署环境 2/2 | 通过 |
| 备份恢复 | 临时库恢复 35 表/13 对象/59 审计/51 Outbox | 通过 |
| 迁移回滚 | 34→35→34→0 表 | 通过 |
| 部署/应用回滚 | demo-a→demo-b→demo-a；final2 部署 | 通过 |
| Nginx | 唯一浏览器入口 8088；内部端口回环 | 通过 |
| 离线构建 | 233 NuGet 包；npm 离线安装/构建 | 通过 |
| Mock/AI 边界 | Mock connector；RuleSimulator；ExternalModelCalled=false | 通过 |
| 文档交接 | 全阶段交付、权限、Mock、测试、部署、交接 | 通过 |

最终发布包：
`D:\Apps\AIDT-Test\releases\AIDT-demo-final3.zip`

SHA256：
`684EE630E912AF3B55CF32C3B8464A9850A3F15F540CFE08A04366F4E882643E`

当前测试部署：
`D:\Apps\AIDT-Test\versions\demo-final3`

访问入口：
`http://127.0.0.1:8088`

最终部署前数据库备份：
`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_demo-final3_20261030_143102.bak`

备份 SHA256：
`31C8DB5A8DC098CF18F4FC31FF7EF4AE52F6216137605D4FD60AFCC81F911029`

## 当前 SQL BLOCKED 与历史恢复

- 当前执行请求明确要求阶段 A 在服务、1433、连接、迁移和集成测试边界立即停止。
- 本轮没有据此启动、修复或连接 SQL Server。
- 原始安装错误码 `3010` 和后续安装恢复只作为历史证据保留。
- 003 未执行；阶段 B 需要重新通过 SQL 第一检查门并另行获得执行任务。

### 阶段 10：岗位场景运行时演示（不改代码）

- [x] 明确本轮只使用现有网页和现有 API，不修改前后端、数据库结构或迁移
- [x] 场景 01：采购员发起研发中试原料加急采购，财务大额复核、生产验收、财务模拟付款
- [x] 场景 02：研发发起耐候型苯丙样品，质量、生产、试产复核与管理者转产发布
- [x] 场景 03：生产现场确认批次温度越限，质量评审、管理者重大偏差复核与 CAPA 决策
- [x] 场景 04：客户粘度投诉受理、生产批次追溯、质量联合调查与客户回访
- [x] 核对 4 个对象、4 个流程实例、18 个任务全部 Completed/Approved
- [x] 核对场景关联审计事件 26 条，并保存对象与审计页面截图
- **状态：** complete

### 岗位场景运行时基线

| 场景 | 业务对象编号 | 岗位链路 |
|---|---|---|
| 采购加急 | `SCN-01-PUR-EXPEDITE-20261030` | 供应链专员 → 财务审批人 → 供应链专员 → 生产班组 → 财务审批人 |
| 样品转产 | `SCN-02-SAMPLE-ANTIWEATHER-20261030` | 研发工程师 → 质量工程师 → 生产班组 → 质量工程师 → 平台管理员 |
| 温度越限 CAPA | `SCN-03-DEV-TEMP-20261030` | 生产班组 → 质量工程师 → 平台管理员 → 质量工程师 |
| 客户投诉回访 | `SCN-04-COMPLAINT-VISCOSITY-20261030` | 质量工程师 → 生产班组 → 质量工程师 → 平台管理员 |

本轮未增加独立“项目经理”身份；现有 Demo 中使用平台管理员承担经理/项目经理复核与发布职责。

### 阶段 11：产品规划文档统一归档

- [x] 在 `docs/001-产品PRD/` 建立产品规划目录
- [x] 迁入项目目标、需求基线、技术栈与架构、计划方案四份基线文档
- [x] 新增产品规划总入口 `README.md`
- [x] 新增下一阶段需求池 `04-需求池.md`
- [x] 更新 `AGENTS.md`、仓库 README、规范和提示词中的文档路径
- [x] 执行旧路径残留检查和全仓 Markdown 相对链接校验
- **状态：** complete

文档迁移只改变产品规划资料的存放位置，不改变已经冻结的需求内容、技术栈或实施范围。

### 阶段 14：需求池批次 2 任务书（历史快照）

- [x] 核对批次 1 冻结标签与批次 2 分支基线
- [x] 从需求池选择 `IDEA-MINUTES-001`、`IDEA-WORKFLOW-001`、`IDEA-AGENT-001`、`IDEA-AI-001`
- [x] 编写 `prompts/12-需求池批次2目标模式.md`
- [x] 更新提示词索引
- [ ] 批次 2 功能实施
- **历史快照状态：** `BATCH2_IMPLEMENTING`（批次 2 后续已完成，见阶段 15）

批次 2 已开始实施，但尚未达到完成条件。

### 阶段 15：需求池批次 2 实施

- [x] 完整读取批次 2 任务书和全部指定基线
- [x] 核对 Git 分支、批次 1 标签、批次 2 基线和 SQL 环境
- [x] 识别既有未提交提示词与测试证据，不覆盖用户内容
- [x] 冻结四个正式 Feature ID
- [x] 完成变更影响分析和上下文包
- [x] 主库备份、恢复校验与隔离测试库
- [x] 迁移 005 和领域/应用层
- [x] Minutes/NodePolicy/SLA/Agent/AiGateway API
- [x] Worker 超时提醒、升级和受限代理
- [x] Vue 页面与 E2E
- [x] 全部门禁、恢复、部署、回滚和 final2 发布
- **状态：** `REQUIREMENT_POOL_BATCH_2_DEMO_COMPLETE`
- **批次 2 备份：** `C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_PRE_005_20261031_094122.bak`
- **备份 SHA256：** `4F67C0A6CFF8CEC733CD863D5A8CDABF48B16B42A3F668F087DA1AB75160EA8F`
- **隔离测试库：** `AIDT_REQ_BATCH2_TEST_20261031_094122`
- **迁移 005：** `20261031014600_005_Batch2MinutesSlaAgentGateway`
- **隔离库：** 5 条迁移、44 张应用表；up/down/re-up 通过
- **隔离库后端测试：** 70/70 通过，0 跳过
- **前端：** 9/9 通过；OpenAPI 53 路径/62 operationId；生产构建通过
- **主库 005：** 已应用；5 条 EF 迁移、44 张应用表
- **最终版本：** `req-batch2-final2`
- **最终 SHA256：** `339AC0BBF400F378FA0E570960F06EF85B0C97DEA5B3EFA1267B066A4301888E`
- **manifest sourceRevision：** `875204ed5d7fbc7cce7054959eab8c4fead255b8`
- **最终 E2E：** 6/6，0 skipped/flaky

### 阶段 16：需求池批次 2.1 实施过程快照（历史，非当前状态）

> 以下未勾选项记录 2.1 实施中段的历史状态，不代表当前门禁结果。最终完成状态以文件顶部唯一权威状态及阶段 14“批次 2.1 可用性补完”完成段为准。

- [x] 完整读取批次 2.1 目标和全部 31 项基线
- [x] 核对 `main`、标签、部署和 SQL
- [x] 从 `main` 创建 `fix/requirement-pool-batch-2-1`
- [x] 保留批次 2 独立复核证据
- [x] 完成变更计划和上下文包
- [x] 主库备份和 2.1 隔离测试库
- [ ] DOCX 解析与测试
- [ ] 节点策略页面、用户/节点查询
- [ ] 默认 Demo 策略和迁移 006
- [ ] 超时后预授权自动代理
- [ ] 正式部署策略和自动代理 E2E
- [ ] 全部门禁、回滚和 `req-batch2-final3`
- **历史快照状态：** `REQUIREMENT_POOL_BATCH_2_1_IN_PROGRESS`（当时状态；后续已完成）
- **2.1 备份：** `C:\ProgramData\AIDT\Backups\AIDT_REQ_BATCH2_1_PRE_006_20261031_121903.bak`
- **备份 SHA256：** `E5F17A4E50B9FF845704A4A3198E9A1ED28A5BF7A81458EAA65563CDCB5470F2`
- **2.1 隔离库：** `AIDT_REQ_BATCH2_1_TEST_20261031_121903`

### 阶段 12：需求池批次 1

- [x] Git 基线提交 `8aff291`
- [x] 批次分支 `feature/requirement-pool-batch-1`
- [x] SQL 备份、`RESTORE VERIFYONLY` 与 SHA256
- [x] 影响分析和变更计划
- [x] `UX-001` 阶段 A：我的工作、员工导航、管理员分层和前端组件测试
- [x] `WS-001` 阶段 A：部门、成员、部门范围、工作区 API/页面和迁移源文件
- [x] `DOC-001` 阶段 A：文件契约、上传/版本/预览/删除申请、安全校验和页面
- [x] `DATA-001` 阶段 A：环境字段、部门过滤、管理员跨环境开关和契约
- [x] 003 EF Migration、幂等 up/down SQL、DBA 审核和种子更新
- [x] 后端 48 个非 SQL 测试、10 个 SQL 用例显式跳过
- [x] 前端类型检查、7 个组件/状态测试、OpenAPI 46 操作校验和生产构建
- [x] SQL 第一检查门、新迁移前备份和恢复验证
- [x] 隔离测试库 002→003 up→003 down→003 up
- [x] 部门/环境/文件 SQL 集成测试与越权审计验证
- [x] 桌面和 390px 浏览器持久化 E2E
- [x] 备份恢复、离线构建、部署和应用回滚验证
- **状态：** `REQUIREMENT_POOL_BATCH_1_COMPLETE`
- **最终发布：** `D:\Apps\AIDT-Test\releases\AIDT-req-batch1-final5.zip`
- **发布 SHA256：** `6DEFDDEB879C2F3CCC73BBD786E4CFDAA1FD3FA3E37E428D6D978B6B6A99353B`
- **最终版本：** `D:\Apps\AIDT-Test\versions\req-batch1-final5`

### 阶段 13：批次 1 版本冻结与批次 2 分支准备

- [x] 完整读取版本冻结任务书、项目基线、发布规范和交接材料
- [x] 审查工作区、改动来源、敏感文件、临时目录与 Playwright 结果
- [x] 重新运行后端、前端、OpenAPI、构建和部署环境 Playwright 门禁
- [x] 提交批次 1 全部改动并确认工作区干净
- [x] 合并批次 1 到 `main`
- [x] 从干净 `main` 构建新发布版本
- [x] 部署并执行 E2E
- [x] 创建 `req-batch1-final6` 标签
- [x] 创建 `feature/requirement-pool-batch-2` 分支
- [x] 更新交付、交接、规划、发现和部署记录
- **状态：** `BATCH1_RELEASE_FROZEN_AND_BATCH2_BRANCH_READY`
- **批次 1 提交：** `e37c55d`、`d385c5e`、`4cb4738`、`ed210b7`
- **main 合并：** `7aa50459acc0f1ec66ba70caa16d82cc574415af`
- **final6：** `D:\Apps\AIDT-Test\releases\AIDT-req-batch1-final6.zip`
- **final6 SHA256：** `008711843CDED2B6F0D0814959FE9B36F432C73FF4277E69E93D5164CE8E4895`
- **manifest sourceRevision：** `7aa50459acc0f1ec66ba70caa16d82cc574415af`
- **发布标签：** `req-batch1-final6`
- **批次 2 基线：** `48ea1a4accc298c0b1bbc1be4e542d2f68f20b85`
- **目标模式提示词：** `prompts/11-批次1版本冻结与批次2分支目标模式.md`

### 阶段 14：批次 2.1 可用性补完

- [x] 修复 DOCX 解析编译问题并补单元测试
- [x] 增加策略用户/节点候选 API 和策略管理页面
- [x] 增加迁移 006、默认低风险策略和 DBA 审核
- [x] 完成 SLA 预授权自动代理、独立身份、通知和审计
- [x] 完成隔离库 006 up/down/re-up
- [x] 完成隔离 SQL/API、前端、OpenAPI 和构建验证
- [x] 完成正式部署 Playwright
- [x] 完成应用回滚和数据库恢复验证
- [x] 提交、非快进合并、构建部署并创建 `req-batch2-final3`
- **状态：** `REQUIREMENT_POOL_BATCH_2_1_READY`

## 批次 1 设计决定

- Department 使用 `Identity` Schema，一个组织内多个部门。
- 用户通过 `UserDepartmentMembership` 建立主部门/兼属部门和负责人标识。
- DataScope 使用 `department:<guid>`，与组织范围组合校验。
- BusinessObject、WorkflowInstance、WorkflowTask、Attachment 均保存 `DepartmentId`。
- 环境标识使用 `demo/test/prod`，并保存 `IsTestData`、`SourceSystem`。
- 文件使用本地目录存正文、SQL 存元数据，物理文件名使用 GUID。
- 文件关联使用通用 `AttachmentLink`，不新建第二套文档系统。
- 普通员工只访问授权部门；平台管理员可跨部门。

### 阶段 17：3157C 真实流程试点与配置化前端决策

- **状态：** `in_progress`
- **边界：** 只使用当前 `req-batch3-final2` 部署和现有网页/API 创建业务数据；不修改前后端源码、数据库结构、迁移或部署配置。
- **试点编号：** `PRJ-3157C-20250808`
- **数据边界：** 不连接真实钉钉；忽略已离职人员；不猜测截图中未出现的检测值；项目按敏感内部资料管理。

#### 七项推进清单

1. [x] 用现有网站完成 `3157C` 真实流程闭环并保留证据（核心流程已闭环；发现 4 个待修复体验/状态问题）
2. [x] 输出试点体验记录和配置需求清单
3. [x] 设计员工端、项目端、配置端页面原型
4. [x] 补齐配置中心最小可执行后端能力
5. [x] 开发配置前端和员工工作台
6. [x] 选择 2–3 个岗位开展第二轮试点（完成，发现项目经理审批权限阻断）
7. [x] 验证流程完成率、证据完整率和重复录入改善（完成；生产化门槛未通过）

#### 第 1 项验收条件

- [x] 创建 `3157C` 项目、成员和里程碑
- [x] 上传并关联原始流程截图
- [x] 创建事件收件箱记录并保留原文
- [x] 数字员工完成建议分析，真人确认后生成项目任务
- [x] 任务完成且项目详情可见来源、附件、任务和审计
- [x] 页面刷新及服务读取后数据仍存在

#### 第 1 项执行记录

- 创建项目：`f507fb2e-cdf0-4841-a988-f3088fccdcbe`
- 项目编号：`PRJ-3157C-20250808`
- 当前成员：项目经理、研发工程师、质量工程师
- 当前里程碑：4 个
- 当前审计事件：7 条

#### 第 1 项过程发现

- 首次附件上传把 PNG 表单误声明为通用二进制类型，被 MIME 校验拒绝且未落盘；按 `image/png` 重试成功。
- 四张截图均已保存为 `Sensitive / Active` 并关联项目。
- 已创建事件 `465ee426-c0d6-46b5-accc-bd4299c42005`，原文 721 字，关联第一张申请截图。
- 项目事件建议数字员工已生成建议：项目识别正确、风险 `Low`、优先级 `P1`、建议负责人默认为项目经理。
- 数字员工按原文识别出“是否发起流程待人工判断”这一缺失信息。
- 观察到分析后的事件状态接口返回仍为 `Received`，而建议已存在；需要区分接口刷新问题和前端展示缺陷，并记录为试点体验问题。
- 项目任务 `2f48608d-aa6d-4acf-aa33-347e98210c46` 已由质量工程师完成，任务状态为 `Completed`。
- 项目进度达到 100%，里程碑 4/4、任务 1/1、文件 4 个、项目关系 6 个、项目经理可见审计 19 条。
- 四张原始截图已通过 `SupportingEvidence` 项目关系显示在项目详情页。
- 已保存网页证据：
  - `docs/project/test-artifacts/3157c-pilot-project-detail.png`
  - `docs/project/test-artifacts/3157c-pilot-inbox.png`

#### 第 1 项待修复发现

1. **事件状态没有持久化**
   - 事件已有 Agent 建议并已创建项目任务，但接口和网页仍显示 `Received`。
   - 网页仍显示“真人确认并创建项目任务”按钮，容易误导用户重复确认。
   - 根因方向：`LoadAccessibleEventAsync` 使用 `AsNoTracking` 读取事件，后续只修改内存对象状态，没有显式更新并保存。

2. **项目状态没有持久化**
   - 网页提交“已完成”后提示“项目状态已更新”，审计也新增 `project.status_changed`，但页面和重新读取仍为“进行中”。
   - 根因方向：项目通过 `AsNoTracking` 读取后直接修改 `Status/Version`，再调用 `SaveChanges`，实体并未被 EF 跟踪。

3. **项目附件关系需要二次补录**
   - 文件上传时已填写 `BusinessObjectId`，文件中心能看到 4 个文件，但项目详情的文件区最初为空。
   - 通过项目关系接口补录 `Attachment / SupportingEvidence` 后才正常显示。
   - 期望：从项目文件区或带项目上下文的文件上传后，应自动形成可追溯的项目证据关系。

4. **数字员工截止日期识别偏一天**
   - 原文“任务截止日期：2025-08-15”被建议为 `2025-08-16 02:00:00`。
   - 真人确认任务时已手工覆盖为正确的 `2025-08-15 18:00:00`。

#### 第 1 项结论

- 核心数据链路已经真实落库并在网页可见：项目 → 附件 → 事件 → 数字员工建议 → 真人确认 → 项目任务 → 质量工程师完成 → 里程碑与审计。
- 因事件/项目状态持久化不一致，第 1 项按“核心流程跑通、状态治理不通过”记录，不能据此宣称批次 3 网页已完全通过真实场景验收。

#### 第 2 项交付

- 交付文档：`docs/project/3157C_PILOT_AND_CONFIGURATION_REQUIREMENTS.md`
- 已记录真实试点范围和已落库结果。
- 已整理 4 个 P0/P1 问题、影响、疑似根因和修复要求。
- 已明确“一个平台、多种岗位视图”，不创建按岗位割裂的独立数据库。
- 已列出 8 类可配置能力和 L1–L4 配置分级。
- 已列出员工端、项目端、配置端和平台治理中心四类页面。
- 已列出配置中心开发前必须具备的后端能力。
- 已形成 5 条候选需求池条目。
- 状态：`complete`

#### 第 3 项交付

- 原型入口：`docs/project/prototypes/AIDT_BUSINESS_UX_PROTOTYPE.html`
- 原型说明：`docs/project/prototypes/README.md`
- 已完成四个可切换页面：
  - 员工“我的工作”
  - `3157C` 项目工作区
  - 业务化流程配置中心
  - 平台治理与配置发布
- 已完成“发起事项”交互弹窗，包含事项类型、项目、截止时间、优先级、说明和附件入口。
- 流程配置页提供可选择的业务节点、责任人、退回人、SLA、数字员工和永久人工边界。
- 项目页保留状态一致性告警，不使用当前后端错误状态冒充完成。
- 已生成桌面、弹窗和窄屏视觉证据，浏览器控制台无 warning/error。
- 未修改正式前端、后端、数据库和部署配置。
- 状态：`complete`

#### 第 4 项交付

- 正式功能：`CFG-001` 业务流程配置版本与发布。
- 新增配置契约、服务接口、SQL 服务、NotReady 实现和 7 个 API 操作。
- 支持配置草稿创建、Revision 并发更新、结构校验、发布和回滚。
- 发布动作生成可执行 `WorkflowDefinition / WorkflowVersion`，并已实际发起流程验证。
- 新增 5 项配置权限：读取、创建、更新、发布和回滚。
- 迁移 `20261031183454_Batch008ConfigurationCenter` 已在隔离库完成 up/down/re-up，并应用到主 `AIDT`。
- 隔离库全量后端测试 97/97 通过，0 失败、0 跳过。
- 前端类型检查、9/9 测试、OpenAPI 76 路径/90 operationId、生产构建通过。
- 能力接口和文档已同步 `CFG-001`。
- 交付文档：`docs/project/CONFIGURATION_CENTER_BACKEND_DELIVERY.md`
- 前序 4 个试点问题未在本项修复，按要求保留。
- 状态：`complete`

#### 第 5 项交付

- 员工工作台 `/` 增加真实事项发起弹窗和常见文件附件。
- 员工选择部门、类型、标题、说明和保密等级后，事项真实进入事件收件箱。
- 新增流程配置中心 `/configurations`，接入真实配置 API。
- 支持配置列表、创建、复制新版本、节点增删编辑、Revision 并发保存、校验、发布和回滚。
- 新增配置人员候选 API，允许项目经理选择有效真人处理人。
- 开发期发现并修复按钮早点击导致空部门提交，以及回滚后 WorkflowDefinition 名称未同步的问题。
- 前端 7 个测试文件、10/10 用例通过；OpenAPI 77 路径、91 operationId。
- 隔离库 SQL/API 97/97；正式部署 Playwright 13/13。
- 发布版本 `req-config-center-final2` 已部署到 `http://127.0.0.1:8088`。
- 交付文档：`docs/project/CONFIGURATION_CENTER_FRONTEND_DELIVERY.md`
- 前序 4 个问题按要求继续保留。
- 状态：`complete`

#### 第 6 项交付

- 完成研发工程师、项目经理、质量工程师、平台管理员四类身份的第二轮试点。
- 试点 A 使用真实项目经理作为经理节点处理人：
  - 研发节点成功审批；
  - 项目经理能看到待办，但缺少流程任务审批权限；
  - 平台管理员也不能处理分配给他人的任务；
  - 实例保持 `Running`，经理任务 `Pending`。
- 试点 B 将经理节点映射为平台管理员兼岗：
  - 研发工程师、平台管理员兼岗、质量工程师依次完成三个节点；
  - WorkflowInstance 为 `Completed`，三个任务全部 `Approved`；
  - 审计记录 Start + 3 次 Approve。
- 研发和质量岗位访问配置中心均返回 403。
- 项目经理可以创建/校验配置，但没有发布按钮；平台管理员可发布。
- 新增阻断：`project-manager` 缺少 `workflow-task:approve/return/terminate`。
- 新增体验问题：“我的待办”加载全部任务，已 Approved 的任务仍显示并保留操作按钮。
- 试点报告：`docs/project/STEP6_MULTI_ROLE_PILOT_REPORT.md`
- 状态：`complete_with_role_blocker`

#### 第 7 项交付

- 统计两个试点流程实例：1 个 `Completed`，1 个因项目经理权限阻断保持 `Running`。
- 全部流程完成率：1/2，50%。
- 全部任务完成率：4/5，80%。
- 管理员兼岗有效路径完成率：1/1，100%。
- `3157C` 证据对象完整性：10/10；但事件和项目状态一致性不通过。
- 成功多岗位实例证据链完整性：6/6。
- 自动化关联覆盖率：4/5，80%；文件自动项目关联失败。
- 因缺少历史人工工时和错误率基线，拒绝虚构“效率提升百分比”。
- 新问题总数：3 个 P0、3 个 P1。
- 验证报告：`docs/project/STEP7_KPI_VALIDATION_REPORT.md`
- 状态：`complete_not_production_ready`

### 阶段 18：前端导航与 IAM 权限批次

- [x] 完整读取批次任务书、冻结基线、需求池、交付、规划和规范
- [x] 从干净 `main` 创建 `feature/frontend-navigation-iam`
- [x] 完成变更计划、上下文包和影响分析
- [x] 完成组织树、人员、角色、权限、数据范围和高风险审核
- [x] 完成导航版本、模板、部门范围、停用恢复和个人偏好
- [x] 完成六类节点处理人策略和任务解析快照
- [x] 修复 `project-manager` 流程任务动作权限 P0
- [x] 新增迁移 009，隔离库 up/down/re-up
- [x] 主库备份、校验并应用 009
- [x] 最终隔离 SQL/API 104/104
- [x] 前端类型、组件、OpenAPI、生产构建
- [x] 源码桌面/390px Playwright 15/15
- [x] 真实项目经理审批、历史角色变更、转交和双人审核测试
- [x] 提交与非快进合并
- [x] 干净提交构建发布包
- [x] 正式部署与 Playwright
- [x] 应用回滚与数据库恢复验证
- [x] 新冻结标签和最终交付报告

#### 本批边界

- 事件状态、项目状态、附件自动项目关系、日期偏移和“我的待办”过滤保持原状并记录。
- 不连接真实钉钉、U9、ERP、DCS。
- 不启用真实外部模型。
- 本地 Cookie 身份不描述为生产 SSO。
