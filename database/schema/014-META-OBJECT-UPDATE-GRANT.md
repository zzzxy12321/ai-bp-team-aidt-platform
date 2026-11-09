# 014 业务对象更新权限兼容

## 🎯 范围

5B 要求普通业务用户编辑已发布表单实例。014 不修改表结构，只把已存在的
`business-object:update` 权限授予既有业务角色：

- `rd-engineer`
- `quality-engineer`
- `supply-specialist`
- `finance-approver`
- `production-team`
- `project-manager`

## 🔐 边界

- 仍要求组织范围、部门范围、环境和 `expectedVersion`。
- 不授予 `object-model:*`，普通员工不能建模。
- Core 字段继续在服务端脱敏，编辑时未提交的 Core 值保持原值。

## 🧪 门禁

- 隔离库 `13→14→13→14`，业务角色授权数 `0→6→0→6`。
- 应用表仍为 67，最大迁移 014。
