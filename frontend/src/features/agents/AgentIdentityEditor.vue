<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue'
import { ElMessage } from 'element-plus'
import type {
  AgentIdentityOptionsDto,
  AgentProfileValidationDto,
} from '@/api/types/contracts'
import {
  type AgentIdentityFormValue,
  cloneAgentForm,
  organizationScope,
} from '@/features/agents/agentForm'
import {
  autonomyLabels,
  confirmationLabels,
  formatAmount,
  riskLabels,
  stopConditionLabels,
} from '@/features/agents/agentPresentation'
import StatusBadge from '@/components/base/StatusBadge.vue'

const props = defineProps<{
  visible: boolean
  mode: 'create' | 'edit'
  initial: AgentIdentityFormValue
  options: AgentIdentityOptionsDto | null
  validation: AgentProfileValidationDto | null
  saving: boolean
  validating: boolean
  publishing: boolean
  canPublish: boolean
}>()

const emit = defineEmits<{
  'update:visible': [value: boolean]
  submit: [value: AgentIdentityFormValue]
  validate: [value: AgentIdentityFormValue]
  publish: [value: AgentIdentityFormValue]
}>()

const form = reactive<AgentIdentityFormValue>(cloneAgentForm(props.initial))
const activeSection = ref('basic')

watch(
  () => [props.visible, props.initial] as const,
  ([visible]) => {
    if (!visible) return
    Object.assign(form, cloneAgentForm(props.initial))
    activeSection.value = 'basic'
  },
  { deep: true },
)

watch(
  () => form.departmentId,
  (departmentId) => {
    if (!departmentId) return
    const scope = `department:${departmentId}`
    if (!form.dataScopes.includes(scope)) form.dataScopes.push(scope)
  },
)

const orgScope = computed({
  get() {
    const scope = organizationScope(props.options)
    return Boolean(scope && form.dataScopes.includes(scope))
  },
  set(enabled: boolean) {
    const scope = organizationScope(props.options)
    if (!scope) return
    form.dataScopes = enabled
      ? [...new Set([...form.dataScopes, scope])]
      : form.dataScopes.filter((item) => item !== scope)
  },
})

const departmentScopeIds = computed({
  get() {
    return form.dataScopes
      .filter((item) => item.startsWith('department:'))
      .map((item) => item.slice('department:'.length))
  },
  set(ids: string[]) {
    const organization = organizationScope(props.options)
    form.dataScopes = [
      ...(orgScope.value && organization ? [organization] : []),
      ...ids.map((id) => `department:${id}`),
    ]
  },
})

const permanentBoundaries = computed(() =>
  props.options?.permanentHumanBoundaries ?? [],
)
const optionalConfirmations = computed(() =>
  (props.options?.confirmationRules ?? []).filter(
    (item) => !permanentBoundaries.value.includes(item),
  ),
)

const toolRows = computed(() =>
  (props.options?.capabilities ?? []).map((item) => ({
    ...item,
    selected: form.allowedTools.includes(item.code),
  })),
)

function toggleTool(code: string, selected: boolean) {
  form.allowedTools = selected
    ? [...new Set([...form.allowedTools, code])]
    : form.allowedTools.filter((item) => item !== code)
}

function close() {
  emit('update:visible', false)
}

function validateForm(): string | null {
  if (!form.code.trim() || !form.name.trim()) return '请填写数字员工名称和稳定编码。'
  if (!form.departmentId) return '请选择所属部门。'
  if (!form.serviceUserId) return '请选择未绑定的数字员工服务账号。'
  if (!form.businessOwnerUserId || !form.technicalOwnerUserId) return '请选择业务负责人和技术负责人。'
  if (form.allowedTools.length === 0) return '至少选择一项已注册工具能力。'
  if (form.dataScopes.length === 0) return '至少选择一个数据范围。'
  if (form.modelScopes.length === 0) return '至少选择一个模型范围。'
  if (form.stopConditions.length === 0) return '至少选择一个熔断条件。'
  return null
}

function submit() {
  const error = validateForm()
  if (error) {
    ElMessage.warning(error)
    return
  }
  emit('submit', cloneAgentForm(form))
}

function validate() {
  const error = validateForm()
  if (error) {
    ElMessage.warning(error)
    return
  }
  emit('validate', cloneAgentForm(form))
}

function publish() {
  emit('publish', cloneAgentForm(form))
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="mode === 'create' ? '新建数字员工身份' : '编辑数字员工身份'"
    width="min(1080px, 96vw)"
    class="agent-editor-dialog"
    destroy-on-close
    @update:model-value="emit('update:visible', $event)"
  >
    <el-tabs v-model="activeSection" tab-position="left" class="editor-tabs">
      <el-tab-pane label="基本信息" name="basic">
        <section class="editor-section">
          <h3>这是什么数字员工？</h3>
          <p class="section-help">名称和说明要能让业务人员看懂；稳定编码发布后不可修改。</p>
          <el-form label-position="top">
            <div class="field-grid">
              <el-form-item label="稳定编码" required>
                <el-input
                  v-model="form.code"
                  data-testid="agent-code-input"
                  maxlength="64"
                  :disabled="mode === 'edit'"
                  placeholder="例如 AGENT-QA-REVIEW"
                />
              </el-form-item>
              <el-form-item label="名称" required>
                <el-input v-model="form.name" data-testid="agent-name-input" maxlength="128" />
              </el-form-item>
            </div>
            <el-form-item label="岗位说明">
              <el-input
                v-model="form.description"
                data-testid="agent-description-input"
                type="textarea"
                :rows="3"
                maxlength="2000"
                show-word-limit
                placeholder="说明它服务哪个岗位、解决什么问题，以及明确的禁止动作。"
              />
            </el-form-item>
          </el-form>
        </section>
      </el-tab-pane>

      <el-tab-pane label="组织与负责人" name="organization">
        <section class="editor-section">
          <h3>归属与责任</h3>
          <p class="section-help">数字员工必须归属部门，并有真人的业务负责人和技术负责人。</p>
          <el-form label-position="top">
            <el-form-item label="所属部门" required>
              <el-select
                v-model="form.departmentId"
                data-testid="agent-department-select"
                filterable
                :disabled="mode === 'edit'"
                placeholder="搜索并选择部门"
              >
                <el-option
                  v-for="department in options?.departments ?? []"
                  :key="department.id"
                  :label="`${department.name} (${department.code})`"
                  :value="department.id"
                />
              </el-select>
            </el-form-item>
            <div class="field-grid">
              <el-form-item label="业务负责人" required>
                <el-select
                  v-model="form.businessOwnerUserId"
                  data-testid="agent-business-owner-select"
                  filterable
                  placeholder="搜索业务负责人"
                >
                  <el-option
                    v-for="person in options?.people ?? []"
                    :key="person.id"
                    :label="`${person.displayName} (${person.userName})`"
                    :value="person.id"
                  />
                </el-select>
              </el-form-item>
              <el-form-item label="技术负责人" required>
                <el-select
                  v-model="form.technicalOwnerUserId"
                  data-testid="agent-technical-owner-select"
                  filterable
                  placeholder="搜索技术负责人"
                >
                  <el-option
                    v-for="person in options?.people ?? []"
                    :key="person.id"
                    :label="`${person.displayName} (${person.userName})`"
                    :value="person.id"
                  />
                </el-select>
              </el-form-item>
            </div>
          </el-form>
        </section>
      </el-tab-pane>

      <el-tab-pane label="身份与服务账号" name="identity">
        <section class="editor-section">
          <h3>独立服务身份</h3>
          <p class="section-help">
            数字员工必须使用独立服务账号，不能冒充真人。一个服务账号只能绑定一个数字员工。
          </p>
          <el-form label-position="top">
            <el-form-item label="数字员工服务账号" required>
              <el-select
                v-model="form.serviceUserId"
                data-testid="agent-service-select"
                filterable
                placeholder="选择未绑定的服务账号"
              >
                <el-option
                  v-for="identity in options?.serviceIdentities ?? []"
                  :key="identity.id"
                  :label="identity.boundAgentProfileId
                    ? `${identity.displayName} · 当前身份`
                    : `${identity.displayName} (${identity.userName})`"
                  :value="identity.id"
                />
              </el-select>
            </el-form-item>
          </el-form>
          <el-alert
            type="info"
            :closable="false"
            show-icon
            title="服务账号只用于受控运行和短期令牌；不能登录治理页面，也不能自审或代替真人确认。"
          />
        </section>
      </el-tab-pane>

      <el-tab-pane label="自主等级与限额" name="autonomy">
        <section class="editor-section">
          <h3>自主等级</h3>
          <el-radio-group v-model="form.autonomyLevel" class="autonomy-grid">
            <el-radio
              v-for="(value, level) in autonomyLabels"
              :key="level"
              :value="level"
              :data-testid="`agent-autonomy-${level}`"
              border
            >
              <strong>{{ level }} · {{ value.action }}</strong>
              <small>{{ value.boundary }}</small>
            </el-radio>
          </el-radio-group>
          <div class="field-grid field-grid--three">
            <el-form-item label="风险上限">
              <el-select v-model="form.riskLimit" data-testid="agent-risk-limit">
                <el-option
                  v-for="(label, risk) in riskLabels"
                  :key="risk"
                  :label="label"
                  :value="risk"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="金额上限">
              <el-input-number
                v-model="form.amountLimit"
                data-testid="agent-amount-limit"
                :min="0"
                :step="100"
                controls-position="right"
              />
              <small class="field-note">当前：{{ formatAmount(form.amountLimit) }}</small>
            </el-form-item>
            <el-form-item label="权重说明">
              <div class="readonly-value">超限时由后端直接拒绝并转人工处理</div>
            </el-form-item>
          </div>
        </section>
      </el-tab-pane>

      <el-tab-pane label="工具与能力" name="tools">
        <section class="editor-section">
          <h3>可用工具</h3>
          <p class="section-help">
            只能组合系统已注册的工具。不会出现 SQL、Shell、任意 HTTP、任意文件系统或代码执行能力。
          </p>
          <div class="capability-list">
            <label
              v-for="tool in toolRows"
              :key="tool.code"
              class="capability-row"
              :class="{ selected: tool.selected }"
            >
              <el-checkbox
                :model-value="tool.selected"
                :data-testid="`agent-tool-${tool.code}`"
                @update:model-value="toggleTool(tool.code, $event === true)"
              />
              <span>
                <strong>{{ tool.name }}</strong>
                <small>{{ tool.description }}</small>
              </span>
              <span class="capability-meta">
                <StatusBadge
                  :label="riskLabels[tool.riskLevel]"
                  :tone="tool.riskLevel === 'High'
                    ? 'danger'
                    : tool.riskLevel === 'Medium' ? 'warning' : 'success'"
                />
                <StatusBadge
                  v-if="tool.requiresConfirmation"
                  label="需人工确认"
                  tone="warning"
                />
              </span>
            </label>
          </div>
        </section>
      </el-tab-pane>

      <el-tab-pane label="人工确认与熔断" name="boundaries">
        <section class="editor-section">
          <h3>永久人工边界</h3>
          <div class="boundary-tags">
            <el-tag
              v-for="boundary in permanentBoundaries"
              :key="boundary"
              type="danger"
              effect="plain"
            >
              {{ confirmationLabels[boundary] }}
            </el-tag>
          </div>
          <p class="section-help">
            这些边界由后端永久保留，任何草稿、审批或运行都不能移除。
          </p>
          <h3>附加确认规则</h3>
          <el-checkbox-group v-model="form.requiredConfirmations" class="option-grid">
            <el-checkbox
              v-for="rule in optionalConfirmations"
              :key="rule"
              :value="rule"
            >
              {{ confirmationLabels[rule] }}
            </el-checkbox>
          </el-checkbox-group>
          <h3>熔断条件</h3>
          <el-checkbox-group v-model="form.stopConditions" class="option-grid">
            <el-checkbox
              v-for="condition in options?.stopConditions ?? []"
              :key="condition"
              :value="condition"
            >
              {{ stopConditionLabels[condition] }}
            </el-checkbox>
          </el-checkbox-group>
        </section>
      </el-tab-pane>

      <el-tab-pane label="数据与模型" name="scope">
        <section class="editor-section">
          <h3>数据范围</h3>
          <el-form label-position="top">
            <el-form-item label="授权部门">
              <el-select v-model="departmentScopeIds" multiple filterable collapse-tags>
                <el-option
                  v-for="department in options?.departments ?? []"
                  :key="department.id"
                  :label="department.name"
                  :value="department.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="组织级范围">
              <el-checkbox v-model="orgScope">允许在组织授权范围内运行</el-checkbox>
            </el-form-item>
          </el-form>
          <h3>模型范围</h3>
          <el-checkbox-group v-model="form.modelScopes" class="option-grid">
            <el-checkbox
              v-for="model in options?.modelScopes ?? []"
              :key="model"
              :value="model"
            >
              {{ model === 'aidt-rule-sim' ? 'RuleSimulator 规则模拟' : model }}
            </el-checkbox>
          </el-checkbox-group>
          <el-alert
            type="warning"
            :closable="false"
            show-icon
            title="外部连接边界"
            description="当前不调用真实外部模型，不连接钉钉、U9、ERP 或 DCS；AiGateway 仅执行规则模拟。"
          />
        </section>
      </el-tab-pane>
    </el-tabs>

    <el-alert
      v-if="validation && !validation.isValid"
      class="validation-alert"
      type="error"
      :closable="false"
      show-icon
      title="校验未通过"
      :description="validation.errors.join('；')"
    />
    <el-alert
      v-else-if="validation?.isHighRisk"
      class="validation-alert"
      type="warning"
      :closable="false"
      show-icon
      title="高风险变更：发布后进入双人审核"
      :description="validation.highRiskReasons.join('；')"
    />
    <el-alert
      v-else-if="validation?.isValid"
      class="validation-alert"
      type="success"
      :closable="false"
      show-icon
      title="校验通过，可以发布"
    />

    <template #footer>
      <div class="editor-footer">
        <el-button @click="close">取消</el-button>
        <el-button
          data-testid="validate-agent-draft"
          :loading="validating"
          :disabled="mode !== 'edit'"
          @click="validate"
        >
          校验草稿
        </el-button>
        <el-button
          data-testid="publish-agent-draft"
          type="warning"
          :loading="publishing"
          :disabled="mode !== 'edit' || !validation?.isValid || !canPublish"
          @click="publish"
        >
          发布或提交审核
        </el-button>
        <el-button
          data-testid="save-agent-draft"
          type="primary"
          :loading="saving"
          @click="submit"
        >
          保存草稿
        </el-button>
      </div>
    </template>
  </el-dialog>
</template>

<style scoped>
.editor-tabs {
  min-height: 560px;
}

.editor-section {
  padding: 0 12px 8px;
}

.editor-section h3 {
  margin: 4px 0 6px;
  font-size: 1rem;
}

.section-help,
.field-note {
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
  line-height: 1.55;
}

.field-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 14px;
}

.field-grid--three {
  grid-template-columns: repeat(3, minmax(0, 1fr));
}

.autonomy-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.autonomy-grid :deep(.el-radio) {
  height: auto;
  min-height: 70px;
  margin: 0;
  padding: 10px 12px;
  white-space: normal;
}

.autonomy-grid strong,
.autonomy-grid small {
  display: block;
}

.autonomy-grid small {
  margin-top: 4px;
  color: var(--aidt-ink-muted);
}

.capability-list {
  display: grid;
  gap: 8px;
}

.capability-row {
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto;
  gap: 10px;
  align-items: center;
  border: 1px solid var(--aidt-border);
  border-radius: 10px;
  padding: 10px;
  cursor: pointer;
}

.capability-row.selected {
  border-color: var(--aidt-brand);
  background: color-mix(in srgb, var(--aidt-brand) 5%, white);
}

.capability-row strong,
.capability-row small {
  display: block;
}

.capability-row small {
  margin-top: 3px;
  color: var(--aidt-ink-muted);
  font-size: 0.72rem;
}

.capability-meta,
.boundary-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.option-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 8px 16px;
  margin: 8px 0 18px;
}

.readonly-value {
  min-height: 32px;
  padding: 7px 10px;
  border-radius: 7px;
  background: var(--aidt-surface-muted);
  color: var(--aidt-ink-muted);
  font-size: 0.78rem;
}

.validation-alert {
  margin-top: 12px;
}

.editor-footer {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 8px;
}

@media (max-width: 760px) {
  .editor-tabs {
    min-height: 0;
  }

  .editor-tabs :deep(.el-tabs__header) {
    float: none;
    margin: 0 0 14px;
  }

  .editor-tabs :deep(.el-tabs__nav-wrap) {
    overflow-x: auto;
  }

  .editor-tabs :deep(.el-tabs__nav) {
    flex-wrap: nowrap;
  }

  .field-grid,
  .field-grid--three,
  .autonomy-grid,
  .option-grid {
    grid-template-columns: 1fr;
  }

  .capability-row {
    grid-template-columns: auto minmax(0, 1fr);
  }

  .capability-meta {
    grid-column: 2;
  }
}
</style>
