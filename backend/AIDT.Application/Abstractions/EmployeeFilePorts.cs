using AIDT.Application.Contracts;
using AIDT.Domain.Documents;
using AIDT.Domain.Identity;

namespace AIDT.Application.Abstractions;

public interface IEmployeeWorkspaceQueryService
{
    Task<MyWorkDto> GetMyWorkAsync(
        UserPrincipal user,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DepartmentDto>> ListDepartmentsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<DepartmentWorkspaceDto> GetDepartmentWorkspaceAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);
}

public interface IDepartmentCommandService
{
    Task<DepartmentDto> CreateAsync(
        UserPrincipal user,
        CreateDepartmentRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task AddMemberAsync(
        UserPrincipal user,
        Guid departmentId,
        AddDepartmentMemberRequest request,
        string traceId,
        CancellationToken cancellationToken);
}

public interface IFileCenterService
{
    Task<PagedResult<FileAttachmentDto>> ListAsync(
        UserPrincipal user,
        AttachmentListQuery query,
        CancellationToken cancellationToken);

    Task<FileAttachmentDto?> GetAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FileVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<FileAttachmentDto> UploadAsync(
        UserPrincipal user,
        UploadFileCommand command,
        string traceId,
        CancellationToken cancellationToken);

    Task<FileAttachmentDto> AddVersionAsync(
        UserPrincipal user,
        Guid attachmentId,
        UploadFileCommand command,
        string traceId,
        CancellationToken cancellationToken);

    Task<FileAttachmentDto> RequestDeletionAsync(
        UserPrincipal user,
        Guid attachmentId,
        RequestFileDeletionCommand command,
        string traceId,
        CancellationToken cancellationToken);

    Task<FileContentDto> OpenContentAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool preview,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken);
}

public interface IFileStorage
{
    Task<StoredFile> SaveAsync(
        Stream content,
        string extension,
        Guid departmentId,
        string environmentCode,
        CancellationToken cancellationToken);

    Task<Stream> OpenReadAsync(string relativePath, CancellationToken cancellationToken);

    Task DeleteAsync(string relativePath, CancellationToken cancellationToken);
}
