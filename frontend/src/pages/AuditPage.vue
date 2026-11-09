<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type { AuditEventDto, WorkflowTaskDto } from '@/api/types/contracts'
import FeaturePlaceholder from '@/components/business/FeaturePlaceholder.vue'
import StatusBadge from '@/components/base/StatusBadge.vue'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'

const loading = ref(true)
const errorMessage = ref('')
const events = ref<AuditEventDto[]>([])
const tasks = ref<WorkflowTaskDto[]>([])
const router = useRouter()
const route = useRoute()

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    const entityType = String(route.query.entityType ?? '')
    const entityId = String(route.query.entityId ?? '')
    const [auditPage, taskPage] = await Promise.all([
      workspaceApi.audit({
        pageSize: 100,
        entityType: entityType || undefined,
        entityId: entityId || undefined,
      }),
      workspaceApi.tasks(),
    ])
    events.value = auditPage.items.filter((item) =>
      (!entityType || item.entityType === entityType)
      && (!entityId || item.entityId === entityId))
    tasks.value = taskPage.items
  } catch (error) {
    errorMessage.value = error instanceof ApiError ? `${error.message} (${error.traceId})` : '加载审计失败。'
  } finally {
    loading.value = false
  }
}

function tone(outcome: AuditEventDto['outcome']) {
  if (outcome === 'Succeeded') return 'success'
  if (outcome === 'Denied') return 'warning'
  return 'danger'
}

function relatedTask(event: AuditEventDto) {
  return event.entityType === 'WorkflowTask'
    ? tasks.value.find((task) => task.id === event.entityId)
    : undefined
}

function openWorkflow(event: AuditEventDto) {
  const task = relatedTask(event)
  if (task) {
    void router.push({ path: '/workflows', query: { instance: task.workflowInstanceId } })
  }
}

onMounted(load)
</script>

<template>
  <div class="page-shell">
    <MainColumnPageHeader
      intro-key="audit"
      title="审计证据"
      scope-label="当前授权范围内的业务与治理记录"
      kicker="追加式审计 · 可追溯"
    >
      <template #actions>
        <el-button :loading="loading" @click="load">刷新</el-button>
      </template>
    </MainColumnPageHeader>

    <el-alert
      v-if="errorMessage"
      type="error"
      :closable="false"
      show-icon
      :title="errorMessage"
    />

    <section v-loading="loading" class="section-card">
      <div class="section-heading">
        <h2>审计事件 · {{ events.length }}</h2>
        <el-button @click="load">刷新</el-button>
      </div>
      <el-table :data="events" empty-text="暂无审计事件">
        <el-table-column label="结果" width="100">
          <template #default="{ row }">
            <StatusBadge :label="row.outcome" :tone="tone(row.outcome)" />
          </template>
        </el-table-column>
        <el-table-column prop="module" label="模块" width="120" />
        <el-table-column prop="action" label="动作" width="130" />
        <el-table-column prop="entityType" label="实体类型" min-width="140" />
        <el-table-column prop="entityId" label="实体 ID" min-width="250" show-overflow-tooltip />
        <el-table-column prop="traceId" label="TraceId" min-width="220" show-overflow-tooltip />
        <el-table-column label="时间" min-width="170">
          <template #default="{ row }">{{ new Date(row.occurredAt).toLocaleString() }}</template>
        </el-table-column>
        <el-table-column prop="reason" label="原因/说明" min-width="180" show-overflow-tooltip />
        <el-table-column label="关联" width="100">
          <template #default="{ row }">
            <el-button v-if="relatedTask(row)" text type="primary" @click="openWorkflow(row)">
              流程
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </section>

    <FeaturePlaceholder
      title="证据链"
      description="审计表通过关联 ID 与 TraceId 追溯对象、流程、任务、Outbox 和 Agent；普通业务接口不提供修改或删除。"
      status="SQL 运行验证通过"
      tone="success"
    />
  </div>
</template>

<style scoped>
.section-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16px;
  margin-bottom: 16px;
}
</style>
