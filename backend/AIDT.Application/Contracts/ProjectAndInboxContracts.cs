using AIDT.Application.Contracts;

namespace AIDT.Application.Contracts;

public sealed record CreateProjectRequest(
    string ObjectCode,
    string Name,
    Guid OwnerUserId,
    Guid DepartmentId,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    string Objective,
    string CurrentStage,
    string ConfidentialityLevel,
    string? InitialStatus = null);

public sealed record UpdateProjectRequest(
    long ExpectedVersion,
    string Name,
    Guid OwnerUserId,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    string Objective,
    string CurrentStage,
    string ConfidentialityLevel);

public sealed record ChangeProjectStatusRequest(
    long ExpectedVersion,
    string Status,
    string? Comment);

public sealed record AddProjectMemberRequest(
    Guid UserId,
    string ProjectRole,
    bool IsLead = false);

public sealed record RemoveProjectMemberRequest(string? Reason);

public sealed record CreateProjectMilestoneRequest(
    string Title,
    Guid OwnerUserId,
    DateTimeOffset PlannedDate,
    string Status = "Planned");

public sealed record UpdateProjectMilestoneRequest(
    long ExpectedVersion,
    string Title,
    Guid OwnerUserId,
    DateTimeOffset PlannedDate,
    string Status);

public sealed record CompleteProjectTaskRequest(
    long ExpectedVersion,
    string? Comment);

public sealed record CreateProjectRelationRequest(
    string EntityType,
    Guid EntityId,
    string RelationCode,
    string? EvidenceExcerpt = null);

public sealed record CreateProjectTaskRequest(
    Guid AssigneeId,
    string Title,
    string Content,
    DateTimeOffset DueAt,
    string Priority = "P1");

public sealed record ProjectDto(
    string Id,
    string ObjectCode,
    string Name,
    string Status,
    string StatusDisplayName,
    long Version,
    string OwnerUserId,
    string OwnerDisplayName,
    string DepartmentId,
    string DepartmentName,
    string EnvironmentCode,
    bool IsTestData,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    string Objective,
    string CurrentStage,
    string ConfidentialityLevel,
    int ProgressPercent,
    int MemberCount,
    int MilestoneCount,
    int CompletedMilestoneCount,
    int OverdueMilestoneCount,
    int ProjectTaskCount,
    int CompletedTaskCount,
    int OverdueTaskCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectMemberDto(
    string ProjectId,
    string UserId,
    string DisplayName,
    string UserName,
    string DepartmentId,
    string DepartmentName,
    string ProjectRole,
    bool IsLead,
    bool IsActive,
    DateTimeOffset JoinedAt,
    DateTimeOffset? RemovedAt);

public sealed record ProjectMilestoneDto(
    string Id,
    string ProjectId,
    string Title,
    string OwnerUserId,
    string OwnerDisplayName,
    DateTimeOffset PlannedDate,
    string Status,
    bool IsOverdue,
    DateTimeOffset? CompletedAt,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProjectTaskDto(
    string Id,
    string ProjectId,
    string? EventInboxId,
    string AssigneeId,
    string AssigneeDisplayName,
    string DepartmentId,
    string Title,
    string Content,
    DateTimeOffset? DueAt,
    string Priority,
    string Status,
    long Version,
    string? BusinessObjectId,
    string? WorkflowInstanceId,
    string? WorkflowTaskId,
    string EnvironmentCode,
    bool IsTestData,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt);

public sealed record ProjectRelationDto(
    string Id,
    string ProjectId,
    string EntityType,
    string EntityId,
    string RelationCode,
    string? EvidenceExcerpt,
    DateTimeOffset CreatedAt);

public sealed record ProjectWorkflowDto(
    WorkflowInstanceDto Instance,
    string DefinitionCode,
    string DefinitionName,
    IReadOnlyList<WorkflowTaskDto> Tasks,
    string SlaRisk);

public sealed record ProjectDetailDto(
    ProjectDto Project,
    IReadOnlyList<ProjectMemberDto> Members,
    IReadOnlyList<ProjectMilestoneDto> Milestones,
    IReadOnlyList<ProjectTaskDto> Tasks,
    IReadOnlyList<ProjectRelationDto> Relations,
    IReadOnlyList<BusinessObjectDto> RelatedBusinessObjects,
    IReadOnlyList<ProjectWorkflowDto> RelatedWorkflows,
    IReadOnlyList<FileAttachmentDto> Files,
    IReadOnlyList<MinutesSourceDto> Minutes,
    IReadOnlyList<AuditEventDto> AuditTimeline);

public sealed record ProjectCandidateUserDto(
    string Id,
    string UserName,
    string DisplayName,
    string DepartmentId,
    string DepartmentName);

public sealed record ReceiveInboxEventRequest(
    Guid DepartmentId,
    string Title,
    string Content,
    string SourceType = "ManualText",
    Guid? SourceId = null,
    Guid? AttachmentId = null);

public sealed record AnalyzeInboxEventRequest(Guid AgentProfileId);

public sealed record ConfirmInboxEventRequest(
    Guid? ProjectId,
    Guid? AssigneeId,
    DateTimeOffset? DueAt,
    string? Priority,
    string? Content,
    Guid? WorkflowInstanceId,
    bool CreateTask = true,
    Guid? WorkflowDefinitionId = null,
    Guid? WorkflowVersionId = null);

public sealed record InboxSuggestionDto(
    string Id,
    string EventInboxId,
    string AgentRunId,
    string? SuggestedProjectId,
    string? SuggestedProjectName,
    string? SuggestedBusinessObjectId,
    string? SuggestedBusinessObjectCode,
    string? SuggestedAssigneeId,
    string? SuggestedAssigneeDisplayName,
    string? SuggestedWorkflowVersionId,
    string? SuggestedWorkflowName,
    DateTimeOffset? SuggestedDueAt,
    string Priority,
    string RiskLevel,
    bool RequiresHumanBoundary,
    string? BoundaryReason,
    string MissingInformation,
    string EvidenceExcerpt,
    string Status,
    DateTimeOffset CreatedAt);

public sealed record InboxEventDto(
    string Id,
    string DepartmentId,
    string DepartmentName,
    string SourceType,
    string? SourceId,
    string? SourceTitle,
    string? AttachmentId,
    string Title,
    string RawText,
    string Status,
    string SubmittedBy,
    string SubmittedByDisplayName,
    string? AnalysisRunId,
    string EnvironmentCode,
    bool IsTestData,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    InboxSuggestionDto? Suggestion);

public sealed record InboxConfirmationDto(
    InboxEventDto Event,
    ProjectTaskDto? Task,
    ProjectDto Project,
    WorkflowInstanceDto? Workflow = null);

public sealed record InboxTaskTraceDto(
    ProjectTaskDto Task,
    InboxEventDto Event,
    ProjectDto Project);

public sealed record InboxSourceOptionDto(
    string Type,
    string Id,
    string Title,
    string DepartmentId,
    string Detail,
    DateTimeOffset OccurredAt);
