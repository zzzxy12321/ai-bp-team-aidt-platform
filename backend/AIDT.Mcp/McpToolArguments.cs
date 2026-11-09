using System.ComponentModel;

namespace AIDT.Mcp;

public sealed record ListPageArguments(
    [property: Description("页码，从 1 开始。")] int Page = 1,
    [property: Description("每页数量，1—100。")] int PageSize = 20,
    [property: Description("是否包含测试数据；仅平台管理员原本有该能力，仍需 MCP 部门作用域。")] bool IncludeTestData = false);

public sealed record ListProjectsArguments(
    int Page = 1,
    int PageSize = 20,
    bool IncludeTestData = false);

public sealed record ProjectIdArguments(
    [property: Description("AIDT 项目 ID。")] Guid ProjectId,
    bool IncludeTestData = false);

public sealed record CreateProjectArguments(
    [property: Description("项目业务编号，大写字母开头。")] string ObjectCode,
    string Name,
    Guid OwnerUserId,
    Guid DepartmentId,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    string Objective,
    string CurrentStage,
    string ConfidentialityLevel,
    string? InitialStatus,
    [property: Description("8—128 字稳定幂等键。")] string IdempotencyKey);

public sealed record AddProjectMemberArguments(
    Guid ProjectId,
    Guid UserId,
    string ProjectRole,
    bool IsLead,
    string IdempotencyKey);

public sealed record AddProjectMilestoneArguments(
    Guid ProjectId,
    string Title,
    Guid OwnerUserId,
    DateTimeOffset PlannedDate,
    string Status,
    string IdempotencyKey);

public sealed record ChangeProjectStatusArguments(
    Guid ProjectId,
    long ExpectedVersion,
    string Status,
    string? Comment,
    string IdempotencyKey,
    Guid? HumanConfirmationId = null);

public sealed record CompleteProjectTaskArguments(
    Guid TaskId,
    long ExpectedVersion,
    string? Comment,
    string IdempotencyKey);

public sealed record EntityIdArguments(Guid Id, bool IncludeTestData = false);

public sealed record ListEventsArguments(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    bool IncludeTestData = false);

public sealed record CreateEventArguments(
    Guid DepartmentId,
    string Title,
    string Content,
    string SourceType,
    Guid? SourceId,
    Guid? AttachmentId,
    string IdempotencyKey);

public sealed record AnalyzeEventToolArguments(
    Guid EventId,
    Guid AgentProfileId,
    string IdempotencyKey);

public sealed record ConfirmEventToolArguments(
    Guid EventId,
    Guid? ProjectId,
    Guid? AssigneeId,
    DateTimeOffset? DueAt,
    string? Priority,
    string? Content,
    Guid? WorkflowInstanceId,
    bool CreateTask,
    string IdempotencyKey,
    Guid? HumanConfirmationId = null);

public sealed record ListWorkflowArguments(
    int Page = 1,
    int PageSize = 20,
    string? Status = null,
    bool IncludeTestData = false);

public sealed record DecideWorkflowTaskArguments(
    Guid TaskId,
    string Decision,
    long ExpectedVersion,
    string? Comment,
    string IdempotencyKey,
    Guid? HumanConfirmationId = null);

public sealed record TransferWorkflowTaskArguments(
    Guid TaskId,
    Guid TargetUserId,
    long ExpectedVersion,
    string Reason,
    string IdempotencyKey,
    Guid? HumanConfirmationId = null);

public sealed record ListFilesArguments(
    Guid? DepartmentId = null,
    Guid? BusinessObjectId = null,
    Guid? WorkflowInstanceId = null,
    Guid? WorkflowTaskId = null,
    string? EnvironmentCode = null,
    bool IncludeTestData = false,
    int Page = 1,
    int PageSize = 20);

public sealed record UploadFileArguments(
    string FileName,
    string MimeType,
    string Base64Content,
    Guid DepartmentId,
    Guid? BusinessObjectId,
    Guid? WorkflowInstanceId,
    Guid? WorkflowTaskId,
    string Sensitivity,
    string IdempotencyKey,
    Guid? HumanConfirmationId = null);

public sealed record DownloadFileArguments(
    Guid FileId,
    bool IncludeTestData = false,
    Guid? HumanConfirmationId = null);

public sealed record ListAuditArguments(
    int Page = 1,
    int PageSize = 20,
    string? EntityType = null,
    string? EntityId = null);
