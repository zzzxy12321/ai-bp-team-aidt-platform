import { flushPromises, mount } from "@vue/test-utils";
import { createPinia, setActivePinia } from "pinia";
import { createMemoryHistory, createRouter } from "vue-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ScenariosPage from "./ScenariosPage.vue";
import { scenarioApi } from "@/api/modules/scenarioApi";
import { workspaceApi } from "@/api/modules/workspaceApi";
import { useAuthStore } from "@/stores/auth";

vi.mock("@/api/modules/scenarioApi", () => ({
  scenarioApi: {
    listVisible: vi.fn(),
    myLaunches: vi.fn(),
  },
}));

vi.mock("@/api/modules/workspaceApi", () => ({
  workspaceApi: {
    departments: vi.fn(),
    publishedObjectTypes: vi.fn(),
    agents: vi.fn(),
  },
}));

const scenario = {
  id: "81000000-0000-0000-0000-000000000001",
  code: "SCN-RND-SAMPLE-TO-PRODUCTION",
  name: "研发样品到转产",
  description: "从客户样品和技术需求发起，完成转产人工确认。",
  ownerDepartmentId: "department-1",
  ownerDepartmentName: "研发",
  lifecycleStatus: "Published" as const,
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
    id: "version-1",
    scenarioId: "81000000-0000-0000-0000-000000000001",
    versionNumber: 1,
    status: "Published" as const,
    revision: 1,
    payload: {
      problemSolved: "统一样品与转产证据",
      startMode: "CreateBusinessObjectAndWorkflow" as const,
      triggerDescription: "收到客户样品时",
      applicableDepartmentCodes: ["RND"],
      participantRoles: ["研发工程师", "质量工程师"],
      objectTypeCodes: ["CUSTOMER_SAMPLE"],
      workflowDefinitionCode: "CUSTOMER_SAMPLE_TO_PRODUCTION",
      workflowVersionNumber: 1,
      steps: [
        {
          sequence: 1,
          code: "RD_INITIATE",
          name: "研发发起",
          businessDescription: "登记样品",
          responsibleRole: "研发工程师",
          action: "创建样品对象",
          expectedResult: "形成样品记录",
          evidence: "样品对象",
          permanentHumanBoundary: false,
        },
      ],
      preparationItems: [
        {
          code: "SAMPLE_FILE",
          name: "技术材料",
          isRequired: true,
          sourceType: "Attachment",
          description: "受控附件",
        },
      ],
      supportedFileSources: ["Attachment"],
      supportedMinutesSources: ["MinutesSource"],
      allowedAgents: [],
      permanentHumanBoundaries: ["配方变更"],
      permissionRequirements: [],
      completionEvidence: ["样品对象", "流程任务", "审计证据"],
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
};

let pinia: ReturnType<typeof createPinia>;

describe("ScenariosPage", () => {
  beforeEach(async () => {
    vi.clearAllMocks();
    pinia = createPinia();
    setActivePinia(pinia);
    useAuthStore().permissions = [{ resource: "scenario", action: "Read" }];
    vi.mocked(scenarioApi.listVisible).mockResolvedValue({
      items: [scenario],
      page: 1,
      pageSize: 100,
      total: 1,
    });
    vi.mocked(scenarioApi.myLaunches).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 100,
      total: 0,
    });
    vi.mocked(workspaceApi.departments).mockResolvedValue([
      {
        id: "department-1",
        code: "RND",
        name: "研发",
        managerUserId: null,
        isActive: true,
        isPrimary: true,
      },
    ]);
    vi.mocked(workspaceApi.publishedObjectTypes).mockResolvedValue([]);
    vi.mocked(workspaceApi.agents).mockResolvedValue([]);
  });

  it("renders authorized scenarios from the 7A API with business language", async () => {
    const router = createRouter({
      history: createMemoryHistory(),
      routes: [{ path: "/scenarios", component: { template: "<div />" } }],
    });
    await router.push("/scenarios");
    await router.isReady();
    const wrapper = mount(ScenariosPage, {
      global: {
        plugins: [pinia, router],
        stubs: {
          ElButton: { template: "<button><slot /></button>" },
          ElInput: true,
          ElSelect: true,
          ElOption: true,
          ElTag: { template: "<span><slot /></span>" },
          ElAlert: true,
          ElEmpty: { template: "<div><slot /></div>" },
          ElDialog: { template: "<div><slot /></div>" },
          ScenarioStartWizard: true,
          ScenarioManagementPanel: true,
        },
        directives: { loading: {} },
      },
    });

    await flushPromises();

    expect(scenarioApi.listVisible).toHaveBeenCalledTimes(1);
    expect(scenarioApi.myLaunches).toHaveBeenCalledTimes(1);
    expect(wrapper.text()).toContain("研发样品到转产");
    expect(wrapper.text()).toContain("研发工程师、质量工程师");
    expect(wrapper.text()).toContain("受控 Demo 试点样例");
    expect(wrapper.text()).not.toContain(scenario.code);
  });
});
