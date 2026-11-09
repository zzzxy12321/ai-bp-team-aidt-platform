<script setup lang="ts">
import { onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import StatusBadge from '@/components/base/StatusBadge.vue'
import { usePlatformStore } from '@/stores/platform'
import type { CapabilityState } from '@/api/types/contracts'

const store = usePlatformStore()
const { capabilities, loadState, errorCode, errorMessage, traceId } = storeToRefs(store)

function tone(state: CapabilityState): 'success' | 'warning' | 'danger' | 'neutral' | 'info' {
  switch (state) {
    case 'available':
      return 'success'
    case 'blocked':
      return 'danger'
    case 'contract_ready':
      return 'info'
    case 'planned':
    case 'disconnected':
      return 'warning'
    default:
      return 'neutral'
  }
}

function label(state: CapabilityState): string {
  return {
    available: '可用',
    skeleton: '骨架',
    contract_ready: '契约就绪',
    planned: '待建设',
    blocked: 'SQL 阻断',
    disconnected: '未连接',
  }[state]
}

onMounted(() => {
  if (loadState.value === 'idle') {
    void store.loadCapabilities()
  }
})
</script>

<template>
  <section class="section-card capability-panel" aria-live="polite">
    <div class="panel-head">
      <div>
        <p class="page-kicker">真实能力状态</p>
        <h2>平台能力不是“全部完成”</h2>
      </div>
      <el-button
        :loading="loadState === 'loading'"
        type="primary"
        plain
        @click="store.loadCapabilities()"
      >
        刷新状态
      </el-button>
    </div>

    <div v-if="loadState === 'loading'" class="panel-state">
      <el-skeleton :rows="4" animated />
    </div>

    <el-alert
      v-else-if="loadState === 'error'"
      type="error"
      :closable="false"
      show-icon
      title="无法读取平台能力"
    >
      <p>{{ errorMessage }}</p>
      <p class="technical">错误码：{{ errorCode }} · TraceId：{{ traceId || '未返回' }}</p>
    </el-alert>

    <el-empty
      v-else-if="!capabilities"
      description="API 尚未返回能力数据"
    />

    <template v-else>
      <div class="project-strip">
        <div>
          <span>项目状态</span>
          <strong>{{ capabilities.projectState }}</strong>
        </div>
        <div>
          <span>数据库</span>
          <strong>{{ capabilities.databaseProvider }}</strong>
        </div>
        <div>
          <span>迁移状态</span>
          <strong>{{ capabilities.databaseMigrationState }}</strong>
        </div>
        <div>
          <span>外部系统</span>
          <strong>{{ capabilities.externalSystemsMode }}</strong>
        </div>
        <div>
          <span>数据环境</span>
          <strong>{{ capabilities.currentEnvironmentCode }}</strong>
        </div>
        <div>
          <span>文件上限</span>
          <strong>{{ Math.round(capabilities.maxUploadBytes / 1024 / 1024) }} MiB</strong>
        </div>
      </div>

      <div class="capability-grid">
        <article
          v-for="capability in capabilities.capabilities"
          :key="capability.featureId"
          class="capability-item"
        >
          <div class="capability-title">
            <code>{{ capability.featureId }}</code>
            <StatusBadge
              :label="label(capability.state)"
              :tone="tone(capability.state)"
            />
          </div>
          <h3>{{ capability.name }}</h3>
          <p>{{ capability.reason }}</p>
        </article>
      </div>
    </template>
  </section>
</template>

<style scoped>
.panel-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 18px;
}

.panel-head h2 {
  margin-top: 3px;
}

.panel-state,
.panel-state :deep(.el-skeleton) {
  margin-top: 20px;
}

.project-strip {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 1px;
  overflow: hidden;
  margin-top: 20px;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: var(--aidt-border);
}

.project-strip div {
  min-width: 0;
  background: #f8faf9;
  padding: 14px;
}

.project-strip span,
.project-strip strong {
  display: block;
}

.project-strip span {
  color: var(--aidt-ink-muted);
  font-size: 0.7rem;
}

.project-strip strong {
  overflow: hidden;
  margin-top: 5px;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.76rem;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.capability-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 12px;
  margin-top: 16px;
}

.capability-item {
  min-width: 0;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: #fff;
  padding: 15px;
}

.capability-title {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 10px;
}

.capability-title code {
  color: var(--aidt-brand);
  font-size: 0.7rem;
}

h3 {
  margin: 12px 0 0;
  font-size: 0.94rem;
}

.capability-item p {
  margin: 7px 0 0;
  font-size: 0.8rem;
}

.technical {
  margin: 6px 0 0;
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.72rem;
}

@media (max-width: 980px) {
  .capability-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .project-strip {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 560px) {
  .panel-head {
    align-items: stretch;
    flex-direction: column;
  }

  .capability-grid,
  .project-strip {
    grid-template-columns: 1fr;
  }
}
</style>
