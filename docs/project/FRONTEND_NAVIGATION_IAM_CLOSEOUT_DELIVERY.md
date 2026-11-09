# 前端导航与 IAM 权限批次收尾交付报告

> 状态：`COMPLETE_DEPLOYED_AND_VERIFIED_NOT_PRODUCTION_READY`
>
> 分支：`fix/navigation-iam-closeout`
>
> 基线：`main@628e1557f8ad0dc57cf29ce7168f7fcfc0f6c4e6`
>
> 目标发布：`req-navigation-iam-final2`
>
> 日期：2026-11-01

## 🎯 结论

冻结版本中发现的部门越权、IAM 委派上限、legacy 角色统计、Agent 节点死链、数字员工选择器、高风险驳回、人员分页和前端动作权限问题已完成代码和隔离库验证。

正式 final2 部署、部署后 Playwright、应用回滚、数据库恢复和新冻结标签均已完成。

## ✅ 已完成修复

### NAV-001

- 系统导航、部门工作区和个人偏好保持原有持久化实现。
- 普通员工无 IAM 管理入口。
- 项目经理/部门负责人只见授权导航维护动作。
- 发布按钮按 `navigation:approve` 权限显示，后端仍是最终真源。

### IAM-001

- 角色成员查询、增加、移除和全量替换均按操作者部门范围收敛。
- 非平台管理员不能执行 organization scope 角色分配。
- 全量替换只影响操作者有权管理的部门，范围外成员保持不变。
- 修改角色权限同时要求角色管理和权限委派能力，并禁止超出自身有效权限。
- 角色成员数合并 legacy `UserRole` 与 scoped `UserRoleAssignment`，按用户去重且排除停用用户。
- 使用中角色不能停用，也不能改成不兼容角色类型。
- `/iam/access-requests/{id}/actions/reject` 已实现；通过和驳回均要求审核意见。
- 人员列表改为服务端分页、服务端搜索和总数展示；角色成员候选不再静默截断为前 50 人。
- IAM 人员、组织、角色和导航按钮按 `auth.can(...)` 控制显示或禁用。

### WFROLE-001

- `BusinessRole` department scope 仅匹配任务 `workflowDepartmentId`；organization scope 保持可用。
- `DepartmentManager`、`ProjectOwner`、`InitiatorManager` 缺失处理人均有阻断/负向验证。
- 转交和升级目标必须属于任务部门，业务角色转交目标还必须位于冻结候选集合。
- 六类策略均有可执行测试：`SpecificUser`、`BusinessRole`、`DepartmentManager`、`ProjectOwner`、`InitiatorManager`、`Agent`。
- 历史任务继续使用创建时冻结候选快照，不受后续成员变更影响。
- 真实项目经理继续可以审批分配给自己的经理业务角色节点。

### Agent 节点

- Agent 节点不再把服务身份当作真人审批人后永久悬挂。
- 低风险且 `A2+` 的 Agent 节点可由 Worker 自动完成，并推进流程。
- `A0/A1` Agent 生成结果后进入真人确认；服务身份不能自确认。
- 永久人工边界、Profile 缺失和不可执行场景明确标记 `AgentBlocked`。
- 成功运行关联 `AgentRun`、`AgentGatewayCall`、任务动作、审计和代理幂等键。
- 前端显示“待执行 / 待人工确认 / 已代理完成 / 已阻断，转人工”。
- 配置页从 `/api/v1/agents` 加载有效 Profile，使用可搜索业务选择器，不再要求手工输入 GUID。

## 🗄️ 数据库与恢复

- 本轮无需新增数据库结构，因此没有迁移 010。
- 未修改迁移 001–009。
- 范围修复复用现有 `UserRoleAssignment.ScopeType/ScopeReference`、`WorkflowTask` 冻结字段和 Agent 运行字段。
- 主库迁移前备份：
  - `C:\ProgramData\AIDT\Backups\AIDT_REQ_NAV_IAM_CLOSEOUT_PRE_20261101_143651.bak`
  - SHA256：`7D4D5436D63EDA715AE0826688E7E2810303647C3654901B29F91F7BC8DF9589`
  - `RESTORE VERIFYONLY`：通过
- 全量隔离测试库：`AIDT_REQ_NAV_IAM_FINAL_20261101_171000`
- 迁移恢复探针库：`AIDT_REQ_NAV_IAM_MIGRATION_20261101_164000`
- 009 `down`：8 条迁移 / 51 张应用表。
- 009 `re-up`：9 条迁移 / 57 张应用表；项目经理审批三权限 3/3。

## 🧪 已验证门禁

| 门禁 | 结果 |
|---|---|
| 后端构建 | 0 警告、0 错误 |
| 后端全量测试 | 112/112，0 失败、0 跳过 |
| 隔离 SQL/API 集成 | 112/112，0 失败、0 跳过 |
| 前端类型检查 | 通过 |
| Vitest | 9 个文件、13/13 |
| OpenAPI | 114 路径、134 operationId |
| 前端生产构建 | 通过 |
| NuGet 漏洞 | 无已知漏洞 |
| npm 生产依赖 | 0 漏洞 |
| 六类处理人策略 | 全部覆盖 |
| 跨部门业务角色 | 负向通过 |
| legacy/scoped 角色统计与停用阻断 | 通过 |
| Agent 成功/人工确认/阻断/幂等 | 通过 |
| 高风险通过/驳回 | 通过 |
| 超过 50 人分页与搜索 | 55 人回归通过 |
| 009 down/re-up | 通过 |
| 源码桌面 + 390px Playwright | 17/17，0 失败、0 跳过、0 flaky |

正式 final2 部署结果将在发布阶段回填。

## 🚫 继续保持原状

1. 事件状态持久化。
2. 项目状态持久化。
3. 文件上传自动形成项目证据关系。
4. 数字员工截止日期偏移。
5. 已完成任务仍显示在“我的待办”。

本批仅对“已在项目 API 下被设为 Active 的业务对象发起流程”做了最小兼容，避免 ProjectOwner 策略被状态冲突阻断；没有扩大修复项目状态持久化问题。

## ⚠️ 生产化差距

- 本地 Cookie + CSRF 仅为演示身份，不是生产 SSO/OIDC/MFA。
- 本地邀请不发送邮件或钉钉消息。
- 钉钉、U9、ERP、DCS 未连接；外部模型未启用。
- SQL Server Express 单机部署不满足生产高可用、灾备和容量要求。
- Agent 仍使用 RuleSimulator，不是真实外部模型。

## 📦 最终发布回填

- 合并提交：`d53fba365b4c760b71e7212972eb3372649f2e81`
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-navigation-iam-final2.zip`
- 发布包 SHA256：`7B538AC01AE4671C83B42266B942339B9A74724797CD14DFB11C0C1E921A8B5B`
- manifest sourceRevision：`d53fba365b4c760b71e7212972eb3372649f2e81`，不含 `-dirty`
- 部署版本：`D:\Apps\AIDT-Test\versions\req-navigation-iam-final2`
- 部署备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-navigation-iam-final2_20261101_151644.bak`
- 部署备份 SHA256：`63F6B97C60214ADDEC8138677027EA05D1E4073E712A580EF7AD845BC5304887`
- 正式桌面/390px Playwright：17/17，0 失败、0 跳过、0 flaky
- 应用回滚：`final2 → final1 → final2` 健康检查通过
- 最终健康检查：页面、API live/ready、Worker、AiGateway、capabilities 全部 200
- 数据库恢复库：`AIDT_NAV_IAM_FINAL2_RESTORE_20261101_151859`
- 恢复结果：`ONLINE / 9 migrations / 57 tables / 项目经理三动作权限 3`
- 冻结标签：`req-navigation-iam-closeout-frozen-v1`
