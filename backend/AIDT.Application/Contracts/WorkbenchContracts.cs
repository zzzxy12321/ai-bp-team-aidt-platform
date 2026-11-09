namespace AIDT.Application.Contracts;

public sealed record WorkbenchCardItemDto(
    string Id,
    string BusinessTitle,
    string ContextTitle,
    string Status,
    string RiskLevel,
    DateTimeOffset? DueAt,
    string DetailPath);

public sealed record WorkbenchResolvedCardDto(
    string Id,
    string CardType,
    string Title,
    string? Description,
    int SortOrder,
    string Width,
    string State,
    string Message,
    int TotalCount,
    IReadOnlyList<WorkbenchCardItemDto> Items);

public sealed record DepartmentWorkbenchDto(
    bool Matched,
    string? TemplateId,
    string? TemplateCode,
    string? TemplateName,
    string? TemplateVersionId,
    int? TemplateVersionNumber,
    string DepartmentId,
    string DepartmentName,
    string AudienceTitle,
    string PageTitle,
    string PrimaryQuestion,
    IReadOnlyList<string> RoleNames,
    string FallbackMode,
    string? FallbackReason,
    DateTimeOffset LastUpdatedAt,
    IReadOnlyList<WorkbenchResolvedCardDto> Cards);

public sealed record WorkbenchCardWriteRequest(
    string CardType,
    string Title,
    string? Description,
    int SortOrder,
    string Width,
    string DataSourceType,
    IReadOnlyDictionary<string, string?>? Filter,
    string RequiredResource,
    string RequiredAction,
    bool IsEnabled = true,
    string CapabilityState = "Connected");

public sealed record WorkbenchAssignmentWriteRequest(
    string AssignmentType,
    string ScopeValue,
    int Priority = 0);

public sealed record WorkbenchCardAdminDto(
    string Id,
    string CardType,
    string Title,
    string? Description,
    int SortOrder,
    string Width,
    string DataSourceType,
    IReadOnlyDictionary<string, string?> Filter,
    string RequiredResource,
    string RequiredAction,
    bool IsEnabled,
    string CapabilityState);

public sealed record WorkbenchAssignmentDto(
    string Id,
    string AssignmentType,
    string ScopeValue,
    int Priority);

public sealed record WorkbenchTemplateVersionDto(
    string Id,
    int VersionNumber,
    string Status,
    long Revision,
    string? ValidationJson,
    string? ImpactAnalysisJson,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    IReadOnlyList<WorkbenchCardAdminDto> Cards,
    IReadOnlyList<WorkbenchAssignmentDto> Assignments);

public sealed record WorkbenchTemplateDto(
    string Id,
    string Code,
    string Name,
    string Description,
    string AudienceTitle,
    string PrimaryQuestion,
    string? DepartmentId,
    string? DepartmentName,
    bool IsSystem,
    bool IsDefault,
    string Status,
    int SortOrder,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    WorkbenchTemplateVersionDto? DraftVersion,
    WorkbenchTemplateVersionDto? PublishedVersion);

public sealed record CreateWorkbenchTemplateRequest(
    string Code,
    string Name,
    string Description,
    string AudienceTitle,
    string PrimaryQuestion,
    Guid? DepartmentId,
    int SortOrder = 100,
    bool IsDefault = false);

public sealed record UpdateWorkbenchTemplateDraftRequest(
    long ExpectedRevision,
    string Name,
    string Description,
    string AudienceTitle,
    string PrimaryQuestion,
    Guid? DepartmentId,
    int SortOrder,
    bool IsDefault,
    IReadOnlyList<WorkbenchCardWriteRequest> Cards,
    IReadOnlyList<WorkbenchAssignmentWriteRequest> Assignments);

public sealed record CopyWorkbenchTemplateRequest(
    string Code,
    string Name,
    Guid? DepartmentId,
    string? AudienceTitle = null);

public sealed record WorkbenchTemplateValidationDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    int VisibleCardCount,
    int AssignmentCount,
    string ImpactSummary);

public sealed record SetWorkbenchTemplateStatusRequest(bool Retire);
