<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { navigationApi } from '@/api/modules/navigationApi'
import { useNavigationStore } from '@/stores/navigation'
import { useAuthStore } from '@/stores/auth'
import type {
  DepartmentTreeNodeDto,
  NavigationItemWriteRequest,
  NavigationWorkspaceDto,
} from '@/api/types/contracts'

const props = defineProps<{ tree: DepartmentTreeNodeDto[] }>()
const navigationStore = useNavigationStore()
const auth = useAuthStore()

const workspaces = ref<NavigationWorkspaceDto[]>([])
const selected = ref<NavigationWorkspaceDto | null>(null)
const editItems = ref<NavigationItemWriteRequest[]>([])
const name = ref('')
const template = ref('DepartmentWorkspace')
const loading = ref(false)
const createVisible = ref(false)
const addItemVisible = ref(false)
const createForm = ref({
  workspaceKey: '',
  name: '',
  workspaceType: 'DepartmentWorkspace',
  departmentId: '',
  template: 'DepartmentWorkspace',
  optionalEntries: [] as string[],
})
const addItemForm = ref({
  itemKey: '',
  name: '',
  itemType: 'BusinessObject',
  route: '/objects',
  requiredResource: 'business-object',
  requiredAction: 'Read',
  sortOrder: 200,
})
const canManageNavigation = computed(() => auth.can('navigation:manage'))
const canPublishNavigation = computed(() => auth.can('navigation:approve'))

function flatten(nodes: DepartmentTreeNodeDto[]): DepartmentTreeNodeDto[] {
  return nodes.flatMap((node) => [node, ...flatten(node.children)])
}

const departments = computed(() =>
  flatten(props.tree).filter((node) => node.hierarchyType !== 'Company' && node.isActive),
)

async function loadWorkspaces() {
  workspaces.value = await navigationApi.listWorkspaces()
  if (!selected.value && workspaces.value.length) {
    await selectWorkspace(workspaces.value[0])
  } else if (selected.value) {
    const refreshed = workspaces.value.find((item) => item.id === selected.value?.id)
    if (refreshed) await selectWorkspace(refreshed)
  }
}

async function selectWorkspace(workspace: NavigationWorkspaceDto) {
  selected.value = workspace
  name.value = workspace.name
  editItems.value = workspace.items.map((item) => ({
    itemKey: item.itemKey,
    name: item.name,
    itemType: item.itemType,
    route: item.route,
    requiredResource: item.requiredResource,
    requiredAction: item.requiredAction,
    sortOrder: item.sortOrder,
    isVisible: item.isVisible,
    isEnabled: item.isEnabled,
    isSystem: item.isSystem,
    icon: item.icon,
  }))
}

function openCreate() {
  createForm.value = {
    workspaceKey: '',
    name: '',
    workspaceType: 'DepartmentWorkspace',
    departmentId: departments.value[0]?.id ?? '',
    template: 'DepartmentWorkspace',
    optionalEntries: ['Projects', 'Minutes'],
  }
  createVisible.value = true
}

async function createWorkspace() {
  loading.value = true
  try {
    const created = await navigationApi.createWorkspace({
      ...createForm.value,
      departmentId: createForm.value.departmentId || null,
    })
    ElMessage.success('导航工作区已创建，默认入口已形成草稿')
    createVisible.value = false
    selected.value = created
    await loadWorkspaces()
  } finally {
    loading.value = false
  }
}

function addItem() {
  editItems.value.push({
    ...addItemForm.value,
    isVisible: true,
    isEnabled: true,
    isSystem: false,
    icon: addItemForm.value.name.slice(0, 1),
  })
  addItemVisible.value = false
}

function moveItem(index: number, direction: -1 | 1) {
  const target = index + direction
  if (target < 0 || target >= editItems.value.length) return
  const rows = [...editItems.value]
  ;[rows[index], rows[target]] = [rows[target], rows[index]]
  rows.forEach((item, itemIndex) => {
    item.sortOrder = (itemIndex + 1) * 10
  })
  editItems.value = rows
}

async function saveDraft() {
  const workspace = selected.value
  if (!workspace) return
  loading.value = true
  try {
    const updated = await navigationApi.updateDraft(workspace.id, {
      expectedVersion:
        workspace.editingVersionStatus === 'Draft' ? workspace.revision : workspace.version,
      name: name.value,
      template: template.value,
      items: editItems.value,
    })
    selected.value = updated
    await selectWorkspace(updated)
    ElMessage.success('导航草稿已保存；系统保留入口未被删除')
    await loadWorkspaces()
  } finally {
    loading.value = false
  }
}

async function publish() {
  const workspace = selected.value
  if (!workspace) return
  loading.value = true
  try {
    const updated = await navigationApi.publish(workspace.id)
    selected.value = updated
    await selectWorkspace(updated)
    ElMessage.success('导航版本已发布，后端会按用户权限返回导航树')
    await loadWorkspaces()
    await navigationStore.load(true)
  } finally {
    loading.value = false
  }
}

async function toggleStatus() {
  const workspace = selected.value
  if (!workspace) return
  const retire = workspace.status !== 'Retired'
  const updated = await navigationApi.setStatus(workspace.id, retire)
  selected.value = updated
  await selectWorkspace(updated)
  ElMessage.success(retire ? '工作区已停止展示，业务数据未删除' : '工作区已恢复展示')
  await loadWorkspaces()
  await navigationStore.load(true)
}

onMounted(loadWorkspaces)
</script>

<template>
  <div class="iam-grid nav-admin-grid" data-testid="iam-navigation-panel">
    <section class="iam-card role-list">
      <div class="card-heading">
        <div>
          <h2>导航工作区</h2>
          <p>工作区引用现有部门；删除仅停止展示。</p>
        </div>
        <el-button
          v-if="canManageNavigation"
          type="primary"
          data-testid="create-navigation-button"
          @click="openCreate"
        >
          创建工作区
        </el-button>
      </div>
      <button
        v-for="workspace in workspaces"
        :key="workspace.id"
        class="role-item"
        :class="{ active: selected?.id === workspace.id }"
        @click="selectWorkspace(workspace)"
      >
        <span>
          <strong>{{ workspace.name }}</strong>
          <small>{{ workspace.workspaceKey }} · v{{ workspace.draftVersionNumber }}</small>
        </span>
        <el-tag :type="workspace.status === 'Published' ? 'success' : workspace.status === 'Retired' ? 'info' : 'warning'">
          {{ workspace.status }}
        </el-tag>
      </button>
    </section>

    <section v-if="selected" class="iam-card">
      <div class="card-heading">
        <div>
          <p class="eyebrow">{{ selected.departmentName || '组织级导航' }}</p>
          <el-input v-model="name" class="nav-title-input" :disabled="!canManageNavigation" />
          <p>版本 v{{ selected.draftVersionNumber }} · Revision {{ selected.revision }}</p>
        </div>
        <div class="heading-actions">
          <el-button v-if="canManageNavigation" @click="addItemVisible = true">新增入口</el-button>
          <el-button v-if="canManageNavigation" :loading="loading" @click="saveDraft">保存草稿</el-button>
          <el-button
            v-if="canPublishNavigation"
            type="primary"
            :loading="loading"
            data-testid="publish-navigation-button"
            @click="publish"
          >
            发布
          </el-button>
          <el-button
            v-if="canManageNavigation"
            :type="selected.status === 'Retired' ? 'success' : 'danger'"
            plain
            :disabled="selected.isSystem && selected.status !== 'Retired'"
            @click="toggleStatus"
          >
            {{ selected.status === 'Retired' ? '恢复入口' : '停止展示' }}
          </el-button>
        </div>
      </div>
      <el-alert
        v-if="selected.isSystem"
        type="warning"
        :closable="false"
        title="系统工作区不能停用；我的工作、待办、事件收件箱不能隐藏或删除。"
      />
      <el-table :data="editItems" row-key="itemKey">
        <el-table-column label="顺序" width="130">
          <template #default="{ row, $index }">
            <el-input-number
              v-model="row.sortOrder"
              :min="0"
              :max="9999"
              size="small"
              :disabled="!canManageNavigation"
            />
            <div class="order-buttons">
              <el-button v-if="canManageNavigation" text size="small" @click="moveItem($index, -1)">↑</el-button>
              <el-button v-if="canManageNavigation" text size="small" @click="moveItem($index, 1)">↓</el-button>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="入口" min-width="180">
          <template #default="{ row }">
            <el-input v-model="row.name" size="small" :disabled="!canManageNavigation" />
            <small>{{ row.itemKey }}<span v-if="row.isSystem"> · 系统保留</span></small>
          </template>
        </el-table-column>
        <el-table-column label="路由" min-width="160">
          <template #default="{ row }"><el-input v-model="row.route" size="small" :disabled="!canManageNavigation" /></template>
        </el-table-column>
        <el-table-column label="权限" min-width="170">
          <template #default="{ row }">
            <el-input v-model="row.requiredResource" size="small" placeholder="资源" :disabled="!canManageNavigation" />
            <el-input v-model="row.requiredAction" size="small" placeholder="动作" :disabled="!canManageNavigation" />
          </template>
        </el-table-column>
        <el-table-column label="显示" width="85">
          <template #default="{ row }">
            <el-switch v-model="row.isVisible" :disabled="row.isSystem || !canManageNavigation" />
          </template>
        </el-table-column>
        <el-table-column label="启用" width="85">
          <template #default="{ row }">
            <el-switch v-model="row.isEnabled" :disabled="row.isSystem || !canManageNavigation" />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="70">
          <template #default="{ $index, row }">
            <el-button
              text
              type="danger"
              :disabled="row.isSystem || !canManageNavigation"
              @click="editItems.splice($index, 1)"
            >
              移除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </section>

    <el-dialog v-model="createVisible" title="创建导航工作区" width="min(600px, 94vw)">
      <el-form label-position="top">
        <el-form-item label="工作区编码">
          <el-input
            v-model="createForm.workspaceKey"
            data-testid="nav-workspace-key"
            placeholder="RND_WORKSPACE"
          />
        </el-form-item>
        <el-form-item label="显示名称">
          <el-input
            v-model="createForm.name"
            data-testid="nav-workspace-name"
            placeholder="研发部"
          />
        </el-form-item>
        <el-form-item label="关联部门">
          <el-select v-model="createForm.departmentId" data-testid="nav-department-select">
            <el-option
              v-for="department in departments"
              :key="department.id"
              :label="`${department.name} · ${department.code}`"
              :value="department.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="默认模板">
          <el-select v-model="createForm.template">
            <el-option label="部门工作区" value="DepartmentWorkspace" />
            <el-option label="业务视图" value="BusinessView" />
          </el-select>
        </el-form-item>
        <el-form-item label="可选入口">
          <el-checkbox-group v-model="createForm.optionalEntries">
            <el-checkbox value="Projects">项目</el-checkbox>
            <el-checkbox value="Minutes">听记</el-checkbox>
            <el-checkbox value="Reports">报表</el-checkbox>
          </el-checkbox-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="createWorkspace">创建</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="addItemVisible" title="新增导航入口" width="min(520px, 92vw)">
      <el-form label-position="top">
        <el-form-item label="入口编码"><el-input v-model="addItemForm.itemKey" /></el-form-item>
        <el-form-item label="显示名称"><el-input v-model="addItemForm.name" /></el-form-item>
        <el-form-item label="路由"><el-input v-model="addItemForm.route" /></el-form-item>
        <el-form-item label="所需资源"><el-input v-model="addItemForm.requiredResource" /></el-form-item>
        <el-form-item label="所需动作"><el-input v-model="addItemForm.requiredAction" /></el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="addItemVisible = false">取消</el-button>
        <el-button type="primary" @click="addItem">添加</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.nav-title-input {
  max-width: 360px;
  margin-bottom: 4px;
}

.order-buttons {
  margin-top: 4px;
}

td small {
  display: block;
  color: var(--aidt-ink-muted);
}
</style>
