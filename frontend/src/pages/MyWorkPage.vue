<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ApiError } from '@/api/http'
import { fileApi } from '@/api/modules/fileApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  DepartmentDto,
  MyWorkDto,
  WorkItemViewDto,
} from '@/api/types/contracts'
import EnvironmentTag from '@/components/base/EnvironmentTag.vue'
import WorkbenchFilterBar from '@/components/workspace/WorkbenchFilterBar.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import WorkbenchStats from '@/components/workspace/WorkbenchStats.vue'
import WorkItemTable from '@/features/work-items/components/WorkItemTable.vue'
import {
  EMPTY_MY_WORK_FILTERS,
  WORK_CATEGORY_LABELS,
  allWorkItems,
  filterMyWorkItems,
  hasActiveMyWorkFilters,
  matchesMyWorkFilters,
  myWorkCategoryCounts,
  workCategoryItems,
  type MyWorkFilterState,
  type WorkCategory,
} from '@/features/work-items/myWorkFilters'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const router = useRouter()
const loading = ref(true)
const errorMessage = ref('')
const includeTestData = ref(false)
const work = ref<MyWorkDto | null>(null)
const departments = ref<DepartmentDto[]>([])
const activeCategory = ref<WorkCategory>('pendingItems')
const filters = reactive<MyWorkFilterState>({ ...EMPTY_MY_WORK_FILTERS })
const pageIndex = ref(1)
const PAGE_SIZE = 50
const createVisible = ref(false)
const createLoading = ref(false)
const selectedFile = ref<File | null>(null)
const quickForm = reactive({
  type: '研发测试申请',
  departmentId: '',
  title: '',
  content: '',
  sensitivity: 'Internal' as 'Internal' | 'Sensitive' | 'Core',
})

const canCreateEvent = computed(() => auth.can('event-inbox:create'))
const canReadFiles = computed(() => auth.can('file:read'))
const canCreateFile = computed(() => auth.can('file:create'))
const canCreateMinutes = computed(() => auth.can('minutes:create'))
const canReadScenarios = computed(() => auth.can('scenario:read'))

const allItems = computed(() => allWorkItems(work.value))
const categoryCounts = computed(() => myWorkCategoryCounts(work.value, filters))
const visibleItems = computed(() =>
  filterMyWorkItems(workCategoryItems(work.value, activeCategory.value), filters),
)
const totalPages = computed(() =>
  Math.max(1, Math.ceil(visibleItems.value.length / PAGE_SIZE)),
)
const pagedItems = computed(() => {
  const start = (pageIndex.value - 1) * PAGE_SIZE
  return visibleItems.value.slice(start, start + PAGE_SIZE)
})
const categories = computed(() =>
  (['all', 'pendingItems', 'initiatedByMe', 'participating', 'needsAttention'] as WorkCategory[]).map(
    (key) => ({
      key,
      label: WORK_CATEGORY_LABELS[key],
      count: categoryCounts.value[key],
    }),
  ),
)
const departmentOptions = computed(() => departments.value.filter((item) => item.isActive))
const sourceOptions = computed(() =>
  Array.from(new Map(allItems.value.map((item) => [item.sourceType, item.sourceLabel])).entries()),
)
const statusOptions = computed(() =>
  Array.from(new Map(allItems.value.map((item) => [item.status, item.statusLabel])).entries()),
)
const primaryDepartment = computed(
  () => departments.value.find((item) => item.isPrimary) ?? departments.value[0] ?? null,
)
const selectedDepartment = computed(
  () => departments.value.find((item) => item.id === filters.departmentId) ?? null,
)
const scopeLabel = computed(() => {
  if (selectedDepartment.value) return `筛选范围：${selectedDepartment.value.name}`
  return `当前部门：${primaryDepartment.value?.name ?? '未分配'} · 全部授权范围`
})
const resultLabel = computed(() =>
  loading.value
    ? '正在加载'
    : `筛选结果 ${visibleItems.value.length} / ${allItems.value.length} 项 · 第 ${pageIndex.value} 页`,
)
const hasFilters = computed(() => hasActiveMyWorkFilters(filters))
const todayFocus = computed(() => {
  const focus = work.value?.todayFocus
  if (!focus) return null
  if (!['all', 'pendingItems'].includes(activeCategory.value)) return null
  return matchesMyWorkFilters(focus, filters) ? focus : null
})

const priorityOptions = [
  ['PermanentHumanBoundary', '永久人工边界'],
  ['Overdue', '已逾期'],
  ['DueToday', '今天到期'],
  ['AwaitingConfirmation', '待我确认'],
  ['ThisWeek', '本周'],
  ['Other', '常规'],
] as const

const stats = computed(() => [
  {
    label: '待我处理',
    value: work.value?.summary.pendingCount ?? '—',
    helper: '所有授权来源统一排序',
    tone: 'primary' as const,
    testId: 'my-work-stat-pending',
  },
  {
    label: '今天到期',
    value: work.value?.summary.dueTodayCount ?? '—',
    helper: '按上海业务时区判断',
    testId: 'my-work-stat-due-today',
  },
  {
    label: '待我确认',
    value: work.value?.summary.awaitingConfirmationCount ?? '—',
    helper: '数字员工不得代真人确认',
    testId: 'my-work-stat-confirmation',
  },
  {
    label: '人工边界',
    value: work.value?.summary.permanentHumanBoundaryCount ?? '—',
    helper: '付款、放行、配方、设备等',
    tone: 'warning' as const,
    testId: 'my-work-stat-boundary',
  },
])

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    ;[work.value, departments.value] = await Promise.all([
      workspaceApi.myWork(includeTestData.value),
      workspaceApi.departments(),
    ])
  } catch (error) {
    errorMessage.value =
      error instanceof ApiError ? error.message : '无法加载我的工作。'
  } finally {
    loading.value = false
  }
}

function resetFilters() {
  Object.assign(filters, EMPTY_MY_WORK_FILTERS)
}

function setPage(page: number) {
  pageIndex.value = Math.min(Math.max(1, page), totalPages.value)
}

function openQuickCreate(type = '研发测试申请') {
  quickForm.type = type
  quickForm.departmentId =
    departments.value.find((item) => item.isPrimary)?.id ??
    departments.value[0]?.id ??
    ''
  quickForm.title =
    type === '采购申请'
      ? '研发中试原料采购申请'
      : type === '质量异常'
        ? '测试数据或批次质量异常'
        : '新的研发测试申请'
  quickForm.content = ''
  quickForm.sensitivity = 'Internal'
  selectedFile.value = null
  createVisible.value = true
}

function handleCreateCommand(command: string) {
  if (command === 'minutes') {
    void router.push('/minutes')
    return
  }
  if (command === 'scenarios') {
    void router.push('/scenarios')
    return
  }
  openQuickCreate(command)
}

function onFileChange(event: Event) {
  const input = event.target as HTMLInputElement
  selectedFile.value = input.files?.[0] ?? null
}

async function submitQuickCreate() {
  if (!quickForm.departmentId || !quickForm.title.trim() || !quickForm.content.trim()) {
    ElMessage.warning('请填写部门、标题和事项说明。')
    return
  }
  createLoading.value = true
  try {
    let attachmentId: string | null = null
    let sourceType: 'ManualText' | 'ManualUpload' = 'ManualText'
    if (selectedFile.value) {
      const attachment = await fileApi.upload({
        file: selectedFile.value,
        departmentId: quickForm.departmentId,
        sensitivity: quickForm.sensitivity,
      })
      attachmentId = attachment.id
      sourceType = 'ManualUpload'
    }
    await workspaceApi.receiveInboxEvent({
      departmentId: quickForm.departmentId,
      title: `[${quickForm.type}] ${quickForm.title.trim()}`,
      content: quickForm.content.trim(),
      sourceType,
      attachmentId,
    })
    createVisible.value = false
    ElMessage.success('事项已进入确认队列，数字员工建议生成后仍需你确认')
    await router.push('/inbox')
  } catch (error) {
    ElMessage.error(
      error instanceof ApiError ? `${error.message}（${error.code}）` : '事项提交失败。',
    )
  } finally {
    createLoading.value = false
  }
}

function openItem(item: WorkItemViewDto) {
  const confirmationTypes: Partial<Record<WorkItemViewDto['sourceType'], string>> = {
    EventInbox: 'event',
    MinutesActionItem: 'minutes',
    McpHumanConfirmation: 'mcp',
    AgentRun: 'agent',
  }
  const confirmationType = confirmationTypes[item.sourceType]
  if (item.requiresConfirmation && confirmationType) {
    void router.push({
      path: '/confirmations',
      query: { type: confirmationType, id: item.detailTargetId || item.id },
    })
    return
  }
  const target = item.detailPath.startsWith('/') ? item.detailPath : '/'
  void router.push(target)
}

function priorityLabel(item: WorkItemViewDto): string {
  return {
    PermanentHumanBoundary: '永久人工边界',
    Overdue: '已逾期',
    DueToday: '今天到期',
    AwaitingConfirmation: '待我确认',
    ThisWeek: '本周',
    Other: '常规',
  }[item.priorityGroup]
}

function friendlyDue(item: WorkItemViewDto): string {
  if (!item.dueAt) return '未设置截止时间'
  return new Intl.DateTimeFormat('zh-CN', {
    timeZone: 'Asia/Shanghai',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(item.dueAt))
}

onMounted(load)
watch(filters, () => {
  pageIndex.value = 1
})
watch(activeCategory, () => {
  pageIndex.value = 1
})
</script>

<template>
  <div class="page-shell my-work-page" data-testid="my-work-page">
    <MainColumnPageHeader
      intro-key="my-work"
      :scope-label="scopeLabel"
      kicker="MY WORK · 统一工作事项"
    >
      <template #actions>
        <el-dropdown trigger="click" @command="handleCreateCommand">
          <el-button
            type="primary"
            data-testid="my-work-create-trigger"
            :disabled="loading || !canCreateEvent || !departments.length"
          >
            发起事项
          </el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="研发测试申请">研发测试申请</el-dropdown-item>
              <el-dropdown-item command="采购申请">采购申请</el-dropdown-item>
              <el-dropdown-item command="质量异常">质量异常</el-dropdown-item>
              <el-dropdown-item command="其他事项">其他事项</el-dropdown-item>
              <el-dropdown-item v-if="canCreateMinutes" divided command="minutes">从听记整理</el-dropdown-item>
              <el-dropdown-item v-if="canReadScenarios" command="scenarios">按业务场景办事</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />

    <WorkbenchStats :items="stats" :loading="loading" />

    <WorkbenchFilterBar :result-label="resultLabel">
      <template #search>
        <el-input
          v-model="filters.keyword"
          clearable
          placeholder="搜索事项、项目、业务对象或来源编号"
          aria-label="搜索我的工作"
          data-testid="my-work-search"
        />
      </template>

      <el-select
        v-model="filters.departmentId"
        clearable
        placeholder="部门"
        aria-label="按部门筛选"
        data-testid="my-work-department-filter"
      >
        <el-option
          v-for="item in departmentOptions"
          :key="item.id"
          :label="item.name"
          :value="item.id"
        />
      </el-select>
      <el-select
        v-model="filters.sourceType"
        clearable
        placeholder="来源类型"
        aria-label="按来源类型筛选"
        data-testid="my-work-source-filter"
      >
        <el-option
          v-for="[value, label] in sourceOptions"
          :key="value"
          :label="label"
          :value="value"
        />
      </el-select>
      <el-select
        v-model="filters.status"
        clearable
        placeholder="状态"
        aria-label="按状态筛选"
        data-testid="my-work-status-filter"
      >
        <el-option
          v-for="[value, label] in statusOptions"
          :key="value"
          :label="label"
          :value="value"
        />
      </el-select>
      <el-select
        v-model="filters.priorityGroup"
        clearable
        placeholder="优先级"
        aria-label="按优先级筛选"
        data-testid="my-work-priority-filter"
      >
        <el-option
          v-for="[value, label] in priorityOptions"
          :key="value"
          :label="label"
          :value="value"
        />
      </el-select>
      <el-select
        v-model="filters.due"
        clearable
        placeholder="截止时间"
        aria-label="按截止时间筛选"
        data-testid="my-work-due-filter"
      >
        <el-option label="今天到期" value="today" />
        <el-option label="未来 7 天" value="week" />
        <el-option label="已逾期" value="overdue" />
        <el-option label="未设置截止" value="none" />
      </el-select>

      <template #actions>
        <el-button
          text
          data-testid="my-work-reset"
          :disabled="!hasFilters"
          @click="resetFilters"
        >重置</el-button>
        <el-button
          v-if="canReadFiles"
          text
          data-testid="my-work-evidence-entry"
          @click="router.push('/documents')"
        >最近证据</el-button>
        <el-button
          text
          data-testid="open-confirmation-center"
          @click="router.push('/confirmations')"
        >
          统一确认
          <span v-if="work?.summary.awaitingConfirmationCount">
            （{{ work.summary.awaitingConfirmationCount }}）
          </span>
        </el-button>
        <label v-if="auth.isPlatformAdmin" class="admin-filter">
          <el-switch v-model="includeTestData" data-testid="my-work-env-switch" @change="load" />
          跨环境
        </label>
      </template>
    </WorkbenchFilterBar>

    <div class="category-tabs" role="tablist" aria-label="我的工作分类">
      <button
        v-for="category in categories"
        :key="category.key"
        class="category-tab"
        :class="{ active: activeCategory === category.key }"
        type="button"
        role="tab"
        :aria-selected="activeCategory === category.key"
        data-testid="my-work-category"
        :data-category="category.key"
        @click="activeCategory = category.key"
      >
        {{ category.label }}
        <span>{{ category.count }}</span>
      </button>
    </div>

    <section v-if="todayFocus" class="focus-strip" data-testid="my-work-today-focus">
      <div class="focus-copy">
        <div class="focus-tags">
          <span class="focus-tag">{{ priorityLabel(todayFocus) }}</span>
          <span class="source-tag">{{ todayFocus.sourceLabel }}</span>
          <EnvironmentTag
            :code="todayFocus.environmentCode"
            :is-test-data="todayFocus.isTestData"
          />
        </div>
        <p>今日焦点</p>
        <h2>{{ todayFocus.businessTitle }}</h2>
        <small>
          {{ todayFocus.contextTitle }} · {{ todayFocus.currentStage }}
          · 负责人 {{ todayFocus.assigneeDisplayName }}
        </small>
      </div>
      <div class="focus-action">
        <span>截止 {{ friendlyDue(todayFocus) }}</span>
        <el-button type="primary" @click="openItem(todayFocus)">
          {{ todayFocus.nextAction }}
        </el-button>
      </div>
    </section>

    <section v-loading="loading" class="work-list-section" aria-label="事项列表">
      <div class="list-heading">
        <div>
          <h2>{{ WORK_CATEGORY_LABELS[activeCategory] }}</h2>
          <p>按人工边界、逾期、今天到期、待确认、本周和其他顺序展示。</p>
        </div>
        <span>{{ resultLabel }}</span>
      </div>

      <WorkItemTable
        v-if="pagedItems.length"
        :items="pagedItems"
        @open="openItem"
      />
      <div v-if="visibleItems.length > PAGE_SIZE" class="list-pagination" data-testid="my-work-pagination">
        <el-button text :disabled="pageIndex <= 1" @click="setPage(pageIndex - 1)">上一页</el-button>
        <span>第 {{ pageIndex }} / {{ totalPages }} 页</span>
        <el-button text :disabled="pageIndex >= totalPages" @click="setPage(pageIndex + 1)">下一页</el-button>
      </div>
      <el-empty
        v-else-if="!loading && !errorMessage"
        :description="hasFilters ? '没有符合当前组合筛选条件的事项' : '当前分类没有事项'"
        data-testid="my-work-empty"
      >
        <el-button v-if="hasFilters" @click="resetFilters">清除筛选</el-button>
      </el-empty>
    </section>

    <el-dialog v-model="createVisible" title="发起事项" width="min(720px, 94vw)">
      <el-form label-position="top">
        <div class="quick-form-grid">
          <el-form-item label="事项类型">
            <el-select v-model="quickForm.type" style="width: 100%">
              <el-option label="研发测试申请" value="研发测试申请" />
              <el-option label="采购申请" value="采购申请" />
              <el-option label="质量异常" value="质量异常" />
              <el-option label="其他事项" value="其他事项" />
            </el-select>
          </el-form-item>
          <el-form-item label="所属部门">
            <el-select v-model="quickForm.departmentId" style="width: 100%">
              <el-option v-for="item in departments" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item class="span-2" label="事项标题">
            <el-input v-model="quickForm.title" maxlength="255" />
          </el-form-item>
          <el-form-item class="span-2" label="事项要求与说明">
            <el-input
              v-model="quickForm.content"
              type="textarea"
              :rows="5"
              placeholder="说明要做什么、涉及哪个项目或批次、需要哪些材料。缺失数据请明确标记。"
            />
          </el-form-item>
          <el-form-item v-if="canCreateFile" label="附件">
            <input
              class="native-file"
              type="file"
              accept=".png,.jpg,.jpeg,.pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.txt,.md,.json"
              @change="onFileChange"
            />
          </el-form-item>
          <el-form-item v-if="canCreateFile" label="附件保密等级">
            <el-select v-model="quickForm.sensitivity" style="width: 100%">
              <el-option label="内部" value="Internal" />
              <el-option label="敏感" value="Sensitive" />
              <el-option label="核心" value="Core" />
            </el-select>
          </el-form-item>
        </div>
        <div class="quick-note">
          提交后进入事项确认队列。数字员工只生成建议，真人确认前不会创建任务或发起流程。
          付款、质量放行、配方和设备控制仍须授权真人确认。
        </div>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="createLoading" @click="submitQuickCreate">
          提交并获取建议
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.my-work-page {
  gap: 14px;
}

.admin-filter {
  display: flex;
  align-items: center;
  gap: 6px;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
  white-space: nowrap;
}

.category-tabs {
  display: flex;
  min-width: 0;
  gap: 4px;
  overflow-x: auto;
  border-bottom: 1px solid var(--aidt-border);
  padding-bottom: 1px;
  scrollbar-width: thin;
}

.category-tab {
  display: inline-flex;
  min-height: 38px;
  flex: 0 0 auto;
  align-items: center;
  gap: 7px;
  border: 0;
  border-bottom: 2px solid transparent;
  background: transparent;
  color: var(--aidt-ink-muted);
  padding: 8px 12px 7px;
  font-size: 0.78rem;
  font-weight: 600;
  cursor: pointer;
}

.category-tab span {
  min-width: 19px;
  border-radius: 999px;
  background: #edf1ef;
  padding: 1px 6px;
  color: #708078;
  font-size: 0.64rem;
  text-align: center;
}

.category-tab.active {
  border-bottom-color: var(--aidt-brand);
  color: var(--aidt-brand-strong);
}

.category-tab.active span {
  background: var(--aidt-brand-soft);
  color: var(--aidt-brand-strong);
}

.focus-strip,
.focus-copy,
.list-heading {
  display: flex;
  gap: 12px;
}

.focus-strip,
.list-heading {
  align-items: center;
  justify-content: space-between;
}

.focus-strip {
  border: 1px solid #d4e1d9;
  border-left: 4px solid #b27a24;
  border-radius: 13px;
  background: linear-gradient(95deg, #fff9ed, #fff);
  padding: 14px 16px;
}

.focus-copy {
  min-width: 0;
  align-items: center;
}

.focus-copy p,
.focus-copy h2,
.focus-copy small {
  margin: 0;
}

.focus-copy p {
  color: #9a5a0a;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.focus-copy h2 {
  overflow: hidden;
  max-width: 520px;
  color: var(--aidt-ink);
  font-size: 0.92rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.focus-copy small {
  overflow: hidden;
  max-width: 520px;
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.focus-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
}

.focus-tag,
.source-tag {
  border-radius: 999px;
  background: var(--aidt-warning-soft);
  padding: 3px 7px;
  color: var(--aidt-warning);
  font-size: 0.64rem;
  font-weight: 800;
}

.source-tag {
  background: var(--aidt-brand-soft);
  color: var(--aidt-brand);
}

.focus-action {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  gap: 10px;
}

.focus-action span {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.work-list-section {
  display: grid;
  min-width: 0;
  gap: 10px;
  min-height: 180px;
}

.list-pagination {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.list-heading {
  align-items: flex-end;
}

.list-heading h2,
.list-heading p {
  margin: 0;
}

.list-heading h2 {
  font-size: 1rem;
}

.list-heading p,
.list-heading > span {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.list-heading p {
  margin-top: 3px;
}

.quick-form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 14px;
}

.span-2 {
  grid-column: 1 / -1;
}

.native-file {
  width: 100%;
  border: 1px dashed #a8c8b6;
  border-radius: 10px;
  background: #f8fbf8;
  padding: 10px;
  color: var(--aidt-ink-muted);
}

.quick-note {
  border: 1px solid #ead8ad;
  border-radius: 12px;
  background: #fff8e9;
  padding: 12px 14px;
  color: #78501d;
  font-size: 0.76rem;
  line-height: 1.65;
}

@media (max-width: 900px) {
  .focus-copy small {
    white-space: normal;
  }

  .focus-strip {
    align-items: stretch;
    flex-direction: column;
  }

  .focus-action {
    justify-content: space-between;
  }
}

@media (max-width: 720px) {
  .list-heading {
    align-items: flex-start;
    flex-direction: column;
  }

  .focus-copy {
    align-items: flex-start;
    flex-direction: column;
  }

  .focus-copy h2,
  .focus-copy small {
    max-width: 100%;
    white-space: normal;
  }

  .focus-action {
    align-items: stretch;
    flex-direction: column;
  }

  .quick-form-grid {
    grid-template-columns: 1fr;
  }

  .span-2 {
    grid-column: auto;
  }
}
</style>
