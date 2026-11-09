<script setup lang="ts">
withDefaults(
  defineProps<{
    label: string
    tone?: 'success' | 'warning' | 'danger' | 'neutral' | 'info'
  }>(),
  { tone: 'neutral' },
)
</script>

<template>
  <span class="status-badge" :class="`tone-${tone}`">
    <span aria-hidden="true" class="dot"></span>
    {{ label }}
  </span>
</template>

<style scoped>
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 7px;
  border: 1px solid transparent;
  border-radius: 999px;
  padding: 4px 10px;
  font-size: 0.75rem;
  font-weight: 700;
  line-height: 1.2;
}

.dot {
  width: 7px;
  height: 7px;
  border-radius: 999px;
  background: currentColor;
}

.tone-success {
  border-color: #b9dac9;
  background: #e8f5ee;
  color: #176144;
}

.tone-warning {
  border-color: #efd19e;
  background: var(--aidt-warning-soft);
  color: var(--aidt-warning);
}

.tone-danger {
  border-color: #edbbb5;
  background: var(--aidt-danger-soft);
  color: var(--aidt-danger);
}

.tone-neutral {
  border-color: var(--aidt-border);
  background: #f5f7f6;
  color: #59655f;
}

.tone-info {
  border-color: #b8d5de;
  background: #e9f4f7;
  color: #256474;
}
</style>
