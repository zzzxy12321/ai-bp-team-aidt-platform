[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Database,
    [string]$SqlServer = 'np:\\.\pipe\sql\query',
    [string]$Suffix = (Get-Date -Format 'yyyyMMddHHmmss'),
    [string]$ApiPort = '5180',
    [string]$WorkerPort = '5181',
    [string]$AiGatewayPort = '5200',
    [string]$McpPort = '5285',
    [string]$FrontendPort = '5173',
    [string]$EvidenceDir = ''
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$frontendRoot = Join-Path $repoRoot 'frontend'
$sqlcmd = 'C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\180\Tools\Binn\SQLCMD.EXE'

$env:ConnectionStrings__Aidt =
    "Server=$SqlServer;Database=$Database;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=5"
$env:AIDT_SQL_CONNECTION = $env:ConnectionStrings__Aidt
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:AIDT__Cache__ConnectionString =
    '127.0.0.1:6480,abortConnect=false,connectTimeout=1000,connectRetry=1'
$env:AIDT_E2E_API_PORT = $ApiPort
$env:AIDT_E2E_WORKER_PORT = $WorkerPort
$env:AIDT_E2E_AI_GATEWAY_PORT = $AiGatewayPort
$env:AIDT_E2E_MCP_PORT = $McpPort
$env:AIDT_E2E_FRONTEND_PORT = $FrontendPort
$env:VITE_API_PROXY_TARGET = "http://127.0.0.1:$ApiPort"
$env:AIDT_E2E_LOWRISK_SERVICE_NAME = "E2E 低风险数字员工服务账号 $Suffix"
$env:AIDT_E2E_HIGHRISK_SERVICE_NAME = "E2E 高风险数字员工服务账号 $Suffix"
if ($EvidenceDir) {
    $env:AIDT_EVIDENCE_DIR = $EvidenceDir
    New-Item -ItemType Directory -Path $EvidenceDir -Force | Out-Null
}

& $sqlcmd -S $SqlServer -d $Database -E -C -b -I -f 65001 `
    -v "E2eSuffix=$Suffix" `
    -i (Join-Path $PSScriptRoot 'Provision-E2eServiceIdentities.sql')
if ($LASTEXITCODE -ne 0) {
    throw 'E2E 服务身份夹具创建失败。'
}

Push-Location $frontendRoot
try {
    & npx playwright test --reporter=list
    $testExitCode = $LASTEXITCODE
}
finally {
    Pop-Location
    & $sqlcmd -S $SqlServer -d $Database -E -C -b -I -f 65001 `
        -v "E2eSuffix=$Suffix" `
        -i (Join-Path $PSScriptRoot 'Cleanup-E2eServiceIdentities.sql')
}

if ($testExitCode -ne 0) {
    exit $testExitCode
}
