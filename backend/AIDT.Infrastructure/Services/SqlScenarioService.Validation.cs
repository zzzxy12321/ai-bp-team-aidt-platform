using System.Text.Json;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Scenarios;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed partial class SqlScenarioService
{
    private async Task EnsurePayloadReferencesReadableAsync(
        UserPrincipal user,
        Guid ownerDepartmentId,
        ScenarioDefinitionPayloadDto payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var errors = ValidatePayloadShape(payload);
        if (errors.Count > 0)
        {
            throw new ValidationException(string.Join("；", errors));
        }

        var departmentCodes = payload.ApplicableDepartmentCodes
            .Append((await dbContext.Departments.AsNoTracking()
                .Where(item => item.Id == ownerDepartmentId)
                .Select(item => item.Code)
                .SingleOrDefaultAsync(cancellationToken)) ?? string.Empty)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var validDepartmentCount = await dbContext.Departments.AsNoTracking()
            .CountAsync(item =>
                item.OrganizationId == user.OrganizationId
                && item.IsActive
                && departmentCodes.Contains(item.Code),
                cancellationToken);
        if (validDepartmentCount != departmentCodes.Length)
        {
            throw new ValidationException("场景包含不存在或已停用的适用部门。");
        }
    }

    private async Task<ScenarioValidationDto> ValidatePayloadAsync(
        UserPrincipal user,
        Guid ownerDepartmentId,
        ScenarioDefinitionPayloadDto payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        var errors = ValidatePayloadShape(payload);
        var warnings = new List<string>();
        var objectTypes = new List<ScenarioObjectTypeReferenceDto>();
        ScenarioWorkflowReferenceDto? workflow = null;
        var agents = new List<ScenarioAgentReferenceDto>();

        var ownerDepartment = await dbContext.Departments.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == ownerDepartmentId
                && item.OrganizationId == user.OrganizationId
                && item.IsActive,
            cancellationToken);
        if (ownerDepartment is null)
        {
            errors.Add("场景所属部门不存在或已停用。");
        }

        var departmentCodes = payload.ApplicableDepartmentCodes
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && item.IsActive
                && departmentCodes.Contains(item.Code))
            .ToListAsync(cancellationToken);
        foreach (var code in departmentCodes)
        {
            if (!departments.Any(item =>
                    string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"适用部门不存在或已停用：{code}。");
            }
        }

        foreach (var code in payload.ObjectTypeCodes
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var objectType = await dbContext.ObjectTypes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == code, cancellationToken);
            if (objectType is null)
            {
                errors.Add($"缺少已发布业务对象：{code}。");
                continue;
            }

            if (objectType.Status != "Published")
            {
                errors.Add($"业务对象类型已停用或未发布：{objectType.Name}（{code}）。");
                continue;
            }

            objectTypes.Add(new ScenarioObjectTypeReferenceDto(
                objectType.Id.ToString("D"),
                objectType.Code,
                objectType.Name,
                objectType.Version,
                objectType.Status));
        }

        var requiresWorkflow = payload.StartMode is
            ScenarioStartMode.CreateBusinessObjectAndWorkflow
            or ScenarioStartMode.ExistingBusinessObjectAndWorkflow;
        if (requiresWorkflow && string.IsNullOrWhiteSpace(payload.WorkflowDefinitionCode))
        {
            errors.Add("当前启动方式必须引用一个已发布流程版本。");
        }

        if (requiresWorkflow && !payload.WorkflowVersionNumber.HasValue)
        {
            errors.Add("场景未指定流程版本号。");
        }

        if (!string.IsNullOrWhiteSpace(payload.WorkflowDefinitionCode)
            && payload.WorkflowVersionNumber.HasValue)
        {
            var definition = await dbContext.WorkflowDefinitions.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.Code == payload.WorkflowDefinitionCode,
                    cancellationToken);
            if (definition is null)
            {
                errors.Add($"缺少已发布流程：{payload.WorkflowDefinitionCode}。");
            }
            else
            {
                var workflowVersion = await dbContext.WorkflowVersions.AsNoTracking()
                    .SingleOrDefaultAsync(item =>
                        item.WorkflowDefinitionId == definition.Id
                        && item.VersionNumber == payload.WorkflowVersionNumber.Value,
                        cancellationToken);
                if (workflowVersion is null)
                {
                    errors.Add(
                        $"流程版本不存在：{definition.Name} 第 {payload.WorkflowVersionNumber} 版。");
                }
                else if (workflowVersion.Status != "Published")
                {
                    errors.Add(
                        $"流程版本已停用：{definition.Name} 第 {workflowVersion.VersionNumber} 版。");
                }
                else
                {
                    var workflowObjectType = TryReadWorkflowObjectType(
                        workflowVersion.DefinitionJson);
                    if (!string.IsNullOrWhiteSpace(workflowObjectType)
                        && !payload.ObjectTypeCodes.Contains(
                            workflowObjectType,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        errors.Add(
                            $"流程引用的对象类型 {workflowObjectType} 不在场景对象范围内。");
                    }

                    workflow = new ScenarioWorkflowReferenceDto(
                        definition.Id.ToString("D"),
                        definition.Code,
                        definition.Name,
                        workflowVersion.Id.ToString("D"),
                        workflowVersion.VersionNumber,
                        workflowVersion.Status);
                }
            }
        }

        foreach (var usage in payload.AllowedAgents
                     .GroupBy(item => item.AgentProfileCode, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.First()))
        {
            var profile = await dbContext.AgentProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Code == usage.AgentProfileCode, cancellationToken);
            if (profile is null)
            {
                errors.Add($"数字员工不可用：{usage.AgentProfileCode}。");
                continue;
            }

            if (!profile.IsActive
                || profile.LifecycleStatus != "Published"
                || !profile.CurrentPublishedVersionId.HasValue)
            {
                errors.Add($"数字员工不可用：{profile.Name} 未发布或已停用。");
                continue;
            }

            var profileVersion = await dbContext.AgentProfileVersions.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.Id == profile.CurrentPublishedVersionId.Value,
                    cancellationToken);
            if (profileVersion is null || profileVersion.Status != "Published")
            {
                errors.Add($"数字员工不可用：{profile.Name} 的发布版本已失效。");
                continue;
            }

            var tools = JsonSerializer.Deserialize<List<string>>(
                profileVersion.AllowedToolsJson,
                ScenarioJsonOptions) ?? [];
            if (!tools.Contains(usage.Capability, StringComparer.Ordinal))
            {
                errors.Add(
                    $"数字员工不可用：{profile.Name} 未获得能力 {usage.Capability}。");
            }

            agents.Add(new ScenarioAgentReferenceDto(
                profile.Id.ToString("D"),
                profile.Code,
                profile.Name,
                profileVersion.VersionNumber,
                profile.LifecycleStatus,
                tools));
        }

        if (payload.AllowedAgents.Count == 0)
        {
            warnings.Add("当前场景未配置可选数字员工，所有判断均需真人完成。");
        }

        if (payload.PreparationItems.All(item => !item.IsRequired))
        {
            warnings.Add("当前场景没有必填准备材料，请确认是否遗漏。");
        }

        var snapshot = new ScenarioReferenceSnapshotDto(
            objectTypes,
            workflow,
            agents,
            clock.UtcNow);
        return new ScenarioValidationDto(
            errors.Count == 0,
            errors.Distinct(StringComparer.Ordinal).ToList(),
            warnings.Distinct(StringComparer.Ordinal).ToList(),
            errors.Count == 0 ? snapshot : null,
            clock.UtcNow);
    }

    private static List<string> ValidatePayloadShape(
        ScenarioDefinitionPayloadDto payload)
    {
        var errors = new List<string>();
        try
        {
            _ = DomainChecks.Required(payload.ProblemSolved, "场景解决的问题", 1000);
            _ = DomainChecks.Required(payload.TriggerDescription, "启动方式说明", 1000);
            if (payload.ApplicableDepartmentCodes.Count == 0)
            {
                errors.Add("场景至少需要声明一个适用部门。");
            }

            if (payload.ParticipantRoles.Count == 0)
            {
                errors.Add("场景至少需要声明一个参与岗位或业务角色。");
            }

            if (payload.Steps.Count == 0)
            {
                errors.Add("场景至少需要一个可执行业务步骤。");
            }

            var duplicateSteps = payload.Steps
                .GroupBy(item => item.Code, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicateSteps.Length > 0)
            {
                errors.Add($"场景步骤编码重复：{string.Join("、", duplicateSteps)}。");
            }

            foreach (var step in payload.Steps)
            {
                _ = DomainChecks.RequiredCode(step.Code, "步骤编码", 64);
                _ = DomainChecks.Required(step.Name, "步骤名称", 128);
                _ = DomainChecks.Required(step.BusinessDescription, "步骤业务说明", 1000);
                _ = DomainChecks.Required(step.ResponsibleRole, "步骤责任人", 128);
                _ = DomainChecks.Required(step.Action, "步骤动作", 500);
                _ = DomainChecks.Required(step.ExpectedResult, "预期结果", 1000);
                _ = DomainChecks.Required(step.Evidence, "结果证据", 1000);
            }

            if (payload.PreparationItems.Count == 0)
            {
                warningsAdd(errors, "场景未声明准备材料清单。");
            }

            if (payload.SupportedFileSources.Count == 0
                && payload.SupportedMinutesSources.Count == 0)
            {
                warningsAdd(errors, "场景未声明文件或 AI 听记来源。");
            }

            if (payload.PermissionRequirements.Count == 0)
            {
                errors.Add("场景必须声明权限和数据范围要求。");
            }

            if (payload.CompletionEvidence.Count == 0)
            {
                errors.Add("场景必须声明完成后形成的业务证据。");
            }

            ScenarioRules.EnsureNoAutomaticHighRiskAction(
                payload.PermanentHumanBoundaries);
            if (payload.StartMode == ScenarioStartMode.ProjectTask
                && !payload.ObjectTypeCodes.Contains(
                    "PROJECT",
                    StringComparer.OrdinalIgnoreCase))
            {
                errors.Add("项目任务场景必须引用 PROJECT 业务对象类型。");
            }
        }
        catch (ValidationException exception)
        {
            errors.Add(exception.Message);
        }

        return errors;
    }

    private async Task<List<string>> ValidateFrozenReferencesAsync(
        ScenarioReferenceSnapshotDto references,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        foreach (var reference in references.ObjectTypes)
        {
            var current = await dbContext.ObjectTypes.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == Guid.Parse(reference.Id), cancellationToken);
            if (current is null
                || current.Status != "Published"
                || current.Code != reference.Code
                || current.Version != reference.Version)
            {
                errors.Add($"场景引用的业务对象已变更或停用：{reference.Name}（{reference.Code}）。");
            }
        }

        if (references.Workflow is not null)
        {
            var workflow = await dbContext.WorkflowVersions.AsNoTracking()
                .SingleOrDefaultAsync(item =>
                    item.Id == Guid.Parse(references.Workflow.VersionId),
                    cancellationToken);
            if (workflow is null
                || workflow.Status != "Published"
                || workflow.VersionNumber != references.Workflow.VersionNumber)
            {
                errors.Add(
                    $"流程版本已停用：{references.Workflow.DefinitionName} 第 {references.Workflow.VersionNumber} 版。");
            }
        }

        foreach (var reference in references.Agents)
        {
            var current = await dbContext.AgentProfiles.AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == Guid.Parse(reference.Id), cancellationToken);
            var currentVersion = current?.CurrentPublishedVersionId is null
                ? null
                : await dbContext.AgentProfileVersions.AsNoTracking().SingleOrDefaultAsync(
                    item => item.Id == current.CurrentPublishedVersionId.Value,
                    cancellationToken);
            if (current is null
                || !current.IsActive
                || current.LifecycleStatus != "Published"
                || currentVersion is null
                || currentVersion.VersionNumber != reference.VersionNumber)
            {
                errors.Add($"数字员工不可用：{reference.Name}（{reference.Code}）。");
            }
        }

        return errors.Distinct(StringComparer.Ordinal).ToList();
    }

    private static void warningsAdd(List<string> errors, string message)
    {
        if (!errors.Contains(message, StringComparer.Ordinal))
        {
            errors.Add(message);
        }
    }

    private static string? TryReadWorkflowObjectType(string definitionJson)
    {
        try
        {
            using var document = JsonDocument.Parse(definitionJson);
            return document.RootElement.TryGetProperty("objectTypeCode", out var property)
                ? property.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<string> DeserializeIds(string json) =>
        JsonSerializer.Deserialize<List<string>>(json, ScenarioJsonOptions) ?? [];

    private static List<ScenarioBusinessLinkDto> DeserializeBusinessLinks(
        string json) =>
        JsonSerializer.Deserialize<List<ScenarioBusinessLinkDto>>(
            json,
            ScenarioJsonOptions) ?? [];
}
