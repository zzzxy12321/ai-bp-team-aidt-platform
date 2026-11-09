using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;

namespace AIDT.Infrastructure.Services;

internal sealed class NotReadyWorkbenchService : IWorkbenchService
{
    public Task<DepartmentWorkbenchDto> ResolveAsync(
        UserPrincipal user,
        Guid departmentId,
        bool includeTestData,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<IReadOnlyList<WorkbenchTemplateDto>> ListTemplatesAsync(
        UserPrincipal user,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<WorkbenchTemplateDto> GetTemplateAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<WorkbenchTemplateDto> CreateTemplateAsync(
        UserPrincipal user,
        CreateWorkbenchTemplateRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<WorkbenchTemplateDto> CopyTemplateAsync(
        UserPrincipal user,
        Guid templateId,
        CopyWorkbenchTemplateRequest request,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<WorkbenchTemplateDto> UpdateDraftAsync(
        UserPrincipal user,
        Guid templateId,
        UpdateWorkbenchTemplateDraftRequest request,
        string traceId,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<WorkbenchTemplateValidationDto> ValidateAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<WorkbenchTemplateDto> PublishAsync(
        UserPrincipal user,
        Guid templateId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<WorkbenchTemplateDto> SetStatusAsync(
        UserPrincipal user,
        Guid templateId,
        bool retire,
        string traceId,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");

    public Task<IReadOnlyList<WorkbenchTemplateVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid templateId,
        CancellationToken cancellationToken) =>
        throw new ServiceUnavailableException(
            ErrorCodes.PersistenceNotReady,
            "工作台模板服务尚未启用 SQL 持久化。");
}
