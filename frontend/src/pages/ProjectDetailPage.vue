<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ApiError } from '@/api/http'
import { fileApi } from '@/api/modules/fileApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  BusinessObjectDto,
  FileAttachmentDto,
  MinutesSourceDto,
  ProjectCandidateUserDto,
  ProjectDetailDto,
  ProjectDto,
  ProjectMilestoneDto,
  ProjectTaskDto,
  WorkflowInstanceDto,
} from '@/api/types/contracts'
import EnvironmentTag from '@/components/base/EnvironmentTag.vue'
import StatusBadge from '@/components/base/StatusBadge.vue'
import {
  defaultRelationCode,
  evidenceOptionsFor,
  getProjectStatusOptions,
  projectDeadline,
  type ProjectRelationEntityType,
} from '@/features/projects/projectOperationRules'
import { useAuthStore } from '@/stores/auth'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const loading = ref(true)
const errorMessage = ref('')
const detail = ref<ProjectDetailDto | null>(null)
const candidates = ref<ProjectCandidateUserDto[]>([])
const businessObjects = ref<BusinessObjectDto[]>([])
const workflowInstances = ref<WorkflowInstanceDto[]>([])
const files = ref<FileAttachmentDto[]>([])
const minutes = ref<MinutesSourceDto[]>([])
const projectVisible = ref(false)
const memberVisible = ref(false)
const taskVisible = ref(false)
const milestoneVisible = ref(false)
const evidenceVisible = ref(false)
const saving = ref(false)
const selectedMilestone = ref<ProjectMilestoneDto | null>(null)

const memberForm = reactive({
  userId: '',
  projectRole: 'Contributor' as 'Manager' | 'Contributor' | 'Viewer',
})

const projectForm = reactive({
  name: '',
  ownerUserId: '',
  plannedStartDate: '',
  plannedEndDate: '',
  objective: '',
  currentStage: '',
  confidentialityLevel: 'Internal' as ProjectDto['confidentialityLevel'],
})

const taskForm = reactive({
  title: '',
  content: '',
  assigneeId: '',
  dueDate: '',
  priority: 'P1' as ProjectTaskDto['priority'],
})

const milestoneForm = reactive({
  title: '',
  ownerUserId: '',
  plannedDate: '',
  status: 'Planned' as ProjectMilestoneDto['status'],
})

const evidenceForm = reactive({
  entityType: 'Attachment' as ProjectRelationEntityType,
  entityId: '',
  relationCode: 'SupportingEvidence',
  evidenceExcerpt: '',
})

const projectId = computed(() => String(route.params.id ?? ''))
const canManageMembers = computed(() => auth.can('project-member:manage'))
const canManageMilestones = computed(() => auth.can('milestone:manage'))
const canUpdateProject = computed(() => auth.can('project:update'))
const canCreateTask = computed(() => auth.can('project-task:create'))
const project = computed(() => detail.value?.project ?? null)
const activeMembers = computed(
  () => detail.value?.members.filter((item) => item.isActive) ?? [],
)
const statusOptions = computed(() => getProjectStatusOptions(project.value?.status))
const evidenceOptions = computed(() => evidenceOptionsFor(
  evidenceForm.entityType,
  {
    files: files.value,
    minutes: minutes.value,
    workflows: workflowInstances.value,
    objects: businessObjects.value,
  },
  formatDate,
))

function statusTone(status: ProjectDto['status']) {
  if (status === 'Completed') return 'success'
  if (status === 'Blocked' || status === 'Terminated') return 'danger'
  if (status === 'AwaitingAcceptance') return 'warning'
  return 'info'
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleString('zh-CN') : '未设置'
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    detail.value = await workspaceApi.project(projectId.value)
    if (canManageMembers.value || canManageMilestones.value || canUpdateProject.value || canCreateTask.value) {
      const [memberRows, objectPage, instances, filePage, minutePage] = await Promise.all([
        workspaceApi.projectMemberCandidates(projectId.value),
        workspaceApi.businessObjects(),
        workspaceApi.workflowInstances(),
        fileApi.list({ departmentId: detail.value.project.departmentId, pageSize: 100 }),
        workspaceApi.minutes(),
      ])
      candidates.value = memberRows
      businessObjects.value = objectPage.items.filter((item) =>
        item.id !== projectId.value && item.departmentId === detail.value!.project.departmentId)
      workflowInstances.value = instances.items.filter((item) =>
        item.departmentId === detail.value!.project.departmentId)
      files.value = filePage.items
      minutes.value = minutePage.items.filter((item) => item.departmentId === detail.value!.project.departmentId)
    }
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '无法加载项目详情。'
  } finally {
    loading.value = false
  }
}

function openProjectDialog() {
  if (!project.value) return
  projectForm.name = project.value.name
  projectForm.ownerUserId = project.value.ownerUserId
  projectForm.plannedStartDate = project.value.plannedStartDate?.slice(0, 10) ?? ''
  projectForm.plannedEndDate = project.value.plannedEndDate?.slice(0, 10) ?? ''
  projectForm.objective = project.value.objective
  projectForm.currentStage = project.value.currentStage
  projectForm.confidentialityLevel = project.value.confidentialityLevel
  projectVisible.value = true
}

async function saveProject() {
  if (!project.value || !projectForm.name.trim() || !projectForm.objective.trim()
    || !projectForm.currentStage.trim() || !projectForm.ownerUserId) {
    ElMessage.warning('请填写项目名称、目标、阶段和负责人')
    return
  }
  saving.value = true
  try {
    await workspaceApi.updateProject(project.value.id, {
      expectedVersion: project.value.version,
      name: projectForm.name.trim(),
      ownerUserId: projectForm.ownerUserId,
      plannedStartDate: projectForm.plannedStartDate ? `${projectForm.plannedStartDate}T00:00:00+08:00` : null,
      plannedEndDate: projectForm.plannedEndDate ? `${projectForm.plannedEndDate}T23:59:59+08:00` : null,
      objective: projectForm.objective.trim(),
      currentStage: projectForm.currentStage.trim(),
      confidentialityLevel: projectForm.confidentialityLevel,
    })
    ElMessage.success('项目信息与负责人已更新')
    projectVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '更新项目失败')
  } finally {
    saving.value = false
  }
}

function openMemberDialog() {
  memberForm.userId = ''
  memberForm.projectRole = 'Contributor'
  memberVisible.value = true
}

async function addMember() {
  if (!project.value || !memberForm.userId) {
    ElMessage.warning('请选择项目成员')
    return
  }
  saving.value = true
  try {
    await workspaceApi.addProjectMember(
      project.value.id,
      memberForm.userId,
      memberForm.projectRole,
    )
    ElMessage.success('项目成员已添加')
    memberVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '添加成员失败')
  } finally {
    saving.value = false
  }
}

async function removeMember(member: ProjectDetailDto['members'][number]) {
  try {
    await ElMessageBox.confirm(
      `确认将 ${member.displayName} 从项目成员中移除？`,
      '移出项目成员',
      { type: 'warning', confirmButtonText: '移除', cancelButtonText: '取消' },
    )
  } catch {
    return
  }
  try {
    await workspaceApi.removeProjectMember(project.value!.id, member.userId, '项目成员管理')
    ElMessage.success('项目成员已移除')
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '移除成员失败')
  }
}

async function updateMemberRole(
  member: ProjectDetailDto['members'][number],
  role: string,
) {
  const projectRole = role as 'Manager' | 'Contributor' | 'Viewer'
  try {
    await workspaceApi.addProjectMember(project.value!.id, member.userId, projectRole)
    ElMessage.success('成员角色已更新')
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '更新成员角色失败')
    await load()
  }
}

function openTaskDialog() {
  taskForm.title = ''
  taskForm.content = ''
  taskForm.assigneeId = project.value?.ownerUserId ?? ''
  taskForm.dueDate = ''
  taskForm.priority = 'P1'
  taskVisible.value = true
}

async function createTask() {
  if (!project.value || !taskForm.title.trim() || !taskForm.content.trim()
    || !taskForm.assigneeId || !taskForm.dueDate) {
    ElMessage.warning('请填写任务标题、内容、负责人和截止日期')
    return
  }
  saving.value = true
  try {
    await workspaceApi.createProjectTask(project.value.id, {
      assigneeId: taskForm.assigneeId,
      title: taskForm.title.trim(),
      content: taskForm.content.trim(),
      dueAt: projectDeadline(taskForm.dueDate),
      priority: taskForm.priority,
    })
    ElMessage.success('项目任务已创建并分配给负责人')
    taskVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '创建项目任务失败')
  } finally {
    saving.value = false
  }
}

function openMilestoneDialog(milestone?: ProjectMilestoneDto) {
  selectedMilestone.value = milestone ?? null
  milestoneForm.title = milestone?.title ?? ''
  milestoneForm.ownerUserId = milestone?.ownerUserId ?? project.value?.ownerUserId ?? auth.session.userId
  milestoneForm.plannedDate = milestone?.plannedDate.slice(0, 10) ?? ''
  milestoneForm.status = milestone?.status ?? 'Planned'
  milestoneVisible.value = true
}

async function saveMilestone() {
  if (!project.value || !milestoneForm.title.trim() || !milestoneForm.ownerUserId || !milestoneForm.plannedDate) {
    ElMessage.warning('请填写里程碑标题、负责人和计划日期')
    return
  }
  saving.value = true
  try {
    const input = {
      title: milestoneForm.title.trim(),
      ownerUserId: milestoneForm.ownerUserId,
      plannedDate: `${milestoneForm.plannedDate}T18:00:00+08:00`,
      status: milestoneForm.status,
    }
    if (selectedMilestone.value) {
      await workspaceApi.updateMilestone(project.value.id, selectedMilestone.value.id, {
        expectedVersion: selectedMilestone.value.version,
        ...input,
      })
      ElMessage.success('里程碑已更新')
    } else {
      await workspaceApi.addMilestone(project.value.id, input)
      ElMessage.success('里程碑已创建')
    }
    milestoneVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '保存里程碑失败')
  } finally {
    saving.value = false
  }
}

async function completeMilestone(milestone: ProjectMilestoneDto) {
  try {
    await workspaceApi.updateMilestone(project.value!.id, milestone.id, {
      expectedVersion: milestone.version,
      title: milestone.title,
      ownerUserId: milestone.ownerUserId,
      plannedDate: milestone.plannedDate,
      status: 'Completed',
    })
    ElMessage.success('里程碑已完成')
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '完成里程碑失败')
  }
}

async function completeTask(task: ProjectTaskDto) {
  try {
    await workspaceApi.completeProjectTask(task.id, task.version, '项目详情页完成')
    ElMessage.success('项目任务已完成，项目进度已重新计算')
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '完成任务失败')
  }
}

async function changeStatus(status: string) {
  if (!project.value) return
  try {
    await workspaceApi.changeProjectStatus(
      project.value.id,
      project.value.version,
      status as ProjectDto['status'],
    )
    await load()
    ElMessage.success('项目状态已更新')
  } catch (error) {
    await load()
    ElMessage.error(error instanceof ApiError ? error.message : '更新项目状态失败')
  }
}

function openEvidenceDialog() {
  changeEvidenceType('Attachment')
  evidenceForm.evidenceExcerpt = ''
  evidenceVisible.value = true
}

function changeEvidenceType(value: typeof evidenceForm.entityType) {
  evidenceForm.entityType = value
  evidenceForm.entityId = ''
  evidenceForm.relationCode = defaultRelationCode(value)
  evidenceForm.entityId = evidenceOptions.value[0]?.id ?? ''
}

async function addEvidence() {
  if (!project.value || !evidenceForm.entityId) {
    ElMessage.warning('请选择要关联的对象或流程')
    return
  }
  saving.value = true
  try {
    await workspaceApi.addProjectRelation(project.value.id, {
      entityType: evidenceForm.entityType,
      entityId: evidenceForm.entityId,
      relationCode: evidenceForm.relationCode,
      evidenceExcerpt: evidenceForm.evidenceExcerpt || null,
    })
    ElMessage.success('项目证据关系已保存')
    evidenceVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '保存项目关系失败')
  } finally {
    saving.value = false
  }
}

function taskRisk(task: ProjectTaskDto) {
  if (task.status === 'Completed' || task.status === 'Cancelled' || !task.dueAt) return '正常'
  return new Date(task.dueAt).getTime() < Date.now() ? '逾期' : '进行中'
}

onMounted(load)
</script>

<template>
  <div v-loading="loading" class="page-shell" data-testid="project-detail-page">
    <el-alert v-if="errorMessage" type="error" :closable="false" show-icon :title="errorMessage" />

    <template v-if="detail && project">
      <header class="project-hero">
        <div>
          <el-button text @click="router.push('/projects')">← 返回项目工作台</el-button>
          <p class="page-kicker">{{ project.objectCode }} · {{ project.departmentName }}</p>
          <h1 data-testid="project-detail-name">{{ project.name }}</h1>
          <p>{{ project.objective }}</p>
          <div class="hero-tags">
            <StatusBadge :label="project.statusDisplayName" :tone="statusTone(project.status)" />
            <EnvironmentTag :code="project.environmentCode" :is-test-data="project.isTestData" />
            <el-tag effect="plain">阶段：{{ project.currentStage }}</el-tag>
            <el-tag effect="plain" type="warning">保密：{{ project.confidentialityLevel }}</el-tag>
          </div>
        </div>
        <div class="hero-actions">
          <el-button v-if="canUpdateProject" data-testid="edit-project-button" @click="openProjectDialog">编辑项目</el-button>
          <el-select
            v-if="canUpdateProject"
            data-testid="project-status-select"
            :placeholder="statusOptions.length ? '变更状态' : '当前状态不可流转'"
            :disabled="statusOptions.length === 0"
            :model-value="undefined"
            aria-label="变更项目状态"
            @change="changeStatus"
          >
            <el-option v-for="option in statusOptions" :key="option.value" :label="option.label" :value="option.value" />
          </el-select>
          <el-button v-if="canUpdateProject" @click="openEvidenceDialog">关联证据</el-button>
        </div>
      </header>

      <section class="metrics">
        <article><span>项目进度</span><strong>{{ project.progressPercent }}%</strong></article>
        <article><span>成员</span><strong>{{ project.memberCount }}</strong></article>
        <article><span>里程碑</span><strong>{{ project.completedMilestoneCount }}/{{ project.milestoneCount }}</strong></article>
        <article><span>项目任务</span><strong>{{ project.completedTaskCount }}/{{ project.projectTaskCount }}</strong></article>
        <article><span>逾期风险</span><strong>{{ project.overdueMilestoneCount + project.overdueTaskCount }}</strong></article>
      </section>

      <div class="progress-block">
        <el-progress :percentage="project.progressPercent" :stroke-width="12" striped />
        <p>进度由未取消的项目任务与里程碑完成比例自动计算；创建、完成或调整状态后重新读取服务端结果。</p>
      </div>

      <section class="split-grid">
        <article class="section-card">
          <div class="section-head">
            <div>
              <h2>项目成员</h2>
              <p>主负责人和成员角色由后端授权校验。</p>
            </div>
            <el-button v-if="canManageMembers" data-testid="add-member-button" type="primary" plain @click="openMemberDialog">添加成员</el-button>
          </div>
          <div class="member-list">
            <div v-for="member in detail.members" :key="member.userId" class="member-row" data-testid="project-member-row">
              <span class="avatar">{{ member.displayName.slice(0, 1) }}</span>
              <div>
                <strong>{{ member.displayName }}</strong>
                <small>{{ member.departmentName }} · {{ member.projectRole }}</small>
              </div>
              <el-tag v-if="member.isLead" type="success" effect="plain">主负责人</el-tag>
              <el-tag v-else-if="!member.isActive" type="info" effect="plain">已移除</el-tag>
              <template v-else-if="canManageMembers">
                <el-select
                  class="member-role-select"
                  data-testid="project-member-role-select"
                  :model-value="member.projectRole"
                  :aria-label="`设置 ${member.displayName} 的项目角色`"
                  @change="updateMemberRole(member, $event)"
                >
                  <el-option label="项目管理者" value="Manager" />
                  <el-option label="协作成员" value="Contributor" />
                  <el-option label="只读成员" value="Viewer" />
                </el-select>
                <el-button data-testid="remove-project-member-button" text type="danger" @click="removeMember(member)">移除</el-button>
              </template>
            </div>
          </div>
        </article>

        <article class="section-card">
          <div class="section-head">
            <div>
              <h2>里程碑</h2>
              <p>到期未完成自动标记逾期。</p>
            </div>
            <el-button v-if="canManageMilestones" data-testid="add-milestone-button" type="primary" plain @click="openMilestoneDialog()">新增里程碑</el-button>
          </div>
          <div class="milestone-list">
            <div v-for="milestone in detail.milestones" :key="milestone.id" class="milestone-row">
              <div>
                <strong>{{ milestone.title }}</strong>
                <small>{{ milestone.ownerDisplayName }} · {{ formatDate(milestone.plannedDate) }}</small>
              </div>
              <el-tag :type="milestone.isOverdue ? 'danger' : milestone.status === 'Completed' ? 'success' : 'info'">
                {{ milestone.isOverdue ? '已逾期' : milestone.status }}
              </el-tag>
              <el-button
                v-if="canManageMilestones && milestone.status !== 'Completed'"
                text
                type="primary"
                @click="completeMilestone(milestone)"
              >
                完成
              </el-button>
              <el-button v-if="canManageMilestones" text @click="openMilestoneDialog(milestone)">编辑</el-button>
            </div>
            <el-empty v-if="detail.milestones.length === 0" description="暂无里程碑" :image-size="72" />
          </div>
        </article>
      </section>

      <section class="section-card">
        <div class="section-head">
          <div>
            <h2>项目任务</h2>
            <p>可直接创建并分配；事件确认生成的任务使用同一进度规则。</p>
          </div>
          <div class="section-actions">
            <el-button v-if="canCreateTask" data-testid="create-project-task-button" type="primary" plain @click="openTaskDialog">创建项目任务</el-button>
            <el-button @click="router.push('/inbox')">打开事件收件箱</el-button>
          </div>
        </div>
        <div class="task-list">
          <div v-for="task in detail.tasks" :key="task.id" class="task-row" data-testid="project-task-row">
            <div class="priority">{{ task.priority }}</div>
            <div class="task-main">
              <strong>{{ task.title }}</strong>
              <p>{{ task.content }}</p>
              <small>
                {{ task.assigneeDisplayName }} · 截止 {{ formatDate(task.dueAt) }}
                <span v-if="task.eventInboxId"> · 已关联事件来源</span>
                <span v-if="task.workflowInstanceId"> · 已关联流程</span>
              </small>
            </div>
            <el-tag :type="taskRisk(task) === '逾期' ? 'danger' : task.status === 'Completed' ? 'success' : 'info'">
              {{ taskRisk(task) }}
            </el-tag>
            <el-button
              v-if="task.status !== 'Completed' && (task.assigneeId === auth.session.userId || canManageMilestones)"
              text
              type="primary"
              @click="completeTask(task)"
            >
              完成
            </el-button>
          </div>
          <el-empty v-if="detail.tasks.length === 0" description="暂无项目任务" :image-size="72" />
        </div>
      </section>

      <section class="split-grid">
        <article class="section-card">
          <div class="section-head">
            <div>
              <h2>关联流程与 SLA</h2>
              <p>流程实例只引用业务主对象。</p>
            </div>
          </div>
          <div v-for="workflow in detail.relatedWorkflows" :key="workflow.instance.id" class="evidence-row">
            <div>
              <strong>{{ workflow.definitionName || workflow.definitionCode }}</strong>
              <small>
                {{ workflow.instance.status }} · 当前节点 {{ workflow.instance.currentNodeCode || '—' }}
              </small>
            </div>
            <el-tag :type="workflow.slaRisk === 'Overdue' ? 'danger' : workflow.slaRisk === 'AtRisk' ? 'warning' : 'info'">
              {{ workflow.slaRisk }}
            </el-tag>
          </div>
          <el-empty v-if="!detail.relatedWorkflows.length" description="尚未关联流程" :image-size="72" />
        </article>

        <article class="section-card">
          <div class="section-head">
            <div>
              <h2>文件、听记与对象</h2>
              <p>关联证据用于反查来源，不复制原文到项目表。</p>
            </div>
          </div>
          <div class="evidence-row" v-for="item in detail.relatedBusinessObjects" :key="item.id">
            <div><strong>{{ item.title || item.objectCode }}</strong><small>业务对象 · {{ item.objectCode }}</small></div>
          </div>
          <div class="evidence-row" v-for="item in detail.files" :key="item.id">
            <div><strong>{{ item.originalFileName }}</strong><small>文件版本 v{{ item.currentVersionNumber }}</small></div>
          </div>
          <div class="evidence-row" v-for="item in detail.minutes" :key="item.id">
            <div><strong>{{ item.title }}</strong><small>听记 · {{ formatDate(item.meetingAt) }}</small></div>
          </div>
          <el-empty
            v-if="!detail.relatedBusinessObjects.length && !detail.files.length && !detail.minutes.length"
            description="暂无对象、文件或听记证据"
            :image-size="72"
          />
        </article>
      </section>

      <section class="section-card">
        <div class="section-head">
          <div>
            <h2>项目证据时间线</h2>
            <p>项目、成员、里程碑、事件和任务动作均写入追加式审计。</p>
          </div>
        </div>
        <el-timeline>
          <el-timeline-item
            v-for="audit in detail.auditTimeline"
            :key="audit.id"
            :timestamp="formatDate(audit.occurredAt)"
            placement="top"
          >
            <strong>{{ audit.action }}</strong>
            <p>{{ audit.reason || audit.entityType }}</p>
          </el-timeline-item>
        </el-timeline>
        <el-empty v-if="!detail.auditTimeline.length" description="暂无审计记录" :image-size="72" />
      </section>
    </template>

    <el-dialog v-model="projectVisible" title="编辑项目" width="min(640px, 92vw)">
      <el-form label-position="top">
        <el-form-item label="项目名称">
          <el-input v-model="projectForm.name" data-testid="project-name-input" />
        </el-form-item>
        <el-form-item label="项目负责人">
          <el-select v-model="projectForm.ownerUserId" filterable style="width: 100%" data-testid="project-owner-select">
            <el-option
              v-for="candidate in candidates"
              :key="candidate.id"
              :label="`${candidate.displayName} · ${candidate.departmentName}`"
              :value="candidate.id"
            />
          </el-select>
        </el-form-item>
        <div class="form-grid">
          <el-form-item label="计划开始">
            <el-date-picker v-model="projectForm.plannedStartDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
          </el-form-item>
          <el-form-item label="计划结束">
            <el-date-picker v-model="projectForm.plannedEndDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
          </el-form-item>
        </div>
        <el-form-item label="项目目标">
          <el-input v-model="projectForm.objective" type="textarea" :rows="3" data-testid="project-objective-input" />
        </el-form-item>
        <div class="form-grid">
          <el-form-item label="当前阶段">
            <el-input v-model="projectForm.currentStage" data-testid="project-stage-input" />
          </el-form-item>
          <el-form-item label="保密等级">
            <el-select v-model="projectForm.confidentialityLevel" style="width: 100%">
              <el-option label="内部" value="Internal" />
              <el-option label="敏感" value="Sensitive" />
              <el-option label="核心" value="Core" />
            </el-select>
          </el-form-item>
        </div>
      </el-form>
      <template #footer>
        <el-button @click="projectVisible = false">取消</el-button>
        <el-button data-testid="save-project-button" type="primary" :loading="saving" @click="saveProject">保存项目</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="memberVisible" title="添加项目成员" width="min(480px, 92vw)">
      <el-form label-position="top">
        <el-form-item label="成员">
          <el-select v-model="memberForm.userId" style="width: 100%">
            <el-option
              v-for="candidate in candidates"
              :key="candidate.id"
              :label="`${candidate.displayName} · ${candidate.departmentName}`"
              :value="candidate.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="项目角色">
          <el-select v-model="memberForm.projectRole" style="width: 100%">
            <el-option label="项目管理者" value="Manager" />
            <el-option label="协作成员" value="Contributor" />
            <el-option label="只读成员" value="Viewer" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="memberVisible = false">取消</el-button>
        <el-button data-testid="submit-member-button" type="primary" :loading="saving" @click="addMember">添加成员</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="taskVisible" title="创建项目任务" width="min(560px, 92vw)">
      <el-form label-position="top">
        <el-form-item label="任务标题">
          <el-input v-model="taskForm.title" data-testid="project-task-title-input" />
        </el-form-item>
        <el-form-item label="任务内容">
          <el-input v-model="taskForm.content" type="textarea" :rows="3" data-testid="project-task-content-input" />
        </el-form-item>
        <el-form-item label="任务负责人">
          <el-select v-model="taskForm.assigneeId" style="width: 100%" data-testid="project-task-assignee-select">
            <el-option
              v-for="member in activeMembers"
              :key="member.userId"
              :label="`${member.displayName} · ${member.projectRole}`"
              :value="member.userId"
            />
          </el-select>
        </el-form-item>
        <div class="form-grid">
          <el-form-item label="截止日期">
            <el-date-picker v-model="taskForm.dueDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" data-testid="project-task-due-input" />
          </el-form-item>
          <el-form-item label="优先级">
            <el-select v-model="taskForm.priority" style="width: 100%">
              <el-option label="P0" value="P0" />
              <el-option label="P1" value="P1" />
              <el-option label="P2" value="P2" />
            </el-select>
          </el-form-item>
        </div>
      </el-form>
      <template #footer>
        <el-button @click="taskVisible = false">取消</el-button>
        <el-button data-testid="submit-project-task-button" type="primary" :loading="saving" @click="createTask">创建任务</el-button>
      </template>
    </el-dialog>

    <el-dialog
      v-model="milestoneVisible"
      :title="selectedMilestone ? '编辑里程碑' : '新增里程碑'"
      width="min(520px, 92vw)"
    >
      <el-form label-position="top">
        <el-form-item label="里程碑标题">
          <el-input v-model="milestoneForm.title" data-testid="milestone-title-input" />
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="milestoneForm.ownerUserId" style="width: 100%">
            <el-option
              v-for="member in activeMembers"
              :key="member.userId"
              :label="member.displayName"
              :value="member.userId"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="计划日期">
          <el-date-picker v-model="milestoneForm.plannedDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
        </el-form-item>
        <el-form-item label="状态">
          <el-select v-model="milestoneForm.status" style="width: 100%">
            <el-option label="计划中" value="Planned" />
            <el-option label="进行中" value="InProgress" />
            <el-option label="风险" value="AtRisk" />
            <el-option label="已完成" value="Completed" />
            <el-option label="已取消" value="Cancelled" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="milestoneVisible = false">取消</el-button>
        <el-button data-testid="submit-milestone-button" type="primary" :loading="saving" @click="saveMilestone">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="evidenceVisible" title="关联项目证据" width="min(560px, 92vw)">
      <el-form label-position="top">
        <el-form-item label="证据类型">
          <el-radio-group :model-value="evidenceForm.entityType" @change="changeEvidenceType">
            <el-radio-button value="Attachment">文件</el-radio-button>
            <el-radio-button value="MinutesSource">听记</el-radio-button>
            <el-radio-button value="WorkflowInstance">流程</el-radio-button>
            <el-radio-button value="BusinessObject">业务对象</el-radio-button>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="关联证据">
          <el-select v-model="evidenceForm.entityId" filterable style="width: 100%" data-testid="project-evidence-select">
            <el-option
              v-for="item in evidenceOptions"
              :key="item.id"
              :label="item.label"
              :value="item.id"
            >
              <span>{{ item.label }}</span>
              <small class="option-detail">{{ item.detail }}</small>
            </el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="关系编码">
          <el-input v-model="evidenceForm.relationCode" readonly />
        </el-form-item>
        <el-form-item label="证据摘要">
          <el-input v-model="evidenceForm.evidenceExcerpt" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="evidenceVisible = false">取消</el-button>
        <el-button data-testid="submit-project-evidence" type="primary" :loading="saving" @click="addEvidence">保存关联</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.project-hero,
.section-head,
.hero-actions,
.hero-tags,
.section-actions,
.member-row,
.milestone-row,
.task-row,
.evidence-row {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.project-hero,
.section-head,
.milestone-row,
.evidence-row {
  justify-content: space-between;
}

.project-hero {
  border: 1px solid var(--aidt-border);
  border-radius: 24px;
  background:
    radial-gradient(circle at 90% 10%, rgb(195 228 212 / 65%), transparent 18rem),
    #fff;
  padding: clamp(22px, 4vw, 38px);
}

.project-hero h1 {
  margin: 8px 0;
  font-size: clamp(1.75rem, 5vw, 2.8rem);
}

.project-hero p {
  max-width: 760px;
  color: var(--aidt-ink-muted);
  line-height: 1.75;
}

.hero-tags,
.hero-actions,
.section-actions {
  align-items: center;
  flex-wrap: wrap;
}

.progress-block p {
  margin: 8px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.metrics {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 12px;
}

.metrics article {
  border: 1px solid var(--aidt-border);
  border-radius: 16px;
  background: #fff;
  padding: 15px;
}

.metrics span,
.metrics strong {
  display: block;
}

.metrics span {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.metrics strong {
  margin-top: 7px;
  color: var(--aidt-brand-strong);
  font-size: 1.55rem;
}

.split-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.member-list,
.milestone-list,
.task-list {
  display: grid;
  gap: 10px;
}

.member-row,
.milestone-row,
.task-row,
.evidence-row {
  align-items: center;
  border-top: 1px solid var(--aidt-border);
  padding: 11px 0;
}

.member-row > div,
.milestone-row > div,
.task-main,
.evidence-row > div {
  min-width: 0;
  flex: 1;
}

.member-row strong,
.member-row small,
.milestone-row strong,
.milestone-row small,
.evidence-row strong,
.evidence-row small {
  display: block;
}

.member-row small,
.milestone-row small,
.task-main small,
.evidence-row small,
.task-main p,
.section-head p {
  color: var(--aidt-ink-muted);
}

.avatar {
  display: grid;
  width: 36px;
  height: 36px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 12px;
  background: #e4f2e9;
  color: var(--aidt-brand-strong);
  font-weight: 800;
}

.member-role-select {
  width: 118px;
  flex: 0 0 auto;
}

.option-detail {
  float: right;
  margin-left: 16px;
  color: var(--aidt-ink-muted);
}

.priority {
  border-radius: 9px;
  background: #fff0df;
  color: #9a5d08;
  padding: 6px 8px;
  font-size: 0.72rem;
  font-weight: 800;
}

.task-main p {
  display: -webkit-box;
  overflow: hidden;
  margin: 5px 0;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

@media (max-width: 980px) {
  .metrics {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .project-hero,
  .section-head,
  .milestone-row,
  .task-row,
  .member-row {
    align-items: stretch;
    flex-direction: column;
  }

  .split-grid,
  .metrics,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .member-role-select,
  .hero-actions :deep(.el-select),
  .hero-actions :deep(.el-button) {
    width: 100%;
  }
}
</style>
