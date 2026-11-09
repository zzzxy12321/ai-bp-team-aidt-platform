import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import MinutesPage from './MinutesPage.vue'
import { workspaceApi } from '@/api/modules/workspaceApi'
import { useAuthStore } from '@/stores/auth'

vi.mock('@/api/modules/workspaceApi', () => ({
  workspaceApi: {
    departments: vi.fn(),
    agents: vi.fn(),
    minutesSummary: vi.fn(),
    minutes: vi.fn(),
    minutesActionItems: vi.fn(),
    agentRuns: vi.fn(),
  },
}))

const sources = [
  {
    id: 'source-1',
    departmentId: 'dept-rnd',
    attachmentId: null,
    title: '客户样品周会',
    meetingAt: '2026-11-04T01:00:00Z',
    sourceType: 'ManualText' as const,
    transcript: '[09:00:00] 研发工程师：完成样品耐候测试。',
    environmentCode: 'demo' as const,
    isTestData: true,
    status: 'Analyzed',
    analysisVersion: 'rule-minutes-v1',
    analysisRunId: null,
    createdAt: '2026-11-04T01:10:00Z',
  },
  {
    id: 'source-2',
    departmentId: 'dept-qa',
    attachmentId: null,
    title: '质量异常复盘',
    meetingAt: '2026-11-03T01:00:00Z',
    sourceType: 'ManualUpload' as const,
    transcript: '[09:00:00] 质量工程师：补充偏差调查证据。',
    environmentCode: 'demo' as const,
    isTestData: true,
    status: 'AwaitingConfirmation',
    analysisVersion: 'none',
    analysisRunId: null,
    createdAt: '2026-11-03T01:10:00Z',
  },
]

let pinia: ReturnType<typeof createPinia>

beforeEach(() => {
  vi.clearAllMocks()
  pinia = createPinia()
  setActivePinia(pinia)
  const auth = useAuthStore()
  auth.session.displayName = '平台管理员'
  auth.session.isAuthenticated = true
  auth.session.roles = ['platform-admin']
  auth.permissions = []

  vi.mocked(workspaceApi.departments).mockResolvedValue([
    {
      id: 'dept-rnd',
      code: 'RND',
      name: '研发',
      managerUserId: null,
      isActive: true,
      isPrimary: true,
    },
    {
      id: 'dept-qa',
      code: 'QA',
      name: '质量',
      managerUserId: null,
      isActive: true,
      isPrimary: false,
    },
  ])
  vi.mocked(workspaceApi.agents).mockResolvedValue([
    {
      id: 'agent-1',
      name: '业务协同数字员工',
      isActive: true,
    } as never,
  ])
  vi.mocked(workspaceApi.minutesSummary).mockResolvedValue({
    totalCount: 32,
    analyzedCount: 18,
    pendingActionItemCount: 7,
    createdThisWeekCount: 4,
  })
  vi.mocked(workspaceApi.minutes).mockResolvedValue({
    items: sources,
    page: 1,
    pageSize: 20,
    total: 32,
  })
  vi.mocked(workspaceApi.minutesActionItems).mockResolvedValue([])
  vi.mocked(workspaceApi.agentRuns).mockResolvedValue({
    items: [],
    page: 1,
    pageSize: 100,
    total: 0,
  })
  vi.stubGlobal(
    'matchMedia',
    vi.fn().mockImplementation((query: string) => ({
      matches: false,
      media: query,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  )
})

afterEach(() => {
  vi.unstubAllGlobals()
  document.body.innerHTML = ''
})

async function mountPage() {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      { path: '/minutes', component: MinutesPage },
      { path: '/confirmations', component: { template: '<div>确认中心</div>' } },
    ],
  })
  await router.push('/minutes')
  await router.isReady()
  const wrapper = mount(MinutesPage, {
    attachTo: document.body,
    global: {
      plugins: [pinia, router],
      stubs: {
        ElAlert: true,
        ElButton: {
          props: ['disabled'],
          emits: ['click'],
          template: '<button :disabled="disabled" @click="$emit(\'click\')"><slot /></button>',
        },
        ElInput: {
          inheritAttrs: false,
          props: ['modelValue'],
          emits: ['update:modelValue'],
          template:
            '<input v-bind="$attrs" :value="modelValue" @input="$emit(\'update:modelValue\', $event.target.value)" />',
        },
        ElSelect: { template: '<div><slot /></div>' },
        ElOption: true,
        ElSwitch: { template: '<button type="button">switch</button>' },
        ElDatePicker: true,
        ElTag: { template: '<span><slot /></span>' },
        ElTable: { template: '<div><slot /></div>' },
        ElTableColumn: true,
        ElEmpty: { template: '<div><slot /></div>' },
        ElDialog: { template: '<div><slot /><slot name="footer" /></div>' },
      },
      directives: { loading: {} },
    },
  })
  await flushPromises()
  return { wrapper, router }
}

describe('MinutesPage', () => {
  it('renders four server metrics and a paginated source pane instead of all records', async () => {
    const { wrapper } = await mountPage()

    expect(wrapper.get('[data-testid="minutes-stat-total"]').text()).toContain('32')
    expect(wrapper.get('[data-testid="minutes-stat-analyzed"]').text()).toContain('18')
    expect(wrapper.get('[data-testid="minutes-stat-pending-actions"]').text()).toContain('7')
    expect(wrapper.get('[data-testid="minutes-stat-week"]').text()).toContain('4')
    expect(wrapper.findAll('.source-row')).toHaveLength(2)
    expect(wrapper.get('[data-testid="minutes-source-list"]').text()).toContain('客户样品周会')
    expect(wrapper.text()).toContain('1 / 2')
  })

  it('keeps the selected source and right detail in sync', async () => {
    const { wrapper, router } = await mountPage()
    await wrapper.get('[data-testid="minutes-source-source-2"]').trigger('click')
    await flushPromises()

    expect(wrapper.get('[data-testid="minutes-detail"]').text()).toContain('质量异常复盘')
    expect((router.currentRoute.value.query.source as string)).toBe('source-2')
  })

  it('sends the keyword to the server-side filter contract', async () => {
    const { wrapper } = await mountPage()
    await wrapper.get<HTMLInputElement>('[data-testid="minutes-search"]').setValue('质量工程师')
    await new Promise((resolve) => setTimeout(resolve, 380))
    await flushPromises()

    expect(workspaceApi.minutes).toHaveBeenLastCalledWith(
      expect.objectContaining({
        page: 1,
        pageSize: 20,
        search: '质量工程师',
      }),
    )
  })
})
