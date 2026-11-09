<script setup lang="ts">
import { computed } from 'vue'

export type WorkbenchStatItem = {
  label: string
  value: number | string
  helper: string
  tone?: 'default' | 'primary' | 'warning'
  testId?: string
}

const props = defineProps<{
  items: WorkbenchStatItem[]
  loading?: boolean
}>()

const visibleItems = computed(() => props.items.slice(0, 5))
</script>

<template>
  <section
    v-loading="loading"
    class="workbench-stats"
    :class="`stat-count-${visibleItems.length}`"
    aria-label="关键统计"
    data-testid="workbench-stats"
  >
    <article
      v-for="item in visibleItems"
      :key="item.label"
      class="workbench-stat"
      :class="item.tone ? `tone-${item.tone}` : undefined"
      :data-testid="item.testId"
    >
      <span>{{ item.label }}</span>
      <strong>{{ loading ? '—' : item.value }}</strong>
      <small>{{ item.helper }}</small>
    </article>
  </section>
</template>

<style scoped>
.workbench-stats {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
  min-height: 94px;
}

.workbench-stats.stat-count-3 {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.workbench-stats.stat-count-5 {
  grid-template-columns: repeat(5, minmax(0, 1fr));
}

.workbench-stat {
  min-width: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 13px;
  background: rgb(255 255 255 / 94%);
  padding: 14px 16px;
}

.workbench-stat.tone-primary {
  border-color: #a9cdb9;
  background: linear-gradient(145deg, #edf7f1, #fbfdfb);
}

.workbench-stat.tone-warning {
  border-color: #ead8ad;
  background: #fff8e9;
}

.workbench-stat span,
.workbench-stat strong,
.workbench-stat small {
  display: block;
}

.workbench-stat span,
.workbench-stat small {
  overflow: hidden;
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.workbench-stat strong {
  margin: 5px 0 2px;
  color: var(--aidt-brand-strong);
  font-size: 1.62rem;
  line-height: 1;
}

@media (max-width: 1080px) {
  .workbench-stats,
  .workbench-stats.stat-count-5 {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 720px) {
  .workbench-stats,
  .workbench-stats.stat-count-3,
  .workbench-stats.stat-count-5 {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .workbench-stat:last-child:nth-child(odd) {
    grid-column: 1 / -1;
  }
}
</style>
