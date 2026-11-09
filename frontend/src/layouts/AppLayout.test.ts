import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { createMemoryHistory, createRouter } from 'vue-router'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import AppLayout from './AppLayout.vue'
import { useAuthStore } from '@/stores/auth'
import { useNavigationStore } from '@/stores/navigation'

const scrollIntoView = vi.fn()

function navigationItem(
  itemKey: string,
  name: string,
  route: string,
  sortOrder: number,
) {
  return {
    id: itemKey,
    itemKey,
    name,
    itemType: 'Page',
    route,
    requiredResource: null,
    requiredAction: null,
    sortOrder,
    isVisible: true,
    isEnabled: true,
    isSystem: false,
    icon: name.slice(0, 1),
    isFavorite: false,
    isHidden: false,
  }
}

function navigationGroup(
  workspaceKey: string,
  workspaceName: string,
  layer: 'Company' | 'Department' | 'Platform',
  departmentId: string | null,
  items: ReturnType<typeof navigationItem>[],
) {
  return {
    workspaceId: workspaceKey,
    workspaceKey,
    workspaceName,
    workspaceType: layer,
    layer,
    departmentId,
    departmentCode: departmentId ? `DEP-${departmentId}` : null,
    departmentName: departmentId ? workspaceName : null,
    isDefault: false,
    isDemoData: true,
    isTestData: true,
    sortOrder: 1,
    items,
  }
}

let pinia: ReturnType<typeof createPinia>

async function mountLayout(path = '/documents') {
  const router = createRouter({
    history: createMemoryHistory(),
    routes: [
      {
        path: '/',
        component: { template: '<div>我的工作内容</div>' },
        meta: { title: '我的工作' },
      },
      {
        path: '/documents',
        component: { template: '<div>部门文件内容</div>' },
        meta: { title: '部门文件' },
      },
      {
        path: '/department',
        component: { template: '<div>部门工作区内容</div>' },
        meta: { title: '部门工作区' },
      },
    ],
  })
  await router.push(path)
  await router.isReady()
  const wrapper = mount(AppLayout, {
    attachTo: document.body,
    global: {
      plugins: [pinia, router],
      stubs: {
        ElButton: {
          props: ['text'],
          emits: ['click'],
          template: '<button v-bind="$attrs" @click="$emit(\'click\')"><slot /></button>',
        },
        ElTag: { template: '<span><slot /></span>' },
        ElDialog: true,
        ElTable: true,
        ElTableColumn: true,
        ElInputNumber: true,
        ElSwitch: true,
      },
      directives: { loading: {} },
    },
  })
  await flushPromises()
  return wrapper
}

describe('AppLayout', () => {
  beforeEach(() => {
    pinia = createPinia()
    setActivePinia(pinia)
    scrollIntoView.mockReset()
    Object.defineProperty(HTMLElement.prototype, 'scrollIntoView', {
      configurable: true,
      value: scrollIntoView,
    })
    Object.defineProperty(window, 'innerWidth', {
      configurable: true,
      writable: true,
      value: 1440,
    })
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockImplementation((query: string) => ({
        matches: query === '(max-width: 820px)' && window.innerWidth <= 820,
        media: query,
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
      })),
    )
    const auth = useAuthStore()
    auth.session.displayName = '测试用户'
    auth.session.isAuthenticated = false
    const navigation = useNavigationStore()
    navigation.currentDepartmentId = 'dept-rnd'
    navigation.groups = [
      navigationGroup('company-main', '公司公共入口', 'Company', null, [
        navigationItem('my-work', '我的工作', '/', 1),
      ]),
      navigationGroup('dept-rnd-main', '研发', 'Department', 'dept-rnd', [
        navigationItem('department-rnd', '部门工作区', '/department', 1),
        navigationItem('documents-rnd', '部门文件', '/documents', 2),
      ]),
      navigationGroup('dept-supply-main', '供应链', 'Department', 'dept-supply', [
        navigationItem('department-supply', '供应链工作区', '/department', 1),
      ]),
    ]
  })

  afterEach(() => {
    vi.unstubAllGlobals()
    document.body.innerHTML = ''
  })

  it('opens only the current department and marks the active menu item', async () => {
    const wrapper = await mountLayout()
    const groups = wrapper.findAll('.department-group')
    const openGroups = wrapper.findAll('.department-group[open]')

    expect(openGroups).toHaveLength(1)
    expect(groups[0].attributes('open')).toBeDefined()
    expect(
      wrapper.get('[data-nav-key="dept-rnd-main:documents-rnd"]').attributes('aria-current'),
    ).toBe('page')
    await flushPromises()
    expect(scrollIntoView).toHaveBeenCalledWith({ block: 'nearest' })
  })

  it('switches the single-open department without allowing multiple expanded groups', async () => {
    const wrapper = await mountLayout()
    const second = wrapper.findAll('.department-group')[1]
    const details = second.element as HTMLDetailsElement
    details.open = true
    await second.trigger('toggle')
    await flushPromises()

    const openGroups = wrapper.findAll('.department-group[open]')
    expect(openGroups).toHaveLength(1)
    expect(openGroups[0].text()).toContain('供应链')
  })

  it('uses an Esc-closable narrow drawer and returns focus to its trigger', async () => {
    window.innerWidth = 600
    const wrapper = await mountLayout('/')
    const sidebar = wrapper.get('[data-testid="app-sidebar"]')
    const trigger = wrapper.get<HTMLButtonElement>('[data-testid="sidebar-drawer-trigger"]')
    trigger.element.style.display = 'grid'

    expect(sidebar.attributes('aria-hidden')).toBe('true')
    await trigger.trigger('click')
    await flushPromises()
    expect(sidebar.attributes('aria-hidden')).toBeUndefined()
    expect(sidebar.classes()).toContain('drawer-open')

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    await flushPromises()
    expect(sidebar.classes()).not.toContain('drawer-open')
    expect(sidebar.attributes('aria-hidden')).toBe('true')
    expect(document.activeElement).toBe(trigger.element)
  })

  it('keeps an opened narrow drawer open when navigation finishes loading', async () => {
    window.innerWidth = 600
    const wrapper = await mountLayout('/')
    const sidebar = wrapper.get('[data-testid="app-sidebar"]')
    const trigger = wrapper.get<HTMLButtonElement>('[data-testid="sidebar-drawer-trigger"]')
    trigger.element.style.display = 'grid'

    await trigger.trigger('click')
    await flushPromises()
    expect(sidebar.classes()).toContain('drawer-open')

    const navigation = useNavigationStore()
    navigation.currentDepartmentId = 'dept-supply'
    navigation.groups = [
      navigationGroup('company-main', '公司公共入口', 'Company', null, [
        navigationItem('my-work', '我的工作', '/', 1),
      ]),
      navigationGroup('dept-supply-main', '供应链', 'Department', 'dept-supply', [
        navigationItem('department-supply', '供应链工作区', '/department', 1),
        navigationItem('documents-supply', '部门文件', '/documents', 2),
      ]),
    ]
    await flushPromises()

    expect(sidebar.classes()).toContain('drawer-open')
    expect(sidebar.attributes('aria-hidden')).toBeUndefined()
  })
})
