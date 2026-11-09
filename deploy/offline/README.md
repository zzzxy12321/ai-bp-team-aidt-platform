# AIDT 离线依赖导出与验证

```powershell
.\deploy\offline\export-and-verify.ps1
```

脚本执行：

1. 将当前解决方案依赖还原到隔离 NuGet 缓存。
2. 汇总 `.nupkg` 为可离线使用的本地源。
3. 使用 npm cache 安装前端依赖。
4. 仅使用本地 NuGet 源重新 restore/build。
5. 使用 `npm ci --offline` 和 Vite 构建验证前端。
6. 输出 `offline-manifest.json`。

离线目录位于 `C:\ProgramData\AIDT\Offline\{timestamp}`，不进入源代码仓库。

## 🔒 边界

- 不包含 SQL Server、Windows 或 Edge 安装介质本体。
- 不包含真实模型文件。
- 全量介质离线交付仍需在最终甲方网络环境按部署手册补齐。
