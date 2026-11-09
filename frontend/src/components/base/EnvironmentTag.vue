<script setup lang="ts">
import { computed } from 'vue'
import type { DataEnvironmentCode } from '@/api/types/contracts'

const props = defineProps<{
  code: DataEnvironmentCode
  isTestData?: boolean
}>()

const label = computed(() => {
  const name = { demo: '演示环境', test: '测试环境', prod: '生产环境' }[props.code]
  return props.isTestData && props.code !== 'prod' ? `${name} · 测试数据` : name
})

const tone = computed(() => {
  if (props.code === 'prod') return 'danger'
  return props.code === 'demo' ? 'warning' : 'info'
})
</script>

<template>
  <el-tag :type="tone" effect="plain" size="small">{{ label }}</el-tag>
</template>
