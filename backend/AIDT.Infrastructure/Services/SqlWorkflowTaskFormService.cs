using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Audit;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;
using AIDT.Infrastructure.Persistence;
using AIDT.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlWorkflowTaskFormService(
    AidtDbContext dbContext,
    IWorkflowTaskFormValidator validator,
    IAuditRepository auditRepository,
    IIdempotencyStore idempotencyStore,
    IUnitOfWork unitOfWork,
    IClock clock) : IWorkflowTaskFormService
{
    public async Task<WorkflowTaskFormDto> GetAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-task", AccessAction.Read);
        var task = await LoadTaskAsync(user, taskId, cancellationToken);
        var instance = await dbContext.WorkflowInstances.AsNoTracking().SingleAsync(
            item => item.Id == task.WorkflowInstanceId,
            cancellationToken);
        var policy = task.NodePolicyId.HasValue
            ? await dbContext.WorkflowNodePolicies.AsNoTracking().SingleOrDefaultAsync(
                item => item.Id == task.NodePolicyId.Value,
                cancellationToken)
            : null;
        var fields = JsonStorage.Deserialize<WorkflowFormFieldRequest[]>(
                policy?.FormSchemaJson ?? task.FormDefinitionJson) ?? [];
        var visibleFields = fields
            .Where(field => CanViewField(user, task.DepartmentId, field))
            .OrderBy(field => field.SortOrder)
            .ToList();
        return new WorkflowTaskFormDto(
            task.Id.ToString("D"),
            instance.Id.ToString("D"),
            instance.BusinessObjectId.ToString("D"),
            task.DepartmentId.ToString("D"),
            task.NodeCode,
            policy?.NodeName ?? task.NodeName,
            policy?.BusinessDescription,
            task.DueAt,
            policy?.FormSchemaVersion ?? task.FormSchemaVersion,
            task.FormDefinitionHash,
            visibleFields,
            JsonStorage.Deserialize<WorkflowNodeActionRequest[]>(policy?.NodeActionsJson ?? "[]") ?? [],
            JsonStorage.Deserialize<WorkflowAiReviewRequest>(policy?.AiReviewJson ?? "{}")
                ?? new WorkflowAiReviewRequest(),
            policy?.NotificationPolicyJson ?? "{}",
            policy?.PermanentHumanBoundary ?? false,
            task.FormDataJson);
    }

    public async Task<WorkflowTaskDto> SaveDraftAsync(
        UserPrincipal user,
        Guid taskId,
        SaveWorkflowTaskFormDraftRequest request,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("workflow-task", AccessAction.Approve);
        if (user.IsServiceIdentity)
        {
            throw new PermissionException(
                ErrorCodes.PermissionDenied,
                "服务身份不能保存真人任务表单。");
        }

        var task = await LoadTaskEntityAsync(user, taskId, cancellationToken);
        EnsureTaskEditor(user, task);
        if (task.Version != request.ExpectedVersion)
        {
            throw new ConflictException(
                ErrorCodes.WorkflowVersionConflict,
                "任务版本已变化，请刷新后重试。");
        }
        EnsureFormVersion(task, request.FormDefinitionHash);
        var hash = Hash(new
        {
            taskId,
            request.ExpectedVersion,
            FormDataJson = request.FormDataJson.Trim(),
            request.FormDefinitionHash
        });
        var scope = $"workflow-task:form-draft:{taskId:D}:{user.UserId:D}";
        var existing = await idempotencyStore.FindAsync(scope, request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            if (!string.Equals(existing.RequestHash, hash, StringComparison.Ordinal))
            {
                throw new ConflictException(
                    ErrorCodes.Conflict,
                    "同一幂等键不能用于不同表单草稿。");
            }

            return MapTask(task);
        }

        var domainTask = MapDomain(task);
        var validation = await validator.ValidateAsync(
            user,
            domainTask,
            request.FormDataJson,
            enforceRequired: false,
            cancellationToken);
        if (!validation.IsValid)
        {
            throw new ValidationException(string.Join("；", validation.Errors));
        }

        var now = clock.UtcNow;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            task.FormDataJson = validation.NormalizedJson;
            task.Version++;
            dbContext.WorkflowTaskActions.Add(new WorkflowTaskActionEntity
            {
                WorkflowTaskId = task.Id,
                ActorId = user.UserId,
                Action = "SaveFormDraft",
                CreatedAt = now
            });
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Workflow",
                    "workflow-task.form_draft_saved",
                    nameof(WorkflowTaskEntity),
                    task.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: validation.NormalizedJson),
                cancellationToken);
            await idempotencyStore.AddAsync(
                new IdempotencyEntry(
                    Guid.NewGuid(),
                    scope,
                    request.IdempotencyKey,
                    hash,
                    nameof(WorkflowTaskEntity),
                    task.Id.ToString("D"),
                    "completed",
                    now,
                    now.AddDays(30)),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return MapTask(task);
    }

    private async Task<WorkflowTaskEntity> LoadTaskAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.WorkflowTasks.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken) ?? throw new NotFoundException("流程任务不存在。");
        user.EnsureDepartmentAccess(task.DepartmentId);
        if (!user.IsPlatformAdmin && task.AssigneeId != user.UserId)
        {
            throw new PermissionException(
                ErrorCodes.TaskAssigneeMismatch,
                "只有当前任务处理人可以查看该任务表单。");
        }
        return task;
    }

    private Task<WorkflowTaskEntity> LoadTaskEntityAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken) =>
        LoadTrackedTaskAsync(user, taskId, cancellationToken);

    private async Task<WorkflowTaskEntity> LoadTrackedTaskAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var task = await dbContext.WorkflowTasks.SingleOrDefaultAsync(
            item => item.Id == taskId,
            cancellationToken) ?? throw new NotFoundException("流程任务不存在。");
        user.EnsureDepartmentAccess(task.DepartmentId);
        return task;
    }

    private static void EnsureTaskEditor(UserPrincipal user, WorkflowTaskEntity task)
    {
        if (task.Status != "Pending")
        {
            throw new ConflictException(
                ErrorCodes.TaskAlreadyProcessed,
                "任务已经处理，不能继续保存表单。");
        }
        if (!user.IsPlatformAdmin && task.AssigneeId != user.UserId)
        {
            throw new PermissionException(
                ErrorCodes.TaskAssigneeMismatch,
                "只有当前任务处理人可以保存该表单。");
        }
    }

    private static void EnsureFormVersion(WorkflowTaskEntity task, string submittedHash)
    {
        if (!string.IsNullOrWhiteSpace(task.FormDefinitionHash)
            && !string.Equals(
                task.FormDefinitionHash,
                submittedHash,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                ErrorCodes.WorkflowFormDefinitionChanged,
                "节点字段定义已变化，请刷新页面。");
        }
    }

    private static bool CanViewField(
        UserPrincipal user,
        Guid departmentId,
        WorkflowFormFieldRequest field) =>
        user.IsPlatformAdmin
        || field.VisibleRoleCodes is null
        || field.VisibleRoleCodes.Count == 0
        || field.VisibleRoleCodes.Any(role => user.HasRoleInScope(role, departmentId));

    private static WorkflowTask MapDomain(WorkflowTaskEntity entity) =>
        WorkflowTask.Restore(
            entity.Id,
            entity.WorkflowInstanceId,
            entity.NodeCode,
            entity.AssigneeId,
            Enum.Parse<WorkflowTaskStatus>(entity.Status),
            entity.Version,
            entity.DecisionComment,
            entity.CreatedAt,
            entity.CompletedAt,
            entity.DepartmentId,
            entity.EnvironmentCode,
            entity.IsTestData,
            entity.NodePolicyId,
            entity.DueAt,
            entity.SlaStatus,
            entity.ReminderSentAt,
            entity.EscalatedAt,
            entity.DecisionMode,
            entity.AgentRunId,
            Enum.TryParse<WorkflowAssignmentStrategy>(entity.AssigneeStrategy, true, out var strategy)
                ? strategy
                : WorkflowAssignmentStrategy.SpecificUser,
            entity.BusinessRoleId,
            entity.BusinessRoleCode,
            entity.BusinessRoleVersion,
            JsonStorage.Deserialize<Guid[]>(entity.CandidateUserIdsJson) ?? [entity.AssigneeId],
            entity.AssigneeSnapshotJson,
            entity.OriginalAssigneeId,
            entity.BackupAssigneeId,
            entity.TransferredBy,
            entity.TransferredAt,
            entity.TransferReason,
            entity.NodeName,
            entity.FormDefinitionJson,
            entity.FormDataJson,
            entity.FormSchemaVersion,
            entity.FormDefinitionHash);

    public static WorkflowTaskDto MapTask(WorkflowTaskEntity item) =>
        new(
            item.Id.ToString("D"),
            item.WorkflowInstanceId.ToString("D"),
            item.NodeCode,
            item.AssigneeId.ToString("D"),
            Enum.Parse<WorkflowTaskStatus>(item.Status),
            item.Version,
            item.DecisionComment,
            item.CreatedAt,
            item.CompletedAt,
            item.DepartmentId.ToString("D"),
            item.EnvironmentCode,
            item.IsTestData,
            item.DueAt,
            item.SlaStatus,
            item.ReminderSentAt,
            item.EscalatedAt,
            item.DecisionMode,
            item.AgentRunId?.ToString("D"),
            item.AssigneeStrategy,
            item.BusinessRoleCode,
            item.BusinessRoleVersion,
            JsonStorage.Deserialize<string[]>(item.CandidateUserIdsJson) ?? [],
            item.OriginalAssigneeId?.ToString("D"),
            item.BackupAssigneeId?.ToString("D"),
            item.TransferredBy?.ToString("D"),
            item.TransferredAt,
            item.TransferReason,
            item.NodeName,
            item.FormDefinitionJson,
            item.FormDataJson,
            item.FormSchemaVersion,
            item.FormDefinitionHash);

    private static string Hash<T>(T value) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(value, JsonSerializerOptions.Web))));
}

public sealed class SqlWorkflowTaskFormValidator(AidtDbContext dbContext)
    : IWorkflowTaskFormValidator
{
    private static readonly HashSet<string> SelectionTypes =
        new(["Select", "MultiSelect"], StringComparer.Ordinal);

    public async Task<WorkflowTaskFormValidationDto> ValidateAsync(
        UserPrincipal user,
        WorkflowTask task,
        string? formDataJson,
        bool enforceRequired,
        CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        string errorCode = ErrorCodes.ValidationFailed;
        var fields = JsonStorage.Deserialize<WorkflowFormFieldRequest[]>(
                task.FormDefinitionJson) ?? [];
        var visibleFields = fields
            .Where(field =>
                user.IsPlatformAdmin
                || field.VisibleRoleCodes is null
                || field.VisibleRoleCodes.Count == 0
                || field.VisibleRoleCodes.Any(role =>
                    user.HasRoleInScope(role, task.DepartmentId)))
            .OrderBy(field => field.SortOrder)
            .ToList();
        var visibleKeys = visibleFields.Select(field => field.Key).ToHashSet(StringComparer.Ordinal);
        Dictionary<string, JsonElement> input;
        try
        {
            input = string.IsNullOrWhiteSpace(formDataJson)
                ? new Dictionary<string, JsonElement>(StringComparer.Ordinal)
                : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    formDataJson,
                    JsonSerializerOptions.Web)
                    ?? new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        }
        catch (JsonException)
        {
            return new WorkflowTaskFormValidationDto(
                false,
                ErrorCodes.ValidationFailed,
                ["表单内容不是有效 JSON。"],
                "{}",
                visibleKeys.ToList());
        }

        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var field in visibleFields)
        {
            if (!input.TryGetValue(field.Key, out var value)
                && !string.IsNullOrWhiteSpace(field.DefaultValue))
            {
                value = JsonSerializer.SerializeToElement(
                    field.DefaultValue,
                    JsonSerializerOptions.Web);
            }

            var writable = !field.ReadOnly
                && (user.IsPlatformAdmin
                    || field.WriteRoleCodes is null
                    || field.WriteRoleCodes.Count == 0
                    || field.WriteRoleCodes.Any(role =>
                        user.HasRoleInScope(role, task.DepartmentId)));
            if (!writable)
            {
                if (input.ContainsKey(field.Key))
                {
                    errors.Add($"字段“{field.DisplayName}”为只读，不能修改。");
                }
                continue;
            }

            var isEmpty = !input.ContainsKey(field.Key)
                || value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined
                || value.ValueKind == JsonValueKind.String
                    && string.IsNullOrWhiteSpace(value.GetString())
                || value.ValueKind == JsonValueKind.Array
                    && value.GetArrayLength() == 0;
            if (isEmpty)
            {
                if (enforceRequired && field.Required)
                {
                    errors.Add($"缺少必填字段“{field.DisplayName}”。");
                    errorCode = ErrorCodes.WorkflowFormRequiredFieldMissing;
                }
                continue;
            }

            var parsed = await ParseFieldAsync(field, value, user, task, cancellationToken);
            if (parsed.Error is not null)
            {
                errors.Add(parsed.Error);
                if (field.DataType == "Attachment")
                {
                    errorCode = ErrorCodes.WorkflowFormAttachmentMissing;
                }
                continue;
            }
            normalized[field.Key] = parsed.Value;
        }

        if (enforceRequired && task.NodePolicyId.HasValue)
        {
            var requiredAttachmentCount = await dbContext.WorkflowNodePolicies.AsNoTracking()
                .Where(item => item.Id == task.NodePolicyId.Value)
                .Select(item => (int?)item.RequiredAttachmentCount)
                .SingleOrDefaultAsync(cancellationToken) ?? 0;
            var suppliedAttachmentCount = visibleFields
                .Where(field => field.DataType == "Attachment")
                .Sum(field =>
                    ArrayValueCount(normalized.GetValueOrDefault(field.Key)));
            if (suppliedAttachmentCount < requiredAttachmentCount)
            {
                errors.Add(
                    $"附件要求未满足：至少需要 {requiredAttachmentCount} 个附件，当前 {suppliedAttachmentCount} 个。");
                errorCode = ErrorCodes.WorkflowFormAttachmentMissing;
            }
        }

        var unexpected = input.Keys
            .Where(key => !visibleKeys.Contains(key))
            .ToArray();
        if (unexpected.Length > 0)
        {
            errors.Add("表单包含当前岗位不可见字段，请刷新后重试。");
        }

        return new WorkflowTaskFormValidationDto(
            errors.Count == 0,
            errorCode,
            errors,
            JsonSerializer.Serialize(normalized, JsonSerializerOptions.Web),
            visibleKeys.ToList());
    }

    private async Task<(object? Value, string? Error)> ParseFieldAsync(
        WorkflowFormFieldRequest field,
        JsonElement value,
        UserPrincipal user,
        WorkflowTask task,
        CancellationToken cancellationToken)
    {
        if (field.DataSource == "BusinessObjectReference")
        {
            return (null, null);
        }

        switch (field.DataType)
        {
            case "Text":
                return StringValue(value) is { Length: <= 2000 } text
                    ? (text, null)
                    : (null, $"字段“{field.DisplayName}”最多 2000 个字符。");
            case "LongText":
                return StringValue(value) is { Length: <= 20000 } longText
                    ? (longText, null)
                    : (null, $"字段“{field.DisplayName}”最多 20000 个字符。");
            case "Number":
            case "Money":
            case "Percent":
                return decimal.TryParse(
                    StringValue(value),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var number)
                    ? (number, null)
                    : (null, $"字段“{field.DisplayName}”必须是有效数字。");
            case "Date":
            case "DateTime":
            case "DateRange":
                return DateTimeOffset.TryParse(
                    StringValue(value),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var date)
                    || field.DataType == "DateRange"
                        && value.ValueKind == JsonValueKind.Array
                    ? (value.Clone(), null)
                    : (null, $"字段“{field.DisplayName}”日期格式无效。");
            case "Boolean":
                return value.ValueKind is JsonValueKind.True or JsonValueKind.False
                    || bool.TryParse(StringValue(value), out _)
                    ? (value.Clone(), null)
                    : (null, $"字段“{field.DisplayName}”必须为是或否。");
            case "Select":
                return ValidateOption(field, StringValue(value))
                    ? (StringValue(value), null)
                    : (null, $"字段“{field.DisplayName}”选项无效。");
            case "MultiSelect":
                var selected = StringArray(value);
                return selected.Count > 0
                    && selected.All(option => field.Options?.Contains(option, StringComparer.Ordinal) == true)
                    ? (selected, null)
                    : (null, $"字段“{field.DisplayName}”包含无效选项。");
            case "User":
            case "MultiUser":
                IReadOnlyList<Guid> userIds = field.DataType == "User"
                    ? OneGuid(value)
                    : StringArray(value)
                        .Select(item => Guid.TryParse(item, out var id) ? id : Guid.Empty)
                        .Where(id => id != Guid.Empty)
                        .ToArray();
                if (userIds.Count == 0)
                {
                    return (null, $"字段“{field.DisplayName}”人员候选无效。");
                }
                var accessibleUsers = await dbContext.Users.AsNoTracking()
                    .Where(item =>
                        userIds.Contains(item.Id)
                        && item.OrganizationId == user.OrganizationId
                        && item.IsActive
                        && !item.IsServiceIdentity)
                    .Where(item =>
                        user.IsPlatformAdmin
                        || dbContext.UserDepartmentMemberships.Any(membership =>
                            membership.UserId == item.Id
                            && membership.DepartmentId == task.DepartmentId
                            && membership.IsActive))
                    .Select(item => item.Id)
                    .ToListAsync(cancellationToken);
                return accessibleUsers.Count == userIds.Count
                    ? (field.DataType == "User" ? userIds[0].ToString("D") : userIds.Select(id => id.ToString("D")).ToList(), null)
                    : (null, $"字段“{field.DisplayName}”包含当前部门无权选择的人员。");
            case "Department":
                var departmentIds = StringArray(value)
                    .Select(item => Guid.TryParse(item, out var id) ? id : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToArray();
                return departmentIds.Length > 0
                    && departmentIds.All(id => user.IsPlatformAdmin || user.DepartmentIds.Contains(id))
                    ? (departmentIds.Length == 1 ? departmentIds[0].ToString("D") : departmentIds.Select(id => id.ToString("D")).ToList(), null)
                    : (null, $"字段“{field.DisplayName}”部门候选超出当前授权范围。");
            case "Attachment":
                var attachmentIds = StringArray(value)
                    .Select(item => Guid.TryParse(item, out var id) ? id : Guid.Empty)
                    .Where(id => id != Guid.Empty)
                    .ToArray();
                var valid = await dbContext.Attachments.AsNoTracking()
                    .CountAsync(item =>
                        attachmentIds.Contains(item.Id)
                        && item.OrganizationId == user.OrganizationId
                        && item.DepartmentId == task.DepartmentId
                        && item.Status == "Active"
                        && (task.IsTestData || !item.IsTestData),
                        cancellationToken);
                return attachmentIds.Length > 0 && valid == attachmentIds.Length
                    ? (attachmentIds.Select(id => id.ToString("D")).ToList(), null)
                    : (null, $"字段“{field.DisplayName}”附件不存在、无权访问或要求未满足。");
            case "ReadOnlySystem":
                return (value.Clone(), null);
            case "BusinessObjectField":
                return (StringValue(value), null);
            default:
                return (null, $"字段“{field.DisplayName}”类型不受支持。");
        }
    }

    private static bool ValidateOption(WorkflowFormFieldRequest field, string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && field.Options?.Contains(value, StringComparer.Ordinal) == true;

    private static string? StringValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };

    private static List<string> StringArray(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(StringValue)
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Cast<string>()
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        var single = StringValue(value);
        return string.IsNullOrWhiteSpace(single) ? [] : [single];
    }

    private static List<Guid> OneGuid(JsonElement value) =>
        Guid.TryParse(StringValue(value), out var id) ? [id] : [];

    private static int ArrayValueCount(object? value) =>
        value switch
        {
            IReadOnlyCollection<string> items => items.Count,
            string text when !string.IsNullOrWhiteSpace(text) => 1,
            _ => 0
        };
}
