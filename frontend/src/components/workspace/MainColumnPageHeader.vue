<script setup lang="ts">
import { computed, nextTick, ref } from 'vue'
import WorkbenchIntroDrawer from './WorkbenchIntroDrawer.vue'
import WorkbenchPageHeader from './WorkbenchPageHeader.vue'
import {
  introCatalog,
  type MainColumnIntroKey,
} from '@/features/workbench-intro/introCatalog'

const props = defineProps<{
  introKey: MainColumnIntroKey
  title?: string
  scopeLabel: string
  kicker?: string
  environmentLabel?: string
}>()

const introOpen = ref(false)
const pageHeader = ref<InstanceType<typeof WorkbenchPageHeader> | null>(null)
const intro = computed(() => introCatalog[props.introKey])
const pageTitle = computed(() => props.title ?? intro.value.title)

function openIntro() {
  introOpen.value = true
}

async function closeIntro() {
  introOpen.value = false
  await nextTick()
  pageHeader.value?.focusIntro()
}

defineExpose({
  openIntro,
  closeIntro,
})
</script>

<template>
  <WorkbenchPageHeader
    ref="pageHeader"
    :title="pageTitle"
    :scope-label="scopeLabel"
    :kicker="kicker"
    :environment-label="environmentLabel ?? '演示环境'"
    @intro="openIntro"
  >
    <template #actions>
      <slot name="actions"></slot>
    </template>
  </WorkbenchPageHeader>

  <WorkbenchIntroDrawer
    :open="introOpen"
    :title="intro.title"
    :purpose="intro.purpose"
    :audiences="intro.audiences"
    :when-to-use="intro.timing"
    :steps="intro.steps"
    :examples="intro.examples"
    :data-sources="intro.dataSources"
    :permissions="intro.permissions"
    :boundaries="intro.boundaries"
    :next-step="intro.nextStep"
    :links="intro.links"
    @close="closeIntro"
  />
</template>
