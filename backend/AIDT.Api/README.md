# AIDT.Api

## 📌 职责

HTTP 适配层。负责路由、请求绑定、身份解析接缝、权限过滤器接缝、统一响应、全局异常、TraceId、健康检查和 OpenAPI 输出。

## 🔌 当前端点

- `GET /health/live`
- `GET /health/ready`
- `GET /openapi/v1.json`
- `GET /api/v1/platform/capabilities`
- `GET /api/v1/auth/session`
- `GET /api/v1/permissions/me`
- 对象、流程、任务、审计、Agent、连接器和配置契约骨架
- `/api/v1/iam/*`：组织、人员、角色、权限、数据范围和高风险审核
- `/api/v1/iam/access-requests/{id}/actions/reject`：第二管理员填写意见后驳回高风险授权
- `/api/v1/navigation/*`：当前导航、工作区版本与个人偏好
- `/api/v1/workflow-roles/*`、`/workflow-tasks/{id}/actions/transfer|escalate`
- `/api/v1/projects/{id}`、`/members`、`/tasks`、`/relations`、`/actions/change-status`：
  项目编辑、成员、任务、证据和状态操作闭环
- `/api/v1/agents*`：有效候选、治理列表/详情、草稿、校验、发布、停用、恢复、
  版本、回滚和高风险变更审核
- `/api/v1/departments/{id}/workspace`、`/api/v1/navigation/current`：三层部门优先导航
- `/api/v1/configurations/{id}/versions|actions/simulate|actions/disable|actions/restore`：
  部门模板生命周期和试运行
- `/api/v1/workflow-tasks/{id}/form`、`/form-draft`、`/actions/ai-review`：
  冻结任务表单、草稿和规则模拟审查

## 🔐 状态

- 匿名访问器只用于阶段 A 启动，不提供伪造登录。
- 持久化未就绪时，依赖数据库的查询和写入返回 `503 PERSISTENCE_NOT_READY`。
- 本地 Cookie/CSRF、RBAC、部门和环境范围已接入；正式 SSO/OIDC/MFA 仍待生产身份源。
- 项目经理只能管理授权部门角色成员；organization scope 和高风险规则仍受后端最终校验。
- 数字员工服务身份不能登录治理页面；治理写接口均要求真人身份、权限、数据范围、
  幂等键和并发版本。
- 权限变化或旧 Cookie 缺授权指纹时 `/api/v1/auth/session` 返回 `Stale` 和重登提示。

## 🛠️ 命令

```powershell
dotnet run --project backend/AIDT.Api --launch-profile http
```

默认监听 `http://127.0.0.1:5080`。
