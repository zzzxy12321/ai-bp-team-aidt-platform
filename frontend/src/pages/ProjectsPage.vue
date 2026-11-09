<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type { DepartmentDto, ProjectDto } from '@/api/types/contracts'
import EnvironmentTag from '@/components/base/EnvironmentTag.vue'
import StatusBadge from '@/components/base/StatusBadge.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const loading = ref(true)
const errorMessage = ref('')
const projects = ref<ProjectDto[]>([])
const departments = ref<DepartmentDto[]>([])
const pendingEventCount = ref(0)
const createVisible = ref(false)
const createLoading = ref(false)
const includeTestData = ref(false)

const form = reactive({
  objectCode: '',
  name: '',
  departmentId: '',
  plannedStartDate: '',
  plannedEndDate: '',
  objective: '',
  currentStage: '需求确认',
  confidentialityLevel: 'Internal' as 'Internal' | 'Sensitive' | 'Core',
})

const canCreate = computed(() => auth.can('project:create'))
const activeCount = computed(
  () => projects.value.filter((item) => ['Active', 'Blocked', 'AwaitingAcceptance'].includes(item.status)).length,
)
const overdueCount = computed(
  () => projects.value.reduce((sum, item) => sum + item.overdueMilestoneCount + item.overdueTaskCount, 0),
)
const averageProgress = computed(() => {
  if (!projects.value.length) return 0
  return Math.round(
    projects.value.reduce((sum, item) => sum + item.progressPercent, 0) / projects.value.length,
  )
})

function statusTone(status: ProjectDto['status']) {
  if (status === 'Completed') return 'success'
  if (status === 'Blocked' || status === 'Terminated') return 'danger'
  if (status === 'AwaitingAcceptance') return 'warning'
  return 'info'
}

function formatDate(value: string | null) {
  return value ? new Date(value).toLocaleDateString('zh-CN') : '未设置'
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [projectPage, departmentRows, inboxPage] = await Promise.all([
      workspaceApi.projects(includeTestData.value),
      workspaceApi.departments(),
      workspaceApi.inbox(includeTestData.value, 'AwaitingConfirmation'),
    ])
    projects.value = projectPage.items
    departments.value = departmentRows
    pendingEventCount.value = inboxPage.total
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? error.message : '无法加载项目工作台。'
  } finally {
    loading.value = false
  }
}

function openCreate() {
  const primary = departments.value.find((item) => item.isPrimary) ?? departments.value[0]
  form.objectCode = `PRJ-${new Date().toISOString().slice(0, 10).replaceAll('-', '')}-${String(Date.now()).slice(-4)}`
  form.name = ''
  form.departmentId = primary?.id ?? ''
  form.plannedStartDate = new Date().toISOString().slice(0, 10)
  form.plannedEndDate = ''
  form.objective = ''
  form.currentStage = '需求确认'
  form.confidentialityLevel = 'Internal'
  createVisible.value = true
}

async function createProject() {
  if (!form.objectCode.trim() || !form.name.trim() || !form.departmentId || !form.objective.trim()) {
    ElMessage.warning('请填写项目编号、名称、部门和项目目标')
    return
  }
  createLoading.value = true
  try {
    const project = await workspaceApi.createProject({
      objectCode: form.objectCode.trim().toUpperCase(),
      name: form.name.trim(),
      ownerUserId: auth.session.userId,
      departmentId: form.departmentId,
      plannedStartDate: form.plannedStartDate ? `${form.plannedStartDate}T00:00:00+08:00` : null,
      plannedEndDate: form.plannedEndDate ? `${form.plannedEndDate}T18:00:00+08:00` : null,
      objective: form.objective.trim(),
      currentStage: form.currentStage,
      confidentialityLevel: form.confidentialityLevel,
      initialStatus: 'Active',
    })
    createVisible.value = false
    ElMessage.success('项目已创建，业务主对象与 PMO 档案已同步持久化')
    await load()
    await router.push(`/projects/${project.id}`)
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '项目创建失败')
  } finally {
    createLoading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-shell" data-testid="projects-page">
    <MainColumnPageHeader
      intro-key="projects"
      scope-label="当前身份可见项目与部门范围"
      kicker="项目与 PMO"
    >
      <template #actions>
        <div class="page-actions">
          <label v-if="auth.isPlatformAdmin" class="admin-filter">
            <el-switch v-model="includeTestData" @change="load" />
            跨环境
          </label>
          <el-button v-if="canCreate" data-testid="create-project-button" type="primary" :disabled="loading" @click="openCreate">新建项目</el-button>
          <el-button @click="router.push('/inbox')">事件收件箱</el-button>
        </div>
      </template>
    </MainColumnPageHeader>

    <el-alert v-if="errorMessage" type="error" :closable="false" show-icon :title="errorMessage" />

    <section class="metrics" aria-label="项目摘要">
      <article><span>可见项目</span><strong>{{ projects.length }}</strong></article>
      <article><span>进行中</span><strong>{{ activeCount }}</strong></article>
      <article><span>平均进度</span><strong>{{ averageProgress }}%</strong></article>
      <article><span>逾期风险</span><strong>{{ overdueCount }}</strong></article>
      <article><span>待确认事件</span><strong>{{ pendingEventCount }}</strong></article>
    </section>

    <el-alert
      v-if="pendingEventCount"
      type="warning"
      :closable="false"
      show-icon
      :title="`有 ${pendingEventCount} 个事件等待真人确认，Agent 未自动派发任务或流程。`"
    />

    <section v-loading="loading" class="project-grid">
      <article
        v-for="project in projects"
        :key="project.id"
        class="project-card"
        tabindex="0"
        @click="router.push(`/projects/${project.id}`)"
        @keydown.enter="router.push(`/projects/${project.id}`)"
      >
        <div class="card-top">
          <div>
            <span class="object-code">{{ project.objectCode }}</span>
            <h2>{{ project.name }}</h2>
          </div>
          <StatusBadge :label="project.statusDisplayName" :tone="statusTone(project.status)" />
        </div>
        <p>{{ project.objective }}</p>
        <div class="card-meta">
          <span>负责人 {{ project.ownerDisplayName }}</span>
          <EnvironmentTag :code="project.environmentCode" :is-test-data="project.isTestData" />
        </div>
        <el-progress :percentage="project.progressPercent" :stroke-width="8" />
        <div class="project-facts">
          <span>阶段 {{ project.currentStage }}</span>
          <span>计划 {{ formatDate(project.plannedEndDate) }}</span>
          <span>{{ project.memberCount }} 人</span>
          <span>{{ project.completedMilestoneCount }}/{{ project.milestoneCount }} 里程碑</span>
        </div>
        <div v-if="project.overdueMilestoneCount || project.overdueTaskCount" class="risk-line">
          风险：{{ project.overdueMilestoneCount }} 个逾期里程碑，{{ project.overdueTaskCount }} 个逾期任务
        </div>
      </article>
      <el-empty v-if="!loading && projects.length === 0" description="当前身份暂无可见项目" />
    </section>

    <el-dialog v-model="createVisible" title="新建项目" width="min(680px, 92vw)">
      <el-form label-position="top">
        <div class="form-grid">
          <el-form-item label="项目编号">
            <el-input v-model="form.objectCode" />
          </el-form-item>
          <el-form-item label="项目名称">
            <el-input v-model="form.name" data-testid="project-name-input" placeholder="例如：耐候型苯丙乳液客户开发" />
          </el-form-item>
          <el-form-item label="所属部门">
            <el-select v-model="form.departmentId" style="width: 100%">
              <el-option
                v-for="department in departments"
                :key="department.id"
                :label="department.name"
                :value="department.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="当前阶段">
            <el-input v-model="form.currentStage" />
          </el-form-item>
          <el-form-item label="计划开始">
            <el-date-picker v-model="form.plannedStartDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
          </el-form-item>
          <el-form-item label="计划结束">
            <el-date-picker v-model="form.plannedEndDate" type="date" value-format="YYYY-MM-DD" style="width: 100%" />
          </el-form-item>
          <el-form-item label="保密等级">
            <el-select v-model="form.confidentialityLevel" style="width: 100%">
              <el-option label="内部" value="Internal" />
              <el-option label="敏感" value="Sensitive" />
              <el-option label="核心" value="Core" />
            </el-select>
          </el-form-item>
        </div>
          <el-form-item label="项目目标">
          <el-input v-model="form.objective" data-testid="project-objective-input" type="textarea" :rows="4" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button data-testid="submit-project-button" type="primary" :loading="createLoading" @click="createProject">创建项目</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.page-head,
.page-actions,
.card-top,
.card-meta,
.project-facts {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.page-head,
.card-top,
.card-meta {
  justify-content: space-between;
}

.page-actions {
  align-items: center;
  flex-wrap: wrap;
  justify-content: flex-end;
}

.admin-filter {
  display: flex;
  align-items: center;
  gap: 8px;
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
  padding: 16px;
}

.metrics span,
.metrics strong {
  display: block;
}

.metrics span {
  color: var(--aidt-ink-muted);
  font-size: 0.74rem;
}

.metrics strong {
  margin-top: 8px;
  color: var(--aidt-brand-strong);
  font-size: 1.75rem;
}

.project-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

.project-card {
  min-width: 0;
  cursor: pointer;
  border: 1px solid var(--aidt-border);
  border-radius: 20px;
  background: #fff;
  box-shadow: 0 14px 36px rgb(19 61 45 / 6%);
  padding: 20px;
}

.project-card:focus-visible {
  outline: 3px solid rgb(33 111 79 / 30%);
  outline-offset: 3px;
}

.project-card h2 {
  margin: 5px 0 0;
  font-size: 1.2rem;
}

.project-card > p {
  display: -webkit-box;
  overflow: hidden;
  min-height: 3em;
  color: var(--aidt-ink-muted);
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.object-code {
  color: var(--aidt-brand-strong);
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.72rem;
}

.card-meta {
  align-items: center;
  margin: 12px 0;
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.project-facts {
  flex-wrap: wrap;
  margin-top: 12px;
  color: var(--aidt-ink-muted);
  font-size: 0.74rem;
}

.risk-line {
  margin-top: 13px;
  border-radius: 10px;
  background: #fff4df;
  color: #8b5706;
  padding: 9px 11px;
  font-size: 0.76rem;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 14px;
}

@media (max-width: 980px) {
  .metrics {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 760px) {
  .page-head {
    align-items: stretch;
    flex-direction: column;
  }

  .page-actions {
    justify-content: flex-start;
  }

  .project-grid,
  .form-grid {
    grid-template-columns: 1fr;
  }

  .metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
