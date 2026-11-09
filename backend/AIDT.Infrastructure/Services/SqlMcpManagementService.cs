using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using AIDT.Domain.Identity;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed class SqlMcpManagementService(
    AidtDbContext dbContext,
    IClock clock,
    IMcpToolCatalog toolCatalog,
    IMcpAuditService auditService) : IMcpManagementService
{
    public async Task<McpManagementSnapshotDto> GetSnapshotAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("mcp", AccessAction.Read);
        var now = clock.UtcNow;
        var clients = await dbContext.McpClients
            .AsNoTracking()
            .OrderByDescending(item => item.CreatedAt)
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
        var clientIds = clients.Select(item => item.ClientId).ToList();
        var tokens = await dbContext.McpAccessTokens
            .AsNoTracking()
            .Where(item => clientIds.Contains(item.ClientId) && item.OrganizationId == user.OrganizationId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
        var userIds = tokens.Select(item => item.UserId).Distinct().ToList();
        var users = await dbContext.Users
            .AsNoTracking()
            .Where(item => userIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.DisplayName, cancellationToken);
        var agentProfileIds = clients
            .Select(item => item.AgentProfileId)
            .Concat(tokens.Select(item => item.AgentProfileId))
            .Where(item => item.HasValue)
            .Select(item => item!.Value)
            .Distinct()
            .ToArray();
        var agentProfiles = await dbContext.AgentProfiles.AsNoTracking()
            .Where(item => agentProfileIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var activeTokenCounts = tokens
            .Where(item => item.TokenKind == "Access" && item.RevokedAt == null && item.ExpiresAt > now)
            .GroupBy(item => item.ClientId)
            .ToDictionary(group => group.Key, group => group.Count());
        var confirmationEntities = await dbContext.McpHumanConfirmations
            .AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && item.Status != "Consumed"
                && item.ExpiresAt > now)
            .OrderByDescending(item => item.CreatedAt)
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);
        var confirmations = confirmationEntities
            .Select(SqlMcpHumanConfirmationService.Map)
            .ToList();
        var audits = await dbContext.AuditEvents
            .AsNoTracking()
            .Where(item => item.Module == "MCP")
            .OrderByDescending(item => item.OccurredAt)
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync(cancellationToken);

        return new McpManagementSnapshotDto(
            toolCatalog.List(),
            clients.Select(item => new McpClientSummaryDto(
                item.ClientId,
                item.ClientName,
                item.IsActive,
                activeTokenCounts.GetValueOrDefault(item.ClientId),
                item.CreatedAt,
                item.LastUsedAt,
                item.AgentProfileId?.ToString("D"),
                item.AgentProfileId.HasValue
                    ? agentProfiles.GetValueOrDefault(item.AgentProfileId.Value)?.Name
                    : null,
                item.AgentProfileId.HasValue
                    && agentProfiles.GetValueOrDefault(item.AgentProfileId.Value) is { } agent
                    ? agent.LifecycleStatus == "Published"
                        && agent.IsActive
                        && agent.CurrentPublishedVersionId.HasValue
                    : null)).ToList(),
            tokens.Select(item => new McpTokenSummaryDto(
                item.Id,
                item.ClientId,
                clients.FirstOrDefault(client => client.ClientId == item.ClientId)?.ClientName ?? item.ClientId,
                users.GetValueOrDefault(item.UserId, "未知用户"),
                item.TokenKind,
                McpSecurity.ReadScopes(item.GrantedScopesJson),
                McpSecurity.ReadDepartmentIds(item.DepartmentIdsJson),
                item.CreatedAt,
                item.ExpiresAt,
                item.RevokedAt,
                item.LastUsedAt,
                item.AgentProfileId?.ToString("D"),
                item.AgentProfileId.HasValue
                    ? agentProfiles.GetValueOrDefault(item.AgentProfileId.Value)?.Name
                    : null,
                item.AgentProfileId.HasValue
                    && agentProfiles.GetValueOrDefault(item.AgentProfileId.Value) is { } tokenAgent
                    ? tokenAgent.LifecycleStatus == "Published"
                        && tokenAgent.IsActive
                        && tokenAgent.CurrentPublishedVersionId.HasValue
                    : null,
                item.RevokedBy?.ToString("D"),
                item.RevocationReason)).ToList(),
            confirmations,
            audits.Select(item => new McpInvocationSummaryDto(
                item.Id,
                ParseAuditValue(item.AfterJson, "ToolOrResource") ?? item.EntityId,
                item.Action,
                ParseAuditValue(item.AfterJson, "ClientId") ?? "unknown",
                item.ActorId,
                item.TraceId,
                item.Outcome,
                item.Reason,
                ParseAuditInt(item.AfterJson, "DurationMs"),
                item.OccurredAt)).ToList());
    }

    public async Task<IReadOnlyList<McpHumanConfirmationDto>> ListOwnConfirmationsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureAuthenticated();
        user.EnsureOrganizationAccess(user.OrganizationId);
        var now = clock.UtcNow;
        var entities = await dbContext.McpHumanConfirmations
            .AsNoTracking()
            .Where(item =>
                item.UserId == user.UserId
                && item.OrganizationId == user.OrganizationId
                && item.Status != "Consumed"
                && item.ExpiresAt > now)
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken);
        return entities.Select(SqlMcpHumanConfirmationService.Map).ToList();
    }

    public async Task<IReadOnlyList<McpHumanConfirmationViewDto>> ListOwnConfirmationViewsAsync(
        UserPrincipal user,
        CancellationToken cancellationToken)
    {
        user.EnsureAuthenticated();
        user.EnsureOrganizationAccess(user.OrganizationId);
        var now = clock.UtcNow;
        var entities = await dbContext.McpHumanConfirmations
            .AsNoTracking()
            .Where(item => item.UserId == user.UserId && item.OrganizationId == user.OrganizationId)
            .OrderByDescending(item => item.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
        var result = new List<McpHumanConfirmationViewDto>(entities.Count);
        foreach (var entity in entities)
        {
            var tool = toolCatalog.Find(entity.ToolName);
            var (resourceDisplayName, projectName) = await ResolveResourceAsync(entity, cancellationToken);
            result.Add(new McpHumanConfirmationViewDto(
                entity.Id,
                tool?.Title ?? BusinessActionTitle(entity.ToolName),
                entity.Status == "Pending" && entity.ExpiresAt <= now
                    ? "Expired"
                    : entity.Status == "Consumed"
                        ? "Confirmed"
                        : entity.Status,
                entity.Summary,
                resourceDisplayName,
                projectName,
                BuildWillExecute(entity.ToolName, entity.Summary),
                "不会执行付款、质量放行、配方变更、DCS/PLC 控制、解除安全联锁、合同承诺或客户重大赔付。",
                entity.DecisionComment,
                entity.ExpiresAt,
                entity.CreatedAt,
                new McpConfirmationTechnicalInfoDto(
                    entity.ClientId,
                    entity.ResourceType,
                    entity.ResourceId,
                    entity.RequestHash)));
        }

        return result;
    }

    public async Task RevokeClientAsync(
        UserPrincipal user,
        string clientId,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("mcp", AccessAction.Manage);
        var client = await dbContext.McpClients
            .SingleOrDefaultAsync(item => item.ClientId == clientId, cancellationToken)
            ?? throw new NotFoundException("MCP 客户端不存在。");
        client.IsActive = false;
        var now = clock.UtcNow;
        await dbContext.McpAccessTokens
            .Where(item => item.ClientId == clientId && item.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.RevokedAt, now),
                cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.RecordSecurityEventAsync(
            user.UserId,
            "client.revoked",
            "McpClient",
            clientId,
            traceId,
            "Succeeded",
            null,
            null,
            cancellationToken);
    }

    public async Task RevokeTokenAsync(
        UserPrincipal user,
        Guid tokenId,
        string traceId,
        CancellationToken cancellationToken)
    {
        user.EnsureCan("mcp", AccessAction.Manage);
        var token = await dbContext.McpAccessTokens
            .SingleOrDefaultAsync(
                item => item.Id == tokenId && item.OrganizationId == user.OrganizationId,
                cancellationToken)
            ?? throw new NotFoundException("MCP 令牌不存在。");
        token.RevokedAt ??= clock.UtcNow;
        token.RevokedBy ??= user.UserId;
        token.RevocationReason ??= "MANUAL_REVOKE";
        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.RecordSecurityEventAsync(
            user.UserId,
            "token.revoked",
            "McpAccessToken",
            token.Id.ToString("D"),
            traceId,
            "Succeeded",
            null,
            $"{{\"clientId\":\"{token.ClientId}\",\"kind\":\"{token.TokenKind}\"}}",
            cancellationToken);
    }

    private static string? ParseAuditValue(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(propertyName, out var value)
                ? value.ToString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<(string ResourceDisplayName, string? ProjectName)> ResolveResourceAsync(
        McpHumanConfirmationEntity entity,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(entity.ResourceId, out var resourceId))
        {
            return ($"{entity.ResourceType} · {entity.ResourceId}", null);
        }

        switch (entity.ResourceType)
        {
            case "EventInbox":
                {
                    var title = await dbContext.EventInboxes.AsNoTracking()
                        .Where(item => item.Id == resourceId)
                        .Select(item => item.Title)
                        .SingleOrDefaultAsync(cancellationToken);
                    return (title ?? "收件箱事件", null);
                }
            case "Project":
                {
                    var project = await dbContext.BusinessObjects.AsNoTracking()
                        .Where(item => item.Id == resourceId)
                        .Select(item => new { item.Title, item.ObjectCode })
                        .SingleOrDefaultAsync(cancellationToken);
                    var name = project is null ? null : project.Title ?? project.ObjectCode;
                    return (name ?? "项目", name);
                }
            case "ProjectTask":
                {
                    var task = await dbContext.ProjectTasks.AsNoTracking()
                        .Where(item => item.Id == resourceId)
                        .Select(item => new { item.Title, item.ProjectId })
                        .SingleOrDefaultAsync(cancellationToken);
                    if (task is null) return ("项目任务", null);
                    var projectName = await dbContext.BusinessObjects.AsNoTracking()
                        .Where(item => item.Id == task.ProjectId)
                        .Select(item => item.Title ?? item.ObjectCode)
                        .SingleOrDefaultAsync(cancellationToken);
                    return (task.Title, projectName);
                }
            case "WorkflowTask":
                {
                    var task = await dbContext.WorkflowTasks.AsNoTracking()
                        .Where(item => item.Id == resourceId)
                        .Select(item => new { item.NodeCode, item.WorkflowInstanceId })
                        .SingleOrDefaultAsync(cancellationToken);
                    if (task is null) return ("流程任务", null);
                    var projectName = await (
                        from instance in dbContext.WorkflowInstances.AsNoTracking()
                        join businessObject in dbContext.BusinessObjects.AsNoTracking()
                            on instance.BusinessObjectId equals businessObject.Id
                        where instance.Id == task.WorkflowInstanceId
                        select businessObject.Title ?? businessObject.ObjectCode)
                        .SingleOrDefaultAsync(cancellationToken);
                    return ($"流程任务 · {task.NodeCode}", projectName);
                }
            case "FileAttachment":
                {
                    var fileName = await dbContext.Attachments.AsNoTracking()
                        .Where(item => item.Id == resourceId)
                        .Select(item => item.OriginalFileName)
                        .SingleOrDefaultAsync(cancellationToken);
                    return (fileName ?? "文件", null);
                }
            default:
                return ($"{entity.ResourceType} · {entity.ResourceId}", null);
        }
    }

    private static string BusinessActionTitle(string toolName) =>
        toolName switch
        {
            "aidt_decide_workflow_task" => "处理流程任务",
            "aidt_transfer_workflow_task" => "转交流程任务",
            "aidt_confirm_event" => "确认企业事项",
            "aidt_change_project_status" => "变更项目状态",
            "aidt_download_file" => "下载受控文件",
            "aidt_upload_file" => "上传受控文件",
            _ => "确认业务动作"
        };

    private static string BuildWillExecute(string toolName, string summary) =>
        $"{BusinessActionTitle(toolName)}；{summary.TrimEnd('。')}。确认后写入真人身份和审计记录。";

    private static int ParseAuditInt(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty(propertyName, out var value)
                && value.TryGetInt32(out var number)
                ? number
                : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }
}
