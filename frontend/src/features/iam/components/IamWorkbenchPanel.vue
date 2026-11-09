<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { workbenchApi } from '@/api/modules/workbenchApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import type {
  DepartmentTreeNodeDto,
  ObjectTypeDto,
  RoleDto,
  WorkbenchAssignmentWriteRequest,
  WorkbenchCardAdminDto,
  WorkbenchCardWriteRequest,
  WorkbenchTemplateDto,
  WorkbenchTemplateValidationDto,
  WorkbenchTemplateVersionDto,
} from '@/api/types/contracts'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{ tree: DepartmentTreeNodeDto[]; roles: RoleDto[] }>()
const emit = defineEmits<{ changed: [] }>()
const auth = useAuthStore()

const templates = ref<WorkbenchTemplateDto[]>([])
const selected = ref<WorkbenchTemplateDto | null>(null)
const cards = ref<WorkbenchCardWriteRequest[]>([])
const assignments = ref<WorkbenchAssignmentWriteRequest[]>([])
const versions = ref<WorkbenchTemplateVersionDto[]>([])
const publishedObjectTypes = ref<ObjectTypeDto[]>([])
const loading = ref(false)
const validation = ref<WorkbenchTemplateValidationDto | null>(null)
const createVisible = ref(false)
const copyVisible = ref(false)
const cardVisible = ref(false)
const previewVisible = ref(false)
const historyVisible = ref(false)
const form = ref({
  name: '',
  description: '',
  audienceTitle: '',
  primaryQuestion: '',
  departmentId: '',
  sortOrder: 100,
  isDefault: false,
})
const createForm = ref({
  code: '',
  name: '',
  description: '',
  audienceTitle: '',
  primaryQuestion: '',
  departmentId: '',
  sortOrder: 100,
  isDefault: false,
})
const copyForm = ref({ code: '', name: '', departmentId: '', audienceTitle: '' })
const cardForm = ref<WorkbenchCardWriteRequest>({
  cardType: 'work_items',
  title: '',
  description: '',
  sortOrder: 10,
  width: 'Third',
  dataSourceType: 'ExistingApi',
  filter: { top: '5' },
  requiredResource: 'dashboard',
  requiredAction: 'Read',
  isEnabled: true,
  capabilityState: 'Connected',
})

const canManage = computed(() => auth.can('workbench-template:manage'))
const canPublish = computed(() => auth.can('workbench-template:approve'))
const departments = computed(() => flatten(props.tree).filter((item) => item.isActive))
const editableVersion = computed(
  () => selected.value?.draftVersion ?? selected.value?.publishedVersion ?? null,
)
const cardTypes = [
  ['work_items', '我的工作事项'],
  ['workflow_tasks', '流程待办'],
  ['project_portfolio', '项目组合'],
  ['project_tasks', '项目任务'],
  ['business_objects', '业务对象'],
  ['recent_files', '最近文件'],
  ['minutes', '最近听记'],
  ['overdue_risks', '逾期和风险'],
  ['team_pending', '部门成员待办'],
  ['ai_activity', 'Agent 活动'],
] as const

function flatten(nodes: DepartmentTreeNodeDto[]): DepartmentTreeNodeDto[] {
  return nodes.flatMap((node) => [node, ...flatten(node.children)])
}

function adminCards(version: WorkbenchTemplateVersionDto | null): WorkbenchCardWriteRequest[] {
  return (version?.cards ?? []).map((card: WorkbenchCardAdminDto) => ({
    cardType: card.cardType,
    title: card.title,
    description: card.description,
    sortOrder: card.sortOrder,
    width: card.width,
    dataSourceType: card.dataSourceType,
    filter: { ...card.filter },
    requiredResource: card.requiredResource,
    requiredAction: card.requiredAction,
    isEnabled: card.isEnabled,
    capabilityState: card.capabilityState,
  }))
}

function adminAssignments(
  version: WorkbenchTemplateVersionDto | null,
): WorkbenchAssignmentWriteRequest[] {
  return (version?.assignments ?? []).map((item) => ({
    assignmentType: item.assignmentType,
    scopeValue: item.scopeValue,
    priority: item.priority,
  }))
}

async function load() {
  loading.value = true
  try {
    ;[templates.value, publishedObjectTypes.value] = await Promise.all([
      workbenchApi.listTemplates(),
      workspaceApi.publishedObjectTypes(),
    ])
    if (!selected.value && templates.value.length) {
      selectTemplate(templates.value[0])
    } else if (selected.value) {
      const refreshed = templates.value.find((item) => item.id === selected.value?.id)
      if (refreshed) selectTemplate(refreshed)
    }
  } catch (error) {
    templates.value = []
    ElMessage.error(error instanceof Error ? error.message : '读取工作台模板失败')
  } finally {
    loading.value = false
  }
}

function objectCodeSelection(filter: Record<string, string | null>): string[] {
  return (filter.objectCodes ?? '')
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)
}

function setObjectCodeSelection(
  filter: Record<string, string | null>,
  values: string[],
) {
  filter.objectCodes = values.length ? values.join(',') : null
}

function selectTemplate(template: WorkbenchTemplateDto) {
  selected.value = template
  form.value = {
    name: template.name,
    description: template.description,
    audienceTitle: template.audienceTitle,
    primaryQuestion: template.primaryQuestion,
    departmentId: template.departmentId ?? '',
    sortOrder: template.sortOrder,
    isDefault: template.isDefault,
  }
  cards.value = adminCards(template.draftVersion ?? template.publishedVersion)
  assignments.value = adminAssignments(template.draftVersion ?? template.publishedVersion)
  validation.value = null
}

function openCreate() {
  createForm.value = {
    code: '',
    name: '',
    description: '',
    audienceTitle: '',
    primaryQuestion: '',
    departmentId: departments.value.find((item) => item.hierarchyType !== 'Company')?.id ?? '',
    sortOrder: 100,
    isDefault: false,
  }
  createVisible.value = true
}

async function createTemplate() {
  loading.value = true
  try {
    const created = await workbenchApi.createTemplate({
      ...createForm.value,
      departmentId: createForm.value.departmentId || null,
    })
    createVisible.value = false
    selected.value = created
    await load()
    ElMessage.success('业务工作台模板草稿已创建')
  } finally {
    loading.value = false
  }
}

function openCopy() {
  if (!selected.value) return
  copyForm.value = {
    code: '',
    name: `${selected.value.name}（业务副本）`,
    departmentId: selected.value.departmentId ?? departments.value[0]?.id ?? '',
    audienceTitle: selected.value.audienceTitle,
  }
  copyVisible.value = true
}

async function copyTemplate() {
  if (!selected.value) return
  loading.value = true
  try {
    const copied = await workbenchApi.copyTemplate(selected.value.id, {
      ...copyForm.value,
      departmentId: copyForm.value.departmentId || null,
    })
    copyVisible.value = false
    ElMessage.success('系统模板已复制为可编辑业务模板')
    selected.value = copied
    await load()
  } finally {
    loading.value = false
  }
}

function addCard() {
  if (!cardForm.value.title.trim()) {
    ElMessage.warning('请填写卡片标题')
    return
  }
  cards.value.push({
    ...cardForm.value,
    description: cardForm.value.description || null,
    sortOrder: (cards.value.length + 1) * 10,
    filter: { ...cardForm.value.filter },
  })
  cardVisible.value = false
}

function moveCard(index: number, direction: -1 | 1) {
  const target = index + direction
  if (target < 0 || target >= cards.value.length) return
  const next = [...cards.value]
  ;[next[index], next[target]] = [next[target], next[index]]
  next.forEach((card, cardIndex) => {
    card.sortOrder = (cardIndex + 1) * 10
  })
  cards.value = next
}

function addAssignment() {
  assignments.value.push({ assignmentType: 'Department', scopeValue: '', priority: 0 })
}

function removeAssignment(index: number) {
  assignments.value.splice(index, 1)
}

async function saveDraft() {
  const template = selected.value
  if (!template) return
  loading.value = true
  try {
    const updated = await workbenchApi.updateDraft(template.id, {
      expectedRevision: editableVersion.value?.revision ?? template.version,
      name: form.value.name,
      description: form.value.description,
      audienceTitle: form.value.audienceTitle,
      primaryQuestion: form.value.primaryQuestion,
      departmentId: form.value.departmentId || null,
      sortOrder: form.value.sortOrder,
      isDefault: form.value.isDefault,
      cards: cards.value,
      assignments: assignments.value.filter((item) => item.scopeValue.trim()),
    })
    selected.value = updated
    selectTemplate(updated)
    await load()
    ElMessage.success('工作台草稿已保存，线上版本未变化')
  } finally {
    loading.value = false
  }
}

async function validateTemplate() {
  if (!selected.value) return
  validation.value = await workbenchApi.validate(selected.value.id)
  if (validation.value.isValid) {
    ElMessage.success(validation.value.impactSummary)
  } else {
    ElMessage.error(validation.value.errors.join('；'))
  }
}

async function publishTemplate() {
  if (!selected.value) return
  await ElMessageBox.confirm(
    '发布后新版本立即影响匹配范围内用户，草稿内容不会继续覆盖线上版本。',
    '发布岗位工作台模板',
    { type: 'warning', confirmButtonText: '确认发布', cancelButtonText: '取消' },
  )
  loading.value = true
  try {
    const updated = await workbenchApi.publish(selected.value.id)
    selected.value = updated
    selectTemplate(updated)
    await load()
    emit('changed')
    ElMessage.success('工作台模板已发布')
  } finally {
    loading.value = false
  }
}

async function toggleStatus() {
  if (!selected.value) return
  const retire = selected.value.status !== 'Retired'
  if (retire) {
    await ElMessageBox.confirm(
      '停用只停止模板匹配，不删除卡片配置或业务数据。',
      '停用工作台模板',
      { type: 'warning', confirmButtonText: '确认停用', cancelButtonText: '取消' },
    )
  }
  loading.value = true
  try {
    const updated = await workbenchApi.setStatus(selected.value.id, retire)
    selected.value = updated
    selectTemplate(updated)
    await load()
    emit('changed')
    ElMessage.success(retire ? '模板已停用，业务数据未删除' : '模板已恢复')
  } finally {
    loading.value = false
  }
}

async function showHistory() {
  if (!selected.value) return
  versions.value = await workbenchApi.versions(selected.value.id)
  historyVisible.value = true
}

function assignmentOptions(type: WorkbenchAssignmentWriteRequest['assignmentType']) {
  if (type === 'Department') {
    return departments.value.map((item) => ({ value: item.id, label: item.name }))
  }
  if (type === 'Role') {
    return props.roles.map((item) => ({ value: item.code, label: item.name }))
  }
  if (type === 'Organization') {
    return [{ value: auth.session.organizationId ?? '', label: '当前组织' }]
  }
  return []
}

onMounted(load)
</script>

<template>
  <div class="iam-grid workbench-admin-grid" data-testid="iam-workbench-panel">
    <section class="iam-card role-list">
      <div class="card-heading">
        <div>
          <h2>工作台模板</h2>
          <p>模板只组合现有能力，发布后按岗位匹配。</p>
        </div>
        <el-button v-if="canManage" type="primary" @click="openCreate">新建业务模板</el-button>
      </div>
      <button
        v-for="template in templates"
        :key="template.id"
        class="role-item"
        :class="{ active: selected?.id === template.id }"
        @click="selectTemplate(template)"
      >
        <span>
          <strong>{{ template.name }}</strong>
          <small>
            {{ template.code }} · {{ template.departmentName || '组织级' }}
            <template v-if="template.isSystem"> · 系统保留</template>
          </small>
        </span>
        <el-tag :type="template.status === 'Published' ? 'success' : template.status === 'Retired' ? 'info' : 'warning'">
          {{ template.status }}
        </el-tag>
      </button>
      <el-empty v-if="!templates.length && !loading" description="没有可管理工作台模板" />
    </section>

    <section v-if="selected" class="iam-card">
      <div class="card-heading">
        <div>
          <p class="eyebrow">{{ selected.departmentName || '组织级模板' }}</p>
          <h2>{{ selected.name }}</h2>
          <p>
            当前草稿 Revision {{ selected.draftVersion?.revision ?? selected.version }}
            · 线上版本 v{{ selected.publishedVersion?.versionNumber ?? '—' }}
          </p>
        </div>
        <div class="heading-actions">
          <el-button @click="previewVisible = true">预览</el-button>
          <el-button @click="showHistory">版本历史</el-button>
          <el-button v-if="canManage" @click="openCopy">复制</el-button>
          <el-button v-if="canManage" :loading="loading" @click="saveDraft">保存草稿</el-button>
          <el-button v-if="canManage" @click="validateTemplate">校验与影响</el-button>
          <el-button
            v-if="canPublish"
            type="primary"
            :loading="loading"
            data-testid="publish-workbench-template"
            @click="publishTemplate"
          >
            发布
          </el-button>
          <el-button
            v-if="canManage"
            :type="selected.status === 'Retired' ? 'success' : 'danger'"
            plain
            :disabled="selected.isSystem && selected.status !== 'Retired'"
            @click="toggleStatus"
          >
            {{ selected.status === 'Retired' ? '恢复' : '停用' }}
          </el-button>
        </div>
      </div>

      <el-alert
        v-if="selected.isSystem"
        type="warning"
        :closable="false"
        title="系统模板不能删除或停用；可复制为业务模板后修改。"
      />

      <el-form label-position="top" class="workbench-form">
        <div class="form-grid">
          <el-form-item label="模板名称">
            <el-input v-model="form.name" :disabled="!canManage" />
          </el-form-item>
          <el-form-item label="岗位名称">
            <el-input v-model="form.audienceTitle" :disabled="!canManage" />
          </el-form-item>
          <el-form-item label="适用部门">
            <el-select v-model="form.departmentId" :disabled="!canManage">
              <el-option label="组织级" value="" />
              <el-option
                v-for="department in departments"
                :key="department.id"
                :label="department.name"
                :value="department.id"
              />
            </el-select>
          </el-form-item>
          <el-form-item label="排序">
            <el-input-number v-model="form.sortOrder" :min="0" :max="9999" :disabled="!canManage" />
          </el-form-item>
        </div>
        <el-form-item label="首屏问题">
          <el-input v-model="form.primaryQuestion" :disabled="!canManage" />
        </el-form-item>
        <el-form-item label="说明">
          <el-input v-model="form.description" type="textarea" :rows="2" :disabled="!canManage" />
        </el-form-item>
        <el-checkbox v-model="form.isDefault" :disabled="!canManage">作为同名范围默认模板</el-checkbox>
      </el-form>

      <section class="config-section">
        <div class="section-heading">
          <div>
            <h3>首屏卡片</h3>
            <p>仅可引用已注册卡片类型；数据权限在各业务模块再次校验。</p>
          </div>
          <el-button v-if="canManage" @click="cardVisible = true">添加卡片</el-button>
        </div>
        <div class="card-config-list">
          <article v-for="(card, index) in cards" :key="`${card.cardType}-${index}`" class="card-config">
            <div class="card-config-main">
              <el-input v-model="card.title" :disabled="!canManage" />
              <small>{{ card.cardType }} · {{ card.requiredResource }}:{{ card.requiredAction }}</small>
            <small v-if="card.cardType === 'business_objects' && card.filter.objectCodes">
              类型范围：{{ card.filter.objectCodes }}
            </small>
            </div>
            <el-select v-model="card.width" :disabled="!canManage" class="compact-select">
              <el-option label="1/3" value="Third" />
              <el-option label="1/2" value="Half" />
              <el-option label="整行" value="Full" />
            </el-select>
            <el-switch v-model="card.isEnabled" :disabled="!canManage" />
            <div class="order-actions">
              <el-button text size="small" :disabled="!canManage" @click="moveCard(index, -1)">↑</el-button>
              <el-button text size="small" :disabled="!canManage" @click="moveCard(index, 1)">↓</el-button>
              <el-button text type="danger" size="small" :disabled="!canManage" @click="cards.splice(index, 1)">移除</el-button>
            </div>
          </article>
        </div>
      </section>

      <section class="config-section">
        <div class="section-heading">
          <div>
            <h3>匹配范围</h3>
            <p>优先级：用户级 &gt; 角色级 &gt; 部门级 &gt; 组织默认。</p>
          </div>
          <el-button v-if="canManage" @click="addAssignment">添加范围</el-button>
        </div>
        <div class="assignment-list">
          <article v-for="(assignment, index) in assignments" :key="index" class="assignment-row">
            <el-select
              v-model="assignment.assignmentType"
              :disabled="!canManage"
              class="compact-select"
              @change="assignment.scopeValue = ''"
            >
              <el-option label="部门" value="Department" />
              <el-option label="角色" value="Role" />
              <el-option label="组织默认" value="Organization" />
            </el-select>
            <el-select v-model="assignment.scopeValue" :disabled="!canManage" class="scope-select">
              <el-option
                v-for="option in assignmentOptions(assignment.assignmentType)"
                :key="option.value"
                :label="option.label"
                :value="option.value"
              />
            </el-select>
            <el-input-number v-model="assignment.priority" :min="0" :max="999" :disabled="!canManage" />
            <el-button text type="danger" :disabled="!canManage" @click="removeAssignment(index)">移除</el-button>
          </article>
          <el-empty v-if="!assignments.length" description="至少配置一个匹配范围" />
        </div>
      </section>

      <el-alert
        v-if="validation"
        :type="validation.isValid ? 'success' : 'error'"
        :closable="false"
        :title="validation.impactSummary"
      >
        <div v-for="error in validation.errors" :key="error">{{ error }}</div>
        <div v-for="warning in validation.warnings" :key="warning">{{ warning }}</div>
      </el-alert>
    </section>

    <el-dialog v-model="createVisible" title="新建业务工作台模板" width="min(620px, 94vw)">
      <el-form label-position="top">
        <el-form-item label="模板编码"><el-input v-model="createForm.code" placeholder="WS-CUSTOM-RND" /></el-form-item>
        <el-form-item label="模板名称"><el-input v-model="createForm.name" /></el-form-item>
        <el-form-item label="岗位名称"><el-input v-model="createForm.audienceTitle" /></el-form-item>
        <el-form-item label="首屏问题"><el-input v-model="createForm.primaryQuestion" /></el-form-item>
        <el-form-item label="适用部门">
          <el-select v-model="createForm.departmentId">
            <el-option label="组织级（仅平台管理员）" value="" />
            <el-option v-for="item in departments" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="说明"><el-input v-model="createForm.description" type="textarea" :rows="3" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="createTemplate">创建草稿</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="copyVisible" title="复制为业务模板" width="min(560px, 94vw)">
      <el-form label-position="top">
        <el-form-item label="新编码"><el-input v-model="copyForm.code" /></el-form-item>
        <el-form-item label="新名称"><el-input v-model="copyForm.name" /></el-form-item>
        <el-form-item label="适用部门">
          <el-select v-model="copyForm.departmentId">
            <el-option v-for="item in departments" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="copyVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="copyTemplate">复制</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="cardVisible" title="添加工作台卡片" width="min(680px, 94vw)">
      <el-form label-position="top">
        <div class="form-grid">
          <el-form-item label="卡片类型">
            <el-select v-model="cardForm.cardType" data-testid="workbench-card-type-select">
              <el-option v-for="[value, label] in cardTypes" :key="value" :label="label" :value="value" />
            </el-select>
          </el-form-item>
          <el-form-item label="标题"><el-input v-model="cardForm.title" /></el-form-item>
          <el-form-item label="宽度">
            <el-select v-model="cardForm.width">
              <el-option label="1/3" value="Third" />
              <el-option label="1/2" value="Half" />
              <el-option label="整行" value="Full" />
            </el-select>
          </el-form-item>
          <el-form-item label="权限资源"><el-input v-model="cardForm.requiredResource" /></el-form-item>
          <el-form-item label="权限动作"><el-input v-model="cardForm.requiredAction" /></el-form-item>
          <el-form-item label="过滤关键词">
            <el-input
              :model-value="cardForm.filter.keyword ?? ''"
              @update:model-value="cardForm.filter.keyword = $event || null"
            />
          </el-form-item>
          <el-form-item v-if="cardForm.cardType === 'business_objects'" label="业务对象类型范围">
            <el-select
              :model-value="objectCodeSelection(cardForm.filter)"
              multiple
              filterable
              style="width: 100%"
              data-testid="workbench-object-types"
              @update:model-value="setObjectCodeSelection(cardForm.filter, $event)"
            >
              <el-option
                v-for="type in publishedObjectTypes"
                :key="type.id"
                :label="`${type.name} · ${type.code}`"
                :value="type.code"
              />
            </el-select>
          </el-form-item>
        </div>
        <el-form-item label="说明">
          <el-input v-model="cardForm.description" type="textarea" :rows="2" />
        </el-form-item>
        <el-checkbox v-model="cardForm.isEnabled">启用</el-checkbox>
      </el-form>
      <template #footer>
        <el-button @click="cardVisible = false">取消</el-button>
        <el-button type="primary" @click="addCard">添加</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="previewVisible" title="首屏配置预览" width="min(860px, 95vw)">
      <p class="preview-question">{{ form.primaryQuestion }}</p>
      <div class="preview-grid">
        <article v-for="card in cards.filter((item) => item.isEnabled)" :key="card.title" :class="`preview-${card.width.toLowerCase()}`">
          <small>{{ card.cardType }}</small>
          <strong>{{ card.title }}</strong>
          <span>{{ card.description || '启用后按当前用户权限加载真实数据。' }}</span>
        </article>
      </div>
    </el-dialog>

    <el-dialog v-model="historyVisible" title="版本历史与影响范围" width="min(760px, 94vw)">
      <el-timeline>
        <el-timeline-item
          v-for="version in versions"
          :key="version.id"
          :timestamp="version.publishedAt || version.createdAt"
          :type="version.status === 'Published' ? 'success' : 'info'"
        >
          <strong>v{{ version.versionNumber }} · {{ version.status }}</strong>
          <p>{{ version.cards.length }} 张卡片 / {{ version.assignments.length }} 个匹配范围</p>
          <pre>{{ version.impactAnalysisJson || '无影响分析' }}</pre>
        </el-timeline-item>
      </el-timeline>
    </el-dialog>
  </div>
</template>

<style scoped>
.workbench-form,
.config-section {
  margin-top: 18px;
}

.form-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0 14px;
}

.section-heading,
.card-config,
.assignment-row,
.order-actions {
  display: flex;
  align-items: center;
  gap: 12px;
}

.section-heading,
.card-config {
  justify-content: space-between;
}

.section-heading {
  align-items: flex-start;
}

.section-heading h3,
.section-heading p {
  margin: 0;
}

.section-heading p {
  margin-top: 4px;
  color: var(--aidt-ink-muted);
  font-size: 0.76rem;
}

.card-config-list,
.assignment-list {
  display: grid;
  gap: 9px;
  margin-top: 12px;
}

.card-config,
.assignment-row {
  border: 1px solid var(--aidt-border);
  border-radius: 13px;
  padding: 11px;
}

.card-config-main {
  min-width: 0;
  flex: 1;
}

.card-config-main small {
  display: block;
  margin-top: 4px;
  color: var(--aidt-ink-muted);
}

.compact-select {
  width: 110px;
}

.scope-select {
  min-width: 210px;
  flex: 1;
}

.preview-question {
  color: var(--aidt-brand-strong);
  font-size: 1.05rem;
  font-weight: 700;
}

.preview-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 10px;
}

.preview-grid article {
  display: grid;
  gap: 5px;
  border: 1px solid var(--aidt-border);
  border-radius: 14px;
  background: #fff;
  padding: 14px;
}

.preview-grid small,
.preview-grid span {
  color: var(--aidt-ink-muted);
}

.preview-full {
  grid-column: 1 / -1;
}

pre {
  max-width: 100%;
  overflow-x: auto;
  border-radius: 8px;
  background: #f5f7f6;
  font-size: 0.7rem;
  padding: 8px;
  white-space: pre-wrap;
}

@media (max-width: 720px) {
  .form-grid,
  .preview-grid {
    grid-template-columns: 1fr;
  }

  .card-config,
  .assignment-row {
    align-items: stretch;
    flex-direction: column;
  }

  .compact-select,
  .scope-select {
    width: 100%;
  }
}
</style>
