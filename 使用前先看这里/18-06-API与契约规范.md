# API 与契约规范

## 🎯 基本原则

- 使用 REST 风格和 `/api/v1` 版本前缀。
- OpenAPI 是接口契约的机器可读源文件。
- 前后端以契约为准，不依赖口头约定。
- API 修改必须分析所有调用方。
- 写接口必须具备权限、幂等和审计。

## 🌐 URL 规范

```text
/api/v1/{resource}
/api/v1/{resource}/{id}
/api/v1/{resource}/{id}/{sub-resource}
/api/v1/{resource}/{id}/actions/{action}
```

示例：

```text
GET  /api/v1/workflow-instances
GET  /api/v1/workflow-instances/{id}
POST /api/v1/workflow-instances
POST /api/v1/workflow-tasks/{id}/actions/approve
POST /api/v1/workflow-tasks/{id}/actions/return
```

规则：

- 资源名使用复数 kebab-case。
- 路径使用小写。
- 禁止 `/getData`、`/doUpdate` 等 RPC 风格。
- 动作只在无法用资源 CRUD 表达时使用。
- 不在 URL 中放敏感信息。

## 📦 标准响应

```json
{
  "code": "OK",
  "message": "success",
  "data": {},
  "traceId": "trace-id"
}
```

错误响应：

```json
{
  "code": "WORKFLOW_VERSION_CONFLICT",
  "message": "流程版本已变化，请刷新后重试",
  "data": null,
  "traceId": "trace-id"
}
```

规则：

- HTTP 状态码表达协议级结果。
- JSON `code` 表达稳定业务错误码。
- 禁止 HTTP 200 包装失败。
- 前端不能依赖 `message` 判断业务类型。

## 📄 请求与响应

- JSON camelCase。
- 时间 ISO 8601 UTC。
- ID 使用字符串传输，避免 JavaScript 数值精度问题。
- 金额同时传输币种或明确统一币种。
- DTO 与领域实体分离。
- 不返回数据库内部字段，如密码哈希、审计备注、软删除标记。
- 列表返回分页元数据。

## 📑 分页、筛选和排序

```text
GET /api/v1/workflow-instances?page=1&pageSize=20&status=pending&sort=updatedAt:desc
```

规则：

- 默认分页。
- `pageSize` 最大值受控。
- 排序字段使用白名单。
- 筛选字段必须建立索引或明确性能风险。
- 响应包含 `page`、`pageSize`、`total`、`items`。

## 🔁 幂等

外部写命令或可重复点击动作必须支持：

```http
Idempotency-Key: <uuid>
```

规则：

- 同键同内容返回原结果。
- 同键不同内容返回冲突。
- 幂等记录有有效期和清理策略。
- 前端重复提交不能产生重复流程、任务或外部命令。

## 🧷 并发控制

- 更新接口携带 `version` 或 ETag。
- 版本不一致返回 `409`.
- 任务动作使用条件更新。
- 不允许后写覆盖先写且无提示。

## ⚠️ 错误码

错误码格式：

```text
DOMAIN_REASON
```

示例：

- `WORKFLOW_NOT_FOUND`
- `WORKFLOW_VERSION_CONFLICT`
- `TASK_ALREADY_PROCESSED`
- `PERMISSION_DENIED`
- `DATA_SCOPE_DENIED`
- `EXTERNAL_SERVICE_UNAVAILABLE`

规则：

- 错误码稳定，不随文案变化。
- 每个错误码有说明、HTTP 状态和前端处理建议。
- 不把数据库异常和堆栈返回客户端。

## 🔐 权限

- 每个端点声明所需动作权限。
- 数据范围在 Application 层强制校验。
- 隐藏菜单不是权限。
- 服务身份和真人身份分别审计。
- 数字员工不得长期持有全量权限。

## 🧾 OpenAPI

OpenAPI 必须包含：

- 路径和版本
- 请求/响应 Schema
- 错误响应
- 鉴权方式
- 幂等和分页
- 示例
- 权限声明

变更流程：

1. 修改 OpenAPI。
2. 生成或更新前端类型。
3. 更新后端实现。
4. 更新契约测试。
5. 检查旧客户端兼容性。
6. 更新功能地图。

## 📈 版本与兼容

- 不以内部实现变化升级 API 大版本。
- 新增可选字段通常兼容。
- 删除字段、改类型或改语义属于破坏性变更。
- 破坏性变更需要新版本、迁移计划和 ADR。
- 弃用接口必须有时间表和调用方清单。

## 🚫 API 反模式

- 页面直接拼接 URL
- 前后端各维护一份不一致类型
- 返回整个数据库实体
- 无分页全量列表
- 全用 HTTP 200
- 错误文案作为业务判断
- 无幂等写接口
- 无权限声明
- 修改接口后不更新契约和测试
