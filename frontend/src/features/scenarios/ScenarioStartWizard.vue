<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";
import { useRouter } from "vue-router";
import { ElMessage } from "element-plus";
import { ApiError } from "@/api/http";
import { fileApi } from "@/api/modules/fileApi";
import { scenarioApi } from "@/api/modules/scenarioApi";
import { workspaceApi } from "@/api/modules/workspaceApi";
import type {
  AgentProfileDto,
  BusinessObjectDto,
  DepartmentDto,
  FileAttachmentDto,
  MinutesSourceDto,
  ObjectTypeDto,
  PagedResult,
  ProjectCandidateUserDto,
  ProjectDto,
  Scenario,
  ScenarioLaunch,
  ScenarioPreflight,
  ScenarioStartInput,
} from "@/api/types/contracts";
import {
  businessLinkTarget,
  scenarioErrorGuidance,
  startModeLabel,
} from "./scenarioPresentation";

const props = defineProps<{
  visible: boolean;
  scenario: Scenario | null;
  departments: DepartmentDto[];
  objectTypes: ObjectTypeDto[];
  agents: AgentProfileDto[];
}>();

const emit = defineEmits<{
  "update:visible": [value: boolean];
  started: [launch: ScenarioLaunch];
}>();

const router = useRouter();
const active = ref(0);
const loading = ref(false);
const actionLoading = ref("");
const errorMessage = ref("");
const preflight = ref<ScenarioPreflight | null>(null);
const launch = ref<ScenarioLaunch | null>(null);
const files = ref<FileAttachmentDto[]>([]);
const minutes = ref<MinutesSourceDto[]>([]);
const businessObjects = ref<BusinessObjectDto[]>([]);
const projects = ref<PagedResult<ProjectDto> | null>(null);
const members = ref<ProjectCandidateUserDto[]>([]);
const selectedFile = ref<File | null>(null);
const selectedMinutesFile = ref<File | null>(null);
const pendingIdempotencyKey = ref("");
const form = reactive({
  departmentId: "",
  businessObjectId: "",
  objectCode: "",
  title: "",
  fields: {} as Record<string, string | null>,
  projectId: "",
  assigneeId: "",
  taskTitle: "",
  taskContent: "",
  dueAt: "",
  priority: "P1",
  content: "",
  attachmentIds: [] as string[],
  minutesSourceIds: [] as string[],
  requestAgentSuggestion: false,
  agentProfileId: "",
  comment: "",
});

const payload = computed(
  () => props.scenario?.currentPublishedVersion?.payload ?? null,
);
const mode = computed(() => payload.value?.startMode ?? "EventInbox");
const selectedDepartment = computed(
  () => props.departments.find((item) => item.id === form.departmentId) ?? null,
);
const objectTypeCode = computed(() => payload.value?.objectTypeCodes[0] ?? "");
const objectType = computed(
  () =>
    props.objectTypes.find((item) => item.code === objectTypeCode.value) ??
    null,
);
const availableAgents = computed(() => {
  const allowed = new Set(
    payload.value?.allowedAgents.map((item) => item.agentProfileCode) ?? [],
  );
  return props.agents.filter(
    (item) => item.isActive && Boolean(item.code) && allowed.has(item.code!),
  );
});
const matchingObjects = computed(() =>
  businessObjects.value.filter((item) => {
    if (!selectedDepartment.value || item.departmentId !== form.departmentId)
      return false;
    const type = props.objectTypes.find(
      (candidate) => candidate.id === item.objectTypeId,
    );
    return !objectTypeCode.value || type?.code === objectTypeCode.value;
  }),
);
const matchingProjects = computed(() =>
  (projects.value?.items ?? []).filter(
    (item) =>
      !selectedDepartment.value || item.departmentId === form.departmentId,
  ),
);
const canAdvance = computed(() => {
  if (active.value === 0) return Boolean(props.scenario);
  if (active.value === 1) return businessFieldsReady.value;
  if (active.value === 2) return true;
  if (active.value === 3) return true;
  if (active.value === 4) return Boolean(preflight.value);
  if (active.value === 5) return preflight.value?.canStart === true;
  return false;
});
const businessFieldsReady = computed(() => {
  if (!props.scenario || !form.departmentId) return false;
  if (mode.value === "CreateBusinessObjectAndWorkflow") {
    return Boolean(form.objectCode.trim() && form.title.trim());
  }
  if (mode.value === "ExistingBusinessObjectAndWorkflow") {
    return Boolean(form.businessObjectId);
  }
  if (mode.value === "EventInbox") {
    return Boolean(form.title.trim() && form.content.trim());
  }
  if (mode.value === "ProjectTask") {
    return Boolean(
      form.projectId &&
      form.assigneeId &&
      form.dueAt &&
      form.taskTitle.trim() &&
      form.taskContent.trim(),
    );
  }
  return false;
});
const stepLabels = [
  "场景确认",
  "业务信息",
  "文件与听记",
  "审批路径",
  "权限与风险",
  "确认发起",
  "发起结果",
];

function resetFromScenario() {
  active.value = 0;
  preflight.value = null;
  launch.value = null;
  errorMessage.value = "";
  files.value = [];
  selectedFile.value = null;
  selectedMinutesFile.value = null;
  pendingIdempotencyKey.value = "";
  form.departmentId =
    props.departments.find(
      (item) =>
        item.code &&
        payload.value?.applicableDepartmentCodes.includes(item.code),
    )?.id ??
    props.departments.find((item) => item.isPrimary)?.id ??
    props.departments[0]?.id ??
    "";
  form.businessObjectId = "";
  form.objectCode = "";
  form.title = props.scenario?.name ?? "";
  form.fields = {};
  for (const field of objectType.value?.fields ?? []) {
    form.fields[field.key] = field.defaultValue;
  }
  form.projectId = "";
  form.assigneeId = "";
  form.taskTitle = "";
  form.taskContent = "";
  form.dueAt = "";
  form.priority = "P1";
  form.content = "";
  form.attachmentIds = [];
  form.minutesSourceIds = [];
  form.requestAgentSuggestion = false;
  form.agentProfileId = availableAgents.value[0]?.id ?? "";
  form.comment = "";
}

async function loadSupportingData() {
  loading.value = true;
  try {
    const [objectPage, projectPage, minutePage] = await Promise.all([
      workspaceApi.businessObjects(),
      workspaceApi.projects(),
      workspaceApi.minutes(),
    ]);
    businessObjects.value = objectPage.items;
    projects.value = projectPage;
    minutes.value = minutePage.items;
    if (form.projectId) {
      members.value = await workspaceApi.projectMemberCandidates(
        form.projectId,
      );
    }
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    loading.value = false;
  }
}

function errorText(error: unknown): string {
  if (error instanceof ApiError) {
    return scenarioErrorGuidance(error.code, error.message);
  }
  return "操作未完成，请稍后重试。";
}

async function uploadMaterial() {
  if (!selectedFile.value || !form.departmentId) {
    ElMessage.warning("请先选择部门和材料文件。");
    return;
  }
  actionLoading.value = "file";
  try {
    const uploaded = await fileApi.upload({
      file: selectedFile.value,
      departmentId: form.departmentId,
      sensitivity: "Internal",
    });
    files.value.push(uploaded);
    form.attachmentIds.push(uploaded.id);
    selectedFile.value = null;
    ElMessage.success("材料已上传到现有文件中心，文件刷新后仍可追溯");
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function uploadMinutesSource() {
  if (!selectedMinutesFile.value || !form.departmentId) {
    ElMessage.warning("请先选择部门和听记文件。");
    return;
  }
  actionLoading.value = "minutes";
  try {
    const source = await workspaceApi.uploadMinutes({
      file: selectedMinutesFile.value,
      departmentId: form.departmentId,
      title: selectedMinutesFile.value.name.replace(/\.[^.]+$/, ""),
    });
    minutes.value.unshift(source);
    form.minutesSourceIds.push(source.id);
    selectedMinutesFile.value = null;
    ElMessage.success("听记来源已导入，仍需本人确认后才能形成业务任务");
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

function buildInput(): ScenarioStartInput {
  return {
    departmentId: form.departmentId,
    businessObjectId:
      mode.value === "ExistingBusinessObjectAndWorkflow"
        ? form.businessObjectId || null
        : null,
    objectCode:
      mode.value === "CreateBusinessObjectAndWorkflow"
        ? form.objectCode.trim().toUpperCase()
        : null,
    title: form.title.trim() || null,
    fields:
      mode.value === "CreateBusinessObjectAndWorkflow"
        ? { ...form.fields }
        : null,
    projectId: mode.value === "ProjectTask" ? form.projectId || null : null,
    assigneeId: mode.value === "ProjectTask" ? form.assigneeId || null : null,
    taskTitle:
      mode.value === "ProjectTask" ? form.taskTitle.trim() || null : null,
    taskContent:
      mode.value === "ProjectTask" ? form.taskContent.trim() || null : null,
    dueAt:
      mode.value === "ProjectTask" && form.dueAt
        ? new Date(form.dueAt).toISOString()
        : null,
    priority: mode.value === "ProjectTask" ? form.priority : null,
    content: mode.value === "EventInbox" ? form.content.trim() || null : null,
    attachmentIds: form.attachmentIds,
    minutesSourceIds: form.minutesSourceIds,
    requestAgentSuggestion: form.requestAgentSuggestion,
    agentProfileId: form.requestAgentSuggestion
      ? form.agentProfileId || null
      : null,
    comment: form.comment.trim() || null,
  };
}

async function runPreflight() {
  if (!props.scenario) return;
  actionLoading.value = "preflight";
  errorMessage.value = "";
  try {
    preflight.value = await scenarioApi.preflight(
      props.scenario.id,
      buildInput(),
    );
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

async function next() {
  errorMessage.value = "";
  if (active.value === 3) {
    active.value = 4;
    await runPreflight();
    return;
  }
  if (!canAdvance.value) {
    ElMessage.warning("请先完成当前步骤的必填信息和检查。");
    return;
  }
  active.value = Math.min(6, active.value + 1);
}

function previous() {
  errorMessage.value = "";
  active.value = Math.max(0, active.value - 1);
}

async function startScenario() {
  if (!props.scenario || preflight.value?.canStart !== true) return;
  actionLoading.value = "start";
  errorMessage.value = "";
  pendingIdempotencyKey.value ||= crypto.randomUUID();
  try {
    launch.value = await scenarioApi.start(
      props.scenario.id,
      buildInput(),
      pendingIdempotencyKey.value,
    );
    active.value = 6;
    emit("started", launch.value);
    ElMessage.success("场景已发起，已进入现有业务对象、流程或任务链");
  } catch (error) {
    errorMessage.value = errorText(error);
  } finally {
    actionLoading.value = "";
  }
}

function openBusinessLink(
  type: string,
  id: string,
  name: string,
  role: string,
  detailPath: string,
) {
  if (!launch.value) return;
  void router.push(
    businessLinkTarget({ type, id, name, role, detailPath }, launch.value),
  );
}

function close() {
  emit("update:visible", false);
}

watch(
  () => props.visible,
  (visible) => {
    if (!visible) return;
    resetFromScenario();
    void loadSupportingData();
  },
);

watch(
  () => form.projectId,
  async (projectId) => {
    form.assigneeId = "";
    members.value = projectId
      ? await workspaceApi.projectMemberCandidates(projectId)
      : [];
  },
);

onMounted(() => {
  if (props.visible) {
    resetFromScenario();
    void loadSupportingData();
  }
});
</script>

<template>
  <el-dialog
    :model-value="visible"
    class="scenario-wizard-dialog"
    data-testid="scenario-start-wizard"
    width="min(980px, 96vw)"
    destroy-on-close
    @update:model-value="emit('update:visible', $event)"
  >
    <template #header>
      <div class="wizard-heading">
        <div>
          <p class="page-kicker">七步业务发起 · 真人确认</p>
          <h2>{{ scenario?.name || "发起业务场景" }}</h2>
        </div>
        <el-tag type="warning" effect="plain">受控 Demo 试点样例</el-tag>
      </div>
    </template>

    <div v-loading="loading" class="wizard-body">
      <el-alert
        v-if="errorMessage"
        type="error"
        :closable="false"
        show-icon
        :title="errorMessage"
      />

      <div class="step-strip" aria-label="发起步骤">
        <div
          v-for="(label, index) in stepLabels"
          :key="label"
          class="step-item"
          :class="{ active: active === index, complete: active > index }"
        >
          <span>{{ active > index ? "✓" : index + 1 }}</span>
          <small>{{ label }}</small>
        </div>
      </div>

      <section
        v-if="active === 0 && scenario && payload"
        class="wizard-section"
      >
        <h3>先确认你要办理的业务</h3>
        <div class="answer-grid">
          <article>
            <span>我现在要办什么</span>
            <strong>{{ scenario.description }}</strong>
          </article>
          <article>
            <span>我需要准备什么</span>
            <strong>{{
              payload.preparationItems.map((item) => item.name).join("、") ||
              "按本步业务信息准备"
            }}</strong>
          </article>
          <article>
            <span>下一步是谁处理</span>
            <strong>{{
              payload.steps
                .slice(0, 2)
                .map((step) => step.responsibleRole)
                .join(" → ")
            }}</strong>
          </article>
          <article>
            <span>结果和证据在哪里</span>
            <strong>{{ payload.completionEvidence.join("、") }}</strong>
          </article>
        </div>
        <div class="business-path">
          <div v-for="step in payload.steps" :key="step.sequence">
            <span>{{ step.sequence }}</span>
            <p>
              <strong>{{ step.name }}</strong
              ><small>{{ step.responsibleRole }} · {{ step.action }}</small>
            </p>
          </div>
        </div>
        <p class="mode-note">
          发起方式：{{ startModeLabel(payload.startMode) }}
        </p>
      </section>

      <section v-else-if="active === 1 && payload" class="wizard-section">
        <h3>填写业务信息</h3>
        <el-form label-position="top">
          <div class="form-grid">
            <el-form-item label="办理部门">
              <el-select
                v-model="form.departmentId"
                filterable
                style="width: 100%"
              >
                <el-option
                  v-for="department in departments.filter((item) =>
                    payload?.applicableDepartmentCodes.includes(item.code),
                  )"
                  :key="department.id"
                  :label="department.name"
                  :value="department.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item
              v-if="mode === 'ExistingBusinessObjectAndWorkflow'"
              label="选择已有业务事项"
            >
              <el-select
                v-model="form.businessObjectId"
                filterable
                style="width: 100%"
              >
                <el-option
                  v-for="item in matchingObjects"
                  :key="item.id"
                  :label="`${item.objectCode} · ${item.title || item.status}`"
                  :value="item.id"
                />
              </el-select>
            </el-form-item>
            <template v-else-if="mode === 'CreateBusinessObjectAndWorkflow'">
              <el-form-item label="业务事项编号">
                <el-input
                  v-model="form.objectCode"
                  placeholder="例如 SAMPLE-20261103-01"
                />
              </el-form-item>
              <el-form-item label="事项名称">
                <el-input v-model="form.title" />
              </el-form-item>
              <el-form-item
                v-for="field in objectType?.fields ?? []"
                :key="field.key"
                :label="`${field.displayName}${field.isRequired ? '（必填）' : ''}`"
              >
                <el-input
                  v-model="form.fields[field.key]"
                  :type="field.dataType === 'LongText' ? 'textarea' : 'text'"
                  :maxlength="field.maxLength || undefined"
                />
              </el-form-item>
            </template>
            <template v-else-if="mode === 'EventInbox'">
              <el-form-item class="span-2" label="事项标题">
                <el-input v-model="form.title" />
              </el-form-item>
              <el-form-item class="span-2" label="事项原文">
                <el-input v-model="form.content" type="textarea" :rows="5" />
              </el-form-item>
            </template>
            <template v-else>
              <el-form-item label="关联项目">
                <el-select
                  v-model="form.projectId"
                  filterable
                  style="width: 100%"
                >
                  <el-option
                    v-for="project in matchingProjects"
                    :key="project.id"
                    :label="`${project.objectCode} · ${project.name}`"
                    :value="project.id"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="任务负责人">
                <el-select
                  v-model="form.assigneeId"
                  filterable
                  style="width: 100%"
                >
                  <el-option
                    v-for="member in members"
                    :key="member.id"
                    :label="`${member.displayName} · ${member.departmentName}`"
                    :value="member.id"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="任务名称">
                <el-input v-model="form.taskTitle" />
              </el-form-item>
              <el-form-item label="截止时间">
                <el-date-picker
                  v-model="form.dueAt"
                  type="datetime"
                  value-format="YYYY-MM-DDTHH:mm:ssZ"
                  style="width: 100%"
                />
              </el-form-item>
              <el-form-item label="优先级">
                <el-select v-model="form.priority" style="width: 100%">
                  <el-option label="P0 紧急" value="P0" />
                  <el-option label="P1 高" value="P1" />
                  <el-option label="P2 常规" value="P2" />
                </el-select>
              </el-form-item>
              <el-form-item class="span-2" label="任务说明">
                <el-input
                  v-model="form.taskContent"
                  type="textarea"
                  :rows="4"
                />
              </el-form-item>
            </template>
          </div>
        </el-form>
      </section>

      <section v-else-if="active === 2" class="wizard-section">
        <h3>补充文件与听记</h3>
        <p>
          文件进入现有文件中心和审计；听记只生成建议，事项、负责人、截止时间和流程仍由真人确认。
        </p>
        <div class="material-grid">
          <article>
            <h4>上传业务材料</h4>
            <input
              type="file"
              accept=".pdf,.docx,.xlsx,.pptx,.png,.jpg,.jpeg,.txt,.csv"
              @change="
                selectedFile =
                  ($event.target as HTMLInputElement).files?.[0] ?? null
              "
            />
            <small v-if="selectedFile">已选择：{{ selectedFile.name }}</small>
            <el-button
              :loading="actionLoading === 'file'"
              @click="uploadMaterial"
              >上传材料</el-button
            >
            <ul>
              <li v-for="file in files" :key="file.id">
                {{ file.originalFileName }} · 已上传
              </li>
            </ul>
          </article>
          <article>
            <h4>导入 AI 听记</h4>
            <input
              type="file"
              accept=".docx,.txt,.md,.json"
              @change="
                selectedMinutesFile =
                  ($event.target as HTMLInputElement).files?.[0] ?? null
              "
            />
            <small v-if="selectedMinutesFile"
              >已选择：{{ selectedMinutesFile.name }}</small
            >
            <el-button
              :loading="actionLoading === 'minutes'"
              @click="uploadMinutesSource"
              >导入听记</el-button
            >
            <el-select
              v-model="form.minutesSourceIds"
              multiple
              filterable
              placeholder="选择已有听记"
              style="width: 100%"
            >
              <el-option
                v-for="source in minutes.filter(
                  (item) => item.departmentId === form.departmentId,
                )"
                :key="source.id"
                :label="source.title"
                :value="source.id"
              />
            </el-select>
          </article>
        </div>
        <el-alert
          v-if="
            payload?.preparationItems.some(
              (item) =>
                item.isRequired &&
                ['Attachment', 'File'].includes(item.sourceType),
            )
          "
          type="warning"
          :closable="false"
          title="存在必填材料。未上传时，下一步的启动前检查会明确阻断并说明需要补充哪一项。"
        />
      </section>

      <section v-else-if="active === 3 && payload" class="wizard-section">
        <h3>审批路径预览</h3>
        <el-timeline>
          <el-timeline-item
            v-for="step in payload.steps"
            :key="step.sequence"
            :type="step.permanentHumanBoundary ? 'danger' : 'primary'"
          >
            <div class="approval-step">
              <strong>{{ step.name }} · {{ step.responsibleRole }}</strong>
              <p>{{ step.action }}</p>
              <small>完成条件：{{ step.expectedResult }}</small>
              <el-tag
                v-if="step.permanentHumanBoundary"
                type="danger"
                effect="plain"
                >永久人工边界</el-tag
              >
            </div>
          </el-timeline-item>
        </el-timeline>
        <div class="agent-boundary" v-if="payload.allowedAgents.length">
          <h4>数字员工能做什么</h4>
          <p
            v-for="agent in payload.allowedAgents"
            :key="agent.agentProfileCode"
          >
            {{ agent.capability }}；边界：{{ agent.boundary }}
          </p>
          <strong
            >不会执行：自动确认、自动审批、自动付款、质量放行、配方变更、设备控制或自动关闭项目。</strong
          >
        </div>
      </section>

      <section v-else-if="active === 4" class="wizard-section">
        <div class="section-heading">
          <div>
            <h3>权限与风险检查</h3>
            <p>
              由服务端核对部门、对象、流程版本、数字员工、文件和永久人工边界。
            </p>
          </div>
          <el-button
            :loading="actionLoading === 'preflight'"
            @click="runPreflight"
            >重新检查</el-button
          >
        </div>
        <template v-if="preflight">
          <el-alert
            :type="preflight.canStart ? 'success' : 'error'"
            :closable="false"
            show-icon
            :title="
              preflight.canStart
                ? '检查通过，可以进入真人确认'
                : '当前不能启动，请按下面说明处理'
            "
          />
          <div class="check-grid">
            <article
              v-for="check in preflight.preparationChecks"
              :key="check.code"
              :class="{ failed: !check.isValid }"
            >
              <strong>{{ check.name }}</strong>
              <p>{{ check.message }}</p>
              <el-tag
                :type="check.isValid ? 'success' : 'danger'"
                effect="plain"
              >
                {{ check.isValid ? "已准备" : "缺失/无效" }}
              </el-tag>
            </article>
          </div>
          <el-alert
            v-for="reason in preflight.blockingReasons"
            :key="reason"
            type="error"
            :closable="false"
            :title="`${reason} 下一步：补齐后重新检查，或联系${preflight.departmentName}负责人。`"
          />
          <div class="boundary-list">
            <strong>永久人工边界</strong>
            <el-tag
              v-for="boundary in payload?.permanentHumanBoundaries ?? []"
              :key="boundary"
              type="danger"
              effect="plain"
            >
              {{ boundary }}
            </el-tag>
          </div>
        </template>
        <el-empty v-else description="正在等待启动前检查" />
      </section>

      <section v-else-if="active === 5" class="wizard-section">
        <h3>确认发起</h3>
        <p>
          发起后将通过现有业务服务创建真实业务链。此动作不会由数字员工自动完成。
        </p>
        <div class="confirm-card">
          <p>
            <span>业务场景</span><strong>{{ scenario?.name }}</strong>
          </p>
          <p>
            <span>办理部门</span><strong>{{ selectedDepartment?.name }}</strong>
          </p>
          <p>
            <span>业务事项</span
            ><strong>{{
              form.title || form.taskTitle || form.objectCode
            }}</strong>
          </p>
          <p>
            <span>材料数量</span
            ><strong
              >{{ form.attachmentIds.length }} 个文件 /
              {{ form.minutesSourceIds.length }} 个听记来源</strong
            >
          </p>
          <p>
            <span>下一处理人</span
            ><strong>{{ payload?.steps[0]?.responsibleRole }}</strong>
          </p>
        </div>
        <el-form label-position="top">
          <el-form-item label="发起说明">
            <el-input
              v-model="form.comment"
              type="textarea"
              :rows="3"
              placeholder="可填写业务背景，便于下一处理人理解"
            />
          </el-form-item>
          <el-checkbox
            v-if="availableAgents.length"
            v-model="form.requestAgentSuggestion"
          >
            请求数字员工生成建议草稿（不会确认、审批或付款）
          </el-checkbox>
          <el-form-item v-if="form.requestAgentSuggestion" label="数字员工身份">
            <el-select v-model="form.agentProfileId" style="width: 100%">
              <el-option
                v-for="agent in availableAgents"
                :key="agent.id"
                :label="`${agent.name} · ${agent.autonomyLevel}`"
                :value="agent.id"
              />
            </el-select>
          </el-form-item>
        </el-form>
      </section>

      <section
        v-else-if="active === 6 && launch"
        class="wizard-section result-section"
      >
        <div class="result-mark">✓</div>
        <h3>场景已发起</h3>
        <p>
          真实对象、流程和任务已由现有业务服务创建，刷新或重新登录后可继续查看。
        </p>
        <div class="result-facts">
          <article>
            <span>当前等待</span
            ><strong>{{ payload?.steps[0]?.responsibleRole }}</strong>
          </article>
          <article>
            <span>业务事项</span
            ><strong>{{
              launch.businessObjectCode || launch.scenarioName
            }}</strong>
          </article>
          <article>
            <span>问题找谁</span
            ><strong>{{ launch.departmentName }}负责人</strong>
          </article>
        </div>
        <div class="evidence-actions">
          <el-button
            v-for="link in launch.businessLinks"
            :key="`${link.type}-${link.id}`"
            @click="
              openBusinessLink(
                link.type,
                link.id,
                link.name,
                link.role,
                link.detailPath,
              )
            "
          >
            {{ link.role }} · {{ link.name }}
          </el-button>
          <el-button type="primary" @click="close">查看我的场景</el-button>
        </div>
      </section>
    </div>

    <template #footer>
      <div class="wizard-footer">
        <el-button v-if="active > 0 && active < 6" @click="previous"
          >上一步</el-button
        >
        <el-button
          v-if="active < 5"
          type="primary"
          :disabled="!canAdvance"
          @click="next"
          >下一步</el-button
        >
        <el-button
          v-if="active === 5"
          type="primary"
          :loading="actionLoading === 'start'"
          :disabled="preflight?.canStart !== true"
          data-testid="scenario-start-confirm"
          @click="startScenario"
        >
          确认真人发起
        </el-button>
        <el-button @click="close">{{
          active === 6 ? "关闭" : "取消"
        }}</el-button>
      </div>
    </template>
  </el-dialog>
</template>

<style scoped>
.wizard-heading,
.section-heading,
.wizard-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 14px;
}

.wizard-heading h2,
.wizard-heading p,
.wizard-section h3,
.wizard-section p {
  margin: 0;
}

.wizard-body {
  display: grid;
  gap: 18px;
  min-height: 520px;
}

.step-strip {
  display: grid;
  grid-template-columns: repeat(7, minmax(0, 1fr));
  gap: 5px;
}

.step-item {
  display: grid;
  place-items: center;
  gap: 5px;
  border-bottom: 3px solid #dce5df;
  padding: 9px 3px;
  color: var(--aidt-ink-muted);
}

.step-item span {
  display: grid;
  width: 27px;
  height: 27px;
  place-items: center;
  border-radius: 999px;
  background: #eef3f0;
  font-size: 0.72rem;
  font-weight: 800;
}

.step-item small {
  text-align: center;
  font-size: 0.68rem;
}

.step-item.active,
.step-item.complete {
  border-color: var(--aidt-brand);
  color: var(--aidt-brand-strong);
}

.step-item.active span,
.step-item.complete span {
  background: var(--aidt-brand-soft);
}

.wizard-section {
  display: grid;
  gap: 16px;
}

.wizard-section > p {
  color: var(--aidt-ink-muted);
  line-height: 1.7;
}

.answer-grid,
.material-grid,
.check-grid,
.result-facts {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.answer-grid article,
.material-grid article,
.check-grid article,
.result-facts article {
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: #fbfdfb;
  padding: 14px;
}

.answer-grid span,
.result-facts span,
.confirm-card span {
  display: block;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.answer-grid strong,
.result-facts strong {
  display: block;
  margin-top: 6px;
  line-height: 1.55;
}

.business-path,
.approval-step {
  display: grid;
  gap: 8px;
}

.business-path > div {
  display: grid;
  grid-template-columns: 28px minmax(0, 1fr);
  align-items: start;
  gap: 9px;
}

.business-path > div > span {
  display: grid;
  width: 28px;
  height: 28px;
  place-items: center;
  border-radius: 9px;
  background: var(--aidt-brand-soft);
  color: var(--aidt-brand-strong);
  font-weight: 800;
}

.business-path p,
.business-path small {
  display: block;
  margin: 0;
}

.business-path small {
  margin-top: 3px;
  color: var(--aidt-ink-muted);
}

.mode-note {
  border-radius: 12px;
  background: #edf5f0;
  padding: 11px;
  color: var(--aidt-brand-strong);
  font-size: 0.8rem;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 14px;
}

.span-2 {
  grid-column: 1 / -1;
}

.material-grid article {
  display: grid;
  gap: 10px;
}

.material-grid h4,
.agent-boundary h4 {
  margin: 0;
}

.material-grid small {
  color: var(--aidt-brand);
}

.material-grid ul {
  margin: 0;
  padding-left: 18px;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.agent-boundary {
  border: 1px solid #e9d29e;
  border-radius: 14px;
  background: var(--aidt-warning-soft);
  padding: 14px;
  color: #76521b;
}

.agent-boundary p {
  margin-top: 7px;
}

.check-grid article {
  display: grid;
  gap: 7px;
}

.check-grid article.failed {
  border-color: #e7b4ae;
  background: var(--aidt-danger-soft);
}

.check-grid p {
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.boundary-list,
.evidence-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}

.confirm-card {
  display: grid;
  gap: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  overflow: hidden;
}

.confirm-card p {
  display: grid;
  grid-template-columns: 130px minmax(0, 1fr);
  gap: 12px;
  margin: 0;
  border-bottom: 1px solid var(--aidt-border);
  padding: 11px 14px;
}

.confirm-card p:last-child {
  border-bottom: 0;
}

.result-section {
  justify-items: center;
  text-align: center;
}

.result-mark {
  display: grid;
  width: 72px;
  height: 72px;
  place-items: center;
  border-radius: 999px;
  background: var(--aidt-brand-soft);
  color: var(--aidt-brand-strong);
  font-size: 2rem;
  font-weight: 900;
}

.result-facts {
  width: 100%;
}

.wizard-footer {
  justify-content: flex-end;
}

@media (max-width: 760px) {
  .step-strip {
    grid-template-columns: repeat(4, minmax(0, 1fr));
  }

  .step-item small {
    display: none;
  }

  .answer-grid,
  .material-grid,
  .check-grid,
  .result-facts,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .span-2 {
    grid-column: auto;
  }

  .confirm-card p {
    grid-template-columns: 1fr;
    gap: 3px;
  }
}
</style>
