<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type { DashboardDto } from '@/api/types/contracts'
import CapabilityPanel from '@/features/platform/components/CapabilityPanel.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'

const loading = ref(true)
const dashboard = ref<DashboardDto | null>(null)
const errorMessage = ref('')
const includeTestData = ref(false)
const router = useRouter()

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    dashboard.value = await workspaceApi.dashboard(includeTestData.value)
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? `${error.message} (${error.traceId})` : '加载工作台失败。'
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div class="page-shell">
    <MainColumnPageHeader
      intro-key="management"
      scope-label="平台管理员 · 当前治理范围"
      kicker="管理概览"
    >
      <template #actions>
        <el-button :loading="loading" @click="load">刷新</el-button>
      </template>
    </MainColumnPageHeader>

    <section class="admin-tools section-card">
      <div>
        <strong>管理区域</strong>
        <p>这些入口与员工入口分离，避免普通业务用户被技术对象干扰。</p>
      </div>
      <div class="tool-links">
        <el-button @click="router.push('/audit')">审计证据</el-button>
        <el-button @click="router.push('/agents')">数字员工</el-button>
        <el-button @click="router.push('/objects')">对象与流程</el-button>
      </div>
    </section>

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />

    <label class="environment-filter">
      <el-switch v-model="includeTestData" @change="load" />
      显式包含跨环境测试数据
    </label>

    <section v-loading="loading" class="metrics" data-testid="dashboard-metrics">
      <article class="metric-card">
        <span>我的待办</span>
        <strong>{{ dashboard?.pendingTaskCount ?? '—' }}</strong>
        <small>Pending tasks assigned to me</small>
      </article>
      <article class="metric-card">
        <span>运行中流程</span>
        <strong>{{ dashboard?.runningWorkflowCount ?? '—' }}</strong>
        <small>Running or waiting instances</small>
      </article>
      <article class="metric-card">
        <span>业务对象</span>
        <strong>{{ dashboard?.businessObjectCount ?? '—' }}</strong>
        <small>Organization-scoped master objects</small>
      </article>
      <article class="metric-card">
        <span>今日审计</span>
        <strong>{{ dashboard?.todayAuditCount ?? '—' }}</strong>
        <small>Append-only audit events</small>
      </article>
      <article class="metric-card accent">
        <span>Agent 待确认</span>
        <strong>{{ dashboard?.pendingAgentConfirmationCount ?? '—' }}</strong>
        <small>Human confirmation required</small>
      </article>
    </section>

    <CapabilityPanel />
  </div>
</template>

<style scoped>
.metrics {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 12px;
  min-height: 120px;
}

.admin-tools,
.tool-links,
.environment-filter {
  display: flex;
  align-items: center;
  gap: 12px;
}

.admin-tools {
  justify-content: space-between;
}

.admin-tools p {
  margin: 4px 0 0;
}

.environment-filter {
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.metric-card {
  border: 1px solid var(--aidt-border);
  border-radius: var(--aidt-radius-md);
  background: var(--aidt-surface);
  padding: 18px;
  box-shadow: var(--aidt-shadow);
}

.metric-card span,
.metric-card strong,
.metric-card small {
  display: block;
}

.metric-card span {
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.metric-card strong {
  margin-top: 8px;
  color: var(--aidt-brand-strong);
  font-size: 2rem;
}

.metric-card small {
  margin-top: 6px;
  color: var(--aidt-ink-muted);
  font-family: ui-monospace, SFMono-Regular, Consolas, monospace;
  font-size: 0.65rem;
}

.metric-card.accent {
  background: linear-gradient(145deg, #143d30, #24604b);
  color: #fff;
}

.metric-card.accent span,
.metric-card.accent small {
  color: #c5ded2;
}

.metric-card.accent strong {
  color: #fff;
}

@media (max-width: 1050px) {
  .metrics {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 640px) {
  .admin-tools {
    align-items: stretch;
    flex-direction: column;
  }

  .tool-links {
    flex-wrap: wrap;
  }

  .metrics {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}
</style>
