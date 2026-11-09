# AIDT 需求池批次 3 交付报告

> **历史交付说明：本报告记录 `req-batch3-final2` 的 Demo 基线，已被
> `req-batch3-final3` 收尾版本取代。**
>
> 后续收尾修复事件/项目状态持久化、PROJECT 文件自动证据和上海时区截止日期。
> 当前有效报告见
> [`REQUIREMENT_POOL_BATCH_3_CLOSEOUT_DELIVERY.md`](./REQUIREMENT_POOL_BATCH_3_CLOSEOUT_DELIVERY.md)。

> 状态：`REQUIREMENT_POOL_BATCH_3_DEMO_COMPLETE`
>
> 日期：2026-11-01
>
> 定位：最后一个业务 Demo 闭环批次，不代表整个 AIDT 生产项目完成

## 🎯 结论

项目经理岗位、项目主对象与生命周期、项目成员/里程碑、企业事件收件箱、Agent 建议、
真人确认和事件转项目任务已完成可运行纵向闭环，并通过 SQL/API、桌面、390px、迁移、
部署、回滚和恢复验证。

## 🆔 正式功能

| Feature ID | 状态 | 结果 |
|---|---|---|
| `PMO-001` | available | `PROJECT` 唯一主对象、项目档案、状态、部门/环境和生命周期 |
| `PMO-002` | available | 成员、主负责人、里程碑、逾期、任务、流程/SLA 和证据时间线 |
| `EVT-001` | available | 手动事项、TXT/MD/JSON/DOCX、听记、行动项、通知和附件来源 |
| `TASK-002` | available | Agent 建议、真人确认、幂等项目任务、进度回算和来源反查 |

## 👔 项目经理

- 本地身份：`demo.pm`
- 角色：`project-manager`
- 数据范围：Demo 组织 + 研发部门
- 普通项目成员可读取参与项目；不能管理成员或里程碑。
- 跨部门非项目成员直接 ID 读取返回 `PROJECT_SCOPE_DENIED` 并写审计。

## 🏗️ 项目主对象

- 公共标题、状态、创建人、部门、环境和版本保存在 `BusinessObject.BusinessObject`。
- 专属字段保存在 `PMO.ProjectProfile`，没有第二张“大而全”项目主表。
- 项目关系统一引用业务对象、流程、任务、听记、行动项、附件、事件和审计。

## 📥 事件到任务

```text
事件来源
 → EventInbox
 → Agent RuleSimulator 建议
 → 真人确认
 → 幂等 ProjectTask
 → 来源关系与审计
```

- 分析阶段不创建任务、不发起流程、不修改项目状态。
- 同事件最多一个项目任务，同幂等键同请求返回原结果。
- 听记任务可反查原文、发言人和时间戳。
- 高风险事项只生成待确认建议，不能自动发起或完成高风险流程。

## 🗄️ 迁移

- 最终隔离库：`AIDT_REQ_BATCH3_FINAL_20261101_010853`
- 序列：006 → 007 up → 007 down → re-up
- 应用表：44 → 51 → 44 → 51；EF 迁移：6 → 7
- 主库 `AIDT`：ONLINE，7 条迁移、51 张应用表
- 迁移前备份 SHA256：`B872EE36A940F338C91A3B15082F92E8D7905BA55C94C28C218F5EC0CB1C274E`

## 🧪 测试

- 后端、Domain、SQL/API：96/96，0 失败、0 跳过。
- 前端：类型检查、Vitest 9/9、OpenAPI 73/85、生产构建通过。
- 依赖：NuGet 无已知漏洞；`npm audit --omit=dev` 为 0。
- 正式部署 Playwright：11/11，0 failure、0 skipped、0 flaky。

## 🚀 发布

- 最终版本：`req-batch3-final2`
- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-batch3-final2.zip`
- SHA256：`C4875993E77D183EBA8EEE1F2D857AB35A11C815C2B00BE5DED85D5C9DB7E435`
- sourceRevision：`9b9b7fe10819e8201011909028204d533625f627`
- 标签：`req-batch3-final2`
- 上一稳定版本：`req-batch2-final3`
- 部署前备份：`C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-batch3-final2_20261101_013031.bak`
- 备份 SHA256：`721CDFD7F290A6E47A9AB8CDB617E93B40C5EC36AA106E255FCB1B32E1CB1B64`
- 恢复库：`AIDT_REQ_BATCH3_FINAL_RESTORE_20261101_013149`

### ⚠️ final1 说明

`req-batch3-final1` 包和部署目录已保留，但正式 E2E 发现管理员 Cookie 超限导致 502。
按“不覆盖已有发布包”规则，未覆盖或删除 final1；该包未打标签。
压缩 Cookie 声明后的 `req-batch3-final2` 是实际最终交付。

## ↩️ 回滚

- 应用回滚 `final2 → req-batch2-final3 → final2` 成功。
- 最终 root/live/ready/capabilities 均 200。
- 007 隔离库 down 和恢复库均通过。

## 🚫 未完成

- 真实钉钉/邮件/聊天/会议监听、音频转写、真实大模型。
- 正式 SSO/OIDC/MFA、企业目录和生产身份。
- 真实 U9/ERP/DCS/WMS/LIMS。
- 资源工时、成本预算、WBS 排程、关键路径和项目组合。
- 生产 DBA、高可用、灾备和正式上线。

## 🏁 项目判定

批次 3 业务 Demo 已完成，整个 AIDT 生产项目仍未完成。
