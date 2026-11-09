[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$PackagePath,
    [string]$TargetRoot = 'D:\Apps\AIDT-Test',
    [string]$NginxExe = 'C:\ProgramData\AIDT\Tools\nginx-1.28.0\nginx.exe',
    [int]$HttpPort = 8088,
    [int]$ApiPort = 5080,
    [int]$WorkerPort = 5081,
    [int]$AiGatewayPort = 5100,
    [int]$McpPort = 5085,
    [int]$GarnetPort = 6380,
    [string]$SqlServer = 'np:\\.\pipe\sql\query',
    [string]$Database = 'AIDT',
    [switch]$ReuseExistingVersion
)

$ErrorActionPreference = 'Stop'
$packagePath = (Resolve-Path -LiteralPath $PackagePath).Path
$packageHashFile = "$packagePath.sha256"
if (-not (Test-Path -LiteralPath $packageHashFile)) {
    throw "缺少发布包校验文件：$packageHashFile"
}

$expectedHash = (Get-Content -LiteralPath $packageHashFile -Raw).Split()[0]
$actualHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $packagePath).Hash
if ($actualHash -ne $expectedHash) {
    throw "发布包 SHA256 校验失败。"
}

$version = [System.IO.Path]::GetFileNameWithoutExtension($packagePath) -replace '^AIDT-', ''
$releaseRoot = Join-Path $TargetRoot "versions/$version"
if ((Test-Path -LiteralPath $releaseRoot) -and -not $ReuseExistingVersion) {
    throw "版本目录已存在，拒绝覆盖：$releaseRoot"
}

New-Item -ItemType Directory -Path (Split-Path $releaseRoot) -Force | Out-Null
if (-not (Test-Path -LiteralPath $releaseRoot)) {
    Expand-Archive -LiteralPath $packagePath -DestinationPath (Split-Path $releaseRoot) -Force
    $extracted = Join-Path (Split-Path $releaseRoot) "AIDT-$version"
    if ($extracted -ne $releaseRoot) {
        Move-Item -LiteralPath $extracted -Destination $releaseRoot
    }
}

$manifest = Get-Content -LiteralPath (Join-Path $releaseRoot 'manifest.json') -Raw | ConvertFrom-Json
$checksumPath = Join-Path $releaseRoot 'checksums.sha256'
foreach ($line in Get-Content -LiteralPath $checksumPath) {
    if ([string]::IsNullOrWhiteSpace($line)) { continue }
    $parts = $line -split '\s{2}', 2
    $filePath = Join-Path $releaseRoot $parts[1]
    $fileHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $filePath).Hash
    if ($fileHash -ne $parts[0]) {
        throw "发布文件校验失败：$($parts[1])"
    }
}

$backupDir = 'C:\ProgramData\AIDT\Backups'
New-Item -ItemType Directory -Path $backupDir -Force | Out-Null
$backupPath = Join-Path $backupDir "AIDT_PRE_DEPLOY_$version`_$(Get-Date -Format 'yyyyMMdd_HHmmss').bak"
$sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE'
& $sqlcmd -S $SqlServer -E -C -b -Q "BACKUP DATABASE [$Database] TO DISK=N'$backupPath' WITH COPY_ONLY, INIT, CHECKSUM; RESTORE VERIFYONLY FROM DISK=N'$backupPath' WITH CHECKSUM;"
if ($LASTEXITCODE -ne 0) {
    throw '数据库部署前备份或恢复校验失败。'
}

$redisListener = Get-NetTCPConnection -LocalPort $GarnetPort -State Listen -ErrorAction SilentlyContinue
if (-not $redisListener) {
    throw "Garnet $GarnetPort 未监听；请先启动既有 Garnet 依赖再部署。"
}
$workerEnvironment = @{
    ASPNETCORE_ENVIRONMENT = 'Development'
    AIDT__Cache__ConnectionString = "127.0.0.1:$GarnetPort,abortConnect=false,connectTimeout=1000,connectRetry=1"
}

$statePath = Join-Path $TargetRoot 'deployment-state.json'
$previousVersion = $null
if (Test-Path -LiteralPath $statePath) {
    $previousState = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    $previousVersion = $previousState.version
    foreach ($pidValue in $previousState.processes.PSObject.Properties.Value) {
        Stop-Process -Id ([int]$pidValue) -Force -ErrorAction SilentlyContinue
    }
    Get-Process nginx -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -eq $NginxExe } |
        Stop-Process -Force -ErrorAction SilentlyContinue
}

$nginxPrefix = Join-Path $releaseRoot 'nginx'
New-Item -ItemType Directory -Path (Join-Path $nginxPrefix 'conf') -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $nginxPrefix 'logs') -Force | Out-Null
foreach ($tempName in 'client_body_temp', 'proxy_temp', 'fastcgi_temp', 'uwsgi_temp', 'scgi_temp') {
    New-Item -ItemType Directory -Path (Join-Path $nginxPrefix "temp/$tempName") -Force | Out-Null
}
$frontendRoot = (Join-Path $releaseRoot 'frontend') -replace '\\', '/'
$config = Get-Content -LiteralPath (Join-Path $releaseRoot 'deploy/nginx.conf.template') -Raw
$config = $config.
    Replace('__AIDT_LISTEN__', "127.0.0.1:$HttpPort").
    Replace('__AIDT_FRONTEND_ROOT__', $frontendRoot).
    Replace('__AIDT_API_UPSTREAM__', "127.0.0.1:$ApiPort").
    Replace('__AIDT_MCP_UPSTREAM__', "127.0.0.1:$McpPort")
$nginxHome = Split-Path $NginxExe -Parent
$mimeTypes = Join-Path $nginxHome 'conf/mime.types'
if (-not (Test-Path -LiteralPath $mimeTypes)) {
    throw "Nginx mime.types 不存在：$mimeTypes"
}
Copy-Item -LiteralPath $mimeTypes -Destination (Join-Path $nginxPrefix 'conf/mime.types')
$config | Set-Content -LiteralPath (Join-Path $nginxPrefix 'conf/nginx.conf') -Encoding utf8NoBOM

$processes = [ordered]@{}
try {
    $processes.api = (Start-Process -FilePath (Join-Path $releaseRoot 'backend/api/AIDT.Api.exe') `
        -ArgumentList '--urls', "http://127.0.0.1:$ApiPort" `
        -WorkingDirectory (Join-Path $releaseRoot 'backend/api') -WindowStyle Hidden `
        -Environment @{ ASPNETCORE_ENVIRONMENT = 'Development' } -PassThru).Id
    $processes.worker = (Start-Process -FilePath (Join-Path $releaseRoot 'backend/worker/AIDT.Worker.exe') `
        -ArgumentList '--urls', "http://127.0.0.1:$WorkerPort" `
        -WorkingDirectory (Join-Path $releaseRoot 'backend/worker') -WindowStyle Hidden `
        -Environment $workerEnvironment -PassThru).Id
    $processes.aigateway = (Start-Process -FilePath (Join-Path $releaseRoot 'backend/aigateway/AIDT.AiGateway.exe') `
        -ArgumentList '--urls', "http://127.0.0.1:$AiGatewayPort" `
        -WorkingDirectory (Join-Path $releaseRoot 'backend/aigateway') -WindowStyle Hidden -PassThru).Id
    $processes.mcp = (Start-Process -FilePath (Join-Path $releaseRoot 'backend/mcp/AIDT.Mcp.exe') `
        -ArgumentList '--urls', "http://127.0.0.1:$McpPort" `
        -WorkingDirectory (Join-Path $releaseRoot 'backend/mcp') -WindowStyle Hidden `
        -Environment @{ ASPNETCORE_ENVIRONMENT = 'Development' } -PassThru).Id
    $processes.nginx = (Start-Process -FilePath $NginxExe `
        -ArgumentList '-p', $nginxPrefix, '-c', 'conf/nginx.conf' `
        -WorkingDirectory $nginxPrefix -WindowStyle Hidden -PassThru).Id

    $deadline = (Get-Date).AddSeconds(45)
    do {
        Start-Sleep -Milliseconds 500
        $healthy = $false
        try {
            $healthy = (Invoke-WebRequest -Uri "http://127.0.0.1:$HttpPort/health/live" `
                -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200
        }
        catch {
            $healthy = $false
        }
    } while (-not $healthy -and (Get-Date) -lt $deadline)
    if (-not $healthy) {
        throw '部署健康检查失败。'
    }

    $apiReady = (Invoke-WebRequest -Uri "http://127.0.0.1:$HttpPort/health/ready" `
        -UseBasicParsing -TimeoutSec 5).StatusCode
    $gatewayReady = (Invoke-WebRequest -Uri "http://127.0.0.1:$AiGatewayPort/health/ready" `
        -UseBasicParsing -TimeoutSec 5).StatusCode
    $workerReady = (Invoke-WebRequest -Uri "http://127.0.0.1:$WorkerPort/health/ready" `
        -UseBasicParsing -TimeoutSec 5).StatusCode
    $mcpLive = (Invoke-WebRequest -Uri "http://127.0.0.1:$McpPort/health/live" `
        -UseBasicParsing -TimeoutSec 5).StatusCode
    $mcpReady = (Invoke-WebRequest -Uri "http://127.0.0.1:$McpPort/health/ready" `
        -UseBasicParsing -TimeoutSec 5).StatusCode
    if ($apiReady -ne 200 -or $gatewayReady -ne 200 -or $workerReady -ne 200 `
        -or $mcpLive -ne 200 -or $mcpReady -ne 200) {
        throw '部署依赖就绪检查失败。'
    }
}
catch {
    foreach ($pidValue in $processes.Values) {
        Stop-Process -Id $pidValue -Force -ErrorAction SilentlyContinue
    }
    Get-Process nginx -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -eq $NginxExe } |
        Stop-Process -Force -ErrorAction SilentlyContinue
    if ($previousVersion) {
        & (Join-Path $PSScriptRoot 'rollback.ps1') `
            -TargetRoot $TargetRoot -ToVersion $previousVersion
    }
    throw
}

$state = [ordered]@{
    version = $version
    releaseRoot = $releaseRoot
    httpPort = $HttpPort
    apiPort = $ApiPort
    workerPort = $WorkerPort
    aiGatewayPort = $AiGatewayPort
    mcpPort = $McpPort
    garnetPort = $GarnetPort
    nginxExe = $NginxExe
    nginxPrefix = $nginxPrefix
    processes = $processes
    backupPath = $backupPath
    previousVersion = $previousVersion
    deployedAt = (Get-Date).ToString('o')
    manifest = $manifest
}
$state | ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath (Join-Path $TargetRoot 'deployment-state.json') -Encoding utf8NoBOM
$version | Set-Content -LiteralPath (Join-Path $TargetRoot 'current-version.txt') -Encoding utf8NoBOM
$historyPath = Join-Path $TargetRoot 'deployment-history.json'
$history = [System.Collections.Generic.List[object]]::new()
if (Test-Path -LiteralPath $historyPath) {
    $existingHistory = Get-Content -LiteralPath $historyPath -Raw | ConvertFrom-Json
    foreach ($item in @($existingHistory)) {
        $history.Add($item)
    }
}
$history.Add([pscustomobject]@{
    version = $version
    deployedAt = $state.deployedAt
    backupPath = $backupPath
})
$history | ConvertTo-Json -Depth 4 |
    Set-Content -LiteralPath $historyPath -Encoding utf8NoBOM

$state | ConvertTo-Json -Depth 6
