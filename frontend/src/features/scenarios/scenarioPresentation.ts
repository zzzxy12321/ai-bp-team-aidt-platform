import type {
  Scenario,
  ScenarioBusinessLink,
  ScenarioLaunch,
  ScenarioStartMode,
} from "@/api/types/contracts";

export function scenarioCategory(scenario: Scenario): string {
  const payload = scenario.currentPublishedVersion?.payload;
  if (!payload) return "业务协同";
  if (payload.objectTypeCodes.includes("QUALITY_DEVIATION")) return "质量管理";
  if (
    payload.objectTypeCodes.includes("PURCHASE_REQUEST") ||
    payload.workflowDefinitionCode === "FINANCE_PAYMENT_REVIEW"
  ) {
    return "采购与财务";
  }
  if (payload.objectTypeCodes.includes("CUSTOMER_SAMPLE")) return "研发与转产";
  if (payload.startMode === "ProjectTask") return "项目与风险";
  if (payload.startMode === "EventInbox") return "部门协同";
  return "其他业务流程";
}

export function startModeLabel(mode: ScenarioStartMode): string {
  return {
    CreateBusinessObjectAndWorkflow: "新建业务事项并进入审批",
    ExistingBusinessObjectAndWorkflow: "选择已有业务事项并进入复核",
    EventInbox: "收集事项后由经理确认分派",
    ProjectTask: "在现有项目中创建行动任务",
  }[mode];
}

export function scenarioPositions(scenario: Scenario): string {
  const roles =
    scenario.currentPublishedVersion?.payload.participantRoles ?? [];
  return roles.length ? roles.join("、") : "当前部门业务人员";
}

export function scenarioRequiresApproval(scenario: Scenario): boolean {
  return Boolean(
    scenario.currentPublishedVersion?.payload.workflowDefinitionCode,
  );
}

export function scenarioUsesAgent(scenario: Scenario): boolean {
  return Boolean(
    scenario.currentPublishedVersion?.payload.allowedAgents.length,
  );
}

export function scenarioRiskText(scenario: Scenario): string {
  const payload = scenario.currentPublishedVersion?.payload;
  if (!payload) return "启动前需完成权限和风险检查";
  const highRisk =
    payload.steps.some((step) => step.permanentHumanBoundary) ||
    payload.permanentHumanBoundaries.some((boundary) =>
      ["财务付款", "质量放行", "配方变更"].includes(boundary),
    );
  return highRisk
    ? "涉及财务付款、质量放行或配方变更等永久人工边界，数字员工不能代替签认。"
    : "关键决定由真人确认；数字员工只可提供建议、草稿和风险提示。";
}

export function businessLinkTarget(
  link: ScenarioBusinessLink,
  launch?: ScenarioLaunch | null,
): string {
  switch (link.type) {
    case "BusinessObject":
      return `/objects?focus=${link.id}`;
    case "WorkflowInstance":
      return `/workflows?instance=${link.id}`;
    case "WorkflowTask":
      return "/workflows";
    case "Project":
      return `/projects/${link.id}`;
    case "ProjectTask":
      return launch?.projectId
        ? `/projects/${launch.projectId}?task=${link.id}`
        : "/my-work";
    case "EventInbox":
      return `/inbox?event=${link.id}`;
    case "AgentRun":
      return `/agents?run=${link.id}`;
    case "Attachment":
      return `/documents?file=${link.id}`;
    case "MinutesSource":
      return `/minutes?source=${link.id}`;
    default:
      return "/scenarios";
  }
}

export function scenarioErrorGuidance(code: string, message: string): string {
  const guidance: Record<string, string> = {
    SCENARIO_DISABLED:
      "该场景已停用，不能再发起新事项。历史记录仍可查看；请联系场景所属部门负责人恢复或选择其他可用场景。",
    SCENARIO_NOT_PUBLISHED:
      "该场景当前没有有效发布版本。请联系部门负责人完成校验和发布，或选择其他可用场景。",
    SCENARIO_REFERENCE_INVALID:
      "场景引用的对象、流程或数字员工已失效。请联系平台管理员重新校验并发布场景。",
    WORKFLOW_NOT_FOUND:
      "场景引用的流程版本已失效。请联系平台管理员重新选择有效流程版本并发布场景。",
    AGENT_PROFILE_NOT_PUBLISHED:
      "场景引用的数字员工已停用或未发布。可关闭数字员工建议后继续，或联系平台管理员恢复身份。",
    SCENARIO_MATERIAL_MISSING:
      "启动所需材料不完整。请补传必填文件或听记，再重新检查。",
    SCENARIO_AGENT_UNAVAILABLE:
      "所选数字员工未发布或已停用。可关闭数字员工建议后继续，或联系平台管理员恢复身份。",
    SCENARIO_DUPLICATE_SUBMISSION:
      "本次发起已经受理，无需重复提交。请到“我的场景”查看同一业务事项。",
    SCENARIO_VERSION_CONFLICT:
      "场景配置已更新。请刷新后重新确认，历史流程仍按启动时版本执行。",
    VALIDATION_FAILED:
      "提交内容未通过业务校验。请按页面提示补齐必填信息后重试。",
    CONFLICT:
      "业务状态已被其他人更新。请刷新页面查看最新状态后再操作，避免覆盖他人处理。",
    PERMISSION_DENIED:
      "当前岗位没有执行此动作的权限。请联系部门负责人或平台管理员授权，不能绕过审批边界。",
    DEPARTMENT_SCOPE_DENIED:
      "所选部门不在你的数据范围内。请切换到获授权部门，或联系部门负责人处理。",
    RESOURCE_NOT_FOUND:
      "目标业务对象不存在、已失效或无权访问。请联系事项所属部门确认。",
    NETWORK_UNAVAILABLE:
      "暂时无法连接业务服务。请确认网络后重试，不要改用其他入口重复提交。",
  };
  return guidance[code]
    ? `${message} ${guidance[code]}`
    : `${message} 如仍无法继续，请联系部门负责人或平台管理员。`;
}
