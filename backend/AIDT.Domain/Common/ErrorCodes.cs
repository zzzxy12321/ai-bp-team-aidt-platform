namespace AIDT.Domain.Common;

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string AuthenticationRequired = "AUTHENTICATION_REQUIRED";
    public const string PermissionDenied = "PERMISSION_DENIED";
    public const string DataScopeDenied = "DATA_SCOPE_DENIED";
    public const string DepartmentScopeDenied = "DEPARTMENT_SCOPE_DENIED";
    public const string ProjectScopeDenied = "PROJECT_SCOPE_DENIED";
    public const string ResourceNotFound = "RESOURCE_NOT_FOUND";
    public const string Conflict = "CONFLICT";
    public const string DuplicateObjectCode = "DUPLICATE_OBJECT_CODE";
    public const string WorkflowNotFound = "WORKFLOW_NOT_FOUND";
    public const string WorkflowVersionConflict = "WORKFLOW_VERSION_CONFLICT";
    public const string WorkflowStateInvalid = "WORKFLOW_STATE_INVALID";
    public const string WorkflowTemplateNotFound = "WORKFLOW_TEMPLATE_NOT_FOUND";
    public const string WorkflowTemplateDisabled = "WORKFLOW_TEMPLATE_DISABLED";
    public const string WorkflowTemplateDepartmentDenied = "WORKFLOW_TEMPLATE_DEPARTMENT_DENIED";
    public const string WorkflowFormDefinitionChanged = "WORKFLOW_FORM_DEFINITION_CHANGED";
    public const string WorkflowFormRequiredFieldMissing = "WORKFLOW_FORM_REQUIRED_FIELD_MISSING";
    public const string WorkflowFormAttachmentMissing = "WORKFLOW_FORM_ATTACHMENT_MISSING";
    public const string WorkflowAgentUnavailable = "WORKFLOW_AGENT_UNAVAILABLE";
    public const string WorkflowPermanentHumanBoundary = "WORKFLOW_PERMANENT_HUMAN_BOUNDARY";
    public const string WorkflowDuplicateSubmission = "WORKFLOW_DUPLICATE_SUBMISSION";
    public const string TaskAlreadyProcessed = "TASK_ALREADY_PROCESSED";
    public const string TaskAssigneeMismatch = "TASK_ASSIGNEE_MISMATCH";
    public const string AgentConfirmationRequired = "AGENT_CONFIRMATION_REQUIRED";
    public const string AgentLimitExceeded = "AGENT_LIMIT_EXCEEDED";
    public const string AgentStopped = "AGENT_STOPPED";
    public const string AgentProfileNotPublished = "AGENT_PROFILE_NOT_PUBLISHED";
    public const string AgentProfileInvalid = "AGENT_PROFILE_INVALID";
    public const string AgentHighRiskReviewRequired = "AGENT_HIGH_RISK_REVIEW_REQUIRED";
    public const string AgentSelfReviewDenied = "AGENT_SELF_REVIEW_DENIED";
    public const string PersistenceNotReady = "PERSISTENCE_NOT_READY";
    public const string FileScanRequired = "FILE_SCAN_REQUIRED";
    public const string FilePreviewUnsupported = "FILE_PREVIEW_UNSUPPORTED";
    public const string DocxParseFailed = "DOCX_PARSE_FAILED";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string ScenarioNotFound = "SCENARIO_NOT_FOUND";
    public const string ScenarioNotPublished = "SCENARIO_NOT_PUBLISHED";
    public const string ScenarioDisabled = "SCENARIO_DISABLED";
    public const string ScenarioVersionConflict = "SCENARIO_VERSION_CONFLICT";
    public const string ScenarioReferenceInvalid = "SCENARIO_REFERENCE_INVALID";
    public const string ScenarioMaterialMissing = "SCENARIO_MATERIAL_MISSING";
    public const string ScenarioBoundaryMissing = "SCENARIO_BOUNDARY_MISSING";
    public const string ScenarioLaunchFailed = "SCENARIO_LAUNCH_FAILED";
    public const string ScenarioDuplicateSubmission = "SCENARIO_DUPLICATE_SUBMISSION";
    public const string UnexpectedError = "UNEXPECTED_ERROR";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ValidationFailed,
        AuthenticationRequired,
        PermissionDenied,
        DataScopeDenied,
        DepartmentScopeDenied,
        ProjectScopeDenied,
        ResourceNotFound,
        Conflict,
        DuplicateObjectCode,
        WorkflowNotFound,
        WorkflowVersionConflict,
        WorkflowStateInvalid,
        WorkflowTemplateNotFound,
        WorkflowTemplateDisabled,
        WorkflowTemplateDepartmentDenied,
        WorkflowFormDefinitionChanged,
        WorkflowFormRequiredFieldMissing,
        WorkflowFormAttachmentMissing,
        WorkflowAgentUnavailable,
        WorkflowPermanentHumanBoundary,
        WorkflowDuplicateSubmission,
        TaskAlreadyProcessed,
        TaskAssigneeMismatch,
        AgentConfirmationRequired,
        AgentLimitExceeded,
        AgentStopped,
        AgentProfileNotPublished,
        AgentProfileInvalid,
        AgentHighRiskReviewRequired,
        AgentSelfReviewDenied,
        PersistenceNotReady,
        FileScanRequired,
        FilePreviewUnsupported,
        DocxParseFailed,
        ServiceUnavailable,
        ScenarioNotFound,
        ScenarioNotPublished,
        ScenarioDisabled,
        ScenarioVersionConflict,
        ScenarioReferenceInvalid,
        ScenarioMaterialMissing,
        ScenarioBoundaryMissing,
        ScenarioLaunchFailed,
        ScenarioDuplicateSubmission,
        UnexpectedError
    };
}
