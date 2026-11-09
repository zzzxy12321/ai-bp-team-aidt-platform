<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { fileApi } from '@/api/modules/fileApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  BusinessObjectContextDto,
  BusinessObjectDto,
  BusinessObjectVersionDto,
  DepartmentDto,
  ObjectFieldDto,
  ObjectFieldUserOptionDto,
  ObjectTypeDto,
  ProjectDto,
  WorkflowInstanceDto,
} from '@/api/types/contracts'
import StatusBadge from '@/components/base/StatusBadge.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import DynamicFieldInput from '@/features/object-model/components/DynamicFieldInput.vue'
import {
  formatFieldValue,
  validateFieldValue,
} from '@/features/object-model/objectModelRules'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const route = useRoute()
const router = useRouter()
const loading = ref(true)
const saving = ref(false)
const errorMessage = ref('')
const denied = ref(false)
const conflict = ref(false)
const types = ref<ObjectTypeDto[]>([])
const objects = ref<BusinessObjectDto[]>([])
const departments = ref<DepartmentDto[]>([])
const users = ref<ObjectFieldUserOptionDto[]>([])
const selectedTypeId = ref('')
const filterTypeId = ref('')
const search = ref('')
const objectCode = ref('')
const createDepartmentId = ref('')
const fields = ref<Record<string, string | null>>({})
const detailVisible = ref(false)
const detailLoading = ref(false)
const detail = ref<BusinessObjectDto | null>(null)
const detailVersions = ref<BusinessObjectVersionDto[]>([])
const detailContext = ref<BusinessObjectContextDto | null>(null)
const detailTab = ref('fields')
const editVisible = ref(false)
const editSaving = ref(false)
const editFields = ref<Record<string, string | null>>({})
const maskedFieldKeys = ref(new Set<string>())
const editError = ref('')
const projectChoices = ref<ProjectDto[]>([])
const selectedProjectId = ref('')
const linkingProject = ref(false)
const evidenceUploading = ref(false)
const evidenceSensitivity = ref<'Internal' | 'Sensitive' | 'Core'>('Internal')
const focusObjectId = computed(() => String(route.query.focus ?? route.query.object ?? ''))

const selectedType = computed(() => types.value.find((item) => item.id === selectedTypeId.value))
const sortedCreateFields = computed(() =>
  [...(selectedType.value?.fields ?? [])].sort(
    (left, right) => left.sortOrder - right.sortOrder || left.key.localeCompare(right.key),
  ),
)
const visibleObjects = computed(() => {
  const keyword = search.value.trim().toLowerCase()
  return objects.value.filter((item) => {
    const matchesType = !filterTypeId.value || item.objectTypeId === filterTypeId.value
    const matchesKeyword =
      !keyword
      || item.objectCode.toLowerCase().includes(keyword)
      || objectTitle(item).toLowerCase().includes(keyword)
      || Object.values(item.fields).some((value) => value?.toLowerCase().includes(keyword))
    return matchesType && matchesKeyword
  })
})
const detailType = computed(() =>
  types.value.find((item) => item.id === detail.value?.objectTypeId),
)
const canUpdate = computed(() => auth.can('business-object:update'))
const canManageProject = computed(() => auth.can('project:update'))
const canModel = computed(() => auth.can('object-model:read') && auth.isPlatformAdmin)
const notPublished = computed(() => !loading.value && types.value.length === 0)
const userLabelMap = computed(
  () => new Map(users.value.map((item) => [item.id, `${item.displayName}（${item.departmentName}）`])),
)
const departmentLabelMap = computed(
  () => new Map(departments.value.map((item) => [item.id, item.name])),
)

function errorText(error: unknown, fallback: string) {
  return error instanceof ApiError ? `${error.message}（${error.code}）` : fallback
}

function objectTitle(object: BusinessObjectDto): string {
  const nameField = detailDefinitions(object).find((field) =>
    ['Name', 'ProjectName', 'Title'].includes(field.key),
  )
  return (
    nameField && object.fields[nameField.key]
      ? String(object.fields[nameField.key])
      : object.title || object.objectCode
  )
}

function typeFor(object: BusinessObjectDto) {
  return types.value.find((item) => item.id === object.objectTypeId)
}

function detailDefinitions(object: BusinessObjectDto): ObjectFieldDto[] {
  return [...(typeFor(object)?.fields ?? [])].sort(
    (left, right) => left.sortOrder - right.sortOrder || left.key.localeCompare(right.key),
  )
}

function listDefinitions(object: BusinessObjectDto): ObjectFieldDto[] {
  return detailDefinitions(object).filter((field) => field.showInList)
}

function displayValue(field: ObjectFieldDto | undefined, value: string | null | undefined) {
  if (value === '***') return '***（已脱敏）'
  if (field?.dataType === 'User' && value) return userLabelMap.value.get(value) ?? value
  if (field?.dataType === 'Department' && value) return departmentLabelMap.value.get(value) ?? value
  return formatFieldValue(field, value)
}

function statusTone(status: BusinessObjectDto['status']) {
  if (status === 'Active' || status === 'Completed') return 'success'
  if (status === 'Suspended' || status === 'Archived' || status === 'Terminated') return 'danger'
  if (status === 'Blocked' || status === 'AwaitingAcceptance') return 'warning'
  return 'neutral'
}

function objectRowClass({ row }: { row: BusinessObjectDto }) {
  return row.id === focusObjectId.value ? 'focused-object-row' : ''
}

function resetCreateFields() {
  fields.value = Object.fromEntries(
    sortedCreateFields.value.map((field) => [
      field.key,
      field.defaultValue
        ?? (field.dataType === 'Boolean'
          ? 'false'
          : field.dataType === 'Select'
            ? field.options[0] ?? ''
            : ''),
    ]),
  )
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  denied.value = false
  conflict.value = false
  try {
    const [publishedTypes, objectPage, departmentRows] = await Promise.all([
      workspaceApi.publishedObjectTypes(),
      workspaceApi.businessObjects(),
      workspaceApi.departments(),
    ])
    types.value = publishedTypes
    objects.value = objectPage.items
    departments.value = departmentRows
    createDepartmentId.value =
      createDepartmentId.value
      || departmentRows.find((item) => item.isPrimary)?.id
      || departmentRows[0]?.id
      || ''
    if (!selectedTypeId.value || !types.value.some((item) => item.id === selectedTypeId.value)) {
      selectedTypeId.value = types.value[0]?.id ?? ''
    } else {
      resetCreateFields()
    }
    try {
      users.value = await workspaceApi.objectFieldUsers()
    } catch {
      users.value = []
    }
    if (focusObjectId.value) {
      const focused = objects.value.find((item) => item.id === focusObjectId.value)
      if (focused) await openDetail(focused)
    }
  } catch (error) {
    if (error instanceof ApiError && error.status === 403) {
      denied.value = true
      errorMessage.value = '当前身份没有业务对象读取或部门数据范围。'
    } else {
      errorMessage.value = errorText(error, '加载业务对象失败。')
    }
  } finally {
    loading.value = false
  }
}

watch(selectedTypeId, resetCreateFields)

async function createObject() {
  if (!selectedType.value || !objectCode.value.trim()) {
    ElMessage.warning('请选择已发布对象类型并填写业务编号。')
    return
  }
  if (!createDepartmentId.value) {
    ElMessage.warning('请选择所属部门。')
    return
  }
  for (const field of sortedCreateFields.value) {
    const issue = validateFieldValue(field, fields.value[field.key])
    if (issue) {
      ElMessage.warning(issue)
      return
    }
  }
  saving.value = true
  errorMessage.value = ''
  try {
    const created = await workspaceApi.createBusinessObject(
      selectedType.value.id,
      objectCode.value.trim(),
      fields.value,
      createDepartmentId.value,
    )
    objectCode.value = ''
    resetCreateFields()
    objects.value = (await workspaceApi.businessObjects()).items
    ElMessage.success('业务对象已持久化')
    await openDetail(created)
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      conflict.value = true
      errorMessage.value = `${error.message}，请刷新后重试。`
    } else {
      errorMessage.value = errorText(error, '创建业务对象失败。')
    }
  } finally {
    saving.value = false
  }
}

async function openDetail(object: BusinessObjectDto) {
  detailVisible.value = true
  detailLoading.value = true
  detailTab.value = 'fields'
  detail.value = object
  detailVersions.value = []
  detailContext.value = null
  editError.value = ''
  try {
    const [fresh, versions, context, projectPage] = await Promise.all([
      workspaceApi.businessObject(object.id),
      workspaceApi.businessObjectVersions(object.id),
      workspaceApi.businessObjectContext(object.id),
      canManageProject.value ? workspaceApi.projects() : Promise.resolve({ items: [] }),
    ])
    detail.value = fresh
    detailVersions.value = versions
    detailContext.value = context
    projectChoices.value = projectPage.items.filter(
      (project) => project.departmentId === fresh.departmentId,
    )
  } catch (error) {
    editError.value = errorText(error, '读取业务对象详情失败。')
  } finally {
    detailLoading.value = false
  }
}

function openEdit() {
  if (!detail.value) return
  editError.value = ''
  editFields.value = { ...detail.value.fields }
  const masked = new Set<string>()
  detailDefinitions(detail.value).forEach((field) => {
    if (!auth.isPlatformAdmin && field.sensitivity === 'Core' && editFields.value[field.key] === '***') {
      masked.add(field.key)
    }
    if (!(field.key in editFields.value)) editFields.value[field.key] = field.defaultValue ?? ''
  })
  maskedFieldKeys.value = masked
  editVisible.value = true
}

async function saveEdit() {
  if (!detail.value) return
  const submitted: Record<string, string | null> = {}
  for (const field of detailDefinitions(detail.value)) {
    if (maskedFieldKeys.value.has(field.key)) continue
    const value = editFields.value[field.key]
    const issue = validateFieldValue(field, value)
    if (issue) {
      editError.value = issue
      return
    }
    submitted[field.key] = value
  }
  editSaving.value = true
  editError.value = ''
  conflict.value = false
  try {
    const updated = await workspaceApi.updateBusinessObject(
      detail.value.id,
      detail.value.version,
      submitted,
    )
    detail.value = updated
    editVisible.value = false
    objects.value = (await workspaceApi.businessObjects()).items
    await openDetail(updated)
    ElMessage.success('业务对象已更新，版本和审计已写入')
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      conflict.value = true
      editError.value = `${error.message}；请关闭编辑并重新读取最新版本。`
    } else {
      editError.value = errorText(error, '保存业务对象失败。')
    }
  } finally {
    editSaving.value = false
  }
}

async function linkProject() {
  if (!detail.value || !selectedProjectId.value) return
  linkingProject.value = true
  try {
    await workspaceApi.addProjectRelation(selectedProjectId.value, {
      entityType: 'BusinessObject',
      entityId: detail.value.id,
      relationCode: 'SupportingEvidence',
      evidenceExcerpt: `业务对象 ${detail.value.objectCode}`,
    })
    selectedProjectId.value = ''
    detailContext.value = await workspaceApi.businessObjectContext(detail.value.id)
    ElMessage.success('业务对象已关联项目')
  } catch (error) {
    ElMessage.error(errorText(error, '关联项目失败。'))
  } finally {
    linkingProject.value = false
  }
}

async function uploadEvidence(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !detail.value) return
  evidenceUploading.value = true
  try {
    await fileApi.upload({
      file,
      departmentId: detail.value.departmentId,
      businessObjectId: detail.value.id,
      sensitivity: evidenceSensitivity.value,
    })
    detailContext.value = await workspaceApi.businessObjectContext(detail.value.id)
    ElMessage.success('证据文件已上传并关联业务对象')
  } catch (error) {
    ElMessage.error(errorText(error, '上传证据失败。'))
  } finally {
    evidenceUploading.value = false
    input.value = ''
  }
}

async function downloadEvidence(fileId: string, fileName: string) {
  try {
    const blob = await fileApi.download(fileId)
    const url = URL.createObjectURL(blob)
    const anchor = document.createElement('a')
    anchor.href = url
    anchor.download = fileName
    anchor.click()
    URL.revokeObjectURL(url)
  } catch (error) {
    ElMessage.error(errorText(error, '下载文件失败。'))
  }
}

function openWorkflow(instance: WorkflowInstanceDto) {
  void router.push({ path: '/workflows', query: { instance: instance.id, object: detail.value?.id } })
}

function openWorkflows(object: BusinessObjectDto) {
  void router.push({ path: '/workflows', query: { object: object.id } })
}

function openAudit() {
  if (!detail.value) return
  void router.push({
    path: '/audit',
    query: { entityType: 'BusinessObject', entityId: detail.value.id },
  })
}

onMounted(load)
</script>

<template>
  <div class="page-shell object-page" data-testid="business-object-page">
    <MainColumnPageHeader
      intro-key="objects"
      title="业务视图"
      scope-label="当前部门 · 已发布业务对象"
      kicker="业务对象 · 动态表单"
    >
      <template #actions>
        <el-button v-if="canModel" data-testid="open-modeling-studio" @click="router.push('/modeling')">
          业务建模工作室
        </el-button>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="errorMessage"
      :type="conflict ? 'warning' : 'error'"
      :closable="false"
      show-icon
      :title="errorMessage"
      data-testid="object-error-state"
    >
      <template #default>
        <el-button text type="primary" @click="load()">重新读取</el-button>
      </template>
    </el-alert>

    <div v-if="denied" class="state-card" data-testid="object-denied-state">
      <strong>无权读取业务对象</strong>
      <p>请确认账号已开通业务对象读取权限和所属部门数据范围。</p>
    </div>

    <template v-else>
      <section v-if="notPublished" class="state-card" data-testid="object-not-published-state">
        <strong>暂无已发布业务表单</strong>
        <p>管理员需要先在业务建模工作室校验并发布对象类型，普通用户才能录入。</p>
        <el-button v-if="canModel" type="primary" @click="router.push('/modeling')">前往发布</el-button>
      </section>

      <section v-else class="section-card create-section">
        <div class="section-heading">
          <div>
            <p class="page-kicker">NEW OBJECT</p>
            <h2>按发布表单新建</h2>
            <p>字段类型、必填、长度、选项和敏感等级均来自当前发布版本。</p>
          </div>
          <div class="section-tags">
            <span v-if="selectedType" class="tag">{{ selectedType.name }}</span>
            <StatusBadge label="仅已发布类型" tone="success" />
          </div>
        </div>
        <el-form label-position="top" class="create-form" @submit.prevent="createObject">
          <el-form-item label="对象类型">
            <el-select v-model="selectedTypeId" data-testid="object-type-select" filterable style="width: 100%">
              <el-option
                v-for="type in types"
                :key="type.id"
                :label="`${type.name} · ${type.code}`"
                :value="type.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="所属部门">
            <el-select v-model="createDepartmentId" data-testid="object-department-select" filterable style="width: 100%">
              <el-option
                v-for="department in departments"
                :key="department.id"
                :label="department.name"
                :value="department.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="业务编号">
            <el-input
              v-model="objectCode"
              data-testid="object-code-input"
              placeholder="例如 SAMPLE-2026-001"
            />
          </el-form-item>
          <el-form-item
            v-for="field in sortedCreateFields"
            :key="field.key"
            :data-testid="`object-field-${field.key}`"
            :label="`${field.displayName}${field.isRequired ? ' *' : ''}`"
          >
            <DynamicFieldInput
              v-model="fields[field.key]"
              :field="field"
              :departments="departments"
              :department-id="createDepartmentId"
              :label="`${field.displayName}${field.isRequired ? ' *' : ''}`"
            />
          </el-form-item>
          <el-button
            type="primary"
            native-type="submit"
            data-testid="create-object-button"
            :loading="saving"
          >保存业务对象</el-button>
        </el-form>
      </section>

      <section v-loading="loading" class="section-card object-list">
        <div class="section-heading list-heading">
          <div><h2>对象列表 · {{ visibleObjects.length }}</h2><p>点击行查看字段、版本和关联证据。</p></div>
          <div class="list-filters">
            <el-select v-model="filterTypeId" clearable placeholder="全部已发布类型" style="width: 210px">
              <el-option
                v-for="type in types"
                :key="type.id"
                :label="type.name"
                :value="type.id"
              />
            </el-select>
            <el-input v-model="search" clearable placeholder="搜索编号或字段值" style="width: 220px" />
          </div>
        </div>
        <el-table
          v-if="visibleObjects.length"
          :data="visibleObjects"
          empty-text="暂无业务对象"
          :row-class-name="objectRowClass"
          @row-click="openDetail"
        >
          <el-table-column prop="objectCode" label="业务编号" min-width="170" />
          <el-table-column label="类型" min-width="150">
            <template #default="{ row }">{{ typeFor(row)?.name ?? '已停用类型' }}</template>
          </el-table-column>
          <el-table-column label="标题" min-width="180">
            <template #default="{ row }">{{ objectTitle(row) }}</template>
          </el-table-column>
          <el-table-column label="状态" width="110">
            <template #default="{ row }">
              <StatusBadge :label="row.status" :tone="statusTone(row.status)" />
            </template>
          </el-table-column>
          <el-table-column prop="version" label="版本" width="76" />
          <el-table-column label="摘要字段" min-width="300">
            <template #default="{ row }">
              <div class="field-chips">
                <span v-for="field in listDefinitions(row).slice(0, 4)" :key="field.key">
                  <strong>{{ field.displayName }}</strong>
                  {{ displayValue(field, row.fields[field.key]) }}
                </span>
                <span v-if="!listDefinitions(row).length" class="muted">无列表字段定义</span>
              </div>
            </template>
          </el-table-column>
          <el-table-column label="更新时间" min-width="165">
            <template #default="{ row }">{{ new Date(row.updatedAt).toLocaleString() }}</template>
          </el-table-column>
          <el-table-column label="操作" width="110">
            <template #default="{ row }">
              <el-button text type="primary" @click.stop="openWorkflows(row)">查看</el-button>
            </template>
          </el-table-column>
        </el-table>
        <el-empty v-else-if="!loading" description="没有匹配的业务对象">
          <el-button v-if="types.length" type="primary" @click="search = ''; filterTypeId = ''">清除筛选</el-button>
        </el-empty>
      </section>
    </template>

    <el-dialog
      v-model="detailVisible"
      title="业务对象详情"
      width="min(980px, 96vw)"
      data-testid="object-detail-dialog"
    >
      <div v-loading="detailLoading">
        <el-alert v-if="editError && !editVisible" type="error" :closable="false" :title="editError" />
        <template v-if="detail">
          <header class="detail-head">
            <div>
              <span class="code-label">{{ detail.objectCode }}</span>
              <h2>{{ objectTitle(detail) }}</h2>
              <p>{{ detailType?.name ?? '已停用或未发布类型' }} · {{ departmentLabelMap.get(detail.departmentId) ?? detail.departmentId }}</p>
            </div>
            <div class="detail-actions">
              <StatusBadge :label="detail.status" :tone="statusTone(detail.status)" />
              <span class="tag">v{{ detail.version }}</span>
              <el-button
                v-if="canUpdate && detail.status !== 'Archived' && detailType"
                type="primary"
                data-testid="edit-business-object"
                @click="openEdit"
              >编辑对象</el-button>
            </div>
          </header>
          <el-alert
            v-if="!detailType"
            type="warning"
            :closable="false"
            title="该类型当前没有已发布版本，对象仅提供历史查看，不能继续编辑。"
            data-testid="detail-not-published-state"
          />

          <el-tabs v-model="detailTab">
            <el-tab-pane label="字段" name="fields">
              <div v-if="detailDefinitions(detail).length" class="detail-field-grid">
                <article v-for="field in detailDefinitions(detail)" :key="field.key">
                  <span>{{ field.displayName }}</span>
                  <strong>{{ displayValue(field, detail.fields[field.key]) }}</strong>
                  <small>
                    {{ field.dataType }} · {{ field.sensitivity }}
                    <template v-if="field.sensitivity === 'Core'"> · 核心字段受权限保护</template>
                  </small>
                </article>
              </div>
              <el-empty v-else description="当前没有可用字段定义" />
            </el-tab-pane>

            <el-tab-pane :label="`版本 · ${detailVersions.length}`" name="versions">
              <el-timeline v-if="detailVersions.length">
                <el-timeline-item
                  v-for="version in detailVersions"
                  :key="version.id"
                  :timestamp="new Date(version.createdAt).toLocaleString()"
                  :type="version.version === detail.version ? 'success' : 'info'"
                >
                  <strong>v{{ version.version }} · {{ version.status }}</strong>
                  <p>{{ version.reason || '字段更新' }}</p>
                  <small>{{ Object.keys(version.fields).length }} 个字段快照</small>
                </el-timeline-item>
              </el-timeline>
              <el-empty v-else description="暂无版本快照" />
            </el-tab-pane>

            <el-tab-pane :label="`关联证据 · ${(detailContext?.projects.length ?? 0) + (detailContext?.workflows.length ?? 0) + (detailContext?.files.length ?? 0)}`" name="context">
              <div v-if="detailContext" class="context-grid">
                <section>
                  <div class="context-heading"><h3>项目</h3><span>{{ detailContext.projects.length }}</span></div>
                  <article v-for="project in detailContext.projects" :key="project.relationId" class="context-item">
                    <strong>{{ project.name }}</strong>
                    <small>{{ project.objectCode }} · {{ project.status }}</small>
                    <el-button text type="primary" @click="router.push(`/projects/${project.projectId}`)">打开项目</el-button>
                  </article>
                  <el-empty v-if="!detailContext.projects.length" description="尚未关联项目" />
                  <div v-if="canManageProject" class="inline-action">
                    <el-select v-model="selectedProjectId" filterable placeholder="选择同部门项目" style="flex: 1">
                      <el-option
                        v-for="project in projectChoices"
                        :key="project.id"
                        :label="`${project.name} · ${project.objectCode}`"
                        :value="project.id"
                      />
                    </el-select>
                    <el-button :loading="linkingProject" :disabled="!selectedProjectId" @click="linkProject">关联</el-button>
                  </div>
                </section>

                <section>
                  <div class="context-heading"><h3>流程</h3><span>{{ detailContext.workflows.length }}</span></div>
                  <article v-for="workflow in detailContext.workflows" :key="workflow.id" class="context-item">
                    <strong>{{ workflow.status }} · {{ workflow.currentNodeCode || '已结束' }}</strong>
                    <small>{{ new Date(workflow.startedAt).toLocaleString() }}</small>
                    <el-button text type="primary" @click="openWorkflow(workflow)">查看流程</el-button>
                  </article>
                  <el-empty v-if="!detailContext.workflows.length" description="尚未发起流程" />
                  <el-button text type="primary" @click="router.push({ path: '/workflows', query: { object: detail.id } })">前往流程工作区</el-button>
                </section>

                <section>
                  <div class="context-heading"><h3>文件</h3><span>{{ detailContext.files.length }}</span></div>
                  <article v-for="file in detailContext.files" :key="file.id" class="context-item">
                    <strong>{{ file.originalFileName }}</strong>
                    <small>{{ file.mimeType }} · v{{ file.currentVersionNumber }} · {{ file.sensitivity }}</small>
                    <el-button text type="primary" @click="downloadEvidence(file.id, file.originalFileName)">下载</el-button>
                  </article>
                  <el-empty v-if="!detailContext.files.length" description="尚未关联文件" />
                  <div class="inline-action evidence-upload">
                    <el-select v-model="evidenceSensitivity" style="width: 130px">
                      <el-option label="Internal" value="Internal" />
                      <el-option label="Sensitive" value="Sensitive" />
                      <el-option label="Core" value="Core" />
                    </el-select>
                    <label class="upload-button">
                      <input type="file" :disabled="evidenceUploading" @change="uploadEvidence" />
                      {{ evidenceUploading ? '上传中…' : '上传证据' }}
                    </label>
                  </div>
                </section>

                <section>
                  <div class="context-heading">
                    <h3>审计</h3>
                    <el-button text type="primary" @click="openAudit">完整审计</el-button>
                  </div>
                  <article v-for="event in detailContext.auditEvents.slice(0, 8)" :key="event.id" class="context-item">
                    <strong>{{ event.action }} · {{ event.outcome }}</strong>
                    <small>{{ new Date(event.occurredAt).toLocaleString() }} · {{ event.module }}</small>
                    <small v-if="event.reason">{{ event.reason }}</small>
                  </article>
                  <el-empty v-if="!detailContext.auditEvents.length" description="暂无审计事件" />
                </section>
              </div>
            </el-tab-pane>
          </el-tabs>
        </template>
      </div>
    </el-dialog>

    <el-dialog v-model="editVisible" title="编辑业务对象" width="min(760px, 95vw)">
      <el-alert v-if="editError" type="error" :closable="false" :title="editError" />
      <el-form v-if="detail" label-position="top">
        <el-form-item
          v-for="field in detailDefinitions(detail)"
          :key="field.key"
          :label="`${field.displayName}${field.isRequired ? ' *' : ''}`"
        >
          <DynamicFieldInput
            v-model="editFields[field.key]"
            :field="field"
            :departments="departments"
            :department-id="detail.departmentId"
            :core-masked="maskedFieldKeys.has(field.key)"
            :disabled="maskedFieldKeys.has(field.key)"
            :label="`${field.displayName}${field.isRequired ? ' *' : ''}`"
          />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="editVisible = false">取消</el-button>
        <el-button
          type="primary"
          data-testid="save-business-object"
          :loading="editSaving"
          @click="saveEdit"
        >保存并生成新版本</el-button>
      </template>
    </el-dialog>

    <div v-if="loading" class="loading-state" data-testid="object-loading-state">正在读取发布表单和业务对象…</div>
  </div>
</template>

<style scoped>
.object-head,
.section-heading,
.detail-head,
.detail-actions,
.list-filters,
.context-heading,
.inline-action,
.section-tags {
  display: flex;
  align-items: center;
  gap: 10px;
}

.object-head,
.section-heading,
.detail-head,
.context-heading {
  justify-content: space-between;
}

.object-head {
  align-items: flex-start;
}

.section-heading h2,
.section-heading p,
.detail-head h2,
.detail-head p {
  margin: 0;
}

.section-heading p,
.detail-head p {
  margin-top: 4px;
  color: var(--aidt-ink-muted);
}

.create-form {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0 16px;
  margin-top: 18px;
}

.create-form :deep(.el-button) {
  grid-column: 1 / -1;
  justify-self: start;
}

.object-list {
  margin-top: 18px;
}

.list-heading {
  margin-bottom: 14px;
}

.field-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 5px;
}

.field-chips span {
  max-width: 220px;
  overflow: hidden;
  border-radius: 999px;
  background: var(--aidt-surface-muted);
  padding: 4px 8px;
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.field-chips strong {
  color: var(--aidt-ink);
}

:deep(.focused-object-row > td) {
  background: #edf8f1 !important;
}

.detail-head {
  align-items: flex-start;
  margin-bottom: 16px;
}

.detail-actions {
  flex-wrap: wrap;
  justify-content: flex-end;
}

.detail-field-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.detail-field-grid article {
  display: grid;
  gap: 5px;
  border: 1px solid var(--aidt-border);
  border-radius: 13px;
  background: #fbfcfb;
  padding: 12px;
}

.detail-field-grid span,
.detail-field-grid small {
  color: var(--aidt-ink-muted);
}

.detail-field-grid strong {
  overflow-wrap: anywhere;
}

.context-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 12px;
}

.context-grid > section {
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  padding: 12px;
}

.context-heading h3 {
  margin: 0;
}

.context-item {
  display: grid;
  gap: 4px;
  border-top: 1px solid var(--aidt-border);
  padding: 10px 0;
}

.context-item small,
.muted {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.inline-action {
  margin-top: 10px;
}

.upload-button {
  display: inline-flex;
  min-height: 32px;
  align-items: center;
  border: 1px solid var(--aidt-border-strong);
  border-radius: 9px;
  padding: 0 12px;
  color: var(--aidt-brand-strong);
  cursor: pointer;
  font-size: 0.78rem;
  font-weight: 700;
}

.upload-button input {
  display: none;
}

.state-card {
  border: 1px solid var(--aidt-border);
  border-radius: 16px;
  background: #fff;
  padding: 26px;
}

.state-card p {
  color: var(--aidt-ink-muted);
}

.loading-state {
  color: var(--aidt-ink-muted);
  padding: 18px;
  text-align: center;
}

@media (max-width: 900px) {
  .create-form,
  .detail-field-grid,
  .context-grid {
    grid-template-columns: 1fr;
  }

  .object-head,
  .list-heading {
    align-items: stretch;
    flex-direction: column;
  }

  .list-filters {
    flex-wrap: wrap;
  }
}

@media (max-width: 600px) {
  .detail-actions,
  .list-filters {
    justify-content: flex-start;
  }

  .list-filters :deep(.el-select),
  .list-filters :deep(.el-input) {
    width: 100% !important;
  }
}
</style>
