[CmdletBinding()]
param(
    [string]$Version = (Get-Date -Format 'yyyyMMdd-HHmmss'),
    [string]$OutputRoot = 'D:\Apps\AIDT-Test\releases',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$stagingRoot = Join-Path ([System.IO.Path]::GetTempPath()) "aidt-release-$Version-$([Guid]::NewGuid().ToString('N'))"
$packageRoot = Join-Path $stagingRoot "AIDT-$Version"
$archivePath = Join-Path $OutputRoot "AIDT-$Version.zip"

if (Test-Path -LiteralPath $archivePath) {
    throw "发布包已存在，拒绝覆盖：$archivePath"
}

New-Item -ItemType Directory -Path $packageRoot -Force | Out-Null
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null

dotnet publish (Join-Path $repoRoot 'backend/AIDT.Api/AIDT.Api.csproj') `
    -c $Configuration --no-restore -o (Join-Path $packageRoot 'backend/api')
if ($LASTEXITCODE -ne 0) {
    throw "API 发布失败，退出码 $LASTEXITCODE。"
}
dotnet publish (Join-Path $repoRoot 'backend/AIDT.Worker/AIDT.Worker.csproj') `
    -c $Configuration --no-restore -o (Join-Path $packageRoot 'backend/worker')
if ($LASTEXITCODE -ne 0) {
    throw "Worker 发布失败，退出码 $LASTEXITCODE。"
}
dotnet publish (Join-Path $repoRoot 'backend/AIDT.AiGateway/AIDT.AiGateway.csproj') `
    -c $Configuration --no-restore -o (Join-Path $packageRoot 'backend/aigateway')
if ($LASTEXITCODE -ne 0) {
    throw "AiGateway 发布失败，退出码 $LASTEXITCODE。"
}
dotnet publish (Join-Path $repoRoot 'backend/AIDT.Mcp/AIDT.Mcp.csproj') `
    -c $Configuration --no-restore -o (Join-Path $packageRoot 'backend/mcp')
if ($LASTEXITCODE -ne 0) {
    throw "MCP 发布失败，退出码 $LASTEXITCODE。"
}

Push-Location (Join-Path $repoRoot 'frontend')
try {
    npm ci
    if ($LASTEXITCODE -ne 0) {
        throw "npm ci 失败，退出码 $LASTEXITCODE。"
    }
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "前端生产构建失败，退出码 $LASTEXITCODE。"
    }
}
finally {
    Pop-Location
}

Copy-Item -LiteralPath (Join-Path $repoRoot 'frontend/dist') `
    -Destination (Join-Path $packageRoot 'frontend') -Recurse
Copy-Item -LiteralPath (Join-Path $repoRoot 'database') `
    -Destination (Join-Path $packageRoot 'database') -Recurse
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs') `
    -Destination (Join-Path $packageRoot 'docs') -Recurse
New-Item -ItemType Directory -Path (Join-Path $packageRoot 'deploy') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'deploy/nginx/nginx.conf.template') `
    -Destination (Join-Path $packageRoot 'deploy/nginx.conf.template')
Copy-Item -LiteralPath (Join-Path $repoRoot 'deploy/deploy.ps1') `
    -Destination (Join-Path $packageRoot 'deploy/deploy.ps1')
Copy-Item -LiteralPath (Join-Path $repoRoot 'deploy/rollback.ps1') `
    -Destination (Join-Path $packageRoot 'deploy/rollback.ps1')

$apiOpenApiDir = Join-Path $packageRoot 'backend/api/docs/contracts/openapi'
New-Item -ItemType Directory -Path $apiOpenApiDir -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $repoRoot 'docs/contracts/openapi/v1.json') `
    -Destination (Join-Path $apiOpenApiDir 'v1.json')

$dotnetVersion = (& dotnet --version).Trim()
$nodeVersion = (& node --version).Trim()
$sourceRevision = (& git -C $repoRoot rev-parse HEAD).Trim()
$workingTreeChanges = @(& git -C $repoRoot status --porcelain)
$sourceRevisionWithState = if ($workingTreeChanges.Count -eq 0) {
    $sourceRevision
} else {
    "$sourceRevision-dirty"
}
$migrationState = Get-Content -LiteralPath (Join-Path $repoRoot 'database/migrations/migration-state.json') -Raw |
    ConvertFrom-Json
$manifest = [ordered]@{
    application = 'AIDT'
    version = $Version
    generatedAt = (Get-Date).ToString('o')
    sourceRevision = $sourceRevisionWithState
    dotnetSdk = $dotnetVersion
    node = $nodeVersion
    migrationState = $migrationState.state
    migrations = @($migrationState.migrations | ForEach-Object { $_.migration })
    externalSystems = 'Mock/Disconnected only'
    aiGatewayMode = 'RuleSimulator'
    mcp = [ordered]@{
        sdk = 'ModelContextProtocol.AspNetCore 2.3.0'
        transports = @('StreamableHttp', 'stdio')
        authorization = 'OAuth 2.1 authorization code + PKCE S256'
    }
}
$manifest | ConvertTo-Json -Depth 5 |
    Set-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Encoding utf8NoBOM

@"
# AIDT $Version

- 生成时间：$((Get-Date).ToString('o'))
- SQL Server：迁移状态 $($migrationState.state)
- 外部系统：仅 Mock/Disconnected
- AI：AiGateway RuleSimulator，不调用外部模型
- MCP：官方 .NET SDK 2.3.0；Streamable HTTP + stdio；OAuth 2.1/PKCE

部署和回滚仅限测试环境。回滚前先核对数据库恢复点。
"@ | Set-Content -LiteralPath (Join-Path $packageRoot 'release-notes.md') -Encoding utf8NoBOM

$checksumPath = Join-Path $packageRoot 'checksums.sha256'
$checksumLines = Get-ChildItem -LiteralPath $packageRoot -Recurse -File |
    Where-Object { $_.FullName -ne $checksumPath } |
    Sort-Object FullName |
    ForEach-Object {
        $relative = [System.IO.Path]::GetRelativePath($packageRoot, $_.FullName) -replace '\\', '/'
        $hash = (Get-FileHash -Algorithm SHA256 -LiteralPath $_.FullName).Hash
        "$hash  $relative"
    }
$checksumLines | Set-Content -LiteralPath $checksumPath -Encoding utf8NoBOM

Compress-Archive -LiteralPath $packageRoot -DestinationPath $archivePath -CompressionLevel Optimal
$archiveHash = (Get-FileHash -Algorithm SHA256 -LiteralPath $archivePath).Hash
"$archiveHash  $([System.IO.Path]::GetFileName($archivePath))" |
    Set-Content -LiteralPath "$archivePath.sha256" -Encoding utf8NoBOM

[pscustomobject]@{
    Version = $Version
    Package = $archivePath
    PackageSha256 = $archiveHash
    Manifest = (Join-Path $packageRoot 'manifest.json')
} | ConvertTo-Json
