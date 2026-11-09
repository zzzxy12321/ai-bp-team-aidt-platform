using AIDT.Application.Contracts;
using AIDT.Domain.Identity;

namespace AIDT.Application.Abstractions;

public interface IWorkbenchService
{
    Task<DepartmentWorkbenchDto> ResolveAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkbenchTemplateDto>> ListTemplatesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken);

    Task<WorkbenchTemplateDto> GetTemplateAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken);

    Task<WorkbenchTemplateDto> CreateTemplateAsync(
        UserPrincipal user,
        CreateWorkbenchTemplateRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkbenchTemplateDto> CopyTemplateAsync(
        UserPrincipal user,
        Guid templateId,
        CopyWorkbenchTemplateRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkbenchTemplateDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid templateId,
        UpdateWorkbenchTemplateDraftRequest request,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkbenchTemplateValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken);

    Task<WorkbenchTemplateDto> PublishAsync(
        UserPrincipal user,
        Guid templateId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken);

    Task<WorkbenchTemplateDto> SetStatusAsync(
        UserPrincipal user,
        Guid templateId,
        bool retire,
        string traceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkbenchTemplateVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken);
}
