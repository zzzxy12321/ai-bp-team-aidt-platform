# AIDT E2E 隔离夹具

`Run-FullE2e.ps1` 用于在已还原的隔离数据库上执行源码全量 Playwright：

```powershell
.\tests\e2e\Run-FullE2e.ps1 -Database AIDT_DEPT_WF_001_E2E_FINAL2_20261104_0100
```

- 脚本强制 `Development` 登录环境，并让 Vite 代理指向同一 API 端口。
- 每次生成唯一低风险/高风险服务身份，避免历史 AgentProfile 占用账号导致用例不可重复。
- 测试结束后停用临时服务身份和对应测试 AgentProfile，不删除历史、审计或证据。
- 测试库必须来自已知迁移基线；禁止指向主库运行破坏性 E2E。
