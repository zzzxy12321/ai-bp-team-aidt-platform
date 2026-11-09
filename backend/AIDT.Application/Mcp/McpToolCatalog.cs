using AIDT.Application.Abstractions;
using AIDT.Application.Contracts;

namespace AIDT.Application.Mcp;

public sealed class McpToolCatalog : IMcpToolCatalog
{
    private static readonly IReadOnlyList<McpToolDescriptorDto> Tools =
    [
        new("aidt_whoami", "当前身份", "读取 MCP 当前用户、组织、部门、角色和有效权限。",
            McpScopeNames.Read, true, false, "Identity", ["permissions:Read"], "identity.read"),
        new("aidt_get_navigation", "当前导航", "读取当前用户有权访问的导航工作区与入口。",
            McpScopeNames.Read, true, false, "Navigation", ["navigation:Read"], "identity.read"),
        new("aidt_list_projects", "项目列表", "按当前用户和 MCP 部门范围分页查询项目。",
            McpScopeNames.Read, true, false, "Project", ["project:Read"], "project.read"),
        new("aidt_get_project", "项目详情", "读取项目成员、里程碑、任务、证据、文件和相关审计。",
            McpScopeNames.Read, true, false, "Project", ["project:Read"], "project.read"),
        new("aidt_create_project", "创建项目", "创建 PROJECT 业务主对象和项目档案。",
            McpScopeNames.Write, false, false, "Project", ["project:Create", "business-object:Create"], "project.write"),
        new("aidt_add_project_member", "添加项目成员", "向受控项目添加成员并指定项目角色。",
            McpScopeNames.Write, false, false, "ProjectMember", ["project-member:Manage"], "project.write"),
        new("aidt_add_project_milestone", "添加项目里程碑", "为授权项目创建里程碑和责任人。",
            McpScopeNames.Write, false, false, "ProjectMilestone", ["milestone:Manage"], "project.write"),
        new("aidt_change_project_status", "变更项目状态", "变更项目状态；最终状态需要真人确认。",
            McpScopeNames.Approve, false, true, "Project", ["project:Update"], "project.write"),
        new("aidt_complete_project_task", "完成项目任务", "由任务负责人或项目经理完成项目任务。",
            McpScopeNames.Write, false, false, "ProjectTask", ["project-task:Update"], "project.write"),
        new("aidt_trace_project_task", "反查项目任务", "读取项目任务的来源事件、文件、流程和证据链。",
            McpScopeNames.Read, true, false, "ProjectTask", ["project-task:Read"], "project.read"),
        new("aidt_list_events", "事件列表", "分页查询当前用户可见的收件箱事件。",
            McpScopeNames.Read, true, false, "EventInbox", ["event-inbox:Read"], "event.inbox.review"),
        new("aidt_get_event", "事件详情", "读取事件原文、来源和 Agent 建议。",
            McpScopeNames.Read, true, false, "EventInbox", ["event-inbox:Read"], "event.inbox.review"),
        new("aidt_create_event", "创建事项", "将受控文本事项写入企业事件收件箱。",
            McpScopeNames.Write, false, false, "EventInbox", ["event-inbox:Create"], "event.inbox.review"),
        new("aidt_analyze_event", "分析事件", "调用已授权事件建议数字员工，只生成建议。",
            McpScopeNames.Write, false, false, "EventInbox", ["event-inbox:Update", "agent-run:Submit"], "event.inbox.review"),
        new("aidt_confirm_event", "确认事件", "由真人确认事件并创建或关联项目任务。",
            McpScopeNames.Approve, false, true, "EventInbox", ["event-inbox:Confirm", "project-task:Create"], "event.inbox.review"),
        new("aidt_list_workflow_instances", "流程实例列表", "分页查询当前用户可见的流程实例。",
            McpScopeNames.Read, true, false, "WorkflowInstance", ["workflow-instance:Read"], "workflow.read"),
        new("aidt_get_workflow_instance", "流程实例详情", "读取固定流程版本、当前节点和状态。",
            McpScopeNames.Read, true, false, "WorkflowInstance", ["workflow-instance:Read"], "workflow.read"),
        new("aidt_list_workflow_tasks", "流程待办列表", "分页查询当前用户有效数据范围内的流程任务。",
            McpScopeNames.Read, true, false, "WorkflowTask", ["workflow-task:Read"], "workflow.read"),
        new("aidt_decide_workflow_task", "处理流程任务", "通过、退回或终止流程任务，必须真人确认。",
            McpScopeNames.Approve, false, true, "WorkflowTask",
            ["workflow-task:Approve", "workflow-task:Return", "workflow-task:Terminate"], "workflow.proxy-low-risk"),
        new("aidt_transfer_workflow_task", "转交流程任务", "把任务转交给任务部门内的合格处理人，必须真人确认。",
            McpScopeNames.Approve, false, true, "WorkflowTask", ["workflow-task:Update"], "workflow.proxy-low-risk"),
        new("aidt_list_files", "文件元数据列表", "按授权部门、对象、流程和环境查询文件元数据。",
            McpScopeNames.Files, true, false, "FileAttachment", ["file:Read"], "file.metadata.read"),
        new("aidt_get_file_metadata", "文件元数据详情", "读取文件摘要、保密等级、扫描和关联信息，不返回物理路径。",
            McpScopeNames.Files, true, false, "FileAttachment", ["file:Read"], "file.metadata.read"),
        new("aidt_upload_file", "上传文件", "上传受控文件；敏感/Core 文件需要真人确认。",
            McpScopeNames.Files, false, true, "FileAttachment", ["file:Create"], "file.upload"),
        new("aidt_download_file", "下载文件正文", "下载已完成授权的文件正文；敏感/Core 文件需要真人确认。",
            McpScopeNames.Files, true, true, "FileAttachment", ["file:Read"], "file.metadata.read"),
        new("aidt_list_audit", "审计列表", "分页读取当前用户可见的追加式审计。",
            McpScopeNames.Read, true, false, "AuditEvent", ["audit:Read"], "audit.read"),
        new("aidt_list_agent_profiles", "数字员工列表", "读取可用的独立数字员工身份、等级和限额。",
            McpScopeNames.Read, true, false, "AgentProfile", ["agent:Read"], "identity.read")
    ];

    public IReadOnlyList<McpToolDescriptorDto> List() => Tools;

    public McpToolDescriptorDto? Find(string toolName) =>
        Tools.FirstOrDefault(item => string.Equals(item.Name, toolName, StringComparison.Ordinal));
}
