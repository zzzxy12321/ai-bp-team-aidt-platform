# PMO-003 项目操作闭环交付报告

> 状态：`COMPLETE_DEPLOYED_AND_FROZEN`
>
> 分支：`feature/ui-step3-project-operations`
>
> 目标发布：`req-ui-step3-final1`
>
> 目标冻结：`req-ui-step3-frozen-v1`

## 🎯 交付范围

项目详情已形成完整操作链：

```text
编辑项目 → 更换负责人 → 管理成员 → 创建项目任务
 → 分配责任与期限 → 关联文件/听记/流程/对象证据
 → 自动更新项目进度 → 状态流转 → 刷新后保持一致
```

## 🧱 技术交付

- 新增 `POST /api/v1/projects/{id}/tasks`，使用 `Idempotency-Key`。
- 项目任务继续写入 `PMO.ProjectTask`，负责人必须为有效项目成员。
- 项目编辑复用 `BusinessObject + PMO.ProjectProfile`，负责人变更同步唯一 Lead 成员和主对象版本快照。
- 文件、听记、流程实例和业务对象继续写入 `PMO.ProjectRelation`。
- 进度由未取消项目任务与里程碑完成率统一计算，不开放任意百分比覆盖。
- 状态选择器只提供后端领域规则允许的下一状态。
- 本批无新迁移，数据库保持 11 条迁移、62 张应用表。

## 🧪 源码验证

- 隔离数据库：`AIDT_PMO_003_TEST_20261102_124500`。
- 测试前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PMO_003_PRE_TEST_20261102_124500.bak`。
- 备份 SHA256：
  `2D7B373F8DF40FBF81F8A5194F66EB17EAB8B47EB3A5EEDD5764A059701B3B6A`。
- 后端/SQL/API 与 MCP：146/146，0 失败、0 跳过。
- 前端类型检查：通过。
- Vitest：14 个文件、27/27 通过。
- OpenAPI：126 路径、146 operationId。
- PMO-003 三端 Playwright：桌面、1280、390px 3/3 通过。
- 完整源码 Playwright：31 通过、1 个部署专用 MCP 浏览器用例按设计跳过、0 失败。

## 🔐 权限与审计

- 任务创建要求 `project-task:create` 和项目管理范围。
- 成员、关系、编辑和状态动作继续要求原有项目管理权限。
- 普通项目成员不能通过前端隐藏绕过后端权限。
- 新增审计动作 `project.task_created`。
- 文件、听记、流程和对象关系继续使用关系新增审计。

## ⚠️ 当前边界

- 不提供任意项目进度编辑、任务编辑/取消和关系删除。
- 不新增项目组合、资源成本、工时或预算。
- 不改变 Agent、付款、质量放行、配方、设备、安全联锁、合同和重大赔付人工边界。

## 🚀 正式发布验证

- 功能提交：`cd81f0e`、`2f85e7c`、`01df3dd`；非快进合并 `e8db2b2`。
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-ui-step3-final1.zip`。
- 发布包 SHA256：
  `B50F0A19AF78EC2EAE4EAE4C811E6F482B92B80445E565F4CCB7F2502DA04541`。
- manifest sourceRevision：`e8db2b2131c6d9807c1f1bdf6dac7eedefcdd59b`，不含 `-dirty`。
- 最终部署时间：`2026-11-02T13:34:03.2051379+08:00`。
- 首次和最终正式部署 E2E 均为 32/32，0 失败、0 跳过、0 flaky；
  最终报告开始时间 `2026-11-02T13:34:10+08:00`，晚于部署时间。
- 应用回滚 `req-ui-step3-final1 → req-ui-step2-final1 → req-ui-step3-final1` 通过。
- 最终部署备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-ui-step3-final1_20261102_133355.bak`。
- 最终部署备份 SHA256：
  `C964476E04205F814967B9CA0DB2967F5039BBCF8291127CAF0CC5AA0601FEFD`；
  `RESTORE VERIFYONLY` 通过。
- 独立恢复库：`AIDT_UI_STEP3_FINAL1_RESTORE_20261102_1335`，
  11 条迁移、62 张应用表、`project.task_created` 审计 102 条、9 类项目关系可恢复。
- 当前页面、API、Worker、AiGateway、MCP 健康检查均 200。
- 冻结标签：`req-ui-step3-frozen-v1`。
