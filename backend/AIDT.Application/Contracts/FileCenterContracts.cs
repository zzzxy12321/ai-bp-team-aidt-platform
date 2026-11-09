using AIDT.Domain.Documents;

namespace AIDT.Application.Contracts;

public sealed record FileLinkDto(string EntityType, string EntityId);

public sealed record FileAttachmentDto(
    string Id,
    string DepartmentId,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string Sha256,
    string VirusScanStatus,
    string Sensitivity,
    string Status,
    string EnvironmentCode,
    bool IsTestData,
    string SourceSystem,
    int CurrentVersionNumber,
    string PreviewKind,
    IReadOnlyList<FileLinkDto> Links,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? DeletionRequestedBy,
    DateTimeOffset? DeletionRequestedAt,
    string? DeletionReason);

public sealed record FileVersionDto(
    string Id,
    int VersionNumber,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string Sha256,
    string VirusScanStatus,
    string Sensitivity,
    Guid CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record FileContentDto(
    Stream Content,
    string MimeType,
    string DownloadName,
    string PreviewKind);

public sealed record UploadFileCommand(
    Stream Content,
    string OriginalFileName,
    string DeclaredContentType,
    Guid DepartmentId,
    Guid? BusinessObjectId,
    Guid? WorkflowInstanceId,
    Guid? WorkflowTaskId,
    string Sensitivity);

public sealed record RequestFileDeletionCommand(string Reason);

public sealed record AttachmentListQuery(
    Guid? DepartmentId,
    Guid? BusinessObjectId,
    Guid? WorkflowInstanceId,
    Guid? WorkflowTaskId,
    string? EnvironmentCode,
    bool IncludeTestData,
    int Page,
    int PageSize);

public sealed record ValidatedFile(
    string Extension,
    string MimeType,
    long SizeBytes,
    string Sha256,
    string PreviewKind);

public sealed record StoredFile(string RelativePath, long SizeBytes, string Sha256);
