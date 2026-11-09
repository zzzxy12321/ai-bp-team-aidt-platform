using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.Documents;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Documents;
using AIDT.Domain.Identity;
using AIDT.Domain.Workflows;
using AIDT.Infrastructure.Configuration;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlFileCenterService(
    AidtDbContext dbContext,
    IFileStorage storage,
    FileContentValidator validator,
    IAuditRepository auditRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    DataEnvironmentOptions environmentOptions,
    SqlProjectService projectService) : IFileCenterService
{
    public async Task<PagedResult<FileAttachmentDto>> ListAsync(
        UserPrincipal user,
        AttachmentListQuery query,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("file", AccessAction.Read);
        var departmentIds = EnsureDepartmentScopes(user);
        var attachments = dbContext.Attachments.AsNoTracking();

        if (!user.IsPlatformAdmin)
        {
            attachments = attachments.Where(item => departmentIds.Contains(item.DepartmentId));
        }
        else if (query.DepartmentId.HasValue)
        {
            attachments = attachments.Where(item => item.DepartmentId == query.DepartmentId.Value);
        }

        var currentEnvironment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        attachments = ApplyEnvironmentFilter(attachments, user, query.IncludeTestData, currentEnvironment);
        if (!string.IsNullOrWhiteSpace(query.EnvironmentCode))
        {
            var requestedEnvironment = DataEnvironment.Normalize(query.EnvironmentCode);
            if (!user.IsPlatformAdmin && requestedEnvironment != currentEnvironment)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份不能切换数据环境。");
            }

            attachments = attachments.Where(item => item.EnvironmentCode == requestedEnvironment);
        }

        if (query.BusinessObjectId.HasValue)
        {
            attachments = attachments.Where(item =>
                item.BusinessObjectId == query.BusinessObjectId
                || dbContext.AttachmentLinks.Any(link =>
                    link.AttachmentId == item.Id
                    && link.EntityType == nameof(BusinessObject)
                    && link.EntityId == query.BusinessObjectId));
        }

        if (query.WorkflowInstanceId.HasValue)
        {
            attachments = attachments.Where(item =>
                dbContext.AttachmentLinks.Any(link =>
                    link.AttachmentId == item.Id
                    && link.EntityType == nameof(WorkflowInstance)
                    && link.EntityId == query.WorkflowInstanceId));
        }

        if (query.WorkflowTaskId.HasValue)
        {
            attachments = attachments.Where(item =>
                dbContext.AttachmentLinks.Any(link =>
                    link.AttachmentId == item.Id
                    && link.EntityType == nameof(WorkflowTask)
                    && link.EntityId == query.WorkflowTaskId));
        }

        attachments = attachments.OrderByDescending(item => item.UpdatedAt).ThenByDescending(item => item.Id);
        var total = await attachments.LongCountAsync(cancellationToken);
        var entities = await attachments
            .Skip((Math.Max(1, query.Page) - 1) * Math.Clamp(query.PageSize, 1, 100))
            .Take(Math.Clamp(query.PageSize, 1, 100))
            .ToListAsync(cancellationToken);
        var items = await MapAttachmentsAsync(entities, cancellationToken);
        return new PagedResult<FileAttachmentDto>(
            items,
            Math.Max(1, query.Page),
            Math.Clamp(query.PageSize, 1, 100),
            total);
    }

    public async Task<FileAttachmentDto?> GetAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("file", AccessAction.Read);
        var entity = await GetAccessibleEntityAsync(
            user,
            attachmentId,
            includeTestData,
            "View",
            null,
            cancellationToken);
        return entity is null
            ? null
            : (await MapAttachmentsAsync([entity], cancellationToken)).Single();
    }

    public async Task<IReadOnlyList<FileVersionDto>> ListVersionsAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool includeTestData,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("file", AccessAction.Read);
        _ = await GetAccessibleEntityAsync(
                user,
                attachmentId,
                includeTestData,
                "ViewVersions",
                null,
                cancellationToken)
            ?? throw new NotFoundException("文件不存在。");
        return await dbContext.DocumentVersions.AsNoTracking()
            .Where(item => item.DocumentId == attachmentId)
            .OrderByDescending(item => item.VersionNumber)
            .Select(item => new FileVersionDto(
                item.Id.ToString("D"),
                item.VersionNumber,
                item.OriginalFileName,
                item.MimeType,
                item.SizeBytes,
                item.Sha256,
                item.VirusScanStatus,
                item.Sensitivity,
                item.CreatedBy,
                item.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<FileAttachmentDto> UploadAsync(
        UserPrincipal user,
        UploadFileCommand command,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        user.EnsureCan("file", AccessAction.Create);
        await EnsureDepartmentAllowedAsync(user, command.DepartmentId, traceId, "UploadDenied", cancellationToken);
        await EnsureAssociationsAsync(
            user,
            command.DepartmentId,
            command.BusinessObjectId,
            command.WorkflowInstanceId,
            command.WorkflowTaskId,
            cancellationToken);

        var validated = await validator.ValidateAsync(
            command.Content,
            command.OriginalFileName,
            command.DeclaredContentType,
            cancellationToken);
        command.Content.Position = 0;
        var sensitivity = ParseSensitivity(command.Sensitivity);
        var scanStatus = ScanStatusFor(sensitivity);
        var environmentCode = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        var stored = await storage.SaveAsync(
            command.Content,
            validated.Extension,
            command.DepartmentId,
            environmentCode,
            cancellationToken);
        var now = clock.UtcNow;
        var attachment = FileAttachment.Create(
            user.OrganizationId,
            command.DepartmentId,
            command.BusinessObjectId,
            environmentCode,
            environmentCode != "prod",
            "AIDT-FILE-CENTER",
            command.OriginalFileName,
            validated.MimeType,
            stored.SizeBytes,
            stored.Sha256,
            stored.RelativePath,
            scanStatus,
            sensitivity,
            user.UserId,
            now);

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var entity = ToEntity(attachment);
            dbContext.Attachments.Add(entity);
            dbContext.DocumentVersions.Add(ToVersionEntity(attachment, now));
            dbContext.AttachmentLinks.AddRange(BuildLinks(attachment.Id, command, now));
            if (command.BusinessObjectId.HasValue)
            {
                await projectService.EnsureAttachmentEvidenceRelationAsync(
                    command.BusinessObjectId.Value,
                    attachment.Id,
                    user.UserId,
                    traceId,
                    now,
                    cancellationToken);
            }
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Document",
                    "Upload",
                    nameof(FileAttachment),
                    attachment.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"{{\"departmentId\":\"{attachment.DepartmentId:D}\",\"version\":1,\"sha256\":\"{attachment.Sha256}\"}}"),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await storage.DeleteAsync(stored.RelativePath, CancellationToken.None);
            throw;
        }

        return (await MapAttachmentsAsync([ToEntity(attachment)], cancellationToken)).Single();
    }

    public async Task<FileAttachmentDto> StageLinkToEntityWithinTransactionAsync(
        UserPrincipal user,
        Guid attachmentId,
        string entityType,
        Guid entityId,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("file", AccessAction.Update);
        var normalizedEntityType = DomainChecks.RequiredIdentifier(entityType, "文件关联类型", 64);
        if (entityId == Guid.Empty)
        {
            throw new ValidationException("文件关联目标不能为空。");
        }

        var entity = await GetAccessibleEntityAsync(
                user,
                attachmentId,
                includeTestData: false,
                action: "ScenarioLink",
                traceId,
                cancellationToken)
            ?? throw new NotFoundException("场景材料文件不存在。");
        if (entity.Status != nameof(FileRecordStatus.Active))
        {
            throw new ConflictException(
                ErrorCodes.ScenarioMaterialMissing,
                "场景材料文件已申请删除，不能用于启动。");
        }

        var exists = await dbContext.AttachmentLinks.AnyAsync(
            item => item.AttachmentId == attachmentId
                && item.EntityType == normalizedEntityType
                && item.EntityId == entityId,
            cancellationToken);
        if (!exists)
        {
            dbContext.AttachmentLinks.Add(new AttachmentLinkEntity
            {
                Id = Guid.NewGuid(),
                AttachmentId = attachmentId,
                EntityType = normalizedEntityType,
                EntityId = entityId,
                CreatedAt = clock.UtcNow
            });
        }

        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Document",
                "scenario.material_linked",
                nameof(FileAttachment),
                attachmentId.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                clock.UtcNow,
                after: $"{{\"entityType\":\"{normalizedEntityType}\",\"entityId\":\"{entityId:D}\"}}"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapAttachmentsAsync([entity], cancellationToken)).Single();
    }

    public async Task<FileAttachmentDto> AddVersionAsync(
        UserPrincipal user,
        Guid attachmentId,
        UploadFileCommand command,
        string traceId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        user.EnsureCan("file", AccessAction.Update);
        var entity = await GetAccessibleEntityAsync(
                user,
                attachmentId,
                includeTestData: false,
                action: "AddVersion",
                traceId: traceId,
                cancellationToken: cancellationToken)
            ?? throw new NotFoundException("文件不存在。");
        if (entity.Status != nameof(FileRecordStatus.Active))
        {
            throw new ConflictException(ErrorCodes.Conflict, "已申请删除的文件不能新增版本。");
        }

        if (command.DepartmentId != entity.DepartmentId)
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "文件版本不能切换到其他部门。");
        }

        await EnsureAssociationsAsync(
            user,
            entity.DepartmentId,
            command.BusinessObjectId,
            command.WorkflowInstanceId,
            command.WorkflowTaskId,
            cancellationToken);

        var validated = await validator.ValidateAsync(
            command.Content,
            command.OriginalFileName,
            command.DeclaredContentType,
            cancellationToken);
        command.Content.Position = 0;
        var sensitivity = ParseSensitivity(command.Sensitivity);
        var scanStatus = ScanStatusFor(sensitivity);
        var stored = await storage.SaveAsync(
            command.Content,
            validated.Extension,
            entity.DepartmentId,
            entity.EnvironmentCode,
            cancellationToken);
        var now = clock.UtcNow;
        var nextVersion = entity.CurrentVersionNumber + 1;

        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            entity.CurrentVersionNumber = nextVersion;
            entity.OriginalFileName = command.OriginalFileName;
            entity.MimeType = validated.MimeType;
            entity.SizeBytes = stored.SizeBytes;
            entity.Sha256 = stored.Sha256;
            entity.StoragePath = stored.RelativePath;
            entity.VirusScanStatus = scanStatus.ToString();
            entity.Sensitivity = sensitivity.ToString();
            entity.UpdatedAt = now;
            dbContext.DocumentVersions.Add(new DocumentVersionEntity
            {
                Id = Guid.NewGuid(),
                DocumentId = entity.Id,
                VersionNumber = nextVersion,
                OriginalFileName = command.OriginalFileName,
                MimeType = validated.MimeType,
                SizeBytes = stored.SizeBytes,
                StoragePath = stored.RelativePath,
                Sha256 = stored.Sha256,
                VirusScanStatus = scanStatus.ToString(),
                Sensitivity = sensitivity.ToString(),
                CreatedBy = user.UserId,
                CreatedAt = now
            });
            await auditRepository.AddAsync(
                AuditEvent.Record(
                    user.UserId,
                    "Document",
                    "AddVersion",
                    nameof(FileAttachment),
                    entity.Id.ToString("D"),
                    traceId,
                    AuditOutcome.Succeeded,
                    now,
                    after: $"{{\"version\":{nextVersion},\"sha256\":\"{stored.Sha256}\"}}"),
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            await storage.DeleteAsync(stored.RelativePath, CancellationToken.None);
            throw;
        }

        return (await MapAttachmentsAsync([entity], cancellationToken)).Single();
    }

    public async Task<FileAttachmentDto> RequestDeletionAsync(
        UserPrincipal user,
        Guid attachmentId,
        RequestFileDeletionCommand command,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("file", AccessAction.Delete);
        var entity = await GetAccessibleEntityAsync(
                user,
                attachmentId,
                includeTestData: false,
                action: "RequestDeletion",
                traceId: traceId,
                cancellationToken: cancellationToken)
            ?? throw new NotFoundException("文件不存在。");
        if (entity.Status == nameof(FileRecordStatus.DeletionRequested))
        {
            return (await MapAttachmentsAsync([entity], cancellationToken)).Single();
        }

        var reason = DomainChecks.Required(command.Reason, "删除原因", 500);
        var now = clock.UtcNow;
        entity.Status = nameof(FileRecordStatus.DeletionRequested);
        entity.DeletionRequestedBy = user.UserId;
        entity.DeletionRequestedAt = now;
        entity.DeletionReason = reason;
        entity.UpdatedAt = now;
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Document",
                "RequestDeletion",
                nameof(FileAttachment),
                entity.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                now,
                reason: reason),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return (await MapAttachmentsAsync([entity], cancellationToken)).Single();
    }

    public async Task<FileContentDto> OpenContentAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool preview,
        bool includeTestData,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("file", AccessAction.Read);
        var entity = await GetAccessibleEntityAsync(
            user,
            attachmentId,
            includeTestData,
            preview ? "Preview" : "Download",
            traceId,
            cancellationToken) ?? throw new NotFoundException("文件不存在。");
        var sensitivity = ParseSensitivity(entity.Sensitivity);
        if (!user.IsPlatformAdmin
            && sensitivity != FileSensitivityLevel.Internal
            && entity.VirusScanStatus != nameof(FileScanStatus.Clean))
        {
            await RecordDeniedAsync(user, entity, preview ? "PreviewDenied" : "DownloadDenied", traceId, cancellationToken);
            throw new PermissionException(ErrorCodes.FileScanRequired, "敏感文件尚未完成病毒扫描和人工放行。");
        }

        var previewKind = PreviewKind(entity.MimeType);
        if (preview && previewKind == "unsupported")
        {
            throw new UnsupportedMediaTypeException("Office 文件不支持内嵌预览，请下载后查看。");
        }

        var stream = await storage.OpenReadAsync(entity.StoragePath, cancellationToken);
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Document",
                preview ? "Preview" : "Download",
                nameof(FileAttachment),
                entity.Id.ToString("D"),
                traceId,
                AuditOutcome.Succeeded,
                clock.UtcNow),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new FileContentDto(stream, entity.MimeType, entity.OriginalFileName, previewKind);
    }

    private async Task<AttachmentEntity?> GetAccessibleEntityAsync(
        UserPrincipal user,
        Guid attachmentId,
        bool includeTestData,
        string action,
        string? traceId,
        CancellationToken cancellationToken)
    {
        var entity = await dbContext.Attachments.SingleOrDefaultAsync(
            item => item.Id == attachmentId,
            cancellationToken);
        if (entity is null || entity.OrganizationId != user.OrganizationId)
        {
            return null;
        }

        if (!user.IsPlatformAdmin && !user.DepartmentIds.Contains(entity.DepartmentId))
        {
            await RecordDeniedAsync(user, entity, action, traceId ?? "unknown", cancellationToken);
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份无权访问该文件。");
        }

        var currentEnvironment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        if (!CanIncludeEnvironment(user, includeTestData) && entity.EnvironmentCode != currentEnvironment)
        {
            await RecordDeniedAsync(user, entity, $"{action}EnvironmentDenied", traceId ?? "unknown", cancellationToken);
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份无权访问该环境的数据。");
        }

        return entity;
    }

    private async Task EnsureDepartmentAllowedAsync(
        UserPrincipal user,
        Guid departmentId,
        string traceId,
        string action,
        CancellationToken cancellationToken)
    {
        var departmentExists = await dbContext.Departments.AsNoTracking().AnyAsync(
            item => item.Id == departmentId
                && item.OrganizationId == user.OrganizationId
                && item.IsActive,
            cancellationToken);
        if (!departmentExists)
        {
            throw new NotFoundException("部门不存在或已停用。");
        }

        if (!user.IsPlatformAdmin && !user.DepartmentIds.Contains(departmentId))
        {
            var entity = new AttachmentEntity
            {
                Id = Guid.Empty,
                DepartmentId = departmentId,
                OriginalFileName = "unknown",
                EnvironmentCode = environmentOptions.CurrentEnvironmentCode
            };
            await RecordDeniedAsync(user, entity, action, traceId, cancellationToken);
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份没有该部门的数据范围。");
        }
    }

    private async Task RecordDeniedAsync(
        UserPrincipal user,
        AttachmentEntity entity,
        string action,
        string traceId,
        CancellationToken cancellationToken)
    {
        await auditRepository.AddAsync(
            AuditEvent.Record(
                user.UserId,
                "Document",
                action,
                nameof(FileAttachment),
                entity.Id == Guid.Empty ? entity.DepartmentId.ToString("D") : entity.Id.ToString("D"),
                traceId,
                AuditOutcome.Denied,
                clock.UtcNow,
                reason: "数据范围或文件安全策略拒绝。"),
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAssociationsAsync(
        UserPrincipal user,
        Guid departmentId,
        Guid? businessObjectId,
        Guid? workflowInstanceId,
        Guid? workflowTaskId,
        CancellationToken cancellationToken)
    {
        var environment = DataEnvironment.Normalize(environmentOptions.CurrentEnvironmentCode);
        Guid? objectDepartmentId = null;
        Guid? instanceDepartmentId = null;
        Guid? taskDepartmentId = null;
        Guid? instanceObjectId = null;
        Guid? taskObjectId = null;
        if (businessObjectId.HasValue)
        {
            var businessObject = await dbContext.BusinessObjects.AsNoTracking()
                .Where(item => item.Id == businessObjectId.Value
                    && item.OrganizationId == user.OrganizationId
                    && !item.IsDeleted)
                .Select(item => new { item.DepartmentId, item.EnvironmentCode })
                .SingleOrDefaultAsync(cancellationToken);
            if (businessObject is null)
            {
                throw new NotFoundException("业务对象不存在。");
            }

            if (businessObject.EnvironmentCode != environment)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "业务对象与文件环境不一致。");
            }

            objectDepartmentId = businessObject.DepartmentId;
        }

        if (workflowInstanceId.HasValue)
        {
            var instance = await (
                from workflowInstance in dbContext.WorkflowInstances.AsNoTracking()
                join businessObject in dbContext.BusinessObjects.AsNoTracking()
                    on workflowInstance.BusinessObjectId equals businessObject.Id
                where workflowInstance.Id == workflowInstanceId.Value
                    && businessObject.OrganizationId == user.OrganizationId
                select new
                {
                    workflowInstance.DepartmentId,
                    workflowInstance.EnvironmentCode,
                    workflowInstance.BusinessObjectId
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (instance is null)
            {
                throw new NotFoundException("流程实例不存在。");
            }

            if (instance.EnvironmentCode != environment
                || businessObjectId.HasValue && instance.BusinessObjectId != businessObjectId.Value)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "流程实例与文件环境或对象不一致。");
            }

            instanceDepartmentId = instance.DepartmentId;
            instanceObjectId = instance.BusinessObjectId;
        }

        if (workflowTaskId.HasValue)
        {
            var task = await (
                from workflowTask in dbContext.WorkflowTasks.AsNoTracking()
                join workflowInstance in dbContext.WorkflowInstances.AsNoTracking()
                    on workflowTask.WorkflowInstanceId equals workflowInstance.Id
                join businessObject in dbContext.BusinessObjects.AsNoTracking()
                    on workflowInstance.BusinessObjectId equals businessObject.Id
                where workflowTask.Id == workflowTaskId.Value
                    && businessObject.OrganizationId == user.OrganizationId
                select new
                {
                    workflowTask.DepartmentId,
                    workflowTask.EnvironmentCode,
                    workflowTask.WorkflowInstanceId,
                    workflowInstance.BusinessObjectId
                })
                .SingleOrDefaultAsync(cancellationToken);
            if (task is null)
            {
                throw new NotFoundException("流程任务不存在。");
            }

            if (task.EnvironmentCode != environment
                || workflowInstanceId.HasValue && task.WorkflowInstanceId != workflowInstanceId.Value
                || businessObjectId.HasValue && task.BusinessObjectId != businessObjectId.Value)
            {
                throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "流程任务与文件环境、流程或对象不一致。");
            }

            taskDepartmentId = task.DepartmentId;
            taskObjectId = task.BusinessObjectId;
        }

        if (instanceObjectId.HasValue
            && taskObjectId.HasValue
            && instanceObjectId != taskObjectId)
        {
            throw new ValidationException("流程实例与流程任务引用的业务对象不一致。");
        }

        var expectedDepartmentId = taskDepartmentId ?? instanceDepartmentId ?? objectDepartmentId;
        if (expectedDepartmentId.HasValue && expectedDepartmentId.Value != departmentId)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "文件部门必须与任务、流程实例或业务对象的最新责任部门一致。");
        }

        if (!user.IsPlatformAdmin)
        {
            user.EnsureDepartmentAccess(departmentId);
        }
    }

    private async Task<List<FileAttachmentDto>> MapAttachmentsAsync(
        IReadOnlyCollection<AttachmentEntity> entities,
        CancellationToken cancellationToken)
    {
        var ids = entities.Select(item => item.Id).ToArray();
        var links = await dbContext.AttachmentLinks.AsNoTracking()
            .Where(item => ids.Contains(item.AttachmentId))
            .OrderBy(item => item.EntityType)
            .ToListAsync(cancellationToken);
        var linksByAttachment = links.GroupBy(item => item.AttachmentId)
            .ToDictionary(group => group.Key, group => group.ToList());
        return entities.Select(entity => new FileAttachmentDto(
            entity.Id.ToString("D"),
            entity.DepartmentId.ToString("D"),
            entity.OriginalFileName,
            entity.MimeType,
            entity.SizeBytes,
            entity.Sha256,
            entity.VirusScanStatus,
            entity.Sensitivity,
            entity.Status,
            entity.EnvironmentCode,
            entity.IsTestData,
            entity.SourceSystem,
            entity.CurrentVersionNumber,
            PreviewKind(entity.MimeType),
            (linksByAttachment.GetValueOrDefault(entity.Id) ?? [])
                .Select(link => new FileLinkDto(link.EntityType, link.EntityId.ToString("D")))
                .ToList(),
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.DeletionRequestedBy,
            entity.DeletionRequestedAt,
            entity.DeletionReason)).ToList();
    }

    private static AttachmentEntity ToEntity(FileAttachment attachment) =>
        new()
        {
            Id = attachment.Id,
            OrganizationId = attachment.OrganizationId,
            DepartmentId = attachment.DepartmentId,
            BusinessObjectId = attachment.BusinessObjectId,
            StoragePath = attachment.StoragePath,
            OriginalFileName = attachment.OriginalFileName,
            MimeType = attachment.MimeType,
            SizeBytes = attachment.SizeBytes,
            Sha256 = attachment.Sha256,
            VirusScanStatus = attachment.ScanStatus.ToString(),
            Sensitivity = attachment.Sensitivity.ToString(),
            Status = attachment.Status.ToString(),
            EnvironmentCode = attachment.EnvironmentCode,
            IsTestData = attachment.IsTestData,
            SourceSystem = attachment.SourceSystem,
            CurrentVersionNumber = attachment.CurrentVersionNumber,
            CreatedBy = attachment.CreatedBy,
            CreatedAt = attachment.CreatedAt,
            UpdatedAt = attachment.UpdatedAt,
            DeletionRequestedBy = attachment.DeletionRequestedBy,
            DeletionRequestedAt = attachment.DeletionRequestedAt,
            DeletionReason = attachment.DeletionReason
        };

    private static DocumentVersionEntity ToVersionEntity(FileAttachment attachment, DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid(),
            DocumentId = attachment.Id,
            VersionNumber = attachment.CurrentVersionNumber,
            OriginalFileName = attachment.OriginalFileName,
            MimeType = attachment.MimeType,
            SizeBytes = attachment.SizeBytes,
            StoragePath = attachment.StoragePath,
            Sha256 = attachment.Sha256,
            VirusScanStatus = attachment.ScanStatus.ToString(),
            Sensitivity = attachment.Sensitivity.ToString(),
            CreatedBy = attachment.CreatedBy,
            CreatedAt = now
        };

    private static IEnumerable<AttachmentLinkEntity> BuildLinks(
        Guid attachmentId,
        UploadFileCommand command,
        DateTimeOffset now)
    {
        yield return new AttachmentLinkEntity
        {
            Id = Guid.NewGuid(),
            AttachmentId = attachmentId,
            EntityType = nameof(Department),
            EntityId = command.DepartmentId,
            CreatedAt = now
        };
        if (command.BusinessObjectId.HasValue)
        {
            yield return Link(command.BusinessObjectId.Value, nameof(BusinessObject), now);
        }
        if (command.WorkflowInstanceId.HasValue)
        {
            yield return Link(command.WorkflowInstanceId.Value, nameof(WorkflowInstance), now);
        }
        if (command.WorkflowTaskId.HasValue)
        {
            yield return Link(command.WorkflowTaskId.Value, nameof(WorkflowTask), now);
        }

        AttachmentLinkEntity Link(Guid entityId, string entityType, DateTimeOffset createdAt) =>
            new()
            {
                Id = Guid.NewGuid(),
                AttachmentId = attachmentId,
                EntityType = entityType,
                EntityId = entityId,
                CreatedAt = createdAt
            };
    }

    private static IQueryable<AttachmentEntity> ApplyEnvironmentFilter(
        IQueryable<AttachmentEntity> query,
        UserPrincipal user,
        bool includeTestData,
        string currentEnvironment)
    {
        if (user.IsPlatformAdmin && includeTestData)
        {
            return query;
        }

        return query.Where(item => item.EnvironmentCode == currentEnvironment
            && (currentEnvironment != "prod" || !item.IsTestData));
    }

    private static bool CanIncludeEnvironment(UserPrincipal user, bool includeTestData) =>
        user.IsPlatformAdmin && includeTestData;

    private static IReadOnlySet<Guid> EnsureDepartmentScopes(UserPrincipal user)
    {
        if (user.IsPlatformAdmin)
        {
            return new HashSet<Guid>();
        }

        var ids = user.DepartmentIds;
        if (ids.Count == 0)
        {
            throw new PermissionException(ErrorCodes.DepartmentScopeDenied, "当前身份没有部门数据范围。");
        }

        return ids;
    }

    private static FileSensitivityLevel ParseSensitivity(string value)
    {
        if (!Enum.TryParse<FileSensitivityLevel>(value, true, out var sensitivity)
            || !Enum.IsDefined(sensitivity))
        {
            throw new ValidationException("文件保密等级必须是 Internal、Sensitive 或 Core。");
        }

        return sensitivity;
    }

    private static FileScanStatus ScanStatusFor(FileSensitivityLevel sensitivity) =>
        sensitivity == FileSensitivityLevel.Internal
            ? FileScanStatus.NotConfigured
            : FileScanStatus.PendingManualReview;

    private static string PreviewKind(string mimeType) =>
        mimeType switch
        {
            "application/pdf" => "pdf",
            "image/png" or "image/jpeg" => "image",
            "text/plain" or "text/csv" or "application/csv" or "application/vnd.ms-excel" => "text",
            _ => "unsupported"
        };
}
