<script setup lang="ts">
import StatusBadge from '@/components/base/StatusBadge.vue'

defineProps<{
  title: string
  description: string
  status?: string
  tone?: 'success' | 'warning' | 'danger' | 'neutral' | 'info'
}>()
</script>

<template>
  <section class="feature-placeholder">
    <div class="feature-heading">
      <div>
        <p class="feature-eyebrow">正式工程骨架</p>
        <h2>{{ title }}</h2>
      </div>
      <StatusBadge :label="status ?? '未连接'" :tone="tone ?? 'warning'" />
    </div>
    <p>{{ description }}</p>
    <slot />
  </section>
</template>

<style scoped>
.feature-placeholder {
  border: 1px dashed #bfcfc6;
  border-radius: var(--aidt-radius-md);
  background:
    linear-gradient(135deg, rgb(255 255 255 / 96%), rgb(237 244 239 / 86%));
  padding: 22px;
}

.feature-heading {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
}

.feature-eyebrow {
  margin: 0 0 5px;
  color: var(--aidt-brand);
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.1em;
}

h2 {
  margin: 0;
  font-size: 1.05rem;
}

p {
  margin: 12px 0 0;
}
</style>
