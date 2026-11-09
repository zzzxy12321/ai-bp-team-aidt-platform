using AIDT.Application.Contracts;
using AIDT.Domain.Identity;
using AIDT.Domain.Scenarios;
using AIDT.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AIDT.Infrastructure.Services;

public sealed partial class SqlScenarioService
{
    public async Task<PagedResult<ScenarioLaunchSummaryDto>> ListMyLaunchesAsync(
        UserPrincipal user,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        EnsureRead(user);
        var normalizedPage = Math.Max(1, page);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.ScenarioLaunches.AsNoTracking()
            .Where(item =>
                item.OrganizationId == user.OrganizationId
                && item.StartedBy == user.UserId)
            .OrderByDescending(item => item.StartedAt)
            .ThenByDescending(item => item.Id);
        var total = await query.LongCountAsync(cancellationToken);
        var launches = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);
        if (launches.Count == 0)
        {
            return new PagedResult<ScenarioLaunchSummaryDto>(
                [],
                normalizedPage,
                normalizedPageSize,
                total);
        }

        var scenarioIds = launches.Select(item => item.ScenarioId).Distinct().ToArray();
        var versionIds = launches.Select(item => item.ScenarioVersionId).Distinct().ToArray();
        var departmentIds = launches.Select(item => item.DepartmentId).Distinct().ToArray();
        var workflowInstanceIds = launches
            .Where(item => item.WorkflowInstanceId.HasValue)
            .Select(item => item.WorkflowInstanceId!.Value)
            .Distinct()
            .ToArray();
        var projectTaskIds = launches
            .Where(item => item.ProjectTaskId.HasValue)
            .Select(item => item.ProjectTaskId!.Value)
            .Distinct()
            .ToArray();
        var eventIds = launches
            .Where(item => item.EventInboxId.HasValue)
            .Select(item => item.EventInboxId!.Value)
            .Distinct()
            .ToArray();
        var agentRunIds = launches
            .Where(item => item.AgentRunId.HasValue)
            .Select(item => item.AgentRunId!.Value)
            .Distinct()
            .ToArray();
        var projectIds = launches
            .Where(item => item.ProjectId.HasValue)
            .Select(item => item.ProjectId!.Value)
            .Distinct()
            .ToArray();

        var scenarios = await dbContext.ScenarioDefinitions.AsNoTracking()
            .Where(item => scenarioIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var versions = await dbContext.ScenarioVersions.AsNoTracking()
            .Where(item => versionIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var departments = await dbContext.Departments.AsNoTracking()
            .Where(item => departmentIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var workflowTasks = await dbContext.WorkflowTasks.AsNoTracking()
            .Where(item => workflowInstanceIds.Contains(item.WorkflowInstanceId))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var activeWorkflowTasks = (await dbContext.WorkflowTasks.AsNoTracking()
            .Where(item =>
                workflowInstanceIds.Contains(item.WorkflowInstanceId)
                && (item.Status == "Pending" || item.Status == "InProgress"))
            .OrderByDescending(item => item.CreatedAt)
            .ToListAsync(cancellationToken))
            .GroupBy(item => item.WorkflowInstanceId)
            .ToDictionary(group => group.Key, group => group.First());
        var workflowInstances = await dbContext.WorkflowInstances.AsNoTracking()
            .Where(item => workflowInstanceIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var projectTasks = await dbContext.ProjectTasks.AsNoTracking()
            .Where(item => projectTaskIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var events = await dbContext.EventInboxes.AsNoTracking()
            .Where(item => eventIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var agentRuns = await dbContext.AgentRuns.AsNoTracking()
            .Where(item => agentRunIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var projects = await dbContext.BusinessObjects.AsNoTracking()
            .Where(item => projectIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var assigneeIds = workflowTasks.Values.Select(item => item.AssigneeId)
            .Concat(projectTasks.Values.Select(item => item.AssigneeId))
            .Distinct()
            .ToArray();
        var assignees = await LoadUserNamesAsync(assigneeIds, cancellationToken);

        var items = launches.Select(launch =>
        {
            scenarios.TryGetValue(launch.ScenarioId, out var scenario);
            versions.TryGetValue(launch.ScenarioVersionId, out var version);
            var payload = version is null
                ? EmptyPayload()
                : DeserializePayload(version.PayloadJson);
            var workflowTask = launch.WorkflowInstanceId.HasValue
                ? activeWorkflowTasks.GetValueOrDefault(launch.WorkflowInstanceId.Value)
                : null;
            if (workflowTask is null)
            {
                workflowTasks.TryGetValue(launch.WorkflowTaskId ?? Guid.Empty, out workflowTask);
            }
            workflowInstances.TryGetValue(launch.WorkflowInstanceId ?? Guid.Empty, out var workflowInstance);
            projectTasks.TryGetValue(launch.ProjectTaskId ?? Guid.Empty, out var projectTask);
            events.TryGetValue(launch.EventInboxId ?? Guid.Empty, out var eventInbox);
            agentRuns.TryGetValue(launch.AgentRunId ?? Guid.Empty, out var agentRun);
            projects.TryGetValue(launch.ProjectId ?? Guid.Empty, out var project);

            var uploadedMaterialCount = DeserializeIds(launch.AttachmentIdsJson).Count;
            var missingMaterials = BuildMissingMaterials(payload, launch, uploadedMaterialCount);
            var workflowStatus = workflowInstance is null
                ? "未使用审批流程"
                : LabelWorkflowStatus(workflowInstance.Status);
            var taskStatus = workflowTask is not null
                ? LabelTaskStatus(workflowTask.Status)
                : projectTask is not null
                    ? LabelProjectTaskStatus(projectTask.Status)
                    : eventInbox is not null
                        ? LabelEventStatus(eventInbox.Status)
                        : "未生成待办";
            var activeAssigneeId = workflowTask?.AssigneeId ?? projectTask?.AssigneeId;
            var waitingFor = activeAssigneeId.HasValue
                && assignees.TryGetValue(activeAssigneeId.Value, out var assigneeName)
                ? assigneeName
                : DetermineWaitingFor(workflowTask, projectTask, eventInbox, workflowInstance);
            var currentStep = ResolveCurrentStep(payload, workflowTask, projectTask, eventInbox, workflowInstance);
            var dueAt = workflowTask?.DueAt ?? projectTask?.DueAt;
            var completed = IsLaunchCompleted(workflowTask, projectTask, eventInbox, workflowInstance);
            var projectName = project is null
                ? "未关联项目"
                : !string.IsNullOrWhiteSpace(project.Title)
                    ? project.Title
                    : project.ObjectCode;

            return new ScenarioLaunchSummaryDto(
                launch.Id.ToString("D"),
                launch.ScenarioId.ToString("D"),
                scenario?.Code ?? "场景",
                scenario?.Name ?? "业务场景",
                launch.DepartmentId.ToString("D"),
                departments.GetValueOrDefault(launch.DepartmentId) ?? "所属部门",
                launch.EnvironmentCode,
                string.Equals(launch.EnvironmentCode, "demo", StringComparison.OrdinalIgnoreCase)
                    || launch.IsTestData,
                launch.StartedAt,
                currentStep,
                waitingFor,
                dueAt,
                uploadedMaterialCount,
                missingMaterials,
                workflowStatus,
                taskStatus,
                projectName,
                DescribeAgentSuggestion(agentRun),
                BuildNextAction(completed, workflowTask, projectTask, eventInbox, waitingFor, missingMaterials),
                completed,
                DeserializeBusinessLinks(launch.BusinessLinksJson),
                $"/audit?entityType=ScenarioLaunch&entityId={launch.Id:D}");
        }).ToList();

        return new PagedResult<ScenarioLaunchSummaryDto>(
            items,
            normalizedPage,
            normalizedPageSize,
            total);
    }

    private static ScenarioDefinitionPayloadDto EmptyPayload() =>
        new(
            string.Empty,
            ScenarioStartMode.CreateBusinessObjectAndWorkflow,
            string.Empty,
            [],
            [],
            [],
            null,
            null,
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);

    private static List<string> BuildMissingMaterials(
        ScenarioDefinitionPayloadDto payload,
        ScenarioLaunchEntity launch,
        int uploadedMaterialCount)
    {
        var missing = new List<string>();
        var requiredAttachments = payload.PreparationItems
            .Where(item => item.IsRequired
                && item.SourceType is "Attachment" or "File" or "Document")
            .ToList();
        if (requiredAttachments.Count > uploadedMaterialCount)
        {
            missing.AddRange(requiredAttachments.Skip(uploadedMaterialCount).Select(item => item.Name));
        }

        foreach (var item in payload.PreparationItems.Where(item => item.IsRequired))
        {
            var isPresent = item.SourceType switch
            {
                "BusinessObject" => launch.BusinessObjectId.HasValue,
                "Project" => launch.ProjectId.HasValue,
                "EventInbox" => launch.EventInboxId.HasValue,
                "Attendance" or "MinutesSource" => DeserializeIds(launch.MinutesSourceIdsJson).Count > 0,
                _ => true
            };
            if (!isPresent && !missing.Contains(item.Name, StringComparer.Ordinal))
            {
                missing.Add(item.Name);
            }
        }

        return missing;
    }

    private static string ResolveCurrentStep(
        ScenarioDefinitionPayloadDto payload,
        WorkflowTaskEntity? workflowTask,
        ProjectTaskEntity? projectTask,
        EventInboxEntity? eventInbox,
        WorkflowInstanceEntity? workflowInstance)
    {
        if (workflowTask is not null
            && workflowTask.Status is "Pending" or "InProgress")
        {
            var matched = payload.Steps.FirstOrDefault(step =>
                string.Equals(step.Code, workflowTask.NodeCode, StringComparison.OrdinalIgnoreCase));
            return matched?.Name ?? "业务审批处理中";
        }

        if (projectTask is not null && projectTask.Status == "Pending")
        {
            return payload.Steps.Count > 0 ? payload.Steps[0].Name : "项目任务执行";
        }

        if (eventInbox is not null)
        {
            return eventInbox.Status == "Confirmed"
                ? "经理已完成分派"
                : payload.Steps.Count > 0 ? payload.Steps[0].Name : "事项确认";
        }

        if (workflowInstance?.Status is "Completed" or "Terminated")
        {
            return "业务流程已结束";
        }

        return payload.Steps.Count > 0 ? payload.Steps[^1].Name : "业务处理";
    }

    private static string DetermineWaitingFor(
        WorkflowTaskEntity? workflowTask,
        ProjectTaskEntity? projectTask,
        EventInboxEntity? eventInbox,
        WorkflowInstanceEntity? workflowInstance)
    {
        if (workflowTask?.Status is "Pending" or "InProgress") return "当前处理人";
        if (projectTask?.Status == "Pending") return "项目任务负责人";
        if (eventInbox?.Status == "Received") return "部门负责人";
        if (workflowInstance?.Status == "Running") return "下一流程节点处理人";
        return "无需等待";
    }

    private static bool IsLaunchCompleted(
        WorkflowTaskEntity? workflowTask,
        ProjectTaskEntity? projectTask,
        EventInboxEntity? eventInbox,
        WorkflowInstanceEntity? workflowInstance)
    {
        if (workflowInstance is not null)
        {
            return workflowInstance.Status is "Completed" or "Terminated";
        }
        if (projectTask is not null)
        {
            return projectTask.Status is "Completed" or "Cancelled";
        }
        if (eventInbox is not null)
        {
            return eventInbox.Status == "Confirmed";
        }
        return workflowTask?.Status is "Approved" or "Returned" or "Terminated";
    }

    private static string BuildNextAction(
        bool completed,
        WorkflowTaskEntity? workflowTask,
        ProjectTaskEntity? projectTask,
        EventInboxEntity? eventInbox,
        string waitingFor,
        List<string> missingMaterials)
    {
        if (missingMaterials.Count > 0) return $"补充材料：{string.Join("、", missingMaterials.Take(3))}";
        if (completed) return "查看结果与完整证据";
        if (workflowTask?.Status is "Pending" or "InProgress") return $"等待 {waitingFor} 在原待办中处理";
        if (projectTask?.Status == "Pending") return $"等待 {waitingFor} 完成项目任务";
        if (eventInbox?.Status == "Received") return "等待部门负责人确认负责人、期限和流程";
        return "打开流程或业务对象查看下一步";
    }

    private static string DescribeAgentSuggestion(AgentRunEntity? run)
    {
        if (run is null) return "未请求数字员工";
        return run.Status switch
        {
            "AwaitingConfirmation" => "已生成建议，等待真人确认",
            "Completed" => "已生成建议，不代替任何人工决定",
            "Failed" => "数字员工不可用，请转人工处理",
            "Stopped" => "数字员工已停止，需人工接管",
            _ => "数字员工正在生成建议"
        };
    }

    private static string LabelWorkflowStatus(string status) => status switch
    {
        "Running" => "进行中",
        "Completed" => "已完成",
        "Terminated" => "已终止",
        _ => "处理中"
    };

    private static string LabelTaskStatus(string status) => status switch
    {
        "Pending" => "待处理",
        "InProgress" => "处理中",
        "Approved" => "已通过",
        "Returned" => "已退回",
        "Terminated" => "已终止",
        _ => "状态更新中"
    };

    private static string LabelProjectTaskStatus(string status) => status switch
    {
        "Pending" => "项目任务待处理",
        "Completed" => "项目任务已完成",
        "Cancelled" => "项目任务已取消",
        _ => "项目任务处理中"
    };

    private static string LabelEventStatus(string status) => status switch
    {
        "Received" => "待经理确认",
        "Confirmed" => "经理已确认",
        _ => "事项处理中"
    };
}
