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

$projectCode = 'PRJ-CFG-PILOT-20261101-02'
$projects = Invoke-RestMethod "$BaseUrl/api/v1/projects?pageSize=100&includeTestData=true" -WebSession $session
$project = @($projects.data.items | Where-Object { $_.objectCode -eq $projectCode }) | Select-Object -First 1
if (-not $project) {
    $project = (Invoke-RestMethod `
        "$BaseUrl/api/v1/projects" `
        -Method Post `
        -WebSession $session `
        -Headers @{
            'X-CSRF-TOKEN' = $csrf
            'Idempotency-Key' = 'cfg-role-pilot-project-v2'
        } `
        -ContentType 'application/json' `
        -Body (@{
            objectCode = $projectCode
            name = '配置中心多岗位审批试点-经理兼岗'
            ownerUserId = '10000000-0000-0000-0000-000000000021'
            departmentId = 'F01311D9-9E4A-4328-8DB8-616C8EF0C95F'
            plannedStartDate = '2026-11-01T00:00:00+08:00'
            plannedEndDate = '2026-11-15T18:00:00+08:00'
            objective = '研发工程师发起、平台管理员兼经理复核、质量工程师判定，验证多岗位流程可以走通。'
            currentStage = '研发申请'
            confidentialityLevel = 'Internal'
            initialStatus = 'Draft'
        } | ConvertTo-Json)).data
}

$projectDetail = (Invoke-RestMethod `
    "$BaseUrl/api/v1/projects/$($project.id)?includeTestData=true" `
    -WebSession $session).data
foreach ($member in @(
    @{ UserId = '10000000-0000-0000-0000-000000000011'; ProjectRole = 'Contributor' },
    @{ UserId = '10000000-0000-0000-0000-000000000012'; ProjectRole = 'Contributor' }
)) {
    if (-not @($projectDetail.members | Where-Object { $_.userId -eq $member.UserId -and $_.isActive })) {
        Invoke-RestMethod `
            "$BaseUrl/api/v1/projects/$($project.id)/members" `
            -Method Post `
            -WebSession $session `
            -Headers @{ 'X-CSRF-TOKEN' = $csrf } `
            -ContentType 'application/json' `
            -Body ($member | ConvertTo-Json) | Out-Null
    }
}

$configurationKey = 'CFG-ROLE-PILOT-20261101'
$configurations = Invoke-RestMethod `
    "$BaseUrl/api/v1/configurations?configurationKey=$configurationKey&pageSize=100" `
    -WebSession $session
$configuration = @($configurations.data.items | Where-Object {
    $_.configurationKey -eq $configurationKey -and
    $_.state -eq 'Validated' -and
    $_.name -eq '研发测试三角色审批-经理兼岗'
}) | Select-Object -First 1
if (-not $configuration) {
    $configuration = (Invoke-RestMethod `
        "$BaseUrl/api/v1/configurations" `
        -Method Post `
        -WebSession $session `
        -Headers @{
            'X-CSRF-TOKEN' = $csrf
            'Idempotency-Key' = 'cfg-role-pilot-fallback-v1'
        } `
        -ContentType 'application/json' `
        -Body (@{
            configurationKey = $configurationKey
            name = '研发测试三角色审批-经理兼岗'
            description = '研发申请、平台管理员兼经理复核、质量工程师判定。'
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
                    name = '经理复核（平台管理员兼岗）'
                    assigneeUserName = 'demo.admin'
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
        } | ConvertTo-Json -Depth 8)).data
    $validation = (Invoke-RestMethod `
        "$BaseUrl/api/v1/configurations/$($configuration.id)/actions/validate" `
        -Method Post `
        -WebSession $session `
        -Headers @{
            'X-CSRF-TOKEN' = $csrf
            'Idempotency-Key' = 'cfg-role-pilot-fallback-validate-v1'
        }).data
}

$projectDetail = (Invoke-RestMethod `
    "$BaseUrl/api/v1/projects/$($project.id)?includeTestData=true" `
    -WebSession $session).data
$configurationDetail = (Invoke-RestMethod `
    "$BaseUrl/api/v1/configurations/$($configuration.id)" `
    -WebSession $session).data
[pscustomobject]@{
    ProjectId = $project.id
    ProjectCode = $project.objectCode
    ProjectState = $projectDetail.project.status
    ConfigurationId = $configurationDetail.id
    ConfigurationKey = $configurationDetail.configurationKey
    ConfigurationVersion = $configurationDetail.versionNumber
    ConfigurationState = $configurationDetail.state
    NodeCount = @($configurationDetail.nodes).Count
} | ConvertTo-Json
