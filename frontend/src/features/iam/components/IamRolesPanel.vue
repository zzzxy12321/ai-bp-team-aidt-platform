<script setup lang="ts">
import { computed, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { iamApi } from '@/api/modules/iamApi'
import type {
  DepartmentTreeNodeDto,
  IamUserListItemDto,
  PermissionCatalogItemDto,
  RoleDto,
  RoleMemberDto,
} from '@/api/types/contracts'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{
  roles: RoleDto[]
  users: IamUserListItemDto[]
  permissions: PermissionCatalogItemDto[]
  tree: DepartmentTreeNodeDto[]
}>()
const emit = defineEmits<{ changed: [] }>()
const auth = useAuthStore()

const selectedRole = ref<RoleDto | null>(props.roles[0] ?? null)
const members = ref<RoleMemberDto[]>([])
const selectedPermissionIds = ref<string[]>([])
const createVisible = ref(false)
const memberVisible = ref(false)
const loading = ref(false)
const roleForm = ref({
  code: '',
  name: '',
  roleType: 'Business' as 'Business' | 'AgentService',
  description: '',
})
const selectedUserIds = ref<string[]>([])
const assignScopeType = ref<'organization' | 'department'>('department')
const assignDepartmentId = ref('')
const canCreateRole = computed(() => auth.can('role:create'))
const canUpdateRole = computed(() => auth.can('role:update'))
const canManageRole = computed(() => auth.can('role:manage'))
const canAssignMembers = computed(() => auth.can('permission:assign'))
const canAssignPermissions = computed(() =>
  auth.can('role:manage') && auth.can('permission:assign'),
)

function flatten(nodes: DepartmentTreeNodeDto[]): DepartmentTreeNodeDto[] {
  return nodes.flatMap((node) => [node, ...flatten(node.children)])
}

const departments = computed(() =>
  flatten(props.tree).filter((node) => node.hierarchyType !== 'Company'),
)
const permissionGroups = computed(() => {
  const groups = new Map<string, PermissionCatalogItemDto[]>()
  for (const permission of props.permissions) {
    const rows = groups.get(permission.resource) ?? []
    rows.push(permission)
    groups.set(permission.resource, rows)
  }
  return [...groups.entries()].map(([resource, items]) => ({ resource, items }))
})

async function selectRole(role: RoleDto) {
  selectedRole.value = role
  selectedPermissionIds.value = [...(role.permissionIds ?? [])]
  members.value = await iamApi.getRoleMembers(role.id)
}

async function savePermissions() {
  const role = selectedRole.value
  if (!role) return
  loading.value = true
  try {
    await iamApi.setRolePermissions(role.id, {
      permissionIds: selectedPermissionIds.value,
      expectedRoleVersion: role.version,
    })
    ElMessage.success('角色权限已保存；高风险变更将进入双人审核队列')
    emit('changed')
  } finally {
    loading.value = false
  }
}

async function createRole() {
  loading.value = true
  try {
    await iamApi.createRole({
      ...roleForm.value,
      description: roleForm.value.description || null,
    })
    ElMessage.success('角色已创建并写入审计')
    createVisible.value = false
    emit('changed')
  } finally {
    loading.value = false
  }
}

async function copyRole() {
  const role = selectedRole.value
  if (!role) return
  const code = await ElMessageBox.prompt('请输入新角色编码', '复制角色', {
    inputValue: `${role.code}-COPY`.slice(0, 64),
    inputPattern: /^[A-Z][A-Z0-9_-]*$/,
    inputErrorMessage: '角色编码须为大写字母开头的稳定编码',
    confirmButtonText: '复制',
    cancelButtonText: '取消',
  })
  await iamApi.copyRole(role.id, {
    code: code.value,
    name: `${role.name}（副本）`,
    description: role.description,
  })
  ElMessage.success('角色及其权限已复制')
  emit('changed')
}

async function renameRole() {
  const role = selectedRole.value
  if (!role || role.isSystem) return
  const nameResult = await ElMessageBox.prompt('请输入新的角色名称', '编辑角色', {
    inputValue: role.name,
    inputPattern: /\S+/,
    inputErrorMessage: '角色名称不能为空',
    confirmButtonText: '保存',
    cancelButtonText: '取消',
  })
  await iamApi.updateRole(role.id, {
    expectedVersion: role.version,
    name: nameResult.value,
    roleType: role.roleType,
    description: role.description,
    isActive: role.isActive,
  })
  ElMessage.success('角色信息已更新')
  emit('changed')
}

async function toggleRole() {
  const role = selectedRole.value
  if (!role) return
  await iamApi.setRoleStatus(role.id, !role.isActive)
  ElMessage.success(role.isActive ? '角色已停用' : '角色已恢复')
  emit('changed')
}

async function openMembers() {
  if (!selectedRole.value) return
  members.value = await iamApi.getRoleMembers(selectedRole.value.id)
  selectedUserIds.value = [...new Set(members.value.filter((member) => member.isActive).map((member) => member.userId))]
  assignScopeType.value = auth.isPlatformAdmin ? 'organization' : 'department'
  assignDepartmentId.value = departments.value[0]?.id ?? ''
  memberVisible.value = true
}

async function saveMembers() {
  const role = selectedRole.value
  if (!role) return
  loading.value = true
  try {
    const activeMembers = members.value.filter((member) =>
      selectedUserIds.value.includes(member.userId) && member.isActive,
    )
    const existingUserIds = new Set(activeMembers.map((member) => member.userId))
    const additions = selectedUserIds.value
      .filter((userId) => !existingUserIds.has(userId))
      .map((userId) => ({
        userId,
        scopeType: assignScopeType.value,
        scopeReference:
          assignScopeType.value === 'organization'
            ? 'organization'
            : assignDepartmentId.value,
        isActive: true,
      }))
    await iamApi.setRoleMembers(role.id, {
      expectedRoleVersion: role.version,
      members: [
        ...activeMembers.map((member) => ({
          userId: member.userId,
          scopeType: member.scopeType as 'organization' | 'department',
          scopeReference: member.scopeReference,
          isActive: true,
        })),
        ...additions,
      ],
    })
    ElMessage.success('角色成员已更新；本人提权会在后端被拒绝')
    memberVisible.value = false
    emit('changed')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="iam-grid roles-grid" data-testid="iam-roles-panel">
    <section class="iam-card role-list">
      <div class="card-heading">
        <div>
          <h2>角色</h2>
          <p>系统角色、业务角色和 Agent 服务角色分别治理。</p>
        </div>
        <el-button v-if="canCreateRole" type="primary" @click="createVisible = true">新增角色</el-button>
      </div>
      <button
        v-for="role in roles"
        :key="role.id"
        class="role-item"
        :class="{ active: selectedRole?.id === role.id }"
        @click="selectRole(role)"
      >
        <span>
          <strong>{{ role.name }}</strong>
          <small>{{ role.code }} · {{ role.roleType }}</small>
        </span>
        <span>
          <el-tag v-if="role.isSystem" size="small" type="warning">系统</el-tag>
          <el-tag v-if="!role.isActive" size="small" type="info">停用</el-tag>
        </span>
      </button>
    </section>

    <section v-if="selectedRole" class="iam-card permission-matrix">
      <div class="card-heading">
        <div>
          <p class="eyebrow">权限矩阵</p>
          <h2>{{ selectedRole.name }}</h2>
          <p>{{ selectedRole.description || '未填写角色说明' }}</p>
        </div>
        <div class="heading-actions">
          <el-button v-if="canCreateRole" :disabled="selectedRole.isSystem" @click="copyRole">复制角色</el-button>
          <el-button v-if="canUpdateRole" :disabled="selectedRole.isSystem" @click="renameRole">编辑</el-button>
          <el-button v-if="canAssignMembers" @click="openMembers">分配成员</el-button>
          <el-button
            v-if="canManageRole"
            :type="selectedRole.isActive ? 'danger' : 'success'"
            plain
            :disabled="selectedRole.isSystem && selectedRole.isActive"
            @click="toggleRole"
          >
            {{ selectedRole.isActive ? '停用角色' : '恢复角色' }}
          </el-button>
          <el-button
            v-if="canAssignPermissions"
            type="primary"
            :loading="loading"
            :disabled="selectedRole.isSystem"
            @click="savePermissions"
          >
            保存权限
          </el-button>
        </div>
      </div>
      <el-alert
        v-if="selectedRole.isSystem"
        type="warning"
        :closable="false"
        title="系统保留角色不可删除、停用或改写权限。"
      />
      <div class="permission-sections">
        <section v-for="group in permissionGroups" :key="group.resource" class="permission-group">
          <h3>{{ group.resource }}</h3>
          <el-checkbox-group
            v-model="selectedPermissionIds"
            :disabled="selectedRole.isSystem || !canAssignPermissions"
          >
            <el-checkbox
              v-for="permission in group.items"
              :key="permission.id"
              :value="permission.id"
            >
              {{ permission.action }}
              <el-tag v-if="permission.riskLevel === 'High'" size="small" type="danger">高风险</el-tag>
            </el-checkbox>
          </el-checkbox-group>
        </section>
      </div>
      <div class="member-summary">
        <strong>{{ selectedRole.memberCount }}</strong> 名有效成员 ·
        <strong>{{ selectedRole.permissionCount }}</strong> 项权限
      </div>
    </section>

    <el-dialog v-model="createVisible" title="新增角色" width="min(520px, 92vw)">
      <el-form label-position="top">
        <el-form-item label="角色编码"><el-input v-model="roleForm.code" /></el-form-item>
        <el-form-item label="角色名称"><el-input v-model="roleForm.name" /></el-form-item>
        <el-form-item label="角色类型">
          <el-select v-model="roleForm.roleType">
            <el-option label="业务角色" value="Business" />
            <el-option label="Agent 服务角色" value="AgentService" />
          </el-select>
        </el-form-item>
        <el-form-item label="说明">
          <el-input v-model="roleForm.description" type="textarea" :rows="3" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="createVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="createRole">创建</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="memberVisible" title="分配角色成员" width="min(680px, 94vw)">
      <p>可限定组织或部门范围。本人提权、系统角色直授等高风险操作由后端阻断或进入双人审核。</p>
      <div class="scope-builder">
        <el-select v-model="assignScopeType">
          <el-option v-if="auth.isPlatformAdmin" label="组织范围" value="organization" />
          <el-option label="部门范围" value="department" />
        </el-select>
        <el-select v-if="assignScopeType === 'department'" v-model="assignDepartmentId">
          <el-option
            v-for="department in departments"
            :key="department.id"
            :label="department.name"
            :value="department.id"
          />
        </el-select>
      </div>
      <el-checkbox-group v-model="selectedUserIds" class="member-picker">
        <el-checkbox
          v-for="user in users.filter((item) => item.isActive && !item.isServiceIdentity)"
          :key="user.id"
          :value="user.id"
        >
          {{ user.displayName }} · {{ user.primaryDepartmentName }}
        </el-checkbox>
      </el-checkbox-group>
      <template #footer>
        <el-button @click="memberVisible = false">取消</el-button>
        <el-button v-if="canAssignMembers" type="primary" :loading="loading" @click="saveMembers">保存成员</el-button>
      </template>
    </el-dialog>
  </div>
</template>
