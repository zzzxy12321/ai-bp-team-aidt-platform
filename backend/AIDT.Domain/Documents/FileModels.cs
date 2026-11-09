using AIDT.Domain.Common;

namespace AIDT.Domain.Documents;

public enum FileRecordStatus
{
    Active,
    DeletionRequested
}

public enum FileScanStatus
{
    NotConfigured,
    PendingManualReview,
    Clean,
    Infected
}

public enum FileSensitivityLevel
{
    Internal,
    Sensitive,
    Core
}

public sealed class FileAttachment
{
    private FileAttachment(
        Guid id,
        Guid organizationId,
        Guid departmentId,
        Guid? businessObjectId,
        FileRecordStatus status,
        string environmentCode,
        bool isTestData,
        string sourceSystem,
        string originalFileName,
        string mimeType,
        long sizeBytes,
        string sha256,
        string storagePath,
        FileScanStatus scanStatus,
        FileSensitivityLevel sensitivity,
        int currentVersionNumber,
        Guid createdBy,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        Guid? deletionRequestedBy,
        DateTimeOffset? deletionRequestedAt,
        string? deletionReason)
    {
        Id = id;
        OrganizationId = organizationId;
        DepartmentId = departmentId;
        BusinessObjectId = businessObjectId;
        Status = status;
        EnvironmentCode = environmentCode;
        IsTestData = isTestData;
        SourceSystem = sourceSystem;
        OriginalFileName = originalFileName;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        Sha256 = sha256;
        StoragePath = storagePath;
        ScanStatus = scanStatus;
        Sensitivity = sensitivity;
        CurrentVersionNumber = currentVersionNumber;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        DeletionRequestedBy = deletionRequestedBy;
        DeletionRequestedAt = deletionRequestedAt;
        DeletionReason = deletionReason;
    }

    public Guid Id { get; }
    public Guid OrganizationId { get; }
    public Guid DepartmentId { get; }
    public Guid? BusinessObjectId { get; }
    public FileRecordStatus Status { get; private set; }
    public string EnvironmentCode { get; }
    public bool IsTestData { get; }
    public string SourceSystem { get; }
    public string OriginalFileName { get; private set; }
    public string MimeType { get; private set; }
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; }
    public string StoragePath { get; private set; }
    public FileScanStatus ScanStatus { get; private set; }
    public FileSensitivityLevel Sensitivity { get; private set; }
    public int CurrentVersionNumber { get; private set; }
    public Guid CreatedBy { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid? DeletionRequestedBy { get; private set; }
    public DateTimeOffset? DeletionRequestedAt { get; private set; }
    public string? DeletionReason { get; private set; }

    public static FileAttachment Create(
        Guid organizationId,
        Guid departmentId,
        Guid? businessObjectId,
        string environmentCode,
        bool isTestData,
        string sourceSystem,
        string originalFileName,
        string mimeType,
        long sizeBytes,
        string sha256,
        string storagePath,
        FileScanStatus scanStatus,
        FileSensitivityLevel sensitivity,
        Guid createdBy,
        DateTimeOffset now) =>
        new(
            Guid.NewGuid(),
            DomainChecks.RequiredId(organizationId, "组织"),
            DomainChecks.RequiredId(departmentId, "部门"),
            businessObjectId,
            FileRecordStatus.Active,
            DataEnvironment.Normalize(environmentCode),
            isTestData,
            DomainChecks.Required(sourceSystem, "来源系统", 64),
            DomainChecks.Required(Path.GetFileName(originalFileName), "原始文件名", 255),
            DomainChecks.Required(mimeType, "MIME", 128),
            RequireSize(sizeBytes),
            DomainChecks.RequiredSha256(sha256),
            DomainChecks.Required(storagePath, "存储路径", 500),
            RequireScanStatus(scanStatus),
            sensitivity,
            1,
            DomainChecks.RequiredId(createdBy, "上传人"),
            now.ToUniversalTime(),
            now.ToUniversalTime(),
            null,
            null,
            null);

    public void AddCurrentVersion(
        string originalFileName,
        string mimeType,
        long sizeBytes,
        string sha256,
        string storagePath,
        FileScanStatus scanStatus,
        FileSensitivityLevel sensitivity,
        DateTimeOffset now)
    {
        if (Status != FileRecordStatus.Active)
        {
            throw new ConflictException(ErrorCodes.Conflict, "已申请删除的文件不能新增版本。");
        }

        CurrentVersionNumber++;
        OriginalFileName = DomainChecks.Required(Path.GetFileName(originalFileName), "原始文件名", 255);
        MimeType = DomainChecks.Required(mimeType, "MIME", 128);
        SizeBytes = RequireSize(sizeBytes);
        Sha256 = DomainChecks.RequiredSha256(sha256);
        StoragePath = DomainChecks.Required(storagePath, "存储路径", 500);
        ScanStatus = RequireScanStatus(scanStatus);
        Sensitivity = sensitivity;
        UpdatedAt = now.ToUniversalTime();
    }

    private static long RequireSize(long sizeBytes) =>
        sizeBytes > 0 ? sizeBytes : throw new ValidationException("文件大小必须大于 0。");

    private static FileScanStatus RequireScanStatus(FileScanStatus scanStatus) =>
        scanStatus == FileScanStatus.Infected
            ? throw new ValidationException("已感染文件不能进入文件中心。")
            : scanStatus;

    public void RequestDeletion(Guid userId, string reason, DateTimeOffset now)
    {
        if (Status == FileRecordStatus.DeletionRequested)
        {
            return;
        }

        Status = FileRecordStatus.DeletionRequested;
        DeletionRequestedBy = userId;
        DeletionRequestedAt = now.ToUniversalTime();
        DeletionReason = DomainChecks.Required(reason, "删除原因", 500);
        UpdatedAt = now.ToUniversalTime();
    }
}

public sealed record FileVersionRecord(
    Guid Id,
    Guid AttachmentId,
    int VersionNumber,
    string OriginalFileName,
    string MimeType,
    long SizeBytes,
    string Sha256,
    string StoragePath,
    FileScanStatus ScanStatus,
    FileSensitivityLevel Sensitivity,
    Guid CreatedBy,
    DateTimeOffset CreatedAt);

public sealed record FileLinkRecord(
    Guid Id,
    Guid AttachmentId,
    string EntityType,
    Guid EntityId,
    DateTimeOffset CreatedAt)
{
    public static readonly IReadOnlySet<string> AllowedEntityTypes =
        new HashSet<string>(
            ["Department", "BusinessObject", "WorkflowInstance", "WorkflowTask", "Project", "ProjectTask", "InboxEvent"],
            StringComparer.Ordinal);
}
