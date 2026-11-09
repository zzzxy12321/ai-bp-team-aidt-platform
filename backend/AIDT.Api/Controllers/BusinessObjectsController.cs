using AIDT.Api.Contracts;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using AIDT.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIDT.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Authorize]
public sealed class BusinessObjectsController(
    ICurrentUserAccessor currentUserAccessor,
    IWorkspaceQueryService queryService,
    IObjectModelService objectModelService,
    CreateBusinessObjectHandler createBusinessObjectHandler,
    UpdateBusinessObjectHandler updateBusinessObjectHandler) : ControllerBase
{
    [HttpGet("object-types")]
    public async Task<ActionResult<ApiResponse<PagedResult<ObjectTypeDto>>>> GetObjectTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.ListObjectTypesAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("object-types")]
    public async Task<ActionResult<ApiResponse<ObjectTypeDto>>> CreateObjectType(
        [FromBody] CreateObjectTypeRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await objectModelService.CreateAsync(
            currentUserAccessor.GetRequiredUser(),
            request,
            HttpContext.TraceIdentifier,
            idempotencyKey,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("object-types/published")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ObjectTypeDto>>>> GetPublishedObjectTypes(
        CancellationToken cancellationToken)
    {
        var result = await queryService.ListPublishedObjectTypesAsync(
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("object-types/{id:guid}/draft")]
    public async Task<ActionResult<ApiResponse<ObjectTypeDto>>> UpdateObjectTypeDraft(
        Guid id,
        [FromBody] UpdateObjectTypeDraftRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await objectModelService.UpdateDraftAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            idempotencyKey,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("object-types/{id:guid}/actions/validate")]
    public async Task<ActionResult<ApiResponse<ObjectTypeValidationDto>>> ValidateObjectType(
        Guid id,
        [FromBody] ObjectTypeActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await objectModelService.ValidateAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            HttpContext.TraceIdentifier,
            idempotencyKey,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPost("object-types/{id:guid}/actions/publish")]
    public Task<ActionResult<ApiResponse<ObjectTypeDto>>> PublishObjectType(
        Guid id,
        [FromBody] ObjectTypeActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(
            objectModelService.PublishAsync,
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            cancellationToken);

    [HttpPost("object-types/{id:guid}/actions/retire")]
    public Task<ActionResult<ApiResponse<ObjectTypeDto>>> RetireObjectType(
        Guid id,
        [FromBody] ObjectTypeActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(
            objectModelService.RetireAsync,
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            cancellationToken);

    [HttpPost("object-types/{id:guid}/actions/restore")]
    public Task<ActionResult<ApiResponse<ObjectTypeDto>>> RestoreObjectType(
        Guid id,
        [FromBody] ObjectTypeActionRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken) =>
        ExecuteActionAsync(
            objectModelService.RestoreAsync,
            currentUserAccessor.GetRequiredUser(),
            id,
            request,
            idempotencyKey,
            cancellationToken);

    [HttpGet("object-types/{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ObjectTypeVersionDto>>>> GetObjectTypeVersions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await objectModelService.ListVersionsAsync(
            id,
            currentUserAccessor.GetRequiredUser(),
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("business-objects")]
    public async Task<ActionResult<ApiResponse<PagedResult<BusinessObjectDto>>>> GetBusinessObjects(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeTestData = false,
        CancellationToken cancellationToken = default)
    {
        var result = await queryService.ListBusinessObjectsAsync(
            currentUserAccessor.GetRequiredUser(),
            Math.Max(1, page),
            Math.Clamp(pageSize, 1, 100),
            includeTestData,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpGet("business-objects/{id:guid}")]
    public async Task<ActionResult<ApiResponse<BusinessObjectDto>>> GetBusinessObject(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var businessObject = await queryService.GetBusinessObjectAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return businessObject is null
            ? NotFound(ApiResponse.Error("RESOURCE_NOT_FOUND", "业务对象不存在。", HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(businessObject, HttpContext.TraceIdentifier));
    }

    [HttpGet("business-objects/{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BusinessObjectVersionDto>>>> GetBusinessObjectVersions(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var versions = await queryService.GetBusinessObjectVersionsAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return versions is null
            ? NotFound(ApiResponse.Error(
                "RESOURCE_NOT_FOUND",
                "业务对象不存在。",
                HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(versions, HttpContext.TraceIdentifier));
    }

    [HttpGet("business-objects/{id:guid}/context")]
    public async Task<ActionResult<ApiResponse<BusinessObjectContextDto>>> GetBusinessObjectContext(
        Guid id,
        [FromQuery] bool includeTestData,
        CancellationToken cancellationToken)
    {
        var context = await queryService.GetBusinessObjectContextAsync(
            currentUserAccessor.GetRequiredUser(),
            id,
            includeTestData,
            HttpContext.TraceIdentifier,
            cancellationToken);
        return context is null
            ? NotFound(ApiResponse.Error(
                "RESOURCE_NOT_FOUND",
                "业务对象不存在。",
                HttpContext.TraceIdentifier))
            : Ok(ApiResponse.Ok(context, HttpContext.TraceIdentifier));
    }

    [HttpGet("business-objects/field-options/users")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ObjectFieldUserOptionDto>>>> GetBusinessObjectUserOptions(
        [FromQuery] string? search,
        [FromQuery] Guid? departmentId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var users = await queryService.ListObjectFieldUsersAsync(
            currentUserAccessor.GetRequiredUser(),
            search,
            departmentId,
            limit,
            cancellationToken);
        return Ok(ApiResponse.Ok(users, HttpContext.TraceIdentifier));
    }

    [HttpPost("business-objects")]
    public async Task<ActionResult<ApiResponse<BusinessObjectDto>>> CreateBusinessObject(
        [FromBody] CreateBusinessObjectRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await createBusinessObjectHandler.HandleAsync(
            request,
            HttpContext.TraceIdentifier,
            idempotencyKey,
            cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    [HttpPut("business-objects/{id:guid}")]
    public async Task<ActionResult<ApiResponse<BusinessObjectDto>>> UpdateBusinessObject(
        Guid id,
        [FromBody] UpdateBusinessObjectRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await updateBusinessObjectHandler.HandleAsync(
            id,
            request,
            HttpContext.TraceIdentifier,
            idempotencyKey,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }

    private async Task<ActionResult<ApiResponse<ObjectTypeDto>>> ExecuteActionAsync(
        Func<UserPrincipal, Guid, ObjectTypeActionRequest, string, string, CancellationToken, Task<ObjectTypeDto>> action,
        UserPrincipal user,
        Guid id,
        ObjectTypeActionRequest request,
        string idempotencyKey,
        CancellationToken cancellationToken)
    {
        var result = await action(
            user,
            id,
            request,
            HttpContext.TraceIdentifier,
            idempotencyKey,
            cancellationToken);
        return Ok(ApiResponse.Ok(result, HttpContext.TraceIdentifier));
    }
}
