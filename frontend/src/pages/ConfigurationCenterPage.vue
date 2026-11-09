<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ApiError } from '@/api/http'
import { configurationApi } from '@/api/modules/configurationApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  AgentProfileDto,
  CreateWorkflowConfigurationRequest,
  DepartmentDto,
  ObjectTypeDto,
  RoleDto,
  WorkflowConfigurationDto,
  WorkflowFormField,
  WorkflowTaskForm,
  WorkflowPolicyUserDto,
} from '@/api/types/contracts'
import StatusBadge from '@/components/base/StatusBadge.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import {
  autonomyLabels,
  formatToolName,
  riskLabels,
} from '@/features/agents/agentPresentation'
import { useAuthStore } from '@/stores/auth'
import WorkflowFormRenderer from '@/features/workflow/components/WorkflowFormRenderer.vue'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const loading = ref(true)
const actionLoading = ref('')
const errorMessage = ref('')
const configurations = ref<WorkflowConfigurationDto[]>([])
const selectedId = ref('')
const selected = ref<WorkflowConfigurationDto | null>(null)
const selectedNodeIndex = ref(0)
const users = ref<WorkflowPolicyUserDto[]>([])
const workflowRoles = ref<RoleDto[]>([])
const agentProfiles = ref<AgentProfileDto[]>([])
const departments = ref<DepartmentDto[]>([])
const publishedObjectTypes = ref<ObjectTypeDto[]>([])
const filterState = ref('Published')
const createVisible = ref(false)
const versionHistoryVisible = ref(false)
const versionHistory = ref<WorkflowConfigurationDto[]>([])
const simulationVisible = ref(false)
const simulationPath = ref<string[]>([])
const simulationErrors = ref<string[]>([])
const selectedFieldIndex = ref(0)
const draggingFieldIndex = ref<number | null>(null)
const previewData = ref<Record<string, string | number | boolean | string[] | null>>({})

const draft = reactive<CreateWorkflowConfigurationRequest>({
  configurationKey: '',
  name: '',
  description: '',
  departmentId: '',
  objectTypeCode: 'ANY',
  startNode: 'INITIATE',
  nodes: [],
  isDefault: false,
  applicableRoleCodes: [],
  riskLevel: null,
})

const canCreate = computed(() => auth.can('configuration:create'))
const canUpdate = computed(() => auth.can('configuration:update'))
const canPublish = computed(() => auth.can('configuration:approve'))
const canRollback = computed(() => auth.can('configuration:manage'))
const currentNode = computed(() => selected.value?.nodes[selectedNodeIndex.value] ?? null)
const currentField = computed(
  () => currentNode.value?.fields?.[selectedFieldIndex.value] ?? null,
)
const currentAiReview = computed(() => {
  if (!currentNode.value) {
    return {
      enabled: false,
      registeredAction: null,
      agentProfileId: null,
      readableFieldKeys: [],
      materialScopes: [],
      outputMode: 'SuggestionOnly' as const,
    }
  }
  return ensureNodeShape(currentNode.value).aiReview!
})
const currentNotification = computed(() => {
  if (!currentNode.value) {
    return {
      enabled: true,
      beforeDueMinutes: null,
      escalateAfterMinutes: null,
      escalateToRoleCode: null,
    }
  }
  return ensureNodeShape(currentNode.value).notification!
})
const selectedObjectType = computed(() =>
  publishedObjectTypes.value.find((item) => item.code === selected.value?.objectTypeCode),
)
const boundObjectFields = computed(() => selectedObjectType.value?.fields ?? [])
const previewForm = computed<WorkflowTaskForm | null>(() => {
  if (!selected.value || !currentNode.value) return null
  return {
    taskId: 'preview',
    workflowInstanceId: 'preview',
    businessObjectId: 'preview',
    departmentId: selected.value.departmentId,
    nodeCode: currentNode.value.code,
    nodeName: currentNode.value.name,
    businessDescription: currentNode.value.businessDescription ?? null,
    dueAt: null,
    formSchemaVersion: currentNode.value.formSchemaVersion ?? '1.0',
    formDefinitionHash: 'preview',
    fields: currentNode.value.fields ?? [],
    actions: currentNode.value.actions ?? [],
    aiReview: currentNode.value.aiReview ?? {
      enabled: false,
      registeredAction: null,
      agentProfileId: null,
      readableFieldKeys: [],
      materialScopes: [],
      outputMode: 'SuggestionOnly',
    },
    notificationPolicyJson: JSON.stringify(currentNode.value.notification ?? {}),
    permanentHumanBoundary: Boolean(currentNode.value.permanentHumanBoundary),
    formDataJson: JSON.stringify(previewData.value),
  }
})
const fieldTypes = [
  ['Text', '单行文本'],
  ['LongText', '多行文本'],
  ['Number', '数字'],
  ['Money', '金额'],
  ['Percent', '百分比'],
  ['Select', '单选'],
  ['MultiSelect', '下拉多选'],
  ['User', '单人'],
  ['MultiUser', '多人'],
  ['Department', '部门'],
  ['Date', '日期'],
  ['DateTime', '日期时间'],
  ['DateRange', '日期范围'],
  ['Attachment', '附件'],
  ['Boolean', '是否'],
  ['ReadOnlySystem', '只读系统字段'],
  ['BusinessObjectField', '绑定业务对象字段'],
] as const
const registeredAiActions = [
  ['CompletenessCheck', '完整性检查'],
  ['RiskIdentify', '风险识别'],
  ['GenerateReviewOpinion', '生成审查意见'],
  ['GenerateDraft', '生成草稿'],
] as const
const isEditable = computed(() =>
  Boolean(
    selected.value
      && canUpdate.value
      && ['Draft', 'Validated'].includes(selected.value.state),
  ),
)
const activeCount = computed(
  () => configurations.value.filter((item) => item.state === 'Published').length,
)
const draftCount = computed(
  () => configurations.value.filter((item) => ['Draft', 'Validated'].includes(item.state)).length,
)

function errorText(error: unknown, fallback: string) {
  return error instanceof ApiError ? `${error.message}（${error.code}）` : fallback
}

function stateTone(state: WorkflowConfigurationDto['state']) {
  if (state === 'Published') return 'success'
  if (state === 'Retired') return 'neutral'
  if (state === 'Validated') return 'info'
  return 'warning'
}

function stateLabel(state: WorkflowConfigurationDto['state']) {
  return {
    Draft: '草稿',
    Validated: '已校验',
    Published: '已发布',
    Retired: '已退役',
  }[state]
}

async function loadConfigurationList(): Promise<WorkflowConfigurationDto[]> {
  const requestedDepartmentId = String(route.query.departmentId ?? '')
  return (
    await configurationApi.list({
      state: filterState.value || undefined,
    })
  ).items.filter(
    (item) => !requestedDepartmentId || item.departmentId === requestedDepartmentId,
  )
}

function ensureNodeShape(
  node: WorkflowConfigurationDto['nodes'][number],
): WorkflowConfigurationDto['nodes'][number] {
  node.businessDescription ??= null
  node.slaMinutes ??= null
  node.deliverables ??= []
  node.relatedRoleCodes ??= []
  node.requiredAttachmentCount ??= 0
  node.completionCondition ??= null
  node.fields ??= []
  node.actions ??= []
  node.allowAgentAssist ??= false
  node.allowAgentProxy ??= false
  node.permanentHumanBoundary ??= false
  node.aiReview ??= {
    enabled: false,
    registeredAction: null,
    agentProfileId: null,
    readableFieldKeys: [],
    materialScopes: [],
    outputMode: 'SuggestionOnly',
  }
  node.notification ??= {
    enabled: true,
    beforeDueMinutes: null,
    escalateAfterMinutes: null,
    escalateToRoleCode: null,
  }
  node.formSchemaVersion ??= '1.0'
  return node
}

function addField(dataType: WorkflowFormField['dataType'] = 'Text') {
  if (!currentNode.value || !isEditable.value) return
  const nextIndex = (currentNode.value.fields?.length ?? 0) + 1
  const field: WorkflowFormField = {
    key: `FIELD_${nextIndex}`,
    displayName: fieldTypes.find((item) => item[0] === dataType)?.[1] ?? '新字段',
    dataType,
    required: false,
    defaultValue: null,
    readOnly: false,
    visibleRoleCodes: [],
    writeRoleCodes: [],
    options: [],
    width: 'Full',
    sortOrder: nextIndex * 10,
    dataSource: dataType === 'BusinessObjectField'
      ? 'BusinessObjectReference'
      : 'TaskInput',
    boundObjectFieldCode: null,
    sensitivity: 'Internal',
    helpText: null,
  }
  currentNode.value.fields ??= []
  currentNode.value.fields.push(field)
  selectedFieldIndex.value = currentNode.value.fields.length - 1
}

function removeField(index: number) {
  if (!currentNode.value?.fields) return
  currentNode.value.fields.splice(index, 1)
  selectedFieldIndex.value = Math.max(0, Math.min(selectedFieldIndex.value, currentNode.value.fields.length - 1))
}

function duplicateField(index: number) {
  if (!currentNode.value?.fields?.[index]) return
  const source = currentNode.value.fields[index]
  const copy = {
    ...source,
    key: `${source.key}_COPY`,
    displayName: `${source.displayName} 副本`,
    options: [...(source.options ?? [])],
    visibleRoleCodes: [...(source.visibleRoleCodes ?? [])],
    writeRoleCodes: [...(source.writeRoleCodes ?? [])],
  }
  currentNode.value.fields.splice(index + 1, 0, copy)
  selectedFieldIndex.value = index + 1
}

function moveField(from: number, to: number) {
  if (!currentNode.value?.fields || from === to) return
  const fields = currentNode.value.fields
  const [moved] = fields.splice(from, 1)
  fields.splice(to, 0, moved)
  fields.forEach((field, index) => {
    field.sortOrder = (index + 1) * 10
  })
  selectedFieldIndex.value = to
}

function dropField(index: number) {
  if (draggingFieldIndex.value !== null) {
    moveField(draggingFieldIndex.value, index)
  }
  draggingFieldIndex.value = null
}

function addNodeAction() {
  if (!currentNode.value || !isEditable.value) return
  currentNode.value.actions ??= []
  currentNode.value.actions.push({
    code: `ACTION_${currentNode.value.actions.length + 1}`,
    name: '提交处理',
    actionType: 'Submit',
    confirmationText: null,
  })
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [page, userRows, departmentRows, roleRows, agentRows, objectTypeRows] = await Promise.all([
      configurationApi.list({ state: filterState.value || undefined }),
      configurationApi.memberCandidates(),
      workspaceApi.departments(),
      configurationApi.workflowRoles(),
      workspaceApi.agents(),
      workspaceApi.publishedObjectTypes(),
    ])
    const requestedDepartmentId = String(route.query.departmentId ?? '')
    configurations.value = page.items.filter(
      (item) => !requestedDepartmentId || item.departmentId === requestedDepartmentId,
    )
    users.value = userRows
    departments.value = departmentRows
    workflowRoles.value = roleRows
    agentProfiles.value = agentRows.filter((item) => item.isActive)
    publishedObjectTypes.value = objectTypeRows
    if (!selectedId.value || !configurations.value.some((item) => item.id === selectedId.value)) {
      selectedId.value = configurations.value[0]?.id ?? ''
    }
    await selectConfiguration(selectedId.value)
  } catch (error) {
    errorMessage.value = errorText(error, '无法加载流程配置。')
  } finally {
    loading.value = false
  }
}

async function selectConfiguration(id: string) {
  selectedId.value = id
  selectedNodeIndex.value = 0
  if (!id) {
    selected.value = null
    return
  }
  try {
    selected.value = await configurationApi.get(id)
    selected.value.nodes.forEach(ensureNodeShape)
    selectedNodeIndex.value = 0
    selectedFieldIndex.value = 0
    previewData.value = {}
  } catch (error) {
    ElMessage.error(errorText(error, '读取配置详情失败。'))
  }
}

function openCreate(seed?: WorkflowConfigurationDto) {
  draft.configurationKey = seed
    ? `${seed.configurationKey}_COPY_${String(Date.now()).slice(-4)}`
    : `WF-${new Date().toISOString().slice(0, 10).replaceAll('-', '')}-${String(Date.now()).slice(-4)}`
  draft.name = seed ? `${seed.name} 副本` : ''
  draft.description = seed?.description ?? ''
  draft.departmentId = seed?.departmentId ?? departments.value.find((item) => item.isPrimary)?.id ?? departments.value[0]?.id ?? ''
  if (!seed && route.query.departmentId) {
    draft.departmentId = String(route.query.departmentId)
  }
  draft.objectTypeCode = seed?.objectTypeCode ?? 'ANY'
  draft.startNode = seed?.startNode ?? 'INITIATE'
  draft.isDefault = false
  draft.applicableRoleCodes = [...(seed?.applicableRoleCodes ?? [])]
  draft.riskLevel = null
  draft.nodes = seed
    ? seed.nodes.map((node) => ensureNodeShape({
        ...node,
        condition: node.condition ? { ...node.condition } : null,
        fields: (node.fields ?? []).map((field) => ({
          ...field,
          options: [...(field.options ?? [])],
          visibleRoleCodes: [...(field.visibleRoleCodes ?? [])],
          writeRoleCodes: [...(field.writeRoleCodes ?? [])],
        })),
        actions: (node.actions ?? []).map((action) => ({ ...action })),
        aiReview: {
          enabled: node.aiReview?.enabled ?? false,
          registeredAction: node.aiReview?.registeredAction ?? null,
          agentProfileId: node.aiReview?.agentProfileId ?? null,
          readableFieldKeys: [...(node.aiReview?.readableFieldKeys ?? [])],
          materialScopes: [...(node.aiReview?.materialScopes ?? [])],
          outputMode: 'SuggestionOnly',
        },
        notification: {
          enabled: node.notification?.enabled ?? true,
          beforeDueMinutes: node.notification?.beforeDueMinutes ?? null,
          escalateAfterMinutes: node.notification?.escalateAfterMinutes ?? null,
          escalateToRoleCode: node.notification?.escalateToRoleCode ?? null,
        },
      }))
    : [
        ensureNodeShape({
          code: 'INITIATE',
          name: '发起事项',
          assigneeUserName:
            users.value.find((user) => user.id === auth.session.userId)?.userName ??
            users.value[0]?.userName ??
            '',
          nextNode: null,
          completeOnApprove: true,
          condition: null,
          assigneeStrategy: 'SpecificUser',
          businessRoleCode: null,
          agentProfileId: null,
        }),
      ]
  createVisible.value = true
}

async function createDraft() {
  if (!draft.configurationKey.trim() || !draft.name.trim() || !draft.departmentId) {
    ElMessage.warning('请填写配置编号、名称和所属部门。')
    return
  }
  if (draft.nodes.some((node) =>
    !node.code
    || !node.name
    || (node.assigneeStrategy === 'SpecificUser' && !node.assigneeUserName)
    || (node.assigneeStrategy === 'BusinessRole' && !node.businessRoleCode)
    || (node.assigneeStrategy === 'Agent' && !node.agentProfileId),
  )) {
    ElMessage.warning('每个节点必须填写编码、名称和可解析的处理人策略。')
    return
  }
  actionLoading.value = 'create'
  try {
    const created = await configurationApi.create({
      ...draft,
      configurationKey: draft.configurationKey.trim().toUpperCase(),
      name: draft.name.trim(),
      description: draft.description?.trim() || null,
      isDefault: draft.isDefault,
      applicableRoleCodes: draft.applicableRoleCodes,
      riskLevel: draft.riskLevel || null,
      nodes: draft.nodes.map((node) => ({
        ...node,
        nextNode: node.nextNode || null,
        condition: node.condition?.field ? node.condition : null,
      })),
    })
    createVisible.value = false
    ElMessage.success(`配置 ${created.configurationKey} v${created.versionNumber} 已保存为草稿`)
    filterState.value = ''
    configurations.value = await loadConfigurationList()
    await selectConfiguration(created.id)
  } catch (error) {
    ElMessage.error(errorText(error, '创建配置草稿失败。'))
  } finally {
    actionLoading.value = ''
  }
}

function addNode() {
  if (!selected.value || !isEditable.value) return
  const sequence = selected.value.nodes.length + 1
  let code = `STEP_${sequence}`
  let suffix = sequence
  while (selected.value.nodes.some((node) => node.code === code)) {
    suffix += 1
    code = `STEP_${suffix}`
  }
  selected.value.nodes.push({
    code,
    name: `新节点 ${suffix}`,
    assigneeUserName: users.value[0]?.userName ?? auth.session.displayName,
    nextNode: null,
    completeOnApprove: false,
    condition: null,
    assigneeStrategy: 'SpecificUser',
    businessRoleCode: null,
    agentProfileId: null,
  })
    ensureNodeShape(selected.value.nodes[selected.value.nodes.length - 1])
  selectedNodeIndex.value = selected.value.nodes.length - 1
    selectedFieldIndex.value = 0
}

function removeCurrentNode() {
  if (!selected.value || !isEditable.value || selected.value.nodes.length <= 1) return
  const [removed] = selected.value.nodes.splice(selectedNodeIndex.value, 1)
  selected.value.nodes.forEach((node) => {
    if (node.nextNode === removed.code) node.nextNode = removed.nextNode
  })
  if (selected.value.startNode === removed.code) {
    selected.value.startNode = selected.value.nodes[0]?.code ?? ''
  }
  selectedNodeIndex.value = Math.max(0, selectedNodeIndex.value - 1)
}

function updateNodeCode(value: string) {
  if (!selected.value || !currentNode.value) return
  const previous = currentNode.value.code
  const next = value.trim().toUpperCase()
  currentNode.value.code = next
  selected.value.nodes.forEach((node) => {
    if (node.nextNode === previous) node.nextNode = next
  })
  if (selected.value.startNode === previous) selected.value.startNode = next
}

async function saveDraft() {
  if (!selected.value || !isEditable.value) return
  actionLoading.value = 'save'
  try {
    selected.value = await configurationApi.update(selected.value.id, {
      expectedRevision: selected.value.revision,
      name: selected.value.name,
      description: selected.value.description,
      departmentId: selected.value.departmentId,
      objectTypeCode: selected.value.objectTypeCode,
      startNode: selected.value.startNode,
      isDefault: selected.value.isDefault ?? false,
      applicableRoleCodes: selected.value.applicableRoleCodes ?? [],
      riskLevel: selected.value.riskLevel ?? null,
      nodes: selected.value.nodes,
    })
    selected.value.nodes.forEach(ensureNodeShape)
    ElMessage.success('配置草稿已保存，Revision 已更新')
    const index = selected.value ? configurations.value.findIndex((item) => item.id === selected.value?.id) : -1
    if (selected.value && index >= 0) configurations.value[index] = selected.value
  } catch (error) {
    ElMessage.error(errorText(error, '保存配置草稿失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function validateConfiguration() {
  if (!selected.value) return
  actionLoading.value = 'validate'
  try {
    const result = await configurationApi.validate(selected.value.id)
    selected.value = await configurationApi.get(selected.value.id)
    selected.value.nodes.forEach(ensureNodeShape)
    if (result.isValid) {
      ElMessage.success('配置校验通过，可以提交发布。')
    } else {
      ElMessage.warning(`发现 ${result.errors.length} 个配置问题。`)
    }
  } catch (error) {
    ElMessage.error(errorText(error, '配置校验失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function publishConfiguration() {
  if (!selected.value) return
  try {
    await ElMessageBox.confirm(
      `发布 ${selected.value.configurationKey} 后，新流程实例将使用新版本；历史实例继续使用原版本。`,
      '确认发布配置',
      { type: 'warning', confirmButtonText: '发布', cancelButtonText: '取消' },
    )
  } catch {
    return
  }
  actionLoading.value = 'publish'
  try {
    selected.value = await configurationApi.publish(selected.value.id)
    selected.value.nodes.forEach(ensureNodeShape)
    ElMessage.success(`已发布流程版本 ${selected.value.publishedWorkflowVersionId}`)
    configurations.value = await loadConfigurationList()
  } catch (error) {
    ElMessage.error(errorText(error, '发布配置失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function rollbackConfiguration() {
  if (!selected.value) return
  try {
    await ElMessageBox.confirm(
      `回滚后，${selected.value.configurationKey} 将重新启用该历史流程版本。`,
      '确认回滚配置',
      { type: 'warning', confirmButtonText: '回滚', cancelButtonText: '取消' },
    )
  } catch {
    return
  }
  actionLoading.value = 'rollback'
  try {
    selected.value = await configurationApi.rollback(
      selected.value.id,
      '流程负责人从配置中心发起历史版本回滚',
    )
    selected.value.nodes.forEach(ensureNodeShape)
    ElMessage.success('配置已回滚到历史稳定版本')
    configurations.value = await loadConfigurationList()
  } catch (error) {
    ElMessage.error(errorText(error, '回滚配置失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function toggleTemplateStatus() {
  if (!selected.value) return
  const disable = selected.value.state === 'Published'
  actionLoading.value = disable ? 'disable' : 'restore'
  try {
    selected.value = await configurationApi.setStatus(selected.value.id, disable)
    selected.value.nodes.forEach(ensureNodeShape)
    ElMessage.success(disable ? '模板已停用，历史实例仍保留' : '模板已恢复使用')
    configurations.value = await loadConfigurationList()
  } catch (error) {
    ElMessage.error(errorText(error, disable ? '停用模板失败。' : '恢复模板失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function simulateConfiguration() {
  if (!selected.value) return
  actionLoading.value = 'simulate'
  try {
    const result = await configurationApi.simulate(selected.value.id)
    simulationPath.value = result.path
    simulationErrors.value = result.errors
    simulationVisible.value = true
    if (!result.isValid) {
      ElMessage.warning('试运行发现配置问题，请先修正')
    }
  } catch (error) {
    ElMessage.error(errorText(error, '试运行失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function openVersionHistory() {
  if (!selected.value) return
  actionLoading.value = 'history'
  try {
    versionHistory.value = await configurationApi.versions(selected.value.id)
    versionHistoryVisible.value = true
  } catch (error) {
    ElMessage.error(errorText(error, '读取版本历史失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function selectIdFromHistory(row: WorkflowConfigurationDto) {
  versionHistoryVisible.value = false
  await selectConfiguration(row.id)
  await rollbackConfiguration()
}

watch(selectedNodeIndex, () => {
  selectedFieldIndex.value = 0
  previewData.value = {}
})

onMounted(load)
</script>

<template>
  <div class="page-shell" data-testid="configuration-center-page">
    <MainColumnPageHeader
      intro-key="configurations"
      title="流程配置"
      scope-label="当前授权部门 · 流程配置版本"
      kicker="草稿 · 校验 · 发布 · 回滚"
    >
      <template #actions>
        <div class="head-actions">
        <el-button v-if="auth.can('scenario:read')" @click="router.push('/scenarios?mode=manage')">
          业务场景配置
        </el-button>
        <el-button v-if="selected && isEditable" :loading="actionLoading === 'save'" @click="saveDraft">保存草稿</el-button>
        <el-button v-if="selected && canUpdate" :loading="actionLoading === 'validate'" @click="validateConfiguration">校验</el-button>
        <el-button v-if="selected" :loading="actionLoading === 'simulate'" @click="simulateConfiguration">试运行</el-button>
        <el-button
          v-if="selected && canPublish && ['Draft', 'Validated'].includes(selected.state)"
          type="primary"
          :loading="actionLoading === 'publish'"
          @click="publishConfiguration"
        >发布</el-button>
        <el-button
          v-if="selected && canUpdate && selected.publishedWorkflowVersionId"
          :loading="['disable', 'restore'].includes(actionLoading)"
          @click="toggleTemplateStatus"
        >
          {{ selected.state === 'Published' ? '停用' : '恢复' }}
        </el-button>
        <el-button
          v-if="selected"
          :loading="actionLoading === 'history'"
          @click="openVersionHistory"
        >版本历史</el-button>
        <el-button
          v-if="selected && canRollback && selected.state === 'Retired'"
          :loading="actionLoading === 'rollback'"
          @click="rollbackConfiguration"
        >回滚到此版本</el-button>
        <el-button v-if="canCreate" text type="primary" :disabled="loading || !departments.length" @click="openCreate()">新建配置</el-button>
        </div>
      </template>
    </MainColumnPageHeader>

    <el-alert v-if="errorMessage" type="error" :closable="false" show-icon :title="errorMessage" />

    <section class="metrics">
      <article><span>全部版本</span><strong>{{ configurations.length }}</strong></article>
      <article><span>发布中</span><strong>{{ activeCount }}</strong></article>
      <article><span>草稿与校验</span><strong>{{ draftCount }}</strong></article>
      <article>
        <span>风险级别</span>
        <strong>{{ selected?.riskLevel === 'High' ? '高' : selected?.riskLevel === 'Medium' ? '中' : '低' }}</strong>
      </article>
    </section>

    <section v-loading="loading" class="configuration-shell">
      <aside class="section-card configuration-list">
        <div class="section-head compact">
          <div><h2>配置版本</h2><p>草稿不会影响正式流程</p></div>
          <el-select
            v-model="filterState"
            data-testid="configuration-state-filter"
            clearable
            placeholder="全部状态"
            style="width: 120px"
            @change="load"
          >
            <el-option label="草稿" value="Draft" />
            <el-option label="已校验" value="Validated" />
            <el-option label="已发布" value="Published" />
            <el-option label="已退役" value="Retired" />
          </el-select>
        </div>
        <div v-if="configurations.length" class="version-list">
          <button
            v-for="item in configurations"
            :key="item.id"
            type="button"
            class="version-item"
            :class="{ active: item.id === selectedId }"
            @click="selectConfiguration(item.id)"
          >
            <span class="version-line">
              <strong>{{ item.name }}</strong>
              <StatusBadge :label="stateLabel(item.state)" :tone="stateTone(item.state)" />
            </span>
            <small>{{ item.configurationKey }} · v{{ item.versionNumber }}</small>
            <small>{{ item.departmentName || '未指定部门' }} · Revision {{ item.revision }}</small>
          </button>
        </div>
        <el-empty v-else-if="!loading" description="暂无流程配置">
          <el-button v-if="canCreate" type="primary" :disabled="loading || !departments.length" @click="openCreate()">创建第一个配置</el-button>
        </el-empty>
        <div class="component-library">
          <div class="library-heading">
            <strong>字段组件库</strong>
            <small>只添加后端允许的字段和动作</small>
          </div>
          <div class="component-grid">
            <button
              v-for="[type, label] in fieldTypes"
              :key="type"
              type="button"
              :disabled="!isEditable"
              @click="addField(type)"
            >
              {{ label }}
            </button>
          </div>
          <div class="library-heading split">
            <strong>业务能力</strong>
            <small>已注册动作</small>
          </div>
          <div class="capability-chips">
            <span>完整性检查</span>
            <span>风险识别</span>
            <span>生成草稿</span>
            <span>人员/部门</span>
            <span>附件证据</span>
          </div>
        </div>
      </aside>

      <main v-if="selected" class="canvas-column">
        <section class="section-card canvas-toolbar">
          <div>
            <div class="eyebrow-row">
              <span class="code-label">{{ selected.configurationKey }}</span>
              <StatusBadge :label="stateLabel(selected.state)" :tone="stateTone(selected.state)" />
              <span class="tag">{{ selected.departmentName }}</span>
            </div>
            <h2>{{ selected.name }}</h2>
            <p>{{ selected.description || '未填写配置说明' }}</p>
          </div>
          <div class="toolbar-actions">
            <el-button v-if="canCreate" @click="openCreate(selected)">复制模板</el-button>
            <span v-if="selected.publishedWorkflowVersionId" class="published-version">
              当前可执行版本：{{ selected.publishedWorkflowVersionId.slice(0, 8) }}
            </span>
          </div>
        </section>

        <section class="section-card flow-section">
          <div class="section-head">
            <div><h2>流程编排</h2><p>选择节点后，在右侧配置处理人、下一节点和条件。</p></div>
            <el-button v-if="isEditable" size="small" @click="addNode">＋ 标准节点</el-button>
          </div>
          <div class="flow">
            <button
              v-for="(node, index) in selected.nodes"
              :key="`${node.code}-${index}`"
              type="button"
              class="flow-node"
              :class="{ selected: index === selectedNodeIndex, terminal: node.completeOnApprove || !node.nextNode }"
              :data-testid="`configuration-node-${index}`"
              @click="selectedNodeIndex = index"
            >
              <span class="node-index">{{ index + 1 }}</span>
              <span class="node-copy">
                <strong>{{ node.name }}</strong>
                <small>{{ node.assigneeStrategy }}{{ node.assigneeUserName ? ` · ${node.assigneeUserName}` : '' }}{{ node.businessRoleCode ? ` · ${node.businessRoleCode}` : '' }} · {{ node.nextNode ? `下一节点 ${node.nextNode}` : '流程完成' }}</small>
              </span>
              <span class="node-code">{{ node.code }}</span>
            </button>
          </div>
        </section>

        <section v-if="previewForm" class="section-card preview-section">
          <div class="section-head">
            <div>
              <h2>员工实际看到的表单</h2>
              <p>预览使用当前节点字段；发布后任务会冻结此定义。</p>
            </div>
            <el-tag size="small" type="info" effect="plain">实时预览</el-tag>
          </div>
          <WorkflowFormRenderer
            v-model="previewData"
            :form="previewForm"
            :departments="departments"
            preview
          />
        </section>

        <section v-if="selected.validation" class="section-card validation-card">
          <div class="section-head">
            <div>
              <h2>校验与影响</h2>
              <p>{{ selected.validation.isValid ? '结构校验通过' : '仍有需要处理的问题' }}</p>
            </div>
            <StatusBadge
              :label="selected.validation.isValid ? '通过' : `${selected.validation.errors.length} 个问题`"
              :tone="selected.validation.isValid ? 'success' : 'warning'"
            />
          </div>
          <div class="validation-summary">
            <span>{{ selected.validation.nodeCount }} 个节点</span>
            <span>{{ selected.validation.assigneeCount }} 个处理人</span>
            <span>{{ selected.validation.terminalNodeCount }} 个完成节点</span>
          </div>
          <ul v-if="selected.validation.errors.length" class="errors">
            <li v-for="message in selected.validation.errors" :key="message">{{ message }}</li>
          </ul>
          <ul v-if="selected.validation.warnings.length" class="warnings">
            <li v-for="message in selected.validation.warnings" :key="message">{{ message }}</li>
          </ul>
        </section>
      </main>

      <aside v-if="selected && currentNode" class="section-card inspector">
        <div class="section-head compact">
          <div>
            <p class="page-kicker">节点属性</p>
            <h2>{{ currentNode.name }}</h2>
          </div>
          <el-button
            v-if="isEditable && selected.nodes.length > 1"
            text
            type="danger"
            @click="removeCurrentNode"
          >删除</el-button>
        </div>

        <el-form label-position="top" :disabled="!isEditable">
          <el-form-item label="节点编码">
            <el-input
              :model-value="currentNode.code"
              @update:model-value="updateNodeCode"
            />
          </el-form-item>
          <el-form-item label="节点名称">
            <el-input v-model="currentNode.name" />
          </el-form-item>
          <el-form-item label="处理人策略">
            <el-select
              v-model="currentNode.assigneeStrategy"
              data-testid="assignee-strategy-select"
              style="width: 100%"
            >
              <el-option label="指定人员" value="SpecificUser" />
              <el-option label="业务角色" value="BusinessRole" />
              <el-option label="部门负责人" value="DepartmentManager" />
              <el-option label="项目负责人" value="ProjectOwner" />
              <el-option label="发起人主管" value="InitiatorManager" />
              <el-option label="数字员工" value="Agent" />
            </el-select>
          </el-form-item>
          <el-form-item v-if="currentNode.assigneeStrategy === 'SpecificUser'" label="指定人员">
            <el-select v-model="currentNode.assigneeUserName" filterable style="width: 100%">
              <el-option
                v-for="user in users"
                :key="user.id"
                :label="`${user.displayName} · ${user.userName}`"
                :value="user.userName"
              />
            </el-select>
          </el-form-item>
          <el-form-item v-if="currentNode.assigneeStrategy === 'BusinessRole'" label="业务流程角色">
            <el-select v-model="currentNode.businessRoleCode" filterable style="width: 100%">
              <el-option
                v-for="role in workflowRoles"
                :key="role.id"
                :label="`${role.name} · ${role.code}`"
                :value="role.code"
              />
            </el-select>
          </el-form-item>
          <el-form-item v-if="currentNode.assigneeStrategy === 'Agent'" label="数字员工">
            <el-select
              v-model="currentNode.agentProfileId"
              filterable
              placeholder="搜索并选择有效数字员工"
              style="width: 100%"
              data-testid="agent-profile-select"
            >
              <el-option
                v-for="agent in agentProfiles"
                :key="agent.id"
                :label="`${agent.name} · ${agent.autonomyLevel} ${autonomyLabels[agent.autonomyLevel].action}`"
                :value="agent.id"
              >
                <span>{{ agent.name }}</span>
                <small>
                  {{ agent.code || '数字员工' }} · {{ agent.description || `${riskLabels[agent.riskLimit]} · 限额 ${agent.amountLimit}` }}
                </small>
              </el-option>
            </el-select>
            <p class="field-hint">
              {{ agentProfiles.find((item) => item.id === currentNode?.agentProfileId)?.allowedTools.map(formatToolName).join(' / ') || '仅可选择已发布且有效的数字员工' }}
            </p>
          </el-form-item>
          <el-form-item label="节点业务说明">
            <el-input
              v-model="currentNode.businessDescription"
              type="textarea"
              :rows="2"
              placeholder="告诉员工本步骤要完成什么，不显示流程引擎术语"
            />
          </el-form-item>
          <div class="inspector-grid">
            <el-form-item label="SLA（分钟）">
              <el-input-number
                v-model="currentNode.slaMinutes"
                :min="1"
                :controls="false"
                style="width: 100%"
              />
            </el-form-item>
            <el-form-item label="必交附件数">
              <el-input-number
                v-model="currentNode.requiredAttachmentCount"
                :min="0"
                :max="50"
                style="width: 100%"
              />
            </el-form-item>
          </div>
          <el-form-item label="交付物">
            <el-select
              v-model="currentNode.deliverables"
              multiple
              filterable
              allow-create
              default-first-option
              placeholder="例如检测报告、验收记录"
              style="width: 100%"
            />
          </el-form-item>
          <el-form-item label="完成条件">
            <el-input
              v-model="currentNode.completionCondition"
              placeholder="例如：负责人签认且附件齐全"
            />
          </el-form-item>
          <el-form-item label="相关岗位">
            <el-select
              v-model="currentNode.relatedRoleCodes"
              multiple
              filterable
              placeholder="选择相关业务角色"
              style="width: 100%"
            >
              <el-option
                v-for="role in workflowRoles"
                :key="role.id"
                :label="role.name"
                :value="role.code"
              />
            </el-select>
          </el-form-item>
          <section class="inspector-section">
            <div class="inspector-section-head">
              <div>
                <strong>节点字段</strong>
                <small>排序、角色可见/可写和业务对象绑定由服务端再校验</small>
              </div>
              <el-button size="small" @click="addField('Text')">＋字段</el-button>
            </div>
            <div
              v-if="currentNode.fields?.length"
              class="field-list"
              @dragover.prevent
            >
              <button
                v-for="(field, index) in currentNode.fields"
                :key="`${field.key}-${index}`"
                type="button"
                draggable="true"
                :class="{ active: selectedFieldIndex === index }"
                @dragstart="draggingFieldIndex = index"
                @dragover.prevent
                @drop="dropField(index)"
                @click="selectedFieldIndex = index"
              >
                <span>{{ field.displayName }}</span>
                <small>{{ field.dataType }} · {{ field.required ? '必填' : '选填' }}</small>
              </button>
            </div>
            <el-empty v-else description="尚未添加字段" :image-size="50" />
            <template v-if="currentField">
              <div class="field-actions">
                <el-button size="small" @click="duplicateField(selectedFieldIndex)">复制</el-button>
                <el-button
                  size="small"
                  :disabled="selectedFieldIndex === 0"
                  @click="moveField(selectedFieldIndex, selectedFieldIndex - 1)"
                >上移</el-button>
                <el-button
                  size="small"
                  :disabled="selectedFieldIndex === (currentNode.fields?.length ?? 1) - 1"
                  @click="moveField(selectedFieldIndex, selectedFieldIndex + 1)"
                >下移</el-button>
                <el-button size="small" type="danger" plain @click="removeField(selectedFieldIndex)">删除</el-button>
              </div>
              <div class="inspector-grid">
                <el-form-item label="字段编码">
                  <el-input v-model="currentField.key" />
                </el-form-item>
                <el-form-item label="显示名">
                  <el-input v-model="currentField.displayName" />
                </el-form-item>
                <el-form-item label="字段类型">
                  <el-select v-model="currentField.dataType" style="width: 100%">
                    <el-option
                      v-for="[type, label] in fieldTypes"
                      :key="type"
                      :label="label"
                      :value="type"
                    />
                  </el-select>
                </el-form-item>
                <el-form-item label="数据来源">
                  <el-select v-model="currentField.dataSource" style="width: 100%">
                    <el-option label="本节点填写" value="TaskInput" />
                    <el-option label="业务对象引用" value="BusinessObjectReference" />
                    <el-option label="证据引用" value="EvidenceReference" />
                  </el-select>
                </el-form-item>
              </div>
              <el-form-item
                v-if="currentField.dataSource === 'BusinessObjectReference'"
                label="绑定业务对象字段"
              >
                <el-select v-model="currentField.boundObjectFieldCode" filterable style="width: 100%">
                  <el-option
                    v-for="field in boundObjectFields"
                    :key="field.key"
                    :label="`${field.displayName} · ${field.code || field.key}`"
                    :value="field.code || field.key"
                  />
                </el-select>
              </el-form-item>
              <el-form-item
                v-if="['Select', 'MultiSelect'].includes(currentField.dataType)"
                label="选项"
              >
                <el-select
                  v-model="currentField.options"
                  multiple
                  filterable
                  allow-create
                  default-first-option
                  style="width: 100%"
                />
              </el-form-item>
              <div class="inspector-grid">
                <el-form-item label="必填">
                  <el-switch v-model="currentField.required" />
                </el-form-item>
                <el-form-item label="只读">
                  <el-switch v-model="currentField.readOnly" />
                </el-form-item>
                <el-form-item label="宽度">
                  <el-select v-model="currentField.width" style="width: 100%">
                    <el-option label="整行" value="Full" />
                    <el-option label="半行" value="Half" />
                    <el-option label="三分之一" value="Third" />
                  </el-select>
                </el-form-item>
                <el-form-item label="敏感等级">
                  <el-select v-model="currentField.sensitivity" style="width: 100%">
                    <el-option label="公开" value="Public" />
                    <el-option label="内部" value="Internal" />
                    <el-option label="敏感" value="Sensitive" />
                    <el-option label="核心" value="Core" />
                  </el-select>
                </el-form-item>
              </div>
              <el-form-item label="可见岗位">
                <el-select v-model="currentField.visibleRoleCodes" multiple clearable style="width: 100%">
                  <el-option v-for="role in workflowRoles" :key="role.id" :label="role.name" :value="role.code" />
                </el-select>
              </el-form-item>
              <el-form-item label="可写岗位">
                <el-select v-model="currentField.writeRoleCodes" multiple clearable style="width: 100%">
                  <el-option v-for="role in workflowRoles" :key="role.id" :label="role.name" :value="role.code" />
                </el-select>
              </el-form-item>
              <el-form-item label="帮助说明">
                <el-input v-model="currentField.helpText" />
              </el-form-item>
            </template>
          </section>
          <section class="inspector-section">
            <div class="inspector-section-head">
              <div>
                <strong>数字员工与 AI 评论</strong>
                <small>AiGateway 当前仅规则模拟</small>
              </div>
            </div>
            <div class="inspector-grid">
              <el-form-item label="辅助">
                <el-switch v-model="currentNode.allowAgentAssist" />
              </el-form-item>
              <el-form-item label="超时代理">
                <el-switch v-model="currentNode.allowAgentProxy" />
              </el-form-item>
              <el-form-item label="AI评论">
                <el-switch v-model="currentAiReview.enabled" />
              </el-form-item>
              <el-form-item label="永久人工">
                <el-switch v-model="currentNode.permanentHumanBoundary" />
              </el-form-item>
            </div>
            <template v-if="currentAiReview.enabled">
              <el-form-item label="注册动作">
                <el-select v-model="currentAiReview.registeredAction" style="width: 100%">
                  <el-option
                    v-for="[code, label] in registeredAiActions"
                    :key="code"
                    :label="label"
                    :value="code"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="辅助数字员工">
                <el-select v-model="currentAiReview.agentProfileId" clearable filterable style="width: 100%">
                  <el-option
                    v-for="agent in agentProfiles"
                    :key="agent.id"
                    :label="agent.name"
                    :value="agent.id"
                  />
                </el-select>
              </el-form-item>
              <el-alert
                type="warning"
                :closable="false"
                title="规则模拟：AI 只输出建议、草稿或审查意见，不修改正式状态，也不能代替真人审批。"
              />
            </template>
          </section>
          <section class="inspector-section">
            <div class="inspector-section-head">
              <div>
                <strong>通知与升级</strong>
                <small>提醒、升级和节点动作</small>
              </div>
            </div>
            <div class="inspector-grid">
              <el-form-item label="启用通知">
                <el-switch v-model="currentNotification.enabled" />
              </el-form-item>
              <el-form-item label="提前提醒">
                <el-input-number v-model="currentNotification.beforeDueMinutes" :min="0" style="width: 100%" />
              </el-form-item>
              <el-form-item label="超时升级">
                <el-input-number v-model="currentNotification.escalateAfterMinutes" :min="0" style="width: 100%" />
              </el-form-item>
              <el-form-item label="升级岗位">
                <el-select v-model="currentNotification.escalateToRoleCode" clearable style="width: 100%">
                  <el-option v-for="role in workflowRoles" :key="role.id" :label="role.name" :value="role.code" />
                </el-select>
              </el-form-item>
            </div>
            <div class="node-actions-editor">
              <button
                v-for="(action, index) in currentNode.actions"
                :key="`${action.code}-${index}`"
                type="button"
                @click="currentNode.actions?.splice(index, 1)"
              >
                {{ action.name }} ×
              </button>
              <el-button size="small" @click="addNodeAction">＋动作</el-button>
            </div>
          </section>
          <el-form-item label="下一节点">
            <el-select v-model="currentNode.nextNode" clearable style="width: 100%">
              <el-option
                v-for="node in selected.nodes.filter((item) => item.code !== currentNode?.code)"
                :key="node.code"
                :label="`${node.name} · ${node.code}`"
                :value="node.code"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="审批后完成">
            <el-switch v-model="currentNode.completeOnApprove" />
          </el-form-item>
          <el-form-item label="条件分支">
            <el-switch
              :model-value="Boolean(currentNode.condition)"
              @update:model-value="currentNode.condition = $event ? { field: 'TrialRequired', operator: 'equals', value: 'Yes' } : null"
            />
          </el-form-item>
          <template v-if="currentNode.condition">
            <el-form-item label="条件字段">
              <el-input v-model="currentNode.condition.field" />
            </el-form-item>
            <el-form-item label="操作符">
              <el-select v-model="currentNode.condition.operator" style="width: 100%">
                <el-option label="等于" value="equals" />
                <el-option label="不等于" value="notEquals" />
                <el-option label="大于" value="greaterThan" />
                <el-option label="大于等于" value="greaterThanOrEqual" />
              </el-select>
            </el-form-item>
            <el-form-item label="条件值">
              <el-input v-model="currentNode.condition.value" />
            </el-form-item>
          </template>
        </el-form>

        <div class="boundary-note">
          发布和回滚属于受控操作。高风险业务动作仍须人工签认，不得由此页面绕过。
        </div>
      </aside>
      <main v-else-if="!loading" class="section-card empty-workspace">
        <el-empty description="选择一个配置版本开始编辑" />
      </main>
    </section>

    <el-dialog v-model="createVisible" title="新建流程配置" width="min(760px, 94vw)">
      <el-form label-position="top">
        <div class="form-grid">
          <el-form-item label="配置编号">
            <el-input v-model="draft.configurationKey" placeholder="例如 RND_TEST_APPROVAL" />
          </el-form-item>
          <el-form-item label="配置名称">
            <el-input v-model="draft.name" />
          </el-form-item>
          <el-form-item label="所属部门">
            <el-select v-model="draft.departmentId" style="width: 100%">
              <el-option v-for="item in departments" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item label="业务对象类型">
            <el-select
              v-model="draft.objectTypeCode"
              filterable
              style="width: 100%"
              data-testid="configuration-object-type"
            >
              <el-option label="ANY · 通用事项" value="ANY" />
              <el-option
                v-for="type in publishedObjectTypes"
                :key="type.id"
                :label="`${type.name} · ${type.code}`"
                :value="type.code"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="开始节点">
            <el-select v-model="draft.startNode" style="width: 100%">
              <el-option v-for="node in draft.nodes" :key="node.code" :label="`${node.name} · ${node.code}`" :value="node.code" />
            </el-select>
          </el-form-item>
          <el-form-item v-if="draft.nodes.length" label="首个节点处理人（指定人员模板）">
            <el-select v-model="draft.nodes[0].assigneeUserName" filterable style="width: 100%">
              <el-option v-for="user in users" :key="user.id" :label="user.displayName" :value="user.userName" />
            </el-select>
          </el-form-item>
          <el-form-item class="span-2" label="配置说明">
            <el-input v-model="draft.description" type="textarea" :rows="3" />
          </el-form-item>
          <el-form-item label="部门默认模板">
            <el-switch v-model="draft.isDefault" />
          </el-form-item>
          <el-form-item label="适用岗位">
            <el-select
              v-model="draft.applicableRoleCodes"
              multiple
              filterable
              style="width: 100%"
            >
              <el-option
                v-for="role in workflowRoles"
                :key="role.id"
                :label="role.name"
                :value="role.code"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="风险级别">
            <el-select v-model="draft.riskLevel" clearable style="width: 100%">
              <el-option label="自动判断（推荐）" value="" />
              <el-option label="低风险" value="Low" />
              <el-option label="中风险" value="Medium" />
              <el-option label="高风险（平台审核）" value="High" />
            </el-select>
          </el-form-item>
        </div>
        <div class="callout">
          部门流程负责人可发布低风险单部门模板；跨部门、付款、质量放行、配方、
          设备、联锁、合同承诺和重大赔付继续由平台管理员审核。
        </div>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="actionLoading === 'create'" @click="createDraft">创建草稿</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="simulationVisible" title="流程试运行" width="min(680px, 94vw)">
      <el-alert
        v-if="simulationErrors.length"
        type="error"
        :closable="false"
        :title="simulationErrors.join('；')"
      />
      <ol v-else class="simulation-path">
        <li v-for="node in simulationPath" :key="node">{{ node }}</li>
      </ol>
      <p class="simulation-note">
        试运行只做结构和路径检查，不创建真实业务对象、流程实例、任务或外部副作用。
      </p>
    </el-dialog>

    <el-dialog v-model="versionHistoryVisible" title="模板版本历史" width="min(880px, 96vw)">
      <el-table :data="versionHistory">
        <el-table-column prop="versionNumber" label="版本" width="70" />
        <el-table-column prop="name" label="名称" min-width="180" />
        <el-table-column prop="state" label="状态" width="100" />
        <el-table-column prop="riskLevel" label="风险" width="80" />
        <el-table-column label="发布时间" min-width="170">
          <template #default="{ row }">
            {{ row.publishedAt ? new Date(row.publishedAt).toLocaleString() : '未发布' }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="110">
          <template #default="{ row }">
            <el-button
              v-if="row.state === 'Retired' && canRollback"
              text
              type="primary"
              @click="selectIdFromHistory(row)"
            >回滚到此版</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-dialog>
  </div>
</template>

<style scoped>
.head-actions,
.section-head,
.section-head.compact,
.version-line,
.eyebrow-row,
.toolbar-actions,
.validation-summary {
  display: flex;
  align-items: center;
}

.head-actions,
.toolbar-actions {
  flex-wrap: wrap;
  gap: 9px;
}

.metrics {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
  margin-bottom: 16px;
}

.metrics article {
  border: 1px solid var(--aidt-border);
  border-radius: 15px;
  background: #fff;
  padding: 15px 17px;
}

.metrics span,
.metrics strong {
  display: block;
}

.metrics span {
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
}

.metrics strong {
  margin-top: 5px;
  color: var(--aidt-brand-strong);
  font-size: 1.7rem;
}

.configuration-shell {
  display: grid;
  grid-template-columns: 250px minmax(380px, 1fr) 310px;
  align-items: start;
  gap: 14px;
  min-height: 660px;
}

.configuration-list,
.inspector {
  position: sticky;
  top: 92px;
  max-height: calc(100vh - 112px);
  overflow-y: auto;
  padding: 16px;
}

.section-head {
  justify-content: space-between;
  gap: 14px;
  margin-bottom: 15px;
}

.section-head.compact {
  align-items: flex-start;
}

.section-head h2,
.section-head p {
  margin: 0;
}

.section-head h2 {
  font-size: 1.05rem;
}

.section-head p {
  margin-top: 4px;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.version-list {
  display: grid;
  gap: 8px;
}

.version-item {
  display: grid;
  gap: 5px;
  width: 100%;
  border: 1px solid transparent;
  border-radius: 12px;
  background: transparent;
  padding: 12px;
  color: var(--aidt-ink);
  text-align: left;
  cursor: pointer;
}

.version-item:hover {
  background: #f5f9f6;
}

.version-item.active {
  border-color: #c0d9cb;
  background: #edf6f0;
}

.version-line {
  justify-content: space-between;
  gap: 8px;
}

.version-line strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.version-item small {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.component-library {
  display: grid;
  gap: 10px;
  margin-top: 18px;
  border-top: 1px solid var(--aidt-border);
  padding-top: 16px;
}

.library-heading {
  display: grid;
  gap: 3px;
}

.library-heading small {
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
}

.library-heading.split {
  margin-top: 8px;
}

.component-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 6px;
}

.component-grid button,
.capability-chips span,
.node-actions-editor button {
  border: 1px solid var(--aidt-border);
  border-radius: 9px;
  background: #fff;
  padding: 6px 7px;
  color: var(--aidt-ink);
  font-size: 0.68rem;
  text-align: left;
  cursor: pointer;
}

.component-grid button:hover:not(:disabled) {
  border-color: var(--aidt-brand);
  color: var(--aidt-brand);
}

.component-grid button:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.capability-chips,
.node-actions-editor {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.capability-chips span {
  background: #eef6f1;
  cursor: default;
}

.canvas-column {
  display: grid;
  gap: 14px;
  min-width: 0;
}

.preview-section {
  padding: 18px;
}

.canvas-toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
  padding: 18px;
}

.canvas-toolbar h2,
.canvas-toolbar p {
  margin: 0;
}

.canvas-toolbar h2 {
  margin-top: 9px;
  font-size: 1.5rem;
}

.canvas-toolbar p {
  margin-top: 6px;
  color: var(--aidt-ink-muted);
  line-height: 1.6;
}

.eyebrow-row {
  flex-wrap: wrap;
  gap: 8px;
}

.code-label,
.tag {
  border-radius: 999px;
  background: #eff4f1;
  padding: 5px 9px;
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.code-label {
  color: var(--aidt-brand);
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-weight: 800;
}

.published-version {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.flow-section,
.validation-card {
  padding: 18px;
}

.flow {
  display: grid;
  gap: 0;
  padding: 18px 16px;
  border: 1px solid #e1e9e3;
  border-radius: 15px;
  background-color: #fbfdfb;
  background-image: radial-gradient(#dce7df 1px, transparent 1px);
  background-size: 20px 20px;
}

.flow-node {
  position: relative;
  display: grid;
  grid-template-columns: 34px minmax(0, 1fr) auto;
  align-items: center;
  gap: 11px;
  width: min(100%, 560px);
  margin: 0 auto;
  border: 1px solid #cfded5;
  border-radius: 14px;
  background: #fff;
  padding: 13px;
  text-align: left;
  box-shadow: 0 8px 20px rgb(28 68 49 / 6%);
  cursor: pointer;
}

.flow-node:not(:last-child)::after {
  content: "";
  position: absolute;
  bottom: -18px;
  left: 50%;
  width: 2px;
  height: 18px;
  background: #aac8b8;
}

.flow-node.selected {
  border-color: #579a7b;
  box-shadow: 0 0 0 3px rgb(61 137 101 / 10%);
}

.flow-node.terminal .node-index {
  background: #d8efe3;
}

.node-index {
  display: grid;
  width: 34px;
  height: 34px;
  place-items: center;
  border-radius: 11px;
  background: #eef4f0;
  color: var(--aidt-brand);
  font-weight: 800;
}

.node-copy {
  min-width: 0;
}

.node-copy strong,
.node-copy small {
  display: block;
}

.node-copy strong {
  font-size: 0.86rem;
}

.node-copy small {
  margin-top: 4px;
  overflow: hidden;
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.node-code {
  color: var(--aidt-ink-muted);
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.68rem;
}

.inspector {
  background: #f8faf8;
}

.inspector-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 10px;
}

.inspector-section {
  display: grid;
  gap: 10px;
  margin: 14px 0;
  border-top: 1px solid var(--aidt-border);
  padding-top: 14px;
}

.inspector-section-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 8px;
}

.inspector-section-head strong,
.inspector-section-head small {
  display: block;
}

.inspector-section-head small {
  margin-top: 3px;
  color: var(--aidt-ink-muted);
  font-size: 0.66rem;
  line-height: 1.45;
}

.field-list {
  display: grid;
  gap: 5px;
}

.field-list button {
  display: grid;
  gap: 2px;
  border: 1px solid var(--aidt-border);
  border-radius: 9px;
  background: #fff;
  padding: 7px 9px;
  text-align: left;
  cursor: grab;
}

.field-list button.active {
  border-color: var(--aidt-brand);
  background: #edf7f1;
}

.field-list small {
  color: var(--aidt-ink-muted);
  font-size: 0.65rem;
}

.field-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 4px;
}

.simulation-path {
  display: grid;
  gap: 8px;
  padding-left: 24px;
}

.simulation-path li {
  border-left: 3px solid var(--aidt-brand);
  background: #f5faf7;
  padding: 9px 12px;
}

.simulation-note {
  margin-top: 14px;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
  line-height: 1.6;
}

.boundary-note,
.callout {
  border: 1px solid #ecd5a6;
  border-radius: 12px;
  background: var(--aidt-warning-soft);
  padding: 12px;
  color: #76521b;
  font-size: 0.72rem;
  line-height: 1.65;
}

.field-hint {
  margin: 7px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
  line-height: 1.5;
}

.validation-summary {
  flex-wrap: wrap;
  gap: 8px;
  margin-bottom: 12px;
}

.validation-summary span {
  border-radius: 999px;
  background: #f0f5f2;
  padding: 6px 9px;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.errors,
.warnings {
  margin: 8px 0 0;
  padding-left: 20px;
  font-size: 0.78rem;
  line-height: 1.7;
}

.errors {
  color: var(--aidt-danger);
}

.warnings {
  color: var(--aidt-warning);
}

.empty-workspace {
  min-height: 380px;
  display: grid;
  place-items: center;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 14px;
}

.span-2 {
  grid-column: 1 / -1;
}

@media (max-width: 1180px) {
  .configuration-shell {
    grid-template-columns: 220px minmax(0, 1fr);
  }

  .inspector {
    position: static;
    grid-column: 1 / -1;
    max-height: none;
  }
}

@media (max-width: 760px) {
  .metrics,
  .configuration-shell,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .configuration-list,
  .inspector {
    position: static;
    max-height: none;
  }

  .canvas-toolbar,
  .page-head {
    align-items: stretch;
    flex-direction: column;
  }

  .version-list {
    max-height: 280px;
    overflow-y: auto;
  }

  .flow-node {
    grid-template-columns: 34px minmax(0, 1fr);
  }

  .node-code {
    grid-column: 2;
  }

  .span-2 {
    grid-column: auto;
  }
}
</style>
