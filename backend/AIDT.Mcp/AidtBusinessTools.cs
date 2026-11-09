using System.ComponentModel;
using System.Text;
using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;
using AIDT.Application.UseCases;
using AIDT.Domain.Common;
using AIDT.Domain.Workflows;
using ModelContextProtocol.Server;

namespace AIDT.Mcp;

[McpServerToolType]
public sealed class AidtBusinessTools(
    McpToolRuntime runtime,
    IWorkspaceQueryService workspaceQueryService,
    IProjectService projectService,
    IEventInboxService inboxService,
    IFileCenterService fileCenterService,
    INavigationService navigationService,
    ProcessWorkflowTaskHandler workflowDecisionHandler,
    IWorkflowTaskAssignmentService workflowAssignmentService,
    IMcpCallContextAccessor callContextAccessor)
{
    [McpServerTool(Name = "aidt_whoami", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("读取 AIDT MCP 当前身份、组织、部门范围、角色和权限。")]
    public Task<McpToolResponse> WhoAmI(CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_whoami",
            "Identity",
            "current",
            (identity, _) => Task.FromResult<object?>(
                new
                {
                    userId = identity.UserId,
                    identity.User.DisplayName,
                    organizationId = identity.OrganizationId,
                    identity.User.IsServiceIdentity,
                    roles = identity.User.Roles,
                    departments = identity.DepartmentIds,
                    permissions = identity.User.Permissions.Select(item => $"{item.Resource}:{item.Action}"),
                    mcpScopes = identity.Scopes
                }),
            cancellationToken);

    [McpServerTool(Name = "aidt_get_navigation", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("读取当前 AIDT 用户可见的导航工作区和业务入口。")]
    public Task<McpToolResponse> GetNavigation(CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_get_navigation",
            "Navigation",
            "current",
            async (identity, token) =>
                await navigationService.GetCurrentAsync(identity.User, token),
            cancellationToken);

    [McpServerTool(Name = "aidt_list_projects", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("分页查询当前 AIDT 用户和 MCP 令牌部门范围可见的项目。")]
    public Task<McpToolResponse> ListProjects(
        ListProjectsArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_list_projects",
            "Project",
            "list",
            async (identity, token) =>
            {
                var result = await projectService.ListAsync(
                    identity.User,
                    Math.Max(1, arguments.Page),
                    Math.Clamp(arguments.PageSize, 1, 100),
                    arguments.IncludeTestData,
                    token);
                var items = result.Items
                    .Where(item => IsDepartmentAllowed(identity, item.DepartmentId))
                    .ToList();
                return new PagedResult<ProjectDto>(
                    items,
                    result.Page,
                    result.PageSize,
                    items.Count);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_get_project", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("读取项目详情、成员、里程碑、任务、证据、文件和审计时间线。")]
    public Task<McpToolResponse> GetProject(
        ProjectIdArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_get_project",
            "Project",
            arguments.ProjectId.ToString("D"),
            async (identity, token) =>
            {
                var result = await projectService.GetAsync(
                    identity.User,
                    arguments.ProjectId,
                    arguments.IncludeTestData,
                    Guid.NewGuid().ToString("N"),
                    token)
                    ?? throw new NotFoundException("项目不存在或无权访问。");
                EnsureDepartmentAllowed(identity, result.Project.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_create_project", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("创建受控项目主对象与 PMO 档案。")]
    public Task<McpToolResponse> CreateProject(
        CreateProjectArguments arguments,
        CancellationToken cancellationToken)
    {
        EnsureDepartment(DepartmentFromArguments(arguments.DepartmentId));
        return runtime.ExecuteWriteAsync(
            "aidt_create_project",
            "Project",
            "new",
            arguments,
            arguments.IdempotencyKey,
            async (identity, token) =>
            {
                EnsureDepartmentAllowed(identity, arguments.DepartmentId);
                return await projectService.CreateAsync(
                    identity.User,
                    new CreateProjectRequest(
                        arguments.ObjectCode,
                        arguments.Name,
                        arguments.OwnerUserId,
                        arguments.DepartmentId,
                        arguments.PlannedStartDate,
                        arguments.PlannedEndDate,
                        arguments.Objective,
                        arguments.CurrentStage,
                        arguments.ConfidentialityLevel,
                        arguments.InitialStatus),
                    arguments.IdempotencyKey,
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);
    }

    [McpServerTool(Name = "aidt_add_project_member", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("向当前用户可管理的项目添加成员。")]
    public Task<McpToolResponse> AddProjectMember(
        AddProjectMemberArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteWriteAsync(
            "aidt_add_project_member",
            "ProjectMember",
            arguments.ProjectId.ToString("D"),
            arguments,
            arguments.IdempotencyKey,
            async (identity, token) =>
            {
                await LoadProjectForTokenAsync(identity, arguments.ProjectId, token);
                return await projectService.AddMemberAsync(
                    identity.User,
                    arguments.ProjectId,
                    new AddProjectMemberRequest(arguments.UserId, arguments.ProjectRole, arguments.IsLead),
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_add_project_milestone", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("为当前用户可管理的项目添加里程碑。")]
    public Task<McpToolResponse> AddProjectMilestone(
        AddProjectMilestoneArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteWriteAsync(
            "aidt_add_project_milestone",
            "ProjectMilestone",
            arguments.ProjectId.ToString("D"),
            arguments,
            arguments.IdempotencyKey,
            async (identity, token) =>
            {
                await LoadProjectForTokenAsync(identity, arguments.ProjectId, token);
                return await projectService.AddMilestoneAsync(
                    identity.User,
                    arguments.ProjectId,
                    new CreateProjectMilestoneRequest(
                        arguments.Title,
                        arguments.OwnerUserId,
                        arguments.PlannedDate,
                        arguments.Status),
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_change_project_status", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("变更项目状态；Completed/Terminated 必须由当前用户在浏览器中真人确认。")]
    public Task<McpToolResponse> ChangeProjectStatus(
        ChangeProjectStatusArguments arguments,
        CancellationToken cancellationToken)
    {
        var request = new ChangeProjectStatusRequest(
            arguments.ExpectedVersion,
            arguments.Status,
            arguments.Comment);
        Func<AIDT.Application.Contracts.McpTokenIdentity, CancellationToken, Task<object?>> action =
            async (identity, token) =>
            {
                await LoadProjectForTokenAsync(identity, arguments.ProjectId, token);
                return await projectService.ChangeStatusAsync(
                    identity.User,
                    arguments.ProjectId,
                    request,
                    Guid.NewGuid().ToString("N"),
                    token);
            };
        return IsFinalProjectStatus(arguments.Status)
            ? runtime.ExecuteHighRiskAsync(
                "aidt_change_project_status",
                "Project",
                arguments.ProjectId.ToString("D"),
                arguments,
                arguments.IdempotencyKey,
                arguments.HumanConfirmationId,
                action,
                cancellationToken)
            : runtime.ExecuteWriteAsync(
                "aidt_change_project_status",
                "Project",
                arguments.ProjectId.ToString("D"),
                arguments,
                arguments.IdempotencyKey,
                action,
                cancellationToken);
    }

    [McpServerTool(Name = "aidt_complete_project_task", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("由任务负责人或项目经理完成项目任务。")]
    public Task<McpToolResponse> CompleteProjectTask(
        CompleteProjectTaskArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteWriteAsync(
            "aidt_complete_project_task",
            "ProjectTask",
            arguments.TaskId.ToString("D"),
            arguments,
            arguments.IdempotencyKey,
            async (identity, token) =>
            {
                var trace = await inboxService.TraceTaskAsync(identity.User, arguments.TaskId, token)
                    ?? throw new NotFoundException("项目任务或事件证据不存在。");
                EnsureDepartmentAllowed(identity, trace.Project.DepartmentId);
                return await projectService.CompleteTaskAsync(
                    identity.User,
                    arguments.TaskId,
                    new CompleteProjectTaskRequest(arguments.ExpectedVersion, arguments.Comment),
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_trace_project_task", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("反查项目任务的来源事件、证据、流程和文件链。")]
    public Task<McpToolResponse> TraceProjectTask(
        EntityIdArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_trace_project_task",
            "ProjectTask",
            arguments.Id.ToString("D"),
            async (identity, token) =>
            {
                var trace = await inboxService.TraceTaskAsync(identity.User, arguments.Id, token)
                    ?? throw new NotFoundException("项目任务或事件证据不存在。");
                EnsureDepartmentAllowed(identity, trace.Project.DepartmentId);
                return trace;
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_list_events", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("分页查询当前用户可见的企业事件收件箱。")]
    public Task<McpToolResponse> ListEvents(
        ListEventsArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_list_events",
            "EventInbox",
            "list",
            async (identity, token) =>
            {
                var result = await inboxService.ListAsync(
                    identity.User,
                    Math.Max(1, arguments.Page),
                    Math.Clamp(arguments.PageSize, 1, 100),
                    arguments.Status,
                    arguments.IncludeTestData,
                    token);
                var items = result.Items
                    .Where(item => IsDepartmentAllowed(identity, item.DepartmentId))
                    .ToList();
                return new PagedResult<InboxEventDto>(
                    items,
                    result.Page,
                    result.PageSize,
                    items.Count);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_get_event", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("读取事件原文、来源和 Agent 建议。")]
    public Task<McpToolResponse> GetEvent(
        EntityIdArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_get_event",
            "EventInbox",
            arguments.Id.ToString("D"),
            async (identity, token) =>
            {
                var result = await inboxService.GetAsync(
                    identity.User,
                    arguments.Id,
                    arguments.IncludeTestData,
                    Guid.NewGuid().ToString("N"),
                    token)
                    ?? throw new NotFoundException("事件不存在或无权访问。");
                EnsureDepartmentAllowed(identity, result.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_create_event", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("将受控文本事项写入企业事件收件箱。")]
    public Task<McpToolResponse> CreateEvent(
        CreateEventArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteWriteAsync(
            "aidt_create_event",
            "EventInbox",
            "new",
            arguments,
            arguments.IdempotencyKey,
            async (identity, token) =>
            {
                EnsureDepartmentAllowed(identity, arguments.DepartmentId);
                return await inboxService.ReceiveAsync(
                    identity.User,
                    new ReceiveInboxEventRequest(
                        arguments.DepartmentId,
                        arguments.Title,
                        arguments.Content,
                        string.IsNullOrWhiteSpace(arguments.SourceType) ? "ManualText" : arguments.SourceType,
                        arguments.SourceId,
                        arguments.AttachmentId),
                    arguments.IdempotencyKey,
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_analyze_event", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("调用已授权事件建议数字员工，只生成建议，不创建任务、发起流程或代替真人确认。")]
    public Task<McpToolResponse> AnalyzeEvent(
        AnalyzeEventToolArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteWriteAsync(
            "aidt_analyze_event",
            "EventInbox",
            arguments.EventId.ToString("D"),
            arguments,
            arguments.IdempotencyKey,
            async (identity, token) =>
            {
                await LoadEventForTokenAsync(identity, arguments.EventId, token);
                return await inboxService.AnalyzeAsync(
                    identity.User,
                    arguments.EventId,
                    new AnalyzeInboxEventRequest(arguments.AgentProfileId),
                    arguments.IdempotencyKey,
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_confirm_event", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("真人确认事件并按现有权限创建或关联项目任务；必须提供浏览器真人确认 ID。")]
    public Task<McpToolResponse> ConfirmEvent(
        ConfirmEventToolArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteHighRiskAsync(
            "aidt_confirm_event",
            "EventInbox",
            arguments.EventId.ToString("D"),
            arguments,
            arguments.IdempotencyKey,
            arguments.HumanConfirmationId,
            async (identity, token) =>
            {
                await LoadEventForTokenAsync(identity, arguments.EventId, token);
                return await inboxService.ConfirmAsync(
                    identity.User,
                    arguments.EventId,
                    new ConfirmInboxEventRequest(
                        arguments.ProjectId,
                        arguments.AssigneeId,
                        arguments.DueAt,
                        arguments.Priority,
                        arguments.Content,
                        arguments.WorkflowInstanceId,
                        arguments.CreateTask),
                    arguments.IdempotencyKey,
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_list_workflow_instances", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("分页查询当前用户可见的流程实例。")]
    public Task<McpToolResponse> ListWorkflowInstances(
        ListWorkflowArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_list_workflow_instances",
            "WorkflowInstance",
            "list",
            async (identity, token) =>
            {
                var result = await workspaceQueryService.ListWorkflowInstancesAsync(
                    identity.User,
                    Math.Max(1, arguments.Page),
                    Math.Clamp(arguments.PageSize, 1, 100),
                    arguments.Status,
                    arguments.IncludeTestData,
                    token);
                var items = result.Items
                    .Where(item => IsDepartmentAllowed(identity, item.DepartmentId))
                    .ToList();
                return new PagedResult<WorkflowInstanceDto>(
                    items,
                    result.Page,
                    result.PageSize,
                    items.Count);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_get_workflow_instance", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("读取固定流程版本的实例状态和当前节点。")]
    public Task<McpToolResponse> GetWorkflowInstance(
        EntityIdArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_get_workflow_instance",
            "WorkflowInstance",
            arguments.Id.ToString("D"),
            async (identity, token) =>
            {
                var result = await workspaceQueryService.GetWorkflowInstanceAsync(
                    identity.User,
                    arguments.Id,
                    arguments.IncludeTestData,
                    Guid.NewGuid().ToString("N"),
                    token)
                    ?? throw new NotFoundException("流程实例不存在或无权访问。");
                EnsureDepartmentAllowed(identity, result.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_list_workflow_tasks", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("分页查询当前用户有效数据范围内的流程任务。")]
    public Task<McpToolResponse> ListWorkflowTasks(
        ListWorkflowArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_list_workflow_tasks",
            "WorkflowTask",
            "list",
            async (identity, token) =>
            {
                var result = await workspaceQueryService.ListTasksAsync(
                    identity.User,
                    Math.Max(1, arguments.Page),
                    Math.Clamp(arguments.PageSize, 1, 100),
                    arguments.Status,
                    arguments.IncludeTestData,
                    token);
                var items = result.Items
                    .Where(item => IsDepartmentAllowed(identity, item.DepartmentId))
                    .ToList();
                return new PagedResult<WorkflowTaskDto>(
                    items,
                    result.Page,
                    result.PageSize,
                    items.Count);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_decide_workflow_task", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("通过、退回或终止流程任务；必须由当前任务处理人在浏览器真人确认后执行。")]
    public Task<McpToolResponse> DecideWorkflowTask(
        DecideWorkflowTaskArguments arguments,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<WorkflowDecision>(arguments.Decision, true, out var decision))
        {
            throw new ValidationException("流程决定必须为 Approve、Return 或 Terminate。");
        }

        return runtime.ExecuteHighRiskAsync(
            "aidt_decide_workflow_task",
            "WorkflowTask",
            arguments.TaskId.ToString("D"),
            arguments,
            arguments.IdempotencyKey,
            arguments.HumanConfirmationId,
            async (identity, token) =>
            {
                await EnsureTaskDepartmentAllowedAsync(identity, arguments.TaskId, token);
                return await workflowDecisionHandler.HandleAsync(
                    arguments.TaskId,
                    new TaskDecisionRequest(
                        decision,
                        arguments.ExpectedVersion,
                        arguments.Comment,
                        arguments.IdempotencyKey),
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);
    }

    [McpServerTool(Name = "aidt_transfer_workflow_task", Destructive = true, Idempotent = true, OpenWorld = false)]
    [Description("把流程任务转交给任务部门内符合条件的处理人；必须真人确认。")]
    public Task<McpToolResponse> TransferWorkflowTask(
        TransferWorkflowTaskArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteHighRiskAsync(
            "aidt_transfer_workflow_task",
            "WorkflowTask",
            arguments.TaskId.ToString("D"),
            arguments,
            arguments.IdempotencyKey,
            arguments.HumanConfirmationId,
            async (identity, token) =>
            {
                await EnsureTaskDepartmentAllowedAsync(identity, arguments.TaskId, token);
                return await workflowAssignmentService.TransferAsync(
                    identity.User,
                    arguments.TaskId,
                    new TransferWorkflowTaskRequest(
                        arguments.TargetUserId,
                        arguments.ExpectedVersion,
                        arguments.Reason,
                        arguments.IdempotencyKey),
                    Guid.NewGuid().ToString("N"),
                    token);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_list_files", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("查询文件元数据；不返回物理路径。")]
    public Task<McpToolResponse> ListFiles(
        ListFilesArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_list_files",
            "FileAttachment",
            "list",
            async (identity, token) =>
            {
                var result = await fileCenterService.ListAsync(
                    identity.User,
                    new AttachmentListQuery(
                        arguments.DepartmentId,
                        arguments.BusinessObjectId,
                        arguments.WorkflowInstanceId,
                        arguments.WorkflowTaskId,
                        arguments.EnvironmentCode,
                        arguments.IncludeTestData,
                        Math.Max(1, arguments.Page),
                        Math.Clamp(arguments.PageSize, 1, 100)),
                    token);
                var items = result.Items
                    .Where(item => IsDepartmentAllowed(identity, item.DepartmentId))
                    .ToList();
                return new PagedResult<FileAttachmentDto>(
                    items,
                    result.Page,
                    result.PageSize,
                    items.Count);
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_get_file_metadata", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("读取文件元数据、摘要、保密等级和关联；不返回物理存储路径。")]
    public Task<McpToolResponse> GetFileMetadata(
        EntityIdArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_get_file_metadata",
            "FileAttachment",
            arguments.Id.ToString("D"),
            async (identity, token) =>
            {
                var result = await fileCenterService.GetAsync(
                    identity.User,
                    arguments.Id,
                    arguments.IncludeTestData,
                    token)
                    ?? throw new NotFoundException("文件不存在或无权访问。");
                EnsureDepartmentAllowed(identity, result.DepartmentId);
                return result;
            },
            cancellationToken);

    [McpServerTool(Name = "aidt_upload_file", Destructive = false, Idempotent = true, OpenWorld = false)]
    [Description("上传 Base64 文件；Sensitive/Core 文件必须先由当前用户浏览器确认。最大正文受 MCP 配置限制。")]
    public async Task<McpToolResponse> UploadFile(
        UploadFileArguments arguments,
        CancellationToken cancellationToken)
    {
        byte[] content;
        try
        {
            content = Convert.FromBase64String(arguments.Base64Content);
        }
        catch (FormatException)
        {
            throw new ValidationException("文件内容不是有效 Base64。");
        }

        if (content.Length > 5 * 1024 * 1024)
        {
            throw new ValidationException("MCP 文件上传正文不能超过 5 MiB。");
        }

        Func<AIDT.Application.Contracts.McpTokenIdentity, CancellationToken, Task<object?>> action =
            async (identity, token) =>
            {
                EnsureDepartmentAllowed(identity, arguments.DepartmentId);
                await using var stream = new MemoryStream(content, writable: false);
                return await fileCenterService.UploadAsync(
                    identity.User,
                    new UploadFileCommand(
                        stream,
                        Path.GetFileName(arguments.FileName),
                        arguments.MimeType,
                        arguments.DepartmentId,
                        arguments.BusinessObjectId,
                        arguments.WorkflowInstanceId,
                        arguments.WorkflowTaskId,
                        arguments.Sensitivity),
                    Guid.NewGuid().ToString("N"),
                    token);
            };
        return IsSensitive(arguments.Sensitivity)
            ? await runtime.ExecuteHighRiskAsync(
                "aidt_upload_file",
                "FileAttachment",
                "new",
                arguments,
                arguments.IdempotencyKey,
                arguments.HumanConfirmationId,
                action,
                cancellationToken)
            : await runtime.ExecuteWriteAsync(
                "aidt_upload_file",
                "FileAttachment",
                "new",
                arguments,
                arguments.IdempotencyKey,
                action,
                cancellationToken);
    }

    [McpServerTool(Name = "aidt_download_file", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("下载授权文件正文；Sensitive/Core 文件必须先由当前用户浏览器确认。返回 Base64，不返回物理路径。")]
    public async Task<McpToolResponse> DownloadFile(
        DownloadFileArguments arguments,
        CancellationToken cancellationToken)
    {
        var context = await callContextAccessor.GetRequiredAsync(cancellationToken);
        if (!context.Identity.Scopes.Contains(McpScopeNames.Files, StringComparer.Ordinal))
        {
            throw new PermissionException("MCP_INSUFFICIENT_SCOPE", "当前 MCP 令牌缺少 aidt:files 作用域。");
        }
        var metadata = await fileCenterService.GetAsync(
            context.Identity.User,
            arguments.FileId,
            arguments.IncludeTestData,
            cancellationToken)
            ?? throw new NotFoundException("文件不存在或无权访问。");
        EnsureDepartmentAllowed(context.Identity, metadata.DepartmentId);
        if (metadata.SizeBytes > 5 * 1024 * 1024)
        {
            throw new ValidationException("MCP 文件下载正文不能超过 5 MiB，请使用 AIDT 文件中心受控下载。");
        }

        Func<AIDT.Application.Contracts.McpTokenIdentity, CancellationToken, Task<object?>> action =
            async (identity, token) =>
            {
                var content = await fileCenterService.OpenContentAsync(
                    identity.User,
                    arguments.FileId,
                    preview: false,
                    arguments.IncludeTestData,
                    Guid.NewGuid().ToString("N"),
                    token);
                await using var buffer = new MemoryStream();
                await content.Content.CopyToAsync(buffer, token);
                return new
                {
                    content.DownloadName,
                    content.MimeType,
                    sizeBytes = buffer.Length,
                    base64Content = Convert.ToBase64String(buffer.ToArray())
                };
            };
        return IsSensitive(metadata.Sensitivity)
            ? await runtime.ExecuteHighRiskAsync(
                "aidt_download_file",
                "FileAttachment",
                arguments.FileId.ToString("D"),
                arguments,
                $"download-{arguments.FileId:D}",
                arguments.HumanConfirmationId,
                action,
                cancellationToken)
            : await runtime.ExecuteReadAsync(
                "aidt_download_file",
                "FileAttachment",
                arguments.FileId.ToString("D"),
                action,
                cancellationToken);
    }

    [McpServerTool(Name = "aidt_list_audit", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("分页读取当前用户有权访问的追加式审计。")]
    public Task<McpToolResponse> ListAudit(
        ListAuditArguments arguments,
        CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_list_audit",
            "AuditEvent",
            "list",
            async (identity, token) =>
                await workspaceQueryService.ListAuditEventsAsync(
                    identity.User,
                    Math.Max(1, arguments.Page),
                    Math.Clamp(arguments.PageSize, 1, 100),
                    arguments.EntityType,
                    arguments.EntityId,
                    token),
            cancellationToken);

    [McpServerTool(Name = "aidt_list_agent_profiles", ReadOnly = true, Idempotent = true, OpenWorld = false)]
    [Description("读取可用的独立数字员工身份、等级和限额。")]
    public Task<McpToolResponse> ListAgentProfiles(CancellationToken cancellationToken) =>
        runtime.ExecuteReadAsync(
            "aidt_list_agent_profiles",
            "AgentProfile",
            "list",
            async (identity, token) =>
                await workspaceQueryService.ListAgentProfilesAsync(identity.User, token),
            cancellationToken);

    private static bool IsSensitive(string sensitivity) =>
        sensitivity is "Sensitive" or "Core";

    private static bool IsFinalProjectStatus(string status) =>
        status is "Completed" or "Terminated";

    private static bool IsDepartmentAllowed(
        AIDT.Application.Contracts.McpTokenIdentity identity,
        string? departmentId) =>
        Guid.TryParse(departmentId, out var parsed)
        && identity.DepartmentIds.Contains(parsed);

    private static void EnsureDepartmentAllowed(
        AIDT.Application.Contracts.McpTokenIdentity identity,
        string? departmentId)
    {
        if (!IsDepartmentAllowed(identity, departmentId))
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "该资源不属于当前 MCP 令牌选择的部门范围。");
        }
    }

    private static void EnsureDepartmentAllowed(
        AIDT.Application.Contracts.McpTokenIdentity identity,
        Guid departmentId) =>
        EnsureDepartmentAllowed(identity, departmentId.ToString("D"));

    private async Task<ProjectDetailDto> LoadProjectForTokenAsync(
        AIDT.Application.Contracts.McpTokenIdentity identity,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        var result = await projectService.GetAsync(
            identity.User,
            projectId,
            includeTestData: false,
            Guid.NewGuid().ToString("N"),
            cancellationToken)
            ?? throw new NotFoundException("项目不存在或无权访问。");
        EnsureDepartmentAllowed(identity, result.Project.DepartmentId);
        return result;
    }

    private async Task<InboxEventDto> LoadEventForTokenAsync(
        AIDT.Application.Contracts.McpTokenIdentity identity,
        Guid eventId,
        CancellationToken cancellationToken)
    {
        var result = await inboxService.GetAsync(
            identity.User,
            eventId,
            includeTestData: false,
            Guid.NewGuid().ToString("N"),
            cancellationToken)
            ?? throw new NotFoundException("事件不存在或无权访问。");
        EnsureDepartmentAllowed(identity, result.DepartmentId);
        return result;
    }

    private async Task EnsureTaskDepartmentAllowedAsync(
        AIDT.Application.Contracts.McpTokenIdentity identity,
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var departmentId = await workspaceQueryService.GetTaskDepartmentAsync(
            identity.User,
            taskId,
            cancellationToken);
        if (departmentId is null)
        {
            throw new PermissionException(
                ErrorCodes.DepartmentScopeDenied,
                "流程任务不存在或不在当前 MCP 令牌的部门范围。");
        }

        EnsureDepartmentAllowed(identity, departmentId.Value);
    }

    private static Guid DepartmentFromArguments(Guid id) => id;

    private static void EnsureDepartment(Guid departmentId)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ValidationException("部门不能为空。");
        }
    }
}
