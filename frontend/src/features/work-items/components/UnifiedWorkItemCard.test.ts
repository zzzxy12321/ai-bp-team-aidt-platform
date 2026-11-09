import { fireEvent, render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import UnifiedWorkItemCard from './UnifiedWorkItemCard.vue'

const item = {
  id: 'internal-id',
  sourceType: 'WorkflowTask' as const,
  sourceLabel: '流程待办',
  businessTitle: '确认客户样品测试结果',
  contextTitle: '3157C 客户样品验证',
  projectId: 'project-id',
  departmentId: 'department-id',
  departmentName: '研发部',
  currentStage: '质量评审',
  assigneeId: 'user-id',
  assigneeDisplayName: '林澄',
  dueAt: '2026-11-01T10:00:00Z',
  riskLevel: 'Medium' as const,
  riskLabel: '今天到期',
  priorityGroup: 'DueToday' as const,
  status: 'Pending',
  statusLabel: '待处理',
  nextAction: '进入流程处理',
  sourceCount: 2,
  detailType: 'WorkflowTask',
  detailTargetId: 'task-id',
  detailPath: '/workflows?task=task-id',
  environmentCode: 'demo' as const,
  isTestData: true,
  isPermanentHumanBoundary: false,
  requiresConfirmation: false,
  occurredAt: '2026-11-01T08:00:00Z',
}

describe('UnifiedWorkItemCard', () => {
  it('shows business context and source count without internal identifiers', () => {
    render(UnifiedWorkItemCard, {
      props: { item },
      global: {
        stubs: {
          'el-button': { template: '<button><slot /></button>' },
        },
      },
    })

    expect(screen.getByText('确认客户样品测试结果')).toBeTruthy()
    expect(screen.getByText('3157C 客户样品验证')).toBeTruthy()
    expect(screen.getByText('研发部')).toBeTruthy()
    expect(screen.getByText('2 个来源')).toBeTruthy()
    expect(screen.queryByText('internal-id')).toBeNull()
    expect(screen.queryByText('task-id')).toBeNull()
    expect(screen.queryByText('user-id')).toBeNull()
  })

  it('emits the full display model when opened', async () => {
    const { emitted } = render(UnifiedWorkItemCard, {
      props: { item },
      global: {
        stubs: {
          'el-button': { template: '<button><slot /></button>' },
        },
      },
    })

    await fireEvent.click(screen.getByTestId('unified-work-item'))
    expect(emitted().open?.[0]).toEqual([item])
  })
})
