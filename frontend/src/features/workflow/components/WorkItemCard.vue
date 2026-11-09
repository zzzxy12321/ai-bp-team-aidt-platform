<script setup lang="ts">
import EnvironmentTag from '@/components/base/EnvironmentTag.vue'
import type { WorkItemDto } from '@/api/types/contracts'

defineProps<{
  item: WorkItemDto
}>()

defineEmits<{
  open: [item: WorkItemDto]
}>()
</script>

<template>
  <article class="work-card">
    <div class="work-card-head">
      <span class="work-kind">{{ item.kind === 'Task' ? '待办' : '流程' }}</span>
      <EnvironmentTag :code="item.environmentCode" :is-test-data="item.isTestData" />
    </div>
    <h3>{{ item.title }}</h3>
    <dl>
      <div>
        <dt>当前步骤</dt>
        <dd>{{ item.currentStep }}</dd>
      </div>
      <div>
        <dt>负责人</dt>
        <dd>{{ item.assigneeDisplayName || '当前部门' }}</dd>
      </div>
      <div>
        <dt>状态</dt>
        <dd>{{ item.nextAction }}</dd>
      </div>
    </dl>
    <el-button text type="primary" @click="$emit('open', item)">
      {{ item.nextAction }}
    </el-button>
  </article>
</template>

<style scoped>
.work-card {
  min-width: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 16px;
  background: #fff;
  padding: 17px;
  box-shadow: var(--aidt-shadow);
}

.work-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.work-kind {
  color: var(--aidt-brand);
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.08em;
}

h3 {
  margin: 13px 0;
  font-size: 1rem;
  line-height: 1.5;
}

dl {
  display: grid;
  gap: 8px;
  margin: 0 0 10px;
}

dl div {
  display: grid;
  grid-template-columns: 72px minmax(0, 1fr);
  gap: 10px;
}

dt {
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

dd {
  overflow: hidden;
  margin: 0;
  font-size: 0.82rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
