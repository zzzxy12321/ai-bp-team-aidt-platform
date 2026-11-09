<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  AgentProfileDto,
  SaveWorkflowNodePolicyRequest,
  WorkflowDefinitionDto,
  WorkflowNodePolicyDto,
  WorkflowPolicyUserDto,
  WorkflowVersionNodeDto,
} from '@/api/types/contracts'
import StatusBadge from '@/components/base/StatusBadge.vue'
import { autonomyLabels, riskLabels } from '@/features/agents/agentPresentation'

const loading = ref(true)
const saving = ref(false)
const definitions = ref<WorkflowDefinitionDto[]>([])
const nodes = ref<WorkflowVersionNodeDto[]>([])
const policies = ref<WorkflowNodePolicyDto[]>([])
const users = ref<WorkflowPolicyUserDto[]>([])
const profiles = ref<AgentProfileDto[]>([])
const workflowVersionId = ref('')
const nodeCode = ref('')
const latestPolicy = computed(() => policies.value.find((item) => item.nodeCode === nodeCode.value))
const form = reactive<SaveWorkflowNodePolicyRequest>({
  approverUserId: null,
  backupApproverUserId: null,
  slaMinutes: 120,
  reminderMinutes: 30,
  escalationMinutes: 60,
  timeoutStrategy: 'Escalate',
  allowAgentAssist: true,
  allowAgentProxy: true,
  agentProfileId: null,
  agentMaxRisk: 'Low',
  agentMaxAmount: 0,
  agentMaxAutonomy: 'A2',
  agentProxyMode: 'PreAuthorizedAutoProxy',
  autoProxyAfterMinutes: 60,
  permanentHumanBoundary: false,
  requiredAttachmentCount: 0,
  precheckItems: '确认研发资料、附件和输入信息完整',
  publish: false,
  assigneeStrategy: 'SpecificUser',
  businessRoleCode: null,
  allowTransfer: false,
  transferTargetStrategy: null,
})

function errorText(error: unknown, fallback: string) {
  return error instanceof ApiError ? `${error.message}（${error.code}）` : fallback
}

function userLabel(userId: string | null) {
  return users.value.find((item) => item.id === userId)?.displayName ?? '未指定'
}

function applyPolicy(policy: WorkflowNodePolicyDto | undefined) {
  if (!policy) return
  Object.assign(form, {
    approverUserId: policy.approverUserId,
    backupApproverUserId: policy.backupApproverUserId,
    slaMinutes: policy.slaMinutes,
    reminderMinutes: policy.reminderMinutes,
    escalationMinutes: policy.escalationMinutes,
    timeoutStrategy: policy.timeoutStrategy,
    allowAgentAssist: policy.allowAgentAssist,
    allowAgentProxy: policy.allowAgentProxy,
    agentProfileId: policy.agentProfileId,
    agentMaxRisk: policy.agentMaxRisk,
    agentMaxAmount: policy.agentMaxAmount,
    agentMaxAutonomy: policy.agentMaxAutonomy,
    agentProxyMode: policy.agentProxyMode,
    autoProxyAfterMinutes: policy.autoProxyAfterMinutes,
    permanentHumanBoundary: policy.permanentHumanBoundary,
    requiredAttachmentCount: policy.requiredAttachmentCount,
    precheckItems: policy.precheckItems,
    assigneeStrategy: policy.assigneeStrategy,
    businessRoleCode: policy.businessRoleCode,
    allowTransfer: policy.allowTransfer,
    transferTargetStrategy: policy.transferTargetStrategy,
  })
}

async function loadVersion() {
  if (!workflowVersionId.value) return
  loading.value = true
  try {
    ;[nodes.value, policies.value] = await Promise.all([
      workspaceApi.workflowVersionNodes(workflowVersionId.value),
      workspaceApi.nodePolicies(workflowVersionId.value),
    ])
    nodeCode.value =
      nodes.value.find((item) => item.code === 'MATERIALS_CONFIRM')?.code ??
      nodes.value[0]?.code ??
      ''
    applyPolicy(latestPolicy.value)
  } catch (error) {
    ElMessage.error(errorText(error, '加载流程节点策略失败。'))
  } finally {
    loading.value = false
  }
}

async function save(publish: boolean) {
  if (!workflowVersionId.value || !nodeCode.value) return
  saving.value = true
  try {
    const result = await workspaceApi.saveNodePolicy(
      workflowVersionId.value,
      nodeCode.value,
      { ...form, publish },
    )
    ElMessage.success(publish ? '策略已发布，新任务将冻结此版本' : '策略草稿已保存')
    policies.value = await workspaceApi.nodePolicies(workflowVersionId.value)
    applyPolicy(result)
  } catch (error) {
    ElMessage.error(errorText(error, '保存策略失败。'))
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  try {
    const [definitionData, userData, profileData] = await Promise.all([
      workspaceApi.workflowDefinitions(),
      workspaceApi.policyUsers(),
      workspaceApi.agents(),
    ])
    definitions.value = definitionData
    users.value = userData
    profiles.value = profileData.filter((item) => item.isActive)
    form.agentProfileId =
      profiles.value.find((item) => item.name.includes('流程代理'))?.id ?? null
    workflowVersionId.value =
      definitions.value.find((item) => item.code === 'RND_MATERIALS_CONFIRM')?.versionId ??
      definitions.value[0]?.versionId ??
      ''
    await loadVersion()
  } catch (error) {
    loading.value = false
    ElMessage.error(errorText(error, '加载策略配置失败。'))
  }
})
</script>

<template>
  <div class="page-shell" data-testid="workflow-policies-page">
    <header class="page-head">
      <div>
        <p class="page-kicker">平台管理 · 版本冻结 · 受控代理</p>
        <h1 class="page-title">流程节点策略</h1>
        <p class="page-lead">
          配置审批人、SLA 与低风险自动代理。发布后的新任务冻结当前策略版本；
          高风险和永久人工边界永不自动通过。
        </p>
      </div>
      <StatusBadge
        :label="latestPolicy ? `v${latestPolicy.versionNumber} ${latestPolicy.status}` : '未配置'"
        :tone="latestPolicy?.status === 'Published' ? 'success' : 'warning'"
      />
    </header>

    <section v-loading="loading" class="policy-grid">
      <aside class="section-card selectors">
        <h2>流程与节点</h2>
        <el-select v-model="workflowVersionId" data-testid="policy-version" @change="loadVersion">
          <el-option
            v-for="item in definitions"
            :key="item.versionId"
            :label="`${item.name} · v${item.versionNumber}`"
            :value="item.versionId"
          />
        </el-select>
        <div class="node-list">
          <button
            v-for="node in nodes"
            :key="node.code"
            type="button"
            :class="{ active: node.code === nodeCode }"
            data-testid="policy-node-option"
            @click="nodeCode = node.code; applyPolicy(latestPolicy)"
          >
            <strong>{{ node.name }}</strong>
            <small>{{ node.code }}</small>
          </button>
        </div>
        <div class="scope">
          <strong>影响范围</strong>
          <span>流程版本 v{{ definitions.find((item) => item.versionId === workflowVersionId)?.versionNumber }}</span>
          <span>当前节点 {{ nodeCode || '未选择' }}</span>
          <span>最近发布 {{ latestPolicy ? new Date(latestPolicy.publishedAt).toLocaleString() : '无' }}</span>
        </div>
      </aside>

      <main class="section-card policy-form">
        <h2>{{ nodes.find((item) => item.code === nodeCode)?.name || '节点策略' }}</h2>
        <el-form label-position="top">
          <div class="form-grid">
            <el-form-item label="审批人">
              <el-select v-model="form.approverUserId" clearable>
                <el-option v-for="user in users" :key="user.id" :label="user.displayName" :value="user.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="备份审批人">
              <el-select v-model="form.backupApproverUserId" clearable>
                <el-option v-for="user in users" :key="user.id" :label="user.displayName" :value="user.id" />
              </el-select>
            </el-form-item>
            <el-form-item label="SLA 分钟">
              <el-input-number v-model="form.slaMinutes" :min="1" />
            </el-form-item>
            <el-form-item label="提醒分钟">
              <el-input-number v-model="form.reminderMinutes" :min="0" />
            </el-form-item>
            <el-form-item label="升级分钟">
              <el-input-number v-model="form.escalationMinutes" :min="0" />
            </el-form-item>
            <el-form-item label="自动代理分钟">
              <el-input-number
                v-model="form.autoProxyAfterMinutes"
                :min="1"
                :disabled="form.agentProxyMode !== 'PreAuthorizedAutoProxy'"
              />
            </el-form-item>
            <el-form-item label="超时动作">
              <el-select v-model="form.timeoutStrategy">
                <el-option label="升级处理" value="Escalate" />
                <el-option label="退回发起人" value="Return" />
                <el-option label="仅通知" value="Notify" />
              </el-select>
            </el-form-item>
            <el-form-item label="代理模式">
              <el-select v-model="form.agentProxyMode">
                <el-option label="仅提醒" value="RemindOnly" />
                <el-option label="人工确认代理" value="HumanConfirmedProxy" />
                <el-option label="预授权自动代理" value="PreAuthorizedAutoProxy" />
              </el-select>
            </el-form-item>
            <el-form-item label="AgentProfile">
              <el-select v-model="form.agentProfileId" clearable>
                <el-option
                  v-for="item in profiles"
                  :key="item.id"
                  :label="`${item.name} · ${item.autonomyLevel} ${autonomyLabels[item.autonomyLevel].action} · ${riskLabels[item.riskLimit]}`"
                  :value="item.id"
                >
                  <span>{{ item.name }}</span>
                  <small>{{ item.code || '数字员工' }} · {{ item.description || autonomyLabels[item.autonomyLevel].boundary }}</small>
                </el-option>
              </el-select>
            </el-form-item>
            <el-form-item label="任务转交">
              <el-switch v-model="form.allowTransfer" active-text="允许当前处理人转交" />
            </el-form-item>
            <el-form-item v-if="form.allowTransfer" label="转交/升级目标">
              <el-select v-model="form.transferTargetStrategy" clearable>
                <el-option label="备份处理人" value="BackupApprover" />
                <el-option label="部门负责人" value="DepartmentManager" />
              </el-select>
            </el-form-item>
            <el-form-item label="风险上限">
              <el-select v-model="form.agentMaxRisk">
                <el-option label="Low" value="Low" />
                <el-option label="Medium" value="Medium" />
                <el-option label="High" value="High" />
              </el-select>
            </el-form-item>
            <el-form-item label="金额上限">
              <el-input-number v-model="form.agentMaxAmount" :min="0" />
            </el-form-item>
            <el-form-item label="执行等级上限">
              <el-select v-model="form.agentMaxAutonomy">
                <el-option v-for="level in ['A0','A1','A2','A3','A4']" :key="level" :label="level" :value="level" />
              </el-select>
            </el-form-item>
            <el-form-item label="附件要求">
              <el-input-number v-model="form.requiredAttachmentCount" :min="0" />
            </el-form-item>
          </div>
          <div class="toggles">
            <el-switch v-model="form.allowAgentAssist" active-text="允许 Agent 辅助" />
            <el-switch v-model="form.allowAgentProxy" active-text="允许 Agent 代理" />
            <el-switch v-model="form.permanentHumanBoundary" active-text="永久人工边界" />
          </div>
          <el-form-item label="预检查项">
            <el-input v-model="form.precheckItems" type="textarea" :rows="3" />
          </el-form-item>
        </el-form>

        <el-alert
          v-if="form.permanentHumanBoundary"
          type="error"
          :closable="false"
          title="永久人工边界节点禁止自动代理和自动通过。"
        />
        <div class="actions">
          <el-button :loading="saving" data-testid="save-policy-draft" @click="save(false)">保存草稿</el-button>
          <el-button type="primary" :loading="saving" data-testid="publish-policy" @click="save(true)">发布策略</el-button>
        </div>

        <div class="current">
          <strong>当前版本</strong>
          <span v-if="latestPolicy">
            v{{ latestPolicy.versionNumber }} · {{ latestPolicy.status }} · 审批人 {{ userLabel(latestPolicy.approverUserId) }}
          </span>
          <span v-else>该节点尚无策略</span>
        </div>
      </main>
    </section>
  </div>
</template>

<style scoped>
.page-head { display: flex; justify-content: space-between; gap: 20px; align-items: flex-start; }
.policy-grid { display: grid; grid-template-columns: minmax(260px, .34fr) minmax(0, 1fr); gap: 16px; }
.selectors { display: grid; align-content: start; gap: 16px; }
.node-list { display: grid; gap: 8px; }
.node-list button { display: grid; gap: 4px; border: 1px solid var(--aidt-border); border-radius: 12px; background: white; padding: 12px; text-align: left; cursor: pointer; }
.node-list button.active { border-color: var(--aidt-brand); background: rgb(231 244 237 / 58%); }
.node-list small, .scope span { color: var(--aidt-ink-muted); }
.scope { display: grid; gap: 6px; border-top: 1px solid var(--aidt-border); padding-top: 14px; font-size: .78rem; }
.form-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 0 14px; }
.toggles { display: flex; flex-wrap: wrap; gap: 18px; margin: 8px 0 18px; }
.actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 18px; }
.current { display: flex; justify-content: space-between; gap: 12px; margin-top: 18px; border-top: 1px solid var(--aidt-border); padding-top: 14px; }
@media (max-width: 820px) {
  .page-head, .current { flex-direction: column; }
  .policy-grid, .form-grid { grid-template-columns: 1fr; }
  .actions { position: sticky; bottom: 8px; background: white; padding: 10px 0; }
}
</style>
