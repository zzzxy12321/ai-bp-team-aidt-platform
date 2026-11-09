<script setup lang="ts">
import { nextTick, onBeforeUnmount, onMounted, ref } from 'vue'

defineProps<{
  resultLabel: string
}>()

const isNarrow = ref(false)
const filtersOpen = ref(false)
const toggleButton = ref<HTMLButtonElement | null>(null)
const filterPanel = ref<HTMLElement | null>(null)
let narrowQuery: MediaQueryList | null = null

function updateNarrowMode(event: MediaQueryListEvent | MediaQueryList) {
  isNarrow.value = event.matches
  if (!event.matches) filtersOpen.value = false
}

async function openFilters() {
  filtersOpen.value = true
  await nextTick()
  filterPanel.value?.querySelector<HTMLElement>('input, select, button, textarea')?.focus()
}

async function closeFilters() {
  filtersOpen.value = false
  await nextTick()
  toggleButton.value?.focus()
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape' && filtersOpen.value) {
    event.preventDefault()
    void closeFilters()
  }
}

onMounted(() => {
  narrowQuery = window.matchMedia('(max-width: 760px)')
  updateNarrowMode(narrowQuery)
  narrowQuery.addEventListener('change', updateNarrowMode)
  window.addEventListener('keydown', onKeydown)
})

onBeforeUnmount(() => {
  narrowQuery?.removeEventListener('change', updateNarrowMode)
  window.removeEventListener('keydown', onKeydown)
})
</script>

<template>
  <section class="workbench-filter-bar" data-testid="workbench-filter-bar">
    <button
      v-if="isNarrow && filtersOpen"
      class="filter-backdrop"
      type="button"
      aria-label="关闭筛选"
      data-testid="workbench-filter-backdrop"
      @click="closeFilters"
    ></button>
    <div class="workbench-search-slot">
      <slot name="search"></slot>
    </div>
    <button
      ref="toggleButton"
      class="mobile-filter-toggle"
      type="button"
      :aria-expanded="filtersOpen"
      data-testid="workbench-filter-trigger"
      @click="openFilters"
    >
      筛选
    </button>
    <div
      v-show="!isNarrow || filtersOpen"
      ref="filterPanel"
      class="workbench-filters"
      :class="{ 'mobile-open': filtersOpen }"
      data-testid="workbench-filters"
    >
      <slot></slot>
      <div class="filter-footer">
        <slot name="actions"></slot>
        <span class="result-count" data-testid="workbench-result-count">{{ resultLabel }}</span>
        <button
          v-if="isNarrow"
          class="mobile-filter-close"
          type="button"
          @click="closeFilters"
        >完成</button>
      </div>
    </div>
  </section>
</template>

<style scoped>
.workbench-filter-bar {
  display: flex;
  min-width: 0;
  align-items: center;
  gap: 10px;
  border: 1px solid var(--aidt-border);
  border-radius: 13px;
  background: rgb(255 255 255 / 94%);
  padding: 11px 12px;
}

.workbench-search-slot {
  width: min(360px, 32vw);
  min-width: 190px;
}

.workbench-filters {
  display: flex;
  flex: 1 1 auto;
  min-width: 0;
  align-items: center;
  gap: 8px;
}

.workbench-filters :deep(.el-select),
.workbench-filters :deep(.el-date-editor) {
  width: 138px;
}

.filter-footer {
  display: flex;
  min-width: max-content;
  align-items: center;
  gap: 8px;
  margin-left: auto;
}

.result-count {
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
  white-space: nowrap;
}

.mobile-filter-toggle,
.mobile-filter-close {
  display: none;
}

.filter-backdrop {
  display: none;
}

@media (max-width: 760px) {
  .workbench-filter-bar {
    align-items: stretch;
    flex-wrap: wrap;
    padding: 10px;
  }

  .workbench-search-slot {
    width: auto;
    min-width: 0;
    flex: 1 1 auto;
  }

  .mobile-filter-toggle,
  .mobile-filter-close {
    display: inline-flex;
    min-height: 32px;
    align-items: center;
    justify-content: center;
    border: 1px solid #cdded4;
    border-radius: 9px;
    background: #fff;
    color: var(--aidt-brand-strong);
    padding: 5px 12px;
    cursor: pointer;
  }

  .filter-backdrop {
    position: fixed;
    z-index: 2050;
    inset: 0;
    display: block;
    width: 100%;
    height: 100%;
    border: 0;
    background: rgb(10 31 23 / 34%);
  }

  .workbench-filters.mobile-open {
    position: fixed;
    z-index: 2100;
    inset: auto 0 0;
    display: grid;
    width: 100%;
    max-height: min(76dvh, 620px);
    overflow-y: auto;
    border-top: 1px solid var(--aidt-border);
    border-radius: 18px 18px 0 0;
    background: #fff;
    padding: 18px;
    box-shadow: 0 -18px 44px rgb(19 45 33 / 18%);
  }

  .workbench-filters.mobile-open :deep(.el-select),
  .workbench-filters.mobile-open :deep(.el-date-editor) {
    width: 100%;
  }

  .filter-footer {
    flex-wrap: wrap;
    margin-left: 0;
  }
}
</style>
