# META-001-A 业务对象建模与字段编辑后端交付

## 🎯 结论

META-001-A 已完成源码实现、013 迁移、隔离库验证、全量后端/MCP、正式部署、
正式 E2E、应用回滚、数据库恢复和冻结。
对象类型由授权平台管理员通过 API 维护，业务对象编辑具备字段校验、版本快照、审计、
幂等和并发控制。5B 前端建模界面不在本批范围。

## 🧩 Feature 范围

- `IDEA-META-001 / META-001-A`
- 分支：`feature/ui-step5a-object-modeling-backend`
- 基线：`main@9b425a3`

## 🔌 API

- `POST /api/v1/object-types`
- `PUT /api/v1/object-types/{id}/draft`
- `POST /api/v1/object-types/{id}/actions/validate`
- `POST /api/v1/object-types/{id}/actions/publish`
- `POST /api/v1/object-types/{id}/actions/retire`
- `POST /api/v1/object-types/{id}/actions/restore`
- `GET /api/v1/object-types/{id}/versions`
- `PUT /api/v1/business-objects/{id}`

## 🗃️ 数据和兼容

- 新增 `Metadata.ObjectTypeVersion`；扩展 ObjectType/FieldDefinition。
- 九类字段：Text、LongText、Number、Date、DateTime、Boolean、Select、User、Department。
- 字段属性：Code/Key、显示名、必填、最大长度、默认值、敏感性、选项、列表显示、筛选、排序。
- 现有五种对象类型回填 Published v1；业务对象、工作流 `objectTypeCode` 和 PMO PROJECT 未重建。
- 删除字段不删除历史业务对象字段值，更新时保留旧键。

## 🔐 权限与安全

- 新增 `object-model:read/create/update/manage/approve`，仅平台管理员。
- `business-object:update` 继续执行动作权限、组织/部门范围和环境规则。
- Core 字段继续对非平台管理员脱敏。
- 对象模型和业务对象编辑均写追加式审计；写接口要求幂等键。

## 🧪 源码结果

- 迁移：13 条、67 张应用表；013 up/down/re-up 通过。
- 后端/SQL/API：163/163；MCP：11/11；合计 174/174。
- Vitest：28/28；OpenAPI：142 路径、163 operationId。
- 源码 E2E：37 通过、1 个部署专用 MCP OAuth 用例跳过、0 失败。
- 构建、格式、生产前端构建通过；NuGet/npm 生产依赖无已知漏洞。

## 🚦 发布门禁

- [x] 非快进合并 `main`
- [x] 干净构建 `req-ui-step5a-final1`
- [x] 正式部署与 E2E
- [x] 应用回滚
- [x] 最终备份恢复
- [x] 冻结标签 `req-ui-step5a-frozen-v1`

## 📦 正式发布

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step5a-final1.zip`
- 发布包 SHA256：`8F66CF0090C9F8C43AFDB14455B391E9438CDAD189024242C3E697D743C3DB06`
- 发布源码：`f3d71c6d51913fba07d2134ecb53b6e57d21cbd8`
- 部署时间：`2026-11-02T22:53:19.4628230+08:00`
- 部署前备份 SHA256：`8BC9164F465AA1836CB9DECA64DC1C8C33D6DC8C63B6E15CB92FF9280AB706E7`
- 正式 E2E：38/38，0 失败、0 跳过、0 flaky；开始时间
  `2026-11-02T22:53:27.898+08:00`，晚于最终部署。
- 应用回滚：`final1 → req-ui-step4-final1 → final1`，均通过健康检查。
- 最终备份：`AIDT_META_001_A_FINAL1_20261102_225845.bak`，
  SHA256 `673AA522C4C2F2BCA65EA7C2D9006F1456E3B48935EFA839D485C7A1DAD7C2EB`，
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库：`AIDT_META_001_A_FINAL1_RESTORE_20261102_225845`，
  ONLINE、13 迁移、67 张应用表、5 个现有类型、11 个版本快照、5 项建模权限。
- 首次试部署包因 Windows 动态端口排除覆盖 6380，未形成有效正式证据；原包保留为
  `AIDT-req-ui-step5a-final1-predeploy.zip`，SHA256
  `47D9EA0C2A053CBECF5B81A5508641830FDB0825CA5A1CB8873E19DA6F4E2EBC`。
- 部署脚本增加显式 `-GarnetPort` 参数；本机使用未排除的 6480，默认仍为 6380，
  回滚脚本沿用部署状态中的端口。
