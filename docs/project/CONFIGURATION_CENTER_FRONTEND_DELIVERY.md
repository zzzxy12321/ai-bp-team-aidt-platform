# AIDT 配置中心前端与员工工作台交付

> 状态：`CONFIGURATION_CENTER_UI_DEPLOYED`
>
> 版本：`req-config-center-final2`
>
> 日期：2026-11-01

## 🎯 结论

第 5 项已完成并部署到测试入口：

```text
http://127.0.0.1:8088
```

员工工作台和流程配置中心均已接入真实 API/SQL Server，不是静态原型。

## 👤 员工工作台

路由：`/`

新增能力：

- “发起事项”弹窗。
- 快速选择研发测试申请、采购申请和质量异常。
- 选择所属部门、标题、事项说明和保密等级。
- 支持 PNG、JPG、PDF、Word、Excel、PPT、TXT、MD、JSON 附件。
- 上传文件后自动创建事件附件引用。
- 提交后真实进入事件收件箱。
- 保持真人确认规则：数字员工只生成建议，不自动派发任务。
- 页面加载完成前禁用发起按钮，避免空部门提交。

## 🎛️ 流程配置中心

路由：`/configurations`

仅拥有 `configuration:read` 的用户显示入口。

已接入功能：

- 查询草稿、已校验、已发布和已退役配置。
- 新建流程配置草稿。
- 从历史版本复制新草稿。
- 选择业务对象类型、所属部门和开始节点。
- 选择有效真人处理人。
- 增加和删除节点。
- 编辑节点编码、名称、处理人、下一节点和完成条件。
- 配置简单字段条件。
- Revision 乐观并发保存。
- 结构校验和错误定位。
- 平台管理员发布。
- 平台管理员回滚历史已发布版本。
- 发布后展示可执行 WorkflowVersion。
- 已发布配置只读，避免直接覆盖生产流程。

## 🔌 前端契约

新增模块：

- `frontend/src/api/modules/configurationApi.ts`
- `frontend/src/api/types/contracts.ts`
- `frontend/src/pages/ConfigurationCenterPage.vue`

新增配置人员候选接口：

```text
GET /api/v1/configurations/member-candidates
```

该接口只返回当前组织的有效真人用户，避免项目经理因缺少流程策略权限而无法选择处理人。

## ✅ 自动化验证

- 前端类型检查：通过
- Vitest：7 个文件、10/10 通过
- OpenAPI：77 路径、91 operationId
- 前端生产构建：通过
- 隔离库后端 SQL/API：97/97，0 失败、0 跳过
- 正式部署 Playwright：13/13，0 失败
- 覆盖桌面员工入口、文件、听记、项目、收件箱、流程策略、新员工事项和配置中心完整发布链路
- 覆盖 390px 移动端关键路由，无页面级横向溢出

## 🖼️ 验收证据

- `docs/project/test-artifacts/desktop-configuration-center.png`
- `docs/project/test-artifacts/desktop-my-work.png`
- `docs/project/test-artifacts/mobile-390-projects.png`
- `docs/project/test-artifacts/playwright-report.json`

## 🚀 部署

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-config-center-final2.zip`
- SHA256：`5E513F0820279B189F4611A54A60CAFC97268DB2E932018CFF608B0B09952E06`
- 部署版本：`req-config-center-final2`
- 上一稳定部署：`req-batch3-final2`；final1 保留但由 final2 取代
- 部署备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-config-center-final2_20261101_025759.bak`
- 备份 SHA256：`3FC621DB7E6777988290A8127392718A19543BE88AE9EF913805B4CDED9EC110`

## 🐞 本项发现并修复

- 页面加载完成前“发起/新建”按钮可点击，可能提交空部门。
  - 已增加加载门禁。
- 配置回滚后 WorkflowDefinition 名称仍保留新版本名称。
  - 已修正为回滚后的配置名称，并增加 SQL/API 回归断言。

## 🧾 前序问题保留

按要求，以下 4 项没有在本轮修复：

1. 事件状态未持久化。
2. 项目状态未持久化。
3. 附件未自动形成项目证据关系。
4. 数字员工日期识别偏移一天。

## 🎯 下一步

第 6 项：使用正式部署入口开展第二轮岗位试点。

建议第二轮至少覆盖：

- 项目经理配置流程。
- 平台管理员发布或回滚。
- 研发工程师发起事项并上传附件。
- 质量工程师处理节点任务或项目任务。
