<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { ApiError } from "@/api/http";
import { scenarioApi } from "@/api/modules/scenarioApi";
import { workspaceApi } from "@/api/modules/workspaceApi";
import type {
  AgentProfileDto,
  DepartmentDto,
  ObjectTypeDto,
  Scenario,
  ScenarioApprovalRequest,
  ScenarioDefinitionPayload,
  ScenarioPreparationItem,
  ScenarioStartMode,
  ScenarioStepDefinition,
  ScenarioVersion,
  WorkflowDefinitionDto,
} from "@/api/types/contracts";
import { scenarioErrorGuidance } from "./scenarioPresentation";
import { useAuthStore } from "@/stores/auth";

const auth = useAuthStore();
const loading = ref(true);
const actionLoading = ref("");
const errorMessage = ref("");
const scenarios = ref<Scenario[]>([]);
const departments = ref<DepartmentDto[]>([]);
const objectTypes = ref<ObjectTypeDto[]>([]);
const workflows = ref<WorkflowDefinitionDto[]>([]);
const agents = ref<AgentProfileDto[]>([]);
const approvals = ref<ScenarioApprovalRequest[]>([]);
const selected = ref<Scenario | null>(null);
const versions = ref<ScenarioVersion[]>([]);
const versionsVisible = ref(false);
const createVisible = ref(false);
const validationVisible = ref(false);
const validation = ref<{ errors: string[]; warnings: string[] } | null>(null);

const permanentBoundaries = [
  "财务付款",
  "质量放行",
  "配方变更",
  "设备控制",
  "安全联锁",
  "外部合同承诺",
  "客户重大赔付",
];

const canCreate = computed(() => auth.can("scenario:create"));
const canUpdate = computed(() => auth.can("scenario:update"));
const canManage = computed(() => auth.can("scenario:manage"));
const canApprove = computed(() => auth.can("scenario:approve"));
const editableVersion = computed(
  () =>
    selected.value?.draftVersion ??
    (selected.value?.currentPublishedVersion ? null : null),
);
const isEditable = computed(() =>
  Boolean(
    editableVersion.value &&
    canUpdate.value &&
    ["Draft", "Validated"].includes(editableVersion.value.status),
  ),
);

const form = reactive({
  name: "",
  description: "",
  ownerDepartmentId: "",
  code: "",
  problemSolved: "",
  triggerDescription: "",
  startMode: "EventInbox" as ScenarioStartMode,
  applicableDepartmentCodes: [] as string[],
  participantRoles: [] as string[],
  objectTypeCodes: [] as string[],
  workflowDefinitionCode: null as string | null,
  workflowVersionNumber: null as number | null,
  steps: [] as ScenarioStepDefinition[],
  preparationItems: [] as ScenarioPreparationItem[],
  supportedFileSources: ["Attachment"] as string[],
  supportedMinutesSources: ["MinutesSource"] as string[],
  allowedAgents: [] as ScenarioDefinitionPayload["allowedAgents"],
  permanentHumanBoundaries: [...permanentBoundaries],
  permissionRequirements: [] as string[],
  completionEvidence: [] as string[],
});

function errorText(error: unknown) {
  return error instanceof ApiError
    ? scenarioErrorGuidance(error.code, error.message)
    : "操作未完成，请稍后重试。";
}

function payloadFromForm(): ScenarioDefinitionPayload {
  return {
    problemSolved: form.problemSolved,
    startMode: form.startMode,
    triggerDescription: form.triggerDescription,
    applicableDepartmentCodes: [...form.applicableDepartmentCodes],
    participantRoles: [...form.participantRoles],
    objectTypeCodes: [...form.objectTypeCodes],
    workflowDefinitionCode: form.workflowDefinitionCode,
    workflowVersionNumber: form.workflowVersionNumber,
    steps: form.steps.map((step, index) => ({
      ...step,
      sequence: index + 1,
      code: step.code.trim().toUpperCase(),
      name: step.name.trim(),
      businessDescription: step.businessDescription.trim(),
      responsibleRole: step.responsibleRole.trim(),
      action: step.action.trim(),
      expectedResult: step.expectedResult.trim(),
      evidence: step.evidence.trim(),
    })),
    preparationItems: form.preparationItems.map((item) => ({
      ...item,
      code: item.code.trim().toUpperCase(),
      name: item.name.trim(),
      description: item.description.trim(),
    })),
    supportedFileSources: [...form.supportedFileSources],
    supportedMinutesSources: [...form.supportedMinutesSources],
    allowedAgents: form.allowedAgents.map((item) => ({ ...item })),
    permanentHumanBoundaries: [...permanentBoundaries],
    permissionRequirements: [...form.permissionRequirements],
    completionEvidence: [...form.completionEvidence],
  };
}

function defaultPayload(): ScenarioDefinitionPayload {
  return {
    problemSolved: "",
    startMode: "EventInbox",
    triggerDescription: "",
    applicableDepartmentCodes: [],
    participantRoles: ["部门负责人", "事项发起人"],
    objectTypeCodes: [],
    workflowDefinitionCode: null,
    workflowVersionNumber: null,
    steps: [
      {
        sequence: 1,
        code: "INTAKE",
        name: "收集业务事项",
        businessDescription: "保留事项原文、来源和附件。",
        responsibleRole: "事项发起人",
        action: "提交业务事项和材料",
        expectedResult: "形成可追踪事项",
        evidence: "事项原文和附件",
        permanentHumanBoundary: false,
      },
    ],
    preparationItems: [
      {
        code: "BUSINESS_EVIDENCE",
        name: "业务材料",
        isRequired: false,
        sourceType: "Attachment",
        description: "受控附件",
      },
    ],
    supportedFileSources: ["Attachment"],
    supportedMinutesSources: ["MinutesSource"],
    allowedAgents: [],
    permanentHumanBoundaries: [...permanentBoundaries],
    permissionRequirements: ["event-inbox:Create", "event-inbox:Confirm"],
    completionEvidence: ["业务对象、流程任务和审计"],
  };
}

function fillForm(
  scenario: Scenario | null,
  version: ScenarioVersion | null,
  fallback?: ScenarioDefinitionPayload,
) {
  const payload = version?.payload ?? fallback ?? defaultPayload();
  form.code = scenario?.code ?? "";
  form.name = scenario?.name ?? "";
  form.description = scenario?.description ?? "";
  form.ownerDepartmentId =
    scenario?.ownerDepartmentId ??
    departments.value.find((item) => item.isPrimary)?.id ??
    departments.value[0]?.id ??
    "";
  form.problemSolved = payload.problemSolved;
  form.triggerDescription = payload.triggerDescription;
  form.startMode = payload.startMode;
  form.applicableDepartmentCodes = [...payload.applicableDepartmentCodes];
  form.participantRoles = [...payload.participantRoles];
  form.objectTypeCodes = [...payload.objectTypeCodes];
  form.workflowDefinitionCode = payload.workflowDefinitionCode;
  form.workflowVersionNumber = payload.workflowVersionNumber;
  form.steps = payload.steps.map((step) => ({ ...step }));
  form.preparationItems = payload.preparationItems.map((item) => ({ ...item }));
  form.supportedFileSources = [...payload.supportedFileSources];
  form.supportedMinutesSources = [...payload.supportedMinutesSources];
  form.allowedAgents = payload.allowedAgents.map((item) => ({ ...item }));
  form.permanentHumanBoundaries = [...payload.permanentHumanBoundaries];
  form.permissionRequirements = [...payload.permissionRequirements];
  form.completionEvidence = [...payload.completionEvidence];
}

async function load() {
  loading.value = true;
  errorMessage.value = "";
  try {
    const [
      scenarioPage,
      departmentRows,
      objectTypeRows,
      workflowRows,
      agentRows,
      approvalRows,
    ] = await Promise.all([
      scenarioApi.listManage(),
      workspaceApi.departments(),
      workspaceApi.publishedObjectTypes(),
      workspaceApi.workflowDefinitions(),
      workspaceApi.agents(),
      auth.can("scenario:approve")
        ? scenarioApi.approvalRequests()
        : Promise.resolve([]),
    ]);
    scenarios.value = scenarioPage.items;
    departments.value = departmentRows;
    objectTypes.value = objectTypeRows;
    workflows.value = workflowRows;
    agents.value = agentRows.filter((item) => item.isActive);
    approvals.value = approvalRows;
    if (
      !selected.value ||
      !scenarios.value.some((item) => item.id === selected.value?.id)
    ) {
      await selectScenario(scenarios.value[0]?.id ?? "");
    }
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    loading.value = false;
  }
}

async function selectScenario(id: string) {
  selected.value = scenarios.value.find((item) => item.id === id) ?? null;
  validation.value = null;
  fillForm(
    selected.value,
    editableVersion.value ?? selected.value?.currentPublishedVersion ?? null,
  );
}

function openCreate() {
  selected.value = null;
  fillForm(null, null, defaultPayload());
  form.code = `SCN-CUSTOM-${new Date().toISOString().slice(0, 10).replaceAll("-", "")}`;
  form.name = "";
  form.description = "";
  form.problemSolved = "";
  form.triggerDescription = "";
  const owner =
    departments.value.find((item) => item.isPrimary) ?? departments.value[0];
  form.applicableDepartmentCodes = owner ? [owner.code] : [];
  createVisible.value = true;
}

async function createDraft() {
  if (
    !form.code.trim() ||
    !form.name.trim() ||
    !form.description.trim() ||
    !form.ownerDepartmentId
  ) {
    ElMessage.warning("请填写场景编码、名称、说明和所属部门。");
    return;
  }
  actionLoading.value = "create";
  try {
    const created = await scenarioApi.createDraft({
      code: form.code.trim().toUpperCase(),
      name: form.name.trim(),
      description: form.description.trim(),
      ownerDepartmentId: form.ownerDepartmentId,
      payload: payloadFromForm(),
    });
    createVisible.value = false;
    scenarios.value = (await scenarioApi.listManage()).items;
    await selectScenario(created.id);
    ElMessage.success("场景草稿已写入 SQL Server，尚未对普通用户生效");
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function createNextDraft() {
  if (!selected.value) return;
  actionLoading.value = "next";
  try {
    const result = await ElMessageBox.prompt(
      "请输入新版本的业务变更说明。",
      "创建下一版本",
      {
        inputPattern: /\S+/,
        inputErrorMessage: "版本变更说明不能为空",
        confirmButtonText: "创建草稿",
      },
    );
    const updated = await scenarioApi.createNextDraft(selected.value.id, {
      expectedVersion: selected.value.version,
      changeSummary: result.value.trim(),
    });
    await selectScenario(updated.id);
    scenarios.value = (await scenarioApi.listManage()).items;
    ElMessage.success("新的场景草稿已创建，历史发布版本保持不变");
  } catch (error) {
    if (error !== "cancel" && error !== "close")
      errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function saveDraft() {
  if (!selected.value || !editableVersion.value || !isEditable.value) return;
  actionLoading.value = "save";
  try {
    const updated = await scenarioApi.updateDraft(
      selected.value.id,
      editableVersion.value.id,
      {
        expectedRevision: editableVersion.value.revision,
        name: form.name.trim(),
        description: form.description.trim(),
        ownerDepartmentId: form.ownerDepartmentId,
        payload: payloadFromForm(),
      },
    );
    selected.value = updated;
    fillForm(updated, updated.draftVersion ?? updated.currentPublishedVersion);
    scenarios.value = (await scenarioApi.listManage()).items;
    ElMessage.success("场景草稿已保存，Revision 已更新");
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function validateDraft() {
  if (!selected.value || !editableVersion.value) return;
  actionLoading.value = "validate";
  try {
    const result = await scenarioApi.validate(
      selected.value.id,
      editableVersion.value.id,
      {
        expectedVersion: selected.value.version,
        comment: "发布前资源引用校验",
      },
    );
    validation.value = {
      errors: [...result.errors],
      warnings: [...result.warnings],
    };
    validationVisible.value = true;
    scenarios.value = (await scenarioApi.listManage()).items;
    await selectScenario(selected.value.id);
    if (!result.isValid) {
      validation.value = {
        errors: [...result.errors],
        warnings: [...result.warnings],
      };
      validationVisible.value = true;
    }
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function publishDraft(versionId: string) {
  if (!selected.value) return;
  await ElMessageBox.confirm(
    "发布后普通员工立即可见，但历史流程仍按启动时版本执行。付款、质量放行等人工边界不会改变。",
    "发布场景版本",
    { type: "warning", confirmButtonText: "发布", cancelButtonText: "取消" },
  );
  actionLoading.value = "publish";
  try {
    const updated = await scenarioApi.publish(selected.value.id, versionId, {
      expectedVersion: selected.value.version,
      comment: "平台管理员从场景配置中心发布",
    });
    scenarios.value = (await scenarioApi.listManage()).items;
    await selectScenario(updated.id);
    if (updated.pendingApprovalRequestId) {
      ElMessage.warning("高风险场景已提交第二位平台管理员审核，审核前不会生效");
      approvals.value = await scenarioApi.approvalRequests();
    } else {
      ElMessage.success("场景版本已发布");
    }
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function toggleStatus() {
  if (!selected.value || !canManage.value) return;
  const isDisabled = selected.value.lifecycleStatus === "Disabled";
  const result = await ElMessageBox.prompt(
    isDisabled
      ? "请输入恢复原因。"
      : "停用后将不能新发起，但历史记录仍可查看。请输入停用原因。",
    isDisabled ? "恢复场景" : "停用场景",
    { inputPattern: /\S+/, inputErrorMessage: "原因不能为空" },
  );
  actionLoading.value = "status";
  try {
    const updated = isDisabled
      ? await scenarioApi.restore(selected.value.id, {
          expectedVersion: selected.value.version,
          comment: result.value,
        })
      : await scenarioApi.disable(selected.value.id, {
          expectedVersion: selected.value.version,
          comment: result.value,
        });
    scenarios.value = (await scenarioApi.listManage()).items;
    await selectScenario(updated.id);
    ElMessage.success(
      isDisabled ? "场景已恢复，可新发起" : "场景已停用，历史数据未删除",
    );
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function showVersions() {
  if (!selected.value) return;
  versions.value = await scenarioApi.versions(selected.value.id);
  versionsVisible.value = true;
}

async function rollbackTo(version: ScenarioVersion) {
  if (!selected.value || !canManage.value) return;
  const result = await ElMessageBox.prompt(
    `回滚将把 v${version.versionNumber} 复制为新的发布版本，旧版本和历史流程不复写。请输入原因。`,
    "回滚场景版本",
    { inputPattern: /\S+/, inputErrorMessage: "回滚原因不能为空" },
  );
  actionLoading.value = "rollback";
  try {
    const updated = await scenarioApi.rollback(selected.value.id, {
      expectedVersion: selected.value.version,
      targetVersionNumber: version.versionNumber,
      reason: result.value,
    });
    versionsVisible.value = false;
    scenarios.value = (await scenarioApi.listManage()).items;
    await selectScenario(updated.id);
    ElMessage.success("已基于历史版本创建新的回滚版本");
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function reviewApproval(
  request: ScenarioApprovalRequest,
  approve: boolean,
) {
  const result = await ElMessageBox.prompt(
    `${approve ? "通过" : "驳回"}场景 ${request.scenarioName} v${request.versionNumber}，请填写双人审核意见。`,
    approve ? "审核通过" : "审核驳回",
    { inputPattern: /\S+/, inputErrorMessage: "审核意见不能为空" },
  );
  actionLoading.value = request.id;
  try {
    const currentScenario = (await scenarioApi.listManage()).items.find(
      (item) => item.id === request.scenarioId,
    );
    if (!currentScenario) {
      throw new Error("待审核场景已不存在或不在当前管理范围。");
    }
    await scenarioApi.reviewApproval(request.id, {
      expectedVersion: currentScenario.version,
      approve,
      comment: result.value,
    });
    approvals.value = await scenarioApi.approvalRequests();
    scenarios.value = (await scenarioApi.listManage()).items;
    if (selected.value) await selectScenario(selected.value.id);
    ElMessage.success(approve ? "审核通过，场景版本已生效" : "审核已驳回");
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

function addStep() {
  const sequence = form.steps.length + 1;
  form.steps.push({
    sequence,
    code: `STEP_${sequence}`,
    name: "",
    businessDescription: "",
    responsibleRole: "",
    action: "",
    expectedResult: "",
    evidence: "",
    permanentHumanBoundary: false,
  });
}

function addPreparation() {
  form.preparationItems.push({
    code: `MATERIAL_${form.preparationItems.length + 1}`,
    name: "",
    isRequired: false,
    sourceType: "Attachment",
    description: "",
  });
}

function updateWorkflow(value: string) {
  if (!value) {
    form.workflowDefinitionCode = null;
    form.workflowVersionNumber = null;
    return;
  }
  const [code, version] = value.split("|");
  form.workflowDefinitionCode = code;
  form.workflowVersionNumber = Number(version);
}

function workflowValue() {
  return form.workflowDefinitionCode && form.workflowVersionNumber
    ? `${form.workflowDefinitionCode}|${form.workflowVersionNumber}`
    : "";
}

function lines(value: string[]): string {
  return value.join("\n");
}

function setLines(target: string[], value: string) {
  target.splice(
    0,
    target.length,
    ...value
      .split("\n")
      .map((item) => item.trim())
      .filter(Boolean),
  );
}

function toggleAgent(agent: AgentProfileDto) {
  if (!agent.code) return;
  const index = form.allowedAgents.findIndex(
    (item) => item.agentProfileCode === agent.code,
  );
  if (index >= 0) {
    form.allowedAgents.splice(index, 1);
  } else {
    form.allowedAgents.push({
      agentProfileCode: agent.code,
      capability: "业务建议与材料检查",
      boundary: "只生成建议、草稿和风险提示，不确认、不审批、不付款、不放行",
    });
  }
}

onMounted(load);
</script>

<template>
  <div
    v-loading="loading"
    class="scenario-admin"
    data-testid="scenario-management-panel"
  >
    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />

    <section v-if="approvals.length" class="section-card">
      <div class="section-heading">
        <div>
          <h2>待第二位平台管理员审核</h2>
          <p>采购、财务和质量高风险发布必须双人审核，申请人不能自审。</p>
        </div>
      </div>
      <article
        v-for="request in approvals"
        :key="request.id"
        class="approval-row"
      >
        <div>
          <strong
            >{{ request.scenarioName }} · v{{ request.versionNumber }}</strong
          >
          <p>
            {{ request.riskReason }} · 申请人
            {{ request.requestedByDisplayName }}
          </p>
        </div>
        <div>
          <el-button
            :loading="actionLoading === request.id"
            @click="reviewApproval(request, false)"
            >驳回</el-button
          >
          <el-button
            type="primary"
            :loading="actionLoading === request.id"
            @click="reviewApproval(request, true)"
            >审核通过</el-button
          >
        </div>
      </article>
    </section>

    <div class="admin-shell">
      <aside class="section-card scenario-admin-list">
        <div class="section-heading">
          <div>
            <h2>场景配置</h2>
            <p>草稿不影响线上版本</p>
          </div>
          <el-button v-if="canCreate" type="primary" @click="openCreate"
            >新建草稿</el-button
          >
        </div>
        <button
          v-for="scenario in scenarios"
          :key="scenario.id"
          class="scenario-admin-item"
          :class="{ active: selected?.id === scenario.id }"
          @click="selectScenario(scenario.id)"
        >
          <span>
            <strong>{{ scenario.name }}</strong>
            <small
              >{{ scenario.ownerDepartmentName }} ·
              {{
                scenario.currentPublishedVersion ? "已发布" : "未发布"
              }}</small
            >
          </span>
          <el-tag
            :type="
              scenario.lifecycleStatus === 'Published'
                ? 'success'
                : scenario.lifecycleStatus === 'Disabled'
                  ? 'info'
                  : 'warning'
            "
          >
            {{
              scenario.lifecycleStatus === "Published"
                ? "可用"
                : scenario.lifecycleStatus === "Disabled"
                  ? "停用"
                  : "草稿"
            }}
          </el-tag>
        </button>
        <el-empty
          v-if="!scenarios.length && !loading"
          description="当前权限下没有可管理场景。请联系平台管理员或部门负责人授权。"
        />
      </aside>

      <main v-if="selected" class="section-card admin-editor">
        <div class="section-heading">
          <div>
            <h2>{{ selected.name }}</h2>
            <p>
              {{ selected.ownerDepartmentName }} · 线上 v{{
                selected.currentPublishedVersion?.versionNumber ?? "—"
              }}
              · 草稿 v{{ selected.draftVersion?.versionNumber ?? "—" }}
            </p>
          </div>
          <div class="action-row">
            <el-button
              v-if="!selected.draftVersion && canUpdate"
              :loading="actionLoading === 'next'"
              @click="createNextDraft"
              >创建下一版本</el-button
            >
            <el-button
              v-if="isEditable"
              :loading="actionLoading === 'save'"
              @click="saveDraft"
              >保存草稿</el-button
            >
            <el-button
              v-if="editableVersion && canUpdate"
              :loading="actionLoading === 'validate'"
              @click="validateDraft"
              >校验</el-button
            >
            <el-button
              v-if="
                editableVersion &&
                canApprove &&
                ['Draft', 'Validated'].includes(editableVersion.status)
              "
              type="primary"
              :loading="actionLoading === 'publish'"
              @click="publishDraft(editableVersion.id)"
            >
              发布
            </el-button>
            <el-button @click="showVersions">版本历史</el-button>
            <el-button
              v-if="canManage"
              :loading="actionLoading === 'status'"
              @click="toggleStatus"
            >
              {{ selected.lifecycleStatus === "Disabled" ? "恢复" : "停用" }}
            </el-button>
          </div>
        </div>

        <el-alert
          v-if="selected.pendingApprovalRequestId"
          type="warning"
          :closable="false"
          title="当前发布正在等待第二位平台管理员审核，普通员工仍使用上一发布版本。"
        />
        <el-alert
          v-if="selected.lifecycleStatus === 'Disabled'"
          type="info"
          :closable="false"
          :title="`场景已停用：${selected.disabledReason || '无原因'}。历史启动仍可查看，新发起被阻断。`"
        />

        <el-form label-position="top" :disabled="!isEditable">
          <div class="form-grid">
            <el-form-item label="场景编码（创建后不可改）"
              ><el-input v-model="form.code"
            /></el-form-item>
            <el-form-item label="场景名称"
              ><el-input v-model="form.name"
            /></el-form-item>
            <el-form-item label="所属部门">
              <el-select v-model="form.ownerDepartmentId" style="width: 100%">
                <el-option
                  v-for="department in departments"
                  :key="department.id"
                  :label="department.name"
                  :value="department.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="发起方式">
              <el-select v-model="form.startMode" style="width: 100%">
                <el-option
                  label="新建业务对象并进入流程"
                  value="CreateBusinessObjectAndWorkflow"
                />
                <el-option
                  label="选择已有对象并进入流程"
                  value="ExistingBusinessObjectAndWorkflow"
                />
                <el-option label="事件收件箱与经理分派" value="EventInbox" />
                <el-option label="项目里程碑风险任务" value="ProjectTask" />
              </el-select>
            </el-form-item>
          </div>
          <el-form-item label="业务说明"
            ><el-input v-model="form.description" type="textarea" :rows="2"
          /></el-form-item>
          <el-form-item label="要解决的问题"
            ><el-input v-model="form.problemSolved" type="textarea" :rows="2"
          /></el-form-item>
          <el-form-item label="什么时候应发起"
            ><el-input
              v-model="form.triggerDescription"
              type="textarea"
              :rows="2"
          /></el-form-item>

          <div class="form-grid">
            <el-form-item label="适用部门">
              <el-select
                v-model="form.applicableDepartmentCodes"
                multiple
                style="width: 100%"
              >
                <el-option
                  v-for="department in departments"
                  :key="department.code"
                  :label="department.name"
                  :value="department.code"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="适用岗位与业务角色">
              <el-input
                :model-value="lines(form.participantRoles)"
                type="textarea"
                :rows="3"
                @update:model-value="setLines(form.participantRoles, $event)"
              />
            </el-form-item>
            <el-form-item label="已发布业务对象类型">
              <el-select
                v-model="form.objectTypeCodes"
                multiple
                filterable
                style="width: 100%"
              >
                <el-option
                  v-for="type in objectTypes"
                  :key="type.id"
                  :label="`${type.name} · ${type.code}`"
                  :value="type.code"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="有效流程版本">
              <el-select
                :model-value="workflowValue()"
                clearable
                style="width: 100%"
                @update:model-value="updateWorkflow"
              >
                <el-option
                  v-for="workflow in workflows"
                  :key="workflow.versionId"
                  :label="`${workflow.name} · V${workflow.versionNumber}`"
                  :value="`${workflow.code}|${workflow.versionNumber}`"
                />
              </el-select>
            </el-form-item>
          </div>

          <section class="editor-section">
            <div class="section-heading">
              <div>
                <h3>业务步骤与责任岗位</h3>
                <p>普通员工看到的是岗位、动作和完成条件，不是节点代码。</p>
              </div>
              <el-button v-if="isEditable" @click="addStep">添加步骤</el-button>
            </div>
            <article
              v-for="(step, index) in form.steps"
              :key="index"
              class="config-row"
            >
              <el-input v-model="step.name" placeholder="步骤名称" />
              <el-input
                v-model="step.businessDescription"
                placeholder="步骤业务说明"
              />
              <el-input v-model="step.responsibleRole" placeholder="责任岗位" />
              <el-input v-model="step.action" placeholder="要做什么" />
              <el-input v-model="step.expectedResult" placeholder="完成条件" />
              <el-input v-model="step.evidence" placeholder="形成什么证据" />
              <el-checkbox v-model="step.permanentHumanBoundary"
                >永久人工边界</el-checkbox
              >
              <el-button
                text
                type="danger"
                :disabled="!isEditable || form.steps.length === 1"
                @click="form.steps.splice(index, 1)"
                >移除</el-button
              >
            </article>
          </section>

          <section class="editor-section">
            <div class="section-heading">
              <div>
                <h3>必填文件与来源</h3>
                <p>
                  只能引用现有文件、听记和业务对象，不能嵌入脚本或外部地址。
                </p>
              </div>
              <el-button v-if="isEditable" @click="addPreparation"
                >添加材料</el-button
              >
            </div>
            <article
              v-for="(item, index) in form.preparationItems"
              :key="index"
              class="config-row material-row"
            >
              <el-input v-model="item.name" placeholder="材料名称" />
              <el-select v-model="item.sourceType" style="width: 100%">
                <el-option label="受控文件" value="Attachment" />
                <el-option label="业务对象" value="BusinessObject" />
                <el-option label="项目" value="Project" />
                <el-option label="事件原文" value="EventInbox" />
                <el-option label="听记来源" value="MinutesSource" />
              </el-select>
              <el-checkbox v-model="item.isRequired">必填</el-checkbox>
              <el-input v-model="item.description" placeholder="说明" />
              <el-button
                text
                type="danger"
                :disabled="!isEditable"
                @click="form.preparationItems.splice(index, 1)"
                >移除</el-button
              >
            </article>
          </section>

          <section class="editor-section">
            <div class="section-heading">
              <div>
                <h3>数字员工</h3>
                <p>只可选择已发布、有效的身份；仅生成建议和草稿。</p>
              </div>
            </div>
            <div class="agent-options">
              <button
                v-for="agent in agents"
                :key="agent.id"
                type="button"
                :class="{
                  selected: form.allowedAgents.some(
                    (item) => item.agentProfileCode === agent.code,
                  ),
                }"
                :disabled="!isEditable"
                @click="toggleAgent(agent)"
              >
                <strong>{{ agent.name }}</strong>
                <small
                  >{{ agent.autonomyLevel }} ·
                  {{ agent.allowedTools.join(" / ") || "建议与草稿" }}</small
                >
              </button>
            </div>
          </section>

          <el-form-item label="永久人工边界">
            <div class="boundary-tags">
              <el-tag
                v-for="boundary in permanentBoundaries"
                :key="boundary"
                type="danger"
                effect="plain"
                >{{ boundary }}</el-tag
              >
            </div>
            <small>七类边界由服务端强制保留，不能通过配置删除。</small>
          </el-form-item>
          <div class="form-grid">
            <el-form-item label="权限要求（业务可读说明）">
              <el-input
                :model-value="lines(form.permissionRequirements)"
                type="textarea"
                :rows="3"
                @update:model-value="
                  setLines(form.permissionRequirements, $event)
                "
              />
            </el-form-item>
            <el-form-item label="完成后形成的数据与证据">
              <el-input
                :model-value="lines(form.completionEvidence)"
                type="textarea"
                :rows="3"
                @update:model-value="setLines(form.completionEvidence, $event)"
              />
            </el-form-item>
          </div>
          <details v-if="auth.isPlatformAdmin">
            <summary>管理员技术详情</summary>
            <p>
              场景编码 {{ form.code || "—" }} · Revision
              {{ editableVersion?.revision ?? "—" }} · 状态
              {{ editableVersion?.status ?? "仅发布版本" }}
            </p>
          </details>
        </el-form>
      </main>

      <main v-else class="section-card empty-editor">
        <el-empty description="选择场景或创建新草稿">
          <el-button v-if="canCreate" type="primary" @click="openCreate"
            >创建场景草稿</el-button
          >
        </el-empty>
      </main>
    </div>

    <el-dialog
      v-model="createVisible"
      title="新建场景草稿"
      width="min(720px, 94vw)"
    >
      <el-form label-position="top">
        <div class="form-grid">
          <el-form-item label="场景编码"
            ><el-input v-model="form.code"
          /></el-form-item>
          <el-form-item label="场景名称"
            ><el-input v-model="form.name"
          /></el-form-item>
          <el-form-item label="所属部门">
            <el-select v-model="form.ownerDepartmentId" style="width: 100%">
              <el-option
                v-for="department in departments"
                :key="department.id"
                :label="department.name"
                :value="department.id"
              />
            </el-select>
          </el-form-item>
        </div>
        <el-form-item label="业务说明"
          ><el-input v-model="form.description" type="textarea" :rows="3"
        /></el-form-item>
        <el-form-item label="要解决的问题"
          ><el-input v-model="form.problemSolved" type="textarea" :rows="2"
        /></el-form-item>
        <el-form-item label="什么时候应发起"
          ><el-input
            v-model="form.triggerDescription"
            type="textarea"
            :rows="2"
        /></el-form-item>
        <el-form-item label="适用部门">
          <el-select
            v-model="form.applicableDepartmentCodes"
            multiple
            style="width: 100%"
          >
            <el-option
              v-for="department in departments"
              :key="department.code"
              :label="department.name"
              :value="department.code"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="适用岗位与业务角色">
          <el-input
            :model-value="lines(form.participantRoles)"
            type="textarea"
            :rows="2"
            @update:model-value="setLines(form.participantRoles, $event)"
          />
        </el-form-item>
        <el-form-item label="权限和数据范围要求">
          <el-input
            :model-value="lines(form.permissionRequirements)"
            type="textarea"
            :rows="2"
            @update:model-value="setLines(form.permissionRequirements, $event)"
          />
        </el-form-item>
        <el-alert
          type="info"
          :closable="false"
          title="先保存为草稿，再补充对象、流程、步骤、材料、听记和数字员工。发布前必须通过服务端引用校验。"
        />
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button
          type="primary"
          :loading="actionLoading === 'create'"
          @click="createDraft"
          >创建草稿</el-button
        >
      </template>
    </el-dialog>

    <el-dialog
      v-model="validationVisible"
      title="场景校验结果"
      width="min(640px, 94vw)"
    >
      <el-alert
        :type="validation?.errors.length ? 'error' : 'success'"
        :closable="false"
        :title="
          validation?.errors.length
            ? `发现 ${validation.errors.length} 个阻断问题`
            : '资源引用校验通过'
        "
      />
      <ul v-if="validation?.errors.length">
        <li v-for="error in validation.errors" :key="error">{{ error }}</li>
      </ul>
      <ul v-if="validation?.warnings.length" class="warning-list">
        <li v-for="warning in validation.warnings" :key="warning">
          {{ warning }}
        </li>
      </ul>
    </el-dialog>

    <el-dialog
      v-model="versionsVisible"
      title="场景版本历史"
      width="min(760px, 95vw)"
    >
      <el-timeline>
        <el-timeline-item
          v-for="version in versions"
          :key="version.id"
          :timestamp="version.publishedAt || version.createdAt"
          :type="version.status === 'Published' ? 'success' : 'info'"
        >
          <strong>v{{ version.versionNumber }} · {{ version.status }}</strong>
          <p>
            {{ version.changeSummary || "无变更说明" }} ·
            {{ version.createdByDisplayName }}
          </p>
          <el-button
            v-if="version.status === 'Published' && canManage"
            text
            type="primary"
            @click="rollbackTo(version)"
          >
            回滚到此版本
          </el-button>
        </el-timeline-item>
      </el-timeline>
    </el-dialog>
  </div>
</template>

<style scoped>
.scenario-admin,
.admin-editor,
.editor-section {
  display: grid;
  gap: 16px;
}

.admin-shell {
  display: grid;
  grid-template-columns: 260px minmax(0, 1fr);
  align-items: start;
  gap: 14px;
}

.scenario-admin-list {
  position: sticky;
  top: 92px;
  display: grid;
  gap: 8px;
  max-height: calc(100vh - 112px);
  overflow-y: auto;
  padding: 16px;
}

.section-heading,
.action-row,
.approval-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.section-heading h2,
.section-heading h3,
.section-heading p {
  margin: 0;
}

.section-heading p {
  margin-top: 4px;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.action-row {
  flex-wrap: wrap;
  justify-content: flex-end;
}

.scenario-admin-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 8px;
  width: 100%;
  border: 1px solid transparent;
  border-radius: 12px;
  background: #f7faf8;
  padding: 11px;
  color: var(--aidt-ink);
  text-align: left;
  cursor: pointer;
}

.scenario-admin-item.active {
  border-color: #b8d3c5;
  background: #edf6f0;
}

.scenario-admin-item span strong,
.scenario-admin-item span small {
  display: block;
}

.scenario-admin-item small {
  margin-top: 4px;
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 14px;
}

.editor-section {
  margin: 16px 0;
  border-top: 1px solid var(--aidt-border);
  padding-top: 16px;
}

.config-row {
  display: grid;
  grid-template-columns: repeat(6, minmax(100px, 1fr)) auto auto;
  align-items: center;
  gap: 7px;
  border: 1px solid var(--aidt-border);
  border-radius: 12px;
  padding: 9px;
}

.material-row {
  grid-template-columns: 1.2fr 1fr auto 1.6fr auto;
}

.agent-options {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.agent-options button {
  display: grid;
  gap: 3px;
  min-width: 190px;
  border: 1px solid var(--aidt-border);
  border-radius: 12px;
  background: #fafcfa;
  padding: 10px;
  color: var(--aidt-ink);
  text-align: left;
  cursor: pointer;
}

.agent-options button.selected {
  border-color: #8eb9a3;
  background: #eaf5ee;
}

.agent-options small {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.boundary-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.empty-editor {
  min-height: 420px;
  display: grid;
  place-items: center;
}

.approval-row {
  border: 1px solid var(--aidt-border);
  border-radius: 12px;
  padding: 12px;
}

.approval-row p {
  margin: 4px 0 0;
  font-size: 0.76rem;
}

details {
  border-radius: 10px;
  background: #f3f6f4;
  padding: 10px;
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
}

ul {
  color: var(--aidt-danger);
}

.warning-list {
  color: var(--aidt-warning);
}

@media (max-width: 1120px) {
  .admin-shell {
    grid-template-columns: 220px minmax(0, 1fr);
  }

  .config-row {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .admin-shell,
  .form-grid,
  .config-row,
  .material-row {
    grid-template-columns: 1fr;
  }

  .scenario-admin-list {
    position: static;
    max-height: 280px;
  }

  .section-heading,
  .approval-row {
    align-items: stretch;
    flex-direction: column;
  }
}
</style>
