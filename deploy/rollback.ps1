[CmdletBinding()]
param(
    [string]$TargetRoot = 'D:\Apps\AIDT-Test',
    [string]$ToVersion
)

$ErrorActionPreference = 'Stop'
$statePath = Join-Path $TargetRoot 'deployment-state.json'
if (-not (Test-Path -LiteralPath $statePath)) {
    throw '没有可回滚的部署状态。'
}

$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
$targetVersion = if ($ToVersion) { $ToVersion } else { $state.previousVersion }
if ([string]::IsNullOrWhiteSpace($targetVersion)) {
    throw '没有上一版本可回滚。'
}

$targetRootPath = Join-Path $TargetRoot "versions/$targetVersion"
if (-not (Test-Path -LiteralPath $targetRootPath)) {
    throw "目标版本目录不存在：$targetRootPath"
}

foreach ($pidValue in $state.processes.PSObject.Properties.Value) {
    Stop-Process -Id ([int]$pidValue) -Force -ErrorAction SilentlyContinue
}
Get-Process nginx -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $state.nginxExe } |
    Stop-Process -Force -ErrorAction SilentlyContinue

$nginxPrefix = Join-Path $targetRootPath 'nginx'
$frontendRoot = (Join-Path $targetRootPath 'frontend') -replace '\\', '/'
$config = Get-Content -LiteralPath (Join-Path $targetRootPath 'deploy/nginx.conf.template') -Raw
$config = $config.
    Replace('__AIDT_LISTEN__', "127.0.0.1:$($state.httpPort)").
    Replace('__AIDT_FRONTEND_ROOT__', $frontendRoot).
    Replace('__AIDT_API_UPSTREAM__', "127.0.0.1:$($state.apiPort)").
    Replace('__AIDT_MCP_UPSTREAM__', "127.0.0.1:$($state.mcpPort)")
$config | Set-Content -LiteralPath (Join-Path $nginxPrefix 'conf/nginx.conf') -Encoding utf8NoBOM

$garnetPort = if ($state.PSObject.Properties.Name -contains 'garnetPort') {
    [int]$state.garnetPort
} else {
    6380
}
$workerEnvironment = @{
    ASPNETCORE_ENVIRONMENT = 'Development'
    AIDT__Cache__ConnectionString = "127.0.0.1:$garnetPort,abortConnect=false,connectTimeout=1000,connectRetry=1"
}
$processes = [ordered]@{
    api = (Start-Process -FilePath (Join-Path $targetRootPath 'backend/api/AIDT.Api.exe') `
        -ArgumentList '--urls', "http://127.0.0.1:$($state.apiPort)" `
        -WorkingDirectory (Join-Path $targetRootPath 'backend/api') -WindowStyle Hidden `
        -Environment @{ ASPNETCORE_ENVIRONMENT = 'Development' } -PassThru).Id
    worker = (Start-Process -FilePath (Join-Path $targetRootPath 'backend/worker/AIDT.Worker.exe') `
        -ArgumentList '--urls', "http://127.0.0.1:$($state.workerPort)" `
        -WorkingDirectory (Join-Path $targetRootPath 'backend/worker') -WindowStyle Hidden `
        -Environment $workerEnvironment -PassThru).Id
    aigateway = (Start-Process -FilePath (Join-Path $targetRootPath 'backend/aigateway/AIDT.AiGateway.exe') `
        -ArgumentList '--urls', "http://127.0.0.1:$($state.aiGatewayPort)" `
        -WorkingDirectory (Join-Path $targetRootPath 'backend/aigateway') -WindowStyle Hidden -PassThru).Id
    nginx = (Start-Process -FilePath $state.nginxExe `
        -ArgumentList '-p', $nginxPrefix, '-c', 'conf/nginx.conf' `
        -WorkingDirectory $nginxPrefix -WindowStyle Hidden -PassThru).Id
}
$targetMcpExecutable = Join-Path $targetRootPath 'backend/mcp/AIDT.Mcp.exe'
if ($state.PSObject.Properties.Name -contains 'mcpPort' -and (Test-Path -LiteralPath $targetMcpExecutable)) {
    $processes.mcp = (Start-Process -FilePath $targetMcpExecutable `
        -ArgumentList '--urls', "http://127.0.0.1:$($state.mcpPort)" `
        -WorkingDirectory (Split-Path $targetMcpExecutable -Parent) -WindowStyle Hidden `
        -Environment @{ ASPNETCORE_ENVIRONMENT = 'Development' } -PassThru).Id
}

Start-Sleep -Seconds 3
$live = (Invoke-WebRequest -Uri "http://127.0.0.1:$($state.httpPort)/health/live" `
    -UseBasicParsing -TimeoutSec 5).StatusCode
$ready = (Invoke-WebRequest -Uri "http://127.0.0.1:$($state.httpPort)/health/ready" `
    -UseBasicParsing -TimeoutSec 5).StatusCode
$workerReady = (Invoke-WebRequest -Uri "http://127.0.0.1:$($state.workerPort)/health/ready" `
    -UseBasicParsing -TimeoutSec 5).StatusCode
$mcpHealthy = $true
if ($processes.Contains('mcp')) {
    $mcpLive = (Invoke-WebRequest -Uri "http://127.0.0.1:$($state.mcpPort)/health/live" `
        -UseBasicParsing -TimeoutSec 5).StatusCode
    $mcpReady = (Invoke-WebRequest -Uri "http://127.0.0.1:$($state.mcpPort)/health/ready" `
        -UseBasicParsing -TimeoutSec 5).StatusCode
    $mcpHealthy = $mcpLive -eq 200 -and $mcpReady -eq 200
}
if ($live -ne 200 -or $ready -ne 200 -or $workerReady -ne 200 -or -not $mcpHealthy) {
    throw '回滚健康检查失败。'
}

$state.version = $targetVersion
$state.releaseRoot = $targetRootPath
$state.nginxPrefix = $nginxPrefix
$state.processes = $processes
$state.manifest = Get-Content -LiteralPath (Join-Path $targetRootPath 'manifest.json') -Raw |
    ConvertFrom-Json
$state | Add-Member -NotePropertyName rolledBackAt -NotePropertyValue (Get-Date).ToString('o') -Force
$state | ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath $statePath -Encoding utf8NoBOM
$targetVersion | Set-Content -LiteralPath (Join-Path $TargetRoot 'current-version.txt') -Encoding utf8NoBOM
$state | ConvertTo-Json -Depth 6
