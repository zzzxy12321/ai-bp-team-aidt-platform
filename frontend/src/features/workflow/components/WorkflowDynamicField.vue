<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import { fileApi } from '@/api/modules/fileApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  DepartmentDto,
  ObjectFieldUserOptionDto,
  WorkflowFormField,
  WorkflowTaskForm,
} from '@/api/types/contracts'

type FormValue = string | number | boolean | string[] | null

const props = withDefaults(defineProps<{
  field: WorkflowFormField
  departments?: DepartmentDto[]
  formContext?: WorkflowTaskForm | null
  preview?: boolean
}>(), {
  departments: () => [],
  formContext: null,
  preview: false,
})

const model = defineModel<FormValue>({ default: null })
const userOptions = ref<ObjectFieldUserOptionDto[]>([])
const userLoading = ref(false)
const uploading = ref(false)

const stringValue = computed({
  get: () => (typeof model.value === 'string' ? model.value : ''),
  set: (value: string) => { model.value = value },
})
const numberValue = computed({
  get: () => (typeof model.value === 'number' ? model.value : undefined),
  set: (value: number | undefined) => { model.value = value ?? null },
})
const booleanValue = computed({
  get: () => model.value === true || model.value === 'true',
  set: (value: boolean) => { model.value = value },
})
const arrayValue = computed({
  get: () => (Array.isArray(model.value) ? model.value : []),
  set: (value: string[]) => { model.value = value },
})
const attachmentIds = computed(() => arrayValue.value)

const disabled = computed(
  () => props.preview || props.field.readOnly || props.field.dataType === 'ReadOnlySystem',
)

async function loadUsers(query = '') {
  if (props.preview && userOptions.value.length) return
  userLoading.value = true
  try {
    userOptions.value = await workspaceApi.objectFieldUsers(
      query,
      props.formContext?.departmentId,
    )
  } finally {
    userLoading.value = false
  }
}

async function upload(event: Event) {
  const input = event.target as HTMLInputElement
  const file = input.files?.[0]
  if (!file || !props.formContext?.taskId) return
  uploading.value = true
  try {
    const uploaded = await fileApi.upload({
      file,
      departmentId: props.formContext.departmentId,
      businessObjectId: props.formContext.businessObjectId,
      workflowInstanceId: props.formContext.workflowInstanceId,
      workflowTaskId: props.formContext.taskId,
      sensitivity: props.field.sensitivity === 'Public' ? 'Internal' : props.field.sensitivity,
    })
    arrayValue.value = [...new Set([...attachmentIds.value, uploaded.id])]
    ElMessage.success('附件已上传并加入节点表单')
  } catch {
    ElMessage.error('附件上传失败，请检查文件类型、大小和权限')
  } finally {
    uploading.value = false
    input.value = ''
  }
}

watch(
  () => props.field.dataType,
  (type) => {
    if (type === 'User' || type === 'MultiUser') void loadUsers()
  },
  { immediate: true },
)

onMounted(() => {
  if (model.value === null && props.field.defaultValue) {
    const type = props.field.dataType
    if (type === 'MultiSelect' || type === 'MultiUser' || type === 'Attachment') {
      try {
        const parsed = JSON.parse(props.field.defaultValue)
        model.value = Array.isArray(parsed) ? parsed.map(String) : []
      } catch {
        model.value = props.field.defaultValue.split(',').map((item) => item.trim()).filter(Boolean)
      }
    } else if (type === 'Number' || type === 'Money' || type === 'Percent') {
      const parsed = Number(props.field.defaultValue)
      model.value = Number.isFinite(parsed) ? parsed : null
    } else if (type === 'Boolean') {
      model.value = props.field.defaultValue === 'true'
    } else {
      model.value = props.field.defaultValue
    }
  }
})
</script>

<template>
  <div class="workflow-field" :class="[`width-${field.width.toLowerCase()}`]">
    <label :for="`workflow-field-${field.key}`">
      {{ field.displayName }}
      <span v-if="field.required" class="required">*</span>
    </label>

    <el-input
      v-if="field.dataType === 'Text'"
      :id="`workflow-field-${field.key}`"
      v-model="stringValue"
      :disabled="disabled"
      maxlength="2000"
      show-word-limit
    />
    <el-input
      v-else-if="field.dataType === 'LongText'"
      :id="`workflow-field-${field.key}`"
      v-model="stringValue"
      type="textarea"
      :rows="4"
      :disabled="disabled"
      maxlength="20000"
      show-word-limit
    />
    <el-input-number
      v-else-if="['Number', 'Money', 'Percent'].includes(field.dataType)"
      v-model="numberValue"
      :disabled="disabled"
      :controls="false"
      :precision="field.dataType === 'Money' ? 2 : undefined"
      style="width: 100%"
    />
    <el-select
      v-else-if="field.dataType === 'Select'"
      :id="`workflow-field-${field.key}`"
      v-model="stringValue"
      :disabled="disabled"
      clearable
      filterable
      style="width: 100%"
    >
      <el-option
        v-for="option in field.options ?? []"
        :key="option"
        :label="option"
        :value="option"
      />
    </el-select>
    <el-select
      v-else-if="field.dataType === 'MultiSelect'"
      :id="`workflow-field-${field.key}`"
      v-model="arrayValue"
      :disabled="disabled"
      multiple
      filterable
      style="width: 100%"
    >
      <el-option
        v-for="option in field.options ?? []"
        :key="option"
        :label="option"
        :value="option"
      />
    </el-select>
    <el-select
      v-else-if="field.dataType === 'User'"
      :id="`workflow-field-${field.key}`"
      v-model="stringValue"
      :disabled="disabled"
      filterable
      remote
      clearable
      :remote-method="loadUsers"
      :loading="userLoading"
      placeholder="搜索人员姓名或账号"
      style="width: 100%"
    >
      <el-option
        v-for="user in userOptions"
        :key="user.id"
        :label="`${user.displayName} · ${user.departmentName}`"
        :value="user.id"
      />
    </el-select>
    <el-select
      v-else-if="field.dataType === 'MultiUser'"
      :id="`workflow-field-${field.key}`"
      v-model="arrayValue"
      :disabled="disabled"
      filterable
      remote
      multiple
      :remote-method="loadUsers"
      :loading="userLoading"
      placeholder="搜索人员姓名或账号"
      style="width: 100%"
    >
      <el-option
        v-for="user in userOptions"
        :key="user.id"
        :label="`${user.displayName} · ${user.departmentName}`"
        :value="user.id"
      />
    </el-select>
    <el-select
      v-else-if="field.dataType === 'Department'"
      :id="`workflow-field-${field.key}`"
      v-model="stringValue"
      :disabled="disabled"
      clearable
      filterable
      style="width: 100%"
    >
      <el-option
        v-for="department in departments"
        :key="department.id"
        :label="department.name"
        :value="department.id"
      />
    </el-select>
    <el-date-picker
      v-else-if="field.dataType === 'Date'"
      :id="`workflow-field-${field.key}`"
      v-model="stringValue"
      type="date"
      value-format="YYYY-MM-DD"
      :disabled="disabled"
      style="width: 100%"
    />
    <el-date-picker
      v-else-if="field.dataType === 'DateTime'"
      :id="`workflow-field-${field.key}`"
      v-model="stringValue"
      type="datetime"
      value-format="YYYY-MM-DDTHH:mm:ssZ"
      :disabled="disabled"
      style="width: 100%"
    />
    <el-date-picker
      v-else-if="field.dataType === 'DateRange'"
      :id="`workflow-field-${field.key}`"
      v-model="arrayValue"
      type="daterange"
      value-format="YYYY-MM-DD"
      :disabled="disabled"
      style="width: 100%"
    />
    <el-switch
      v-else-if="field.dataType === 'Boolean'"
      v-model="booleanValue"
      :disabled="disabled"
      active-text="是"
      inactive-text="否"
    />
    <div
      v-else-if="field.dataType === 'Attachment' || field.dataType === 'BusinessObjectField' || field.dataType === 'ReadOnlySystem'"
      class="special-field"
    >
      <template v-if="field.dataType === 'Attachment'">
        <el-tag v-for="attachmentId in attachmentIds" :key="attachmentId" type="info" effect="plain">
          {{ attachmentId.slice(0, 8) }}
        </el-tag>
        <label v-if="!preview" class="upload-button">
          {{ uploading ? '上传中…' : '上传附件' }}
          <input type="file" hidden :disabled="uploading" @change="upload" />
        </label>
      </template>
      <span v-else-if="field.dataType === 'BusinessObjectField'" class="readonly-copy">
        引用业务对象字段：{{ field.boundObjectFieldCode || '未绑定' }}
      </span>
      <span v-else class="readonly-copy">
        只读系统字段：{{ field.boundObjectFieldCode || field.key }}
      </span>
    </div>

    <p v-if="field.helpText" class="help-copy">{{ field.helpText }}</p>
    <div class="field-meta">
      <span>{{ field.dataSource === 'TaskInput' ? '本节点填写' : field.dataSource === 'EvidenceReference' ? '关联证据' : '业务对象引用' }}</span>
      <span v-if="field.sensitivity === 'Sensitive'">敏感</span>
      <span v-if="field.sensitivity === 'Core'">核心</span>
      <span v-if="field.readOnly || field.dataType === 'ReadOnlySystem'">只读</span>
    </div>
  </div>
</template>

<style scoped>
.workflow-field {
  display: grid;
  gap: 6px;
  min-width: 0;
}

.workflow-field label {
  color: var(--aidt-ink);
  font-size: 0.78rem;
  font-weight: 700;
}

.required {
  color: #b42318;
}

.width-full {
  grid-column: 1 / -1;
}

.width-half {
  grid-column: span 1;
}

.width-third {
  grid-column: span 1;
}

.special-field {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
  min-height: 34px;
  border: 1px dashed var(--aidt-border);
  border-radius: 10px;
  padding: 8px;
}

.upload-button {
  cursor: pointer;
  border-radius: 8px;
  background: #e8f3ed;
  color: #155f45;
  padding: 6px 10px;
}

.readonly-copy,
.help-copy,
.field-meta {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.field-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

@media (max-width: 640px) {
  .width-half,
  .width-third {
    grid-column: 1 / -1;
  }
}
</style>
