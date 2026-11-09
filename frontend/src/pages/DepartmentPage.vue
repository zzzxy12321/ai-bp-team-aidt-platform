<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { scenarioApi } from '@/api/modules/scenarioApi'
import { workbenchApi } from '@/api/modules/workbenchApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  DepartmentDto,
  DepartmentWorkbenchDto,
  DepartmentWorkspaceDto,
  Scenario,
  WorkbenchCardItemDto,
  WorkbenchResolvedCardDto,
  WorkItemDto,
} from '@/api/types/contracts'
import EnvironmentTag from '@/components/base/EnvironmentTag.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import WorkItemCard from '@/features/workflow/components/WorkItemCard.vue'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const departments = ref<DepartmentDto[]>([])
const selectedDepartmentId = ref('')
const workbench = ref<DepartmentWorkbenchDto | null>(null)
const fallbackWorkspace = ref<DepartmentWorkspaceDto | null>(null)
const loading = ref(true)
const cardLoading = ref(false)
const errorMessage = ref('')
const fallbackMessage = ref('')
const includeTestData = ref(false)
const scenarios = ref<Scenario[]>([])

const selectedDepartment = computed(
  () => departments.value.find((item) => item.id === selectedDepartmentId.value) ?? null,
)
const pageTitle = computed(
  () => workbench.value?.pageTitle ?? selectedDepartment.value?.name ?? '部门协同空间',
)
const roleLabel = computed(() => {
  const roles = workbench.value?.roleNames ?? []
  return roles.length ? roles.join(' · ') : workbench.value?.audienceTitle ?? '当前岗位'
})
const departmentScenarios = computed(() => {
  const departmentCode = selectedDepartment.value?.code
  if (!departmentCode) return []
  return scenarios.value.filter((scenario) =>
    scenario.currentPublishedVersion?.payload.applicableDepartmentCodes.includes(departmentCode))
})

async function loadDepartments() {
  departments.value = await workspaceApi.departments()
  const preferred = departments.value.find((item) => item.isPrimary) ?? departments.value[0]
  selectedDepartmentId.value = preferred?.id ?? ''
}

async function loadWorkbench() {
  workbench.value = null
  fallbackWorkspace.value = null
  fallbackMessage.value = ''
  errorMessage.value = ''
  if (!selectedDepartmentId.value) {
    loading.value = false
    return
  }

  cardLoading.value = true
  try {
    const resolved = await workbenchApi.resolve(
      selectedDepartmentId.value,
      includeTestData.value,
    )
    workbench.value = resolved
    if (!resolved.matched) {
      fallbackMessage.value = resolved.fallbackReason ?? '没有匹配的岗位模板，已回退通用部门工作区。'
      await loadGenericWorkspace()
    }
  } catch (error) {
    fallbackMessage.value =
      error instanceof ApiError ? error.message : '岗位模板加载失败，已回退通用部门工作区。'
    await loadGenericWorkspace()
  } finally {
    cardLoading.value = false
    loading.value = false
  }
}

async function loadGenericWorkspace() {
  try {
    fallbackWorkspace.value = await workspaceApi.departmentWorkspace(
      selectedDepartmentId.value,
      includeTestData.value,
    )
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError ? error.message : '无法加载部门工作区。'
  }
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    await loadDepartments()
    if (auth.can('scenario:read')) {
      scenarios.value = (await scenarioApi.listVisible()).items
    }
    await loadWorkbench()
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError ? error.message : '无法读取部门列表。'
    loading.value = false
  }
}

function openCardItem(item: WorkbenchCardItemDto) {
  if (!item.detailPath.startsWith('/')) return
  void router.push(item.detailPath)
}

function openWorkItem(item: WorkItemDto) {
  void router.push({
    name: 'workflows',
    query: item.taskId ? { task: item.taskId } : { instance: item.workflowInstanceId ?? '' },
  })
}

function cardClass(card: WorkbenchResolvedCardDto) {
  return {
    'card-third': card.width === 'Third',
    'card-half': card.width === 'Half',
    'card-full': card.width === 'Full',
    'is-unavailable': card.state === 'NotConnected' || card.state === 'Denied',
  }
}

onMounted(load)
</script>

<template>
  <div
    class="page-shell"
    data-testid="department-page"
    :data-template-code="workbench?.templateCode ?? 'FALLBACK'"
  >
    <MainColumnPageHeader
      intro-key="department"
      :title="pageTitle"
      :scope-label="`${selectedDepartment?.name ?? '当前部门'} · ${roleLabel}`"
      :kicker="workbench?.audienceTitle || '部门工作区'"
    >
      <template #actions>
        <div class="filters">
          <el-tag v-if="workbench?.matched" effect="plain" type="success">
            {{ workbench.templateName }} · v{{ workbench.templateVersionNumber }}
          </el-tag>
          <el-select
            v-model="selectedDepartmentId"
            aria-label="选择部门"
            placeholder="选择部门"
            @change="loadWorkbench"
          >
            <el-option
              v-for="department in departments"
              :key="department.id"
              :label="`${department.name}${department.isPrimary ? '（主部门）' : ''}`"
              :value="department.id"
            />
          </el-select>
          <label v-if="auth.isPlatformAdmin" class="admin-filter">
            <el-switch v-model="includeTestData" @change="loadWorkbench" />
            跨环境
          </label>
        </div>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />
    <el-alert
      v-else-if="fallbackMessage"
      type="warning"
      :closable="false"
      show-icon
      title="已回退通用部门工作区"
      :description="fallbackMessage"
    />

    <section v-if="auth.can('scenario:read')" class="section-card scenario-entry">
      <div class="section-head">
        <div>
          <h2>业务场景</h2>
          <p>只展示当前岗位有权启动的场景；点击进入统一的岗位业务场景中心。</p>
        </div>
        <el-button type="primary" @click="router.push('/scenarios')">查看全部场景</el-button>
      </div>
      <div v-if="departmentScenarios.length" class="scenario-entry-grid">
        <button
          v-for="scenario in departmentScenarios.slice(0, 3)"
          :key="scenario.id"
          @click="router.push({ path: '/scenarios', query: { scenario: scenario.id } })"
        >
          <strong>{{ scenario.name }}</strong>
          <span>{{ scenario.description }}</span>
          <small>{{ scenario.currentPublishedVersion?.payload.participantRoles.join(' · ') }}</small>
        </button>
      </div>
      <el-empty
        v-else
        :image-size="50"
        description="当前部门暂无可启动场景，可进入场景中心查看其他获授权业务目标。"
      >
        <el-button @click="router.push('/scenarios')">打开场景中心</el-button>
      </el-empty>
    </section>

    <section
      v-if="workbench?.matched"
      v-loading="cardLoading"
      class="workbench-grid"
      aria-label="岗位工作台卡片"
    >
      <article
        v-for="card in workbench.cards"
        :key="card.id"
        class="workbench-card"
        :class="cardClass(card)"
        :data-card-type="card.cardType"
        :data-card-state="card.state"
      >
        <header class="card-heading">
          <div>
            <p class="card-eyebrow">{{ card.cardType }}</p>
            <h2>{{ card.title }}</h2>
            <p v-if="card.description">{{ card.description }}</p>
          </div>
          <span class="card-count">{{ card.totalCount }}</span>
        </header>

        <div v-if="card.state === 'NotConnected'" class="card-state unavailable">
          <strong>能力尚未接入</strong>
          <span>{{ card.message }}</span>
        </div>
        <div v-else-if="card.state === 'Denied'" class="card-state unavailable">
          <strong>没有权限</strong>
          <span>{{ card.message }}</span>
        </div>
        <div v-else-if="card.state === 'Error'" class="card-state error">
          <strong>加载失败</strong>
          <span>{{ card.message }}</span>
        </div>
        <el-empty
          v-else-if="!card.items.length"
          :image-size="56"
          :description="card.message"
        />
        <div v-else class="card-items">
          <button
            v-for="item in card.items"
            :key="item.id"
            class="card-item"
            type="button"
            @click="openCardItem(item)"
          >
            <span class="item-main">
              <strong>{{ item.businessTitle }}</strong>
              <small>{{ item.contextTitle }}</small>
            </span>
            <span class="item-meta">
              <el-tag
                size="small"
                effect="plain"
                :type="item.riskLevel === 'High' ? 'danger' : item.riskLevel === 'Medium' ? 'warning' : 'info'"
              >
                {{ item.status }}
              </el-tag>
              <small v-if="item.dueAt">{{ new Date(item.dueAt).toLocaleDateString('zh-CN') }}</small>
            </span>
          </button>
        </div>
      </article>
      <el-empty
        v-if="!workbench.cards.length"
        class="empty-workbench"
        description="当前岗位没有可见卡片，请联系部门负责人检查模板配置。"
      />
    </section>

    <template v-else-if="fallbackWorkspace">
      <section class="metrics" aria-label="部门摘要">
        <article><span>待处理</span><strong>{{ fallbackWorkspace.pendingTaskCount }}</strong></article>
        <article><span>超期关注</span><strong>{{ fallbackWorkspace.overdueTaskCount }}</strong></article>
        <article><span>运行中流程</span><strong>{{ fallbackWorkspace.runningWorkflowCount }}</strong></article>
        <article><span>近期文件</span><strong>{{ fallbackWorkspace.recentFileCount }}</strong></article>
      </section>

      <section class="section-card">
        <div class="section-head">
          <div>
            <h2>部门待办</h2>
            <p>仅显示当前身份有权访问的部门任务。</p>
          </div>
          <el-button type="primary" plain @click="router.push('/documents')">上传部门文件</el-button>
        </div>
        <div v-if="fallbackWorkspace.pendingTasks.length" class="fallback-work-grid">
          <WorkItemCard
            v-for="item in fallbackWorkspace.pendingTasks"
            :key="item.id"
            :item="item"
            @open="openWorkItem"
          />
        </div>
        <el-empty v-else-if="!loading" description="部门暂无待处理事项" />
      </section>

      <section class="split-grid">
        <article class="section-card">
          <div class="section-head">
            <h2>近期业务事项</h2>
            <el-button text type="primary" @click="router.push('/objects')">查看全部</el-button>
          </div>
          <div v-if="fallbackWorkspace.recentObjects.length" class="simple-list">
            <div v-for="item in fallbackWorkspace.recentObjects" :key="item.id" class="simple-row">
              <span>{{ item.objectCode }}</span>
              <EnvironmentTag :code="item.environmentCode" :is-test-data="item.isTestData" />
              <small>{{ item.status }}</small>
            </div>
          </div>
          <el-empty v-else description="暂无业务事项" />
        </article>
        <article class="section-card">
          <div class="section-head">
            <h2>近期文件</h2>
            <el-button text type="primary" @click="router.push('/documents')">文件中心</el-button>
          </div>
          <div v-if="fallbackWorkspace.recentFiles.length" class="simple-list">
            <div v-for="item in fallbackWorkspace.recentFiles" :key="item.id" class="simple-row">
              <span>{{ item.originalFileName }}</span>
              <EnvironmentTag :code="item.environmentCode" :is-test-data="item.isTestData" />
              <small>v{{ item.currentVersionNumber }}</small>
            </div>
          </div>
          <el-empty v-else description="暂无部门文件" />
        </article>
      </section>
    </template>

    <el-empty v-else-if="!loading" description="没有可访问的部门工作区。" />
  </div>
</template>

<style scoped>
.page-head,
.section-head,
.filters,
.admin-filter,
.context-line,
.card-heading,
.card-item,
.item-meta,
.simple-row {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-head,
.section-head {
  justify-content: space-between;
  align-items: flex-start;
}

.page-copy {
  min-width: 0;
}

.context-line {
  flex-wrap: wrap;
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.filters {
  flex: 0 0 auto;
  flex-wrap: wrap;
  justify-content: flex-end;
}

.filters :deep(.el-select) {
  width: 210px;
}

.admin-filter {
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.workbench-grid {
  display: grid;
  grid-template-columns: repeat(6, minmax(0, 1fr));
  gap: 14px;
  min-height: 180px;
}

.workbench-card {
  min-width: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 18px;
  background: #fff;
  padding: 18px;
  box-shadow: 0 14px 34px rgb(18 60 45 / 6%);
}

.card-third {
  grid-column: span 2;
}

.card-half {
  grid-column: span 3;
}

.card-full {
  grid-column: 1 / -1;
}

.card-heading {
  align-items: flex-start;
  justify-content: space-between;
}

.card-heading h2 {
  margin: 2px 0 5px;
  font-size: 1rem;
}

.card-heading p {
  margin: 0;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
  line-height: 1.55;
}

.card-eyebrow {
  color: #39785d !important;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.64rem !important;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.card-count {
  display: grid;
  width: 34px;
  height: 34px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 11px;
  background: #edf7f1;
  color: var(--aidt-brand-strong);
  font-weight: 800;
}

.card-items {
  display: grid;
  gap: 6px;
  margin-top: 14px;
}

.card-item {
  width: 100%;
  justify-content: space-between;
  border: 0;
  border-top: 1px solid var(--aidt-border);
  background: transparent;
  padding: 10px 0 2px;
  text-align: left;
  cursor: pointer;
}

.card-item:hover strong {
  color: var(--aidt-brand-strong);
}

.item-main,
.item-main strong,
.item-main small {
  display: block;
  min-width: 0;
}

.item-main strong {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-main small,
.item-meta small {
  margin-top: 3px;
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.item-meta {
  flex: 0 0 auto;
  align-items: flex-end;
  flex-direction: column;
  gap: 2px;
}

.card-state {
  display: grid;
  gap: 5px;
  margin-top: 18px;
  border-radius: 13px;
  background: #f4f6f5;
  color: var(--aidt-ink-muted);
  padding: 14px;
  font-size: 0.78rem;
}

.card-state.error {
  background: #fff4f1;
  color: #9f3c30;
}

.is-unavailable {
  border-style: dashed;
}

.empty-workbench {
  grid-column: 1 / -1;
}

.metrics {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 12px;
  min-height: 90px;
}

.metrics article {
  border: 1px solid var(--aidt-border);
  border-radius: 15px;
  background: #fff;
  padding: 16px;
}

.metrics span,
.metrics strong {
  display: block;
}

.metrics span {
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.metrics strong {
  margin-top: 8px;
  color: var(--aidt-brand-strong);
  font-size: 1.8rem;
}

.fallback-work-grid,
.split-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
}

.split-grid {
  grid-template-columns: repeat(2, minmax(0, 1fr));
}

.simple-list {
  display: grid;
  margin-top: 12px;
}

.simple-row {
  justify-content: space-between;
  border-top: 1px solid var(--aidt-border);
  padding: 11px 0;
}

.simple-row span {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.simple-row small {
  color: var(--aidt-ink-muted);
}

.scenario-entry,
.scenario-entry-grid {
  display: grid;
  gap: 12px;
}

.scenario-entry-grid {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.scenario-entry-grid button {
  display: grid;
  gap: 6px;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: #f8fbf9;
  padding: 14px;
  color: var(--aidt-ink);
  text-align: left;
  cursor: pointer;
}

.scenario-entry-grid span,
.scenario-entry-grid small {
  color: var(--aidt-ink-muted);
  line-height: 1.5;
}

@media (max-width: 1080px) {
  .card-third,
  .card-half {
    grid-column: span 3;
  }
}

@media (max-width: 800px) {
  .page-head,
  .section-head {
    align-items: stretch;
    flex-direction: column;
  }

  .filters {
    justify-content: stretch;
  }

  .filters :deep(.el-select) {
    width: 100%;
  }

  .workbench-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .card-third,
  .card-half,
  .card-full {
    grid-column: span 2;
  }

  .metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .fallback-work-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 560px) {
  .workbench-grid,
  .metrics,
  .fallback-work-grid,
  .split-grid,
  .scenario-entry-grid {
    grid-template-columns: 1fr;
  }

  .card-third,
  .card-half,
  .card-full {
    grid-column: 1;
  }

  .workbench-card {
    padding: 15px;
  }
}
</style>
