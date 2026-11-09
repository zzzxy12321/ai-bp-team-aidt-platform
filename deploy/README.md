# AIDT 测试环境部署

## 📦 发布包

```powershell
.\deploy\build-release.ps1 -Version demo-a
.\deploy\build-release.ps1 -Version demo-b
```

输出：

- `AIDT-{version}.zip`
- `.zip.sha256`
- 包内 `manifest.json`
- 包内逐文件 `checksums.sha256`

## 🚀 部署

前置依赖 Garnet 默认在 `127.0.0.1:6380` 监听；部署脚本会检查 API、Worker、
AiGateway、MCP 和 Garnet 就绪状态。

本机已安装的 Garnet 入口：

```text
C:\Users\PSD-ZXY\.nuget\packages\garnet-server\2.2.5\tools\net10.0\any\win-x64\GarnetServer.exe
```

启动参数：

```powershell
GarnetServer.exe --port 6380 --bind 127.0.0.1 --memory 256m
```

若 Windows 动态端口排除区占用了 6380，可选择未排除端口并通过 `-GarnetPort`
显式部署；脚本会把同一端口注入 Worker 缓存连接串，其他配置保持不变：

```powershell
GarnetServer.exe --port 6480 --bind 127.0.0.1 --memory 256m
.\deploy\deploy.ps1 -PackagePath 'D:\Apps\AIDT-Test\releases\AIDT-req-ui-step5a-final1.zip' -GarnetPort 6480
```

```powershell
.\deploy\deploy.ps1 -PackagePath 'D:\Apps\AIDT-Test\releases\AIDT-req-batch1-final5.zip'
```

部署前会：

1. 校验 ZIP SHA256。
2. 校验包内每个文件。
3. 全量备份 `AIDT` 并执行 `RESTORE VERIFYONLY`。
4. 解压到独立版本目录。
5. 启动 API、Worker、AiGateway、MCP 和 Nginx。
6. 通过 Nginx `8088` 做健康检查。

API、Worker、AiGateway、MCP 只监听 `127.0.0.1`。测试环境由 Nginx
`127.0.0.1:8088` 提供唯一浏览器入口：

- AIDT 页面：`http://127.0.0.1:8088/`
- MCP Streamable HTTP：`http://127.0.0.1:8088/mcp`
- OAuth 授权：`http://127.0.0.1:8088/oauth/authorize`

远程客户端必须在 Nginx 前配置 HTTPS；当前本地测试入口不得作为生产安全边界。

## ↩️ 回滚

```powershell
.\deploy\rollback.ps1 -TargetRoot 'D:\Apps\AIDT-Test'
```

应用回滚使用已部署的上一版本目录。若新版本包含数据库迁移，回滚前必须按部署记录中的 `backupPath` 完成数据库恢复；Demo 演示脚本不自动覆盖数据库。

## 🧹 停止测试服务

```powershell
.\deploy\stop-test-deployment.ps1
```

## 🚫 边界

- 仅用于测试环境。
- 不部署真实生产。
- 不连接真实外部业务系统。
- 不将本地演示身份描述为生产 SSO。
