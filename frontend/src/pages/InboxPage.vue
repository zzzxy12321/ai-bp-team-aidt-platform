<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import {
  formatShanghaiDate,
  fromShanghaiInput,
  isConfirmationDraftComplete,
  toShanghaiInput,
} from '@/features/event-confirmation/confirmationRules'
import type {
  AgentProfileDto,
  DepartmentDto,
  InboxEventDto,
  InboxSourceOptionDto,
  ProjectCandidateUserDto,
  ProjectDto,
  WorkflowDefinitionDto,
  WorkflowInstanceDto,
} from '@/api/types/contracts'
import { useAuthStore } from '@/stores/auth'

type FlowMode = 'none' | 'existing' | 'new'

type ConfirmationDraft = {
  projectId: string
  assigneeId: string
  dueLocal: string
  priority: 'P0' | 'P1' | 'P2'
  content: string
  createTask: boolean
  flowMode: FlowMode
  workflowInstanceId: string
  workflowDefinitionId: string
}

const auth = useAuthStore()
const route = useRoute()
const loading = ref(true)
const actionLoading = ref('')
const errorMessage = ref('')
const events = ref<InboxEventDto[]>([])
const departments = ref<DepartmentDto[]>([])
const projects = ref<ProjectDto[]>([])
const sources = ref<InboxSourceOptionDto[]>([])
const agents = ref<AgentProfileDto[]>([])
const workflowDefinitions = ref<WorkflowDefinitionDto[]>([])
const selectedDepartmentId = ref('')
const statusFilter = ref('')
const createVisible = ref(false)
const includeTestData = ref(false)
const highlightedEventId = ref(String(route.query.event ?? ''))
const selectedFile = ref<File | null>(null)
const selectedSourceKey = ref('')
const confirmVisible = ref(false)
const confirmEvent = ref<InboxEventDto | null>(null)
const confirmLoading = ref(false)
const drafts = reactive<Record<string, ConfirmationDraft>>({})
const projectCandidates = reactive<Record<string, ProjectCandidateUserDto[]>>({})
const projectWorkflows = reactive<Record<string, WorkflowInstanceDto[]>>({})

const form = reactive({
  title: '',
  content: '',
  sourceType: 'ManualText' as InboxEventDto['sourceType'],
  sourceId: null as string | null,
})

const defaultAgent = computed(
  () =>
    agents.value.find((item) => item.name.includes('项目事件'))
    ?? agents.value.find((item) => item.autonomyLevel !== 'A1')
    ?? agents.value[0]
    ?? null,
)
const waitingCount = computed(
  () => events.value.filter((item) => item.status === 'AwaitingConfirmation').length,
)
const highRiskCount = computed(
  () => events.value.filter((item) => item.suggestion?.requiresHumanBoundary).length,
)
const projectWorkflowDefinitions = computed(() =>
  workflowDefinitions.value.filter((item) => item.objectTypeCode === 'PROJECT'),
)
const activeDraft = computed(() =>
  confirmEvent.value ? drafts[confirmEvent.value.id] ?? null : null,
)
const activeCandidates = computed(() =>
  confirmEvent.value ? projectCandidates[confirmEvent.value.id] ?? [] : [],
)
const activeExistingWorkflows = computed(() =>
  confirmEvent.value ? projectWorkflows[confirmEvent.value.id] ?? [] : [],
)
const selectedProject = computed(() =>
  projects.value.find((project) => project.id === activeDraft.value?.projectId) ?? null,
)
const eventProjects = computed(() =>
  projects.value.filter(
    (project) => !confirmEvent.value || project.departmentId === confirmEvent.value.departmentId,
  ),
)
const selectedAssignee = computed(() =>
  activeCandidates.value.find((candidate) => candidate.id === activeDraft.value?.assigneeId)
  ?? null,
)
const selectedWorkflowDefinition = computed(() =>
  projectWorkflowDefinitions.value.find(
    (definition) => definition.id === activeDraft.value?.workflowDefinitionId,
  ) ?? null,
)
const selectedExistingWorkflow = computed(() =>
  activeExistingWorkflows.value.find(
    (workflow) => workflow.id === activeDraft.value?.workflowInstanceId,
  ) ?? null,
)
const canConfirm = computed(() =>
  activeDraft.value ? isConfirmationDraftComplete(activeDraft.value) : false,
)

function initialDraft(event: InboxEventDto): ConfirmationDraft {
  return {
    projectId: event.suggestion?.suggestedProjectId ?? '',
    assigneeId: event.suggestion?.suggestedAssigneeId ?? '',
    dueLocal: toShanghaiInput(event.suggestion?.suggestedDueAt ?? null),
    priority: event.suggestion?.priority ?? 'P1',
    content: event.rawText,
    createTask: true,
    flowMode: 'none',
    workflowInstanceId: '',
    workflowDefinitionId: '',
  }
}

function syncDraftFromSuggestion(event: InboxEventDto) {
  const draft = drafts[event.id] ??= initialDraft(event)
  if (!event.suggestion || event.status === 'Confirmed') return
  if (!draft.projectId && event.suggestion.suggestedProjectId) {
    draft.projectId = event.suggestion.suggestedProjectId
  }
  if (!draft.assigneeId && event.suggestion.suggestedAssigneeId) {
    draft.assigneeId = event.suggestion.suggestedAssigneeId
  }
  if (!draft.dueLocal && event.suggestion.suggestedDueAt) {
    draft.dueLocal = toShanghaiInput(event.suggestion.suggestedDueAt)
  }
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [departmentRows, projectPage, eventPage, agentRows, definitionRows] = await Promise.all([
      workspaceApi.departments(),
      workspaceApi.projects(includeTestData.value),
      workspaceApi.inbox(includeTestData.value, statusFilter.value || undefined),
      workspaceApi.agents(),
      workspaceApi.workflowDefinitions(),
    ])
    departments.value = departmentRows
    projects.value = projectPage.items
    events.value = eventPage.items
    agents.value = agentRows
    workflowDefinitions.value = definitionRows
    if (!selectedDepartmentId.value) {
      selectedDepartmentId.value =
        departmentRows.find((item) => item.isPrimary)?.id ?? departmentRows[0]?.id ?? ''
    }
    if (selectedDepartmentId.value) {
      sources.value = await workspaceApi.inboxSources(selectedDepartmentId.value)
    }
    for (const event of events.value) {
      syncDraftFromSuggestion(event)
    }
    await nextTick()
    document.querySelector(`[data-event-id="${highlightedEventId.value}"]`)?.scrollIntoView({
      behavior: 'smooth',
      block: 'center',
    })
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '无法加载事件收件箱。'
  } finally {
    loading.value = false
  }
}

async function loadSources() {
  if (!selectedDepartmentId.value) return
  try {
    sources.value = await workspaceApi.inboxSources(selectedDepartmentId.value)
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '读取来源候选失败')
  }
}

function openCreate() {
  form.title = ''
  form.content = ''
  form.sourceType = 'ManualText'
  form.sourceId = null
  selectedSourceKey.value = ''
  selectedFile.value = null
  createVisible.value = true
}

function applySource(key: string) {
  const source = sources.value.find((item) => `${item.type}:${item.id}` === key)
  if (!source) return
  form.sourceType = source.type
  form.sourceId = source.id
  form.title = source.title
  form.content = source.detail
}

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
}

async function createEvent() {
  if (!selectedDepartmentId.value) {
    ElMessage.warning('请选择事件所属部门')
    return
  }
  if (!form.title.trim()) {
    ElMessage.warning('请填写事件标题')
    return
  }
  actionLoading.value = 'create'
  try {
    if (selectedFile.value) {
      await workspaceApi.uploadInboxEvent({
        file: selectedFile.value,
        departmentId: selectedDepartmentId.value,
        title: form.title.trim(),
      })
    } else {
      if (!form.sourceId && !form.content.trim()) {
        throw new Error('请粘贴事件内容或选择一个已有来源')
      }
      await workspaceApi.receiveInboxEvent({
        departmentId: selectedDepartmentId.value,
        title: form.title.trim(),
        content: form.content.trim(),
        sourceType: form.sourceType,
        sourceId: form.sourceId,
      })
    }
    ElMessage.success('事件已进入收件箱，尚未创建任务或流程')
    createVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError || error instanceof Error ? error.message : '事件接收失败')
  } finally {
    actionLoading.value = ''
  }
}

async function analyze(event: InboxEventDto) {
  if (!defaultAgent.value) {
    ElMessage.warning('未找到可用的业务协同数字员工')
    return
  }
  actionLoading.value = `analyze-${event.id}`
  try {
    await workspaceApi.analyzeInboxEvent(event.id, defaultAgent.value.id)
    ElMessage.success('RuleSimulator 已生成建议；未创建任务、未发起流程')
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : 'Agent 分析失败')
  } finally {
    actionLoading.value = ''
  }
}

async function loadConfirmContext(event: InboxEventDto, projectId: string) {
  if (!projectId) {
    projectCandidates[event.id] = []
    projectWorkflows[event.id] = []
    return
  }
  confirmLoading.value = true
  try {
    const [members, workflowPage] = await Promise.all([
      workspaceApi.projectMemberCandidates(projectId),
      workspaceApi.workflowInstances(includeTestData.value),
    ])
    projectCandidates[event.id] = members
    projectWorkflows[event.id] = workflowPage.items.filter(
      (workflow) =>
        workflow.businessObjectId === projectId
        && (workflow.status === 'Running' || workflow.status === 'Waiting'),
    )
    const draft = drafts[event.id]
    if (draft && !members.some((member) => member.id === draft.assigneeId)) {
      draft.assigneeId = members[0]?.id ?? ''
    }
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '读取项目候选失败')
  } finally {
    confirmLoading.value = false
  }
}

async function openConfirmation(event: InboxEventDto) {
  confirmEvent.value = event
  drafts[event.id] ??= initialDraft(event)
  confirmVisible.value = true
  await loadConfirmContext(event, drafts[event.id].projectId)
}

async function onConfirmProjectChange() {
  const event = confirmEvent.value
  const draft = activeDraft.value
  if (!event || !draft) return
  draft.assigneeId = ''
  draft.workflowInstanceId = ''
  draft.flowMode = 'none'
  await loadConfirmContext(event, draft.projectId)
}

async function submitConfirmation() {
  const event = confirmEvent.value
  const draft = activeDraft.value
  if (!event || !draft) return
  if (!draft.assigneeId) {
    ElMessage.warning('最终确认前必须选择负责人')
    return
  }
  if (!draft.dueLocal) {
    ElMessage.warning('最终确认前必须选择截止时间')
    return
  }
  const dueAt = fromShanghaiInput(draft.dueLocal)
  if (!dueAt) {
    ElMessage.warning('截止时间格式无效')
    return
  }
  if (draft.flowMode === 'existing' && !draft.workflowInstanceId) {
    ElMessage.warning('请选择要关联的已有流程')
    return
  }
  if (draft.flowMode === 'new' && !selectedWorkflowDefinition.value) {
    ElMessage.warning('请选择要发起的新流程')
    return
  }

  actionLoading.value = `confirm-${event.id}`
  try {
    const result = await workspaceApi.confirmAndStartInboxFlow(event.id, {
      projectId: draft.projectId,
      assigneeId: draft.assigneeId,
      dueAt,
      priority: draft.priority,
      content: draft.content.trim(),
      createTask: draft.createTask,
      workflowInstanceId:
        draft.flowMode === 'existing' ? draft.workflowInstanceId || null : null,
      workflowDefinitionId:
        draft.flowMode === 'new' ? selectedWorkflowDefinition.value?.id ?? null : null,
      workflowVersionId:
        draft.flowMode === 'new' ? selectedWorkflowDefinition.value?.versionId ?? null : null,
    })
    const changed = [
      result.task ? `任务「${result.task.title}」` : null,
      result.workflow ? '新的业务流程' : null,
    ].filter(Boolean).join('、')
    ElMessage.success(changed ? `确认完成，已创建${changed}` : '确认完成，事件已关联项目')
    confirmVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '最终确认失败')
  } finally {
    actionLoading.value = ''
  }
}

function sourceTypeLabel(type: InboxEventDto['sourceType']) {
  return {
    ManualText: '手动事项',
    ManualUpload: '上传材料',
    MinutesSource: 'AI 听记',
    MinutesActionItem: '听记行动项',
    WorkflowNotification: '流程通知',
    Attachment: '文件附件',
  }[type]
}

onMounted(load)
</script>

<template>
  <div class="page-shell" data-testid="inbox-page">
    <MainColumnPageHeader
      intro-key="inbox"
      title="事件收件箱"
      scope-label="当前部门与事件来源范围"
      kicker="企业事件收集 · 真人确认"
    >
      <template #actions>
        <div class="page-actions">
          <label v-if="auth.isPlatformAdmin" class="admin-filter">
            <el-switch v-model="includeTestData" @change="load" />
            跨环境
          </label>
          <el-button
            data-testid="receive-event-button"
            type="primary"
            :disabled="loading || !selectedDepartmentId"
            @click="openCreate"
          >
            接收事件
          </el-button>
        </div>
      </template>
    </MainColumnPageHeader>

    <el-alert v-if="errorMessage" type="error" :closable="false" show-icon :title="errorMessage" />

    <section class="metrics">
      <article><span>收件箱总数</span><strong>{{ events.length }}</strong></article>
      <article><span>待真人确认</span><strong>{{ waitingCount }}</strong></article>
      <article><span>高风险边界</span><strong>{{ highRiskCount }}</strong></article>
      <article><span>可用流程图</span><strong>{{ projectWorkflowDefinitions.length }}</strong></article>
    </section>

    <section class="toolbar section-card">
      <div>
        <strong>事件范围</strong>
        <small>部门、项目、负责人和流程范围均由后端强制校验。</small>
      </div>
      <div class="filters">
        <el-select v-model="selectedDepartmentId" placeholder="事件部门" @change="loadSources">
          <el-option
            v-for="department in departments"
            :key="department.id"
            :label="department.name"
            :value="department.id"
          />
        </el-select>
        <el-select v-model="statusFilter" placeholder="全部状态" clearable @change="load">
          <el-option label="刚接收" value="Received" />
          <el-option label="待确认" value="AwaitingConfirmation" />
          <el-option label="已确认" value="Confirmed" />
        </el-select>
        <el-button @click="load">刷新</el-button>
      </div>
    </section>

    <section v-loading="loading" class="event-list">
      <article
        v-for="event in events"
        :key="event.id"
        class="event-card"
        :class="{ focused: event.id === highlightedEventId }"
        :data-event-id="event.id"
      >
        <div class="event-head">
          <div>
            <span class="event-source">
              {{ sourceTypeLabel(event.sourceType) }} · {{ event.departmentName }}
            </span>
            <h2>{{ event.title }}</h2>
          </div>
          <el-tag
            :type="event.suggestion?.requiresHumanBoundary
              ? 'danger'
              : event.status === 'Confirmed' ? 'success' : 'warning'"
          >
            {{ event.suggestion?.requiresHumanBoundary
              ? '高风险待人工判断'
              : event.status === 'Confirmed' ? '已确认' : '待确认' }}
          </el-tag>
        </div>
        <p class="event-text">{{ event.rawText }}</p>

        <div v-if="event.suggestion" class="suggestion">
          <div class="suggestion-head">
            <strong>Agent 建议</strong>
            <span>{{ event.suggestion.riskLevel }} · {{ event.suggestion.priority }}</span>
          </div>
          <div class="suggestion-grid">
            <span>建议项目</span><strong>{{ event.suggestion.suggestedProjectName || '未识别' }}</strong>
            <span>建议负责人</span>
            <strong>{{ event.suggestion.suggestedAssigneeDisplayName || '未识别' }}</strong>
            <span>建议流程</span>
            <strong>{{ event.suggestion.suggestedWorkflowName || '待人工判断' }}</strong>
            <span>建议截止</span>
            <strong>{{ formatShanghaiDate(event.suggestion.suggestedDueAt) }}</strong>
          </div>
          <el-alert
            v-if="event.suggestion.requiresHumanBoundary"
            type="error"
            :closable="false"
            show-icon
            :title="`永久人工边界：${event.suggestion.boundaryReason || '高风险动作'}`"
            description="确认可形成人工跟进事项，但不会自动付款、质量放行、改配方、控制设备、解除联锁、承诺合同或赔付。"
          />
          <p v-if="event.suggestion.missingInformation" class="missing">
            缺失信息：{{ event.suggestion.missingInformation }}
          </p>
          <blockquote>{{ event.suggestion.evidenceExcerpt }}</blockquote>
        </div>

        <div class="event-actions">
          <el-button
            v-if="!event.suggestion && event.status !== 'Confirmed'"
            type="primary"
            plain
            :loading="actionLoading === `analyze-${event.id}`"
            @click="analyze(event)"
          >
            Agent 分析建议
          </el-button>
          <el-button
            v-if="event.suggestion && event.status !== 'Confirmed'"
            data-testid="open-confirmation"
            type="primary"
            @click="openConfirmation(event)"
          >
            编辑并确认业务去向
          </el-button>
          <el-tag v-else-if="event.status === 'Confirmed'" type="success">
            已确认；任务、流程和来源关系受幂等保护
          </el-tag>
        </div>
      </article>
      <el-empty v-if="!loading && events.length === 0" description="收件箱暂无事件" />
    </section>

    <el-dialog v-model="createVisible" title="接收企业事件" width="min(720px, 94vw)">
      <el-form label-position="top">
        <el-form-item label="来源（可选）">
          <el-select
            v-model="selectedSourceKey"
            clearable
            filterable
            placeholder="直接粘贴事项，或选择听记/通知/行动项/附件"
            style="width: 100%"
            @change="applySource"
          >
            <el-option
              v-for="source in sources"
              :key="`${source.type}:${source.id}`"
              :label="`${sourceTypeLabel(source.type)} · ${source.title}`"
              :value="`${source.type}:${source.id}`"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="事件标题">
          <el-input
            v-model="form.title"
            data-testid="event-title-input"
            placeholder="例如：客户要求补充耐候样品资料"
          />
        </el-form-item>
        <el-form-item v-if="!form.sourceId" label="事件原文">
          <el-input
            v-model="form.content"
            data-testid="event-content-input"
            type="textarea"
            :rows="7"
            placeholder="粘贴会议事项、邮件摘要、客户要求、异常说明等"
          />
        </el-form-item>
        <el-form-item v-if="!form.sourceId" label="或上传文字材料（TXT/MD/JSON/DOCX）">
          <input type="file" accept=".txt,.md,.json,.docx" @change="onFileChange" />
        </el-form-item>
        <el-alert
          type="info"
          :closable="false"
          show-icon
          title="事件接收只建立待分析事项；不会自动派发任务、发起流程或变更项目状态。"
        />
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button
          data-testid="submit-event-button"
          type="primary"
          :loading="actionLoading === 'create'"
          :disabled="loading || !selectedDepartmentId"
          @click="createEvent"
        >
          进入收件箱
        </el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="confirmVisible"
      data-testid="confirmation-dialog"
      title="真人确认业务去向"
      width="min(920px, 96vw)"
      destroy-on-close
    >
      <div v-if="confirmEvent && activeDraft" v-loading="confirmLoading" class="confirm-dialog">
        <div class="confirm-source">
          <span>正在处理</span>
          <strong>{{ confirmEvent.title }}</strong>
          <small>{{ confirmEvent.departmentName }} · {{ sourceTypeLabel(confirmEvent.sourceType) }}</small>
        </div>

        <el-form label-position="top">
          <div class="confirm-grid">
            <el-form-item label="所属项目">
              <el-select
                v-model="activeDraft.projectId"
                data-testid="confirmation-project"
                filterable
                placeholder="选择项目"
                @change="onConfirmProjectChange"
              >
                <el-option
                  v-for="project in eventProjects"
                  :key="project.id"
                  :label="`${project.name} · ${project.objectCode}`"
                  :value="project.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="负责人">
              <el-select
                v-model="activeDraft.assigneeId"
                data-testid="confirmation-assignee"
                filterable
                placeholder="选择项目成员"
              >
                <el-option
                  v-for="candidate in activeCandidates"
                  :key="candidate.id"
                  :label="`${candidate.displayName} · ${candidate.departmentName}`"
                  :value="candidate.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="截止时间（Asia/Shanghai）">
              <el-date-picker
                v-model="activeDraft.dueLocal"
                data-testid="confirmation-due"
                type="datetime"
                format="YYYY-MM-DD HH:mm"
                value-format="YYYY-MM-DDTHH:mm:ss"
                placeholder="选择上海时区截止时间"
                style="width: 100%"
              />
            </el-form-item>
            <el-form-item label="优先级">
              <el-radio-group v-model="activeDraft.priority">
                <el-radio-button value="P0">P0</el-radio-button>
                <el-radio-button value="P1">P1</el-radio-button>
                <el-radio-button value="P2">P2</el-radio-button>
              </el-radio-group>
            </el-form-item>
          </div>

          <el-form-item label="可编辑任务内容">
            <el-input
              v-model="activeDraft.content"
              data-testid="confirmation-content"
              type="textarea"
              :rows="5"
              maxlength="4000"
              show-word-limit
            />
          </el-form-item>

          <div class="choice-row">
            <label>
              <el-switch v-model="activeDraft.createTask" data-testid="confirmation-create-task" />
              <span>创建项目任务</span>
            </label>
            <el-radio-group v-model="activeDraft.flowMode" data-testid="confirmation-flow-mode">
              <el-radio-button value="none">不启动流程</el-radio-button>
              <el-radio-button value="existing">关联已有流程</el-radio-button>
              <el-radio-button value="new">发起新流程</el-radio-button>
            </el-radio-group>
          </div>

          <el-form-item v-if="activeDraft.flowMode === 'existing'" label="关联已有流程">
            <el-select
              v-model="activeDraft.workflowInstanceId"
              data-testid="confirmation-existing-flow"
              filterable
              placeholder="选择该项目已有流程"
            >
              <el-option
                v-for="workflow in activeExistingWorkflows"
                :key="workflow.id"
                :label="`${workflow.status === 'Running' ? '进行中' : workflow.status} · ${formatShanghaiDate(workflow.startedAt)} · ${workflow.currentNodeCode || '已结束'}`"
                :value="workflow.id"
              />
            </el-select>
          </el-form-item>

          <el-form-item v-if="activeDraft.flowMode === 'new'" label="发起新流程">
            <el-select
              v-model="activeDraft.workflowDefinitionId"
              data-testid="confirmation-new-flow"
              filterable
              placeholder="选择适用于项目的已发布流程"
            >
              <el-option
                v-for="definition in projectWorkflowDefinitions"
                :key="definition.id"
                :label="`${definition.name} · V${definition.versionNumber}`"
                :value="definition.id"
              />
            </el-select>
          </el-form-item>
        </el-form>

        <section class="impact-preview" data-testid="confirmation-impact">
          <div class="impact-head">
            <div>
              <span>确认前影响预览</span>
              <strong>一次提交，同一事务生效</strong>
            </div>
            <el-tag :type="canConfirm ? 'success' : 'warning'">
              {{ canConfirm ? '信息完整' : '等待补全' }}
            </el-tag>
          </div>
          <div class="impact-grid">
            <span>关联项目</span>
            <strong>{{ selectedProject ? `${selectedProject.name} · ${selectedProject.objectCode}` : '未选择' }}</strong>
            <span>任务负责人</span>
            <strong>{{ selectedAssignee?.displayName || '未选择' }}</strong>
            <span>截止时间</span>
            <strong>{{ activeDraft.dueLocal ? formatShanghaiDate(`${activeDraft.dueLocal}+08:00`) : '未选择' }}</strong>
            <span>任务</span>
            <strong>{{ activeDraft.createTask ? `创建 ${activeDraft.priority} 项目任务` : '不创建项目任务' }}</strong>
            <span>流程</span>
            <strong v-if="activeDraft.flowMode === 'existing'">
              关联 {{ selectedExistingWorkflow?.currentNodeCode || '已有流程' }}
            </strong>
            <strong v-else-if="activeDraft.flowMode === 'new'">
              发起 {{ selectedWorkflowDefinition?.name || '未选择流程' }}
            </strong>
            <strong v-else>不关联或发起流程</strong>
            <span>通知对象</span>
            <strong>{{ selectedAssignee?.displayName || '负责人待定' }}；流程节点处理人由服务端解析</strong>
          </div>
          <el-alert
            type="info"
            :closable="false"
            show-icon
            title="不会执行：付款、质量放行、配方变更、DCS/PLC 控制、安全联锁、合同承诺、客户重大赔付"
          />
          <el-alert
            v-if="confirmEvent.suggestion?.requiresHumanBoundary"
            type="warning"
            :closable="false"
            show-icon
            title="该事项命中高风险边界：本次确认只形成人工跟进，不会把 AI 建议当作业务批准。"
          />
        </section>
      </div>
      <template #footer>
        <el-button @click="confirmVisible = false">取消</el-button>
        <el-button
          data-testid="submit-confirmation"
          type="primary"
          :disabled="!canConfirm"
          :loading="confirmEvent ? actionLoading === `confirm-${confirmEvent.id}` : false"
          @click="submitConfirmation"
        >
          按影响预览最终确认
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page-head,
.page-actions,
.toolbar,
.filters,
.event-head,
.suggestion-head,
.event-actions,
.choice-row,
.impact-head {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.page-head,
.toolbar,
.event-head,
.suggestion-head,
.impact-head {
  justify-content: space-between;
}

.page-actions,
.filters,
.event-actions,
.choice-row {
  flex-wrap: wrap;
  align-items: center;
}

.admin-filter,
.choice-row > label {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.metrics {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.metrics article {
  border: 1px solid var(--aidt-border);
  border-radius: 16px;
  background: #fff;
  padding: 16px;
}

.metrics span,
.metrics strong,
.toolbar small {
  display: block;
}

.metrics span,
.toolbar small {
  color: var(--aidt-ink-muted);
  font-size: 0.74rem;
}

.metrics strong {
  margin-top: 8px;
  color: var(--aidt-brand-strong);
  font-size: 1.75rem;
}

.toolbar {
  align-items: center;
}

.filters :deep(.el-select) {
  width: 190px;
}

.event-list {
  display: grid;
  gap: 14px;
}

.event-card {
  border: 1px solid var(--aidt-border);
  border-radius: 20px;
  background: #fff;
  padding: 20px;
}

.event-card.focused {
  border-color: #78a88f;
  box-shadow: 0 0 0 4px rgb(58 125 91 / 10%), var(--aidt-shadow);
}

.event-source {
  color: var(--aidt-brand-strong);
  font-size: 0.72rem;
  font-weight: 800;
}

.event-card h2 {
  margin: 6px 0 0;
}

.event-text {
  display: -webkit-box;
  overflow: hidden;
  color: var(--aidt-ink-muted);
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 3;
}

.suggestion {
  margin: 14px 0;
  border-radius: 15px;
  background: #f5f8f6;
  padding: 15px;
}

.suggestion-grid,
.impact-grid {
  display: grid;
  grid-template-columns: 120px minmax(0, 1fr);
  gap: 7px 12px;
  margin: 12px 0;
  font-size: 0.8rem;
}

.suggestion-grid span,
.impact-grid span {
  color: var(--aidt-ink-muted);
}

.suggestion blockquote {
  margin: 12px 0 0;
  border-left: 3px solid #b5d8c5;
  color: var(--aidt-ink-muted);
  padding-left: 12px;
  line-height: 1.7;
}

.missing {
  color: #9a5d08;
  font-size: 0.78rem;
}

.confirm-dialog {
  display: grid;
  gap: 18px;
}

.confirm-source {
  display: grid;
  gap: 5px;
  border-radius: 16px;
  background: #f2f7f4;
  padding: 14px 16px;
}

.confirm-source span,
.confirm-source small {
  color: var(--aidt-ink-muted);
  font-size: 0.74rem;
}

.confirm-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 14px;
}

.choice-row {
  justify-content: space-between;
  margin: 8px 0 18px;
  border-block: 1px solid var(--aidt-border);
  padding: 14px 0;
}

.impact-preview {
  display: grid;
  gap: 12px;
  border: 1px solid #b8d7c6;
  border-radius: 18px;
  background: #f7fbf8;
  padding: 16px;
}

.impact-head span,
.impact-head strong {
  display: block;
}

.impact-head span {
  color: var(--aidt-brand-strong);
  font-size: 0.72rem;
  font-weight: 800;
}

@media (max-width: 760px) {
  .page-head,
  .toolbar,
  .event-head {
    align-items: stretch;
    flex-direction: column;
  }

  .metrics,
  .confirm-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .filters,
  .filters :deep(.el-select),
  .event-actions :deep(.el-button) {
    width: 100%;
  }

  .suggestion-grid,
  .impact-grid {
    grid-template-columns: 1fr;
  }

  .choice-row,
  .choice-row :deep(.el-radio-group) {
    align-items: stretch;
    width: 100%;
  }

  .choice-row :deep(.el-radio-button),
  .choice-row :deep(.el-radio-button__inner) {
    width: 100%;
  }
}
</style>
