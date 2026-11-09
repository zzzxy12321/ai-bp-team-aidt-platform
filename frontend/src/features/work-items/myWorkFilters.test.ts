import { describe, expect, it } from 'vitest'
import type { MyWorkDto, WorkItemViewDto } from '@/api/types/contracts'
import {
  EMPTY_MY_WORK_FILTERS,
  allWorkItems,
  filterMyWorkItems,
  myWorkCategoryCounts,
} from './myWorkFilters'

const baseItem: WorkItemViewDto = {
  id: 'item-1',
  sourceType: 'WorkflowTask',
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
  riskLevel: 'High',
  riskLabel: '高风险',
  priorityGroup: 'DueToday',
  status: 'Pending',
  statusLabel: '待处理',
  nextAction: '进入流程处理',
  sourceCount: 1,
  detailType: 'WorkflowTask',
  detailTargetId: 'target-1',
  detailPath: '/workflows',
  environmentCode: 'demo',
  isTestData: true,
  isPermanentHumanBoundary: false,
  requiresConfirmation: false,
  occurredAt: '2026-11-04T08:00:00Z',
}

const secondary: WorkItemViewDto = {
  ...baseItem,
  id: 'item-2',
  sourceType: 'MinutesActionItem',
  sourceLabel: '会议行动项',
  businessTitle: '补充质量异常调查报告',
  contextTitle: '客户投诉回访',
  departmentId: 'dept-qa',
  departmentName: '质量',
  dueAt: '2026-11-10T10:00:00Z',
  riskLevel: 'Medium',
  riskLabel: '待确认',
  priorityGroup: 'AwaitingConfirmation',
  status: 'Draft',
  requiresConfirmation: true,
}

const work: MyWorkDto = {
  displayName: '林澄',
  todayFocus: baseItem,
  pendingItems: [baseItem, secondary],
  initiatedByMe: [baseItem],
  participating: [secondary],
  needsAttention: [secondary],
  recentSourceEvidence: [],
  summary: {
    pendingCount: 2,
    dueTodayCount: 1,
    awaitingConfirmationCount: 1,
    permanentHumanBoundaryCount: 0,
  },
}

describe('my work filters', () => {
  it('deduplicates all items while keeping each category source intact', () => {
    expect(allWorkItems(work).map((item) => item.id)).toEqual(['item-1', 'item-2'])
  })

  it('combines keyword, department, source, status and priority filters', () => {
    const result = filterMyWorkItems(
      allWorkItems(work),
      {
        ...EMPTY_MY_WORK_FILTERS,
        keyword: '客户',
        departmentId: 'dept-qa',
        sourceType: 'MinutesActionItem',
        status: 'Draft',
        priorityGroup: 'AwaitingConfirmation',
      },
      new Date('2026-11-04T09:00:00Z'),
    )
    expect(result.map((item) => item.id)).toEqual(['item-2'])
  })

  it('applies due filters using Shanghai business dates', () => {
    const now = new Date('2026-11-04T01:00:00Z')
    expect(
      filterMyWorkItems(allWorkItems(work), { ...EMPTY_MY_WORK_FILTERS, due: 'today' }, now),
    ).toHaveLength(1)
    expect(
      filterMyWorkItems(allWorkItems(work), { ...EMPTY_MY_WORK_FILTERS, due: 'week' }, now),
    ).toHaveLength(2)
  })

  it('reports counts for all five categories after filtering', () => {
    const counts = myWorkCategoryCounts(
      work,
      { ...EMPTY_MY_WORK_FILTERS, departmentId: 'dept-qa' },
      new Date('2026-11-04T01:00:00Z'),
    )
    expect(counts).toEqual({
      all: 1,
      pendingItems: 1,
      initiatedByMe: 0,
      participating: 1,
      needsAttention: 1,
    })
  })
})
