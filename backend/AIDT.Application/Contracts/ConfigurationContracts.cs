namespace AIDT.Application.Contracts;

public sealed record WorkflowConfigurationConditionRequest(
    string Field,
    string Operator,
    string Value);

public sealed record WorkflowFormFieldRequest(
    string Key,
    string DisplayName,
    string DataType,
    bool Required = false,
    string? DefaultValue = null,
    bool ReadOnly = false,
    IReadOnlyList<string>? VisibleRoleCodes = null,
    IReadOnlyList<string>? WriteRoleCodes = null,
    IReadOnlyList<string>? Options = null,
    string Width = "Full",
    int SortOrder = 0,
    string DataSource = "TaskInput",
    string? BoundObjectFieldCode = null,
    string Sensitivity = "Internal",
    string? HelpText = null);

public sealed record WorkflowNodeActionRequest(
    string Code,
    string Name,
    string ActionType,
    string? ConfirmationText = null);

public sealed record WorkflowAiReviewRequest(
    bool Enabled = false,
    string? RegisteredAction = null,
    Guid? AgentProfileId = null,
    IReadOnlyList<string>? ReadableFieldKeys = null,
    IReadOnlyList<string>? MaterialScopes = null,
    string OutputMode = "SuggestionOnly");

public sealed record WorkflowNotificationRequest(
    bool Enabled = true,
    int? BeforeDueMinutes = null,
    int? EscalateAfterMinutes = null,
    string? EscalateToRoleCode = null);

public sealed record WorkflowConfigurationNodeRequest(
    string Code,
    string Name,
    string? AssigneeUserName,
    string? NextNode,
    bool CompleteOnApprove = false,
    WorkflowConfigurationConditionRequest? Condition = null,
    string AssigneeStrategy = "SpecificUser",
    string? BusinessRoleCode = null,
    Guid? AgentProfileId = null,
    string? BusinessDescription = null,
    int? SlaMinutes = null,
    IReadOnlyList<string>? Deliverables = null,
    IReadOnlyList<string>? RelatedRoleCodes = null,
    int RequiredAttachmentCount = 0,
    string? CompletionCondition = null,
    IReadOnlyList<WorkflowFormFieldRequest>? Fields = null,
    IReadOnlyList<WorkflowNodeActionRequest>? Actions = null,
    bool AllowAgentAssist = false,
    bool AllowAgentProxy = false,
    bool PermanentHumanBoundary = false,
    WorkflowAiReviewRequest? AiReview = null,
    WorkflowNotificationRequest? Notification = null,
    string FormSchemaVersion = "1.0");

public sealed record CreateWorkflowConfigurationRequest(
    string ConfigurationKey,
    string Name,
    string? Description,
    Guid DepartmentId,
    string ObjectTypeCode,
    string StartNode,
    IReadOnlyList<WorkflowConfigurationNodeRequest> Nodes,
    bool IsDefault = false,
    IReadOnlyList<string>? ApplicableRoleCodes = null,
    string? RiskLevel = null);

public sealed record UpdateWorkflowConfigurationRequest(
    long ExpectedRevision,
    string Name,
    string? Description,
    Guid DepartmentId,
    string ObjectTypeCode,
    string StartNode,
    IReadOnlyList<WorkflowConfigurationNodeRequest> Nodes,
    bool IsDefault = false,
    IReadOnlyList<string>? ApplicableRoleCodes = null,
    string? RiskLevel = null);

public sealed record RollbackWorkflowConfigurationRequest(string? Comment);

public sealed record SimulateWorkflowConfigurationRequest(Guid? BusinessObjectId = null);

public sealed record WorkflowConfigurationSimulationDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Path,
    string RiskLevel,
    bool RequiresPlatformApproval);

public sealed record WorkflowTaskFormDto(
    string TaskId,
    string WorkflowInstanceId,
    string BusinessObjectId,
    string DepartmentId,
    string NodeCode,
    string NodeName,
    string? BusinessDescription,
    DateTimeOffset? DueAt,
    string FormSchemaVersion,
    string FormDefinitionHash,
    IReadOnlyList<WorkflowFormFieldRequest> Fields,
    IReadOnlyList<WorkflowNodeActionRequest> Actions,
    WorkflowAiReviewRequest AiReview,
    string NotificationPolicyJson,
    bool PermanentHumanBoundary,
    string FormDataJson);

public sealed record SaveWorkflowTaskFormDraftRequest(
    long ExpectedVersion,
    string FormDataJson,
    string FormDefinitionHash,
    string IdempotencyKey);

public sealed record WorkflowTaskFormValidationDto(
    bool IsValid,
    string ErrorCode,
    IReadOnlyList<string> Errors,
    string NormalizedJson,
    IReadOnlyList<string> VisibleFieldKeys);

public sealed record WorkflowTaskAiReviewResultDto(
    string AgentRunId,
    string? GatewayCallId,
    string Mode,
    bool ExternalModelCalled,
    bool RequiresConfirmation,
    string RegisteredAction,
    string Suggestion,
    IReadOnlyList<string> CheckedFieldKeys,
    DateTimeOffset CreatedAt);

public sealed record WorkflowConfigurationValidationDto(
    bool IsValid,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings,
    string ObjectTypeCode,
    int NodeCount,
    int AssigneeCount,
    int TerminalNodeCount,
    DateTimeOffset ValidatedAt);

public sealed record WorkflowConfigurationDto(
    string Id,
    string ConfigurationKey,
    string Name,
    string? Description,
    string ConfigurationType,
    int VersionNumber,
    string State,
    long Revision,
    string OrganizationId,
    string DepartmentId,
    string DepartmentName,
    string ObjectTypeCode,
    string StartNode,
    IReadOnlyList<WorkflowConfigurationNodeRequest> Nodes,
    string CreatedBy,
    string CreatedByDisplayName,
    DateTimeOffset CreatedAt,
    string? UpdatedBy,
    string? UpdatedByDisplayName,
    DateTimeOffset UpdatedAt,
    string? PublishedBy,
    string? PublishedByDisplayName,
    DateTimeOffset? PublishedAt,
    string? PublishedWorkflowDefinitionId,
    string? PublishedWorkflowVersionId,
    WorkflowConfigurationValidationDto? Validation,
    bool IsDefault = false,
    IReadOnlyList<string>? ApplicableRoleCodes = null,
    string RiskLevel = "Low",
    bool RequiresPlatformApproval = false,
    string EnvironmentCode = "demo",
    bool IsTestData = true);
