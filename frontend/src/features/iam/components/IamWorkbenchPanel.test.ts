import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import IamWorkbenchPanel from './IamWorkbenchPanel.vue'
import { workbenchApi } from '@/api/modules/workbenchApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import { useAuthStore } from '@/stores/auth'

vi.mock('@/api/modules/workbenchApi', () => ({
  workbenchApi: {
    listTemplates: vi.fn(),
    createTemplate: vi.fn(),
    copyTemplate: vi.fn(),
    updateDraft: vi.fn(),
    validate: vi.fn(),
    publish: vi.fn(),
    setStatus: vi.fn(),
    versions: vi.fn(),
  },
}))

vi.mock('@/api/modules/workspaceApi', () => ({
  workspaceApi: {
    publishedObjectTypes: vi.fn(),
  },
}))

describe('IamWorkbenchPanel', () => {
  let pinia: ReturnType<typeof createPinia>

  beforeEach(() => {
    vi.clearAllMocks()
    pinia = createPinia()
    setActivePinia(pinia)
    const auth = useAuthStore()
    auth.permissions = [
      { resource: 'workbench-template', action: 'Read' },
      { resource: 'workbench-template', action: 'Manage' },
      { resource: 'workbench-template', action: 'Approve' },
    ]
    vi.mocked(workbenchApi.listTemplates).mockResolvedValue([
      {
        id: 'template-1',
        code: 'WS-RND',
        name: '研发工作台',
        description: '研发岗位首屏',
        audienceTitle: '研发岗位',
        primaryQuestion: '哪些实验和资料需要处理？',
        departmentId: 'department-1',
        departmentName: '研发',
        isSystem: true,
        isDefault: true,
        status: 'Published',
        sortOrder: 10,
        version: 2,
        createdAt: '2026-11-02T00:00:00Z',
        updatedAt: '2026-11-02T00:00:00Z',
        publishedAt: '2026-11-02T00:00:00Z',
        draftVersion: null,
        publishedVersion: {
          id: 'version-1',
          versionNumber: 1,
          status: 'Published',
          revision: 1,
          validationJson: null,
          impactAnalysisJson: '{"cardCount":1}',
          createdAt: '2026-11-02T00:00:00Z',
          publishedAt: '2026-11-02T00:00:00Z',
          cards: [
            {
              id: 'card-1',
              cardType: 'work_items',
              title: '我的研发事项',
              description: '真实事项',
              sortOrder: 10,
              width: 'Half',
              dataSourceType: 'ExistingApi',
              filter: { top: '5' },
              requiredResource: 'dashboard',
              requiredAction: 'Read',
              isEnabled: true,
              capabilityState: 'Connected',
            },
          ],
          assignments: [
            {
              id: 'assignment-1',
              assignmentType: 'Department',
              scopeValue: 'department-1',
              priority: 0,
            },
          ],
        },
      },
    ])
    vi.mocked(workspaceApi.publishedObjectTypes).mockResolvedValue([])
  })

  it('renders system template cards and management boundaries', async () => {
    const wrapper = mount(IamWorkbenchPanel, {
      props: { tree: [], roles: [] },
      global: {
        plugins: [pinia],
        stubs: {
          ElButton: { template: '<button><slot /></button>' },
          ElAlert: { props: ['title'], template: '<div>{{ title }}</div>' },
          ElEmpty: true,
          ElTag: { template: '<span><slot /></span>' },
          ElForm: { template: '<form><slot /></form>' },
          ElFormItem: { template: '<label><slot /></label>' },
          ElInput: true,
          ElSelect: true,
          ElOption: true,
          ElInputNumber: true,
          ElCheckbox: { template: '<label><slot /></label>' },
          ElSwitch: true,
          ElDialog: { template: '<div><slot /></div>' },
          ElTimeline: { template: '<div><slot /></div>' },
          ElTimelineItem: { template: '<div><slot /></div>' },
        },
      },
    })

    await flushPromises()

    expect(workbenchApi.listTemplates).toHaveBeenCalledTimes(1)
    expect(wrapper.text()).toContain('研发工作台')
    expect(wrapper.text()).toContain('我的研发事项')
    expect(wrapper.text()).toContain('系统模板不能删除或停用')
    expect(wrapper.text()).toContain('发布')
    expect(wrapper.text()).toContain('版本历史')
  })
})
