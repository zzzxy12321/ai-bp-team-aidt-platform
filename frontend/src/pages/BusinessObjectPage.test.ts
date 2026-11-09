import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { workspaceApi } from '@/api/modules/workspaceApi'
import { useAuthStore } from '@/stores/auth'
import BusinessObjectPage from './BusinessObjectPage.vue'

vi.mock('@/api/modules/workspaceApi', () => ({
  workspaceApi: {
    publishedObjectTypes: vi.fn(),
    businessObjects: vi.fn(),
    departments: vi.fn(),
    objectFieldUsers: vi.fn(),
    businessObject: vi.fn(),
    businessObjectVersions: vi.fn(),
    businessObjectContext: vi.fn(),
    projects: vi.fn(),
    createBusinessObject: vi.fn(),
    updateBusinessObject: vi.fn(),
    addProjectRelation: vi.fn(),
  },
}))

vi.mock('@/api/modules/fileApi', () => ({
  fileApi: {
    upload: vi.fn(),
    download: vi.fn(),
  },
}))

let pinia: ReturnType<typeof createPinia>

function mountPage() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [{ path: '/:pathMatch(.*)*', component: { template: '<div />' } }],
  })
  return mount(BusinessObjectPage, {
    global: {
      plugins: [pinia, router],
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
        ElTable: { props: ['data'], template: '<div><slot /></div>' },
        ElTableColumn: { template: '<div><slot /></div>' },
        ElDialog: { template: '<div><slot /></div>' },
        ElTabs: { template: '<div><slot /></div>' },
        ElTabPane: { template: '<div><slot /></div>' },
        ElTimeline: true,
        ElTimelineItem: true,
        DynamicFieldInput: true,
      },
      directives: { loading: {} },
    },
  })
}

describe('BusinessObjectPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    pinia = createPinia()
    setActivePinia(pinia)
    const auth = useAuthStore()
    auth.permissions = [
      { resource: 'business-object', action: 'Read' },
      { resource: 'business-object', action: 'Update' },
    ]
    vi.mocked(workspaceApi.departments).mockResolvedValue([
      {
        id: 'dept-rnd',
        code: 'RND',
        name: '研发',
        managerUserId: null,
        isActive: true,
        isPrimary: true,
      },
    ])
    vi.mocked(workspaceApi.objectFieldUsers).mockResolvedValue([])
  })

  it('renders published dynamic form and object workspace', async () => {
    vi.mocked(workspaceApi.publishedObjectTypes).mockResolvedValue([
      {
        id: 'type-1',
        code: 'CUSTOMER_VISIT',
        name: '客户拜访记录',
        version: 1,
        status: 'Published',
        revision: 2,
        publishedAt: '2026-11-02T00:00:00Z',
        retiredAt: null,
        fields: [
          {
            key: 'Customer',
            displayName: '客户',
            dataType: 'Text',
            isRequired: true,
            maxLength: 100,
            sensitivity: 'Sensitive',
            defaultValue: null,
            options: [],
            showInList: true,
            filterable: true,
            sortOrder: 10,
            code: 'Customer',
          },
        ],
      },
    ])
    vi.mocked(workspaceApi.businessObjects).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 100,
      total: 0,
    })
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.get('h1').text()).toContain('业务视图')
    expect(wrapper.text()).toContain('客户拜访记录')
    expect(wrapper.find('[data-testid="create-object-button"]').exists()).toBe(true)
  })

  it('shows an explicit not-published state when no schema is available', async () => {
    vi.mocked(workspaceApi.publishedObjectTypes).mockResolvedValue([])
    vi.mocked(workspaceApi.businessObjects).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 100,
      total: 0,
    })
    const wrapper = mountPage()
    await flushPromises()

    expect(wrapper.find('[data-testid="object-not-published-state"]').exists()).toBe(true)
    expect(wrapper.text()).toContain('暂无已发布业务表单')
  })
})
