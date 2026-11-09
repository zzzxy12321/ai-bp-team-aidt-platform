<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  DepartmentDto,
  ObjectFieldDto,
  ObjectFieldUserOptionDto,
} from '@/api/types/contracts'
import { fieldTypeLabels } from '@/features/object-model/objectModelRules'

const props = withDefaults(defineProps<{
  field: ObjectFieldDto
  departments?: DepartmentDto[]
  departmentId?: string
  disabled?: boolean
  coreMasked?: boolean
  compact?: boolean
  label?: string
}>(), {
  departments: () => [],
  departmentId: '',
  disabled: false,
  coreMasked: false,
  compact: false,
  label: '',
})

const model = defineModel<string | null>({ default: '' })
const userOptions = ref<ObjectFieldUserOptionDto[]>([])
const userLoading = ref(false)

const numberValue = computed({
  get: () => {
    if (model.value === null || model.value === '') return undefined
    const parsed = Number(model.value)
    return Number.isFinite(parsed) ? parsed : undefined
  },
  set: (value: number | undefined) => {
    model.value = value === undefined || value === null ? null : String(value)
  },
})

const fieldDisabled = computed(() => props.disabled || props.coreMasked)

async function searchUsers(query = '') {
  userLoading.value = true
  try {
    userOptions.value = await workspaceApi.objectFieldUsers(query, props.departmentId || undefined)
  } finally {
    userLoading.value = false
  }
}

watch(
  () => props.field.dataType,
  (type) => {
    if (type === 'User') void searchUsers()
  },
  { immediate: true },
)
</script>

<template>
  <div class="dynamic-field" :class="{ compact }">
    <el-input
      v-if="field.dataType === 'Text'"
      v-model="model"
      :disabled="fieldDisabled"
      :maxlength="field.maxLength"
      :aria-label="label || field.displayName"
      show-word-limit
    />
    <el-input
      v-else-if="field.dataType === 'LongText'"
      v-model="model"
      type="textarea"
      :rows="compact ? 2 : 4"
      :disabled="fieldDisabled"
      :maxlength="field.maxLength"
      :aria-label="label || field.displayName"
      show-word-limit
    />
    <el-input-number
      v-else-if="field.dataType === 'Number'"
      v-model="numberValue"
      :disabled="fieldDisabled"
      :controls="false"
      :aria-label="label || field.displayName"
      style="width: 100%"
    />
    <el-date-picker
      v-else-if="field.dataType === 'Date'"
      v-model="model"
      type="date"
      value-format="YYYY-MM-DD"
      :disabled="fieldDisabled"
      :aria-label="label || field.displayName"
      :disabled-date="() => false"
      style="width: 100%"
    />
    <el-date-picker
      v-else-if="field.dataType === 'DateTime'"
      v-model="model"
      type="datetime"
      value-format="YYYY-MM-DDTHH:mm:ssZ"
      :disabled="fieldDisabled"
      :aria-label="label || field.displayName"
      style="width: 100%"
    />
    <el-switch
      v-else-if="field.dataType === 'Boolean'"
      v-model="model"
      active-value="true"
      inactive-value="false"
      active-text="是"
      inactive-text="否"
      :disabled="fieldDisabled"
      :aria-label="label || field.displayName"
    />
    <el-select
      v-else-if="field.dataType === 'Select'"
      v-model="model"
      :disabled="fieldDisabled"
      :aria-label="label || field.displayName"
      filterable
      clearable
      style="width: 100%"
    >
      <el-option v-for="option in field.options" :key="option" :label="option" :value="option" />
    </el-select>
    <el-select
      v-else-if="field.dataType === 'User'"
      v-model="model"
      :disabled="fieldDisabled"
      filterable
      remote
      reserve-keyword
      clearable
      remote-show-suffix
      :remote-method="searchUsers"
      :loading="userLoading"
      placeholder="搜索姓名或账号"
      :aria-label="label || field.displayName"
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
      v-model="model"
      :disabled="fieldDisabled"
      filterable
      clearable
      placeholder="搜索部门"
      :aria-label="label || field.displayName"
      style="width: 100%"
    >
      <el-option
        v-for="department in departments"
        :key="department.id"
        :label="department.name"
        :value="department.id"
      />
    </el-select>
    <el-input
      v-else
      v-model="model"
      :disabled="fieldDisabled"
      :aria-label="label || field.displayName"
    />

    <div class="field-meta">
      <span>{{ fieldTypeLabels[field.dataType] ?? field.dataType }}</span>
      <span v-if="field.sensitivity === 'Sensitive'" class="sensitive">敏感字段</span>
      <span v-if="field.sensitivity === 'Core'" class="core">核心字段</span>
      <span v-if="coreMasked" class="core-mask">已按权限脱敏，编辑时保留原值</span>
    </div>
  </div>
</template>

<style scoped>
.dynamic-field {
  display: grid;
  gap: 5px;
}

.field-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
}

.sensitive {
  color: #a35b00;
}

.core,
.core-mask {
  color: #b42318;
  font-weight: 700;
}

.compact :deep(.el-form-item) {
  margin-bottom: 10px;
}
</style>
