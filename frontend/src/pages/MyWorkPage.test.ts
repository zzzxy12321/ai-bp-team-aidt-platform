import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import MyWorkPage from './MyWorkPage.vue'
import { workspaceApi } from '@/api/modules/workspaceApi'
import { useAuthStore } from '@/stores/auth'

vi.mock('@/api/modules/workspaceApi', () => ({
  workspaceApi: {
    myWork: vi.fn(),
    departments: vi.fn(),
    receiveInboxEvent: vi.fn(),
  },
}))

vi.mock('@/api/modules/fileApi', () => ({
  fileApi: {
    upload: vi.fn(),
  },
}))

const item = {
  id: 'work-1',
  sourceType: 'WorkflowTask' as const,
  sourceLabel: '流程待办',
  businessTitle: '客户样品耐候测试确认',
  contextTitle: '3157C 客户样品验证',
  projectId: 'project-1',
  departmentId: 'dept-rnd',
  departmentName: '研发',
  currentStage: '质量评审',
  assigneeId: 'user-1',
  assigneeDisplayName: '林澄',
  dueAt: '2026-11-04T10:00:00Z',
  riskLevel: 'High' as const,
  riskLabel: '永久人工边界',
  priorityGroup: 'PermanentHumanBoundary' as const,
  status: 'Pending',
  statusLabel: '待处理',
  nextAction: '进入流程处理',
  sourceCount: 1,
  detailType: 'WorkflowTask',
  detailTargetId: 'target-1',
  detailPath: '/workflows?task=target-1',
  environmentCode: 'demo' as const,
  isTestData: true,
  isPermanentHumanBoundary: true,
  requiresConfirmation: true,
  occurredAt: '2026-11-04T08:00:00Z',
}

let pinia: ReturnType<typeof createPinia>

beforeEach(() => {
  vi.clearAllMocks()
  pinia = createPinia()
  setActivePinia(pinia)
  const auth = useAuthStore()
  auth.session.displayName = '林澄'
  auth.session.isAuthenticated = true
  auth.session.roles = ['platform-admin']
  auth.permissions = [
    { resource: 'dashboard', action: 'Read' },
    { resource: 'event-inbox', action: 'Create' },
    { resource: 'file', action: 'Read' },
    { resource: 'file', action: 'Create' },
    { resource: 'minutes', action: 'Create' },
    { resource: 'scenario', action: 'Read' },
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
    {
      id: 'dept-qa',
      code: 'QA',
      name: '质量',
      managerUserId: null,
      isActive: true,
      isPrimary: false,
    },
  ])
  vi.mocked(workspaceApi.myWork).mockResolvedValue({
    displayName: '林澄',
    todayFocus: item,
    pendingItems: [item],
    initiatedByMe: [item],
    participating: [item],
    needsAttention: [item],
    recentSourceEvidence: [],
    summary: {
      pendingCount: 1,
      dueTodayCount: 1,
      awaitingConfirmationCount: 1,
      permanentHumanBoundaryCount: 1,
    },
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
      { path: '/', component: MyWorkPage },
      { path: '/workflows', component: { template: '<div>流程详情</div>' } },
      { path: '/confirmations', component: { template: '<div>确认中心</div>' } },
      { path: '/documents', component: { template: '<div>文件中心</div>' } },
      { path: '/minutes', component: { template: '<div>听记</div>' } },
      { path: '/scenarios', component: { template: '<div>场景</div>' } },
    ],
  })
  await router.push('/')
  await router.isReady()
  const wrapper = mount(MyWorkPage, {
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
        ElDropdown: { template: '<div><slot /><slot name="dropdown" /></div>' },
        ElDropdownMenu: { template: '<div><slot /></div>' },
        ElDropdownItem: { template: '<div><slot /></div>' },
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
        ElEmpty: { template: '<div><slot /><slot name="default" /></div>' },
        ElDialog: { template: '<div><slot /></div>' },
        ElForm: { template: '<form><slot /></form>' },
        ElFormItem: { template: '<label><slot /></label>' },
        ElTag: { template: '<span><slot /></span>' },
      },
      directives: { loading: {} },
    },
  })
  await flushPromises()
  return { wrapper, router }
}

describe('MyWorkPage', () => {
  it('uses real summary metrics, concise header and the full work table', async () => {
    const { wrapper } = await mountPage()

    expect(wrapper.get('[data-testid="my-work-stat-pending"]').text()).toContain('1')
    expect(wrapper.get('[data-testid="my-work-stat-due-today"]').text()).toContain('1')
    expect(wrapper.get('[data-testid="my-work-stat-confirmation"]').text()).toContain('1')
    expect(wrapper.get('[data-testid="my-work-stat-boundary"]').text()).toContain('1')
    expect(wrapper.text()).toContain('当前部门：研发 · 全部授权范围')
    expect(wrapper.find('.page-lead').exists()).toBe(false)
    expect(wrapper.findAll('[data-testid="my-work-category"]')).toHaveLength(5)
    expect(wrapper.get('[data-testid="work-item-table"]').text()).toContain('客户样品耐候测试确认')
    expect(wrapper.text()).toContain('付款、质量放行、配方或设备控制须授权真人确认')
  })

  it('combines search with category filtering and opens the matching item', async () => {
    const { wrapper, router } = await mountPage()
    const search = wrapper.get<HTMLInputElement>('[data-testid="my-work-search"]')
    await search.setValue('不存在的事项')
    await flushPromises()
    expect(wrapper.find('[data-testid="unified-work-item"]').exists()).toBe(false)
    expect(wrapper.get('[data-testid="workbench-result-count"]').text()).toContain('0 / 1')

    await wrapper.get('[data-testid="my-work-reset"]').trigger('click')
    await wrapper.get('[data-category="initiatedByMe"]').trigger('click')
    await wrapper.get('[data-testid="unified-work-item"]').trigger('click')
    await flushPromises()
    expect(router.currentRoute.value.path).toBe('/workflows')
  })

  it('opens the introduction drawer and returns focus to its trigger', async () => {
    const { wrapper } = await mountPage()
    const trigger = wrapper.get<HTMLButtonElement>('[data-testid="workbench-intro-trigger"]')
    trigger.element.style.display = 'inline-flex'
    await trigger.trigger('click')
    await flushPromises()

    const drawer = document.querySelector('[data-testid="workbench-intro-drawer"]')
    expect(drawer?.textContent).toContain('批次异常调查')
    expect(drawer?.textContent).toContain('永久人工边界')

    document
      .querySelector<HTMLButtonElement>('[data-testid="workbench-intro-close"]')
      ?.click()
    await flushPromises()
    expect(document.querySelector('[data-testid="workbench-intro-drawer"]')).toBeNull()
    expect(document.activeElement).toBe(trigger.element)
  })
})
