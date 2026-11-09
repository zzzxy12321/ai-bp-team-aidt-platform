<script setup lang="ts">
import type {
  DepartmentDto,
  WorkflowTaskForm,
} from '@/api/types/contracts'
import WorkflowDynamicField from './WorkflowDynamicField.vue'

type FormValue = string | number | boolean | string[] | null

withDefaults(defineProps<{
  form: WorkflowTaskForm
  departments?: DepartmentDto[]
  preview?: boolean
}>(), {
  departments: () => [],
  preview: false,
})

const model = defineModel<Record<string, FormValue>>({ required: true })
</script>

<template>
  <section class="workflow-form" :class="{ preview }">
    <header>
      <div>
        <span class="form-kicker">{{ preview ? '员工表单预览' : '本节点业务表单' }}</span>
        <h3>{{ form.nodeName }}</h3>
      </div>
      <span class="form-version">表单 {{ form.formSchemaVersion }}</span>
    </header>
    <p v-if="form.businessDescription" class="description">{{ form.businessDescription }}</p>
    <el-alert
      v-if="form.permanentHumanBoundary"
      type="warning"
      :closable="false"
      show-icon
      title="永久人工边界：系统只提供材料、校验和建议，不能代替真人签认。"
    />
    <div class="form-grid">
      <WorkflowDynamicField
        v-for="field in form.fields"
        :key="field.key"
        v-model="model[field.key]"
        :field="field"
        :departments="departments"
        :form-context="form"
        :preview="preview"
      />
    </div>
    <el-empty v-if="form.fields.length === 0" description="本节点没有人工填写字段" />
    <footer v-if="form.actions.length">
      <el-tag v-for="action in form.actions" :key="action.code" type="info" effect="plain">
        {{ action.name }}
      </el-tag>
    </footer>
  </section>
</template>

<style scoped>
.workflow-form {
  display: grid;
  gap: 14px;
  border: 1px solid var(--aidt-border);
  border-radius: 16px;
  background: #fbfdfb;
  padding: 16px;
}

.workflow-form.preview {
  background: #fff;
}

.workflow-form header,
.workflow-form footer {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.workflow-form h3,
.workflow-form p {
  margin: 0;
}

.form-kicker,
.form-version,
.description {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.description {
  line-height: 1.65;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 14px;
}

@media (max-width: 640px) {
  .form-grid {
    grid-template-columns: 1fr;
  }
}
</style>
