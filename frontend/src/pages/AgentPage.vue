<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { agentApi } from '@/api/modules/agentApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  AgentIdentityOptionsDto,
  AgentProfileChangeRequestDto,
  AgentProfileContextDto,
  AgentProfileDetailDto,
  AgentProfilePublishResultDto,
  AgentProfileValidationDto,
  AgentProfileVersionDto,
  AgentRunDto,
  AgentRunEvidenceDto,
  ConnectorDto,
  CreateAgentProfileDraftRequest,
  UpdateAgentProfileDraftRequest,
} from '@/api/types/contracts'
import FeaturePlaceholder from '@/components/business/FeaturePlaceholder.vue'
import StatusBadge from '@/components/base/StatusBadge.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import AgentIdentityEditor from '@/features/agents/AgentIdentityEditor.vue'
import {
  type AgentIdentityFormValue,
  copyAgentForm,
  emptyAgentForm,
  formFromVersion,
} from '@/features/agents/agentForm'
import {
  actionLabels,
  autonomyLabels,
  confirmationLabels,
  formatAmount,
  formatDataScope,
  formatTime,
  formatToolName,
  lifecyclePresentation,
  riskLabels,
  runStatusPresentation,
  stopConditionLabels,
  versionStatusPresentation,
} from '@/features/agents/agentPresentation'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

const loading = ref(true)
const listLoading = ref(false)
const detailLoading = ref(false)
const errorMessage = ref('')
const actionLoading = ref('')
const profiles = ref<AgentProfileDetailDto[]>([])
const runs = ref<AgentRunDto[]>([])
const connectors = ref<ConnectorDto[]>([])
const options = ref<AgentIdentityOptionsDto | null>(null)
const selectedProfileId = ref('')
const selectedProfile = ref<AgentProfileDetailDto | null>(null)
const context = ref<AgentProfileContextDto | null>(null)
const versions = ref<AgentProfileVersionDto[]>([])
const changeRequests = ref<AgentProfileChangeRequestDto[]>([])
const activeDetailTab = ref('versions')

const editorVisible = ref(false)
const editorMode = ref<'create' | 'edit'>('create')
const editorInitial = ref<AgentIdentityFormValue>(emptyAgentForm(null))
const editorVersionId = ref('')
const editorRevision = ref(0)
const editorValidation = ref<AgentProfileValidationDto | null>(null)
const editorSaving = ref(false)
const editorValidating = ref(false)
const editorPublishing = ref(false)

const filters = reactive({
  search: '',
  departmentCode: '',
  lifecycleStatus: '',
})

const canCreate = computed(() => auth.can('agent:create'))
const canReviewHighRisk = computed(() => auth.isPlatformAdmin && auth.can('agent:approve'))
const canStartRun = computed(() => auth.can('agent-run:submit'))
const canConfirmRun = computed(() => auth.can('agent-run:approve'))
const canStopRun = computed(() => auth.can('agent-run:stop'))
const canExecuteRun = computed(() => auth.can('agent-run:update'))

const selectedCanManage = computed(() => {
  const profile = selectedProfile.value
  if (!profile) return false
  if (auth.isPlatformAdmin) return true
  return options.value?.departments.some(
    (department) => department.id === profile.departmentId && department.isManager,
  ) ?? false
})

const latestDraft = computed(() =>
  versions.value.find((item) => item.status === 'Draft' || item.status === 'Validated'),
)

const departmentNames = computed(() => new Map(
  (options.value?.departments ?? []).map((department) => [department.id, department.name]),
))

const selectedRun = computed(() => {
  const runId = String(route.query.run ?? '')
  if (!runId) return null
  return context.value?.recentRuns.find((item) => item.id === runId)
    ?? null
})

const pendingReviewRequests = computed(() =>
  changeRequests.value.filter((item) => item.status === 'Pending'),
)

const lifecycleOptions = computed(() => {
  const values = ['Draft', 'Published', 'Disabled', 'Retired'] as const
  return values
    .filter((value) => auth.isPlatformAdmin || selectedCanManage.value || value === 'Published')
    .map((value) => ({ value, label: lifecyclePresentation[value].label }))
})

const effectiveCount = computed(() =>
  profiles.value.filter((item) => item.lifecycleStatus === 'Published').length,
)
const draftCount = computed(() =>
  profiles.value.filter((item) => item.lifecycleStatus === 'Draft').length,
)
const disabledCount = computed(() =>
  profiles.value.filter((item) => item.lifecycleStatus === 'Disabled').length,
)

function errorText(error: unknown, fallback: string): string {
  if (!(error instanceof ApiError)) return fallback
  const actionHint: Record<string, string> = {
    AGENT_STOPPED: '该数字员工未发布或已停用，请先恢复并发布新版本。',
    AGENT_PROFILE_NOT_PUBLISHED: '当前没有可用的已发布版本，请先完成校验和发布。',
    AGENT_SELF_REVIEW_DENIED: '申请人不能审核自己的高风险变更，请由第二位平台管理员审核。',
    CONFLICT: '身份数据已更新，请刷新后重新操作。',
    DEPARTMENT_SCOPE_DENIED: '该身份不属于您的授权部门。',
    PERMISSION_DENIED: '当前身份没有执行该操作的权限。',
  }
  return `${actionHint[error.code] ?? error.message}（Trace：${error.traceId}）`
}

async function loadAll() {
  loading.value = true
  errorMessage.value = ''
  try {
    await auth.initialize()
    options.value = await agentApi.options()
    const [profilePage, runPage] = await Promise.all([
      agentApi.list({
        pageSize: 100,
        search: filters.search || undefined,
        departmentCode: filters.departmentCode || undefined,
        lifecycleStatus: filters.lifecycleStatus || undefined,
      }),
      workspaceApi.agentRuns(),
    ])
    profiles.value = profilePage.items
    runs.value = runPage.items
    try {
      connectors.value = await workspaceApi.connectors()
    } catch {
      connectors.value = []
    }
    changeRequests.value = canReviewHighRisk.value
      ? await agentApi.changeRequests('Pending')
      : []

    const requestedRunId = String(route.query.run ?? '')
    const requestedProfileId = String(route.query.profile ?? '')
    const runProfileId = requestedRunId
      ? runs.value.find((item) => item.id === requestedRunId)?.agentProfileId
      : undefined
    const profileId = requestedProfileId
      || runProfileId
      || selectedProfileId.value
      || profiles.value[0]?.id
      || ''
    if (profileId) {
      await selectProfileById(profileId)
    } else {
      selectedProfileId.value = ''
      selectedProfile.value = null
      context.value = null
      versions.value = []
    }
  } catch (error) {
    errorMessage.value = errorText(error, '无法加载数字员工身份中心。')
  } finally {
    loading.value = false
  }
}

async function applyFilters() {
  listLoading.value = true
  errorMessage.value = ''
  try {
    const page = await agentApi.list({
      pageSize: 100,
      search: filters.search || undefined,
      departmentCode: filters.departmentCode || undefined,
      lifecycleStatus: filters.lifecycleStatus || undefined,
    })
    profiles.value = page.items
    if (!profiles.value.some((item) => item.id === selectedProfileId.value)) {
      const first = profiles.value[0]
      if (first) await selectProfileById(first.id)
      else {
        selectedProfileId.value = ''
        selectedProfile.value = null
        context.value = null
        versions.value = []
      }
    }
  } catch (error) {
    errorMessage.value = errorText(error, '筛选数字员工失败。')
  } finally {
    listLoading.value = false
  }
}

async function selectProfileById(profileId: string) {
  selectedProfileId.value = profileId
  detailLoading.value = true
  errorMessage.value = ''
  try {
    const [profileContext, versionRows] = await Promise.all([
      agentApi.context(profileId),
      auth.can('agent:read') ? agentApi.versions(profileId).catch(() => []) : Promise.resolve([]),
    ])
    selectedProfile.value = profileContext.profile
    context.value = profileContext
    versions.value = versionRows
    if (route.query.run) activeDetailTab.value = 'runs'
  } catch (error) {
    selectedProfile.value = null
    context.value = null
    versions.value = []
    errorMessage.value = errorText(error, '无法读取数字员工详情。')
  } finally {
    detailLoading.value = false
  }
}

async function refreshSelected(profileId = selectedProfileId.value) {
  await loadAll()
  if (profileId) await selectProfileById(profileId)
}

function openCreate() {
  editorMode.value = 'create'
  editorInitial.value = emptyAgentForm(options.value)
  editorVersionId.value = ''
  editorRevision.value = 0
  editorValidation.value = null
  editorVisible.value = true
}

async function openCopy() {
  if (!selectedProfile.value) return
  options.value = await agentApi.options()
  editorMode.value = 'create'
  editorInitial.value = copyAgentForm(selectedProfile.value)
  editorVersionId.value = ''
  editorRevision.value = 0
  editorValidation.value = null
  editorVisible.value = true
}

async function openEdit() {
  const profile = selectedProfile.value
  if (!profile) return
  if (!selectedCanManage.value) {
    ElMessage.warning('当前身份只能查看该数字员工，不能编辑。')
    return
  }
  if (profile.lifecycleStatus === 'Disabled') {
    ElMessage.warning('该身份已停用，请先恢复，再创建新草稿。')
    return
  }
  actionLoading.value = 'open-edit'
  try {
    let versionRows = await agentApi.versions(profile.id)
    let draft = versionRows.find((item) => item.status === 'Draft' || item.status === 'Validated')
    if (!draft) {
      if (!profile.currentPublishedVersionId) {
        throw new Error('该身份没有可继续编辑的草稿或已发布版本。')
      }
      draft = await agentApi.createNextDraft(profile.id, {
        expectedVersion: profile.version,
        reason: '网页编辑已发布身份，创建新草稿版本',
      })
      versionRows = await agentApi.versions(profile.id)
    }
    options.value = await agentApi.options(profile.id)
    versions.value = versionRows
    editorMode.value = 'edit'
    editorInitial.value = formFromVersion(draft)
    editorVersionId.value = draft.id
    editorRevision.value = draft.revision
    editorValidation.value = null
    editorVisible.value = true
  } catch (error) {
    errorMessage.value = errorText(error, '无法创建编辑草稿。')
  } finally {
    actionLoading.value = ''
  }
}

function createRequest(value: AgentIdentityFormValue): CreateAgentProfileDraftRequest {
  return {
    code: value.code,
    name: value.name,
    description: value.description,
    serviceUserId: value.serviceUserId,
    departmentId: value.departmentId,
    businessOwnerUserId: value.businessOwnerUserId,
    technicalOwnerUserId: value.technicalOwnerUserId,
    autonomyLevel: value.autonomyLevel,
    riskLimit: value.riskLimit,
    amountLimit: value.amountLimit,
    dataScopes: value.dataScopes,
    modelScopes: value.modelScopes,
    allowedTools: value.allowedTools,
    requiredConfirmations: value.requiredConfirmations,
    stopConditions: value.stopConditions,
  }
}

function updateRequest(value: AgentIdentityFormValue): UpdateAgentProfileDraftRequest {
  return {
    expectedRevision: editorRevision.value,
    name: value.name,
    description: value.description,
    serviceUserId: value.serviceUserId,
    autonomyLevel: value.autonomyLevel,
    riskLimit: value.riskLimit,
    amountLimit: value.amountLimit,
    dataScopes: value.dataScopes,
    modelScopes: value.modelScopes,
    allowedTools: value.allowedTools,
    requiredConfirmations: value.requiredConfirmations,
    stopConditions: value.stopConditions,
  }
}

async function saveEditor(value: AgentIdentityFormValue) {
  editorSaving.value = true
  errorMessage.value = ''
  try {
    if (editorMode.value === 'create') {
      const created = await agentApi.createDraft(createRequest(value))
      editorVisible.value = false
      ElMessage.success('数字员工草稿已保存，下一步请继续编辑并校验。')
      await loadAll()
      await selectProfileById(created.id)
      return
    }

    const profileId = selectedProfileId.value
    if (!profileId || !editorVersionId.value) throw new Error('缺少草稿版本信息，请重新打开编辑。')
    const updated = await agentApi.updateDraft(
      profileId,
      editorVersionId.value,
      updateRequest(value),
    )
    editorRevision.value = updated.revision
    editorInitial.value = formFromVersion(updated)
    editorValidation.value = null
    versions.value = await agentApi.versions(profileId)
    ElMessage.success('草稿已保存，可继续校验或发布。')
  } catch (error) {
    errorMessage.value = errorText(error, '保存数字员工草稿失败。')
  } finally {
    editorSaving.value = false
  }
}

async function validateEditor() {
  const profileId = selectedProfileId.value
  if (!profileId || !editorVersionId.value) return
  editorValidating.value = true
  try {
    const validation = await agentApi.validate(profileId, editorVersionId.value, {
      expectedRevision: editorRevision.value,
    })
    editorValidation.value = validation
    const versionRows = await agentApi.versions(profileId)
    versions.value = versionRows
    const current = versionRows.find((item) => item.id === editorVersionId.value)
    if (current) editorRevision.value = current.revision
    if (validation.isValid) {
      ElMessage.success(validation.isHighRisk ? '校验通过；发布后将进入双人审核。' : '校验通过。')
    } else {
      ElMessage.error(`校验未通过：${validation.errors.join('；')}`)
    }
  } catch (error) {
    errorMessage.value = errorText(error, '校验失败，请处理业务提示后重试。')
  } finally {
    editorValidating.value = false
  }
}

async function publishEditor() {
  const profileId = selectedProfileId.value
  if (!profileId || !editorVersionId.value || !editorValidation.value?.isValid) return
  editorPublishing.value = true
  try {
    const result = await agentApi.publish(profileId, editorVersionId.value, {
      expectedRevision: editorRevision.value,
      reason: '从数字员工身份中心发布',
    })
    applyPublishResult(result)
    editorVisible.value = false
    await loadAll()
  } catch (error) {
    errorMessage.value = errorText(error, '发布失败，请刷新后重试。')
  } finally {
    editorPublishing.value = false
  }
}

function applyPublishResult(result: AgentProfilePublishResultDto) {
  if (result.status === 'Published') {
    ElMessage.success('新版本已发布并成为唯一有效版本。')
    return
  }
  if (result.status === 'AwaitingApproval') {
    ElMessage.warning('该变更提升能力边界，已提交第二位平台管理员审核。')
    return
  }
  if (result.status === 'Rejected') {
    ElMessage.error('高风险变更已被驳回，请查看审核意见。')
    return
  }
  ElMessage.info('版本已校验，尚未发布。')
}

async function disableProfile() {
  const profile = selectedProfile.value
  if (!profile || !selectedCanManage.value) return
  const result = await ElMessageBox.prompt(
    '停用后新运行、新流程节点和新服务令牌会立即被拒绝，已有服务令牌将撤销。',
    '停用数字员工',
    {
      type: 'warning',
      inputType: 'textarea',
      inputPlaceholder: '填写停用原因',
      confirmButtonText: '确认停用',
      cancelButtonText: '取消',
      inputValidator: (value) => Boolean(value?.trim()) || '必须填写停用原因',
    },
  )
  actionLoading.value = 'disable'
  try {
    await agentApi.disable(profile.id, {
      expectedVersion: profile.version,
      reason: result.value.trim(),
    })
    ElMessage.success('数字员工已停用，服务令牌已撤销。')
    await loadAll()
  } catch (error) {
    errorMessage.value = errorText(error, '停用数字员工失败。')
  } finally {
    actionLoading.value = ''
  }
}

async function resumeProfile() {
  const profile = selectedProfile.value
  if (!profile || !selectedCanManage.value) return
  const result = await ElMessageBox.prompt(
    '恢复前会重新校验当前发布版本、服务身份和权限边界。',
    '恢复数字员工',
    {
      type: 'warning',
      inputType: 'textarea',
      inputPlaceholder: '填写恢复原因',
      confirmButtonText: '确认恢复',
      cancelButtonText: '取消',
      inputValidator: (value) => Boolean(value?.trim()) || '必须填写恢复原因',
    },
  )
  actionLoading.value = 'resume'
  try {
    await agentApi.resume(profile.id, {
      expectedVersion: profile.version,
      reason: result.value.trim(),
    })
    ElMessage.success('数字员工已恢复；旧令牌不会重新生效。')
    await loadAll()
  } catch (error) {
    errorMessage.value = errorText(error, '恢复数字员工失败。')
  } finally {
    actionLoading.value = ''
  }
}

async function rollbackVersion(version: AgentProfileVersionDto) {
  const profile = selectedProfile.value
  if (!profile || !selectedCanManage.value) return
  await ElMessageBox.confirm(
    `回滚会以版本 v${version.versionNumber} 为内容创建新的下一版本，不会改写历史记录。`,
    '回滚到历史版本',
    {
      type: 'warning',
      confirmButtonText: '继续回滚',
      cancelButtonText: '取消',
    },
  )
  const reason = await ElMessageBox.prompt('请填写回滚原因。', '回滚原因', {
    inputType: 'textarea',
    confirmButtonText: '提交回滚',
    cancelButtonText: '取消',
    inputValidator: (value) => Boolean(value?.trim()) || '必须填写回滚原因',
  })
  actionLoading.value = `rollback-${version.id}`
  try {
    const result = await agentApi.rollback(profile.id, {
      expectedVersion: profile.version,
      targetVersionId: version.id,
      reason: reason.value.trim(),
    })
    applyPublishResult(result)
    await loadAll()
  } catch (error) {
    errorMessage.value = errorText(error, '回滚失败。')
  } finally {
    actionLoading.value = ''
  }
}

async function reviewChange(request: AgentProfileChangeRequestDto, approve: boolean) {
  const result = await ElMessageBox.prompt(
    approve
      ? '审核通过后，高风险版本将立即发布。请填写审核依据。'
      : '请填写驳回原因，申请人可据此修改后重新提交。',
    approve ? '通过高风险变更' : '驳回高风险变更',
    {
      type: 'warning',
      inputType: 'textarea',
      confirmButtonText: approve ? '审核通过' : '确认驳回',
      cancelButtonText: '取消',
      inputValidator: (value) => Boolean(value?.trim()) || '必须填写审核意见',
    },
  )
  actionLoading.value = `review-${request.id}`
  try {
    const versionRows = await agentApi.versions(request.agentProfileId)
    const version = versionRows.find((item) => item.id === request.agentProfileVersionId)
      ?? (() => { throw new Error('待审核版本已不存在。') })()
    const reviewResult = await agentApi.reviewChangeRequest(request.id, {
      approve,
      comment: result.value.trim(),
      expectedRevision: version.revision,
    })
    applyPublishResult(reviewResult)
    await loadAll()
  } catch (error) {
    errorMessage.value = errorText(error, '审核高风险变更失败。')
  } finally {
    actionLoading.value = ''
  }
}

async function startRun() {
  const profile = selectedProfile.value
  if (!profile || profile.lifecycleStatus !== 'Published') return
  const toolCode = profile.allowedTools[0]
  if (!toolCode) {
    ElMessage.warning('该数字员工没有可用工具，不能发起运行。')
    return
  }
  actionLoading.value = 'start-run'
  try {
    const run = await agentApi.startRun(profile.id, {
      action: toolCode === 'workflow.read' ? 'Read' : 'Draft',
      riskLevel: profile.riskLimit === 'Low' ? 'Low' : 'Medium',
      model: profile.modelScopes[0] ?? null,
      toolCode,
    })
    ElMessage.success(
      run.status === 'AwaitingConfirmation'
        ? '运行已创建，等待真人确认。'
        : '运行已创建并进入受控执行队列。',
    )
    await loadAll()
    activeDetailTab.value = 'runs'
    await router.replace({ query: { ...route.query, run: run.id } })
  } catch (error) {
    errorMessage.value = errorText(error, '发起数字员工运行失败。')
  } finally {
    actionLoading.value = ''
  }
}

async function confirmRun(run: AgentRunEvidenceDto) {
  await ElMessageBox.confirm(
    '该确认将由当前真人身份完成并写入审计，数字员工不能自确认。',
    '确认数字员工运行',
    { type: 'warning', confirmButtonText: '确认运行', cancelButtonText: '取消' },
  )
  actionLoading.value = `confirm-${run.id}`
  try {
    await workspaceApi.confirmAgentRun(run.id)
    ElMessage.success('真人确认已提交。')
    await refreshSelected()
  } catch (error) {
    errorMessage.value = errorText(error, '确认运行失败。')
  } finally {
    actionLoading.value = ''
  }
}

async function stopRun(run: AgentRunEvidenceDto) {
  await ElMessageBox.confirm(
    '紧急停止会立即终止当前运行，但保留输入、输出和审计记录。',
    '停止数字员工运行',
    { type: 'error', confirmButtonText: '立即停止', cancelButtonText: '取消' },
  )
  actionLoading.value = `stop-${run.id}`
  try {
    await workspaceApi.stopAgentRun(run.id)
    ElMessage.success('数字员工运行已停止。')
    await refreshSelected()
  } catch (error) {
    errorMessage.value = errorText(error, '停止运行失败。')
  } finally {
    actionLoading.value = ''
  }
}

async function executeRun(run: AgentRunEvidenceDto) {
  actionLoading.value = `execute-${run.id}`
  try {
    const result = await workspaceApi.executeAgentRun(run.id)
    ElMessage.success(
      result.call.status === 'Succeeded'
        ? '受控网关调用完成，外部模型未启用。'
        : '受控网关调用未完成，已保留失败证据。',
    )
    await refreshSelected()
  } catch (error) {
    errorMessage.value = errorText(error, '执行受控网关调用失败。')
  } finally {
    actionLoading.value = ''
  }
}

function tokenStatus(token: AgentProfileContextDto['mcpTokens'][number]) {
  if (token.revokedAt) return { label: '已撤销', tone: 'danger' as const }
  if (new Date(token.expiresAt).getTime() <= Date.now()) {
    return { label: '已过期', tone: 'neutral' as const }
  }
  return { label: '有效', tone: 'success' as const }
}

function versionStatus(row: AgentProfileVersionDto) {
  return versionStatusPresentation[row.status]
}

function runStatus(row: AgentRunEvidenceDto) {
  return runStatusPresentation[row.status]
}

function runRisk(row: AgentRunEvidenceDto): string {
  return riskLabels[row.riskLevel]
}

function versionRowClass({ row }: { row: AgentProfileVersionDto }): string {
  return row.id === String(route.query.run ?? '') ? 'focused-row' : ''
}

function runRowClass({ row }: { row: AgentRunEvidenceDto }): string {
  return row.id === String(route.query.run ?? '') ? 'focused-run-row' : ''
}

function openWorkflowInstance(row: { workflowInstanceId: string }) {
  router.push({ path: '/workflows', query: { instance: row.workflowInstanceId } })
}

function auditActionLabel(action: string): string {
  return {
    'agent.profile.draft_created': '创建草稿',
    'agent.profile.next_draft_created': '创建后续草稿',
    'agent.profile.draft_updated': '保存草稿',
    'agent.profile.validated': '校验身份',
    'agent.profile.published': '发布版本',
    'agent.profile.disabled': '停用并撤销令牌',
    'agent.profile.resumed': '恢复身份',
    'agent.profile.high_risk_submitted': '提交高风险审核',
    'agent.profile.high_risk_approved': '高风险审核通过',
    'agent.profile.high_risk_rejected': '高风险审核驳回',
  }[action] ?? action
}

function selectRun(run: AgentRunEvidenceDto) {
  router.replace({ query: { ...route.query, run: run.id } })
}

onMounted(loadAll)
</script>

<template>
  <div class="page-shell agent-center" data-testid="agent-identity-center">
    <MainColumnPageHeader
      intro-key="agents"
      title="数字员工身份"
      scope-label="当前部门 · 数字员工治理范围"
      kicker="独立身份 · 工具与人工边界"
    >
      <template #actions>
        <div class="hero-actions">
          <el-button
            v-if="canCreate"
            type="primary"
            data-testid="create-agent-identity"
            @click="openCreate"
          >
            新建数字员工
          </el-button>
          <el-button @click="loadAll">刷新</el-button>
        </div>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      title="操作或读取未完成"
      :description="errorMessage"
    />

    <section class="summary-grid" aria-label="数字员工概览">
      <article>
        <small>当前有效</small>
        <strong>{{ effectiveCount }}</strong>
        <span>可进入新流程、新运行和受控令牌签发</span>
      </article>
      <article>
        <small>待完善草稿</small>
        <strong>{{ draftCount }}</strong>
        <span>校验并发布后才会生效</span>
      </article>
      <article>
        <small>已停用</small>
        <strong>{{ disabledCount }}</strong>
        <span>历史证据保留，新动作立即阻断</span>
      </article>
      <article>
        <small>待复核高风险变更</small>
        <strong>{{ pendingReviewRequests.length }}</strong>
        <span>必须由第二位平台管理员审核</span>
      </article>
    </section>

    <section class="filter-bar section-card">
      <el-input
        v-model="filters.search"
        clearable
        placeholder="搜索名称或稳定编码"
        class="search-input"
        @keyup.enter="applyFilters"
        @clear="applyFilters"
      />
      <el-select
        v-model="filters.departmentCode"
        clearable
        filterable
        placeholder="全部授权部门"
        @change="applyFilters"
      >
        <el-option
          v-for="department in options?.departments ?? []"
          :key="department.id"
          :label="department.name"
          :value="department.code"
        />
      </el-select>
      <el-select
        v-model="filters.lifecycleStatus"
        clearable
        placeholder="全部生命周期"
        @change="applyFilters"
      >
        <el-option
          v-for="status in lifecycleOptions"
          :key="status.value"
          :label="status.label"
          :value="status.value"
        />
      </el-select>
      <el-button type="primary" plain :loading="listLoading" @click="applyFilters">
        查询
      </el-button>
    </section>

    <section v-if="canReviewHighRisk && pendingReviewRequests.length" class="review-panel section-card">
      <div class="section-head">
        <div>
          <p class="page-kicker">HIGH-RISK REVIEW</p>
          <h2>高风险能力变更复核</h2>
          <p>提升自主等级、风险、金额、工具/数据范围或更换服务身份，必须由第二位平台管理员审核。</p>
        </div>
      </div>
      <div class="review-list">
        <article v-for="request in pendingReviewRequests" :key="request.id" class="review-row">
          <div>
            <strong>{{ request.agentProfileName }} · v{{ request.versionNumber }}</strong>
            <p>{{ request.riskReason }}</p>
            <small>
              {{ request.requestedByDisplayName }} 于 {{ formatTime(request.requestedAt) }} 提交
            </small>
          </div>
          <div class="row-actions">
            <StatusBadge label="等待双人审核" tone="warning" />
            <el-button
              type="success"
              plain
              data-testid="approve-agent-change"
              :disabled="request.requestedBy === auth.session.userId"
              :loading="actionLoading === `review-${request.id}`"
              @click="reviewChange(request, true)"
            >
              审核通过
            </el-button>
            <el-button
              type="danger"
              plain
              data-testid="reject-agent-change"
              :disabled="request.requestedBy === auth.session.userId"
              @click="reviewChange(request, false)"
            >
              驳回
            </el-button>
          </div>
          <small v-if="request.requestedBy === auth.session.userId" class="self-review-note">
            申请人不能审核自己的变更，请由第二位平台管理员处理。
          </small>
        </article>
      </div>
    </section>

    <section v-if="loading" class="section-card loading-card">
      <el-skeleton :rows="8" animated />
    </section>

    <section v-else class="identity-layout">
      <aside class="identity-list section-card">
        <div class="section-head">
          <div>
            <p class="page-kicker">IDENTITIES</p>
            <h2>身份列表</h2>
          </div>
          <span class="result-count">{{ profiles.length }} 个</span>
        </div>
        <el-empty
          v-if="profiles.length === 0"
          description="当前筛选下没有可查看的数字员工。"
        >
          <el-button v-if="canCreate" type="primary" @click="openCreate">
            新建第一个数字员工
          </el-button>
          <span v-else>请联系平台管理员或部门负责人配置身份。</span>
        </el-empty>
        <button
          v-for="profile in profiles"
          :key="profile.id"
          type="button"
          class="identity-card agent-card"
          :class="{ selected: profile.id === selectedProfileId }"
          data-testid="agent-identity-card"
          @click="selectProfileById(profile.id)"
        >
          <div class="identity-card-head">
            <span>
              <small>{{ profile.code }}</small>
              <strong>{{ profile.name }}</strong>
            </span>
            <StatusBadge
              :label="lifecyclePresentation[profile.lifecycleStatus].label"
              :tone="lifecyclePresentation[profile.lifecycleStatus].tone"
            />
          </div>
          <p>{{ profile.description || '尚未填写岗位说明。' }}</p>
          <div class="identity-meta">
            <span>{{ profile.departmentName }}</span>
            <span>{{ profile.autonomyLevel }} · {{ autonomyLabels[profile.autonomyLevel].action }}</span>
            <span>{{ riskLabels[profile.riskLimit] }}</span>
            <span>限额 {{ formatAmount(profile.amountLimit) }}</span>
          </div>
          <div class="identity-owner-row">
            <span>业务：{{ profile.businessOwnerDisplayName }}</span>
            <span>技术：{{ profile.technicalOwnerDisplayName }}</span>
          </div>
          <div class="identity-foot">
            <span>当前版本 {{ profile.currentPublishedVersionNumber ?? '—' }}</span>
            <span>最近运行 {{ formatTime(profile.lastRunAt) }}</span>
          </div>
        </button>
      </aside>

      <main v-loading="detailLoading" class="identity-detail">
        <el-empty
          v-if="!selectedProfile"
          class="section-card"
          description="请选择一个数字员工查看身份、能力和证据链。"
        />
        <template v-else>
          <section class="section-card profile-head" data-testid="agent-identity-detail">
            <div class="profile-title">
              <div>
                <p class="page-kicker">{{ selectedProfile.code }}</p>
                <h2>{{ selectedProfile.name }}</h2>
                <p>{{ selectedProfile.description || '尚未填写岗位说明。' }}</p>
              </div>
              <StatusBadge
                :label="lifecyclePresentation[selectedProfile.lifecycleStatus].label"
                :tone="lifecyclePresentation[selectedProfile.lifecycleStatus].tone"
              />
            </div>
            <el-alert
              class="lifecycle-hint"
              :type="selectedProfile.lifecycleStatus === 'Published'
                ? 'success'
                : selectedProfile.lifecycleStatus === 'Disabled' ? 'error' : 'warning'"
              :closable="false"
              show-icon
              :title="lifecyclePresentation[selectedProfile.lifecycleStatus].hint"
            />
            <div class="profile-actions">
              <el-button
                v-if="selectedCanManage && selectedProfile.lifecycleStatus !== 'Disabled'"
                type="primary"
                data-testid="edit-agent-identity"
                :loading="actionLoading === 'open-edit'"
                @click="openEdit"
              >
                {{ latestDraft ? '编辑草稿' : '创建新草稿' }}
              </el-button>
              <el-button v-if="canCreate" @click="openCopy">复制身份</el-button>
              <el-button
                v-if="canStartRun && selectedProfile.lifecycleStatus === 'Published'"
                type="success"
                data-testid="start-agent-run-button"
                :loading="actionLoading === 'start-run'"
                @click="startRun"
              >
                发起受控运行
              </el-button>
              <el-button
                v-if="selectedCanManage && selectedProfile.lifecycleStatus !== 'Disabled'"
                type="warning"
                :loading="actionLoading === 'disable'"
                @click="disableProfile"
              >
                停用并撤销令牌
              </el-button>
              <el-button
                v-if="selectedCanManage && selectedProfile.lifecycleStatus === 'Disabled'"
                type="success"
                :loading="actionLoading === 'resume'"
                @click="resumeProfile"
              >
                恢复身份
              </el-button>
            </div>
            <p
              v-if="selectedProfile.lifecycleStatus !== 'Published'"
              class="blocked-run-reason"
            >
              当前不可发起新运行。{{ lifecyclePresentation[selectedProfile.lifecycleStatus].hint }}
            </p>
          </section>

          <section class="fact-grid">
            <article class="section-card">
              <small>所属部门</small>
              <strong>{{ selectedProfile.departmentName }}</strong>
              <span>{{ selectedProfile.businessOwnerDisplayName }} 业务负责 · {{ selectedProfile.technicalOwnerDisplayName }} 技术负责</span>
            </article>
            <article class="section-card">
              <small>自主与风险</small>
              <strong>{{ selectedProfile.autonomyLevel }} · {{ autonomyLabels[selectedProfile.autonomyLevel].action }}</strong>
              <span>{{ riskLabels[selectedProfile.riskLimit] }} · {{ formatAmount(selectedProfile.amountLimit) }}</span>
            </article>
            <article class="section-card">
              <small>发布版本</small>
              <strong>v{{ selectedProfile.currentPublishedVersionNumber ?? '—' }}</strong>
              <span>最近运行 {{ formatTime(selectedProfile.lastRunAt) }} · 共 {{ selectedProfile.recentRunCount ?? 0 }} 次</span>
            </article>
            <article class="section-card">
              <small>独立服务身份</small>
              <strong>{{ selectedProfile.serviceUserName }}</strong>
              <span>不得登录治理页面，不得冒充真人</span>
            </article>
          </section>

          <section class="section-card governance-section">
            <h3>职责与能力边界</h3>
            <div class="detail-columns">
              <div>
                <h4>授权工具</h4>
                <div class="tag-list">
                  <el-tag
                    v-for="tool in selectedProfile.allowedTools"
                    :key="tool"
                    effect="plain"
                  >
                    {{ formatToolName(tool) }}
                  </el-tag>
                </div>
                <h4>数据范围</h4>
                <div class="tag-list">
                  <el-tag
                    v-for="scope in selectedProfile.dataScopes"
                    :key="scope"
                    type="info"
                    effect="plain"
                  >
                    {{ formatDataScope(scope, departmentNames) }}
                  </el-tag>
                </div>
                <h4>模型范围</h4>
                <div class="tag-list">
                  <el-tag
                    v-for="model in selectedProfile.modelScopes"
                    :key="model"
                    type="success"
                    effect="plain"
                  >
                    {{ model === 'aidt-rule-sim' ? 'RuleSimulator 规则模拟' : model }}
                  </el-tag>
                </div>
              </div>
              <div>
                <h4>永久人工边界</h4>
                <div class="tag-list">
                  <el-tag
                    v-for="boundary in options?.permanentHumanBoundaries ?? []"
                    :key="boundary"
                    type="danger"
                    effect="plain"
                  >
                    {{ confirmationLabels[boundary] ?? boundary }}
                  </el-tag>
                </div>
                <h4>熔断条件</h4>
                <div class="tag-list">
                  <el-tag
                    v-for="condition in selectedProfile.stopConditions"
                    :key="condition"
                    type="warning"
                    effect="plain"
                  >
                    {{ stopConditionLabels[condition] ?? condition }}
                  </el-tag>
                </div>
              </div>
            </div>
          </section>

          <section class="section-card">
            <el-tabs v-model="activeDetailTab" class="evidence-tabs">
              <el-tab-pane label="版本历史" name="versions">
                <el-table
                  :data="versions"
                  empty-text="暂无版本记录，或当前身份无权查看版本。"
                  :row-class-name="versionRowClass"
                >
                  <el-table-column label="版本" width="80">
                    <template #default="{ row }">v{{ row.versionNumber }}</template>
                  </el-table-column>
                  <el-table-column label="状态" width="120">
                    <template #default="{ row }">
                      <StatusBadge
                        :label="versionStatus(row).label"
                        :tone="versionStatus(row).tone"
                      />
                    </template>
                  </el-table-column>
                  <el-table-column prop="name" label="名称" min-width="180" />
                  <el-table-column label="风险" width="150">
                    <template #default="{ row }">
                      {{ row.isHighRisk ? '高风险变更' : '常规变更' }}
                    </template>
                  </el-table-column>
                  <el-table-column label="发布时间" width="190">
                    <template #default="{ row }">{{ formatTime(row.publishedAt) }}</template>
                  </el-table-column>
                  <el-table-column label="操作" width="150">
                    <template #default="{ row }">
                      <el-button
                        v-if="selectedCanManage && row.publishedAt"
                        text
                        type="primary"
                        :data-testid="`rollback-agent-version-${row.versionNumber}`"
                        :disabled="row.id === selectedProfile.currentPublishedVersionId"
                        :loading="actionLoading === `rollback-${row.id}`"
                        @click="rollbackVersion(row)"
                      >
                        回滚到此版本
                      </el-button>
                    </template>
                  </el-table-column>
                </el-table>
              </el-tab-pane>

              <el-tab-pane label="运行记录" name="runs">
                <div v-if="selectedRun" class="focused-run-card">
                  <StatusBadge
                    :label="runStatus(selectedRun).label"
                    :tone="runStatus(selectedRun).tone"
                  />
                  <strong>已从深链定位运行 {{ selectedRun.id.slice(-8) }}</strong>
                  <span>{{ runStatus(selectedRun).hint }}</span>
                </div>
                <el-table
                  :data="context?.recentRuns ?? []"
                  empty-text="该数字员工还没有运行记录。"
                  :row-class-name="runRowClass"
                  @row-click="selectRun"
                >
                  <el-table-column label="发起人" width="150">
                    <template #default="{ row }">
                      {{ row.requestedByDisplayName || '真人用户' }}
                    </template>
                  </el-table-column>
                  <el-table-column label="动作" width="150">
                    <template #default="{ row }">
                      {{ actionLabels[row.action] ?? row.action }}
                    </template>
                  </el-table-column>
                  <el-table-column label="状态" width="150">
                    <template #default="{ row }">
                      <StatusBadge
                        :label="runStatus(row).label"
                        :tone="runStatus(row).tone"
                      />
                    </template>
                  </el-table-column>
                  <el-table-column label="风险" width="110">
                    <template #default="{ row }">{{ runRisk(row) }}</template>
                  </el-table-column>
                  <el-table-column label="业务与流程" min-width="250">
                    <template #default="{ row }">
                      <div class="run-context">
                        <span>{{ row.businessObjectTitle || row.minutesSourceTitle || '独立运行' }}</span>
                        <small v-if="row.workflowName">
                          {{ row.workflowName }} · {{ row.workflowTaskNode || '流程节点' }}
                        </small>
                      </div>
                    </template>
                  </el-table-column>
                  <el-table-column label="时间" width="180">
                    <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
                  </el-table-column>
                  <el-table-column label="操作" width="180">
                    <template #default="{ row }">
                      <el-button
                        v-if="row.status === 'AwaitingConfirmation' && canConfirmRun"
                        type="success"
                        text
                        data-testid="confirm-agent-run-button"
                        :loading="actionLoading === `confirm-${row.id}`"
                        @click.stop="confirmRun(row)"
                      >
                        真人确认
                      </el-button>
                      <el-button
                        v-if="row.status === 'Running' && canExecuteRun"
                        type="primary"
                        text
                        :loading="actionLoading === `execute-${row.id}`"
                        @click.stop="executeRun(row)"
                      >
                        受控执行
                      </el-button>
                      <el-button
                        v-if="['Running', 'AwaitingConfirmation'].includes(row.status) && canStopRun"
                        type="danger"
                        text
                        data-testid="stop-agent-run-button"
                        :loading="actionLoading === `stop-${row.id}`"
                        @click.stop="stopRun(row)"
                      >
                        停止
                      </el-button>
                    </template>
                  </el-table-column>
                </el-table>
              </el-tab-pane>

              <el-tab-pane label="流程节点" name="nodes">
                <el-table
                  :data="context?.workflowNodes ?? []"
                  empty-text="该数字员工尚未被任何流程节点引用。"
                >
                  <el-table-column prop="workflowDefinitionName" label="流程" min-width="200" />
                  <el-table-column label="版本/节点" width="190">
                    <template #default="{ row }">
                      v{{ row.workflowVersionNumber }} · {{ row.nodeCode }}
                    </template>
                  </el-table-column>
                  <el-table-column prop="status" label="状态" width="110" />
                  <el-table-column label="能力" min-width="230">
                    <template #default="{ row }">
                      {{ row.allowAgentAssist ? '辅助' : '不辅助' }}
                      · {{ row.allowAgentProxy ? '可代代理' : '不可代理' }}
                      <el-tag v-if="row.permanentHumanBoundary" type="danger" size="small">
                        永久人工边界
                      </el-tag>
                    </template>
                  </el-table-column>
                  <el-table-column label="发布时间" width="180">
                    <template #default="{ row }">{{ formatTime(row.publishedAt) }}</template>
                  </el-table-column>
                </el-table>
                <div class="linked-list-title">最近流程任务</div>
                <el-table
                  :data="context?.workflowTasks ?? []"
                  empty-text="暂无由该身份关联的流程任务。"
                  @row-click="openWorkflowInstance"
                >
                  <el-table-column prop="workflowDefinitionName" label="流程" min-width="200" />
                  <el-table-column prop="nodeCode" label="节点" width="160" />
                  <el-table-column prop="status" label="任务状态" width="120" />
                  <el-table-column prop="decisionMode" label="处理方式" width="130" />
                  <el-table-column label="创建时间" width="180">
                    <template #default="{ row }">{{ formatTime(row.createdAt) }}</template>
                  </el-table-column>
                </el-table>
              </el-tab-pane>

              <el-tab-pane label="MCP 授权" name="mcp">
                <template v-if="context?.canViewMcpEvidence">
                  <h4>关联客户端</h4>
                  <el-table
                    :data="context?.mcpClients ?? []"
                    empty-text="暂无关联 MCP 客户端。"
                  >
                    <el-table-column prop="clientName" label="客户端" min-width="200" />
                    <el-table-column label="状态" width="110">
                      <template #default="{ row }">
                        <StatusBadge
                          :label="row.isActive ? '可用' : '已停用'"
                          :tone="row.isActive ? 'success' : 'danger'"
                        />
                      </template>
                    </el-table-column>
                    <el-table-column prop="activeTokenCount" label="有效令牌" width="110" />
                    <el-table-column label="最近使用" width="180">
                      <template #default="{ row }">{{ formatTime(row.lastUsedAt) }}</template>
                    </el-table-column>
                  </el-table>
                  <h4>服务令牌</h4>
                  <el-table
                    :data="context?.mcpTokens ?? []"
                    empty-text="暂无服务令牌；停用身份后不会继续签发。"
                  >
                    <el-table-column label="令牌" width="170">
                      <template #default="{ row }">
                        {{ row.kind === 'Access' ? '访问令牌' : '刷新令牌' }}
                        · {{ row.id.slice(-6) }}
                      </template>
                    </el-table-column>
                    <el-table-column prop="clientName" label="客户端" min-width="170" />
                    <el-table-column label="状态" width="110">
                      <template #default="{ row }">
                        <StatusBadge
                          :label="tokenStatus(row).label"
                          :tone="tokenStatus(row).tone"
                        />
                      </template>
                    </el-table-column>
                    <el-table-column label="范围" min-width="170">
                      <template #default="{ row }">{{ row.scopes.join(' · ') }}</template>
                    </el-table-column>
                    <el-table-column label="到期/撤销" min-width="190">
                      <template #default="{ row }">
                        {{ formatTime(row.revokedAt ?? row.expiresAt) }}
                        <small v-if="row.revocationReason">{{ row.revocationReason }}</small>
                      </template>
                    </el-table-column>
                  </el-table>
                </template>
                <el-empty
                  v-else
                  description="您没有查看 MCP 授权证据的权限；仍可确认该身份已发布且有效。"
                />
              </el-tab-pane>

              <el-tab-pane label="审计时间线" name="audit">
                <el-timeline v-if="context?.canViewAuditTimeline">
                  <el-timeline-item
                    v-for="event in context?.auditTimeline ?? []"
                    :key="event.id"
                    :timestamp="formatTime(event.occurredAt)"
                  >
                    <strong>{{ auditActionLabel(event.action) }}</strong>
                    <p>{{ event.reason || `操作结果：${event.outcome === 'Succeeded' ? '成功' : event.outcome === 'Denied' ? '已拒绝' : '失败'}` }}</p>
                  </el-timeline-item>
                </el-timeline>
                <el-empty
                  v-else
                  description="您没有查看该身份审计时间线的权限。"
                />
              </el-tab-pane>
            </el-tabs>
          </section>
        </template>
      </main>
    </section>

    <FeaturePlaceholder
      title="外部连接保持未启用"
      description="本批不连接真实钉钉、U9、ERP、DCS，不调用真实外部大模型。页面只呈现现有受控工具、RuleSimulator 和部署测试能力。"
      status="Mock / RuleSimulator"
      tone="warning"
    />

    <section v-if="connectors.length" class="section-card connector-strip">
      <div v-for="connector in connectors" :key="connector.id">
        <strong>{{ connector.name }}</strong>
        <StatusBadge
          :label="connector.connectionStatus === 'Mock'
            ? '仅模拟'
            : connector.connectionStatus === 'Disconnected' ? '未连接' : connector.connectionStatus"
          :tone="connector.connectionStatus === 'Mock' ? 'warning' : 'neutral'"
        />
      </div>
    </section>

    <AgentIdentityEditor
      v-if="editorVisible"
      v-model:visible="editorVisible"
      :mode="editorMode"
      :initial="editorInitial"
      :options="options"
      :validation="editorValidation"
      :saving="editorSaving"
      :validating="editorValidating"
      :publishing="editorPublishing"
      :can-publish="selectedCanManage && auth.can('agent:approve')"
      @submit="saveEditor"
      @validate="validateEditor"
      @publish="publishEditor"
    />
  </div>
</template>

<style scoped>
.agent-center {
  display: grid;
  gap: 18px;
}

.identity-hero {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 22px;
}

.hero-actions,
.profile-actions,
.row-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.summary-grid,
.fact-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
}

.summary-grid article,
.fact-grid article {
  display: grid;
  gap: 5px;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  padding: 16px;
  background: var(--aidt-surface);
}

.summary-grid small,
.fact-grid small {
  color: var(--aidt-ink-muted);
  font-size: 0.74rem;
}

.summary-grid strong {
  color: var(--aidt-brand);
  font-size: 2rem;
  line-height: 1;
}

.summary-grid span,
.fact-grid span {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
  line-height: 1.5;
}

.filter-bar {
  display: grid;
  grid-template-columns: minmax(240px, 1.6fr) minmax(180px, 1fr) minmax(170px, 0.8fr) auto;
  gap: 10px;
  align-items: center;
}

.identity-layout {
  display: grid;
  grid-template-columns: minmax(300px, 0.8fr) minmax(0, 2.2fr);
  gap: 16px;
  align-items: start;
}

.identity-list {
  display: grid;
  gap: 10px;
  max-height: calc(100vh - 210px);
  overflow: auto;
}

.section-head,
.identity-card-head,
.profile-title {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.section-head h2,
.profile-title h2 {
  margin: 0;
}

.section-head p,
.profile-title p {
  margin: 5px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.result-count {
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
}

.identity-card {
  display: grid;
  gap: 9px;
  width: 100%;
  border: 1px solid var(--aidt-border);
  border-radius: 12px;
  padding: 13px;
  background: white;
  color: inherit;
  text-align: left;
  cursor: pointer;
  transition: border-color 0.16s ease, box-shadow 0.16s ease;
}

.identity-card:hover,
.identity-card.selected {
  border-color: var(--aidt-brand);
  box-shadow: 0 6px 18px rgb(28 89 72 / 10%);
}

.identity-card-head strong,
.identity-card-head small {
  display: block;
}

.identity-card-head strong {
  margin-top: 3px;
  font-size: 0.94rem;
}

.identity-card-head small {
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
}

.identity-card > p {
  margin: 0;
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
  line-height: 1.55;
}

.identity-meta,
.identity-owner-row,
.identity-foot,
.tag-list {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.identity-meta span {
  border-radius: 999px;
  padding: 3px 7px;
  background: var(--aidt-surface-muted);
  font-size: 0.66rem;
}

.identity-owner-row,
.identity-foot {
  justify-content: space-between;
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
}

.identity-detail {
  display: grid;
  gap: 14px;
  min-width: 0;
}

.profile-head {
  display: grid;
  gap: 12px;
}

.lifecycle-hint {
  margin-top: 2px;
}

.blocked-run-reason,
.self-review-note {
  margin: 0;
  color: var(--aidt-danger);
  font-size: 0.76rem;
}

.fact-grid strong {
  font-size: 0.92rem;
}

.governance-section h3 {
  margin-top: 0;
}

.detail-columns {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 24px;
}

.detail-columns h4,
.linked-list-title {
  margin: 16px 0 8px;
  font-size: 0.78rem;
}

.detail-columns h4:first-child,
.linked-list-title:first-child {
  margin-top: 0;
}

.evidence-tabs :deep(.el-table),
.evidence-tabs :deep(.el-timeline) {
  margin-top: 8px;
}

.run-context {
  display: grid;
  gap: 3px;
}

.run-context small,
.evidence-tabs small {
  color: var(--aidt-ink-muted);
}

.focused-run-card,
.review-row {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 14px;
  border: 1px solid var(--aidt-border);
  border-radius: 10px;
  padding: 12px;
  margin-bottom: 10px;
}

.focused-run-card span,
.review-row p {
  margin: 3px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
}

.focused-run-card {
  justify-content: flex-start;
  border-color: #7ab89b;
  background: #f2fbf6;
}

.review-list {
  display: grid;
  gap: 10px;
  margin-top: 12px;
}

.review-row {
  margin: 0;
}

.review-row > div:first-child {
  min-width: 0;
}

.review-row small {
  color: var(--aidt-ink-muted);
}

.connector-strip {
  display: flex;
  flex-wrap: wrap;
  gap: 10px 18px;
}

.connector-strip > div {
  display: flex;
  align-items: center;
  gap: 7px;
  font-size: 0.75rem;
}

:deep(.focused-run-row > td) {
  background: #edf8f1 !important;
}

@media (max-width: 1100px) {
  .summary-grid,
  .fact-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .filter-bar {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .identity-layout {
    grid-template-columns: minmax(270px, 0.9fr) minmax(0, 1.8fr);
  }
}

@media (max-width: 760px) {
  .identity-hero,
  .profile-title,
  .review-row {
    display: grid;
  }

  .hero-actions,
  .profile-actions {
    width: 100%;
  }

  .hero-actions :deep(.el-button),
  .profile-actions :deep(.el-button) {
    flex: 1 1 auto;
  }

  .summary-grid,
  .fact-grid,
  .filter-bar,
  .identity-layout,
  .detail-columns {
    grid-template-columns: 1fr;
  }

  .identity-list {
    max-height: none;
  }

  .evidence-tabs :deep(.el-tabs__nav-wrap) {
    overflow-x: auto;
  }

  .evidence-tabs :deep(.el-tabs__nav) {
    flex-wrap: nowrap;
  }

  .review-row .row-actions {
    justify-content: flex-start;
  }
}
</style>
