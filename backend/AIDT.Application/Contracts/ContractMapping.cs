using AIDT.Domain.Agents;
using AIDT.Domain.Audit;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Workflows;

namespace AIDT.Application.Contracts;

public static class ContractMapping
{
    public static BusinessObjectDto ToDto(this BusinessObject businessObject) =>
        new(
            businessObject.Id.ToString("D"),
            businessObject.ObjectTypeId.ToString("D"),
            businessObject.ObjectCode,
            businessObject.Status.ToString(),
            businessObject.Version,
            new Dictionary<string, string?>(businessObject.Fields, StringComparer.Ordinal),
            businessObject.CreatedAt,
            businessObject.UpdatedAt,
            businessObject.DepartmentId.ToString("D"),
            businessObject.EnvironmentCode,
            businessObject.IsTestData,
            businessObject.SourceSystem,
            businessObject.Title,
            businessObject.CreatedByUserId?.ToString("D"));

    public static WorkflowInstanceDto ToDto(this WorkflowInstance instance) =>
        new(
            instance.Id.ToString("D"),
            instance.WorkflowDefinitionId.ToString("D"),
            instance.WorkflowVersionId.ToString("D"),
            instance.BusinessObjectId.ToString("D"),
            instance.Status,
            instance.Version,
            instance.CurrentNodeCode,
            instance.StartedAt,
            instance.EndedAt,
            instance.DepartmentId.ToString("D"),
            instance.StartedByUserId?.ToString("D"),
            instance.EnvironmentCode,
            instance.IsTestData,
            instance.SourceSystem);

    public static WorkflowTaskDto ToDto(this WorkflowTask task) =>
        new(
            task.Id.ToString("D"),
            task.WorkflowInstanceId.ToString("D"),
            task.NodeCode,
            task.AssigneeId.ToString("D"),
            task.Status,
            task.Version,
            task.DecisionComment,
            task.CreatedAt,
            task.CompletedAt,
            task.DepartmentId.ToString("D"),
            task.EnvironmentCode,
            task.IsTestData,
            task.DueAt,
            task.SlaStatus,
            task.ReminderSentAt,
            task.EscalatedAt,
            task.DecisionMode,
            task.AgentRunId?.ToString("D"));

    public static AuditEventDto ToDto(this AuditEvent auditEvent) =>
        new(
            auditEvent.Id.ToString("D"),
            auditEvent.ActorId.ToString("D"),
            auditEvent.Module,
            auditEvent.Action,
            auditEvent.EntityType,
            auditEvent.EntityId,
            auditEvent.TraceId,
            auditEvent.Outcome,
            auditEvent.OccurredAt,
            auditEvent.Reason);

    public static AgentRunDto ToDto(this AgentRun run) =>
        new(
            run.Id.ToString("D"),
            run.AgentProfileId.ToString("D"),
            run.RequestedBy.ToString("D"),
            run.Action,
            run.Amount,
            run.RiskLevel,
            run.Status,
            run.ConfirmationId?.ToString("D"),
            run.CreatedAt,
            run.OrganizationId.ToString("D"),
            run.DepartmentId.ToString("D"),
            run.EnvironmentCode,
            run.IsTestData,
            run.SubjectType,
            run.BusinessObjectId?.ToString("D"),
            run.WorkflowInstanceId?.ToString("D"),
            run.WorkflowTaskId?.ToString("D"),
            run.MinutesSourceId?.ToString("D"),
            run.Model,
            run.GatewayCallId?.ToString("D"),
            run.GatewayMode,
            run.ExternalModelCalled,
            run.ProxyTriggerMode,
            run.ProxyTriggeredAt,
            run.OriginalAssigneeId?.ToString("D"),
            run.CompensationStatus,
            run.ProxyIdempotencyKey);
}
