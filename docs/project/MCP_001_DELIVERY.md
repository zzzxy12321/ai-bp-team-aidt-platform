# AIDT 标准 MCP 服务交付报告

> 状态：`MCP_001_COMPLETE_DEPLOYED_AND_FROZEN`
>
> 正式发布：`req-mcp-final5`
>
> 冻结标签：`req-mcp-frozen-v1`

## 🎯 结论

AIDT 已具备可被 Codex 和其他标准 MCP 客户端使用的独立 MCP 服务。客户端可通过浏览器
OAuth 2.1/PKCE 授权连接，发现受控工具和 Resources，按当前用户权限与令牌部门范围
查询项目、事件、任务、流程、文件和审计，并执行明确授权的业务动作。

高风险审批、事件确认、流程转交、项目最终状态和敏感文件正文保留一次性真人确认。
每次工具调用和资源读取进入追加式 AIDT 审计。

本判定只表示 `MCP-001` 完成；正式生产 SSO、真实外部系统和生产高可用仍需甲方环境。

## ✅ 已实现

### 协议与传输

- 官方 `ModelContextProtocol.AspNetCore` / `ModelContextProtocol` `2.3.0`
- Streamable HTTP：`/mcp`
- stdio：`AIDT.Mcp.exe --stdio`
- 标准工具发现、Resources、调用、取消和错误响应由 SDK 处理

### 浏览器授权

- OAuth 2.1 授权码 + PKCE S256
- 动态客户端注册
- Access Token 1 小时
- Refresh Token 30 天并轮换
- `TokenFamilyId` 家族撤销
- 原始令牌、授权码和客户端密钥只保存 SHA-256 摘要

### 工具

- 当前身份、导航和审计
- 项目列表/详情/创建/成员/里程碑/状态
- 项目任务完成和来源追溯
- 事件列表/详情/创建/Agent 分析/真人确认
- 流程实例、待办、通过/退回/终止和转交
- 文件元数据、Base64 受控上传与下载
- Agent Profile 目录
- 共 26 个业务语义工具；没有 SQL、Shell、任意 HTTP 或文件系统原语

### Resources

- `aidt://projects/{id}`
- `aidt://events/{id}`
- `aidt://project-tasks/{id}`
- `aidt://workflow-instances/{id}`
- `aidt://files/{id}`
- `aidt://navigation/current`

### 机器身份

- 新增 `agent.mcp` MCP 网关审计身份
- 管理员可为既有业务服务身份签发 1—168 小时服务令牌
- 服务令牌只继承服务身份已有权限和所选部门
- 原始服务令牌只显示一次，可立即撤销

### 管理端

- `/mcp-center`：工具目录、客户端、令牌、服务身份令牌、确认和调用审计
- `/mcp-confirmations`：当前用户一次性高风险确认页
- `mcp:read` / `mcp:manage` 平台管理权限

## 🔐 安全边界

- 双门禁：MCP token scope + AIDT 用户/服务身份业务权限。
- 部门硬门禁：资源部门必须属于令牌选择部门；管理员身份不会自动扩大 MCP 范围。
- 跨环境、跨部门、跨项目、无权限、服务身份无业务权限和撤销令牌均明确拒绝。
- 不返回 Cookie、CSRF、原始令牌、连接串、文件物理路径或密钥。
- 高风险动作必须由当前真人确认同一请求摘要，确认一次性消费。
- 默认请求限流、请求大小限制和响应大小限制可配置。

## 🗄️ 数据库

迁移：`20261101122556_010_McpStandardServer`

- `Mcp.Client`
- `Mcp.AuthorizationRequest`
- `Mcp.AuthorizationCode`
- `Mcp.AccessToken`
- `Mcp.HumanConfirmation`

隔离库验证：

- up：57 表/9 迁移 → 62 表/10 迁移
- down：62/10 → 57/9，且移除 `Mcp` Schema
- re-up：57/9 → 62/10
- 主库已应用 10 条迁移、62 张应用表

## 🧪 验证

| 门禁 | 结果 |
|---|---|
| 后端/SQL/API | 116/116 |
| MCP 单元、OAuth、协议、stdio、权限与审计 | 11/11 |
| 前端类型检查 | 通过 |
| Vitest | 10 文件、14/14 |
| OpenAPI | 122 路径、142 operationId |
| 前端生产构建 | 通过 |
| NuGet 漏洞 | 无已知漏洞 |
| npm 生产依赖 | 0 漏洞 |
| 源码桌面 + 390px Playwright | 18/18，MCP 部署用例 1 个按设计跳过 |
| 正式部署桌面 + 390px + MCP OAuth | 19/19，0 失败、0 跳过、0 flaky |
| 部署后 stdio 服务身份 | initialize / tools/list / whoami 通过 |

正式 E2E 覆盖：

- 浏览器登录、OAuth 注册、PKCE 授权和 consent
- scoped `tools/list`
- `aidt_whoami`
- 高风险 `HUMAN_CONFIRMATION_REQUIRED`
- 真人确认页
- MCP 管理页
- MCP 审计可查
- 令牌撤销后下一次调用返回 401

## 🚀 发布

- 发布包：`D:\Apps\AIDT-Test\releases\AIDT-req-mcp-final5.zip`
- SHA256：`B949F1D218A7539F8BBA9FC53372B14A92382FD29ADA229D0C1E14F0F1F68FD5`
- 源码提交：`d0d816d38cc6a1ef8ccc27a3de3761c2a6c83594`
- manifest：不含 `-dirty`
- 部署时间：`2026-11-01T21:18:51.2379573+08:00`
- 部署备份：
  `C:\ProgramData\AIDT\Backups\AIDT_PRE_DEPLOY_req-mcp-final5_20261101_211843.bak`
- 备份 SHA256：
  `9C3B06B1D71EF98B1D0E706367C6569022E0E82E431B602B606D2CB30EC2EEF3`
- 最终版本：`req-mcp-final5`
- 页面、API live/ready、Worker、AiGateway、MCP、capabilities：均 HTTP 200

## ↩️ 回滚与恢复

- 应用回滚：`final5 → final4 → final5`，两次健康检查通过。
- 恢复库：`AIDT_MCP_FINAL5_RESTORE_20261101_211843`
- 恢复结果：ONLINE、10 条迁移、62 张应用表、5 张 `Mcp` 表、2 项 MCP 权限、
  `agent.mcp` 和 MCP 导航入口均存在。
- 无效/中间发布包：
  - `final3` 缺 restore 资产，不完整；
  - `final1` 端口发现地址不完整；
  - `final2` OAuth snake_case 注册绑定失败；
  - 以上包均保留但不使用、不打冻结标签。

## ⚠️ 保持边界

- 当前仍是本地 Demo 身份，不是生产 SSO/OIDC/MFA。
- 远程生产必须由 Nginx/反向代理终止 HTTPS。
- 外部 U9、ERP、DCS、钉钉和真实模型仍未连接。
- 数据库为 SQL Server 2025 Express 单机测试实例，不等于生产高可用。
- 构建时开发依赖存在既有 5 个 moderate npm audit 提示；生产依赖为 0。
