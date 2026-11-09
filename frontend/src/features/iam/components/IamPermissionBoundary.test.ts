import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import IamPeoplePanel from './IamPeoplePanel.vue'
import IamRolesPanel from './IamRolesPanel.vue'
import { iamApi } from '@/api/modules/iamApi'
import { useAuthStore } from '@/stores/auth'

vi.mock('@/api/modules/iamApi', () => ({
  iamApi: {
    listUsers: vi.fn(),
    inviteUser: vi.fn(),
    importUsers: vi.fn(),
    getUser: vi.fn(),
    setUserStatus: vi.fn(),
    setUserDataScopes: vi.fn(),
    setUserMemberships: vi.fn(),
    getRoleMembers: vi.fn(),
    setRoleMembers: vi.fn(),
    setRolePermissions: vi.fn(),
    createRole: vi.fn(),
    copyRole: vi.fn(),
    updateRole: vi.fn(),
    setRoleStatus: vi.fn(),
  },
}))

const tree = [
  {
    id: 'org',
    parentId: null,
    code: 'PSD',
    name: '普赛达',
    hierarchyType: 'Company' as const,
    path: '/',
    sortOrder: 0,
    managerUserId: null,
    managerDisplayName: null,
    version: 1,
    isActive: true,
    children: [
      {
        id: 'rnd',
        parentId: 'org',
        code: 'RND',
        name: '研发',
        hierarchyType: 'Department' as const,
        path: '/RND',
        sortOrder: 10,
        managerUserId: null,
        managerDisplayName: null,
        version: 1,
        isActive: true,
        children: [],
      },
    ],
  },
]

const users = [
  {
    id: 'u1',
    userName: 'demo.researcher',
    displayName: '研发工程师',
    isServiceIdentity: false,
    isActive: true,
    invitationStatus: 'Active',
    primaryDepartmentId: 'rnd',
    primaryDepartmentName: '研发',
    version: 1,
    roleCodes: ['rd-engineer'],
    dataScopeTypes: ['department'],
    createdAt: '2026-11-01T00:00:00Z',
  },
]

const roles = [
  {
    id: 'role-1',
    code: 'business-manager-reviewer',
    name: '经理审核角色',
    roleType: 'Business' as const,
    description: null,
    isSystem: false,
    isActive: true,
    version: 1,
    memberCount: 1,
    permissionCount: 1,
    createdAt: '2026-11-01T00:00:00Z',
    updatedAt: '2026-11-01T00:00:00Z',
    permissionIds: [],
  },
]

const stubs = {
  ElButton: { template: '<button><slot /></button>' },
  ElInput: true,
  ElTable: { template: '<div><slot /></div>' },
  ElTableColumn: true,
  ElTag: { template: '<span><slot /></span>' },
  ElDialog: { template: '<div><slot /></div>' },
  ElForm: { template: '<form><slot /></form>' },
  ElFormItem: { template: '<div><slot /></div>' },
  ElSelect: { template: '<div><slot /></div>' },
  ElOption: { template: '<div><slot /></div>' },
  ElCheckboxGroup: { template: '<div><slot /></div>' },
  ElCheckbox: { template: '<label><slot /></label>' },
  ElPagination: true,
  ElAlert: true,
  ElSwitch: true,
  ElRadio: true,
}

describe('IAM action permission boundaries', () => {
  let pinia: ReturnType<typeof createPinia>

  beforeEach(() => {
    vi.clearAllMocks()
    pinia = createPinia()
    setActivePinia(pinia)
    vi.mocked(iamApi.listUsers).mockResolvedValue({
      items: users,
      page: 1,
      pageSize: 50,
      total: 75,
    })
  })

  it('shows only role actions available to a project manager', () => {
    const auth = useAuthStore()
    auth.permissions = [
      { resource: 'identity-admin', action: 'Read' },
      { resource: 'role', action: 'Read' },
      { resource: 'permission', action: 'Assign' },
      { resource: 'workflow-role', action: 'Manage' },
    ]
    const wrapper = mount(IamRolesPanel, {
      props: { roles, users, permissions: [], tree },
      global: { plugins: [pinia], stubs },
    })

    expect(wrapper.text()).toContain('分配成员')
    expect(wrapper.text()).not.toContain('新增角色')
    expect(wrapper.text()).not.toContain('保存权限')
    expect(wrapper.text()).not.toContain('停用角色')
  })

  it('hides people management actions and requests server-side pagination', async () => {
    const auth = useAuthStore()
    auth.permissions = [{ resource: 'identity-admin', action: 'Read' }]
    const wrapper = mount(IamPeoplePanel, {
      props: { users, tree },
      global: {
        plugins: [pinia],
        stubs,
        directives: { loading: {} },
      },
    })
    await flushPromises()

    expect(iamApi.listUsers).toHaveBeenCalledWith(1, 50, '')
    expect(wrapper.text()).toContain('共 75 人')
    expect(wrapper.text()).not.toContain('邀请人员')
    expect(wrapper.text()).not.toContain('批量导入')
    expect(wrapper.findAll('button').some((button) => button.text() === '停用')).toBe(false)
  })
})
