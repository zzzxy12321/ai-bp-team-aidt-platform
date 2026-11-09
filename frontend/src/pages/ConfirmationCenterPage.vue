<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { mcpApi } from '@/api/modules/mcpApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import type {
  AgentRunDto,
  BusinessObjectDto,
  InboxEventDto,
  McpHumanConfirmationView,
  MinutesActionItemDto,
  MinutesConfirmationUserDto,
  MinutesSourceDto,
  ProjectDto,
  WorkflowDefinitionDto,
  WorkflowInstanceDto,
} from '@/api/types/contracts'
import {
  formatShanghaiDate,
  fromShanghaiInput,
  toShanghaiInput,
} from '@/features/event-confirmation/confirmationRules'
import {
  agentConfirmationStatus,
  canConfirmMinutesAction,
  eventConfirmationStatus,
  mcpConfirmationStatus,
  minutesConfirmationStatus,
} from '@/features/event-confirmation/confirmationCenterRules'
import type { ConfirmationStatus as RuleConfirmationStatus } from '@/features/event-confirmation/confirmationCenterRules'

type ConfirmationType = 'event' | 'minutes' | 'mcp' | 'agent'
type ConfirmationStatus = RuleConfirmationStatus

type ConfirmationItem = {
  key: string
  type: ConfirmationType
  id: string
  status: ConfirmationStatus
  title: string
  summary: string
  contextTitle: string
  sourceLabel: string
  assigneeName: string
  dueAt: string | null
  actionLabel: string
  detailPath: string
  requiresHumanBoundary: boolean
  event?: InboxEventDto
  minutesAction?: MinutesActionItemDto
  minutesSource?: MinutesSourceDto
  mcp?: McpHumanConfirmationView
  agent?: AgentRunDto
}

type MinutesDraft = {
  content: string
  assigneeId: string
  dueLocal: string
  priority: 'P0' | 'P1' | 'P2'
  projectId: string
  businessObjectId: string
  workflowVersionId: string
  workflowInstanceId: string
}

const route = useRoute()
const router = useRouter()
const loading = ref(true)
const saving = ref(false)
const minutesError = ref('')
const sourceErrors = ref<string[]>([])
const events = ref<InboxEventDto[]>([])
const minutesSources = ref<MinutesSourceDto[]>([])
const minutesActions = ref<MinutesActionItemDto[]>([])
const mcpViews = ref<McpHumanConfirmationView[]>([])
const agentRuns = ref<AgentRunDto[]>([])
const projects = ref<ProjectDto[]>([])
const businessObjects = ref<BusinessObjectDto[]>([])
const workflows = ref<WorkflowInstanceDto[]>([])
const workflowDefinitions = ref<WorkflowDefinitionDto[]>([])
const minutesUsers = reactive<Record<string, MinutesConfirmationUserDto[]>>({})
const typeFilter = ref<'all' | ConfirmationType>(
  ['event', 'minutes', 'mcp', 'agent'].includes(String(route.query.type))
    ? String(route.query.type) as ConfirmationType
    : 'all',
)
const statusFilter = ref<'open' | 'all' | ConfirmationStatus>('open')
const selectedKey = ref(
  route.query.type && route.query.id
    ? `${String(route.query.type)}:${String(route.query.id)}`
    : '',
)
const minutesDraft = reactive<MinutesDraft>({
  content: '',
  assigneeId: '',
  dueLocal: '',
  priority: 'P1',
  projectId: '',
  businessObjectId: '',
  workflowVersionId: '',
  workflowInstanceId: '',
})

const sourceOptions = [
  { key: 'all', label: '全部来源' },
  { key: 'event', label: '事件确认' },
  { key: 'minutes', label: '听记行动项' },
  { key: 'mcp', label: '智能体确认' },
  { key: 'agent', label: '数字员工确认' },
] as const

const statusLabels: Record<ConfirmationStatus, string> = {
  Pending: '待确认',
  NeedsInformation: '待补充',
  Expired: '已过期',
  Confirmed: '已确认',
  Rejected: '已拒绝',
  Failed: '失败',
}

const sourceByMinutes = computed(() =>
  new Map(minutesSources.value.map((item) => [item.id, item])),
)

const allItems = computed<ConfirmationItem[]>(() => {
  const items: ConfirmationItem[] = []
  for (const event of events.value) {
    const status = eventConfirmationStatus(event.status)
    items.push({
      key: `event:${event.id}`,
      type: 'event',
      id: event.id,
      status,
      title: event.title,
      summary: event.suggestion?.evidenceExcerpt || event.rawText,
      contextTitle: event.suggestion?.suggestedProjectName || event.departmentName,
      sourceLabel: '事件确认',
      assigneeName: event.suggestion?.suggestedAssigneeDisplayName || '待选择',
      dueAt: event.suggestion?.suggestedDueAt ?? null,
      actionLabel: status === 'Pending' ? '进入事件确认' : '查看事件',
      detailPath: `/inbox?event=${event.id}`,
      requiresHumanBoundary: event.suggestion?.requiresHumanBoundary ?? false,
      event,
    })
  }

  for (const action of minutesActions.value) {
    const source = sourceByMinutes.value.get(action.minutesSourceId)
    const status = minutesConfirmationStatus(action)
    items.push({
      key: `minutes:${action.id}`,
      type: 'minutes',
      id: action.id,
      status,
      title: action.content,
      summary: action.sourceExcerpt,
      contextTitle: source?.title || '听记行动项',
      sourceLabel: '听记行动项',
      assigneeName: action.assigneeDisplayName || '待选择',
      dueAt: action.dueAt,
      actionLabel: status === 'Pending' || status === 'NeedsInformation'
        ? '编辑行动项'
        : '查看行动项',
      detailPath: `/minutes?source=${action.minutesSourceId}&actionItem=${action.id}`,
      requiresHumanBoundary: false,
      minutesAction: action,
      minutesSource: source,
    })
  }

  for (const view of mcpViews.value) {
    const status = mcpConfirmationStatus(view.status, view.expiresAt)
    items.push({
      key: `mcp:${view.id}`,
      type: 'mcp',
      id: view.id,
      status,
      title: view.actionTitle,
      summary: view.summary,
      contextTitle: view.projectName || view.resourceDisplayName,
      sourceLabel: '智能体确认',
      assigneeName: '当前真人',
      dueAt: view.expiresAt,
      actionLabel: status === 'Pending' ? '进入业务确认' : '查看确认结果',
      detailPath: `/mcp-confirmations?confirmationId=${view.id}`,
      requiresHumanBoundary: true,
      mcp: view,
    })
  }

  for (const run of agentRuns.value) {
    const status = agentConfirmationStatus(run.status)
    items.push({
      key: `agent:${run.id}`,
      type: 'agent',
      id: run.id,
      status,
      title: run.action === 'Draft' ? '数字员工生成业务草稿' : `数字员工动作：${run.action}`,
      summary: `风险 ${run.riskLevel}，金额 ${run.amount}，状态 ${run.status}`,
      contextTitle: run.subjectType || '数字员工运行',
      sourceLabel: '数字员工确认',
      assigneeName: '当前真人',
      dueAt: null,
      actionLabel: status === 'Pending' ? '进入人工确认' : '查看运行记录',
      detailPath: `/agents?run=${run.id}`,
      requiresHumanBoundary: run.riskLevel === 'High',
      agent: run,
    })
  }

  return items.sort((left, right) => {
    const rank = (status: ConfirmationStatus) =>
      status === 'NeedsInformation' ? 0
        : status === 'Pending' ? 1
          : status === 'Expired' ? 2
            : 3
    return rank(left.status) - rank(right.status) || left.title.localeCompare(right.title, 'zh-CN')
  })
})

const filteredItems = computed(() =>
  allItems.value.filter((item) => {
    if (typeFilter.value !== 'all' && item.type !== typeFilter.value) return false
    if (statusFilter.value === 'open') {
      return item.status === 'Pending' || item.status === 'NeedsInformation'
    }
    return statusFilter.value === 'all' || item.status === statusFilter.value
  }),
)

const selected = computed(() =>
  allItems.value.find((item) => item.key === selectedKey.value)
  ?? filteredItems.value[0]
  ?? null,
)
const pendingCount = computed(() =>
  allItems.value.filter((item) => item.status === 'Pending').length,
)
const needsInfoCount = computed(() =>
  allItems.value.filter((item) => item.status === 'NeedsInformation').length,
)
const deniedCount = computed(() => sourceErrors.value.length)
const selectedMinutesUsers = computed(() => {
  const departmentId = selected.value?.minutesSource?.departmentId
  return departmentId ? minutesUsers[departmentId] ?? [] : []
})
const selectedProjects = computed(() =>
  projects.value.filter(
    (item) => !selected.value?.minutesSource
      || (
        item.departmentId === selected.value.minutesSource.departmentId
        && item.environmentCode === selected.value.minutesSource.environmentCode
      ),
  ),
)
const selectedBusinessObjects = computed(() =>
  businessObjects.value.filter(
    (item) => !selected.value?.minutesSource
      || (
        item.departmentId === selected.value.minutesSource.departmentId
        && item.environmentCode === selected.value.minutesSource.environmentCode
      ),
  ),
)
const selectedWorkflows = computed(() =>
  workflows.value.filter(
    (item) => !selected.value?.minutesSource
      || (
        item.departmentId === selected.value.minutesSource.departmentId
        && item.environmentCode === selected.value.minutesSource.environmentCode
        && (item.status === 'Running' || item.status === 'Waiting')
      ),
  ),
)
const canConfirmMinutes = computed(() =>
  selected.value?.type === 'minutes'
  && selected.value.minutesAction
    ? canConfirmMinutesAction({
        status: selected.value.minutesAction.status,
        assigneeId: minutesDraft.assigneeId || null,
        dueAt: fromShanghaiInput(minutesDraft.dueLocal),
        content: minutesDraft.content,
      })
    : false,
)

async function load() {
  loading.value = true
  sourceErrors.value = []
  const [
    eventResult,
    sourceResult,
    mcpResult,
    agentResult,
    projectResult,
    objectResult,
    workflowResult,
    definitionResult,
  ] = await Promise.allSettled([
    workspaceApi.inbox(false),
    workspaceApi.minutes({ includeTestData: false }),
    mcpApi.getOwnConfirmationViews(),
    workspaceApi.agentRuns(),
    workspaceApi.projects(false),
    workspaceApi.businessObjects(false),
    workspaceApi.workflowInstances(false),
    workspaceApi.workflowDefinitions(),
  ])

  const take = <T>(result: PromiseSettledResult<T>, setter: (value: T) => void, label: string) => {
    if (result.status === 'fulfilled') {
      setter(result.value)
    } else {
      sourceErrors.value.push(label)
    }
  }

  take(eventResult, (value) => { events.value = value.items }, '事件确认')
  take(sourceResult, (value) => { minutesSources.value = value.items }, '听记行动项')
  take(mcpResult, (value) => { mcpViews.value = value }, '智能体确认')
  take(agentResult, (value) => { agentRuns.value = value.items }, '数字员工确认')
  take(projectResult, (value) => { projects.value = value.items }, '项目候选')
  take(objectResult, (value) => { businessObjects.value = value.items }, '业务对象候选')
  take(workflowResult, (value) => { workflows.value = value.items }, '已有流程')
  take(definitionResult, (value) => { workflowDefinitions.value = value }, '流程版本')

  if (sourceResult.status === 'fulfilled') {
    const actionResults = await Promise.allSettled(
      sourceResult.value.items.map(async (source) => ({
        sourceId: source.id,
        actions: await workspaceApi.minutesActionItems(source.id),
      })),
    )
    minutesActions.value = actionResults.flatMap((result) =>
      result.status === 'fulfilled' ? result.value.actions : [],
    )
    const departmentIds = [...new Set(sourceResult.value.items.map((item) => item.departmentId))]
    const userResults = await Promise.allSettled(
      departmentIds.map(async (departmentId) => ({
        departmentId,
        users: await workspaceApi.minutesConfirmationUsers(departmentId),
      })),
    )
    for (const result of userResults) {
      if (result.status === 'fulfilled') {
        minutesUsers[result.value.departmentId] = result.value.users
      }
    }
    if (actionResults.some((result) => result.status === 'rejected')) {
      sourceErrors.value.push('部分听记行动项')
    }
  }

  if (!selected.value && filteredItems.value[0]) {
    selectedKey.value = filteredItems.value[0].key
  }
  syncMinutesDraft()
  loading.value = false
}

function syncMinutesDraft() {
  const action = selected.value?.minutesAction
  if (!action) return
  minutesError.value = ''
  minutesDraft.content = action.content
  minutesDraft.assigneeId = action.assigneeId || action.suggestedAssigneeId || ''
  minutesDraft.dueLocal = toShanghaiInput(action.dueAt || action.suggestedDueAt)
  minutesDraft.priority = action.priority === 'P0' || action.priority === 'P2'
    ? action.priority
    : 'P1'
  minutesDraft.projectId = action.projectId || ''
  minutesDraft.businessObjectId = action.businessObjectId || ''
  minutesDraft.workflowVersionId = action.workflowVersionId || action.suggestedWorkflowVersionId || ''
  minutesDraft.workflowInstanceId = action.workflowInstanceId || ''
}

async function selectItem(item: ConfirmationItem) {
  selectedKey.value = item.key
  await router.replace({ path: '/confirmations', query: { type: item.type, id: item.id } })
  syncMinutesDraft()
  document.querySelector(`[data-confirmation-key="${item.key}"]`)?.scrollIntoView({
    behavior: 'smooth',
    block: 'center',
  })
}

function minutesPayload() {
  const action = selected.value?.minutesAction
  if (!action) return null
  return {
    ...action,
    content: minutesDraft.content,
    assigneeId: minutesDraft.assigneeId || null,
    dueAt: fromShanghaiInput(minutesDraft.dueLocal),
    priority: minutesDraft.priority,
    projectId: minutesDraft.projectId || null,
    businessObjectId: minutesDraft.businessObjectId || null,
    workflowVersionId: minutesDraft.workflowVersionId || null,
    workflowInstanceId: minutesDraft.workflowInstanceId || null,
  }
}

async function saveMinutes() {
  const item = minutesPayload()
  if (!item) return
  saving.value = true
  minutesError.value = ''
  try {
    const updated = await workspaceApi.updateMinutesAction(item.minutesSourceId, item)
    ElMessage.success(
      updated.assigneeId && updated.dueAt
        ? '人工修改已保存'
        : '草稿已保存；补齐负责人和截止时间后才能确认',
    )
    await load()
    selectedKey.value = `minutes:${updated.id}`
    syncMinutesDraft()
  } catch (error) {
    minutesError.value = error instanceof ApiError ? error.message : '保存听记行动项失败'
    ElMessage.error(minutesError.value)
  } finally {
    saving.value = false
  }
}

async function confirmMinutes() {
  const item = minutesPayload()
  if (!item) return
  if (!canConfirmMinutes.value) {
    ElMessage.warning('请先补齐负责人和截止时间')
    return
  }
  saving.value = true
  minutesError.value = ''
  try {
    const task = await workspaceApi.confirmMinutesAction(item)
    ElMessage.success(`任务已创建：${task.title}`)
    await load()
    selectedKey.value = `minutes:${item.id}`
    syncMinutesDraft()
  } catch (error) {
    minutesError.value = error instanceof ApiError ? error.message : '确认听记行动项失败'
    ElMessage.error(minutesError.value)
  } finally {
    saving.value = false
  }
}

function typeLabel(type: ConfirmationType) {
  return {
    event: '事件',
    minutes: '听记',
    mcp: '智能体',
    agent: '数字员工',
  }[type]
}

function statusTone(status: ConfirmationStatus) {
  if (status === 'Confirmed') return 'success'
  if (status === 'Rejected' || status === 'Failed') return 'danger'
  if (status === 'Expired') return 'info'
  return 'warning'
}

watch(
  () => [typeFilter.value, statusFilter.value],
  () => {
    if (!filteredItems.value.some((item) => item.key === selectedKey.value)) {
      selectedKey.value = filteredItems.value[0]?.key ?? ''
    }
    syncMinutesDraft()
  },
)

onMounted(load)
</script>

<template>
  <div class="page-shell" data-testid="confirmation-center-page">
    <MainColumnPageHeader
      intro-key="confirmations"
      title="统一确认"
      scope-label="当前身份的全部授权确认来源"
      kicker="HUMAN CONFIRMATION CENTER"
    >
      <template #actions>
        <div class="head-stats">
          <article><span>待确认</span><strong>{{ pendingCount }}</strong></article>
          <article><span>待补充</span><strong>{{ needsInfoCount }}</strong></article>
          <article><span>无权限来源</span><strong>{{ deniedCount }}</strong></article>
        </div>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="sourceErrors.length"
      type="warning"
      :closable="false"
      show-icon
      :title="`部分来源无权限或加载失败：${sourceErrors.join('、')}`"
    />

    <section class="confirmation-toolbar section-card">
      <div>
        <el-radio-group v-model="typeFilter">
          <el-radio-button
            v-for="option in sourceOptions"
            :key="option.key"
            :value="option.key"
          >
            {{ option.label }}
          </el-radio-button>
        </el-radio-group>
      </div>
      <el-select v-model="statusFilter" placeholder="确认状态">
        <el-option label="待处理" value="open" />
        <el-option label="全部状态" value="all" />
        <el-option label="待确认" value="Pending" />
        <el-option label="待补充" value="NeedsInformation" />
        <el-option label="已过期" value="Expired" />
        <el-option label="已确认" value="Confirmed" />
        <el-option label="已拒绝" value="Rejected" />
      </el-select>
    </section>

    <section v-loading="loading" class="confirmation-layout">
      <div class="confirmation-list">
        <button
          v-for="item in filteredItems"
          :key="item.key"
          type="button"
          class="confirmation-item"
          :class="{ active: item.key === selected?.key }"
          :data-confirmation-key="item.key"
          @click="selectItem(item)"
        >
          <span class="item-source">{{ typeLabel(item.type) }} · {{ item.sourceLabel }}</span>
          <strong>{{ item.title }}</strong>
          <small>{{ item.contextTitle }} · {{ item.assigneeName }}</small>
          <span class="item-foot">
            <el-tag :type="statusTone(item.status)" size="small">
              {{ statusLabels[item.status] }}
            </el-tag>
            <time>{{ formatShanghaiDate(item.dueAt) }}</time>
          </span>
        </button>
        <el-empty
          v-if="!loading && filteredItems.length === 0"
          :description="sourceErrors.length
            ? '没有可访问的确认来源'
            : '当前筛选下没有确认事项'"
        />
      </div>

      <article v-if="selected" class="confirmation-detail section-card">
        <div class="detail-head">
          <div>
            <span class="item-source">{{ selected.sourceLabel }}</span>
            <h2>{{ selected.title }}</h2>
            <p>{{ selected.contextTitle }}</p>
          </div>
          <el-tag :type="statusTone(selected.status)" effect="dark">
            {{ statusLabels[selected.status] }}
          </el-tag>
        </div>

        <div class="detail-meta">
          <div><span>负责人</span><strong>{{ selected.assigneeName }}</strong></div>
          <div><span>截止</span><strong>{{ formatShanghaiDate(selected.dueAt) }}</strong></div>
          <div><span>来源</span><strong>{{ selected.sourceLabel }}</strong></div>
        </div>

        <el-alert
          v-if="selected.requiresHumanBoundary"
          type="warning"
          :closable="false"
          show-icon
          title="永久人工边界：AI 不得自动付款、质量放行、修改配方、控制设备、解除联锁、承诺合同或重大赔付。"
        />

        <section v-if="selected.type === 'minutes' && selected.minutesAction" class="minutes-editor">
          <div class="editor-grid">
            <el-form-item label="负责人">
              <el-select
                v-model="minutesDraft.assigneeId"
                data-testid="minutes-assignee"
                clearable
                filterable
                placeholder="选择听记部门成员"
              >
                <el-option
                  v-for="user in selectedMinutesUsers"
                  :key="user.id"
                  :label="`${user.displayName} · ${user.departmentName}`"
                  :value="user.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="截止时间（Asia/Shanghai）">
              <el-date-picker
                v-model="minutesDraft.dueLocal"
                type="datetime"
                format="YYYY-MM-DD HH:mm"
                value-format="YYYY-MM-DDTHH:mm:ss"
                placeholder="选择截止时间"
                style="width: 100%"
              />
            </el-form-item>
            <el-form-item label="优先级">
              <el-radio-group v-model="minutesDraft.priority">
                <el-radio-button value="P0">P0</el-radio-button>
                <el-radio-button value="P1">P1</el-radio-button>
                <el-radio-button value="P2">P2</el-radio-button>
              </el-radio-group>
            </el-form-item>
            <el-form-item label="项目">
              <el-select
                v-model="minutesDraft.projectId"
                data-testid="minutes-project"
                clearable
                filterable
                placeholder="关联项目"
              >
                <el-option
                  v-for="project in selectedProjects"
                  :key="project.id"
                  :label="`${project.name} · ${project.objectCode}`"
                  :value="project.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="业务对象">
              <el-select
                v-model="minutesDraft.businessObjectId"
                data-testid="minutes-business-object"
                clearable
                filterable
                placeholder="关联业务对象"
              >
                <el-option
                  v-for="businessObject in selectedBusinessObjects"
                  :key="businessObject.id"
                  :label="`${businessObject.objectCode} · ${businessObject.title || '未命名'}`"
                  :value="businessObject.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="流程版本">
              <el-select
                v-model="minutesDraft.workflowVersionId"
                clearable
                filterable
                placeholder="选择已发布流程版本"
              >
                <el-option
                  v-for="definition in workflowDefinitions"
                  :key="definition.id"
                  :label="`${definition.name} · V${definition.versionNumber}`"
                  :value="definition.versionId"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="关联已有流程">
              <el-select
                v-model="minutesDraft.workflowInstanceId"
                data-testid="minutes-existing-workflow"
                clearable
                filterable
                placeholder="关联已有流程"
              >
                <el-option
                  v-for="workflow in selectedWorkflows"
                  :key="workflow.id"
                  :label="`${workflow.status} · ${workflow.currentNodeCode || '已结束'} · ${formatShanghaiDate(workflow.startedAt)}`"
                  :value="workflow.id"
                />
              </el-select>
            </el-form-item>
          </div>

          <el-form-item label="可编辑任务内容">
            <el-input
              v-model="minutesDraft.content"
              data-testid="minutes-content"
              type="textarea"
              :rows="4"
              maxlength="2000"
              show-word-limit
            />
          </el-form-item>

          <el-alert
            v-if="minutesError"
            type="error"
            :closable="false"
            show-icon
            :title="minutesError"
          />

          <div class="info-warning">
            <strong>{{ canConfirmMinutes ? '信息完整，可确认创建任务' : '信息不完整，可保存草稿但不能确认' }}</strong>
            <span v-if="!canConfirmMinutes">负责人和截止建议时间缺失时，统一页会保持“待补充”。</span>
          </div>

          <div class="detail-actions">
            <el-button
              data-testid="save-minutes-action"
              :loading="saving"
              @click="saveMinutes"
            >
              保存人工修改
            </el-button>
            <el-button
              data-testid="confirm-minutes-action"
              type="primary"
              :disabled="!canConfirmMinutes"
              :loading="saving"
              @click="confirmMinutes"
            >
              确认并创建任务
            </el-button>
          </div>
        </section>

        <section v-else class="external-detail">
          <p>{{ selected.summary }}</p>
          <el-button type="primary" @click="router.push(selected.detailPath)">
            {{ selected.actionLabel }}
          </el-button>
        </section>
      </article>

      <el-empty v-else description="选择一条待确认事项" />
    </section>
  </div>
</template>

<style scoped>
.page-head, .confirmation-toolbar, .detail-head, .detail-actions, .item-foot {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
}
.head-stats { display: grid; grid-template-columns: repeat(3, minmax(90px, 1fr)); gap: 8px; }
.head-stats article { min-width: 92px; border: 1px solid var(--aidt-border); border-radius: 14px; background: #fff; padding: 12px; }
.head-stats span, .head-stats strong { display: block; }
.head-stats span { color: var(--aidt-ink-muted); font-size: .7rem; }
.head-stats strong { margin-top: 4px; color: var(--aidt-brand-strong); font-size: 1.45rem; }
.confirmation-toolbar { align-items: center; flex-wrap: wrap; }
.confirmation-toolbar :deep(.el-select) { width: 170px; }
.confirmation-layout { display: grid; grid-template-columns: minmax(280px, .42fr) minmax(0, 1fr); gap: 16px; min-height: 420px; }
.confirmation-list { display: grid; align-content: start; gap: 9px; min-width: 0; }
.confirmation-item { display: grid; min-width: 0; gap: 7px; border: 1px solid var(--aidt-border); border-radius: 16px; background: #fff; padding: 14px; text-align: left; cursor: pointer; }
.confirmation-item.active { border-color: #8cbda3; background: #f2f9f5; box-shadow: 0 8px 24px rgb(27 94 65 / 8%); }
.confirmation-item strong { overflow: hidden; text-overflow: ellipsis; }
.confirmation-item small { color: var(--aidt-ink-muted); }
.item-source { color: var(--aidt-brand); font-size: .68rem; font-weight: 800; letter-spacing: .08em; }
.item-foot { align-items: center; }
.item-foot time { color: var(--aidt-ink-muted); font-size: .7rem; }
.confirmation-detail { display: grid; gap: 18px; align-self: start; min-width: 0; }
.detail-head h2 { margin: 5px 0 0; }
.detail-head p { margin: 5px 0 0; color: var(--aidt-ink-muted); }
.detail-meta, .editor-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.detail-meta div { border-radius: 13px; background: #f5f8f6; padding: 12px; }
.detail-meta span, .detail-meta strong { display: block; }
.detail-meta span { color: var(--aidt-ink-muted); font-size: .7rem; }
.detail-meta strong { margin-top: 4px; }
.minutes-editor { display: grid; gap: 12px; }
.info-warning { display: grid; gap: 4px; border: 1px solid #e4c78c; border-radius: 13px; background: #fff8e9; padding: 12px 14px; }
.info-warning span { color: #8e6515; font-size: .74rem; }
.detail-actions { justify-content: flex-end; flex-wrap: wrap; }
.external-detail { display: grid; gap: 14px; justify-items: start; border-radius: 15px; background: #f6f8f7; padding: 16px; }
.external-detail p { margin: 0; color: var(--aidt-ink-muted); line-height: 1.75; }
@media (max-width: 900px) {
  .page-head { flex-direction: column; }
  .confirmation-layout { grid-template-columns: 1fr; }
  .confirmation-list { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (max-width: 620px) {
  .head-stats, .detail-meta, .editor-grid, .confirmation-list { grid-template-columns: 1fr; }
  .confirmation-toolbar :deep(.el-segmented) { overflow-x: auto; max-width: 100%; }
  .detail-actions .el-button { width: 100%; }
}
</style>
