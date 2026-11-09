<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  BusinessObjectDto,
  ObjectTypeDto,
  ObjectTypeValidationDto,
  ObjectTypeVersionDto,
  WorkflowDefinitionDto,
} from '@/api/types/contracts'
import StatusBadge from '@/components/base/StatusBadge.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import DynamicFieldInput from '@/features/object-model/components/DynamicFieldInput.vue'
import {
  buildFieldPreviewValues,
  createObjectField,
  fieldTypeLabels,
  normalizeField,
  objectFieldTypes,
  objectTypeStatusLabel,
  objectTypeStatusTone,
  validateObjectTypeDraft,
} from '@/features/object-model/objectModelRules'
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore()
const loading = ref(true)
const actionLoading = ref('')
const errorMessage = ref('')
const denied = ref(false)
const conflict = ref(false)
const types = ref<ObjectTypeDto[]>([])
const objects = ref<BusinessObjectDto[]>([])
const workflowDefinitions = ref<WorkflowDefinitionDto[]>([])
const selectedId = ref('')
const selected = ref<ObjectTypeDto | null>(null)
const draftName = ref('')
const draftFields = ref<ObjectTypeDto['fields']>([])
const savedSignature = ref('')
const validationResult = ref<ObjectTypeValidationDto | null>(null)
const versions = ref<ObjectTypeVersionDto[]>([])
const historyVisible = ref(false)
const previewVisible = ref(false)
const createVisible = ref(false)
const previewValues = reactive<Record<string, string | null>>({})
const createForm = reactive({ code: '', name: '' })

const canCreate = computed(() => auth.can('object-model:create'))
const canUpdate = computed(() => auth.can('object-model:update'))
const canManage = computed(() => auth.can('object-model:manage'))
const canApprove = computed(() => auth.can('object-model:approve'))
const isRetired = computed(() => selected.value?.status === 'Retired')
const localErrors = computed(() => validateObjectTypeDraft(draftName.value, draftFields.value))
const currentSignature = computed(() => JSON.stringify({
  name: draftName.value.trim(),
  fields: draftFields.value.map(normalizeField),
}))
const hasUnsavedChanges = computed(
  () => Boolean(selected.value) && currentSignature.value !== savedSignature.value,
)
const canValidate = computed(
  () => Boolean(selected.value && !isRetired.value && !hasUnsavedChanges.value && localErrors.value.length === 0),
)
const canPublish = computed(
  () =>
    Boolean(
      selected.value
        && canApprove.value
        && ['Draft', 'Validated'].includes(selected.value.status)
        && !hasUnsavedChanges.value
        && localErrors.value.length === 0,
    ),
)
const relatedObjectCount = computed(
  () => objects.value.filter((item) => item.objectTypeId === selected.value?.id).length,
)
const relatedWorkflowCount = computed(
  () => workflowDefinitions.value.filter((item) => item.objectTypeCode === selected.value?.code).length,
)
const publishedVersion = computed(
  () => versions.value.find((item) => item.status === 'Published') ?? null,
)

function errorText(error: unknown, fallback: string) {
  return error instanceof ApiError
    ? `${error.message}（${error.code}）`
    : fallback
}

function handleLoadError(error: unknown) {
  if (error instanceof ApiError && error.status === 403) {
    denied.value = true
    errorMessage.value = '当前身份没有业务模型读取或管理权限。'
  } else if (error instanceof ApiError && error.status === 409) {
    conflict.value = true
    errorMessage.value = `${error.message}，请重新读取最新版本。`
  } else {
    errorMessage.value = errorText(error, '加载业务建模工作室失败。')
  }
}

function setSelectedType(type: ObjectTypeDto) {
  selected.value = cloneObjectType(type)
  selectedId.value = type.id
  draftName.value = type.name
  draftFields.value = type.fields.map((field) => ({
    ...field,
    options: [...field.options],
  }))
  savedSignature.value = JSON.stringify({
    name: type.name,
    fields: type.fields.map(normalizeField),
  })
  validationResult.value = null
  Object.keys(previewValues).forEach((key) => delete previewValues[key])
  Object.assign(previewValues, buildFieldPreviewValues(draftFields.value))
}

function cloneObjectType(type: ObjectTypeDto): ObjectTypeDto {
  return {
    ...type,
    fields: type.fields.map((field) => ({
      ...field,
      options: [...field.options],
    })),
  }
}

async function load(preferredId = selectedId.value) {
  loading.value = true
  errorMessage.value = ''
  denied.value = false
  conflict.value = false
  try {
    const [typePage, objectPage, definitions] = await Promise.all([
      workspaceApi.objectTypes(),
      workspaceApi.businessObjects(),
      workspaceApi.workflowDefinitions(),
    ])
    types.value = typePage.items
    objects.value = objectPage.items
    workflowDefinitions.value = definitions
    const next = types.value.find((item) => item.id === preferredId) ?? types.value[0]
    if (next) {
      setSelectedType(next)
    } else {
      selected.value = null
      selectedId.value = ''
      draftName.value = ''
      draftFields.value = []
    }
  } catch (error) {
    handleLoadError(error)
  } finally {
    loading.value = false
  }
}

async function reloadAndSelect(id: string) {
  const typePage = await workspaceApi.objectTypes()
  types.value = typePage.items
  const fresh = types.value.find((item) => item.id === id)
  if (fresh) setSelectedType(fresh)
}

function selectType(type: ObjectTypeDto) {
  setSelectedType(type)
}

function openCreate() {
  createForm.code = `FORM_${new Date().toISOString().slice(0, 10).replaceAll('-', '')}`
  createForm.name = ''
  createVisible.value = true
}

async function createType() {
  const code = createForm.code.trim().toUpperCase()
  if (!/^[A-Z][A-Z0-9_-]*$/.test(code) || !createForm.name.trim()) {
    ElMessage.warning('请填写名称，并使用大写字母开头的稳定编码。')
    return
  }
  actionLoading.value = 'create'
  try {
    const created = await workspaceApi.createObjectType({
      code,
      name: createForm.name.trim(),
      fields: [createObjectField(1)],
    })
    createVisible.value = false
    await reloadAndSelect(created.id)
    ElMessage.success('对象类型草稿已创建')
  } catch (error) {
    ElMessage.error(errorText(error, '创建对象类型失败。'))
  } finally {
    actionLoading.value = ''
  }
}

function addField() {
  const field = createObjectField(draftFields.value.length + 1)
  while (draftFields.value.some((item) => item.key.toLowerCase() === field.key.toLowerCase())) {
    field.key = `${field.key}_${draftFields.value.length + 1}`
    field.code = field.key
  }
  draftFields.value.push(field)
  previewValues[field.key] = ''
}

function removeField(index: number) {
  const [removed] = draftFields.value.splice(index, 1)
  if (removed) delete previewValues[removed.key]
}

function moveField(index: number, direction: -1 | 1) {
  const target = index + direction
  if (target < 0 || target >= draftFields.value.length) return
  const next = [...draftFields.value]
  ;[next[index], next[target]] = [next[target], next[index]]
  next.forEach((field, fieldIndex) => {
    field.sortOrder = (fieldIndex + 1) * 10
  })
  draftFields.value = next
}

function updateFieldCode(index: number, value: string) {
  const field = draftFields.value[index]
  if (!field) return
  const previous = field.key
  field.key = value.trim()
  field.code = field.key
  if (previous in previewValues) {
    previewValues[field.key] = previewValues[previous] ?? ''
    delete previewValues[previous]
  }
}

function updateDataType(index: number, value: string) {
  const field = draftFields.value[index]
  if (!field) return
  field.dataType = value
  if (value === 'Select' && !field.options.length) field.options = ['选项 1']
  if (value !== 'Select') field.options = []
  if (['Date', 'DateTime', 'Boolean', 'User', 'Department'].includes(value)) {
    field.maxLength = Math.min(field.maxLength, 100)
  }
}

async function saveDraft() {
  if (!selected.value || !canUpdate.value || isRetired.value) return
  if (localErrors.value.length) {
    ElMessage.warning(localErrors.value[0])
    return
  }
  actionLoading.value = 'save'
  try {
    const updated = await workspaceApi.updateObjectTypeDraft(selected.value.id, {
      expectedRevision: selected.value.revision,
      name: draftName.value.trim(),
      fields: draftFields.value.map(normalizeField),
    })
    await reloadAndSelect(updated.id)
    await load(updated.id)
    ElMessage.success('新草稿版本已持久化，线上发布版本保持不变')
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) {
      conflict.value = true
    }
    ElMessage.error(errorText(error, '保存草稿失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function validateDraft() {
  if (!selected.value) return
  if (hasUnsavedChanges.value) {
    ElMessage.warning('请先保存草稿，再执行服务端校验。')
    return
  }
  if (localErrors.value.length) {
    ElMessage.warning(localErrors.value[0])
    return
  }
  actionLoading.value = 'validate'
  try {
    const result = await workspaceApi.validateObjectType(
      selected.value.id,
      selected.value.revision,
    )
    await reloadAndSelect(selected.value.id)
    validationResult.value = result
    ElMessage.success('模型结构校验通过')
  } catch (error) {
    ElMessage.error(errorText(error, '对象模型校验失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function publishType() {
  if (!selected.value || !canPublish.value) return
  await ElMessageBox.confirm(
    `发布后，${selected.value.code} 将立即用于新对象录入、流程配置和工作台对象卡片。历史对象与流程不受影响。`,
    '发布业务表单',
    { type: 'warning', confirmButtonText: '确认发布', cancelButtonText: '取消' },
  )
  actionLoading.value = 'publish'
  try {
    const published = await workspaceApi.publishObjectType(
      selected.value.id,
      selected.value.revision,
    )
    await load(published.id)
    ElMessage.success(`已发布 ${published.code} v${published.version}`)
  } catch (error) {
    if (error instanceof ApiError && error.status === 409) conflict.value = true
    ElMessage.error(errorText(error, '发布对象类型失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function toggleRetire() {
  if (!selected.value || !canManage.value) return
  const retiring = selected.value.status !== 'Retired'
  if (retiring) {
    await ElMessageBox.confirm(
      '停用后普通用户不能再创建新对象；已有对象和历史数据保留。',
      '停用业务表单',
      { type: 'warning', confirmButtonText: '确认停用', cancelButtonText: '取消' },
    )
  }
  actionLoading.value = retiring ? 'retire' : 'restore'
  try {
    const updated = retiring
      ? await workspaceApi.retireObjectType(selected.value.id, selected.value.revision)
      : await workspaceApi.restoreObjectType(selected.value.id, selected.value.revision)
    await load(updated.id)
    ElMessage.success(retiring ? '对象类型已停用，历史数据保留' : '对象类型已恢复发布')
  } catch (error) {
    ElMessage.error(errorText(error, retiring ? '停用失败。' : '恢复失败。'))
  } finally {
    actionLoading.value = ''
  }
}

async function showHistory() {
  if (!selected.value) return
  versions.value = await workspaceApi.objectTypeVersions(selected.value.id)
  historyVisible.value = true
}

function openPreview() {
  if (localErrors.value.length) {
    ElMessage.warning('请先修正字段校验问题，再查看预览。')
    return
  }
  Object.assign(previewValues, buildFieldPreviewValues(draftFields.value))
  previewVisible.value = true
}

watch(
  draftFields,
  (fields) => {
    fields.forEach((field) => {
      if (!(field.key in previewValues)) previewValues[field.key] = ''
    })
  },
  { deep: true },
)

onMounted(load)
</script>

<template>
  <div class="page-shell modeling-page" data-testid="business-modeling-page">
    <MainColumnPageHeader
      intro-key="modeling"
      scope-label="平台管理员 · 已发布表单与草稿"
      kicker="业务建模工作室 · 版本化表单"
    >
      <template #actions>
        <el-button v-if="canCreate" type="primary" data-testid="create-object-type" @click="openCreate">
          新建对象类型
        </el-button>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="errorMessage"
      :type="conflict ? 'warning' : denied ? 'error' : 'error'"
      :closable="false"
      show-icon
      :title="errorMessage"
      data-testid="modeling-error-state"
    >
      <template #default>
        <el-button text type="primary" @click="load()">重新读取</el-button>
      </template>
    </el-alert>

    <div v-if="denied" class="state-card" data-testid="modeling-denied-state">
      <strong>无权访问业务建模</strong>
      <p>只有具备对象模型读取权限的平台管理员可以进入工作室。</p>
    </div>

    <section v-else v-loading="loading" class="modeling-grid">
      <aside class="section-card type-list">
        <div class="section-heading">
          <div><h2>对象类型</h2><p>草稿、发布与停用一览</p></div>
          <span>{{ types.length }}</span>
        </div>
        <div v-if="types.length" class="type-list-body">
          <button
            v-for="type in types"
            :key="type.id"
            type="button"
            class="type-item"
            :class="{ active: type.id === selectedId }"
            :data-testid="`object-type-item-${type.code}`"
            @click="selectType(type)"
          >
            <span class="type-title">
              <strong>{{ type.name }}</strong>
              <StatusBadge
                :label="objectTypeStatusLabel(type.status)"
                :tone="objectTypeStatusTone(type.status)"
              />
            </span>
            <small>{{ type.code }} · v{{ type.version }} · Revision {{ type.revision }}</small>
            <small>{{ type.fields.length }} 个字段</small>
          </button>
        </div>
        <el-empty v-else-if="!loading" description="还没有对象类型">
          <el-button v-if="canCreate" type="primary" @click="openCreate">创建第一个表单</el-button>
        </el-empty>
      </aside>

      <main v-if="selected" class="modeling-main">
        <section class="section-card modeling-toolbar">
          <div>
            <div class="status-line">
              <StatusBadge
                :label="objectTypeStatusLabel(selected.status)"
                :tone="objectTypeStatusTone(selected.status)"
              />
              <span class="code-label">{{ selected.code }}</span>
              <span class="tag">v{{ selected.version }}</span>
              <span v-if="hasUnsavedChanges" class="tag warning">有未保存修改</span>
            </div>
            <h2>{{ selected.name }}</h2>
            <p>
              {{ selected.status === 'Published'
                ? '当前有发布版本供业务使用。修改会生成新草稿，不会覆盖历史。'
                : selected.status === 'Retired'
                  ? '对象类型已停用，历史对象保留；恢复后才能继续发布。'
                  : '草稿尚未进入业务录入。校验并发布后才对普通用户可见。'
              }}
            </p>
          </div>
          <div class="toolbar-actions">
            <el-button data-testid="preview-object-form" @click="openPreview">表单预览</el-button>
            <el-button @click="showHistory">版本历史</el-button>
            <el-button
              v-if="canUpdate && !isRetired"
              data-testid="save-model-draft"
              :loading="actionLoading === 'save'"
              :disabled="localErrors.length > 0"
              @click="saveDraft"
            >保存新草稿</el-button>
            <el-button
              v-if="canManage && !isRetired"
              data-testid="validate-model"
              :loading="actionLoading === 'validate'"
              :disabled="!canValidate"
              @click="validateDraft"
            >校验</el-button>
            <el-button
              v-if="canApprove && ['Draft', 'Validated'].includes(selected.status)"
              type="primary"
              data-testid="publish-object-type"
              :loading="actionLoading === 'publish'"
              :disabled="!canPublish"
              @click="publishType"
            >发布</el-button>
            <el-button
              v-if="canManage && ['Published', 'Retired'].includes(selected.status)"
              :type="selected.status === 'Retired' ? 'success' : 'danger'"
              plain
              :loading="['retire', 'restore'].includes(actionLoading)"
              @click="toggleRetire"
            >{{ selected.status === 'Retired' ? '恢复' : '停用' }}</el-button>
          </div>
        </section>

        <section class="section-card field-designer" data-testid="field-designer">
          <div class="section-heading">
            <div><h2>字段设计器</h2><p>字段编码用于 API，显示名称用于员工表单。</p></div>
            <el-button v-if="canUpdate && !isRetired" @click="addField">添加字段</el-button>
          </div>

          <el-form label-position="top" :disabled="!canUpdate || isRetired">
            <el-form-item label="对象类型名称">
              <el-input v-model="draftName" data-testid="model-name-input" />
            </el-form-item>
          </el-form>

          <div v-if="draftFields.length" class="field-list">
            <article
              v-for="(field, index) in draftFields"
              :key="`${index}-${field.key}`"
              class="field-card"
              :data-testid="`field-card-${index}`"
            >
              <div class="field-card-head">
                <span class="field-index">{{ index + 1 }}</span>
                <strong>{{ field.displayName || '未命名字段' }}</strong>
                <span class="field-type">{{ fieldTypeLabels[field.dataType] }}</span>
                <div class="field-order">
                  <el-button text size="small" :disabled="index === 0" @click="moveField(index, -1)">↑</el-button>
                  <el-button text size="small" :disabled="index === draftFields.length - 1" @click="moveField(index, 1)">↓</el-button>
                  <el-button text size="small" type="danger" :disabled="draftFields.length === 1" @click="removeField(index)">删除</el-button>
                </div>
              </div>
              <div class="field-grid">
                <el-form-item label="字段编码">
                  <el-input
                    :model-value="field.key"
                    :data-testid="`field-code-${index}`"
                    @update:model-value="updateFieldCode(index, $event)"
                  />
                </el-form-item>
                <el-form-item label="显示名称">
                  <el-input v-model="field.displayName" :data-testid="`field-name-${index}`" />
                </el-form-item>
                <el-form-item label="数据类型">
                  <el-select
                    :model-value="field.dataType"
                    :data-testid="`field-type-${index}`"
                    aria-label="数据类型"
                    style="width: 100%"
                    @update:model-value="updateDataType(index, $event)"
                  >
                    <el-option
                      v-for="type in objectFieldTypes"
                      :key="type"
                      :label="fieldTypeLabels[type]"
                      :value="type"
                    />
                  </el-select>
                </el-form-item>
                <el-form-item label="最大长度">
                  <el-input-number v-model="field.maxLength" :min="1" :max="100000" style="width: 100%" />
                </el-form-item>
                <el-form-item label="敏感等级">
                  <el-select v-model="field.sensitivity" style="width: 100%">
                    <el-option label="Internal" value="Internal" />
                    <el-option label="Sensitive" value="Sensitive" />
                    <el-option label="Core" value="Core" />
                  </el-select>
                </el-form-item>
                <el-form-item label="默认值">
                  <DynamicFieldInput
                    v-model="field.defaultValue"
                    :field="field"
                    compact
                    :disabled="!canUpdate || isRetired"
                  />
                </el-form-item>
              </div>
              <el-form-item v-if="field.dataType === 'Select'" label="下拉选项">
                <el-select
                  v-model="field.options"
                  aria-label="下拉选项"
                  multiple
                  filterable
                  allow-create
                  default-first-option
                  style="width: 100%"
                  :data-testid="`field-options-${index}`"
                  placeholder="输入选项后按回车"
                />
              </el-form-item>
              <div class="field-flags">
                <el-checkbox v-model="field.isRequired">必填</el-checkbox>
                <el-checkbox v-model="field.showInList">列表中显示</el-checkbox>
                <el-checkbox v-model="field.filterable">可筛选</el-checkbox>
                <span>排序 {{ field.sortOrder }}</span>
              </div>
            </article>
          </div>
          <el-empty v-else description="至少添加一个字段" />
        </section>

        <section class="section-card validation-panel" data-testid="model-validation-panel">
          <div class="section-heading">
            <div><h2>校验结果</h2><p>本地结构检查与服务端持久化校验共同生效。</p></div>
            <StatusBadge
              :label="localErrors.length ? `${localErrors.length} 个问题` : validationResult ? '服务端通过' : '待校验'"
              :tone="localErrors.length ? 'danger' : validationResult ? 'success' : 'neutral'"
            />
          </div>
          <ul v-if="localErrors.length" class="issue-list">
            <li v-for="error in localErrors" :key="error">{{ error }}</li>
          </ul>
          <el-alert
            v-else-if="validationResult"
            type="success"
            :closable="false"
            :title="`服务端校验通过 · Revision ${validationResult.revision}`"
          />
          <p v-else class="muted">保存草稿后可执行服务端校验。</p>
        </section>

        <section class="section-card impact-panel" data-testid="model-impact-panel">
          <div class="section-heading">
            <div><h2>影响范围</h2><p>发布只影响后续新录入与配置选择，不改写历史。</p></div>
            <span class="tag">{{ selected.code }}</span>
          </div>
          <div class="impact-grid">
            <article><strong>{{ relatedObjectCount }}</strong><span>已有业务对象</span></article>
            <article><strong>{{ relatedWorkflowCount }}</strong><span>引用该类型的流程定义</span></article>
            <article><strong>{{ selected.fields.length }}</strong><span>当前字段数</span></article>
            <article><strong>{{ publishedVersion?.version ?? '—' }}</strong><span>当前发布版本</span></article>
          </div>
          <p>
            发布后立即出现在业务对象录入、流程配置对象类型选择和岗位工作台
            `business_objects` 卡片的 `objectCodes` 筛选中。历史对象、流程实例和版本快照保持不变。
          </p>
        </section>
      </main>

      <el-empty v-else-if="!loading" class="section-card" description="选择或创建一个对象类型" />
    </section>

    <el-dialog v-model="createVisible" title="新建对象类型草稿" width="min(560px, 94vw)">
      <el-form label-position="top">
        <el-form-item label="稳定编码">
          <el-input v-model="createForm.code" data-testid="new-type-code" placeholder="例如 CUSTOMER_VISIT" />
        </el-form-item>
        <el-form-item label="业务名称">
          <el-input v-model="createForm.name" data-testid="new-type-name" placeholder="例如 客户拜访记录" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="actionLoading === 'create'" @click="createType">创建草稿</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="previewVisible" title="表单预览" width="min(760px, 95vw)">
      <div class="preview-shell">
        <div class="preview-heading">
          <span class="code-label">{{ selected?.code }}</span>
          <h2>{{ draftName || '未命名表单' }}</h2>
          <p>预览使用本地草稿，不会写入业务对象。</p>
        </div>
        <el-form label-position="top">
          <el-form-item
            v-for="field in draftFields"
            :key="field.key"
            :label="`${field.displayName}${field.isRequired ? ' *' : ''}`"
          >
            <DynamicFieldInput
              v-model="previewValues[field.key]"
              :field="field"
            />
          </el-form-item>
        </el-form>
      </div>
    </el-dialog>

    <el-dialog v-model="historyVisible" title="对象类型版本历史" width="min(760px, 95vw)">
      <el-timeline v-if="versions.length">
        <el-timeline-item
          v-for="version in versions"
          :key="version.id"
          :timestamp="version.publishedAt || version.updatedAt"
          :type="version.status === 'Published' ? 'success' : 'info'"
        >
          <strong>v{{ version.version }} · {{ objectTypeStatusLabel(version.status) }}</strong>
          <p>{{ version.name }} · {{ version.fields.length }} 个字段</p>
          <small>{{ version.fields.map((field) => field.displayName).join('、') }}</small>
        </el-timeline-item>
      </el-timeline>
      <el-empty v-else description="暂无版本历史" />
    </el-dialog>
  </div>
</template>

<style scoped>
.modeling-head,
.section-heading,
.toolbar-actions,
.status-line,
.type-title,
.field-card-head,
.field-flags {
  display: flex;
  align-items: center;
  gap: 10px;
}

.modeling-head {
  justify-content: space-between;
  align-items: flex-start;
}

.section-heading {
  justify-content: space-between;
}

.section-heading h2,
.section-heading p {
  margin: 0;
}

.section-heading p {
  margin-top: 4px;
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.modeling-grid {
  display: grid;
  grid-template-columns: minmax(220px, 0.7fr) minmax(0, 2.3fr);
  gap: 16px;
  align-items: start;
}

.type-list {
  position: sticky;
  top: 18px;
  max-height: calc(100vh - 40px);
  overflow: auto;
}

.type-list-body,
.modeling-main,
.field-list {
  display: grid;
  gap: 12px;
}

.type-list-body {
  margin-top: 14px;
}

.type-item {
  display: grid;
  width: 100%;
  gap: 5px;
  border: 1px solid var(--aidt-border);
  border-radius: 13px;
  background: #fff;
  padding: 12px;
  text-align: left;
  cursor: pointer;
}

.type-item.active {
  border-color: #8ab8a2;
  background: #f0f8f4;
}

.type-title {
  justify-content: space-between;
}

.type-item small {
  color: var(--aidt-ink-muted);
}

.modeling-toolbar {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
}

.modeling-toolbar h2,
.modeling-toolbar p {
  margin: 8px 0 0;
}

.modeling-toolbar p {
  color: var(--aidt-ink-muted);
}

.toolbar-actions {
  flex-wrap: wrap;
  justify-content: flex-end;
}

.tag.warning {
  border-color: #efd19e;
  background: var(--aidt-warning-soft);
  color: var(--aidt-warning);
}

.field-card {
  display: grid;
  gap: 10px;
  border: 1px solid var(--aidt-border);
  border-radius: 15px;
  background: #fbfcfb;
  padding: 14px;
}

.field-card-head {
  min-width: 0;
}

.field-index {
  display: grid;
  width: 28px;
  height: 28px;
  flex: 0 0 auto;
  place-items: center;
  border-radius: 9px;
  background: #dcefe5;
  color: #185c42;
  font-weight: 800;
}

.field-type {
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
}

.field-order {
  display: flex;
  margin-left: auto;
}

.field-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0 12px;
}

.field-flags {
  flex-wrap: wrap;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.issue-list {
  margin: 10px 0 0;
  color: var(--aidt-danger);
}

.impact-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
  margin: 14px 0;
}

.impact-grid article {
  display: grid;
  gap: 4px;
  border: 1px solid var(--aidt-border);
  border-radius: 12px;
  padding: 12px;
}

.impact-grid strong {
  color: var(--aidt-brand-strong);
  font-size: 1.4rem;
}

.impact-grid span,
.muted {
  color: var(--aidt-ink-muted);
  font-size: 0.75rem;
}

.preview-shell {
  display: grid;
  gap: 18px;
}

.preview-heading {
  border-bottom: 1px solid var(--aidt-border);
  padding-bottom: 12px;
}

.preview-heading h2,
.preview-heading p {
  margin: 8px 0 0;
}

.preview-heading p {
  color: var(--aidt-ink-muted);
}

.state-card {
  border: 1px solid #edbbb5;
  border-radius: 14px;
  background: var(--aidt-danger-soft);
  padding: 20px;
  color: var(--aidt-danger);
}

@media (max-width: 900px) {
  .modeling-grid {
    grid-template-columns: 1fr;
  }

  .type-list {
    position: static;
    max-height: none;
  }

  .type-list-body {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .modeling-toolbar {
    flex-direction: column;
  }

  .toolbar-actions {
    justify-content: flex-start;
  }

  .field-grid,
  .impact-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 600px) {
  .modeling-head,
  .field-card-head {
    align-items: stretch;
    flex-direction: column;
  }

  .type-list-body,
  .field-grid,
  .impact-grid {
    grid-template-columns: 1fr;
  }

  .field-order {
    margin-left: 0;
  }
}
</style>
