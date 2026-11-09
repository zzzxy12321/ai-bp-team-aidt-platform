# 前端导航与 IAM 权限批次交付报告

> 状态：`DEPLOYED_AND_VERIFIED_NOT_PRODUCTION_READY`
>
> 日期：2026-11-01
>
> 功能：`NAV-001`、`IAM-001`、`WFROLE-001`
>
> 分支：`feature/frontend-navigation-iam`
>
> 后续收尾：见 `FRONTEND_NAVIGATION_IAM_CLOSEOUT_DELIVERY.md`

## 🎯 结论

本批已完成组织、人员、角色、权限、数据范围、导航、流程业务角色和节点级授权的全栈纵向切片。

关键 P0 已修复：

```text
demo.pm
 → 收到 MANAGER_REVIEW
 → business-manager-reviewer 节点资格
 → workflow-task:approve 动作权限
 → 部门数据范围
 → 成功完成经理审批
```

不再依赖平台管理员兼岗。

## ✅ NAV-001

- 新增 `Navigation` Schema 和 4 张表。
- 导航工作区复用 `Identity.Department`。
- 支持系统、组织、部门工作区版本。
- 默认模板生成部门工作区、流程事项、业务视图、部门文件，并可选项目、听记和报表。
- 系统入口 `MY_WORK/TODO/EVENT_INBOX` 不可隐藏、停用或删除。
- 普通入口支持排序、显隐、启停和完整列表发布。
- 停用只停止展示，不删除业务数据；支持恢复。
- 后端按组织、部门、权限和个人偏好过滤当前导航树。
- 新增个人排序、收藏和隐藏 API 与前端偏好对话框。

## ✅ IAM-001

- 部门支持父级、路径、中心/部门/班组类型、排序和版本。
- 组织根为公司，部门树直接引用现有 Identity 主数据。
- 停用部门前检查人员、项目/对象、运行中流程、文件和下级组织。
- 人员支持查询、详情、本地邀请、开通、停用、恢复和批量导入。
- 支持主部门和兼职部门归属。
- 数据范围支持组织、部门、本人和参与项目。
- 角色支持系统/业务/Agent 服务分类、创建、复制、修改和停用。
- 角色权限矩阵和范围化用户角色分配已落地。
- 系统角色不可修改/停用；使用中的角色停用返回冲突。
- 禁止给自己授予角色或通过自己的角色新增权限。
- 高风险管理权限与系统角色分配进入双人审核；申请人不能自审。
- 全部管理变更写追加式审计。

## ✅ WFROLE-001

节点处理人策略：

- `SpecificUser`
- `BusinessRole`
- `DepartmentManager`
- `ProjectOwner`
- `InitiatorManager`
- `Agent`

实现证据：

- 配置发布校验处理人策略与部门范围。
- 任务创建时冻结候选人、角色 ID/Code/Version 和解析 JSON。
- 审批检查动作权限、当前处理人、冻结候选资格和部门范围。
- 角色成员后续变更不影响历史任务。
- 支持转交和升级；原责任人、操作人、时间、原因保留。
- `project-manager` 补齐审批/退回/终止动作权限。
- 永久人工边界未放宽，Agent 不能绕过高风险节点。

## 🗄️ 数据库

- 新迁移：`20261031195740_009_FrontendNavigationIamWorkflowRoles`
- 001–008 未修改。
- 应用表：51 → 57。
- 主库迁移前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_FRONTEND_NAV_009_PRE_20261101_040523.bak`
- 备份 SHA256：
  `112246EBB6F19AE5695B0F0BC694C1A92B54D8A312710F6F2FD7CFE125FDF235`
- 隔离 up/down/re-up：通过。
- 最终隔离库：`AIDT_FRONTEND_NAV_FINAL_20261101_041947`。
- 主库已应用 009：9 条迁移、57 张应用表、导航工作区 1、范围化角色分配 3、项目经理三类动作权限 3/3。

## 🔌 API / OpenAPI

- 新增 IAM、导航、流程业务角色、转交和升级 API。
- OpenAPI：113 路径、133 operationId。
- 所有写接口要求权限与 CSRF；关键写接口要求幂等键并写审计。

## 🖥️ 前端

- 新增 `/iam` 配置中心。
- 组织树、人员、角色权限矩阵、数据范围、导航配置、高风险审核均连接真实 API。
- `AppLayout` 不再硬编码导航，读取 `/api/v1/navigation/current`。
- 普通用户可维护个人排序、收藏和隐藏。
- 流程配置节点支持处理人策略和业务角色。
- 流程待办展示冻结策略、角色版本、原责任人和转交信息。

## 🧪 验证

当前已完成：

- .NET 构建：0 警告、0 错误。
- 默认后端测试：SQL 跳过版本通过。
- 最终隔离库 SQL/API：104/104，0 失败、0 跳过。
- 前端类型检查：通过。
- Vitest：8 个文件、11/11 通过。
- OpenAPI：113 路径、133 operationId。
- 生产构建：通过。
- 源码环境桌面 + 390px Playwright：15/15。
- 真实项目经理角色审批 E2E：通过。
- 导航创建/发布/停用/恢复 E2E：通过。
- 迁移 up/down/re-up：通过。

## 🚀 正式部署

- 合并提交：`0bd8547e1b0cda2818f76f1ac3751b9ba201b410`
- 部署版本：`req-navigation-iam-final1`
- 发布包：
  `D:\Apps\AIDT-Test\releases\AIDT-req-navigation-iam-final1.zip`
- 发布包 SHA256：
  `C5DA0827D1D00D1804234958439AB49EFD92BAACCFBD554D082D4EA364F68DEF`
- manifest `sourceRevision`：
  `0bd8547e1b0cda2818f76f1ac3751b9ba201b410`，不含 `-dirty`
- 部署前备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-navigation-iam-final1_20261101_042635.bak`
- 备份 SHA256：
  `4A82C57C73DA4C982FEEE549B6D5825939E9B6C4EC0A94EBCE4E4D024DD5AD7D`
- 正式部署 Playwright：15/15，0 失败、0 跳过、0 flaky。
- 应用回滚：final1 → `req-config-center-final3` 成功后重新部署 final1。
- 最终首页、API live/ready、Worker、AiGateway、capabilities 均 HTTP 200。

## 🛟 备份恢复

- `RESTORE VERIFYONLY`：通过。
- 独立恢复库：`AIDT_NAV_IAM_RESTORE_20261101_042649`。
- 恢复结果：9 条迁移、57 张应用表。
- 关键数据：导航工作区存在、范围化角色分配存在、项目经理三类动作权限存在。
- 恢复库在验证后清理，主库和备份保持不变。

## 🏷️ 冻结

- 新冻结标签：`req-navigation-iam-frozen-v1`
- 旧冻结标签 `req-config-center-frozen-v1` 未移动、未覆盖。
- 标签指向最终发布证据提交；源码发布提交仍为 `0bd8547`。

## 🚫 保持原状的冻结问题

本批未扩展修复：

1. 事件分析/确认状态持久化。
2. 项目状态持久化。
3. 文件上传自动形成项目证据关系。
4. 数字员工截止日期偏移。
5. 已完成任务仍出现在“我的待办”。

## ⚠️ 生产化差距

- 本地演示 Cookie 不是生产 SSO/OIDC/MFA。
- 邀请为本地受控开通，不发送真实邮件或钉钉消息。
- 未连接真实钉钉、U9、ERP、DCS。
- 未启用真实外部模型。
- SQL Server Express 单机环境不满足生产高可用、灾备和容量要求。
