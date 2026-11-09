using AIDT.Domain.Common;

namespace AIDT.Domain.Scenarios;

public enum ScenarioLifecycleStatus
{
    Draft,
    Published,
    Disabled
}

public enum ScenarioVersionStatus
{
    Draft,
    Validated,
    Published,
    Retired
}

public enum ScenarioStartMode
{
    CreateBusinessObjectAndWorkflow,
    ExistingBusinessObjectAndWorkflow,
    EventInbox,
    ProjectTask
}

public static class ScenarioRules
{
    public static readonly IReadOnlySet<string> PermanentHumanBoundaries =
        new HashSet<string>(
            [
                "财务付款",
                "质量放行",
                "配方变更",
                "设备控制",
                "安全联锁",
                "外部合同承诺",
                "客户重大赔付"
            ],
            StringComparer.Ordinal);

    public static void EnsureMetadataEditable(
        ScenarioLifecycleStatus lifecycleStatus,
        bool isServiceIdentity)
    {
        if (isServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "数字员工服务身份不能创建、编辑、发布或启动业务场景。");
        }

        if (lifecycleStatus == ScenarioLifecycleStatus.Disabled)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioDisabled,
                "当前场景已停用，请先恢复后再编辑。");
        }
    }

    public static void EnsureVersionEditable(
        ScenarioVersionStatus status,
        bool isServiceIdentity)
    {
        if (isServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "数字员工服务身份不能编辑场景版本。");
        }

        if (status is not (ScenarioVersionStatus.Draft or ScenarioVersionStatus.Validated))
        {
            throw new ConflictException(
                ErrorCodes.ScenarioVersionConflict,
                "已发布的场景版本不可原地修改，请创建新版本。");
        }
    }

    public static ScenarioLifecycleStatus ParseLifecycleStatus(string value) =>
        Enum.TryParse<ScenarioLifecycleStatus>(value, true, out var status)
            ? status
            : throw new ValidationException("场景生命周期状态无效。");

    public static ScenarioVersionStatus ParseVersionStatus(string value) =>
        Enum.TryParse<ScenarioVersionStatus>(value, true, out var status)
            ? status
            : throw new ValidationException("场景版本状态无效。");

    public static ScenarioStartMode ParseStartMode(string value) =>
        Enum.TryParse<ScenarioStartMode>(value, true, out var mode)
            ? mode
            : throw new ValidationException("场景启动方式无效。");

    public static void EnsureCanPublish(
        IReadOnlyCollection<string> validationErrors,
        ScenarioVersionStatus status)
    {
        if (validationErrors.Count > 0)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioReferenceInvalid,
                $"场景校验未通过：{string.Join("；", validationErrors.Take(5))}。");
        }

        if (status is ScenarioVersionStatus.Published or ScenarioVersionStatus.Retired)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioVersionConflict,
                "当前场景版本已发布或已退役，不能重复发布。");
        }
    }

    public static void EnsureStartable(
        ScenarioLifecycleStatus lifecycleStatus,
        ScenarioVersionStatus versionStatus,
        bool referencesValid,
        IReadOnlyCollection<string> referenceErrors)
    {
        if (lifecycleStatus == ScenarioLifecycleStatus.Disabled)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioDisabled,
                "当前场景已停用，不能发起新的业务事项。");
        }

        if (lifecycleStatus != ScenarioLifecycleStatus.Published
            || versionStatus != ScenarioVersionStatus.Published)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioNotPublished,
                "当前场景版本尚未发布或已退役，不能启动。");
        }

        if (!referencesValid)
        {
            throw new ConflictException(
                ErrorCodes.ScenarioReferenceInvalid,
                $"当前场景版本不可启动：{string.Join("；", referenceErrors.Take(5))}。");
        }
    }

    public static void EnsureNoAutomaticHighRiskAction(
        IReadOnlyCollection<string> permanentHumanBoundaries)
    {
        foreach (var boundary in PermanentHumanBoundaries)
        {
            if (!permanentHumanBoundaries.Contains(boundary))
            {
                throw new ValidationException(
                    ErrorCodes.ScenarioBoundaryMissing,
                    $"场景必须保留永久人工边界：{boundary}。");
            }
        }
    }
}
