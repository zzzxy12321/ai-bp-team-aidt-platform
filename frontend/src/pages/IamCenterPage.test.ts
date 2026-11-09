import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import IamCenterPage from './IamCenterPage.vue'
import { iamApi } from '@/api/modules/iamApi'
import { useAuthStore } from '@/stores/auth'

vi.mock('@/api/modules/iamApi', () => ({
  iamApi: {
    getSnapshot: vi.fn(),
    listAllUsers: vi.fn(),
    approveAccessRequest: vi.fn(),
    rejectAccessRequest: vi.fn(),
  },
}))

describe('IamCenterPage', () => {
  let pinia: ReturnType<typeof createPinia>

  beforeEach(() => {
    vi.clearAllMocks()
    pinia = createPinia()
    setActivePinia(pinia)
    vi.mocked(iamApi.listAllUsers).mockResolvedValue([])
    vi.mocked(iamApi.getSnapshot).mockResolvedValue({
      organizationTree: [
        {
          id: 'org',
          parentId: null,
          code: 'PSD',
          name: '普赛达',
          hierarchyType: 'Company',
          path: '/',
          sortOrder: 0,
          managerUserId: null,
          managerDisplayName: null,
          version: 1,
          isActive: true,
          children: [],
        },
      ],
      users: { items: [], page: 1, pageSize: 50, total: 0 },
      permissions: [],
      roles: [],
      pendingAccessRequests: [
        {
          id: 'request-1',
          requestType: 'RolePermissions',
          targetUserId: null,
          targetRoleId: 'role-1',
          riskReason: '高风险权限需要第二人审核',
          status: 'Pending',
          requestedBy: 'user-1',
          requestedByDisplayName: '平台管理员',
          requestedAt: '2026-11-01T00:00:00Z',
          reviewedBy: null,
          reviewedAt: null,
          reviewComment: null,
        },
      ],
    })
  })

  it('loads IAM snapshot and renders the governance sections', async () => {
    const auth = useAuthStore()
    auth.permissions = [{ resource: 'identity-admin', action: 'Manage' }]
    const wrapper = mount(IamCenterPage, {
      global: {
        plugins: [pinia],
        stubs: {
          ElButton: { template: '<button><slot /></button>' },
          ElAlert: true,
          ElSkeleton: true,
          ElTabs: { template: '<div><slot /></div>' },
          ElTabPane: { template: '<section><slot /></section>' },
          IamOrganizationPanel: { template: '<div>组织架构面板</div>' },
          IamPeoplePanel: { template: '<div>人员面板</div>' },
          IamRolesPanel: { template: '<div>权限矩阵面板</div>' },
          IamNavigationPanel: { template: '<div>导航面板</div>' },
          IamWorkbenchPanel: { template: '<div>工作台模板面板</div>' },
          ElEmpty: true,
        },
      },
    })

    await flushPromises()

    expect(iamApi.getSnapshot).toHaveBeenCalledTimes(1)
    expect(iamApi.listAllUsers).toHaveBeenCalledTimes(1)
    expect(wrapper.get('h1').text()).toContain('IAM 与导航')
    expect(wrapper.text()).toContain('组织架构面板')
    expect(wrapper.text()).toContain('权限矩阵面板')
    expect(wrapper.text()).toContain('复核通过')
    expect(wrapper.text()).toContain('复核驳回')
  })
})
