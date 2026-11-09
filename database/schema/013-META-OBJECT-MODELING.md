# 013 对象建模后端 Schema

## 🎯 范围

- 扩展 `Metadata.ObjectType`：状态、修订、行版本、发布时间和停用时间。
- 扩展 `Metadata.FieldDefinition`：默认值、选项、列表显示、筛选和排序。
- 新增 `Metadata.ObjectTypeVersion`：不可变对象模型版本快照。
- 注入 `object-model:read/create/update/manage/approve` 权限。

## 🔐 兼容

- 现有 CUSTOMER_SAMPLE、QUALITY_DEVIATION、PURCHASE_REQUEST、
  CUSTOMER_COMPLAINT、PROJECT 回填为 Published v1。
- 现有业务对象、工作流和历史版本不改写。
- 删除字段只影响当前草稿定义，不级联删除业务对象 JSON 或历史快照。

## 🧪 门禁

- 001–012 未修改，只新增 013。
- 隔离库 up/down/re-up：12/66 → 13/67 → 12/66 → 13/67。
- 主库应用前备份和 `RESTORE VERIFYONLY` 通过。
