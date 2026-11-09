import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '@/api/http'
import { workspaceApi } from '@/api/modules/workspaceApi'
import { useAuthStore } from '@/stores/auth'
import BusinessModelingPage from './BusinessModelingPage.vue'

vi.mock('@/api/modules/workspaceApi', () => ({
  workspaceApi: {
    objectTypes: vi.fn(),
    businessObjects: vi.fn(),
    workflowDefinitions: vi.fn(),
    createObjectType: vi.fn(),
    updateObjectTypeDraft: vi.fn(),
    validateObjectType: vi.fn(),
    publishObjectType: vi.fn(),
    retireObjectType: vi.fn(),
    restoreObjectType: vi.fn(),
    objectTypeVersions: vi.fn(),
    objectFieldUsers: vi.fn(),
  },
}))

const objectType = {
  id: 'type-1',
  code: 'CUSTOMER_VISIT',
  name: '客户拜访记录',
  version: 2,
  status: 'Draft' as const,
  revision: 2,
  publishedAt: null,
  retiredAt: null,
  fields: [
    {
      key: 'Customer',
      displayName: '客户',
      dataType: 'Text',
      isRequired: true,
      maxLength: 100,
      sensitivity: 'Sensitive' as const,
      defaultValue: null,
      options: [],
      showInList: true,
      filterable: true,
      sortOrder: 10,
      code: 'Customer',
    },
  ],
}

let pinia: ReturnType<typeof createPinia>

function mountPage() {
  return mount(BusinessModelingPage, {
    global: {
      plugins: [pinia],
      stubs: {
        ElButton: { template: '<button><slot /></button>' },
        ElAlert: { props: ['title'], template: '<div>{{ title }}<slot /></div>' },
        ElEmpty: true,
        ElForm: { template: '<form><slot /></form>' },
        ElFormItem: { props: ['label'], template: '<label>{{ label }}<slot /></label>' },
        ElInput: true,
        ElInputNumber: true,
        ElSelect: true,
        ElOption: true,
        ElCheckbox: { template: '<label><slot /></label>' },
        ElDialog: { template: '<div><slot /></div>' },
        ElTimeline: true,
        ElTimelineItem: true,
        DynamicFieldInput: true,
      },
      directives: { loading: {} },
    },
  })
}

describe('BusinessModelingPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    pinia = createPinia()
    setActivePinia(pinia)
    const auth = useAuthStore()
    auth.session = {
      userId: 'admin',
      displayName: '平台管理员',
      organizationId: 'org',
      isAuthenticated: true,
      isServiceIdentity: false,
      roles: ['platform-admin'],
      authorizationState: 'Current',
      authorizationMessage: null,
    }
    auth.permissions = [
      { resource: 'object-model', action: 'Read' },
      { resource: 'object-model', action: 'Create' },
      { resource: 'object-model', action: 'Update' },
      { resource: 'object-model', action: 'Manage' },
      { resource: 'object-model', action: 'Approve' },
    ]
    vi.mocked(workspaceApi.objectTypes).mockResolvedValue({
      items: [objectType],
      page: 1,
      pageSize: 100,
      total: 1,
    })
    vi.mocked(workspaceApi.businessObjects).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 100,
      total: 0,
    })
    vi.mocked(workspaceApi.workflowDefinitions).mockResolvedValue([])
  })

  it('renders object lifecycle, field designer and impact scope', async () => {
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('h1').text()).toContain('业务建模工作室')
    expect(wrapper.text()).toContain('客户拜访记录')
    expect(wrapper.text()).toContain('字段设计器')
    expect(wrapper.text()).toContain('影响范围')
    expect(wrapper.find('[data-testid="save-model-draft"]').exists()).toBe(true)
  })

  it('shows a denied state when metadata access is rejected', async () => {
    vi.mocked(workspaceApi.objectTypes).mockRejectedValue(
      new ApiError('PERMISSION_DENIED', '无权限', 403, 'trace-denied'),
    )
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('[data-testid="modeling-denied-state"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('无权访问业务建模')
  })
})
