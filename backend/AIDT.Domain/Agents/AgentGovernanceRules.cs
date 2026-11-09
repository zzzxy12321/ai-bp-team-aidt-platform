using System.Text.RegularExpressions;
using AIDT.Domain.Common;

namespace AIDT.Domain.Agents;

public enum AgentProfileLifecycleStatus
{
    Draft,
    Published,
    Disabled,
    Retired
}

public enum AgentProfileVersionStatus
{
    Draft,
    Validated,
    Published,
    Retired
}

public sealed record AgentProfileSnapshot(
    string Code,
    string Name,
    string Description,
    Guid ServiceUserId,
    Guid DepartmentId,
    Guid BusinessOwnerUserId,
    Guid TechnicalOwnerUserId,
    AgentAutonomyLevel AutonomyLevel,
    AgentRiskLevel RiskLimit,
    decimal AmountLimit,
    IReadOnlyList<string> DataScopes,
    IReadOnlyList<string> ModelScopes,
    IReadOnlyList<string> AllowedTools,
    IReadOnlyList<string> RequiredConfirmations,
    IReadOnlyList<string> StopConditions);

public sealed record AgentCapabilityDefinition(
    string Code,
    string Name,
    IReadOnlyList<string> RequiredPermissions,
    string? McpScope = null,
    AgentRiskLevel RiskLevel = AgentRiskLevel.Low,
    bool RequiresConfirmation = false,
    string Description = "");

public static class AgentCapabilityRegistry
{
    public static IReadOnlyList<string> PermanentHumanBoundaries { get; } =
    [
        "payment",
        "quality-release",
        "formula-change",
        "equipment-control",
        "safety-interlock",
        "contract-commitment",
        "customer-compensation"
    ];

    public static IReadOnlyList<string> AllowedConfirmationRules { get; } =
    [
        .. PermanentHumanBoundaries,
        "low-confidence",
        "external-impact",
        "data-sensitivity"
    ];

    public static IReadOnlyList<string> AllowedStopConditions { get; } =
    [
        "manual-stop",
        "risk-limit",
        "amount-limit",
        "unauthorized-tool",
        "validation-failure",
        "model-error",
        "data-scope-violation"
    ];

    public static IReadOnlyList<string> AllowedModelScopes { get; } = ["aidt-rule-sim"];

    public static IReadOnlyDictionary<string, AgentCapabilityDefinition> Capabilities { get; } =
        new Dictionary<string, AgentCapabilityDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["workflow.read"] = new(
                "workflow.read",
                "读取流程与待办",
                ["workflow-instance:read", "workflow-task:read"],
                "aidt:read",
                Description: "读取流程实例、当前节点和待办摘要。"),
            ["workflow.assist"] = new(
                "workflow.assist",
                "生成流程辅助意见",
                ["workflow-task:read"],
                "aidt:write",
                RequiresConfirmation: true,
                Description: "生成建议或草稿；提交前由真人确认。"),
            ["workflow.proxy-low-risk"] = new(
                "workflow.proxy-low-risk",
                "低风险代理流程动作",
                ["workflow-task:read", "workflow-task:update", "workflow-task:approve"],
                "aidt:approve",
                AgentRiskLevel.Medium,
                RequiresConfirmation: true,
                Description: "仅代理低风险、可逆流程动作，高风险节点禁止。"),
            ["event.inbox.review"] = new(
                "event.inbox.review",
                "审阅事件收件箱",
                ["event-inbox:read"],
                "aidt:read",
                Description: "读取事件原文并生成待真人确认的业务建议。"),
            ["project.task.draft"] = new(
                "project.task.draft",
                "生成项目任务草稿",
                ["project-task:read"],
                "aidt:write",
                RequiresConfirmation: true,
                Description: "提取项目任务草稿；不会自动派发。"),
            ["project.read"] = new(
                "project.read",
                "读取项目",
                ["project:read"],
                "aidt:read",
                Description: "读取授权项目、成员、里程碑和任务摘要。"),
            ["project.write"] = new(
                "project.write",
                "维护项目及项目任务",
                ["project:update", "project-task:create", "project-task:update"],
                "aidt:write",
                AgentRiskLevel.Medium,
                RequiresConfirmation: true,
                Description: "创建或变更项目任务草稿，最终状态仍由真人确认。"),
            ["audit.read"] = new(
                "audit.read",
                "读取审计证据",
                ["audit:read"],
                "aidt:read",
                Description: "读取数字员工相关审计证据，不返回敏感原文。"),
            ["file.metadata.read"] = new(
                "file.metadata.read",
                "读取文件元数据",
                ["file:read"],
                "aidt:files",
                Description: "读取文件名、摘要和扫描状态，不返回物理路径。"),
            ["file.upload"] = new(
                "file.upload",
                "上传受控文件",
                ["file:create"],
                "aidt:files",
                AgentRiskLevel.Medium,
                RequiresConfirmation: true,
                Description: "上传前确认文件范围和保密等级。"),
            ["identity.read"] = new(
                "identity.read",
                "读取当前身份",
                [],
                "aidt:read",
                Description: "读取服务身份、组织和授权范围摘要。")
        };

    public static AgentCapabilityDefinition? Find(string code) =>
        Capabilities.GetValueOrDefault(code.Trim());

    public static bool IsMcpScopeSupported(string scope, IEnumerable<string> allowedCapabilities) =>
        allowedCapabilities
            .Select(Find)
            .Where(item => item?.McpScope is not null)
            .Any(item => string.Equals(item!.McpScope, scope, StringComparison.Ordinal));
}

public static partial class AgentGovernanceRules
{
    public static AgentProfileSnapshot Normalize(AgentProfileSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (snapshot.AmountLimit < 0)
        {
            throw new ValidationException("数字员工金额上限不能为负数。");
        }

        var code = DomainChecks.RequiredCode(
            snapshot.Code?.Trim().ToUpperInvariant() ?? string.Empty,
            "数字员工编码",
            64);
        var name = DomainChecks.Required(snapshot.Name, "数字员工名称", 128);
        var description = NormalizeOptional(snapshot.Description, "业务说明", 2000);
        DomainChecks.RequiredId(snapshot.ServiceUserId, "独立服务用户");
        DomainChecks.RequiredId(snapshot.DepartmentId, "所属部门");
        DomainChecks.RequiredId(snapshot.BusinessOwnerUserId, "业务负责人");
        DomainChecks.RequiredId(snapshot.TechnicalOwnerUserId, "技术负责人");

        var tools = NormalizeValues(snapshot.AllowedTools, "工具能力", 128);
        var unknownTools = tools
            .Where(item => AgentCapabilityRegistry.Find(item) is null)
            .ToArray();
        if (unknownTools.Length > 0)
        {
            throw new ValidationException(
                "数字员工包含未注册工具能力：" + string.Join("、", unknownTools));
        }

        var models = NormalizeValues(snapshot.ModelScopes, "模型范围", 128);
        var unsupportedModels = models
            .Except(AgentCapabilityRegistry.AllowedModelScopes, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unsupportedModels.Length > 0)
        {
            throw new ValidationException(
                "数字员工包含未启用模型：" + string.Join("、", unsupportedModels));
        }

        var dataScopes = NormalizeValues(snapshot.DataScopes, "数据范围", 128);
        if (dataScopes.Count == 0
            || dataScopes.Any(item => !DataScopePattern().IsMatch(item)))
        {
            throw new ValidationException(
                "数字员工数据范围必须使用 organization:<id> 或 department:<id> 格式。");
        }

        if (!dataScopes.Contains(
                $"department:{snapshot.DepartmentId:D}",
                StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationException("数字员工数据范围必须包含所属部门。");
        }

        var confirmations = NormalizeValues(
            snapshot.RequiredConfirmations,
            "人工确认规则",
            128);
        var unsupportedConfirmations = confirmations
            .Except(AgentCapabilityRegistry.AllowedConfirmationRules, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unsupportedConfirmations.Length > 0)
        {
            throw new ValidationException(
                "数字员工包含未注册人工确认规则：" + string.Join("、", unsupportedConfirmations));
        }

        confirmations = confirmations
            .Concat(AgentCapabilityRegistry.PermanentHumanBoundaries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToList();

        var stopConditions = NormalizeValues(snapshot.StopConditions, "熔断条件", 128);
        var unsupportedStops = stopConditions
            .Except(AgentCapabilityRegistry.AllowedStopConditions, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (unsupportedStops.Length > 0)
        {
            throw new ValidationException(
                "数字员工包含未注册熔断条件：" + string.Join("、", unsupportedStops));
        }

        if (stopConditions.Count == 0)
        {
            throw new ValidationException("数字员工至少需要一个熔断条件。");
        }

        return snapshot with
        {
            Code = code,
            Name = name,
            Description = description,
            DataScopes = dataScopes,
            ModelScopes = models,
            AllowedTools = tools,
            RequiredConfirmations = confirmations,
            StopConditions = stopConditions
        };
    }

    public static IReadOnlyList<string> HighRiskReasons(
        AgentProfileSnapshot? current,
        AgentProfileSnapshot next)
    {
        var reasons = new List<string>();
        if (current is null)
        {
            if (next.AutonomyLevel >= AgentAutonomyLevel.A3)
            {
                reasons.Add("首次发布即授予 A3/A4 自主等级");
            }

            if (next.RiskLimit != AgentRiskLevel.Low)
            {
                reasons.Add("首次发布风险上限高于 Low");
            }

            if (next.AmountLimit > 0)
            {
                reasons.Add("首次发布即授予金额能力");
            }

            if (next.AllowedTools.Any(item =>
                    item.Contains("proxy", StringComparison.OrdinalIgnoreCase)
                    || item.Contains("write", StringComparison.OrdinalIgnoreCase)))
            {
                reasons.Add("首次发布即授予写操作或代理能力");
            }

            return reasons;
        }

        if (next.AutonomyLevel > current.AutonomyLevel)
        {
            reasons.Add("提升自主等级");
        }

        if (next.RiskLimit > current.RiskLimit)
        {
            reasons.Add("提升风险上限");
        }

        if (next.AmountLimit > current.AmountLimit)
        {
            reasons.Add("提升金额上限");
        }

        if (next.ServiceUserId != current.ServiceUserId)
        {
            reasons.Add("更换独立服务身份");
        }

        if (next.AllowedTools.Except(current.AllowedTools, StringComparer.OrdinalIgnoreCase).Any())
        {
            reasons.Add("扩大工具白名单");
        }

        if (next.DataScopes.Except(current.DataScopes, StringComparer.OrdinalIgnoreCase).Any())
        {
            reasons.Add("扩大数据范围");
        }

        if (next.ModelScopes.Except(current.ModelScopes, StringComparer.OrdinalIgnoreCase).Any())
        {
            reasons.Add("扩大模型范围");
        }

        if (AgentCapabilityRegistry.PermanentHumanBoundaries.Any(boundary =>
                current.RequiredConfirmations.Contains(boundary, StringComparer.OrdinalIgnoreCase)
                && !next.RequiredConfirmations.Contains(boundary, StringComparer.OrdinalIgnoreCase)))
        {
            reasons.Add("删除永久人工确认边界");
        }

        if (current.StopConditions.Except(next.StopConditions, StringComparer.OrdinalIgnoreCase).Any())
        {
            reasons.Add("删除熔断条件");
        }

        return reasons;
    }

    public static bool ExceedsDepartmentManagerLimit(AgentProfileSnapshot snapshot) =>
        snapshot.AutonomyLevel is AgentAutonomyLevel.A3 or AgentAutonomyLevel.A4
        || snapshot.RiskLimit is AgentRiskLevel.High
        || snapshot.AmountLimit > 0;

    private static List<string> NormalizeValues(
        IReadOnlyList<string>? values,
        string label,
        int maxLength) =>
        (values ?? [])
            .Select(value => DomainChecks.Required(value, label, maxLength))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.Ordinal)
            .ToList();

    private static string NormalizeOptional(string? value, string label, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length > maxLength)
        {
            throw new ValidationException($"{label}不能超过 {maxLength} 字。");
        }

        return normalized;
    }

    [GeneratedRegex(
        "^(organization|department):[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex DataScopePattern();
}
