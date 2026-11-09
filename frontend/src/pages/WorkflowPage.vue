<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  BusinessObjectDto,
  WorkflowDefinitionDto,
  WorkflowInstanceDto,
  WorkflowTaskDto,
  WorkflowTimelineItemDto,
  WorkflowNodePolicyDto,
  WorkflowTaskForm,
} from '@/api/types/contracts'
import FeaturePlaceholder from '@/components/business/FeaturePlaceholder.vue'
import StatusBadge from '@/components/base/StatusBadge.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import WorkflowStateGuide from '@/features/workflow/components/WorkflowStateGuide.vue'
import SlaStatusBadge from '@/features/workflow/components/SlaStatusBadge.vue'
import WorkflowFormRenderer from '@/features/workflow/components/WorkflowFormRenderer.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const loading = ref(true)
const actionLoading = ref('')
const errorMessage = ref('')
const tasks = ref<WorkflowTaskDto[]>([])
const instances = ref<WorkflowInstanceDto[]>([])
const definitions = ref<WorkflowDefinitionDto[]>([])
const objects = ref<BusinessObjectDto[]>([])
const selectedDefinition = ref('')
const selectedObject = ref('')
const timelineVisible = ref(false)
const timelineLoading = ref(false)
const timeline = ref<WorkflowTimelineItemDto[]>([])
const timelineTitle = ref('')
const policyVisible = ref(false)
const policies = ref<WorkflowNodePolicyDto[]>([])
const taskFormVisible = ref(false)
const taskFormLoading = ref(false)
const activeTask = ref<WorkflowTaskDto | null>(null)
const activeTaskForm = ref<WorkflowTaskForm | null>(null)
const activeFormData = ref<Record<string, string | number | boolean | string[] | null>>({})
const activeDecision = ref<'Approve' | 'Return' | 'Terminate'>('Approve')
const taskComment = ref('')
const taskDepartments = ref<Awaited<ReturnType<typeof workspaceApi.departments>>>([])
const aiReviewLoading = ref(false)
const aiReviewResult = ref<{
  suggestion: string
  mode: string
  externalModelCalled: boolean
  requiresConfirmation: boolean
} | null>(null)
const route = useRoute()
const router = useRouter()
const scopedTasks = computed(() => {
  const departmentId = String(route.query.departmentId ?? '')
  return departmentId
    ? tasks.value.filter((task) => task.departmentId === departmentId)
    : tasks.value
})
const visibleInstances = computed(() => {
  const objectId = String(route.query.object ?? '')
  const departmentId = String(route.query.departmentId ?? '')
  return instances.value.filter((instance) =>
    (!objectId || instance.businessObjectId === objectId)
    && (!departmentId || instance.departmentId === departmentId),
  )
})

function showError(error: unknown) {
  errorMessage.value = error instanceof ApiError
    ? `${error.message} · ${error.code} · ${error.traceId}`
    : '操作失败，请稍后重试。'
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [taskPage, instancePage, definitionData, objectPage] = await Promise.all([
      workspaceApi.tasks(),
      workspaceApi.workflowInstances(),
      workspaceApi.workflowDefinitions(),
      workspaceApi.businessObjects(),
    ])
    tasks.value = taskPage.items
    instances.value = instancePage.items
    definitions.value = definitionData
    objects.value = objectPage.items
    selectedDefinition.value ||= definitionData[0]?.versionId ?? ''
    selectedObject.value ||= objectPage.items[0]?.id ?? ''
    const requestedInstanceId = String(route.query.instance ?? '')
    const requestedInstance = instancePage.items.find((item) => item.id === requestedInstanceId)
    if (requestedInstance) {
      await openTimeline(requestedInstance)
    }
  } catch (error) {
    showError(error)
  } finally {
    loading.value = false
  }
}

async function startWorkflow() {
  const definition = definitions.value.find((item) => item.versionId === selectedDefinition.value)
  if (!definition || !selectedObject.value) {
    ElMessage.warning('请选择流程和业务对象')
    return
  }

  actionLoading.value = 'start'
  try {
    await workspaceApi.startWorkflow(
      definition.id,
      definition.versionId,
      selectedObject.value,
    )
    ElMessage.success('流程已发起，首个待办已生成')
    await load()
  } catch (error) {
    showError(error)
  } finally {
    actionLoading.value = ''
  }
}

async function decide(
  task: WorkflowTaskDto,
  decision: 'Approve' | 'Return' | 'Terminate',
) {
  const hasForm = task.formDefinitionJson && task.formDefinitionJson.trim() !== '[]'
  if (hasForm) {
    await openTaskForm(task, decision)
    return
  }

  let comment = ''
  if (decision === 'Approve') {
    await ElMessageBox.confirm(`确认通过“${task.nodeName || task.nodeCode}”？`, '流程审批', {
      type: 'warning',
      confirmButtonText: '确认通过',
      cancelButtonText: '取消',
    })
    comment = '桌面端审批通过'
  } else {
    const prompt = await ElMessageBox.prompt(
      decision === 'Return' ? '请输入退回原因' : '请输入终止原因',
      decision === 'Return' ? '退回任务' : '终止流程',
      {
        inputPattern: /\S+/,
        inputErrorMessage: '必须填写原因',
        type: 'warning',
        confirmButtonText: decision === 'Return' ? '确认退回' : '确认终止',
        cancelButtonText: '取消',
      },
    )
    comment = prompt.value
  }

  await submitDecision(task, decision, comment, '{}', task.formDefinitionHash)
}

async function openTaskForm(
  task: WorkflowTaskDto,
  decision: 'Approve' | 'Return' | 'Terminate' = 'Approve',
) {
  activeTask.value = task
  activeDecision.value = decision
  taskComment.value = decision === 'Approve' ? '同意' : ''
  aiReviewResult.value = null
  taskFormVisible.value = true
  taskFormLoading.value = true
  try {
    const [form, departments] = await Promise.all([
      workspaceApi.taskForm(task.id),
      workspaceApi.departments(),
    ])
    activeTaskForm.value = form
    taskDepartments.value = departments
    const parsed = JSON.parse(form.formDataJson || '{}') as Record<
      string,
      string | number | boolean | string[] | null
    >
    for (const field of form.fields) {
      if (!(field.key in parsed) && field.defaultValue) {
        parsed[field.key] = field.defaultValue
      }
    }
    activeFormData.value = parsed
  } catch (error) {
    showError(error)
    taskFormVisible.value = false
  } finally {
    taskFormLoading.value = false
  }
}

async function runAiReview() {
  if (!activeTask.value) return
  aiReviewLoading.value = true
  try {
    const result = await workspaceApi.runTaskAiReview(activeTask.value.id)
    aiReviewResult.value = {
      suggestion: result.suggestion,
      mode: result.mode,
      externalModelCalled: result.externalModelCalled,
      requiresConfirmation: result.requiresConfirmation,
    }
    ElMessage.success(
      result.requiresConfirmation
        ? 'AI 运行已请求，等待真人确认'
        : '规则模拟审查已完成',
    )
  } catch (error) {
    showError(error)
  } finally {
    aiReviewLoading.value = false
  }
}

async function submitDecision(
  task: WorkflowTaskDto,
  decision: 'Approve' | 'Return' | 'Terminate',
  comment: string,
  formDataJson: string,
  formDefinitionHash: string,
) {
  actionLoading.value = task.id
  try {
    await workspaceApi.decideTask(
      task.id,
      decision,
      task.version,
      comment,
      formDataJson,
      formDefinitionHash,
    )
    ElMessage.success('任务已处理，表单、审计与流程状态已同步保存')
    taskFormVisible.value = false
    await load()
  } catch (error) {
    showError(error)
  } finally {
    actionLoading.value = ''
  }
}

async function submitActiveForm() {
  if (!activeTask.value || !activeTaskForm.value) return
  if (
    ['Return', 'Terminate'].includes(activeDecision.value) &&
    !taskComment.value.trim()
  ) {
    ElMessage.warning('退回或终止必须填写原因')
    return
  }
  await submitDecision(
    activeTask.value,
    activeDecision.value,
    taskComment.value.trim() || (activeDecision.value === 'Approve' ? '同意' : ''),
    JSON.stringify(activeFormData.value),
    activeTaskForm.value.formDefinitionHash,
  )
}

async function saveActiveDraft() {
  if (!activeTask.value || !activeTaskForm.value) return
  actionLoading.value = activeTask.value.id
  try {
    const updated = await workspaceApi.saveTaskFormDraft(
      activeTask.value.id,
      activeTask.value.version,
      JSON.stringify(activeFormData.value),
      activeTaskForm.value.formDefinitionHash,
    )
    activeTask.value = updated
    ElMessage.success('表单草稿已保存')
    await load()
  } catch (error) {
    showError(error)
  } finally {
    actionLoading.value = ''
  }
}

async function openTimeline(instance: WorkflowInstanceDto) {
  timelineVisible.value = true
  timelineLoading.value = true
  timeline.value = []
  timelineTitle.value = `${instance.currentNodeCode ?? '已结束'} · ${instance.status}`
  try {
    timeline.value = await workspaceApi.timeline(instance.id)
  } catch (error) {
    showError(error)
  } finally {
    timelineLoading.value = false
  }
}

async function openPolicies() {
  const definition = definitions.value.find((item) => item.versionId === selectedDefinition.value)
  if (!definition) return
  policyVisible.value = true
  try {
    policies.value = await workspaceApi.nodePolicies(definition.versionId)
  } catch (error) {
    showError(error)
  }
}

async function requestProxy(task: WorkflowTaskDto) {
  actionLoading.value = task.id
  try {
    const run = await workspaceApi.requestAgentProxy(task.id)
    await ElMessageBox.confirm(
      '该动作将由独立数字员工服务身份执行，并由你作为真人显式确认。原责任人会收到通知。',
      '确认低风险 Agent 代理',
      { type: 'warning', confirmButtonText: '确认代理', cancelButtonText: '取消' },
    )
    await workspaceApi.confirmAgentProxy(run.id)
    ElMessage.success('数字员工代理已执行并写入 AgentProxy 审计')
    await load()
  } catch (error) {
    if (error !== 'cancel' && error !== 'close') showError(error)
  } finally {
    actionLoading.value = ''
  }
}

async function confirmAgentNode(task: WorkflowTaskDto) {
  if (!task.agentRunId) return
  actionLoading.value = task.id
  try {
    await workspaceApi.confirmAgentProxy(task.agentRunId)
    ElMessage.success('数字员工结果已由真人确认，流程已继续推进')
    await load()
  } catch (error) {
    showError(error)
  } finally {
    actionLoading.value = ''
  }
}

function agentStateLabel(task: WorkflowTaskDto) {
  if (task.assigneeStrategy !== 'Agent') return ''
  if (task.decisionMode === 'AwaitingAgentConfirmation') return '待人工确认'
  if (task.decisionMode === 'AgentProxy') return '已代理完成'
  if (task.decisionMode === 'AgentBlocked') return '已阻断，转人工'
  return '待执行'
}

async function escalate(task: WorkflowTaskDto) {
  const prompt = await ElMessageBox.prompt('请输入升级原因', '升级到备份处理人或部门负责人', {
    inputPattern: /\S+/,
    inputErrorMessage: '必须填写升级原因',
    confirmButtonText: '确认升级',
    cancelButtonText: '取消',
  })
  await workspaceApi.escalateTask(task.id, {
    expectedVersion: task.version,
    reason: prompt.value,
    idempotencyKey: crypto.randomUUID(),
  })
  ElMessage.success('任务已升级，原责任人和转交证据已保留')
  await load()
}

async function transfer(task: WorkflowTaskDto) {
  const prompt = await ElMessageBox.prompt(
    '请输入目标人员 ID 和原因，格式：用户ID,原因',
    '转交流程任务',
    {
      inputPattern: /^[0-9a-f-]{36},.+/i,
      inputErrorMessage: '请输入“用户ID,原因”',
      confirmButtonText: '确认转交',
      cancelButtonText: '取消',
    },
  )
  const [targetUserId, ...reasonParts] = prompt.value.split(',')
  await workspaceApi.transferTask(task.id, {
    targetUserId,
    expectedVersion: task.version,
    reason: reasonParts.join(','),
    idempotencyKey: crypto.randomUUID(),
  })
  ElMessage.success('任务已转交，原责任人和审计记录已保留')
  await load()
}

function instanceForTask(task: WorkflowTaskDto) {
  return instances.value.find((instance) => instance.id === task.workflowInstanceId)
}

function openObject(instance: WorkflowInstanceDto) {
  void router.push({ path: '/objects', query: { focus: instance.businessObjectId } })
}

function openTaskTimeline(task: WorkflowTaskDto) {
  const instance = instanceForTask(task)
  if (instance) {
    void openTimeline(instance)
  }
}

function openTaskObject(task: WorkflowTaskDto) {
  const instance = instanceForTask(task)
  if (instance) {
    openObject(instance)
  }
}

function statusTone(status: string) {
  if (status === 'Completed' || status === 'Approved') return 'success'
  if (status === 'Returned' || status === 'Terminated') return 'danger'
  if (status === 'Pending' || status === 'Waiting') return 'warning'
  return 'info'
}

onMounted(load)
</script>

<template>
  <div class="page-shell">
    <MainColumnPageHeader
      intro-key="workflows"
      title="流程事项"
      scope-label="当前部门与本人待办范围"
      kicker="流程待办 · 审批与进度"
    />

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />

    <section class="section-card launch-panel">
      <div>
        <p class="page-kicker">发起流程</p>
        <h2>选择已发布流程与业务主对象</h2>
      </div>
      <div class="launch-controls">
        <el-select v-model="selectedDefinition" data-testid="workflow-definition-select" placeholder="选择流程">
          <el-option
            v-for="definition in definitions"
            :key="definition.versionId"
            :label="`${definition.name} · V${definition.versionNumber}`"
            :value="definition.versionId"
          />
        </el-select>
        <el-select v-model="selectedObject" data-testid="workflow-object-select" placeholder="选择业务对象" filterable>
          <el-option
            v-for="object in objects"
            :key="object.id"
            :label="`${object.objectCode} · ${object.status}`"
            :value="object.id"
          />
        </el-select>
        <el-button
          type="primary"
          data-testid="start-workflow-button"
          :loading="actionLoading === 'start'"
          @click="startWorkflow"
        >
          发起流程
        </el-button>
        <el-button @click="openPolicies">节点策略</el-button>
      </div>
    </section>

    <section v-loading="loading" class="section-card">
      <el-tabs>
        <el-tab-pane :label="`我的待办 ${scopedTasks.length}`">
          <el-empty v-if="scopedTasks.length === 0" description="当前身份没有待办" />
          <div v-else class="task-list">
            <article
              v-for="task in scopedTasks"
              :key="task.id"
              class="task-card"
              data-testid="workflow-task"
              :data-workflow-instance-id="task.workflowInstanceId"
              :data-node-code="task.nodeCode"
            >
              <div>
                <span class="task-badge">当前待办</span>
                <h3>{{ task.nodeName || '业务任务' }}</h3>
                <p>
                  当前步骤由你处理 · 截止
                  {{ task.dueAt ? new Date(task.dueAt).toLocaleString() : '未设置' }}
                </p>
                <p>
                  责任方式 {{ task.assigneeStrategy === 'SpecificUser' ? '指定人员' : '业务岗位' }}
                  <template v-if="task.businessRoleCode">
                    · 业务角色 {{ task.businessRoleCode }} v{{ task.businessRoleVersion }}
                  </template>
                </p>
                <el-tag
                  v-if="agentStateLabel(task)"
                  size="small"
                  :type="task.decisionMode === 'AgentBlocked' ? 'danger' : task.decisionMode === 'AgentProxy' ? 'success' : 'warning'"
                >
                  {{ agentStateLabel(task) }}
                </el-tag>
                <SlaStatusBadge
                  :due-at="task.dueAt"
                  :status="task.slaStatus"
                  :decision-mode="task.decisionMode"
                />
                <details class="technical-details">
                  <summary>管理技术信息</summary>
                  <small>节点 {{ task.nodeCode }} · 任务 {{ task.id }} · 版本 {{ task.version }}</small>
                </details>
              </div>
              <div class="task-actions">
                <el-button
                  v-if="task.status === 'Pending' && task.decisionMode === 'AwaitingAgentConfirmation'"
                  type="primary"
                  :loading="actionLoading === task.id"
                  @click="confirmAgentNode(task)"
                >
                  确认数字员工结果
                </el-button>
                <el-button
                  v-if="task.status === 'Pending' && task.decisionMode !== 'AwaitingAgentConfirmation'"
                  type="primary"
                  plain
                  @click="openTaskForm(task)"
                >
                  填写表单
                </el-button>
                <el-button
                  v-if="task.status === 'Pending' && task.decisionMode !== 'AwaitingAgentConfirmation'"
                  type="success"
                  data-testid="approve-task-button"
                  :loading="actionLoading === task.id"
                  @click="decide(task, 'Approve')"
                >
                  通过
                </el-button>
                <el-button v-if="task.status === 'Pending' && task.decisionMode !== 'AwaitingAgentConfirmation'" data-testid="return-task-button" @click="decide(task, 'Return')">退回</el-button>
                <el-button v-if="task.status === 'Pending' && task.decisionMode !== 'AwaitingAgentConfirmation'" type="danger" data-testid="terminate-task-button" plain @click="decide(task, 'Terminate')">终止</el-button>
                <el-button
                  v-if="task.status === 'Pending' && task.assigneeStrategy !== 'Agent'"
                  data-testid="agent-proxy-button"
                  :loading="actionLoading === task.id"
                  @click="requestProxy(task)"
                >
                  Agent 辅助/代理
                </el-button>
                <el-button v-if="task.status === 'Pending' && auth.can('workflow-task:update')" @click="escalate(task)">
                  升级
                </el-button>
                <el-button v-if="task.status === 'Pending' && auth.can('workflow-task:update')" text @click="transfer(task)">
                  转交
                </el-button>
                <el-button
                  text
                  type="primary"
                  @click="openTaskTimeline(task)"
                >
                  时间线
                </el-button>
                <el-button
                  text
                  type="primary"
                  @click="openTaskObject(task)"
                >
                  对象
                </el-button>
              </div>
            </article>
          </div>
        </el-tab-pane>

        <el-tab-pane :label="`流程实例 ${instances.length}`">
          <el-table :data="visibleInstances" empty-text="暂无流程实例">
            <el-table-column prop="currentNodeCode" label="当前节点" min-width="150" />
            <el-table-column label="状态" width="110">
              <template #default="{ row }">
                <StatusBadge :label="row.status" :tone="statusTone(row.status)" />
              </template>
            </el-table-column>
            <el-table-column prop="version" label="版本" width="80" />
            <el-table-column label="开始时间" min-width="170">
              <template #default="{ row }">{{ new Date(row.startedAt).toLocaleString() }}</template>
            </el-table-column>
            <el-table-column label="操作" width="100">
              <template #default="{ row }">
                <el-button text type="primary" @click="openTimeline(row)">时间线</el-button>
              </template>
            </el-table-column>
            <el-table-column label="对象" width="80">
              <template #default="{ row }">
                <el-button text type="primary" @click="openObject(row)">查看</el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-tab-pane>

        <el-tab-pane label="状态规则">
          <WorkflowStateGuide />
        </el-tab-pane>
      </el-tabs>
    </section>

    <FeaturePlaceholder
      title="流程运行时接缝"
      description="Elsa、Hangfire 与 Garnet 的实际可用状态以平台能力页和部署检查为准；SQL 阻断期间不声称持久化闭环已经验证。"
      status="SQL · ELSA · OUTBOX"
      tone="success"
    />

    <el-dialog v-model="timelineVisible" :title="`流程时间线 · ${timelineTitle}`" width="min(720px, 92vw)">
      <div v-loading="timelineLoading">
        <el-empty v-if="!timelineLoading && timeline.length === 0" description="暂无时间线记录" />
        <div v-else class="timeline">
          <article v-for="item in timeline" :key="`${item.kind}-${item.id}`">
            <span>{{ item.kind }}</span>
            <strong>{{ item.title }}</strong>
            <p>{{ item.detail || '无附加说明' }}</p>
            <small>{{ new Date(item.occurredAt).toLocaleString() }} · {{ item.actorId }}</small>
          </article>
        </div>
      </div>
    </el-dialog>

    <el-dialog v-model="policyVisible" title="流程节点能力包" width="min(980px, 96vw)">
      <el-table :data="policies" empty-text="该版本尚无节点策略">
        <el-table-column prop="nodeCode" label="节点" min-width="150" />
        <el-table-column prop="versionNumber" label="策略版本" width="90" />
        <el-table-column prop="slaMinutes" label="SLA 分钟" width="100" />
        <el-table-column prop="reminderMinutes" label="提醒分钟" width="100" />
        <el-table-column prop="escalationMinutes" label="升级分钟" width="100" />
        <el-table-column prop="allowAgentAssist" label="Agent 辅助" width="100" />
        <el-table-column prop="allowAgentProxy" label="Agent 代理" width="100" />
        <el-table-column prop="permanentHumanBoundary" label="人工边界" width="100" />
        <el-table-column prop="status" label="状态" width="100" />
      </el-table>
    </el-dialog>

    <el-dialog
      v-model="taskFormVisible"
      :title="activeTask ? `办理 · ${activeTask.nodeName || activeTask.nodeCode}` : '办理任务'"
      width="min(980px, 96vw)"
    >
      <div v-loading="taskFormLoading" class="task-form-dialog">
        <WorkflowFormRenderer
          v-if="activeTaskForm"
          v-model="activeFormData"
          :form="activeTaskForm"
          :departments="taskDepartments"
        />
        <section v-if="activeTaskForm?.aiReview.enabled" class="ai-review-note">
          <strong>AI 评论 / 审查</strong>
          <p>
            当前使用已注册动作“{{ activeTaskForm.aiReview.registeredAction }}”，
            AiGateway 模式为“规则模拟”，仅生成建议或草稿，不代替真人审批。
          </p>
          <el-button
            size="small"
            :loading="aiReviewLoading"
            @click="runAiReview"
          >
            运行规则模拟审查
          </el-button>
          <p v-if="aiReviewResult" class="ai-review-result">
            {{ aiReviewResult.suggestion }}
            <small>
              {{ aiReviewResult.mode }} · 外部模型
              {{ aiReviewResult.externalModelCalled ? '已调用' : '未调用' }}
            </small>
          </p>
        </section>
        <el-form-item label="处理意见">
          <el-input
            v-model="taskComment"
            type="textarea"
            :rows="3"
            placeholder="通过可填写简要意见；退回或终止必须说明原因"
            maxlength="2000"
            show-word-limit
          />
        </el-form-item>
      </div>
      <template #footer>
        <el-button @click="taskFormVisible = false">取消</el-button>
        <el-button
          :loading="activeTask && actionLoading === activeTask.id"
          @click="saveActiveDraft"
        >
          保存草稿
        </el-button>
        <el-button
          type="success"
          :loading="activeTask && actionLoading === activeTask.id"
          @click="submitActiveForm"
        >
          提交当前动作
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.launch-panel {
  display: grid;
  gap: 18px;
}

.launch-controls {
  display: grid;
  grid-template-columns: minmax(0, 1fr) minmax(0, 1fr) auto;
  gap: 10px;
}

.task-list {
  display: grid;
  gap: 12px;
}

.task-card {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  padding: 16px;
}

.task-card code {
  color: var(--aidt-brand);
  font-size: 0.74rem;
}

.task-card h3 {
  margin: 6px 0 0;
  font-size: 1rem;
}

.task-badge {
  color: var(--aidt-brand);
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.task-card p {
  margin: 6px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
}

.task-actions {
  display: flex;
  flex: 0 0 auto;
  gap: 4px;
}

.technical-details {
  margin-top: 8px;
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
}

.technical-details summary {
  cursor: pointer;
}

.task-form-dialog {
  display: grid;
  gap: 16px;
}

.ai-review-note {
  border: 1px solid #f0d9a8;
  border-radius: 12px;
  background: #fffaf0;
  padding: 12px;
}

.ai-review-note p {
  margin: 5px 0 0;
  color: var(--aidt-ink-muted);
  line-height: 1.65;
}

.ai-review-note small {
  display: block;
  margin-top: 4px;
}

.ai-review-result {
  border-top: 1px solid #ecd9ae;
  padding-top: 8px;
  font-weight: 700;
}

.timeline {
  display: grid;
  gap: 10px;
}

.timeline article {
  border-left: 3px solid var(--aidt-brand);
  border-radius: 0 12px 12px 0;
  background: var(--aidt-surface-muted);
  padding: 12px 14px;
}

.timeline span,
.timeline strong,
.timeline small {
  display: block;
}

.timeline span {
  color: var(--aidt-brand);
  font-size: 0.68rem;
}

.timeline strong {
  margin-top: 4px;
}

.timeline p {
  margin: 5px 0;
}

.timeline small {
  color: var(--aidt-ink-muted);
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.68rem;
}

@media (max-width: 760px) {
  .launch-controls {
    grid-template-columns: 1fr;
  }

  .task-card {
    align-items: stretch;
    flex-direction: column;
  }

  .task-actions {
    flex-wrap: wrap;
  }
}
</style>
