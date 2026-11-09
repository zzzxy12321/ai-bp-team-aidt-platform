# Git、提交与代码评审规范

## 🌿 分支模型

- `main`：随时可构建、可部署。
- `feature/<feature-id>-<short-name>`：新功能。
- `fix/<bug-id>-<short-name>`：Bug 修复。
- `refactor/<scope>-<short-name>`：纯重构。
- `docs/<scope>-<short-name>`：文档更新。

规则：

- 一个分支只解决一个目标。
- 分支存活时间尽量短。
- 合并后删除分支。
- 禁止直接推送 `main`。
- 禁止在功能分支夹带无关格式化和升级。

## 📝 提交信息

格式：

```text
type(scope): subject
```

允许类型：

- `feat`
- `fix`
- `refactor`
- `test`
- `docs`
- `chore`
- `build`
- `ci`

示例：

```text
feat(workflow): add version-pinned instance creation
fix(auth): reject task action without data scope
test(database): cover idempotency unique constraint
docs(feature-map): register CAPA workflow
```

要求：

- 使用祈使句。
- 说明“做了什么”，不写“update code”。
- 一次提交形成一个可理解变更。
- AI 生成的重要代码在提交正文标记 `AI-assisted`。

## 🧾 提交正文

必要时包括：

```text
Why:
- 业务原因或缺陷根因

What:
- 主要变更

Impact:
- API/数据库/前端/权限影响

Test:
- 执行命令和结果

Rollback:
- 回滚方式
```

## 🔀 Pull Request

PR 必须包含：

- 关联功能 ID 或 Bug ID
- 背景和目标
- 修改范围
- 明确未修改范围
- API 和数据库影响
- 权限和安全影响
- 测试证据
- 截图或响应示例
- 回滚方案
- 文档更新清单

PR 过大信号：

- 修改超过 8—10 个业务文件
- 同时包含功能、重构、依赖升级
- 同时修改三个以上模块且没有影响分析
- 无法用一段话说明变更

出现上述情况应拆分。

## 🔍 代码评审

### 架构

- 是否遵守分层和模块边界
- 是否直接访问其他模块数据
- 是否引入第二套技术栈
- 是否扩大共享状态

### 行为

- 是否满足验收标准
- 是否处理状态转换和边界条件
- 是否处理空值、重复、并发和超时

### 数据

- 是否有迁移
- 是否有约束和索引
- 是否有版本和审计
- 是否有回滚

### API

- 是否更新 OpenAPI
- 错误码是否稳定
- 是否向后兼容
- 前端类型是否同步

### 安全

- 是否校验身份、权限和数据范围
- 是否泄漏敏感数据
- 是否遗漏审计
- 是否引入 CSRF/XSS/注入风险

### 测试

- 是否覆盖失败路径
- 是否覆盖权限和并发
- 是否证明不会破坏邻近功能
- 是否保存证据

### 可维护性

- 命名是否清楚
- 函数是否过长
- 依赖是否合理
- 是否留下临时调试代码

## 🚦 合并门槛

- 至少一人评审
- CI 全绿
- 无未解决高风险评论
- 功能和数据库文档已更新
- 测试证据完整
- 无硬编码密钥
- 无临时文件
- 回滚方式明确

## 🧹 合并后

- 更新 CHANGELOG 或发布说明
- 更新功能地图
- 记录部署影响
- 删除功能分支
- 跟进未完成技术债

## 🚫 Git 反模式

- 一个提交混合多个功能
- 提交密码、密钥、数据文件
- 直接修改 `main`
- 删除失败测试
- 无意义提交信息
- 强制推送覆盖他人工作
- 用格式化制造巨大 diff
- 不写测试证据就合并
