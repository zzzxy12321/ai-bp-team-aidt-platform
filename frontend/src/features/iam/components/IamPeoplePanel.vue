<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { iamApi } from '@/api/modules/iamApi'
import type {
  DepartmentTreeNodeDto,
  IamUserDetailDto,
  IamUserListItemDto,
  SetUserDataScopesRequest,
} from '@/api/types/contracts'
import { useAuthStore } from '@/stores/auth'

const props = defineProps<{
  users: IamUserListItemDto[]
  tree: DepartmentTreeNodeDto[]
}>()
const emit = defineEmits<{ changed: [] }>()
const auth = useAuthStore()

const inviteVisible = ref(false)
const importVisible = ref(false)
const scopeVisible = ref(false)
const membershipVisible = ref(false)
const detail = ref<IamUserDetailDto | null>(null)
const loading = ref(false)
const search = ref('')
const userRows = ref<IamUserListItemDto[]>([...props.users])
const page = ref(1)
const pageSize = ref(50)
const total = ref(props.users.length)

const invite = ref({
  userName: '',
  displayName: '',
  departmentId: '',
  isPrimary: true,
  isManager: false,
})
const importText = ref('')
const selectedScopes = ref<Array<{ scopeType: string; scopeReference: string | null }>>([])
const scopeType = ref('department')
const scopeDepartmentId = ref('')
const membershipRows = ref<Array<{
  departmentId: string
  checked: boolean
  isPrimary: boolean
  isManager: boolean
}>>([])

function flatten(nodes: DepartmentTreeNodeDto[]): DepartmentTreeNodeDto[] {
  return nodes.flatMap((node) => [node, ...flatten(node.children)])
}

const departments = computed(() =>
  flatten(props.tree).filter((node) => node.hierarchyType !== 'Company'),
)

const canInvite = computed(() => auth.can('identity-admin:manage'))
const canUpdateScope = computed(() =>
  auth.can('identity-admin:manage') || auth.can('data-scope:update'),
)

async function loadUsers() {
  loading.value = true
  try {
    const result = await iamApi.listUsers(page.value, pageSize.value, search.value.trim())
    userRows.value = result.items
    total.value = result.total
    page.value = result.page
  } finally {
    loading.value = false
  }
}

async function searchUsers() {
  page.value = 1
  await loadUsers()
}

async function refreshAfterChange() {
  await loadUsers()
  emit('changed')
}

function openInvite() {
  invite.value = {
    userName: '',
    displayName: '',
    departmentId: departments.value[0]?.id ?? '',
    isPrimary: true,
    isManager: false,
  }
  inviteVisible.value = true
}

async function submitInvite() {
  loading.value = true
  try {
    await iamApi.inviteUser(invite.value)
    ElMessage.success('人员已邀请；当前为受控本地开通流程，不是生产邮件邀请')
    inviteVisible.value = false
    await refreshAfterChange()
  } finally {
    loading.value = false
  }
}

async function importUsers() {
  loading.value = true
  try {
    const lines = importText.value
      .split('\n')
      .map((line) => line.trim())
      .filter(Boolean)
    const users = lines.map((line) => {
      const [userName, displayName, departmentId] = line.split(',').map((item) => item.trim())
      if (!userName || !displayName || !departmentId) {
        throw new Error('每行格式必须为：登录名,姓名,部门ID')
      }
      return {
        userName,
        displayName,
        departmentId,
        isPrimary: true,
        isManager: false,
      }
    })
    await iamApi.importUsers(users)
    ElMessage.success(`已批量邀请 ${users.length} 名人员`)
    importVisible.value = false
    importText.value = ''
    await refreshAfterChange()
  } catch (error) {
    ElMessage.error(error instanceof Error ? error.message : '批量导入格式无效')
  } finally {
    loading.value = false
  }
}

async function toggleUser(user: IamUserListItemDto) {
  await iamApi.setUserStatus(user.id, !user.isActive)
  ElMessage.success(user.isActive ? '人员已停用' : '人员已开通/启用')
  await refreshAfterChange()
}

async function openScopes(user: IamUserListItemDto) {
  detail.value = await iamApi.getUser(user.id)
  selectedScopes.value = detail.value.dataScopes.map((scope) => ({
    scopeType: scope.scopeType,
    scopeReference: scope.scopeReference,
  }))
  scopeType.value = 'department'
  scopeDepartmentId.value = departments.value[0]?.id ?? ''
  scopeVisible.value = true
}

async function openMemberships(user: IamUserListItemDto) {
  detail.value = await iamApi.getUser(user.id)
  const activeMemberships = new Map(
    detail.value.memberships
      .filter((membership) => membership.isActive)
      .map((membership) => [membership.departmentId, membership]),
  )
  membershipRows.value = departments.value.map((department) => {
    const membership = activeMemberships.get(department.id)
    return {
      departmentId: department.id,
      checked: Boolean(membership),
      isPrimary: membership?.isPrimary ?? false,
      isManager: membership?.isManager ?? false,
    }
  })
  membershipVisible.value = true
}

function setPrimaryDepartment(departmentId: string) {
  for (const row of membershipRows.value) {
    row.checked = row.isPrimary ? true : row.checked
    row.isPrimary = row.departmentId === departmentId
    if (row.isPrimary) row.checked = true
  }
}

async function saveMemberships() {
  if (!detail.value) return
  const selected = membershipRows.value.filter((row) => row.checked)
  if (selected.length === 0 || selected.filter((row) => row.isPrimary).length !== 1) {
    ElMessage.warning('必须选择至少一个部门并指定唯一主部门')
    return
  }
  loading.value = true
  try {
    await iamApi.setUserMemberships(detail.value.user.id, {
      expectedVersion: detail.value.user.version,
      memberships: selected.map((row) => ({
        departmentId: row.departmentId,
        isPrimary: row.isPrimary,
        isManager: row.isManager,
        isActive: true,
      })),
    })
    ElMessage.success('主职与兼职部门已更新')
    membershipVisible.value = false
    await refreshAfterChange()
  } finally {
    loading.value = false
  }
}

function addScope() {
  const item = {
    scopeType: scopeType.value,
    scopeReference:
      scopeType.value === 'department'
        ? scopeDepartmentId.value
        : scopeType.value === 'project'
          ? 'participant'
          : scopeType.value === 'self'
            ? detail.value?.user.id ?? ''
            : null,
  }
  if (!selectedScopes.value.some((scope) => `${scope.scopeType}:${scope.scopeReference}` === `${item.scopeType}:${item.scopeReference}`)) {
    selectedScopes.value.push(item)
  }
}

async function saveScopes() {
  if (!detail.value) return
  loading.value = true
  try {
    const request: SetUserDataScopesRequest = {
      scopes: selectedScopes.value,
      expectedVersion: detail.value.user.version,
    }
    await iamApi.setUserDataScopes(detail.value.user.id, request)
    ElMessage.success('数据范围已更新；下次登录后重新装载权限声明')
    scopeVisible.value = false
    await refreshAfterChange()
  } finally {
    loading.value = false
  }
}

onMounted(loadUsers)
</script>

<template>
  <section class="iam-card" data-testid="iam-people-panel">
    <div class="card-heading people-heading">
      <div>
        <h2>人员与部门归属</h2>
        <p>邀请、停用、启用、主职/兼职与数据范围均写入审计。</p>
      </div>
      <div class="heading-actions">
        <el-input
          v-model="search"
          clearable
          placeholder="搜索姓名 / 登录名 / 部门"
          @keyup.enter="searchUsers"
          @clear="searchUsers"
        />
        <el-button @click="searchUsers">搜索</el-button>
        <el-button v-if="canInvite" @click="importVisible = true">批量导入</el-button>
        <el-button v-if="canInvite" type="primary" data-testid="invite-user-button" @click="openInvite">
          邀请人员
        </el-button>
      </div>
    </div>
    <el-table v-loading="loading" :data="userRows" stripe>
      <el-table-column prop="displayName" label="姓名" min-width="120" />
      <el-table-column prop="userName" label="登录名" min-width="150" />
      <el-table-column prop="primaryDepartmentName" label="主部门" min-width="120" />
      <el-table-column label="角色" min-width="220">
        <template #default="{ row }">
          <el-tag v-for="role in row.roleCodes" :key="role" size="small" effect="plain">
            {{ role }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="数据范围" min-width="160">
        <template #default="{ row }">{{ row.dataScopeTypes.join(' / ') || '未配置' }}</template>
      </el-table-column>
      <el-table-column label="状态" width="100">
        <template #default="{ row }">
          <el-tag :type="row.isActive ? 'success' : 'info'">
            {{ row.isActive ? '启用' : row.invitationStatus }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="280" fixed="right">
        <template #default="{ row }">
          <el-button v-if="canInvite" text type="primary" @click="openMemberships(row)">部门归属</el-button>
          <el-button v-if="canUpdateScope" text type="primary" @click="openScopes(row)">数据范围</el-button>
          <el-button v-if="canInvite" text :type="row.isActive ? 'danger' : 'success'" @click="toggleUser(row)">
            {{ row.isActive ? '停用' : '开通' }}
          </el-button>
        </template>
      </el-table-column>
    </el-table>
    <div class="people-pagination">
      <span>共 {{ total }} 人 · 第 {{ page }} 页</span>
      <el-pagination
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[20, 50, 100]"
        layout="sizes, prev, pager, next"
        @current-change="loadUsers"
        @size-change="searchUsers"
      />
    </div>

    <el-dialog v-model="inviteVisible" title="邀请人员（本地受控开通）" width="min(520px, 92vw)">
      <el-alert
        type="info"
        :closable="false"
        title="当前未接入邮件/钉钉邀请；人员创建为 Invited，需管理员执行本地开通。"
      />
      <el-form label-position="top">
        <el-form-item label="登录名"><el-input v-model="invite.userName" /></el-form-item>
        <el-form-item label="姓名"><el-input v-model="invite.displayName" /></el-form-item>
        <el-form-item label="主部门">
          <el-select v-model="invite.departmentId">
            <el-option
              v-for="department in departments"
              :key="department.id"
              :label="`${department.name} · ${department.code}`"
              :value="department.id"
            />
          </el-select>
        </el-form-item>
        <el-form-item label="管理标识">
          <el-switch v-model="invite.isManager" active-text="部门负责人" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="inviteVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="submitInvite">邀请</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="importVisible" title="批量导入人员" width="min(680px, 94vw)">
      <p>每行格式：登录名,姓名,部门ID。导入为受控本地邀请，最多 200 行。</p>
      <el-input v-model="importText" type="textarea" :rows="10" placeholder="demo.user1,用户一,部门UUID" />
      <template #footer>
        <el-button @click="importVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="importUsers">导入</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="scopeVisible" title="数据范围配置" width="min(640px, 94vw)">
      <template v-if="detail">
        <p><strong>{{ detail.user.displayName }}</strong> · {{ detail.user.userName }}</p>
        <div class="scope-builder">
          <el-select v-model="scopeType">
            <el-option label="组织" value="organization" />
            <el-option label="部门" value="department" />
            <el-option label="本人" value="self" />
            <el-option label="参与项目" value="project" />
          </el-select>
          <el-select v-if="scopeType === 'department'" v-model="scopeDepartmentId">
            <el-option
              v-for="department in departments"
              :key="department.id"
              :label="department.name"
              :value="department.id"
            />
          </el-select>
          <el-button @click="addScope">添加范围</el-button>
        </div>
        <div class="scope-list">
          <el-tag
            v-for="(scope, index) in selectedScopes"
            :key="`${scope.scopeType}:${scope.scopeReference}`"
            closable
            @close="selectedScopes.splice(index, 1)"
          >
            {{ scope.scopeType }} · {{ scope.scopeReference || '参与项目' }}
          </el-tag>
        </div>
      </template>
      <template #footer>
        <el-button @click="scopeVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="saveScopes">保存</el-button>
      </template>
    </el-dialog>

    <el-dialog v-model="membershipVisible" title="主职与兼职部门" width="min(680px, 94vw)">
      <template v-if="detail">
        <p><strong>{{ detail.user.displayName }}</strong> · {{ detail.user.userName }}</p>
        <el-table :data="membershipRows">
          <el-table-column label="所属" width="80">
            <template #default="{ row }">
              <el-checkbox v-model="row.checked" />
            </template>
          </el-table-column>
          <el-table-column label="部门" min-width="180">
            <template #default="{ row }">
              {{ departments.find((department) => department.id === row.departmentId)?.name }}
            </template>
          </el-table-column>
          <el-table-column label="主职" width="100">
            <template #default="{ row }">
              <el-radio
                :model-value="row.isPrimary"
                :value="true"
                :disabled="!row.checked"
                @update:model-value="setPrimaryDepartment(row.departmentId)"
              >
                主部门
              </el-radio>
            </template>
          </el-table-column>
          <el-table-column label="负责人" width="100">
            <template #default="{ row }">
              <el-switch v-model="row.isManager" :disabled="!row.checked" />
            </template>
          </el-table-column>
        </el-table>
      </template>
      <template #footer>
        <el-button @click="membershipVisible = false">取消</el-button>
        <el-button type="primary" :loading="loading" @click="saveMemberships">保存</el-button>
      </template>
    </el-dialog>
  </section>
</template>

<style scoped>
.people-pagination {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 14px;
  color: var(--aidt-ink-muted);
}

@media (max-width: 720px) {
  .people-pagination {
    align-items: flex-start;
    flex-direction: column;
  }
}
</style>
