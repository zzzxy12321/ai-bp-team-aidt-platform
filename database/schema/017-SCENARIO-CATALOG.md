# SCN-001-A 场景数据模型

## 🗂️ 表

| 表 | 责任 |
|---|---|
| `Scenario.ScenarioDefinition` | 稳定编码、所属部门、当前生命周期与发布指针 |
| `Scenario.ScenarioVersion` | 不可变版本、业务配置、引用快照、校验和影响分析 |
| `Scenario.ScenarioLaunch` | 场景版本到真实对象、流程、任务、项目、事件、Agent 和材料 ID 的映射 |

## 🔗 数据边界

- 场景不复制 `BusinessObject` 字段、流程/任务状态或审计账本。
- 启动映射只保存真实业务链 ID 和展示链接。
- 发布版本冻结对象类型、流程版本和 AgentProfile 版本。
- 历史启动按 `ScenarioVersionId` 解释，不跟随当前最新版本漂移。
