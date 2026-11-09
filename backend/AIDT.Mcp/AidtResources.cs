using System.ComponentModel;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Domain.Common;
using ModelContextProtocol.Server;

namespace AIDT.Mcp;

[McpServerResourceType]
public sealed class AidtResources(
    McpToolRuntime runtime,
    IProjectService projectService,
    IEventInboxService inboxService,
    IWorkspaceQueryService workspaceQueryService,
    IFileCenterService fileCenterService,
    INavigationService navigationService)
{
    [McpServerResource(
        UriTemplate = "aidt://projects/{id}",
        Name = "AIDT Project",
        MimeType = "application/json")]
    [Description("读取当前用户有权访问的 AIDT 项目详情。")]
    public Task<string> Project(string id, CancellationToken cancellationToken) =>
        runtime.ExecuteResourceReadAsync(
            "aidt://projects/{id}",
            McpScopeNames.Read,
            "Project",
            id,
            async (identity, token) =>
            {
                var result = await projectService.GetAsync(
                    identity.User,
                    ParseId(id),
                    includeTestData: false,
                    Guid.NewGuid().ToString("N"),
                    token)
                    ?? throw new NotFoundException("项目不存在或无权访问。");
                EnsureDepartmentAllowed(identity, result.Project.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerResource(
        UriTemplate = "aidt://events/{id}",
        Name = "AIDT Event",
        MimeType = "application/json")]
    [Description("读取当前用户有权访问的事件、原文来源和 Agent 建议。")]
    public Task<string> Event(string id, CancellationToken cancellationToken) =>
        runtime.ExecuteResourceReadAsync(
            "aidt://events/{id}",
            McpScopeNames.Read,
            "EventInbox",
            id,
            async (identity, token) =>
            {
                var result = await inboxService.GetAsync(
                    identity.User,
                    ParseId(id),
                    includeTestData: false,
                    Guid.NewGuid().ToString("N"),
                    token)
                    ?? throw new NotFoundException("事件不存在或无权访问。");
                EnsureDepartmentAllowed(identity, result.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerResource(
        UriTemplate = "aidt://project-tasks/{id}",
        Name = "AIDT Project Task Trace",
        MimeType = "application/json")]
    [Description("读取项目任务及其事件、证据和文件来源链。")]
    public Task<string> ProjectTask(string id, CancellationToken cancellationToken) =>
        runtime.ExecuteResourceReadAsync(
            "aidt://project-tasks/{id}",
            McpScopeNames.Read,
            "ProjectTask",
            id,
            async (identity, token) =>
            {
                var result = await inboxService.TraceTaskAsync(identity.User, ParseId(id), token)
                    ?? throw new NotFoundException("项目任务或证据不存在。");
                EnsureDepartmentAllowed(identity, result.Project.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerResource(
        UriTemplate = "aidt://workflow-instances/{id}",
        Name = "AIDT Workflow Instance",
        MimeType = "application/json")]
    [Description("读取当前用户有权访问的流程实例。")]
    public Task<string> WorkflowInstance(string id, CancellationToken cancellationToken) =>
        runtime.ExecuteResourceReadAsync(
            "aidt://workflow-instances/{id}",
            McpScopeNames.Read,
            "WorkflowInstance",
            id,
            async (identity, token) =>
            {
                var result = await workspaceQueryService.GetWorkflowInstanceAsync(
                    identity.User,
                    ParseId(id),
                    includeTestData: false,
                    Guid.NewGuid().ToString("N"),
                    token)
                    ?? throw new NotFoundException("流程实例不存在或无权访问。");
                EnsureDepartmentAllowed(identity, result.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerResource(
        UriTemplate = "aidt://files/{id}",
        Name = "AIDT File Metadata",
        MimeType = "application/json")]
    [Description("读取文件元数据；不返回文件正文或物理存储路径。")]
    public Task<string> File(string id, CancellationToken cancellationToken) =>
        runtime.ExecuteResourceReadAsync(
            "aidt://files/{id}",
            McpScopeNames.Files,
            "FileAttachment",
            id,
            async (identity, token) =>
            {
                var result = await fileCenterService.GetAsync(
                    identity.User,
                    ParseId(id),
                    includeTestData: false,
                    token)
                    ?? throw new NotFoundException("文件不存在或无权访问。");
                EnsureDepartmentAllowed(identity, result.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerResource(
        UriTemplate = "aidt://navigation/current",
        Name = "AIDT Current Navigation",
        MimeType = "application/json")]
    [Description("读取当前用户的 AIDT 导航树。")]
    public Task<string> Navigation(CancellationToken cancellationToken) =>
        runtime.ExecuteResourceReadAsync(
            "aidt://navigation/current",
            McpScopeNames.Read,
            "Navigation",
            "current",
            async (identity, token) =>
                await navigationService.GetCurrentAsync(identity.User, token),
            cancellationToken);

    private static Guid ParseId(string value) =>
        Guid.TryParse(value, out var id)
            ? id
            : throw new ValidationException("资源 ID 必须是有效 GUID。");

    private static void EnsureDepartmentAllowed(
        AIDT.Application.Contracts.McpTokenIdentity identity,
        string? departmentId)
    {
        if (!Guid.TryParse(departmentId, out var parsed)
            || !identity.DepartmentIds.Contains(parsed))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "该资源不属于当前 MCP 令牌选择的部门范围。");
        }
    }
}
