using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;

namespace AIDT.Infrastructure.Services;

internal sealed class NotReadyWorkflowTaskFormService : IWorkflowTaskFormService
{
    public Task<WorkflowTaskFormDto> GetAsync(
        UserPrincipal user,
        Guid taskId,
        CancellationToken cancellationToken) => throw NotReady.Error();

    public Task<WorkflowTaskDto> SaveDraftAsync(
        UserPrincipal user,
        Guid taskId,
        SaveWorkflowTaskFormDraftRequest request,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyWorkflowTaskFormValidator : IWorkflowTaskFormValidator
{
    public Task<WorkflowTaskFormValidationDto> ValidateAsync(
        UserPrincipal user,
        WorkflowTask task,
        string? formDataJson,
        bool enforceRequired,
        CancellationToken cancellationToken) => throw NotReady.Error();
}

internal sealed class NotReadyWorkflowTaskAiReviewService : IWorkflowTaskAiReviewService
{
    public Task<WorkflowTaskAiReviewResultDto> ReviewAsync(
        UserPrincipal user,
        Guid taskId,
        string idempotencyKey,
        string traceId,
        CancellationToken cancellationToken) => throw NotReady.Error();
}
