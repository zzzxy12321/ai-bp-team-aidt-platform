# SCN-001-B 上下文包

## 🎯 当前目标

在 `feature/ui-step7b-scenario-center-ui` 完成岗位业务场景中心前端、试用闭环、
配置治理、真实聚合、全量验证和最终冻结。

## 🧱 已确认基线

- `main`：`1b33a9f518a320e4561582bcad3ece2d3e611400`
- 前序冻结：`req-ui-step6-frozen-v1`、`req-ui-step7a-frozen-v1`
- 当前部署：`req-ui-step7a-final1`
- 最大迁移：017；71 张应用表；6 个已发布场景
- 7A 测试基线：后端/SQL/API 181/181、MCP 11/11、Vitest 41/41、
  OpenAPI 179/200、正式部署 E2E 45/45
- 现有真实流程：
  `CUSTOMER_SAMPLE_TO_PRODUCTION`、`PURCHASE_TO_PAYMENT`、
  `FINANCE_PAYMENT_REVIEW`、`BATCH_DEVIATION_CAPA`
- 现有真实业务入口：业务对象、流程实例/任务、事件收件箱、项目任务、
  文件、听记、AgentRun、项目和审计

## 📐 已确认架构决策

- 场景表只保存配置、不可变版本和真实业务链 ID。
- 启动通过 7A 编排服务调用原业务服务，前端不直接拼装流程实例或任务。
- 场景进度不在场景表或前端复制；新增只读聚合从真实流程、任务、项目和事件读取。
- 配置只能引用注册资源；任何 SQL、Shell、HTTP、HTML、JS 或脚本文本均为非法输入。
- 永久人工边界在服务端和 Worker 双重约束，前端负责清楚展示。

## 📦 前端现状

- Vue 3 + Element Plus，路由在 `frontend/src/router/index.ts`。
- 导航来自服务端 Navigation API，不能只靠前端硬编码入口。
- 已存在 Agent、配置中心、岗位工作台、项目、文件、听记、工作流和审计页面。
- 7A 已生成 `Scenario*` TypeScript 契约，但尚未生成场景 API 模块和页面。

## 🧪 完成门禁

- 六类场景在网页可见并按部门/岗位筛选正确。
- 六类场景可从网页预检、上传材料/选择听记并真实启动。
- 刷新、重新登录和深链返回后启动记录一致。
- “我的场景”来自服务端聚合真源，等待人、截止、当前步骤、状态和下一步正确。
- 配置写 SQL Server，生命周期、权限、版本、审计和双人审核真实生效。
- 场景卡片明确 Demo、审批、数字员工、风险和人工边界。
- 桌面 1440、1280 和移动 390px 可通过。
- 原 45/45 正式 E2E 无回归。
- 干净构建、正式部署、应用回滚和数据库独立恢复通过。

