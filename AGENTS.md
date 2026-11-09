# AIDT AI 开发总入口

> 本文件是所有 AI 开发窗口的最短入口。完整规则位于 `standards/`。  
> `CLAUDE.md` 只做入口转发，不得维护与本文冲突的规则。

## 🎯 核心目标

> **我的代码必须适合直接作为甲方便于复制、部署、接手和长期维护的正式代码仓库结构。**

Demo 是正式仓库的可运行纵向切片，不是一次性演示目录。

## 🔒 技术栈

`.NET 10 + SQL Server 2025 + Vue 3 + Element Plus + Elsa Workflows + Hangfire + Garnet + Nginx + AiGateway + 离线部署包`

禁止擅自引入：

- Java、Spring Boot、Flowable
- PostgreSQL、MySQL、MongoDB
- React、Angular、Next.js、Nuxt
- Docker、Kubernetes、RabbitMQ、Kafka
- 第二套认证、日志、ORM 或前端组件库

任何偏离必须写 ADR 并经确认。

## 📖 每次任务开始

按顺序读取：

1. `docs/001-产品PRD/00-项目目标.md`
2. `docs/001-产品PRD/01-需求基线.md`
3. `docs/001-产品PRD/02-技术栈与架构.md`
4. `docs/001-产品PRD/03-计划方案.md`
5. `standards/00-AI工程宪法.md`
6. `standards/01-上下文阅读与恢复.md`
7. `task_plan.md`
8. `findings.md`
9. `progress.md`
10. `docs/project/FEATURE_MAP.md`
11. 当前功能对应的模块 README 和规范

不要默认读取整个仓库。

## 🧭 强制工作流

1. 识别任务类型。
2. 查功能地图。
3. 建立最小上下文包。
4. 做变更影响分析。
5. 写实施计划。
6. 编码。
7. 执行测试。
8. 做架构、行为、交付三次自审。
9. 更新功能地图和契约文档。
10. 更新 `task_plan.md`、`findings.md`、`progress.md`。

没有影响分析和计划，不得开始大规模修改。

## 🧱 仓库结构

```text
backend/
frontend/
database/
tests/
deploy/
docs/
standards/
templates/
prompts/
```

禁止：

- 继续使用 `003.前端代码` 混放前后端。
- 在根目录随意新建业务源码目录。
- 把 `TMP.可删除` 提交进仓库。
- 用本地临时文件维持功能。

## 🔐 硬边界

- 业务对象是唯一主对象，流程只引用对象。
- 不在流程中复制业务字段。
- 不直接写其他模块的表。
- 不直接写 U9、ERP、DCS 数据库。
- 数字员工不得控制 DCS/PLC、直接付款、质量放行或修改批准配方。
- 所有高风险动作人工确认并审计。
- 密钥、令牌和生产数据不得进入代码、日志或公共模型。
- Mock、未连接和真实连接必须明确区分。

## 🧪 完成条件

最低要求：

- 验收标准满足
- 前后端和数据契约一致
- 相关测试通过
- 权限和审计已验证
- 数据库迁移可恢复
- 文档和功能地图已更新
- 没有未说明的风险

详见 `standards/14-完成定义与交付清单.md`。

## 🚨 三次失败

同一错误最多进行三次不同尝试。第三次仍失败后：

1. 保存错误和尝试记录。
2. 停止重复修改。
3. 说明阻塞点并请求用户指导。

## 📤 每次任务结束

输出：

- 结果
- 修改文件
- 测试命令与结果
- 未完成事项
- 风险
- 下一步

同步更新：

- `task_plan.md`
- `findings.md`
- `progress.md`
- `docs/project/FEATURE_MAP.md`

## 📚 规范入口

- [开发规范总索引](./standards/README.md)
- [AI 工程宪法](./standards/00-AI工程宪法.md)
- [上下文阅读与恢复](./standards/01-上下文阅读与恢复.md)
- [Vibecoding 标准工作流](./standards/13-Vibecoding工作流.md)
- [完成定义](./standards/14-完成定义与交付清单.md)
