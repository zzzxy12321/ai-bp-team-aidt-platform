[CmdletBinding()]
param(
    [string]$OutputRoot = "C:\ProgramData\AIDT\Offline\$(Get-Date -Format 'yyyyMMdd-HHmmss')"
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$nugetCache = Join-Path $OutputRoot 'nuget-cache'
$nugetFeed = Join-Path $OutputRoot 'nuget-feed'
$npmCache = Join-Path $OutputRoot 'npm-cache'
$verificationPackages = Join-Path $OutputRoot 'verification-packages'

New-Item -ItemType Directory -Path $nugetCache, $nugetFeed, $npmCache -Force | Out-Null

dotnet restore (Join-Path $repoRoot 'AIDT.slnx') `
    --packages $nugetCache --force --no-cache
if ($LASTEXITCODE -ne 0) {
    throw '在线依赖导出 restores 失败。'
}

Get-ChildItem -LiteralPath $nugetCache -Recurse -File -Filter '*.nupkg' |
    ForEach-Object {
        $destination = Join-Path $nugetFeed $_.Name
        if (-not (Test-Path -LiteralPath $destination)) {
            Copy-Item -LiteralPath $_.FullName -Destination $destination
        }
    }

Push-Location (Join-Path $repoRoot 'frontend')
try {
    npm ci --cache $npmCache
    if ($LASTEXITCODE -ne 0) {
        throw 'npm 在线依赖导出失败。'
    }
}
finally {
    Pop-Location
}

New-Item -ItemType Directory -Path $verificationPackages -Force | Out-Null
dotnet restore (Join-Path $repoRoot 'AIDT.slnx') `
    --source $nugetFeed --packages $verificationPackages --force --no-cache
if ($LASTEXITCODE -ne 0) {
    throw 'NuGet 离线 restore 失败。'
}
dotnet build (Join-Path $repoRoot 'AIDT.slnx') --no-restore
if ($LASTEXITCODE -ne 0) {
    throw '.NET 离线 build 失败。'
}

Push-Location (Join-Path $repoRoot 'frontend')
try {
    npm ci --cache $npmCache --offline
    if ($LASTEXITCODE -ne 0) {
        throw 'npm 离线安装失败。'
    }
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw '前端离线 build 失败。'
    }
}
finally {
    Pop-Location
}

$manifest = [ordered]@{
    generatedAt = (Get-Date).ToString('o')
    outputRoot = $OutputRoot
    nugetPackages = (Get-ChildItem -LiteralPath $nugetFeed -File -Filter '*.nupkg').Count
    npmCacheBytes = (Get-ChildItem -LiteralPath $npmCache -Recurse -File |
        Measure-Object Length -Sum).Sum
    nugetOfflineRestore = 'passed'
    dotnetOfflineBuild = 'passed'
    npmOfflineInstall = 'passed'
    frontendBuild = 'passed'
}
$manifest | ConvertTo-Json -Depth 4 |
    Set-Content -LiteralPath (Join-Path $OutputRoot 'offline-manifest.json') -Encoding utf8NoBOM

$manifest | ConvertTo-Json -Depth 4
