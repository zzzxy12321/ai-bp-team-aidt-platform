<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { mcpApi } from '@/api/modules/mcpApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  AgentProfileDto,
  DepartmentDto,
  McpManagementSnapshot,
  McpServiceToken,
  McpServiceTokenRequest,
} from '@/api/types/contracts'
import { useAuthStore } from '@/stores/auth'
import MainColumnPageHeader from '@/components/workspace/MainColumnPageHeader.vue'
import { autonomyLabels, riskLabels } from '@/features/agents/agentPresentation'

const auth = useAuthStore()
const loading = ref(false)
const errorMessage = ref('')
const activeTab = ref('tools')
const snapshot = ref<McpManagementSnapshot | null>(null)
const canManage = computed(() => auth.can('mcp:manage'))
const availableAgents = ref<AgentProfileDto[]>([])
const departments = ref<DepartmentDto[]>([])
const serviceDialogVisible = ref(false)
const serviceTokenVisible = ref(false)
const serviceToken = ref<McpServiceToken | null>(null)
const issuingServiceToken = ref(false)
const serviceForm = ref<McpServiceTokenRequest>({
  serviceUserId: '',
  clientName: '数字员工 MCP 接入',
  scopes: ['aidt:read'],
  departmentIds: [],
  lifetimeHours: 24,
  agentProfileId: null,
})
const activeTokenCount = computed(
  () => snapshot.value?.tokens.filter((item) => item.kind === 'Access' && !item.revokedAt).length ?? 0,
)
const pendingConfirmationCount = computed(
  () => snapshot.value?.confirmations.filter((item) => item.status === 'Pending').length ?? 0,
)

function formatTime(value?: string | null) {
  return value ? new Date(value).toLocaleString('zh-CN') : '—'
}

async function load() {
  loading.value = true
  errorMessage.value = ''
  try {
    snapshot.value = await mcpApi.getSnapshot()
    if (canManage.value) {
      const [agentRows, departmentRows] = await Promise.all([
        workspaceApi.agents(),
        workspaceApi.departments(),
      ])
      availableAgents.value = agentRows
      departments.value = departmentRows
    }
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : '读取 MCP 治理数据失败。'
  } finally {
    loading.value = false
  }
}

function openServiceTokenDialog() {
  const firstAgent = availableAgents.value[0]
  serviceForm.value = {
    serviceUserId: '',
    clientName: '数字员工 MCP 接入',
    scopes: ['aidt:read'],
    departmentIds: departments.value[0]?.id ? [departments.value[0].id] : [],
    lifetimeHours: 24,
    agentProfileId: firstAgent?.id ?? null,
  }
  selectAgent(firstAgent?.id ?? '')
  serviceDialogVisible.value = true
}

function selectAgent(agentProfileId: string) {
  serviceForm.value.agentProfileId = agentProfileId || null
  const profile = availableAgents.value.find((item) => item.id === agentProfileId)
  serviceForm.value.serviceUserId = profile?.serviceUserId ?? ''
  if (profile?.departmentId) {
    serviceForm.value.departmentIds = [profile.departmentId]
  }
}

function agentNameFor(
  agentProfileId?: string | null,
  agentProfileName?: string | null,
) {
  if (!agentProfileId) return '历史真人授权'
  return agentProfileName
    ?? availableAgents.value.find((item) => item.id === agentProfileId)?.name
    ?? `数字员工 ${agentProfileId.slice(-8)}`
}

function tokenStatus(row: McpManagementSnapshot['tokens'][number]) {
  if (row.revokedAt) return { label: '已撤销', tone: 'info' as const }
  if (new Date(row.expiresAt).getTime() <= Date.now()) {
    return { label: '已过期', tone: 'info' as const }
  }
  return { label: '有效', tone: 'success' as const }
}

async function issueServiceToken() {
  if (!serviceForm.value.agentProfileId
    || !serviceForm.value.clientName.trim()
    || serviceForm.value.scopes.length === 0
    || serviceForm.value.departmentIds.length === 0) {
    ElMessage.warning('请选择有效数字员工、部门和至少一个工具作用域。')
    return
  }
  issuingServiceToken.value = true
  try {
    serviceToken.value = await mcpApi.issueServiceToken(serviceForm.value)
    serviceDialogVisible.value = false
    serviceTokenVisible.value = true
    await load()
  } finally {
    issuingServiceToken.value = false
  }
}

async function revokeClient(clientId: string, clientName: string) {
  await ElMessageBox.confirm(
    `停用“${clientName}”会立即撤销该客户端全部 MCP 令牌。`,
    '停用 MCP 客户端',
    { type: 'warning', confirmButtonText: '确认停用', cancelButtonText: '取消' },
  )
  await mcpApi.revokeClient(clientId)
  ElMessage.success('MCP 客户端已停用，全部令牌立即失效')
  await load()
}

async function revokeToken(tokenId: string, clientName: string) {
  await ElMessageBox.confirm(
    `撤销 ${clientName} 的该令牌后，下一次 MCP 调用会立即失败。`,
    '撤销 MCP 令牌',
    { type: 'warning', confirmButtonText: '确认撤销', cancelButtonText: '取消' },
  )
  await mcpApi.revokeToken(tokenId)
  ElMessage.success('MCP 令牌已撤销')
  await load()
}

onMounted(load)
</script>

<template>
  <div class="mcp-page" data-testid="mcp-center-page">
    <MainColumnPageHeader
      intro-key="mcp-center"
      scope-label="当前组织、部门与工具授权范围"
      kicker="AIDT · MCP GOVERNANCE"
    >
      <template #actions>
        <div class="header-actions">
        <el-button
          v-if="canManage"
          data-testid="issue-mcp-service-token"
          @click="openServiceTokenDialog"
        >
          签发服务身份令牌
        </el-button>
        <el-button :loading="loading" @click="load">刷新状态</el-button>
        </div>
      </template>
    </MainColumnPageHeader>

    <el-alert v-if="errorMessage" type="error" :closable="false" show-icon :title="errorMessage" />
    <el-skeleton v-if="loading && !snapshot" :rows="8" animated />

    <template v-if="snapshot">
      <section class="metric-grid">
        <article><span>工具目录</span><strong>{{ snapshot.tools.length }}</strong><small>业务语义工具</small></article>
        <article><span>授权客户端</span><strong>{{ snapshot.clients.filter((item) => item.isActive).length }}</strong><small>共 {{ snapshot.clients.length }} 个</small></article>
        <article><span>有效访问令牌</span><strong>{{ activeTokenCount }}</strong><small>撤销后即时失效</small></article>
        <article><span>待真人确认</span><strong>{{ pendingConfirmationCount }}</strong><small>审批与敏感文件</small></article>
      </section>

      <section class="mcp-card">
        <el-tabs v-model="activeTab">
          <el-tab-pane label="工具目录" name="tools">
            <div class="tool-grid">
              <article v-for="tool in snapshot.tools" :key="tool.name" class="tool-card">
                <div class="tool-heading">
                  <strong>{{ tool.title }}</strong>
                  <el-tag size="small" effect="plain">{{ tool.scope }}</el-tag>
                </div>
                <code>{{ tool.name }}</code>
                <p>{{ tool.description }}</p>
                <div class="tag-row">
                  <el-tag v-if="tool.readOnly" size="small" type="success" effect="plain">只读</el-tag>
                  <el-tag v-if="tool.highRisk" size="small" type="warning" effect="plain">真人确认</el-tag>
                  <el-tag size="small" type="info" effect="plain">{{ tool.resourceType }}</el-tag>
                </div>
              </article>
            </div>
          </el-tab-pane>

          <el-tab-pane :label="`授权客户端 (${snapshot.clients.length})`" name="clients">
            <el-empty v-if="!snapshot.clients.length" description="尚无 MCP 客户端" />
            <el-table v-else :data="snapshot.clients" row-key="clientId">
              <el-table-column prop="clientName" label="客户端" min-width="180" />
              <el-table-column label="数字员工" min-width="190">
                <template #default="{ row }">
                  {{ agentNameFor(row.agentProfileId, row.agentProfileName) }}
                  <el-tag
                    v-if="row.agentProfileAvailable === false"
                    type="danger"
                    size="small"
                    effect="plain"
                  >
                    不可用
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="状态" width="90">
                <template #default="{ row }">
                  <el-tag :type="row.isActive ? 'success' : 'info'" effect="plain">
                    {{ row.isActive ? '有效' : '已停用' }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="activeTokenCount" label="有效令牌" width="100" />
              <el-table-column label="最后使用" min-width="170">
                <template #default="{ row }">{{ formatTime(row.lastUsedAt) }}</template>
              </el-table-column>
              <el-table-column v-if="canManage" label="动作" width="110">
                <template #default="{ row }">
                  <el-button
                    v-if="row.isActive"
                    type="danger"
                    link
                    @click="revokeClient(row.clientId, row.clientName)"
                  >停用</el-button>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>

          <el-tab-pane :label="`令牌 (${snapshot.tokens.length})`" name="tokens">
            <el-empty v-if="!snapshot.tokens.length" description="尚无 MCP 令牌" />
            <el-table v-else :data="snapshot.tokens" row-key="id">
              <el-table-column prop="clientName" label="客户端" min-width="150" />
              <el-table-column label="数字员工" min-width="170">
                <template #default="{ row }">
                  {{ agentNameFor(row.agentProfileId, row.agentProfileName) }}
                  <el-tag
                    v-if="row.agentProfileAvailable === false"
                    type="danger"
                    size="small"
                    effect="plain"
                  >
                    不可用
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column prop="userDisplayName" label="用户" min-width="120" />
              <el-table-column label="类型" width="110">
                <template #default="{ row }">
                  {{ row.kind === 'Access' ? '访问令牌' : '刷新令牌' }}
                </template>
              </el-table-column>
              <el-table-column label="作用域" min-width="220">
                <template #default="{ row }">
                  <el-tag v-for="scope in row.scopes" :key="scope" size="small" class="scope-tag">{{ scope }}</el-tag>
                </template>
              </el-table-column>
              <el-table-column label="到期" min-width="170">
                <template #default="{ row }">{{ formatTime(row.expiresAt) }}</template>
              </el-table-column>
              <el-table-column label="状态" width="90">
                <template #default="{ row }">
                  <el-tag :type="tokenStatus(row).tone" effect="plain">
                    {{ tokenStatus(row).label }}
                  </el-tag>
                </template>
              </el-table-column>
              <el-table-column label="撤销原因" min-width="180">
                <template #default="{ row }">{{ row.revocationReason || '—' }}</template>
              </el-table-column>
              <el-table-column v-if="canManage" label="动作" width="100">
                <template #default="{ row }">
                  <el-button
                    v-if="!row.revokedAt"
                    type="danger"
                    link
                    @click="revokeToken(row.id, row.clientName)"
                  >撤销</el-button>
                </template>
              </el-table-column>
            </el-table>
          </el-tab-pane>

          <el-tab-pane :label="`真人确认 (${snapshot.confirmations.length})`" name="confirmations">
            <el-empty v-if="!snapshot.confirmations.length" description="没有待处理 MCP 真人确认" />
            <div v-else class="confirmation-list">
              <article v-for="item in snapshot.confirmations" :key="item.id">
                <div>
                  <strong>{{ item.toolName }}</strong>
                  <p>{{ item.summary }}</p>
                  <small>{{ item.clientId }} · {{ formatTime(item.createdAt) }} 到期 {{ formatTime(item.expiresAt) }}</small>
                </div>
                <el-tag :type="item.status === 'Confirmed' ? 'success' : 'warning'" effect="plain">
                  {{ item.status }}
                </el-tag>
              </article>
            </div>
          </el-tab-pane>

          <el-tab-pane label="调用审计" name="calls">
            <el-empty v-if="!snapshot.recentInvocations.length" description="尚无 MCP 调用审计" />
            <el-table v-else :data="snapshot.recentInvocations" row-key="auditId">
              <el-table-column prop="toolOrResource" label="工具/资源" min-width="180" />
              <el-table-column prop="operation" label="动作" width="130" />
              <el-table-column prop="clientId" label="客户端" min-width="200" />
              <el-table-column prop="outcome" label="结果" width="100" />
              <el-table-column prop="code" label="错误码" min-width="150" />
              <el-table-column prop="traceId" label="TraceId" min-width="180" />
              <el-table-column label="时间" min-width="170">
                <template #default="{ row }">{{ formatTime(row.occurredAt) }}</template>
              </el-table-column>
            </el-table>
          </el-tab-pane>
        </el-tabs>
      </section>
    </template>

    <el-dialog v-model="serviceDialogVisible" title="签发服务身份 MCP 令牌" width="min(640px, 94vw)">
      <el-alert
        type="warning"
        :closable="false"
        show-icon
        title="服务令牌只继承所选服务身份的业务权限和所选部门，不会获得平台管理员权限。原始令牌只显示一次。"
      />
      <el-form label-position="top" class="service-token-form">
        <el-form-item label="数字员工身份">
          <el-select
            :model-value="serviceForm.agentProfileId"
            data-testid="mcp-agent-profile-select"
            placeholder="选择已发布且有效的数字员工"
            @update:model-value="selectAgent"
          >
            <el-option
              v-for="item in availableAgents"
              :key="item.id"
              :label="`${item.name} · ${item.autonomyLevel} ${autonomyLabels[item.autonomyLevel].action} · ${riskLabels[item.riskLimit]}`"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="令牌名称">
          <el-input v-model="serviceForm.clientName" maxlength="200" />
        </el-form-item>
        <el-form-item label="部门范围">
          <el-select v-model="serviceForm.departmentIds" multiple collapse-tags>
            <el-option
              v-for="item in departments"
              :key="item.id"
              :label="item.name"
              :value="item.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="工具作用域">
          <el-checkbox-group v-model="serviceForm.scopes">
            <el-checkbox value="aidt:read">aidt:read</el-checkbox>
            <el-checkbox value="aidt:write">aidt:write</el-checkbox>
            <el-checkbox value="aidt:files">aidt:files</el-checkbox>
            <el-checkbox value="aidt:approve">aidt:approve</el-checkbox>
          </el-checkbox-group>
        </el-form-item>
        <el-form-item label="有效期（小时）">
          <el-input-number v-model="serviceForm.lifetimeHours" :min="1" :max="168" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="serviceDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="issuingServiceToken" @click="issueServiceToken">签发</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="serviceTokenVisible" title="服务身份令牌已签发" width="min(720px, 94vw)">
      <el-alert
        type="success"
        :closable="false"
        show-icon
        title="请立即在受控密钥系统中保存；关闭后 AIDT 不再显示原始令牌。"
      />
      <el-input
        v-if="serviceToken"
        class="token-output"
        type="textarea"
        :rows="5"
        readonly
        :model-value="serviceToken.accessToken"
      />
      <template #footer>
        <el-button type="primary" @click="serviceTokenVisible = false">我已保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.mcp-page { display: grid; gap: 22px; }
.mcp-header { display: flex; align-items: flex-start; justify-content: space-between; gap: 18px; }
.mcp-header h1 { margin: 4px 0 8px; }
.mcp-header p { max-width: 780px; color: var(--aidt-ink-muted); line-height: 1.75; }
.header-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.metric-grid { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 14px; }
.metric-grid article { border: 1px solid var(--aidt-border); border-radius: 18px; background: white; padding: 18px; box-shadow: 0 12px 30px rgb(18 60 45 / 5%); }
.metric-grid span, .metric-grid small { display: block; color: var(--aidt-ink-muted); }
.metric-grid strong { display: block; margin: 8px 0 4px; font-size: 1.8rem; }
.mcp-card { min-width: 0; border: 1px solid var(--aidt-border); border-radius: 20px; background: rgb(255 255 255 / 94%); padding: 20px; }
.tool-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 12px; }
.tool-card { min-width: 0; border: 1px solid var(--aidt-border); border-radius: 15px; padding: 15px; background: #f9fbfa; }
.tool-heading { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.tool-card code, .inline-code { overflow-wrap: anywhere; color: #2b5b49; font-size: .72rem; }
.tool-card p { min-height: 3.4em; color: var(--aidt-ink-muted); font-size: .8rem; line-height: 1.6; }
.tag-row, .scope-tag { display: inline-flex; gap: 6px; margin: 2px 4px 2px 0; }
.confirmation-list { display: grid; gap: 10px; }
.confirmation-list article { display: flex; justify-content: space-between; gap: 16px; border: 1px solid var(--aidt-border); border-radius: 14px; padding: 14px; }
.confirmation-list p { margin: 6px 0; color: var(--aidt-ink-muted); line-height: 1.6; }
.confirmation-list small { color: var(--aidt-ink-muted); overflow-wrap: anywhere; }
.service-token-form { margin-top: 16px; }
.service-token-form .el-select { width: 100%; }
.token-output { margin-top: 16px; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; }
@media (max-width: 1100px) { .tool-grid { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 760px) {
  .mcp-header { align-items: stretch; flex-direction: column; }
  .metric-grid, .tool-grid { grid-template-columns: 1fr; }
  .confirmation-list article { flex-direction: column; }
  .mcp-card { padding: 14px; overflow: hidden; }
}
</style>
