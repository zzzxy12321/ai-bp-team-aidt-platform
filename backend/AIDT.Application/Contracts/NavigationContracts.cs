namespace AIDT.Application.Contracts;

public sealed record NavigationItemDto(
    string Id,
    string ItemKey,
    string Name,
    string ItemType,
    string Route,
    string? RequiredResource,
    string? RequiredAction,
    int SortOrder,
    bool IsVisible,
    bool IsEnabled,
    bool IsSystem,
    string? Icon,
    bool IsFavorite,
    bool IsHidden);

public sealed record NavigationGroupDto(
    string WorkspaceId,
    string WorkspaceKey,
    string WorkspaceName,
    string WorkspaceType,
    string Layer,
    string? DepartmentId,
    string? DepartmentCode,
    string? DepartmentName,
    bool IsDefault,
    bool IsDemoData,
    bool IsTestData,
    int SortOrder,
    IReadOnlyList<NavigationItemDto> Items);

public sealed record CurrentNavigationDto(
    DateTimeOffset GeneratedAt,
    string? CurrentDepartmentId,
    IReadOnlyList<NavigationGroupDto> Groups);

public sealed record NavigationItemWriteRequest(
    string ItemKey,
    string Name,
    string ItemType,
    string Route,
    string? RequiredResource,
    string? RequiredAction,
    int SortOrder,
    bool IsVisible = true,
    bool IsEnabled = true,
    bool IsSystem = false,
    string? Icon = null);

public sealed record CreateNavigationWorkspaceRequest(
    string WorkspaceKey,
    string Name,
    string WorkspaceType,
    Guid? DepartmentId,
    string Template,
    IReadOnlyList<string>? OptionalEntries);

public sealed record UpdateNavigationWorkspaceRequest(
    long ExpectedVersion,
    string Name,
    string Template,
    IReadOnlyList<NavigationItemWriteRequest> Items);

public sealed record NavigationWorkspaceDto(
    string Id,
    string WorkspaceKey,
    string Name,
    string WorkspaceType,
    string? DepartmentId,
    string? DepartmentName,
    bool IsSystem,
    bool IsDefault,
    bool IsDemoData,
    string EnvironmentCode,
    bool IsTestData,
    string Status,
    long Version,
    string DraftVersionId,
    int DraftVersionNumber,
    long Revision,
    string EditingVersionStatus,
    IReadOnlyList<NavigationItemDto> Items,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt);

public sealed record NavigationPreferenceItem(
    string WorkspaceKey,
    string ItemKey,
    bool IsFavorite,
    bool IsHidden,
    int SortOrder);

public sealed record SetNavigationPreferencesRequest(
    IReadOnlyList<NavigationPreferenceItem> Items);

public sealed record NavigationPreferenceDto(
    string WorkspaceKey,
    string ItemKey,
    bool IsFavorite,
    bool IsHidden,
    int SortOrder,
    DateTimeOffset UpdatedAt);
