import { describe, expect, it } from "vitest";
import type { Scenario } from "@/api/types/contracts";
import {
  businessLinkTarget,
  scenarioCategory,
  scenarioErrorGuidance,
  scenarioRequiresApproval,
} from "./scenarioPresentation";

const scenario = {
  id: "81000000-0000-0000-0000-000000000001",
  code: "SCN-RND-SAMPLE-TO-PRODUCTION",
  name: "研发样品到转产",
  description: "从客户样品发起，完成转产人工确认。",
  ownerDepartmentId: "d1",
  ownerDepartmentName: "研发",
  lifecycleStatus: "Published",
  version: 2,
  revision: 2,
  createdAt: "2026-11-03T00:00:00Z",
  createdBy: "user",
  createdByDisplayName: "管理员",
  updatedAt: "2026-11-03T00:00:00Z",
  updatedBy: null,
  updatedByDisplayName: null,
  disabledBy: null,
  disabledByDisplayName: null,
  disabledAt: null,
  disabledReason: null,
  canManage: false,
  pendingApprovalRequestId: null,
  pendingApprovalStatus: null,
  draftVersion: null,
  currentPublishedVersion: {
    id: "version",
    scenarioId: "81000000-0000-0000-0000-000000000001",
    versionNumber: 1,
    status: "Published",
    revision: 1,
    payload: {
      problemSolved: "统一样品证据",
      startMode: "CreateBusinessObjectAndWorkflow",
      triggerDescription: "收到样品时",
      applicableDepartmentCodes: ["RND"],
      participantRoles: ["研发工程师", "质量工程师"],
      objectTypeCodes: ["CUSTOMER_SAMPLE"],
      workflowDefinitionCode: "CUSTOMER_SAMPLE_TO_PRODUCTION",
      workflowVersionNumber: 1,
      steps: [],
      preparationItems: [],
      supportedFileSources: ["Attachment"],
      supportedMinutesSources: ["MinutesSource"],
      allowedAgents: [],
      permanentHumanBoundaries: ["配方变更"],
      permissionRequirements: [],
      completionEvidence: ["对象", "审计"],
    },
    references: null,
    validation: null,
    changeSummary: null,
    createdBy: "user",
    createdByDisplayName: "管理员",
    createdAt: "2026-11-03T00:00:00Z",
    updatedBy: null,
    updatedByDisplayName: null,
    updatedAt: "2026-11-03T00:00:00Z",
    publishedBy: "user",
    publishedByDisplayName: "管理员",
    publishedAt: "2026-11-03T00:00:00Z",
    retiredBy: null,
    retiredByDisplayName: null,
    retiredAt: null,
  },
} as Scenario;

describe("scenario presentation", () => {
  it("maps technical modes into business categories and approval language", () => {
    expect(scenarioCategory(scenario)).toBe("研发与转产");
    expect(scenarioRequiresApproval(scenario)).toBe(true);
  });

  it("maps evidence records to existing business pages", () => {
    expect(
      businessLinkTarget({
        type: "WorkflowInstance",
        id: "instance-1",
        name: "样品转产流程",
        role: "流程执行",
        detailPath: "/api/v1/workflow-instances/instance-1",
      }),
    ).toBe("/workflows?instance=instance-1");
    expect(
      businessLinkTarget({
        type: "Attachment",
        id: "file-1",
        name: "检测材料",
        role: "受控附件",
        detailPath: "/api/v1/files/file-1",
      }),
    ).toBe("/documents?file=file-1");
  });

  it("explains why a disabled scenario cannot start and who to contact", () => {
    const message = scenarioErrorGuidance(
      "SCENARIO_DISABLED",
      "当前场景已停用。",
    );
    expect(message).toContain("不能再发起");
    expect(message).toContain("部门负责人");
  });
});
