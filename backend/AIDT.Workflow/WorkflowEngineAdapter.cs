using System.Globalization;
using System.Text.Json;
using AIDT.Application.Abstractions;
using AIDT.Domain.BusinessObjects;
using AIDT.Domain.Common;
using AIDT.Domain.Workflows;

namespace AIDT.Workflow;

public sealed class WorkflowEngineAdapter(
    IWorkflowCatalogRepository workflowCatalogRepository,
    IBusinessObjectRepository businessObjectRepository,
    IObjectTypeRepository objectTypeRepository,
    IWorkflowAssignmentResolver assignmentResolver) : IWorkflowEngine
{
    public async Task<WorkflowStartResult> StartAsync(
        WorkflowVersionDefinition definition,
        BusinessObject businessObject,
        CancellationToken cancellationToken,
        Guid? initiatorUserId = null)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(businessObject);
        var blueprint = Parse(definition.DefinitionJson);
        var objectType = await objectTypeRepository.GetByIdAsync(
            businessObject.ObjectTypeId,
            cancellationToken) ?? throw new NotFoundException("业务对象类型不存在。");
        if (!string.Equals(blueprint.ObjectTypeCode, "ANY", StringComparison.Ordinal)
            && !string.Equals(blueprint.ObjectTypeCode, objectType.Code, StringComparison.Ordinal))
        {
            throw new ValidationException("业务对象类型与流程定义不匹配。");
        }

        var firstNode = ResolveNode(blueprint, blueprint.StartNode, businessObject);
        var assignment = await ResolveAssignmentAsync(
            firstNode,
            businessObject,
            initiatorUserId,
            cancellationToken);
        return new WorkflowStartResult(firstNode.Code, assignment.PrimaryAssigneeId, assignment);
    }

    public async Task<WorkflowAdvanceResult> AdvanceAsync(
        WorkflowInstance instance,
        WorkflowTask task,
        WorkflowDecision decision,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(task);
        if (decision != WorkflowDecision.Approve)
        {
            return new WorkflowAdvanceResult(null, null, instance.Status);
        }

        var definition = await workflowCatalogRepository.GetVersionAsync(
            instance.WorkflowVersionId,
            cancellationToken) ?? throw new NotFoundException("流程版本不存在。");
        var businessObject = await businessObjectRepository.GetByIdAsync(
            instance.BusinessObjectId,
            cancellationToken) ?? throw new NotFoundException("流程业务对象不存在。");
        var blueprint = Parse(definition.DefinitionJson);
        var currentNode = blueprint.Nodes.SingleOrDefault(node =>
            string.Equals(node.Code, task.NodeCode, StringComparison.Ordinal))
            ?? throw new ConflictException(ErrorCodes.WorkflowStateInvalid, "当前任务节点不在冻结流程版本中。");

        if (currentNode.CompleteOnApprove || string.IsNullOrWhiteSpace(currentNode.NextNode))
        {
            return new WorkflowAdvanceResult(null, null, WorkflowInstanceStatus.Completed);
        }

        var nextNode = ResolveNode(blueprint, currentNode.NextNode, businessObject);
        var assignment = await ResolveAssignmentAsync(
            nextNode,
            businessObject,
            instance.StartedByUserId,
            cancellationToken);
        return new WorkflowAdvanceResult(
            nextNode.Code,
            assignment.PrimaryAssigneeId,
            WorkflowInstanceStatus.Waiting,
            assignment);
    }

    private static WorkflowNodeDefinition ResolveNode(
        WorkflowDefinitionModel definition,
        string code,
        BusinessObject businessObject)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var currentCode = code;
        while (visited.Add(currentCode))
        {
            var node = definition.Nodes.SingleOrDefault(item =>
                string.Equals(item.Code, currentCode, StringComparison.Ordinal))
                ?? throw new ValidationException($"流程节点 {currentCode} 未定义。");
            if (node.Condition is null || Evaluate(node.Condition, businessObject))
            {
                return node;
            }

            if (string.IsNullOrWhiteSpace(node.NextNode))
            {
                throw new ValidationException($"条件节点 {node.Code} 缺少 fallback 节点。");
            }

            currentCode = node.NextNode;
        }

        throw new ValidationException("流程定义存在条件循环。");
    }

    private async Task<ResolvedWorkflowAssignment> ResolveAssignmentAsync(
        WorkflowNodeDefinition node,
        BusinessObject businessObject,
        Guid? initiatorUserId,
        CancellationToken cancellationToken)
    {
        var strategy = ParseStrategy(node.AssigneeStrategy);
        return await assignmentResolver.ResolveAsync(
            businessObject.OrganizationId,
            businessObject.DepartmentId,
            initiatorUserId,
            businessObject.Id,
            new WorkflowAssignmentRule(
                strategy,
                node.AssigneeUserName,
                node.BusinessRoleCode,
                node.AgentProfileId),
            cancellationToken);
    }

    private static WorkflowAssignmentStrategy ParseStrategy(string? value) =>
        Enum.TryParse<WorkflowAssignmentStrategy>(value, true, out var strategy)
            ? strategy
            : WorkflowAssignmentStrategy.SpecificUser;

    private static bool Evaluate(WorkflowCondition condition, BusinessObject businessObject)
    {
        if (!businessObject.Fields.TryGetValue(condition.Field, out var rawValue)
            || string.IsNullOrWhiteSpace(rawValue))
        {
            return false;
        }

        return condition.Operator switch
        {
            "equals" => string.Equals(rawValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            "notEquals" => !string.Equals(rawValue, condition.Value, StringComparison.OrdinalIgnoreCase),
            "greaterThan" => decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var left)
                && decimal.TryParse(condition.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var right)
                && left > right,
            "greaterThanOrEqual" => decimal.TryParse(rawValue, NumberStyles.Number, CultureInfo.InvariantCulture, out var left)
                && decimal.TryParse(condition.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var right)
                && left >= right,
            _ => throw new ValidationException($"不支持的条件操作符 {condition.Operator}。")
        };
    }

    private static WorkflowDefinitionModel Parse(string json)
    {
        try
        {
            var result = JsonSerializer.Deserialize<WorkflowDefinitionModel>(
                json,
                JsonSerializerOptions.Web);
            if (result is null || result.Nodes.Count == 0)
            {
                throw new ValidationException("流程定义没有节点。");
            }

            return result;
        }
        catch (JsonException exception)
        {
            throw new ValidationException($"流程定义 JSON 无效：{exception.Message}");
        }
    }
}

internal sealed record WorkflowDefinitionModel(
    string ObjectTypeCode,
    string StartNode,
    IReadOnlyList<WorkflowNodeDefinition> Nodes);

internal sealed record WorkflowNodeDefinition(
    string Code,
    string Name,
    string? AssigneeUserName,
    string? NextNode,
    bool CompleteOnApprove,
    WorkflowCondition? Condition,
    string? AssigneeStrategy = null,
    string? BusinessRoleCode = null,
    Guid? AgentProfileId = null);

internal sealed record WorkflowCondition(string Field, string Operator, string Value);
