<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import type { WorkItemViewDto } from '@/api/types/contracts'
import UnifiedWorkItemCard from './UnifiedWorkItemCard.vue'

defineProps<{
  items: WorkItemViewDto[]
}>()

const emit = defineEmits<{
  open: [item: WorkItemViewDto]
}>()

const mobile = ref(false)
let mobileQuery: MediaQueryList | null = null

function updateMobile(event: MediaQueryListEvent | MediaQueryList) {
  mobile.value = event.matches
}

function formatDue(item: WorkItemViewDto): string {
  if (!item.dueAt) return '未设置'
  return new Intl.DateTimeFormat('zh-CN', {
    timeZone: 'Asia/Shanghai',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(item.dueAt))
}

function riskWarning(item: WorkItemViewDto): string {
  if (item.isPermanentHumanBoundary) {
    return '付款、质量放行、配方或设备控制须授权真人确认'
  }
  if (item.requiresConfirmation) return '请核对建议内容后由本人确认'
  if (item.riskLevel === 'High') return '高风险动作不得由数字员工代办'
  return ''
}

onMounted(() => {
  mobileQuery = window.matchMedia('(max-width: 760px)')
  updateMobile(mobileQuery)
  mobileQuery.addEventListener('change', updateMobile)
})

onBeforeUnmount(() => {
  mobileQuery?.removeEventListener('change', updateMobile)
})
</script>

<template>
  <div v-if="mobile" class="work-item-mobile-list" data-testid="work-item-mobile-list">
    <UnifiedWorkItemCard
      v-for="item in items"
      :key="`${item.sourceType}:${item.id}`"
      :item="item"
      compact
      @open="emit('open', $event)"
    />
  </div>
  <div v-else class="work-item-table-wrap" data-testid="work-item-table">
    <table>
      <thead>
        <tr>
          <th>事项</th>
          <th>来源</th>
          <th>当前节点</th>
          <th>负责人</th>
          <th>截止时间</th>
          <th>状态</th>
          <th>操作</th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="item in items"
          :key="`${item.sourceType}:${item.id}`"
          class="work-item-row"
          tabindex="0"
          :aria-label="`${item.businessTitle}，${item.nextAction}`"
          data-testid="unified-work-item"
          :data-source-type="item.sourceType"
          :data-source-id="item.id"
          @click="emit('open', item)"
          @keydown.enter.prevent="emit('open', item)"
          @keydown.space.prevent="emit('open', item)"
        >
          <td class="item-column">
            <strong>{{ item.businessTitle }}</strong>
            <small>{{ item.contextTitle }} · {{ item.departmentName }}</small>
          </td>
          <td><span class="source-chip">{{ item.sourceLabel }}</span></td>
          <td>{{ item.currentStage }}</td>
          <td>{{ item.assigneeDisplayName }}</td>
          <td>{{ formatDue(item) }}</td>
          <td><span class="status-chip">{{ item.statusLabel }}</span></td>
          <td class="action-column">
            <div class="row-action">
              <button type="button" @click.stop="emit('open', item)">
                {{ item.nextAction }} →
              </button>
            </div>
            <p v-if="riskWarning(item)" class="action-boundary-warning">
              {{ riskWarning(item) }}
            </p>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
.work-item-table-wrap {
  min-width: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: #fff;
}

table {
  width: 100%;
  border-collapse: collapse;
  table-layout: fixed;
}

th,
td {
  border-bottom: 1px solid #e4ebe7;
  padding: 10px 12px;
  text-align: left;
  vertical-align: middle;
}

th {
  position: sticky;
  z-index: 1;
  top: 0;
  background: #f3f7f4;
  color: #68766f;
  font-size: 0.69rem;
  font-weight: 700;
  white-space: nowrap;
}

th:nth-child(1) { width: 27%; }
th:nth-child(2) { width: 9%; }
th:nth-child(3) { width: 12%; }
th:nth-child(4) { width: 10%; }
th:nth-child(5) { width: 12%; }
th:nth-child(6) { width: 9%; }
th:nth-child(7) { width: 21%; }

tbody tr:last-child td {
  border-bottom: 0;
}

.work-item-row {
  cursor: pointer;
  transition: background 120ms ease;
}

.work-item-row:hover,
.work-item-row:focus-visible {
  background: #f6fbf8;
  outline: none;
}

.item-column {
  min-width: 0;
}

.item-column strong,
.item-column small {
  display: block;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.item-column strong {
  color: var(--aidt-ink);
  font-size: 0.82rem;
}

.item-column small {
  margin-top: 3px;
  color: var(--aidt-ink-muted);
  font-size: 0.68rem;
}

td {
  color: #4d5d55;
  font-size: 0.75rem;
}

.source-chip,
.status-chip {
  display: inline-flex;
  max-width: 100%;
  overflow: hidden;
  border-radius: 999px;
  background: #edf5f0;
  color: var(--aidt-brand-strong);
  padding: 3px 7px;
  font-size: 0.66rem;
  font-weight: 700;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.status-chip {
  background: #f2f4f3;
  color: #5e6b64;
}

.row-action {
  display: flex;
  align-items: center;
  justify-content: flex-end;
}

.row-action button {
  border: 0;
  background: transparent;
  color: var(--aidt-brand);
  padding: 3px 0;
  font-size: 0.72rem;
  font-weight: 700;
  cursor: pointer;
}

.action-boundary-warning {
  margin: 4px 0 0;
  color: #9a5a0a;
  font-size: 0.64rem;
  line-height: 1.45;
  text-align: right;
}

.work-item-mobile-list {
  display: grid;
  min-width: 0;
  gap: 9px;
}
</style>
