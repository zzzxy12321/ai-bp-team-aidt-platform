# UX-003 影响分析

## 🔄 调用链

```text
Vue MyWorkPage
 → GET /api/v1/my-work
 → SqlEmployeeWorkspaceQueryService
 → WorkflowTask / ProjectTask / EventInbox / MinutesActionItem /
   McpHumanConfirmation / AgentRun 只读查询
 → WorkItemViewDto + 中文展示映射 + 排序 + 去重
 → 原业务详情深链（流程 / 项目 / 收件箱 / 听记 / MCP 确认 / Agent）
```

## 🗃️ 数据影响

- 无新表、无字段变更、预计无迁移。
- 不改变任何业务状态。
- 活动记录仍由原详情动作写入。

## 🔐 权限影响

- 保持现有权限码不变。
- 聚合查询分别检查每个来源的读取/确认权限。
- 组织、部门、项目、环境过滤仍在服务端执行。
- 无权限来源不进入展示投影，不通过空卡或锁卡泄露标题。

## 🧪 回归范围

- 原有 UX-001 发起事项。
- 流程待办与流程详情。
- 项目创建、项目任务与详情。
- 事件分析/确认/任务去重。
- 听记分析、行动项确认。
- MCP 高风险确认深链。
- Agent 运行确认和停止。
- 文件中心与管理权限入口。

## ↩️ 回滚

代码回滚到 `main@6a378f5` 即可恢复旧 `/my-work` 模型。由于本批无数据库变更，
不需要数据库结构回滚；新生成的测试业务数据不属于结构迁移。
