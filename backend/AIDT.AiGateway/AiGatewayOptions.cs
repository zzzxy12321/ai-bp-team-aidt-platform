using Microsoft.Extensions.Options;

namespace AIDT.AiGateway;

public sealed class AiGatewayOptions
{
    public const string SectionName = "AiGateway";

    public string Mode { get; set; } = "RuleSimulator";

    public bool ExternalModelEnabled { get; set; }

    public int TimeoutSeconds { get; set; } = 10;

    public string[] AllowedModels { get; set; } = ["aidt-rule-sim"];
}

public sealed class AiGatewayOptionsValidation : IValidateOptions<AiGatewayOptions>
{
    public ValidateOptionsResult Validate(string? name, AiGatewayOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var failures = new List<string>();
        if (!string.Equals(options.Mode, "RuleSimulator", StringComparison.Ordinal))
        {
            failures.Add("Demo 只允许 AiGateway:Mode=RuleSimulator。");
        }

        if (options.ExternalModelEnabled)
        {
            failures.Add("Demo 禁止 ExternalModelEnabled=true。");
        }

        if (options.TimeoutSeconds is < 1 or > 60)
        {
            failures.Add("AiGateway 超时必须为 1—60 秒。");
        }

        if (options.AllowedModels.Length == 0
            || options.AllowedModels.Any(string.IsNullOrWhiteSpace))
        {
            failures.Add("AiGateway 至少需要一个非空白名单模型。");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
