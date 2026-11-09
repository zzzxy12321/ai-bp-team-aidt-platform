<script setup lang="ts">
import { computed } from 'vue'
import type { WorkItemViewDto } from '@/api/types/contracts'

const props = defineProps<{
  item: WorkItemViewDto
  compact?: boolean
}>()

defineEmits<{
  open: [item: WorkItemViewDto]
}>()

const initials: Record<WorkItemViewDto['sourceType'], string> = {
  WorkflowTask: '流',
  ProjectTask: '项',
  EventInbox: '事',
  MinutesActionItem: '会',
  McpHumanConfirmation: '智',
  AgentRun: 'AI',
}

const dueText = computed(() => {
  if (!props.item.dueAt) return '未设截止'
  const due = new Date(props.item.dueAt)
  const now = new Date()
  if (due.getTime() < now.getTime()) return `已逾期 · ${formatDate(due)}`
  if (sameShanghaiDate(due, now)) return `今天 · ${formatTime(due)}`
  return formatDate(due)
})

function sameShanghaiDate(left: Date, right: Date): boolean {
  const format = new Intl.DateTimeFormat('zh-CN', {
    timeZone: 'Asia/Shanghai',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  })
  return format.format(left) === format.format(right)
}

function formatDate(value: Date): string {
  return new Intl.DateTimeFormat('zh-CN', {
    timeZone: 'Asia/Shanghai',
    month: '2-digit',
    day: '2-digit',
  }).format(value)
}

function formatTime(value: Date): string {
  return new Intl.DateTimeFormat('zh-CN', {
    timeZone: 'Asia/Shanghai',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(value)
}

function open() {
  return props.item
}

function priorityLabel(value: string): string {
  return {
    PermanentHumanBoundary: '人工边界',
    Overdue: '已逾期',
    DueToday: '今天到期',
    AwaitingConfirmation: '待我确认',
    ThisWeek: '本周',
    Other: '常规',
  }[value] ?? '待处理'
}
</script>

<template>
  <article
    class="work-item-card"
    :class="[`risk-${item.riskLevel.toLowerCase()}`, { compact }]"
    role="button"
    tabindex="0"
    :aria-label="`${item.sourceLabel}：${item.businessTitle}，${item.nextAction}`"
    data-testid="unified-work-item"
    :data-source-type="item.sourceType"
    :data-source-id="item.id"
    @click="$emit('open', open())"
    @keydown.enter.prevent="$emit('open', open())"
    @keydown.space.prevent="$emit('open', open())"
  >
    <div class="source-mark" aria-hidden="true">{{ initials[item.sourceType] }}</div>
    <div class="item-main">
      <div class="item-topline">
        <span class="source-label">{{ item.sourceLabel }}</span>
        <span class="priority-tag" :class="`priority-${item.priorityGroup.toLowerCase()}`">
          {{ priorityLabel(item.priorityGroup) }}
        </span>
      </div>
      <h3>{{ item.businessTitle }}</h3>
      <p class="context-line">
        <span>{{ item.contextTitle }}</span>
        <span aria-hidden="true">·</span>
        <span>{{ item.departmentName }}</span>
      </p>
      <div class="meta-line">
        <span>{{ item.currentStage }}</span>
        <span>负责人：{{ item.assigneeDisplayName }}</span>
        <span>截止：{{ dueText }}</span>
        <span v-if="item.sourceCount > 1">{{ item.sourceCount }} 个来源</span>
      </div>
    </div>
    <div class="item-action">
      <span class="risk-pill" :class="`risk-${item.riskLevel.toLowerCase()}`">{{ item.riskLabel }}</span>
      <el-button text type="primary">{{ item.nextAction }} →</el-button>
    </div>
  </article>
</template>

<style scoped>
.work-item-card {
  display: grid;
  min-width: 0;
  grid-template-columns: 42px minmax(0, 1fr) auto;
  align-items: center;
  gap: 14px;
  border: 1px solid var(--aidt-border);
  border-left: 4px solid #9dcbb2;
  border-radius: 16px;
  background: #fff;
  padding: 15px 16px;
  cursor: pointer;
  transition: transform 160ms ease, border-color 160ms ease, box-shadow 160ms ease;
}

.work-item-card:hover,
.work-item-card:focus-visible {
  transform: translateY(-1px);
  border-color: #a9c9b8;
  box-shadow: 0 12px 28px rgb(21 72 52 / 8%);
  outline: none;
}

.work-item-card.risk-high { border-left-color: #b44b43; }
.work-item-card.risk-medium { border-left-color: #c88b37; }

.source-mark {
  display: grid;
  width: 40px;
  height: 40px;
  place-items: center;
  border-radius: 13px;
  background: #e9f3ed;
  color: var(--aidt-brand);
  font-size: 0.76rem;
  font-weight: 900;
}

.item-main { min-width: 0; }
.item-topline, .meta-line { display: flex; align-items: center; flex-wrap: wrap; gap: 8px; }
.source-label {
  color: var(--aidt-brand);
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
}

.priority-tag, .risk-pill {
  border: 1px solid var(--aidt-border);
  border-radius: 999px;
  padding: 3px 8px;
  color: var(--aidt-ink-muted);
  font-size: 0.65rem;
  font-weight: 700;
  white-space: nowrap;
}

.priority-permanenthumanboundary,
.priority-overdue,
.risk-high {
  border-color: #eabdb8;
  background: var(--aidt-danger-soft);
  color: var(--aidt-danger);
}

.priority-duetoday,
.priority-awaitingconfirmation,
.risk-medium {
  border-color: #ecd09d;
  background: var(--aidt-warning-soft);
  color: var(--aidt-warning);
}

.risk-pill.risk-low {
  border-color: #bcdccb;
  background: #e8f5ee;
  color: #176144;
}

h3 {
  overflow: hidden;
  margin: 6px 0 4px;
  font-size: 0.98rem;
  line-height: 1.45;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.context-line, .meta-line {
  margin: 0;
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.meta-line { margin-top: 7px; gap: 5px 12px; }
.item-action { display: grid; justify-items: end; gap: 5px; }

.compact { padding: 12px 14px; }
.compact .source-mark { width: 34px; height: 34px; }

@media (max-width: 640px) {
  .work-item-card {
    grid-template-columns: 38px minmax(0, 1fr);
    align-items: start;
  }

  .source-mark { width: 36px; height: 36px; }
  .item-action {
    grid-column: 2;
    grid-template-columns: 1fr auto;
    align-items: center;
    justify-items: start;
    width: 100%;
  }

  h3 { white-space: normal; }
}
</style>
