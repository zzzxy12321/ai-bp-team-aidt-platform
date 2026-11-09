<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  AgentProfileDto,
  AgentRunDto,
  DepartmentDto,
  MinutesActionItemDto,
  MinutesSourceDto,
  MinutesSummaryDto,
} from '@/api/types/contracts'
import EnvironmentTag from '@/components/base/EnvironmentTag.vue'
import StatusBadge from '@/components/base/StatusBadge.vue'
import WorkbenchFilterBar from '@/components/workspace/WorkbenchFilterBar.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import WorkbenchStats from '@/components/workspace/WorkbenchStats.vue'
import { useAuthStore } from '@/stores/auth'

type MinutesFilters = {
  keyword: string
  departmentId: string
  analysisStatus: string
  actionItemStatus: string
  sourceType: string
  meetingRange: [string, string] | null
}

const EMPTY_FILTERS: MinutesFilters = {
  keyword: '',
  departmentId: '',
  analysisStatus: '',
  actionItemStatus: '',
  sourceType: '',
  meetingRange: null,
}

const PAGE_SIZE = 20
const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const loading = ref(true)
const listLoading = ref(false)
const detailLoading = ref(false)
const actionLoading = ref('')
const errorMessage = ref('')
const departments = ref<DepartmentDto[]>([])
const profiles = ref<AgentProfileDto[]>([])
const sources = ref<MinutesSourceDto[]>([])
const selectedSourceId = ref('')
const actionItems = ref<MinutesActionItemDto[]>([])
const activeRun = ref<AgentRunDto | null>(null)
const selectedMinutesFile = ref<File | null>(null)
const registerVisible = ref(false)
const filters = reactive<MinutesFilters>({ ...EMPTY_FILTERS })
const includeTestData = ref(false)
const summary = ref<MinutesSummaryDto | null>(null)
const pageIndex = ref(1)
const pageTotal = ref(0)
const title = ref('客户样品周会')
const departmentId = ref('')
const transcript = ref(
  '会议：客户样品周会\n' +
    '时间：2026-10-31 09:30\n' +
    '[09:31:05] 研发工程师：请质量工程师在 2026-11-02 完成耐候样品测试确认。\n' +
    '[09:33:12] 质量工程师：需要研发整理测试原始数据并提交。\n' +
    '[09:35:20] 平台管理员：跟进试产评审材料，紧急安排一次内部确认。',
)
let searchTimer: ReturnType<typeof setTimeout> | null = null

const selectedSource = computed(
  () => sources.value.find((item) => item.id === selectedSourceId.value) ?? null,
)
const primaryDepartment = computed(
  () => departments.value.find((item) => item.isPrimary) ?? departments.value[0] ?? null,
)
const selectedDepartment = computed(
  () => departments.value.find((item) => item.id === filters.departmentId) ?? null,
)
const environmentLabel = computed(() => {
  const code = selectedSource.value?.environmentCode ?? sources.value[0]?.environmentCode ?? 'demo'
  return {
    demo: '演示环境',
    test: '测试环境',
    prod: '生产环境',
  }[code]
})
const scopeLabel = computed(() => {
  const department = selectedDepartment.value?.name ?? primaryDepartment.value?.name ?? '未分配'
  return `当前部门：${department} · ${environmentLabel.value}`
})
const pageCount = computed(() => Math.max(1, Math.ceil(pageTotal.value / PAGE_SIZE)))
const resultLabel = computed(() =>
  listLoading.value
    ? '正在加载'
    : `听记 ${pageTotal.value} 条 · 第 ${pageIndex.value} / ${pageCount.value} 页`,
)
const stats = computed(() => [
  {
    label: '听记总数',
    value: summary.value?.totalCount ?? '—',
    helper: '当前授权范围全部来源',
    tone: 'primary' as const,
    testId: 'minutes-stat-total',
  },
  {
    label: '已分析',
    value: summary.value?.analyzedCount ?? '—',
    helper: '规则模拟分析已完成',
    testId: 'minutes-stat-analyzed',
  },
  {
    label: '待确认行动项',
    value: summary.value?.pendingActionItemCount ?? '—',
    helper: '必须由真人核对确认',
    tone: 'warning' as const,
    testId: 'minutes-stat-pending-actions',
  },
  {
    label: '本周新增',
    value: summary.value?.createdThisWeekCount ?? '—',
    helper: '上海时间周一开始',
    testId: 'minutes-stat-week',
  },
])
const canToggleTestData = computed(() => auth.isPlatformAdmin)

function messageFor(error: unknown, fallback: string): string {
  return error instanceof ApiError ? `${error.message}（${error.code}）` : fallback
}

function sourceQuery(page: number) {
  const [meetingFrom, meetingTo] = filters.meetingRange ?? []
  return {
    page,
    pageSize: PAGE_SIZE,
    includeTestData: includeTestData.value,
    search: filters.keyword.trim() || undefined,
    departmentId: filters.departmentId || undefined,
    status: filters.analysisStatus || undefined,
    actionItemStatus: filters.actionItemStatus || undefined,
    sourceType: filters.sourceType || undefined,
    meetingFrom: meetingFrom ? new Date(`${meetingFrom}T00:00:00+08:00`).toISOString() : undefined,
    meetingTo: meetingTo ? new Date(`${meetingTo}T23:59:59+08:00`).toISOString() : undefined,
  }
}

async function loadSummary() {
  summary.value = await workspaceApi.minutesSummary(includeTestData.value)
}

async function loadSources(page = 1) {
  listLoading.value = true
  try {
    const result = await workspaceApi.minutes(sourceQuery(page))
    sources.value = result.items
    pageIndex.value = result.page
    pageTotal.value = result.total
    const requestedSourceId = String(route.query.source ?? '')
    const candidateIds = new Set(result.items.map((item) => item.id))
    if (requestedSourceId && candidateIds.has(requestedSourceId)) {
      selectedSourceId.value = requestedSourceId
    } else if (!candidateIds.has(selectedSourceId.value)) {
      selectedSourceId.value = result.items[0]?.id ?? ''
    }
    await loadSelected()
    await focusRequestedAction()
  } finally {
    listLoading.value = false
  }
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const [departmentsData, profileData] = await Promise.all([
      workspaceApi.departments(),
      workspaceApi.agents(),
      loadSummary(),
    ])
    departments.value = departmentsData
    profiles.value = profileData.filter((item) => item.isActive)
    departmentId.value =
      departmentId.value ||
      departments.value.find((item) => item.isPrimary)?.id ||
      departments.value[0]?.id ||
      ''
    await loadSources(1)
  } catch (error) {
    errorMessage.value = messageFor(error, '加载 AI 听记失败。')
  } finally {
    loading.value = false
  }
}

async function refreshAfterMutation() {
  await Promise.all([loadSummary(), loadSources(pageIndex.value)])
}

async function applyFilters() {
  pageIndex.value = 1
  await loadSources(1)
}

async function resetFilters() {
  Object.assign(filters, EMPTY_FILTERS)
  includeTestData.value = false
  await Promise.all([loadSummary(), applyFilters()])
}

async function toggleTestData() {
  await Promise.all([loadSummary(), applyFilters()])
}

async function selectSource(source: MinutesSourceDto) {
  selectedSourceId.value = source.id
  await router.replace({ query: { ...route.query, source: source.id } })
  await loadSelected()
}

async function focusRequestedAction() {
  const actionItemId = String(route.query.actionItem ?? '')
  if (!actionItemId) return
  await nextTick()
  document.querySelector(`[data-action-item-id="${actionItemId}"]`)?.scrollIntoView({
    behavior: 'smooth',
    block: 'center',
  })
}

async function loadSelected() {
  if (!selectedSourceId.value) {
    actionItems.value = []
    activeRun.value = null
    return
  }
  detailLoading.value = true
  try {
    actionItems.value = await workspaceApi.minutesActionItems(selectedSourceId.value)
    const source = selectedSource.value
    if (source?.analysisRunId) {
      const runs = await workspaceApi.agentRuns()
      activeRun.value = runs.items.find((item) => item.id === source.analysisRunId) ?? null
    } else {
      activeRun.value = null
    }
  } finally {
    detailLoading.value = false
  }
}

async function goToPage(page: number) {
  const safePage = Math.min(Math.max(1, page), pageCount.value)
  await loadSources(safePage)
}

async function registerMinutes() {
  if (!departmentId.value || !title.value.trim()
    || (!selectedMinutesFile.value && !transcript.value.trim())) {
    ElMessage.warning('请填写部门、标题和文字稿或选择文字稿文件。')
    return
  }
  actionLoading.value = 'register'
  try {
    const source = selectedMinutesFile.value
      ? await workspaceApi.uploadMinutes({
          file: selectedMinutesFile.value,
          departmentId: departmentId.value,
          title: title.value.trim(),
          meetingAt: new Date().toISOString(),
        })
      : await workspaceApi.registerMinutes({
          departmentId: departmentId.value,
          title: title.value.trim(),
          meetingAt: new Date().toISOString(),
          transcript: transcript.value,
        })
    registerVisible.value = false
    selectedSourceId.value = source.id
    ElMessage.success('听记已登记，原始文字稿已保存')
    await load()
  } catch (error) {
    ElMessage.error(messageFor(error, '听记登记失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function chooseMinutesFile(event: Event) {
  const input = event.target as HTMLInputElement
  selectedMinutesFile.value = input.files?.[0] ?? null
  if (selectedMinutesFile.value) {
    transcript.value = await selectedMinutesFile.value.text()
    title.value ||= selectedMinutesFile.value.name.replace(/\.[^.]+$/, '')
  }
}

async function requestAnalysis() {
  const source = selectedSource.value
  const profile = profiles.value.find((item) => item.name.includes('业务协同'))
  if (!source || !profile) {
    ElMessage.warning('缺少听记或业务协同 Agent 配置。')
    return
  }
  actionLoading.value = 'analyze'
  try {
    activeRun.value = await workspaceApi.analyzeMinutes(source.id, profile.id)
    ElMessage.success('分析运行已创建，请由真人确认后执行')
    await refreshAfterMutation()
  } catch (error) {
    ElMessage.error(messageFor(error, '创建分析运行失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function confirmAnalysis() {
  if (!activeRun.value) return
  actionLoading.value = 'confirm'
  try {
    activeRun.value = await workspaceApi.confirmAgentRun(activeRun.value.id)
    ElMessage.success('真人确认已记录，现在可执行规则模拟分析')
    await loadSelected()
  } catch (error) {
    ElMessage.error(messageFor(error, '确认分析失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function executeAnalysis() {
  if (!activeRun.value) return
  actionLoading.value = 'execute'
  try {
    const result = await workspaceApi.executeAgentRun(activeRun.value.id)
    actionItems.value = result.actionItems
    ElMessage.success(
      `规则模拟完成，生成 ${result.actionItems.length} 条草稿；外部模型未调用`,
    )
    await refreshAfterMutation()
  } catch (error) {
    ElMessage.error(messageFor(error, '执行分析失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function saveAction(item: MinutesActionItemDto) {
  if (!selectedSourceId.value) return
  actionLoading.value = item.id
  try {
    await workspaceApi.updateMinutesAction(selectedSourceId.value, item)
    ElMessage.success('人工修改已保存')
  } catch (error) {
    ElMessage.error(messageFor(error, '保存行动项失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function confirmAction(item: MinutesActionItemDto) {
  actionLoading.value = item.id
  try {
    const task = await workspaceApi.confirmMinutesAction(item)
    ElMessage.success(`任务已创建：${task.id}`)
    await refreshAfterMutation()
  } catch (error) {
    ElMessage.error(messageFor(error, '确认行动项失败。'))
  } finally {
    actionLoading.value = ''
  }
}

function statusTone(status: string) {
  if (status === 'Completed' || status === 'Confirmed' || status === 'Analyzed') return 'success'
  if (status === 'Running') return 'info'
  if (status === 'AwaitingConfirmation' || status === 'Draft') return 'warning'
  return 'neutral'
}

function formatDate(value: string | null) {
  return value
    ? new Intl.DateTimeFormat('zh-CN', {
        timeZone: 'Asia/Shanghai',
        month: '2-digit',
        day: '2-digit',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
      }).format(new Date(value))
    : '未设置'
}

watch(
  () => filters.keyword,
  () => {
    if (searchTimer) clearTimeout(searchTimer)
    searchTimer = setTimeout(() => void applyFilters(), 350)
  },
)
watch(
  [
    () => filters.departmentId,
    () => filters.analysisStatus,
    () => filters.actionItemStatus,
    () => filters.sourceType,
    () => filters.meetingRange,
  ],
  () => void applyFilters(),
)

onMounted(load)
onBeforeUnmount(() => {
  if (searchTimer) clearTimeout(searchTimer)
})
</script>

<template>
  <div class="page-shell minutes-page" data-testid="minutes-page">
    <MainColumnPageHeader
      intro-key="minutes"
      :scope-label="scopeLabel"
      kicker="AI MINUTES · 规则模拟与人工确认"
    >
      <template #actions>
        <el-button type="primary" data-testid="open-minutes-import" @click="registerVisible = true">
          导入听记
        </el-button>
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
          placeholder="搜索标题、参会人或文字稿"
          aria-label="搜索听记"
          data-testid="minutes-search"
        />
      </template>

      <el-select
        v-model="filters.departmentId"
        clearable
        placeholder="部门"
        aria-label="按部门筛选"
        data-testid="minutes-department-filter"
      >
        <el-option
          v-for="item in departments"
          :key="item.id"
          :label="item.name"
          :value="item.id"
        />
      </el-select>
      <el-select
        v-model="filters.analysisStatus"
        clearable
        placeholder="分析状态"
        aria-label="按分析状态筛选"
        data-testid="minutes-analysis-filter"
      >
        <el-option label="待分析" value="Imported" />
        <el-option label="等待真人确认" value="AwaitingConfirmation" />
        <el-option label="已分析" value="Analyzed" />
        <el-option label="已确认" value="Confirmed" />
      </el-select>
      <el-select
        v-model="filters.actionItemStatus"
        clearable
        placeholder="行动项状态"
        aria-label="按行动项状态筛选"
        data-testid="minutes-action-status-filter"
      >
        <el-option label="待确认" value="Draft" />
        <el-option label="已确认" value="Confirmed" />
      </el-select>
      <el-select
        v-model="filters.sourceType"
        clearable
        placeholder="来源类型"
        aria-label="按来源类型筛选"
        data-testid="minutes-source-filter"
      >
        <el-option label="手动文字" value="ManualText" />
        <el-option label="文件导入" value="ManualUpload" />
      </el-select>
      <el-date-picker
        v-model="filters.meetingRange"
        type="daterange"
        value-format="YYYY-MM-DD"
        start-placeholder="会议开始"
        end-placeholder="会议结束"
        range-separator="至"
        aria-label="按会议时间筛选"
        data-testid="minutes-meeting-filter"
      />

      <template #actions>
        <el-button text data-testid="minutes-reset" @click="resetFilters">重置</el-button>
        <label v-if="canToggleTestData" class="admin-filter">
          <el-switch
            v-model="includeTestData"
            data-testid="minutes-env-switch"
            @change="toggleTestData"
          />
          跨环境
        </label>
      </template>
    </WorkbenchFilterBar>

    <section
      v-loading="loading"
      class="minutes-workspace"
      aria-label="听记来源与详情"
      data-testid="minutes-workspace"
    >
      <aside class="minutes-source-panel">
        <div class="source-panel-head">
          <div>
            <h2>听记来源</h2>
            <small>{{ resultLabel }}</small>
          </div>
          <span class="source-panel-mark" aria-hidden="true">源</span>
        </div>
        <div v-loading="listLoading" class="source-scroll" data-testid="minutes-source-list">
          <button
            v-for="source in sources"
            :key="source.id"
            type="button"
            class="source-row"
            :class="{ active: source.id === selectedSourceId }"
            :aria-current="source.id === selectedSourceId ? 'true' : undefined"
            :data-testid="`minutes-source-${source.id}`"
            @click="selectSource(source)"
          >
            <span class="source-row-top">
              <strong>{{ source.title }}</strong>
              <StatusBadge :label="source.status" :tone="statusTone(source.status)" />
            </span>
            <span class="source-row-meta">
              {{ formatDate(source.meetingAt) }} · {{ source.sourceType === 'ManualUpload' ? '文件导入' : '手动文字' }}
            </span>
            <EnvironmentTag :code="source.environmentCode" :is-test-data="source.isTestData" />
          </button>
          <el-empty v-if="!sources.length && !listLoading" description="没有符合筛选条件的听记" :image-size="64" />
        </div>
        <div class="source-pagination">
          <el-button text :disabled="pageIndex <= 1 || listLoading" @click="goToPage(pageIndex - 1)">
            上一页
          </el-button>
          <span>{{ pageIndex }} / {{ pageCount }}</span>
          <el-button text :disabled="pageIndex >= pageCount || listLoading" @click="goToPage(pageIndex + 1)">
            下一页
          </el-button>
        </div>
      </aside>

      <main class="minutes-detail-panel" data-testid="minutes-detail">
        <div v-if="selectedSource" v-loading="detailLoading" class="detail-scroll">
          <div class="detail-head">
            <div>
              <div class="detail-tags">
                <StatusBadge :label="selectedSource.status" :tone="statusTone(selectedSource.status)" />
                <EnvironmentTag
                  :code="selectedSource.environmentCode"
                  :is-test-data="selectedSource.isTestData"
                />
                <span>{{ selectedSource.analysisVersion }}</span>
              </div>
              <h2>{{ selectedSource.title }}</h2>
              <p>{{ formatDate(selectedSource.meetingAt) }} · {{ selectedSource.sourceType === 'ManualUpload' ? '文件导入' : '手动文字' }}</p>
            </div>
          </div>

          <div class="pipeline">
            <el-button
              v-if="!activeRun"
              type="primary"
              data-testid="analyze-minutes-button"
              :loading="actionLoading === 'analyze'"
              @click="requestAnalysis"
            >
              创建分析运行
            </el-button>
            <el-button
              v-if="activeRun?.status === 'AwaitingConfirmation'"
              type="warning"
              data-testid="confirm-minutes-run-button"
              :loading="actionLoading === 'confirm'"
              @click="confirmAnalysis"
            >
              真人确认运行
            </el-button>
            <el-button
              v-if="activeRun?.status === 'Running'"
              type="success"
              data-testid="execute-minutes-run-button"
              :loading="actionLoading === 'execute'"
              @click="executeAnalysis"
            >
              执行规则模拟
            </el-button>
            <el-tag v-if="activeRun" effect="plain">
              AgentRun {{ activeRun.status }} · {{ activeRun.gatewayMode || '待调用' }}
            </el-tag>
            <el-tag type="info" effect="plain">规则模拟 · ExternalModelCalled=false</el-tag>
          </div>

          <details class="transcript">
            <summary>查看原始文字稿</summary>
            <pre>{{ selectedSource.transcript }}</pre>
          </details>

          <div class="action-heading">
            <div>
              <h3>行动项草稿</h3>
              <p>数字员工只生成草稿；负责人、期限和内容由真人核对后确认。</p>
            </div>
            <span>{{ actionItems.length }} 条</span>
          </div>
          <div class="action-table-wrap">
            <el-table
              :data="actionItems"
              empty-text="尚无行动项；请先确认并执行分析运行"
              :row-class-name="({ row }: { row: MinutesActionItemDto }) => row.id === route.query.actionItem ? 'focused-action-row' : ''"
            >
              <el-table-column label="来源" min-width="180">
                <template #default="{ row }">
                  <div :data-action-item-id="row.id" class="action-source">
                    <strong>{{ row.speaker }}</strong>
                    <small>{{ row.sourceTimestamp }}</small>
                    <p>{{ row.sourceExcerpt }}</p>
                  </div>
                </template>
              </el-table-column>
              <el-table-column label="人工校对内容" min-width="250">
                <template #default="{ row }">
                  <el-input v-model="row.content" :disabled="row.status !== 'Draft'" type="textarea" />
                </template>
              </el-table-column>
              <el-table-column label="优先级" width="96">
                <template #default="{ row }">
                  <el-select v-model="row.priority" :disabled="row.status !== 'Draft'">
                    <el-option label="P0" value="P0" />
                    <el-option label="P1" value="P1" />
                    <el-option label="P2" value="P2" />
                  </el-select>
                </template>
              </el-table-column>
              <el-table-column prop="confidence" label="置信度" width="82" />
              <el-table-column label="业务上下文" min-width="190">
                <template #default="{ row }">
                  <strong>{{ row.assigneeDisplayName || '负责人待选' }}</strong>
                  <small>截止 {{ formatDate(row.dueAt) }}</small>
                  <small>{{ row.projectName || '未关联项目' }}</small>
                  <small>{{ row.businessObjectCode ? `${row.businessObjectCode} · ${row.businessObjectTitle || '业务对象'}` : '未关联业务对象' }}</small>
                  <small>{{ row.workflowInstanceStatus ? `流程 ${row.workflowInstanceStatus}` : row.workflowVersionId ? '已选流程版本' : '未关联流程' }}</small>
                </template>
              </el-table-column>
              <el-table-column prop="missingInformation" label="缺失信息" min-width="130" />
              <el-table-column label="状态" width="92">
                <template #default="{ row }">
                  <StatusBadge :label="row.status" :tone="statusTone(row.status)" />
                </template>
              </el-table-column>
              <el-table-column label="操作" width="142">
                <template #default="{ row }">
                  <template v-if="row.status === 'Draft'">
                    <el-button
                      text
                      type="primary"
                      @click="router.push({ path: '/confirmations', query: { type: 'minutes', id: row.id } })"
                    >
                      统一确认
                    </el-button>
                    <el-button
                      text
                      type="primary"
                      :loading="actionLoading === row.id"
                      @click="saveAction(row)"
                    >
                      保存
                    </el-button>
                    <el-button
                      text
                      type="success"
                      :loading="actionLoading === row.id"
                      @click="confirmAction(row)"
                    >
                      确认
                    </el-button>
                  </template>
                  <span v-else>已建任务</span>
                </template>
              </el-table-column>
            </el-table>
          </div>
        </div>
        <el-empty v-else description="选择或导入一份听记" />
      </main>
    </section>

    <el-dialog v-model="registerVisible" title="手动导入听记" width="min(760px, 94vw)">
      <div class="register-form">
        <el-select v-model="departmentId" placeholder="部门">
          <el-option
            v-for="item in departments"
            :key="item.id"
            :label="item.name"
            :value="item.id"
          />
        </el-select>
        <el-input v-model="title" placeholder="会议标题" />
        <input
          type="file"
          accept=".docx,.txt,.md,.json"
          data-testid="minutes-file-input"
          @change="chooseMinutesFile"
        />
        <el-input
          v-model="transcript"
          data-testid="minutes-transcript"
          type="textarea"
          :rows="12"
          placeholder="粘贴 UTF-8 文字稿"
        />
        <el-alert
          type="info"
          :closable="false"
          title="支持 .docx/.txt/.md/.json 文字稿；PDF 可保存在文件中心，但需粘贴文字内容后分析。音频转写和钉钉自动同步未启用。"
        />
      </div>
      <template #footer>
        <el-button @click="registerVisible = false">取消</el-button>
        <el-button
          type="primary"
          data-testid="register-minutes-button"
          :loading="actionLoading === 'register'"
          @click="registerMinutes"
        >
          保存听记
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.minutes-page {
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

.minutes-workspace {
  display: grid;
  height: clamp(520px, calc(100dvh - 420px), 760px);
  min-width: 0;
  min-height: 0;
  grid-template-columns: minmax(300px, 340px) minmax(0, 1fr);
  gap: 12px;
}

.minutes-source-panel,
.minutes-detail-panel {
  min-width: 0;
  min-height: 0;
  overflow: hidden;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: #fff;
}

.minutes-source-panel {
  display: grid;
  grid-template-rows: auto minmax(0, 1fr) auto;
}

.source-panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
  border-bottom: 1px solid var(--aidt-border);
  background: #f5f9f6;
  padding: 12px 13px;
}

.source-panel-head h2,
.source-panel-head small {
  margin: 0;
}

.source-panel-head h2 {
  font-size: 0.92rem;
}

.source-panel-head small {
  display: block;
  margin-top: 3px;
  color: var(--aidt-ink-muted);
  font-size: 0.66rem;
}

.source-panel-mark {
  display: grid;
  width: 29px;
  height: 29px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 8px;
  background: var(--aidt-brand-soft);
  color: var(--aidt-brand);
  font-size: 0.7rem;
  font-weight: 800;
}

.source-scroll {
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: 4px 7px;
  scrollbar-gutter: stable;
}

.source-row {
  display: grid;
  width: 100%;
  gap: 6px;
  border: 1px solid transparent;
  border-bottom-color: #e7ede9;
  border-radius: 9px;
  background: transparent;
  padding: 10px 8px;
  color: inherit;
  text-align: left;
  cursor: pointer;
}

.source-row.active {
  border-color: #c7ddd1;
  background: #edf7f1;
}

.source-row-top {
  display: flex;
  min-width: 0;
  align-items: flex-start;
  justify-content: space-between;
  gap: 7px;
}

.source-row strong {
  display: -webkit-box;
  overflow: hidden;
  font-size: 0.76rem;
  line-height: 1.45;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

.source-row-meta {
  color: var(--aidt-ink-muted);
  font-size: 0.66rem;
}

.source-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  border-top: 1px solid var(--aidt-border);
  padding: 4px 8px;
}

.source-pagination span {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.minutes-detail-panel {
  position: relative;
}

.detail-scroll {
  height: 100%;
  overflow-y: auto;
  overscroll-behavior: contain;
  padding: 16px;
  scrollbar-gutter: stable;
}

.detail-head,
.action-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 12px;
}

.detail-tags {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
}

.detail-head h2 {
  margin: 7px 0 3px;
  font-size: 1.18rem;
}

.detail-head p,
.action-heading p {
  margin: 0;
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.pipeline {
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 8px;
  margin: 14px 0;
}

.transcript {
  margin-bottom: 15px;
}

.transcript summary {
  cursor: pointer;
  color: var(--aidt-brand-strong);
  font-size: 0.76rem;
  font-weight: 700;
}

.transcript pre {
  overflow: auto;
  max-height: 220px;
  border-radius: 10px;
  background: #102a22;
  color: #eff8f3;
  padding: 13px;
  font-size: 0.72rem;
  white-space: pre-wrap;
}

.action-heading {
  align-items: flex-end;
  margin-bottom: 8px;
}

.action-heading h3 {
  margin: 0;
  font-size: 0.9rem;
}

.action-heading > span {
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
}

.action-table-wrap {
  min-width: 0;
  overflow-x: auto;
}

.action-source strong,
.action-source small,
.action-source p {
  display: block;
}

.action-source small,
.action-source p,
.minutes-detail-panel :deep(.el-table small) {
  margin: 2px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.66rem;
  line-height: 1.45;
}

.action-source p {
  white-space: normal;
}

:deep(.focused-action-row > td) {
  background: #edf8f1 !important;
}

.register-form {
  display: grid;
  gap: 12px;
}

@media (max-width: 900px) {
  .minutes-workspace {
    height: auto;
    grid-template-columns: 1fr;
  }

  .minutes-source-panel,
  .minutes-detail-panel {
    height: min(560px, 72dvh);
  }

  .minutes-detail-panel {
    height: min(650px, 78dvh);
  }
}

@media (max-width: 600px) {
  .detail-scroll {
    padding: 12px;
  }

  .detail-head,
  .action-heading {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
