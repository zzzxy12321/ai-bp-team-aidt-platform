<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  dueAt: string | null
  status: string
  decisionMode: string
}>()

const dueLabel = computed(() =>
  props.dueAt ? new Date(props.dueAt).toLocaleString() : '未配置 SLA',
)
</script>

<template>
  <span class="sla-line">
    <el-tag effect="plain" size="small">{{ status }}</el-tag>
    <span>截止 {{ dueLabel }}</span>
    <strong v-if="decisionMode !== 'Human'">{{ decisionMode }}</strong>
  </span>
</template>

<style scoped>
.sla-line {
  display: flex;
  align-items: center;
  gap: 7px;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.sla-line strong {
  color: var(--aidt-brand-strong);
}
</style>
