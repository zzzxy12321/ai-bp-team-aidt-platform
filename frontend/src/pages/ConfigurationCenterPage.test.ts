import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import ConfigurationCenterPage from './ConfigurationCenterPage.vue'
import { configurationApi } from '@/api/modules/configurationApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import { useAuthStore } from '@/stores/auth'

vi.mock('vue-router', () => ({
  useRoute: () => ({ query: {} }),
  useRouter: () => ({ push: vi.fn() }),
}))

vi.mock('@/api/modules/configurationApi', () => ({
  configurationApi: {
    list: vi.fn(),
    get: vi.fn(),
    memberCandidates: vi.fn(),
    workflowRoles: vi.fn(),
    create: vi.fn(),
    update: vi.fn(),
    validate: vi.fn(),
    publish: vi.fn(),
    rollback: vi.fn(),
  },
}))

vi.mock('@/api/modules/workspaceApi', () => ({
  workspaceApi: {
    departments: vi.fn(),
    agents: vi.fn(),
    publishedObjectTypes: vi.fn(),
  },
}))

const configuration = {
  id: 'c0000000-0000-0000-0000-000000000001',
  configurationKey: 'RND_TEST_FLOW',
  name: '研发测试申请与异常判断',
  description: '测试流程配置',
  configurationType: 'Workflow' as const,
  versionNumber: 2,
  state: 'Validated' as const,
  revision: 3,
  organizationId: '10000000-0000-0000-0000-000000000001',
  departmentId: 'f01311d9-9e4a-4328-8db8-616c8ef0c95f',
  departmentName: '研发',
  objectTypeCode: 'CUSTOMER_SAMPLE',
  startNode: 'INITIATE',
  nodes: [
    {
      code: 'INITIATE',
      name: '发起测试申请',
      assigneeUserName: 'demo.researcher',
      nextNode: 'QA_REVIEW',
      completeOnApprove: false,
          condition: null,
          assigneeStrategy: 'SpecificUser',
          businessRoleCode: null,
          agentProfileId: null,
    },
    {
      code: 'QA_REVIEW',
      name: '质量确认',
      assigneeUserName: 'demo.qa',
      nextNode: null,
      completeOnApprove: true,
          condition: null,
          assigneeStrategy: 'SpecificUser',
          businessRoleCode: null,
          agentProfileId: null,
    },
  ],
  createdBy: '10000000-0000-0000-0000-000000000021',
  createdByDisplayName: '项目经理',
  createdAt: '2026-11-01T02:00:00+08:00',
  updatedBy: '10000000-0000-0000-0000-000000000021',
  updatedByDisplayName: '项目经理',
  updatedAt: '2026-11-01T02:10:00+08:00',
  publishedBy: null,
  publishedByDisplayName: null,
  publishedAt: null,
  publishedWorkflowDefinitionId: null,
  publishedWorkflowVersionId: null,
  validation: null,
}

let pinia: ReturnType<typeof createPinia>

describe('ConfigurationCenterPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    pinia = createPinia()
    setActivePinia(pinia)
    const auth = useAuthStore()
    auth.permissions = [
      { resource: 'configuration', action: 'Read' },
      { resource: 'configuration', action: 'Create' },
      { resource: 'configuration', action: 'Update' },
      { resource: 'configuration', action: 'Approve' },
      { resource: 'configuration', action: 'Manage' },
    ]
    vi.mocked(configurationApi.list).mockResolvedValue({
      items: [configuration],
      page: 1,
      pageSize: 100,
      total: 1,
    })
    vi.mocked(configurationApi.get).mockResolvedValue(configuration)
    vi.mocked(configurationApi.memberCandidates).mockResolvedValue([
      { id: 'u1', userName: 'demo.researcher', displayName: '研发工程师' },
      { id: 'u2', userName: 'demo.qa', displayName: '质量工程师' },
    ])
    vi.mocked(configurationApi.workflowRoles).mockResolvedValue([
      {
        id: 'role-1',
        code: 'business-manager-reviewer',
        name: '经理审核角色',
        roleType: 'Business',
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
    ])
    vi.mocked(workspaceApi.departments).mockResolvedValue([
      {
        id: configuration.departmentId,
        code: 'RND',
        name: '研发',
        managerUserId: null,
        isActive: true,
        isPrimary: true,
      },
    ])
    vi.mocked(workspaceApi.agents).mockResolvedValue([
      {
        id: '60000000-0000-0000-0000-000000000001',
        name: '业务协同数字员工',
        autonomyLevel: 'A2',
        amountLimit: 0,
        riskLimit: 'Low',
        allowedTools: ['Draft'],
        isActive: true,
      },
    ])
    vi.mocked(workspaceApi.publishedObjectTypes).mockResolvedValue([])
  })

  it('loads a persisted configuration and exposes its business nodes', async () => {
    const wrapper = mount(ConfigurationCenterPage, {
      global: {
        plugins: [pinia],
        stubs: {
          ElButton: { template: '<button><slot /></button>' },
          ElAlert: true,
          ElEmpty: true,
          ElForm: { template: '<form><slot /></form>' },
          ElFormItem: { template: '<div><slot /></div>' },
          ElInput: true,
          ElSelect: true,
          ElOption: true,
          ElSwitch: true,
          ElTag: true,
          ElInputNumber: true,
          ElTable: true,
          ElTableColumn: true,
          ElDialog: { template: '<div><slot /></div>' },
        },
        directives: {
          loading: {},
        },
      },
    })

    await flushPromises()

    expect(configurationApi.list).toHaveBeenCalled()
    expect(workspaceApi.agents).toHaveBeenCalled()
    expect(wrapper.get('h1').text()).toContain('流程配置')
    expect(wrapper.text()).toContain('研发测试申请与异常判断')
    expect(wrapper.text()).toContain('质量确认')
  })
})
