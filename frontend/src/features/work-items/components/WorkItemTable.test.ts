import { flushPromises, mount } from '@vue/test-utils'
import { afterEach, describe, expect, it, vi } from 'vitest'
import WorkItemTable from './WorkItemTable.vue'

const item = {
  id: 'item-1',
  sourceType: 'WorkflowTask' as const,
  sourceLabel: '流程待办',
  businessTitle: '客户样品质量放行确认',
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
  detailPath: '/workflows',
  environmentCode: 'demo' as const,
  isTestData: true,
  isPermanentHumanBoundary: true,
  requiresConfirmation: true,
  occurredAt: '2026-11-04T08:00:00Z',
}

afterEach(() => {
  vi.unstubAllGlobals()
})

describe('WorkItemTable', () => {
  it('renders the compact desktop columns and keeps human boundary beside the action', () => {
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockReturnValue({
        matches: false,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
      }),
    )
    const wrapper = mount(WorkItemTable, {
      props: { items: [item] },
    })

    expect(wrapper.findAll('th').map((cell) => cell.text())).toEqual([
      '事项',
      '来源',
      '当前节点',
      '负责人',
      '截止时间',
      '状态',
      '操作',
    ])
    expect(wrapper.text()).toContain('付款、质量放行、配方或设备控制须授权真人确认')
    expect(wrapper.get('[data-testid="unified-work-item"]').attributes('data-source-id')).toBe('item-1')
  })

  it('switches to business cards on the phone breakpoint', async () => {
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockReturnValue({
        matches: true,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
      }),
    )
    const wrapper = mount(WorkItemTable, {
      props: { items: [item] },
      global: {
        stubs: {
          ElButton: { template: '<button><slot /></button>' },
        },
      },
    })
    await flushPromises()

    expect(wrapper.get('[data-testid="work-item-mobile-list"]')).toBeTruthy()
    expect(wrapper.text()).toContain('客户样品质量放行确认')
    expect(wrapper.get('[data-testid="unified-work-item"]').attributes('data-source-id')).toBe('item-1')
  })
})
