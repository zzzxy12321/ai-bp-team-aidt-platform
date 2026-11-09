[CmdletBinding()]
param([string]$TargetRoot = 'D:\Apps\AIDT-Test')

$statePath = Join-Path $TargetRoot 'deployment-state.json'
if (-not (Test-Path -LiteralPath $statePath)) {
    return
}

$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
foreach ($pidValue in $state.processes.PSObject.Properties.Value) {
    Stop-Process -Id ([int]$pidValue) -Force -ErrorAction SilentlyContinue
}
Get-Process nginx -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -eq $state.nginxExe } |
    Stop-Process -Force -ErrorAction SilentlyContinue
$state | Add-Member -NotePropertyName stoppedAt -NotePropertyValue (Get-Date).ToString('o') -Force
$state | ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath $statePath -Encoding utf8NoBOM
