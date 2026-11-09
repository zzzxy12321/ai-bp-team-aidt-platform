[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://127.0.0.1:8088',
    [string]$TargetRoot = 'D:\Apps\AIDT-Test'
)

$ErrorActionPreference = 'Stop'
$session = [Microsoft.PowerShell.Commands.WebRequestSession]::new()

function Get-CsrfToken {
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1/auth/csrf" -WebSession $session
    return [string]$response.data.requestToken
}

$csrf = Get-CsrfToken
$login = Invoke-RestMethod `
    -Uri "$BaseUrl/api/v1/auth/demo-login" `
    -Method Post `
    -WebSession $session `
    -Headers @{ 'X-CSRF-TOKEN' = $csrf } `
    -ContentType 'application/json' `
    -Body (@{ userName = 'demo.admin' } | ConvertTo-Json)
if (-not $login.data.userId) {
    throw 'MCP stdio smoke: demo login failed.'
}

$departments = Invoke-RestMethod -Uri "$BaseUrl/api/v1/departments" -WebSession $session
$departmentId = [string]($departments.data | Where-Object code -eq 'RND' | Select-Object -First 1).id
if (-not $departmentId) {
    throw 'MCP stdio smoke: RND department missing.'
}

$users = Invoke-RestMethod `
    -Uri "$BaseUrl/api/v1/iam/users?pageSize=100&search=agent.project" `
    -WebSession $session
$serviceUserId = [string]($users.data.items | Where-Object userName -eq 'agent.project' | Select-Object -First 1).id
if (-not $serviceUserId) {
    throw 'MCP stdio smoke: agent.project service identity missing.'
}

$csrf = Get-CsrfToken
$serviceToken = Invoke-RestMethod `
    -Uri "$BaseUrl/api/v1/mcp/service-tokens" `
    -Method Post `
    -WebSession $session `
    -Headers @{ 'X-CSRF-TOKEN' = $csrf } `
    -ContentType 'application/json' `
    -Body (@{
        serviceUserId = $serviceUserId
        clientName = "AIDT stdio smoke $((Get-Date).ToString('HHmmss'))"
        scopes = @('aidt:read')
        departmentIds = @($departmentId)
        lifetimeHours = 2
    } | ConvertTo-Json -Depth 4)
$accessToken = [string]$serviceToken.data.accessToken
if (-not $accessToken) {
    throw 'MCP stdio smoke: service access token missing.'
}

$version = (Get-Content -LiteralPath (Join-Path $TargetRoot 'current-version.txt') -Raw).Trim()
$mcpRoot = Join-Path $TargetRoot "versions/$version/backend/mcp"
$mcpExe = Join-Path $mcpRoot 'AIDT.Mcp.exe'
$startInfo = [System.Diagnostics.ProcessStartInfo]::new($mcpExe)
$startInfo.ArgumentList.Add('--stdio')
$startInfo.WorkingDirectory = $mcpRoot
$startInfo.UseShellExecute = $false
$startInfo.RedirectStandardInput = $true
$startInfo.RedirectStandardOutput = $true
$startInfo.RedirectStandardError = $true
$startInfo.Environment['AIDT_MCP_TOKEN'] = $accessToken
$startInfo.Environment['ASPNETCORE_ENVIRONMENT'] = 'Development'

$process = [System.Diagnostics.Process]::Start($startInfo)
try {
    $requests = @(
        '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"stdio-smoke","version":"1.0"}}}',
        '{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}',
        '{"jsonrpc":"2.0","id":2,"method":"tools/list","params":{}}',
        '{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"aidt_whoami","arguments":{}}}'
    )
    foreach ($request in $requests) {
        $process.StandardInput.WriteLine($request)
    }
    $process.StandardInput.Flush()

    $lines = [System.Collections.Generic.List[string]]::new()
    $deadline = (Get-Date).AddSeconds(30)
    $read = $process.StandardOutput.ReadLineAsync()
    while ((Get-Date) -lt $deadline) {
        if (-not $read.Wait(1000)) {
            continue
        }
        if ($null -ne $read.Result) {
            $lines.Add($read.Result)
        }
        $joined = $lines -join "`n"
        if ($joined.Contains('"id":1') -and $joined.Contains('"id":2') -and $joined.Contains('"id":3')) {
            break
        }
        $read = $process.StandardOutput.ReadLineAsync()
    }

    $output = $lines -join "`n"
    if (-not $output.Contains('protocolVersion') `
        -or -not $output.Contains('aidt_whoami') `
        -or -not $output.Contains($serviceUserId)) {
        throw 'MCP stdio smoke: initialize, tools/list or whoami response invalid.'
    }
    [pscustomobject]@{
        Authentication = 'service_identity'
        ClientId = $serviceToken.data.clientId
        Version = $version
        Initialize = 'passed'
        ToolsList = 'passed'
        WhoAmI = 'passed'
    } | ConvertTo-Json
}
finally {
    if (-not $process.HasExited) {
        $process.Kill($true)
        $process.WaitForExit()
    }
}
