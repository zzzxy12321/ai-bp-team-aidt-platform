# AIDT 配置中心最小可执行后端交付

> 状态：`CONFIGURATION_CENTER_BACKEND_READY`
>
> 日期：2026-11-01
>
> 功能编号：`CFG-001`

## 🎯 结论

配置中心最小后端闭环已完成：

```text
创建草稿
 → 修改草稿
 → 结构/对象/处理人校验
 → 平台管理员发布
 → 生成可执行 WorkflowDefinition / WorkflowVersion
 → 实际发起流程
 → 发布新版本
 → 回滚到历史版本
```

本轮只完成后端能力，尚未把正式网站配置页面接入这些接口。

## 🔌 API

| 方法 | 路径 | 作用 | 权限 |
|---|---|---|---|
| GET | `/api/v1/configurations` | 分页查询配置版本 | `configuration:read` |
| GET | `/api/v1/configurations/{id}` | 查询配置详情 | `configuration:read` |
| POST | `/api/v1/configurations` | 创建配置草稿 | `configuration:create` |
| PUT | `/api/v1/configurations/{id}` | 修改草稿，使用 Revision 乐观并发控制 | `configuration:update` |
| POST | `/api/v1/configurations/{id}/actions/validate` | 校验配置并保存影响分析 | `configuration:update` |
| POST | `/api/v1/configurations/{id}/actions/publish` | 发布并生成可执行流程版本 | `configuration:approve` |
| POST | `/api/v1/configurations/{id}/actions/rollback` | 回滚到历史已发布版本 | `configuration:manage` |

创建、校验、发布和回滚均要求 `Idempotency-Key`。

## 🧩 配置结构

最小可执行配置包含：

- 配置编号 `ConfigurationKey`
- 配置名称和说明
- 所属部门
- 业务对象类型 `ObjectTypeCode`
- 开始节点 `StartNode`
- 节点列表
  - 节点编码和名称
  - 节点处理人用户名
  - 下一节点
  - 是否审批后完成
  - 可选条件字段、操作符和值

节点结构最终转换为现有 Workflow 引擎可解析的 `DefinitionJson`。

## ✅ 校验规则

- 配置编号必须为稳定大写编码。
- 所属部门必须有效且在当前用户数据范围。
- 业务对象类型必须为 `ANY` 或已注册对象类型。
- 节点数量至少为 1，编码不能重复。
- 开始节点和所有下一节点必须存在。
- 不允许从开始节点形成循环。
- 不允许存在不可达节点。
- 至少存在一个完成节点。
- 每个节点必须有处理人。
- 处理人必须是当前有效的真人用户，不能是服务身份。
- 条件操作符只允许：
  - `equals`
  - `notEquals`
  - `greaterThan`
  - `greaterThanOrEqual`
- 节点超过 20 个时产生拆分警告。

校验结果写入 `ImpactAnalysisJson`，状态在 `Draft / Validated` 间切换。

## 🚀 发布与执行

发布时：

1. 重新执行完整校验。
2. 按配置编号查找或创建 `WorkflowDefinition`。
3. 自动计算下一个 `WorkflowVersion.VersionNumber`。
4. 将旧发布流程版本标记为 `Retired`。
5. 创建新的 `Published` WorkflowVersion。
6. 将配置状态更新为 `Published`。
7. 保存发布人、发布时间和 PublishedWorkflowVersion 引用。
8. 写入审计。

隔离库测试已使用发布后的 Definition/Version 实际调用：

```text
POST /api/v1/workflow-instances
```

返回 `201 Created`。

## ↩️ 回滚

回滚要求：

- 目标配置状态为 `Retired`
- 目标配置存在 PublishedWorkflowVersion
- 只有具有高权限配置管理权限的身份可操作

回滚后：

- 目标配置恢复为 `Published`
- 当前其他同编号发布配置变为 `Retired`
- 目标 WorkflowVersion 恢复为 `Published`
- 其他 WorkflowVersion 标记为 `Retired`
- 历史实例仍保留原 workflowVersionId

## 🔐 权限

新增权限：

- `configuration-create`
- `configuration-update`
- `configuration-publish`
- `configuration-rollback`

授权：

- `platform-admin`：读取、创建、修改、发布、回滚
- `project-manager`：读取、创建、修改
- 发布和回滚仅平台管理员可执行

## 🗄️ 数据库

- Schema：`Configuration`
- 主表：`Configuration.ConfigurationVersion`
- 新增迁移：`20261031183454_Batch008ConfigurationCenter`
- 应用表数量：仍为 51
- 未新增重复配置表

新增字段：

- `Name`
- `Description`
- `ConfigurationType`
- `OrganizationId`
- `DepartmentId`
- `Revision`
- `UpdatedBy`
- `UpdatedAt`
- `PublishedBy`
- `PublishedWorkflowVersionId`

迁移前备份：

```text
C:\ProgramData\AIDT\Backups\AIDT_PRE_BATCH4_008_20261101_023726.bak
```

SHA256：

```text
38E0AB14005613AEB0C3B861C0A4B363E1932F9D1AB2AEAE177E7CD5B9B06919
```

## 🧪 验证结果

- 隔离库：`AIDT_CFG_BATCH4_TEST_20261101_023700`
- 迁移顺序：007 → 008 up → 008 down → 008 up
- 主库：008 已应用，8 条 EF 迁移
- 配置权限：5 项
- 无效组织配置：0
- 全量后端测试：97/97，0 失败、0 跳过
- 配置中心专项：创建、校验、禁止普通经理发布、管理员发布、第二版发布、回滚、实际发起流程均通过
- `.NET` 构建：0 警告、0 错误
- `dotnet format`：通过
- EF 模型：无待生成迁移
- NuGet 漏洞检查：无已知漏洞
- 前端类型、9/9 测试、OpenAPI 76/90、生产构建：通过

## 🚫 当前边界

- 正式配置中心页面尚未接入。
- 节点 SLA、附件要求和数字员工策略仍通过原有工作流节点策略 API 配置。
- 本轮校验不是完整流程沙箱执行，只验证结构、对象、处理人、可达性和风险提示。
- 当前部署的 `req-batch3-final2` 仍运行旧二进制；新后端将在配置前端完成后的版本中统一部署。

## 🧾 前序问题保留

用户已要求不在本项混入前序修复。以下问题继续保留：

1. `P0` 事件分析/确认状态未持久化。
2. `P0` 项目状态提示成功但主对象未持久化。
3. `P1` 项目上传附件未自动形成项目证据关系。
4. `P1` 数字员工截止日期识别偏移一天。

这些问题记录在：

- `docs/project/3157C_PILOT_AND_CONFIGURATION_REQUIREMENTS.md`
- `findings.md`

## 🎯 下一步

第 5 项：开发正式配置中心前端和员工工作台。

前端接入前建议先完成一次接口联调评审，明确：

- 草稿编辑中的 Revision 冲突提示。
- 校验错误如何逐项定位到节点。
- 发布/回滚的二次确认和影响范围展示。
- 前序 4 个问题是否与本轮前端一起修复。
