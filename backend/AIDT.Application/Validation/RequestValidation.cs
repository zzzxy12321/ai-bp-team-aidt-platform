using System.Text.RegularExpressions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;

namespace AIDT.Application.Validation;

public static partial class RequestValidation
{
    public static void Validate(CreateBusinessObjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ObjectTypeId == Guid.Empty)
        {
            throw new ValidationException("对象类型不能为空。");
        }

        var code = request.ObjectCode?.Trim();
        if (string.IsNullOrWhiteSpace(code) || code.Length > 80 || !StableCodeRegex().IsMatch(code))
        {
            throw new ValidationException("对象编号必须为 1—80 位大写字母、数字、下划线或短横线。");
        }

        if (request.Fields is null)
        {
            throw new ValidationException("业务对象字段不能为空。");
        }
    }

    public static void Validate(UpdateBusinessObjectRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ExpectedVersion < 1)
        {
            throw new ValidationException("业务对象期望版本必须大于等于 1。");
        }

        if (request.Fields is null)
        {
            throw new ValidationException("业务对象字段不能为空。");
        }
    }

    public static void Validate(TaskDecisionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ExpectedVersion < 1)
        {
            throw new ValidationException("任务版本必须大于等于 1。");
        }

        var idempotencyKey = request.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || idempotencyKey.Length is < 8 or > 128)
        {
            throw new ValidationException("任务动作必须提供 8—128 位幂等键。");
        }

        if (request.Decision == Domain.Workflows.WorkflowDecision.Return
            && string.IsNullOrWhiteSpace(request.Comment))
        {
            throw new ValidationException("退回操作必须填写原因。");
        }
    }

    public static void Validate(StartWorkflowInstanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.WorkflowDefinitionId == Guid.Empty
            || request.WorkflowVersionId == Guid.Empty
            || request.BusinessObjectId == Guid.Empty)
        {
            throw new ValidationException("流程定义、流程版本和业务对象均不能为空。");
        }

        var idempotencyKey = request.IdempotencyKey?.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey)
            || idempotencyKey.Length is < 8 or > 128)
        {
            throw new ValidationException("流程发起必须提供 8—128 位幂等键。");
        }
    }

    [GeneratedRegex("^[A-Z][A-Z0-9_-]*$", RegexOptions.CultureInvariant)]
    private static partial Regex StableCodeRegex();
}
