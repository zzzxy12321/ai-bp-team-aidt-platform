param(
    [string]$BaseUrl = 'http://127.0.0.1:8088'
)

$ErrorActionPreference = 'Stop'
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$anonymousToken = (Invoke-RestMethod "$BaseUrl/api/v1/auth/csrf" -WebSession $session).data.requestToken
Invoke-RestMethod "$BaseUrl/api/v1/auth/demo-login" `
    -Method Post `
    -WebSession $session `
    -Headers @{ 'X-CSRF-TOKEN' = $anonymousToken } `
    -ContentType 'application/json' `
    -Body (@{ userName = 'demo.pm' } | ConvertTo-Json) | Out-Null
$csrf = (Invoke-RestMethod "$BaseUrl/api/v1/auth/csrf" -WebSession $session).data.requestToken

$configurationKey = 'CFG-ROLE-PILOT-20261101'
$existing = Invoke-RestMethod `
    "$BaseUrl/api/v1/configurations?configurationKey=$configurationKey&pageSize=100" `
    -WebSession $session
$configuration = @(
    $existing.data.items |
        Where-Object { $_.configurationKey -eq $configurationKey -and $_.state -ne 'Retired' }
) | Select-Object -First 1

if (-not $configuration) {
    $body = @{
        configurationKey = $configurationKey
        name = '研发测试三角色审批'
        description = '研发申请、项目经理审核、质量工程师判定。'
        departmentId = 'F01311D9-9E4A-4328-8DB8-616C8EF0C95F'
        objectTypeCode = 'PROJECT'
        startNode = 'TEST_REQUEST'
        nodes = @(
            @{
                code = 'TEST_REQUEST'
                name = '研发提交测试申请'
                assigneeUserName = 'demo.researcher'
                nextNode = 'MANAGER_REVIEW'
                completeOnApprove = $false
                condition = $null
            },
            @{
                code = 'MANAGER_REVIEW'
                name = '项目经理审核'
                assigneeUserName = 'demo.pm'
                nextNode = 'QA_JUDGEMENT'
                completeOnApprove = $false
                condition = $null
            },
            @{
                code = 'QA_JUDGEMENT'
                name = '质量工程师数据判定'
                assigneeUserName = 'demo.qa'
                nextNode = $null
                completeOnApprove = $true
                condition = $null
            }
        )
    } | ConvertTo-Json -Depth 8
    $configuration = (Invoke-RestMethod `
        "$BaseUrl/api/v1/configurations" `
        -Method Post `
        -WebSession $session `
        -Headers @{
            'X-CSRF-TOKEN' = $csrf
            'Idempotency-Key' = 'cfg-role-pilot-config-v1'
        } `
        -ContentType 'application/json' `
        -Body $body).data
}

$current = (Invoke-RestMethod `
    "$BaseUrl/api/v1/configurations/$($configuration.id)" `
    -WebSession $session).data
if ($current.state -eq 'Draft') {
    $validation = (Invoke-RestMethod `
        "$BaseUrl/api/v1/configurations/$($configuration.id)/actions/validate" `
        -Method Post `
        -WebSession $session `
        -Headers @{
            'X-CSRF-TOKEN' = $csrf
            'Idempotency-Key' = 'cfg-role-pilot-validate-v1'
        }).data
} else {
    $validation = $current.validation
}

$after = (Invoke-RestMethod `
    "$BaseUrl/api/v1/configurations/$($configuration.id)" `
    -WebSession $session).data
[pscustomobject]@{
    ConfigurationId = $after.id
    ConfigurationKey = $after.configurationKey
    VersionNumber = $after.versionNumber
    State = $after.state
    Revision = $after.revision
    NodeCount = @($after.nodes).Count
    IsValid = $validation.isValid
    Errors = @($validation.errors) -join '; '
} | ConvertTo-Json -Depth 5
