<script setup lang="ts">
import { computed, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { iamApi } from '@/api/modules/iamApi'
import type {
  DepartmentTreeNodeDto,
  IamUserListItemDto,
} from '@/api/types/contracts'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{
  tree: DepartmentTreeNodeDto[]
  users: IamUserListItemDto[]
}>()
const emit = defineEmits<{ changed: [] }>()
const auth = useAuthStore()
const canManageOrganization = computed(() => auth.can('identity-admin:manage'))

const selected = ref<DepartmentTreeNodeDto | null>(props.tree[0] ?? null)
const createVisible = ref(false)
const saving = ref(false)
const form = ref({
  code: '',
  name: '',
  parentId: null as string | null,
  hierarchyType: 'Department' as 'Center' | 'Department' | 'Team',
  managerUserId: null as string | null,
  sortOrder: 100,
})
const editName = ref('')
const editManagerId = ref<string | null>(null)
const editSortOrder = ref(0)

const isCompany = computed(() => selected.value?.hierarchyType === 'Company')
const managerOptions = computed(() =>
  props.users.filter((user) => user.isActive && !user.isServiceIdentity),
)

function selectNode(data: DepartmentTreeNodeDto) {
  selected.value = data
  editName.value = data.name
  editManagerId.value = data.managerUserId
  editSortOrder.value = data.sortOrder
}

function openCreate() {
  form.value = {
    code: '',
    name: '',
    parentId: selected.value && !isCompany.value ? selected.value.id : null,
    hierarchyType: 'Department',
    managerUserId: null,
    sortOrder: 100,
  }
  createVisible.value = true
}

async function createDepartment() {
  saving.value = true
  try {
    await iamApi.createDepartment(form.value)
    ElMessage.success('组织单元已创建并写入审计')
    createVisible.value = false
    emit('changed')
  } finally {
    saving.value = false
  }
}

async function saveSelected() {
  const node = selected.value
  if (!node || isCompany.value) return
  saving.value = true
  try {
    await iamApi.updateDepartment(node.id, {
      expectedVersion: node.version,
      name: editName.value,
      parentId: node.parentId,
      hierarchyType: node.hierarchyType as 'Center' | 'Department' | 'Team',
      managerUserId: editManagerId.value || null,
      sortOrder: editSortOrder.value,
      isActive: node.isActive,
    })
    ElMessage.success('组织信息已更新并写入审计')
    emit('changed')
  } finally {
    saving.value = false
  }
}

async function toggleStatus() {
  const node = selected.value
  if (!node || isCompany.value) return
  try {
    await iamApi.setDepartmentStatus(node.id, !node.isActive)
    ElMessage.success(node.isActive ? '组织已停用' : '组织已恢复')
    emit('changed')
  } catch {
    // API error is displayed by the global message layer.
  }
}
</script>

<template>
  <div class="iam-grid" data-testid="iam-organization-panel">
    <section class="iam-card iam-tree-card">
      <div class="card-heading">
        <div>
          <h2>组织架构</h2>
          <p>公司 → 中心 → 部门 → 班组；人员主数据不会复制。</p>
        </div>
        <el-button
          v-if="canManageOrganization"
          data-testid="create-department-button"
          type="primary"
          @click="openCreate"
        >
          新增组织单元
        </el-button>
      </div>
      <el-tree
        :data="tree"
        node-key="id"
        default-expand-all
        :expand-on-click-node="false"
        :props="{ label: 'name', children: 'children' }"
        @node-click="selectNode"
      >
        <template #default="{ data }">
          <span class="tree-node" :class="{ inactive: !data.isActive }">
            <strong>{{ data.name }}</strong>
            <small>{{ data.hierarchyType }} · {{ data.code }}</small>
          </span>
        </template>
      </el-tree>
    </section>

    <section class="iam-card">
      <template v-if="selected">
        <div class="card-heading">
          <div>
            <p class="eyebrow">当前组织单元</p>
            <h2>{{ selected.name }}</h2>
          </div>
          <el-tag :type="selected.isActive ? 'success' : 'danger'">
            {{ selected.isActive ? '启用' : '停用' }}
          </el-tag>
        </div>
        <dl class="iam-details">
          <div><dt>编码</dt><dd>{{ selected.code }}</dd></div>
          <div><dt>层级</dt><dd>{{ selected.hierarchyType }}</dd></div>
          <div><dt>路径</dt><dd>{{ selected.path }}</dd></div>
          <div><dt>负责人</dt><dd>{{ selected.managerDisplayName || '未设置' }}</dd></div>
        </dl>
        <template v-if="!isCompany">
          <el-form
            label-position="top"
            class="iam-form"
            :disabled="!canManageOrganization"
            @submit.prevent="saveSelected"
          >
            <el-form-item label="组织名称">
              <el-input v-model="editName" />
            </el-form-item>
            <el-form-item label="负责人">
              <el-select v-model="editManagerId" clearable placeholder="请选择负责人">
                <el-option
                  v-for="user in managerOptions"
                  :key="user.id"
                  :label="`${user.displayName} · ${user.userName}`"
                  :value="user.id"
                />
              </el-select>
            </el-form-item>
            <el-form-item label="排序">
              <el-input-number v-model="editSortOrder" :min="0" :max="9999" />
            </el-form-item>
            <div class="form-actions">
              <el-button v-if="canManageOrganization" type="primary" native-type="submit" :loading="saving">保存变更</el-button>
              <el-button v-if="canManageOrganization" :type="selected.isActive ? 'danger' : 'success'" plain @click="toggleStatus">
                {{ selected.isActive ? '停用组织' : '恢复组织' }}
              </el-button>
            </div>
          </el-form>
        </template>
      </template>
      <el-empty v-else description="请选择组织节点" />
    </section>

    <el-dialog v-model="createVisible" title="新增组织单元" width="min(520px, 92vw)">
      <el-form label-position="top">
        <el-form-item label="组织编码"><el-input v-model="form.code" /></el-form-item>
        <el-form-item label="组织名称"><el-input v-model="form.name" /></el-form-item>
        <el-form-item label="层级类型">
          <el-select v-model="form.hierarchyType">
            <el-option label="中心" value="Center" />
            <el-option label="部门" value="Department" />
            <el-option label="班组" value="Team" />
          </el-select>
        </el-form-item>
        <el-form-item label="负责人">
          <el-select v-model="form.managerUserId" clearable>
            <el-option
              v-for="user in managerOptions"
              :key="user.id"
              :label="`${user.displayName} · ${user.userName}`"
              :value="user.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="排序">
          <el-input-number v-model="form.sortOrder" :min="0" :max="9999" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="createDepartment">创建</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.tree-node {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  gap: 10px;
  padding-right: 8px;
}

.tree-node small {
  color: var(--aidt-ink-muted);
}

.tree-node.inactive {
  opacity: 0.5;
}
</style>
