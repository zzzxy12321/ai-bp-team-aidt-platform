import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import WorkbenchFilterBar from './WorkbenchFilterBar.vue'
import WorkbenchIntroDrawer from './WorkbenchIntroDrawer.vue'
import WorkbenchPageHeader from './WorkbenchPageHeader.vue'
import WorkbenchStats from './WorkbenchStats.vue'
import MainColumnPageHeader from './MainColumnPageHeader.vue'

const intro = {
  title: '我的工作',
  purpose: '集中处理当前身份有权办理的事项。',
  audiences: ['研发、质量和项目岗位'],
  whenToUse: '每天开始工作和收到新待办时使用。',
  steps: ['查看待办。', '筛选范围。', '进入真实业务页面。'],
  examples: ['客户样品测试申请等待研发确认。', '质量异常行动项等待负责人补充截止时间。'],
  dataSources: ['流程、项目、听记和确认来源。'],
  permissions: ['只显示已授权部门的数据。'],
  boundaries: ['付款和质量放行必须由授权真人确认。', '列表只读取已授权部门的数据。'],
  nextStep: '先处理逾期事项。',
}

afterEach(() => {
  vi.unstubAllGlobals()
  document.body.innerHTML = ''
})

describe('workspace framework components', () => {
  it('renders a concise page header with scope, actions and intro trigger', async () => {
    const wrapper = mount(WorkbenchPageHeader, {
      props: {
        title: '我的工作',
        scopeLabel: '当前部门：研发',
        kicker: 'MY WORK',
      },
      slots: {
        actions: '<button data-testid="primary-action">发起事项</button>',
      },
    })

    expect(wrapper.get('h1').text()).toBe('我的工作')
    expect(wrapper.text()).toContain('当前部门：研发')
    expect(wrapper.get('[data-testid="primary-action"]').text()).toBe('发起事项')
    await wrapper.get('[data-testid="workbench-intro-trigger"]').trigger('click')
    expect(wrapper.emitted('intro')).toHaveLength(1)
  })

  it('opens a right-side introduction drawer with real examples and closes with Escape', async () => {
    const wrapper = mount(WorkbenchIntroDrawer, {
      props: { open: true, ...intro },
      attachTo: document.body,
    })
    await flushPromises()

    const drawer = document.querySelector('[data-testid="workbench-intro-drawer"]')
    expect(drawer).not.toBeNull()
    expect(drawer?.textContent).toContain(intro.examples[0])
    expect(drawer?.textContent).toContain(intro.boundaries[0])
    expect(drawer?.textContent).toContain('业务操作路径')
    expect(drawer?.textContent).toContain('适用岗位')
    expect(drawer?.textContent).toContain('数据来源与权限范围')
    expect(drawer?.textContent).toContain('建议的下一步')
    expect(document.activeElement).toBe(
      document.querySelector('[data-testid="workbench-intro-close"]'),
    )

    document
      .querySelector('[data-testid="workbench-intro-close"]')
      ?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))
    await flushPromises()
    expect(wrapper.emitted('close')).toHaveLength(1)
  })

  it('combines the shared header, catalog drawer, close paths and trigger focus restoration', async () => {
    const wrapper = mount(MainColumnPageHeader, {
      props: {
        introKey: 'my-work',
        scopeLabel: '当前部门：研发',
      },
      attachTo: document.body,
      global: {
        stubs: {
          RouterLink: {
            props: ['to'],
            template: '<a :href="to"><slot /></a>',
          },
        },
      },
    })
    const trigger = wrapper.get<HTMLButtonElement>('[data-testid="workbench-intro-trigger"]')
    trigger.element.style.display = 'inline-flex'
    expect(wrapper.text()).toContain('演示环境')

    await trigger.trigger('click')
    await flushPromises()
    expect(document.querySelector('[data-testid="workbench-intro-drawer"]')?.textContent)
      .toContain('质量工程师早上打开页面')

    document
      .querySelector<HTMLButtonElement>('[data-testid="workbench-intro-close"]')
      ?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }))
    await flushPromises()
    expect(document.querySelector('[data-testid="workbench-intro-drawer"]')).toBeNull()
    expect(document.activeElement).toBe(trigger.element)

    await trigger.trigger('click')
    await flushPromises()
    const layer = document.querySelector<HTMLElement>('[data-testid="workbench-intro-drawer"]')
    layer?.dispatchEvent(new MouseEvent('click', { bubbles: true }))
    await flushPromises()
    expect(document.querySelector('[data-testid="workbench-intro-drawer"]')).toBeNull()
    expect(document.activeElement).toBe(trigger.element)

    await trigger.trigger('click')
    await flushPromises()
    document.querySelector<HTMLButtonElement>('[data-testid="workbench-intro-close"]')?.click()
    await flushPromises()
    expect(document.querySelector('[data-testid="workbench-intro-drawer"]')).toBeNull()
    expect(document.activeElement).toBe(trigger.element)
  })

  it('shows only supplied statistics and caps optional pages at five metrics', () => {
    const wrapper = mount(WorkbenchStats, {
      props: {
        items: [
          { label: '待我处理', value: 4, helper: '全部授权范围', tone: 'primary' },
          { label: '今天到期', value: 1, helper: '上海时间' },
          { label: '待我确认', value: 2, helper: '真人确认' },
          { label: '人工边界', value: 1, helper: '不可代理', tone: 'warning' },
          { label: '不会渲染', value: 9, helper: '超过上限' },
          { label: '也不会渲染', value: 10, helper: '超过上限' },
        ],
      },
    })

    expect(wrapper.findAll('.workbench-stat')).toHaveLength(5)
    expect(wrapper.text()).toContain('待我处理')
    expect(wrapper.text()).not.toContain('也不会渲染')
  })

  it('keeps search visible and moves filters into a mobile panel', async () => {
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockReturnValue({
        matches: true,
        media: '(max-width: 760px)',
        onchange: null,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        addListener: vi.fn(),
        removeListener: vi.fn(),
        dispatchEvent: vi.fn(),
      }),
    )
    const wrapper = mount(WorkbenchFilterBar, {
      props: { resultLabel: '共 6 条结果' },
      slots: {
        search: '<input data-testid="filter-search" aria-label="搜索" />',
        default: '<button data-testid="filter-status">状态</button>',
        actions: '<button data-testid="filter-reset">重置</button>',
      },
      attachTo: document.body,
    })
    await flushPromises()

    expect(wrapper.get('[data-testid="filter-search"]').isVisible()).toBe(true)
    expect(wrapper.text()).toContain('共 6 条结果')
    const trigger = wrapper.get<HTMLButtonElement>('[data-testid="workbench-filter-trigger"]')
    trigger.element.style.display = 'inline-flex'
    await trigger.trigger('click')
    await flushPromises()
    expect(wrapper.get('[data-testid="workbench-filters"]').classes()).toContain('mobile-open')

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }))
    await flushPromises()
    expect(wrapper.get('[data-testid="workbench-filters"]').classes()).not.toContain('mobile-open')
    expect(document.activeElement).toBe(trigger.element)
  })
})
