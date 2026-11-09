# AIDT 测试

## 📌 当前测试

`AIDT.UnitTests` 当前覆盖：

- 业务对象版本、冲突和归档规则
- 流程版本固定、重复处理、退回原因和终止
- Agent 风险、限额、人工确认和服务身份自确认拒绝
- 创建对象和任务动作的成功、权限拒绝、重复冲突
- DTO camelCase、错误码和请求验证
- EF Core 35 张应用表模型
- API 存活/就绪、能力状态、OpenAPI、Cookie/CSRF、RBAC、组织范围和 Core 脱敏
- SQL 连接、约束、事务回滚、并发和幂等重放
- AiGateway 白名单与规则模拟
- Mock 连接器回执
- 组织/角色范围、自提权阻断和双人审核
- 导航默认模板、发布、停用、恢复和菜单/API 一致性
- 流程业务角色、任务快照、真实项目经理审批、转交和升级
- 六类处理人策略、部门候选收敛、legacy/scoped 角色统计、人员分页和 Agent 节点闭环
- 数字员工身份生命周期、不可变版本、回滚、部门边界、双人审核、停用撤令牌和 MCP 能力过滤

## 🛠️ 命令

```powershell
dotnet test AIDT.slnx --logger "console;verbosity=minimal"
```

## 🗄️ SQL 集成测试

SQL 集成测试通过 `AIDT_RUN_SQL_INTEGRATION_TESTS=1`、`ConnectionStrings__Aidt` 和 `AIDT_SQL_CONNECTION` 指向隔离库。治理测试会变更共享部门和角色状态，因此测试程序集显式关闭并行执行。

最近一次 AGT-003-B 源码门禁结果：

```text
后端/SQL/API 总数 172；通过 172；失败 0；跳过 0
MCP 总数 11；通过 11；失败 0；跳过 0
Vitest：20 个文件、41/41
源码 Playwright：44 通过 + 1 个部署专用用例跳过；失败 0；flaky 0
正式 Nginx Playwright：45/45；失败 0；跳过 0；flaky 0
```

## ✅ 已验证

- 迁移 up/down 已在隔离测试库验证
- 唯一键、检查约束、并发和幂等已验证
- 三条主流程和退回/终止已持久化验证
- 批次 3 收尾新增事件/项目刷新持久化、PROJECT 自动文件证据和上海时区精确测试
- 源码桌面 + 390px Playwright 18/18 通过
- 备份恢复、部署与回滚已验证
