<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref, watch } from 'vue'
import { RouterLink } from 'vue-router'
import type { IntroLink } from '@/features/workbench-intro/introCatalog'

const props = defineProps<{
  open: boolean
  title: string
  purpose: string
  whenToUse: string
  examples: string[]
  boundaries: string[]
  audiences?: string[]
  steps?: string[]
  dataSources?: string[]
  permissions?: string[]
  nextStep?: string
  links?: IntroLink[]
}>()

const emit = defineEmits<{
  close: []
}>()

const closeButton = ref<HTMLButtonElement | null>(null)
const titleId = computed(
  () => `workbench-intro-title-${props.title.replace(/[^\p{L}\p{N}]+/gu, '-')}`,
)
let previousBodyOverflow = ''

async function close() {
  emit('close')
  await nextTick()
}

function onKeydown(event: KeyboardEvent) {
  if (event.key === 'Escape') {
    event.preventDefault()
    void close()
  }
}

watch(
  () => props.open,
  async (open) => {
    if (!open) {
      document.body.style.overflow = previousBodyOverflow
      return
    }
    previousBodyOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    await nextTick()
    closeButton.value?.focus()
  },
  { immediate: true },
)

onBeforeUnmount(() => {
  document.body.style.overflow = previousBodyOverflow
})
</script>

<template>
  <Teleport to="body">
    <div
      v-if="open"
      class="workbench-intro-layer"
      data-testid="workbench-intro-drawer"
      @click.self="close"
    >
      <section
        class="workbench-intro-drawer"
        role="dialog"
        aria-modal="true"
        :aria-labelledby="titleId"
        @keydown="onKeydown"
      >
        <header class="intro-header">
          <div>
            <p class="intro-kicker">页面介绍</p>
            <h2 :id="titleId">{{ title }}</h2>
          </div>
          <button
            ref="closeButton"
            class="intro-close"
            type="button"
            aria-label="关闭介绍"
            data-testid="workbench-intro-close"
            @click="close"
          >
            ×
          </button>
        </header>

        <div class="intro-body">
          <section>
            <h3>这个页面做什么</h3>
            <p>{{ purpose }}</p>
          </section>
          <section>
            <h3>什么时候使用</h3>
            <p>{{ whenToUse }}</p>
          </section>
          <section v-if="audiences?.length">
            <h3>适用岗位</h3>
            <ul>
              <li v-for="audience in audiences" :key="audience">{{ audience }}</li>
            </ul>
          </section>
          <section v-if="steps?.length">
            <h3>业务操作路径</h3>
            <ol>
              <li v-for="step in steps" :key="step">{{ step }}</li>
            </ol>
          </section>
          <section>
            <h3>业务例子</h3>
            <ol>
              <li v-for="example in examples" :key="example">{{ example }}</li>
            </ol>
          </section>
          <section v-if="dataSources?.length || permissions?.length">
            <h3>数据来源与权限范围</h3>
            <ul>
              <li v-for="source in dataSources ?? []" :key="source">{{ source }}</li>
              <li v-for="permission in permissions ?? []" :key="permission">{{ permission }}</li>
            </ul>
          </section>
          <section class="boundary-section">
            <h3>人工确认与安全边界</h3>
            <ul>
              <li v-for="boundary in boundaries" :key="boundary">{{ boundary }}</li>
            </ul>
          </section>
          <section v-if="nextStep">
            <h3>建议的下一步</h3>
            <p>{{ nextStep }}</p>
          </section>
          <section v-if="links?.length">
            <h3>相关入口</h3>
            <nav class="intro-links" aria-label="相关入口">
              <RouterLink
                v-for="link in links"
                :key="link.to"
                :to="link.to"
                @click="close"
              >
                {{ link.label }}
              </RouterLink>
            </nav>
          </section>
        </div>
      </section>
    </div>
  </Teleport>
</template>

<style scoped>
.workbench-intro-layer {
  position: fixed;
  z-index: 2200;
  inset: 0;
  background: rgb(11 28 21 / 34%);
}

.workbench-intro-drawer {
  position: absolute;
  inset: 0 0 0 auto;
  width: min(420px, 94vw);
  height: 100%;
  max-width: 100%;
  overflow-x: hidden;
  overflow-y: auto;
  border-left: 1px solid #d8e5dd;
  background: #fbfdfc;
  box-shadow: -18px 0 48px rgb(18 45 33 / 16%);
}

.intro-header {
  position: sticky;
  z-index: 1;
  top: 0;
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 16px;
  border-bottom: 1px solid var(--aidt-border);
  background: rgb(251 253 252 / 94%);
  padding: 22px 24px 18px;
  backdrop-filter: blur(10px);
}

.intro-kicker {
  margin: 0 0 4px;
  color: var(--aidt-brand);
  font-size: 0.7rem;
  font-weight: 800;
  letter-spacing: 0.1em;
}

.intro-header h2 {
  margin: 0;
  font-size: 1.35rem;
}

.intro-close {
  display: grid;
  width: 34px;
  height: 34px;
  flex: 0 0 auto;
  place-items: center;
  border: 1px solid var(--aidt-border);
  border-radius: 9px;
  background: #fff;
  color: var(--aidt-ink-muted);
  font-size: 1.2rem;
  cursor: pointer;
}

.intro-body {
  display: grid;
  gap: 20px;
  padding: 22px 24px 40px;
}

.intro-body section {
  border-bottom: 1px solid #e5ece8;
  padding-bottom: 18px;
}

.intro-body section:last-child {
  border-bottom: 0;
  padding-bottom: 0;
}

.intro-body h3 {
  margin: 0 0 8px;
  color: var(--aidt-ink);
  font-size: 0.92rem;
}

.intro-body p,
.intro-body li,
.intro-links a {
  color: var(--aidt-ink-muted);
  font-size: 0.82rem;
  line-height: 1.75;
  overflow-wrap: anywhere;
}

.intro-body p {
  margin: 0;
}

.intro-body ol,
.intro-body ul {
  display: grid;
  gap: 7px;
  margin: 0;
  padding-left: 20px;
}

.intro-links {
  display: grid;
  gap: 8px;
}

.intro-links a {
  border: 1px solid #d8e5dd;
  border-radius: 9px;
  background: #fff;
  color: var(--aidt-brand-strong);
  padding: 8px 10px;
  font-weight: 700;
  text-decoration: none;
}

.intro-links a:hover {
  border-color: #9fc6b2;
  background: var(--aidt-brand-soft);
}

.boundary-section {
  border: 1px solid #eadfbe;
  border-radius: 12px;
  background: #fffaf0;
  padding: 14px !important;
}

@media (max-width: 720px) {
  .workbench-intro-drawer {
    width: 100%;
    border-left: 0;
  }

  .intro-header {
    padding: 16px 18px 14px;
  }

  .intro-body {
    padding: 18px 18px 32px;
  }
}
</style>
