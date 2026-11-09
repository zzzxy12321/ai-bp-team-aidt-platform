# AIDT 需求池批次 1 启动说明

## 📋 前置环境

- Windows 10/11 或 Windows Server 2019+
- .NET SDK `10.0.403`
- Node.js `24.18.0`
- npm `11.17.0`

批次 1 完成态连接本机 SQL Server：

- `AIDT:Database:IsReady=true`
- `AIDT:Database:MigrationState=applied_verified`
- `AIDT:Workflow:IsConfigured=true`
- `AIDT:Worker:EnableBackgroundProcessing=true`
- `ExternalSystems:Mode=Mock`
- `AllowWrites=false`

本机测试使用命名管道；生产必须替换为正式证书、SPN 和受控登录。

## 🖥️ 后端

```powershell
dotnet tool restore
dotnet restore AIDT.slnx
dotnet build AIDT.slnx --no-restore
dotnet run --project backend/AIDT.Api --launch-profile http
```

访问：

- `http://127.0.0.1:5080/health/live`：200
- `http://127.0.0.1:5080/health/ready`：应返回 `200 ready`
- `http://127.0.0.1:5080/api/v1/platform/capabilities`
- `http://127.0.0.1:5080/openapi/v1.json`

Worker：

```powershell
dotnet run --project backend/AIDT.Worker --launch-profile http
```

访问 `http://127.0.0.1:5081/health/live`。Worker 运行 Hangfire、Outbox
和 Garnet 接缝。

AiGateway：

```powershell
dotnet run --project backend/AIDT.AiGateway --launch-profile http
```

访问 `http://127.0.0.1:5100/health/ready`。该服务只执行规则模拟。

## 🌐 前端

```powershell
Set-Location frontend
npm ci
npm run dev
```

访问 `http://127.0.0.1:5173`。开发代理指向后端 `127.0.0.1:5080`。

## 🧪 验证命令

```powershell
dotnet build AIDT.slnx --no-restore
dotnet test AIDT.slnx --no-restore
dotnet format AIDT.slnx --verify-no-changes --no-restore
dotnet list AIDT.slnx package --vulnerable --include-transitive

Set-Location frontend
npm run typecheck
npm run test
npm run contract:check
npm run build
npm audit --omit=dev
```

## ⚠️ 期望状态

- API 存活正常。
- API `/health/ready` 返回 `200 ready`，并显示 `applied_verified`。
- Worker `/health/ready` 返回 `200`，后台执行开启。
- AiGateway 只执行规则模拟，不调用外部模型。
- 外部连接器明确显示 Mock 或 Disconnected。
- SQL 迁移、集成、浏览器持久化 E2E、部署回滚和恢复均有批次 1 证据。

任何把 `500`、无响应或虚假数据当作正常启动结果的行为都不符合阶段 A 预期。
