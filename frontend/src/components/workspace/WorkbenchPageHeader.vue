<script setup lang="ts">
import { ref } from 'vue'

defineProps<{
  title: string
  scopeLabel: string
  kicker?: string
  environmentLabel?: string
}>()

const emit = defineEmits<{
  intro: []
}>()

const introButton = ref<HTMLButtonElement | null>(null)

function focusIntro() {
  introButton.value?.focus()
}

defineExpose({ focusIntro })
</script>

<template>
  <header class="workbench-page-header" data-testid="workbench-page-header">
    <div class="workbench-page-heading">
      <p v-if="kicker" class="workbench-page-kicker">{{ kicker }}</p>
      <h1>{{ title }}</h1>
      <p class="workbench-scope">
        <span class="scope-dot" aria-hidden="true"></span>
        <span>{{ scopeLabel }}</span>
        <span v-if="environmentLabel" class="environment-label">{{ environmentLabel }}</span>
      </p>
    </div>
    <div class="workbench-page-actions">
      <slot name="actions"></slot>
      <button
        ref="introButton"
        class="workbench-intro-button"
        type="button"
        data-testid="workbench-intro-trigger"
        @click="emit('intro')"
      >
        <span aria-hidden="true">?</span>
        介绍
      </button>
    </div>
  </header>
</template>

<style scoped>
.workbench-page-header,
.workbench-page-actions {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.workbench-page-header {
  min-width: 0;
  align-items: center;
  justify-content: space-between;
}

.workbench-page-heading {
  min-width: 0;
}

.workbench-page-kicker {
  margin: 0 0 5px;
  color: var(--aidt-brand);
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.1em;
  text-transform: uppercase;
}

h1 {
  margin: 0;
  color: var(--aidt-ink);
  font-size: clamp(1.65rem, 2.5vw, 2.25rem);
  line-height: 1.16;
}

.workbench-scope {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 7px;
  margin: 7px 0 0;
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.environment-label {
  border: 1px solid #d7e6de;
  border-radius: 999px;
  background: #f5faf7;
  padding: 1px 7px;
  color: var(--aidt-brand-strong);
  font-size: 0.68rem;
  font-weight: 700;
}

.scope-dot {
  width: 7px;
  height: 7px;
  border-radius: 999px;
  background: #4f9d78;
  box-shadow: 0 0 0 4px rgb(79 157 120 / 12%);
}

.workbench-page-actions {
  flex: 0 0 auto;
  flex-wrap: wrap;
  align-items: center;
  justify-content: flex-end;
}

.workbench-intro-button {
  display: inline-flex;
  min-height: 34px;
  align-items: center;
  gap: 6px;
  border: 1px solid #cdded4;
  border-radius: 9px;
  background: #fff;
  color: var(--aidt-brand-strong);
  padding: 6px 11px;
  font-size: 0.78rem;
  font-weight: 700;
  cursor: pointer;
}

.workbench-intro-button span {
  display: grid;
  width: 17px;
  height: 17px;
  place-items: center;
  border-radius: 999px;
  background: var(--aidt-brand-soft);
  font-size: 0.68rem;
}

@media (max-width: 720px) {
  .workbench-page-header {
    align-items: stretch;
    flex-direction: column;
  }

  .workbench-page-actions {
    justify-content: flex-start;
  }
}
</style>
