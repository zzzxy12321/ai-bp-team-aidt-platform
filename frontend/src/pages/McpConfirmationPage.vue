<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { useRoute } from 'vue-router'
import { ApiError } from '@/api/http'
import { mcpApi } from '@/api/modules/mcpApi'
import type { McpHumanConfirmationView } from '@/api/types/contracts'

const route = useRoute()
const loading = ref(false)
const deciding = ref(false)
const errorMessage = ref('')
const confirmations = ref<McpHumanConfirmationView[]>([])
const comment = ref('')
const selectedId = ref(String(route.query.confirmationId ?? ''))
const selected = computed(
  () => confirmations.value.find((item) => item.id === selectedId.value)
    ?? confirmations.value.find((item) => item.status === 'Pending')
    ?? confirmations.value[0]
    ?? null,
)

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    confirmations.value = await mcpApi.getOwnConfirmationViews()
    if (!selectedId.value && confirmations.value[0]) {
      selectedId.value = confirmations.value[0].id
    }
  } catch (error) {
    errorMessage.value = error instanceof ApiError
      ? `${error.message}（${error.code}）`
      : '读取智能体确认失败。'
  } finally {
    loading.value = false
  }
}

async function decide(approve: boolean) {
  if (!selected.value || deciding.value) return
  const decisionComment = comment.value.trim()
  if (!decisionComment) {
    ElMessage.warning(approve ? '通过前必须填写确认意见' : '拒绝前必须填写拒绝意见')
    return
  }
  deciding.value = true
  try {
    if (approve) {
      await mcpApi.confirm(selected.value.id, decisionComment)
      ElMessage.success('已通过；智能体可使用同一确认编号重试一次')
    } else {
      await mcpApi.reject(selected.value.id, decisionComment)
      ElMessage.success('已拒绝该业务动作')
    }
    comment.value = ''
    await load()
  } catch (error) {
    ElMessage.error(error instanceof ApiError ? error.message : '提交确认决定失败')
  } finally {
    deciding.value = false
  }
}

function formatTime(value: string) {
  return new Intl.DateTimeFormat('zh-CN', {
    timeZone: 'Asia/Shanghai',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(value))
}

function statusLabel(status: McpHumanConfirmationView['status']) {
  return {
    Pending: '待真人确认',
    Confirmed: '已通过',
    Rejected: '已拒绝',
    Expired: '已过期',
  }[status]
}

function statusTone(status: McpHumanConfirmationView['status']) {
  if (status === 'Confirmed') return 'success'
  if (status === 'Rejected') return 'danger'
  if (status === 'Expired') return 'info'
  return 'warning'
}

onMounted(load)
</script>

<template>
  <div class="confirmation-page" data-testid="mcp-confirmation-page">
    <header>
      <p class="page-kicker">AIDT · 智能体业务确认</p>
      <h1>智能体高风险动作确认</h1>
      <p>
        页面只展示业务动作和影响。技术标识收在折叠区，不显示任何访问令牌；
        通过和拒绝都必须由当前真人填写意见。
      </p>
    </header>

    <el-alert v-if="errorMessage" type="error" :closable="false" show-icon :title="errorMessage" />
    <el-skeleton v-if="loading && !confirmations.length" :rows="5" animated />
    <el-empty v-else-if="!selected" description="没有需要你处理的智能体业务动作" />

    <section v-else class="confirmation-layout">
      <aside class="confirmation-list">
        <button
          v-for="item in confirmations"
          :key="item.id"
          type="button"
          class="confirmation-row"
          :class="{ active: item.id === selected.id }"
          @click="selectedId = item.id; comment = ''"
        >
          <span>{{ item.actionTitle }}</span>
          <strong>{{ item.resourceDisplayName }}</strong>
          <small>{{ statusLabel(item.status) }} · {{ formatTime(item.createdAt) }}</small>
        </button>
      </aside>

      <article class="confirmation-card">
        <div class="confirmation-heading">
          <div>
            <span class="confirm-label">业务动作</span>
            <h2>{{ selected.actionTitle }}</h2>
            <p>{{ selected.summary }}</p>
          </div>
          <el-tag :type="statusTone(selected.status)" effect="dark">
            {{ statusLabel(selected.status) }}
          </el-tag>
        </div>

        <div class="business-grid">
          <div>
            <span>业务对象</span>
            <strong>{{ selected.resourceDisplayName }}</strong>
          </div>
          <div>
            <span>所属项目</span>
            <strong>{{ selected.projectName || '未关联项目' }}</strong>
          </div>
          <div>
            <span>有效期</span>
            <strong>{{ formatTime(selected.expiresAt) }}</strong>
          </div>
          <div>
            <span>提出时间</span>
            <strong>{{ formatTime(selected.createdAt) }}</strong>
          </div>
        </div>

        <div class="impact-grid">
          <el-alert type="success" :closable="false" show-icon title="确认将执行">
            <template #default>{{ selected.willExecute }}</template>
          </el-alert>
          <el-alert type="warning" :closable="false" show-icon title="不会执行">
            <template #default>{{ selected.willNotExecute }}</template>
          </el-alert>
        </div>

        <details class="technical-info">
          <summary>技术信息</summary>
          <dl>
            <dt>客户端</dt><dd>{{ selected.technical.clientId }}</dd>
            <dt>资源类型</dt><dd>{{ selected.technical.resourceType }}</dd>
            <dt>资源标识</dt><dd>{{ selected.technical.resourceId }}</dd>
            <dt>请求摘要</dt><dd><code>{{ selected.technical.requestHash }}</code></dd>
          </dl>
        </details>

        <template v-if="selected.status === 'Pending'">
          <el-input
            v-model="comment"
            data-testid="mcp-decision-comment"
            type="textarea"
            :rows="3"
            maxlength="1000"
            show-word-limit
            placeholder="必填：说明通过或拒绝的业务依据"
          />
          <div class="confirmation-actions">
            <el-button
              data-testid="reject-mcp-confirmation"
              type="danger"
              plain
              :disabled="!comment.trim() || deciding"
              @click="decide(false)"
            >
              拒绝
            </el-button>
            <el-button
              data-testid="confirm-mcp-confirmation"
              type="primary"
              :disabled="!comment.trim() || deciding"
              :loading="deciding"
              @click="decide(true)"
            >
              确认一次
            </el-button>
          </div>
        </template>
        <el-alert
          v-else
          :type="selected.status === 'Rejected' ? 'error' : statusTone(selected.status)"
          :closable="false"
          show-icon
          :title="statusLabel(selected.status)"
          :description="selected.decisionComment || '该确认已结束，不能再执行。'"
        />
      </article>
    </section>
  </div>
</template>

<style scoped>
.confirmation-page { display: grid; gap: 22px; max-width: 1120px; margin: 0 auto; }
.confirmation-page header h1 { margin: 4px 0 8px; }
.confirmation-page header p { color: var(--aidt-ink-muted); line-height: 1.75; }
.confirmation-layout { display: grid; grid-template-columns: minmax(230px, .35fr) minmax(0, 1fr); gap: 16px; }
.confirmation-list { display: grid; align-content: start; gap: 8px; }
.confirmation-row { display: grid; gap: 5px; border: 1px solid var(--aidt-border); border-radius: 14px; background: #fff; padding: 13px; text-align: left; cursor: pointer; }
.confirmation-row.active { border-color: #96c3aa; background: #f0f8f3; }
.confirmation-row span, .confirmation-row small { color: var(--aidt-ink-muted); font-size: .72rem; }
.confirmation-card { display: grid; gap: 18px; border: 1px solid #ead4a6; border-radius: 22px; background: white; padding: clamp(20px, 4vw, 32px); box-shadow: 0 20px 50px rgb(80 58 15 / 9%); }
.confirmation-heading { display: flex; align-items: flex-start; justify-content: space-between; gap: 18px; }
.confirmation-heading h2 { margin: 5px 0 0; }
.confirmation-heading p { color: var(--aidt-ink-muted); line-height: 1.65; }
.confirm-label { color: #9a6d19; font-size: .72rem; font-weight: 800; letter-spacing: .12em; }
.business-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.business-grid div { min-width: 0; border-radius: 14px; background: #f7f9f7; padding: 13px; }
.business-grid span, .business-grid strong { display: block; overflow-wrap: anywhere; }
.business-grid span { color: var(--aidt-ink-muted); font-size: .72rem; }
.business-grid strong { margin-top: 4px; }
.impact-grid { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.technical-info { border-radius: 13px; background: #f3f5f4; padding: 12px 14px; }
.technical-info summary { cursor: pointer; color: var(--aidt-ink-muted); font-size: .76rem; font-weight: 700; }
.technical-info dl { display: grid; grid-template-columns: 100px minmax(0, 1fr); gap: 8px; margin: 12px 0 0; font-size: .72rem; }
.technical-info dt { color: var(--aidt-ink-muted); }
.technical-info dd { min-width: 0; margin: 0; overflow-wrap: anywhere; }
.confirmation-actions { display: flex; justify-content: flex-end; gap: 10px; }
@media (max-width: 820px) {
  .confirmation-layout, .business-grid, .impact-grid { grid-template-columns: 1fr; }
  .confirmation-list { grid-template-columns: repeat(2, minmax(0, 1fr)); }
}
@media (max-width: 560px) {
  .confirmation-heading { flex-direction: column; }
  .confirmation-list { grid-template-columns: 1fr; }
  .confirmation-actions { flex-direction: column-reverse; }
  .confirmation-actions .el-button { width: 100%; }
}
</style>
