# 003 部门、文件与环境数据模型

> 状态：`applied_verified`
>
> 003/004 已通过隔离库和主库验证。

## 🏢 Identity

### Department

- 主键：`Id`
- 唯一键：`OrganizationId + Code`
- 关键字段：`Name`、`ManagerUserId`、`IsActive`
- 外键：组织、负责人

### UserDepartmentMembership

- 复合主键：`UserId + DepartmentId`
- 关键字段：`IsPrimary`、`IsManager`、`IsActive`、`JoinedAt`
- 每个用户最多一个启用主部门，由过滤唯一索引约束

## 🌍 数据环境

业务对象、流程实例、任务和附件统一使用：

- `EnvironmentCode`：`demo`、`test`、`prod`
- `IsTestData`：是否测试数据
- `SourceSystem`：来源系统

普通员工只读取当前部署环境；平台管理员可显式请求包含跨环境测试数据，
后端仍强制部门范围。

业务对象和流程实例保留业务归属部门；工作流任务使用处理人主部门作为责任部门，
从而同时支持跨部门流转与“我的待办”隔离。004 迁移负责校正历史任务数据。

## 📁 Document

### Attachment

- 元数据主表，拥有组织、部门、环境、状态、保密等级、扫描状态和 SHA256
- 当前版本号指向最新 `DocumentVersion`
- 删除采用申请状态，不静默删除正文

### DocumentVersion

- 每个版本保留原文件名、MIME、大小、存储路径、SHA256、扫描状态和保密等级
- 旧版本正文不被覆盖

### AttachmentLink

- 通用关联：`AttachmentId + EntityType + EntityId`
- 允许类型：`Department`、`BusinessObject`、`WorkflowInstance`、`WorkflowTask`
- 同一附件和同一目标只能有一条关联

## 🔐 文件安全

- 扩展名、MIME 和文件签名同时校验
- Office 文件校验 ZIP 内部 `word/`、`xl/`、`ppt/` 标记
- 物理路径由 GUID 生成，数据库不向前端返回
- 敏感和核心文件默认进入人工扫描复核
- PDF、图片和 UTF-8 文本支持内嵌预览；Office 仅下载

## ✅ 已验证

- 表创建、回填、外键、唯一约束和检查约束
- 003 up/down/re-up 与 004 up
- 数据库级部门和环境隔离
- 备份恢复和端到端文件闭环
