import { fireEvent, render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import WorkItemCard from './WorkItemCard.vue'

const item = {
  id: 'internal-id',
  kind: 'Task' as const,
  title: '客户样品 · 质量评审',
  objectCode: 'SAMPLE-001',
  workflowName: '客户样品到转产',
  currentStep: '质量评审',
  assigneeDisplayName: '质量工程师',
  dueAt: null,
  status: 'Pending',
  nextAction: '处理待办',
  environmentCode: 'demo' as const,
  isTestData: true,
  departmentId: 'department-id',
  workflowInstanceId: 'instance-id',
  taskId: 'task-id',
}

describe('WorkItemCard', () => {
  it('shows business language without exposing internal identifiers', () => {
    render(WorkItemCard, {
      props: { item },
      global: {
        stubs: {
          'el-tag': { template: '<span><slot /></span>' },
          'el-button': { template: '<button @click="$emit(\'click\')"><slot /></button>' },
        },
      },
    })

    expect(screen.getByText('客户样品 · 质量评审')).toBeTruthy()
    expect(screen.getByText('质量评审')).toBeTruthy()
    expect(screen.queryByText('internal-id')).toBeNull()
    expect(screen.queryByText('instance-id')).toBeNull()
  })

  it('emits the work item when opened', async () => {
    const { emitted } = render(WorkItemCard, {
      props: { item },
      global: {
        stubs: {
          'el-tag': { template: '<span><slot /></span>' },
          'el-button': { template: '<button><slot /></button>' },
        },
      },
    })

    await fireEvent.click(screen.getByRole('button', { name: '处理待办' }))
    expect(emitted().open?.[0]).toEqual([item])
  })
})
