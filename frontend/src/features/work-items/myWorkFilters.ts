import type { MyWorkDto, WorkItemViewDto } from '@/api/types/contracts'

export type WorkCategory =
  | 'all'
  | 'pendingItems'
  | 'initiatedByMe'
  | 'participating'
  | 'needsAttention'

export type MyWorkFilterState = {
  keyword: string
  departmentId: string
  sourceType: string
  status: string
  priorityGroup: string
  due: '' | 'today' | 'week' | 'overdue' | 'none'
}

export const EMPTY_MY_WORK_FILTERS: MyWorkFilterState = {
  keyword: '',
  departmentId: '',
  sourceType: '',
  status: '',
  priorityGroup: '',
  due: '',
}

export const WORK_CATEGORY_LABELS: Record<WorkCategory, string> = {
  all: '全部',
  pendingItems: '待我处理',
  initiatedByMe: '我发起',
  participating: '我参与',
  needsAttention: '需要关注',
}

const categoryKeys: Exclude<WorkCategory, 'all'>[] = [
  'pendingItems',
  'initiatedByMe',
  'participating',
  'needsAttention',
]

function itemKey(item: WorkItemViewDto) {
  return `${item.sourceType}:${item.id}`
}

export function allWorkItems(work: MyWorkDto | null): WorkItemViewDto[] {
  if (!work) return []
  const seen = new Set<string>()
  return categoryKeys
    .flatMap((key) => work[key])
    .filter((item) => {
      const key = itemKey(item)
      if (seen.has(key)) return false
      seen.add(key)
      return true
    })
}

export function workCategoryItems(
  work: MyWorkDto | null,
  category: WorkCategory,
): WorkItemViewDto[] {
  if (!work) return []
  return category === 'all' ? allWorkItems(work) : work[category]
}

function shanghaiDateKey(value: Date): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Shanghai',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(value)
}

function dueMatches(dueAt: string | null, due: MyWorkFilterState['due'], now: Date): boolean {
  if (!due) return true
  if (due === 'none') return !dueAt
  if (!dueAt) return false
  const dueDate = new Date(dueAt)
  if (Number.isNaN(dueDate.getTime())) return false
  if (due === 'overdue') return dueDate.getTime() < now.getTime()
  if (due === 'today') return shanghaiDateKey(dueDate) === shanghaiDateKey(now)
  const distance = dueDate.getTime() - now.getTime()
  return distance >= 0 && distance <= 7 * 24 * 60 * 60 * 1000
}

export function matchesMyWorkFilters(
  item: WorkItemViewDto,
  filters: MyWorkFilterState,
  now = new Date(),
): boolean {
  const keyword = filters.keyword.trim().toLocaleLowerCase('zh-CN')
  if (
    keyword &&
    ![
      item.businessTitle,
      item.contextTitle,
      item.sourceLabel,
      item.departmentName,
      item.detailTargetId,
    ].some((value) => value.toLocaleLowerCase('zh-CN').includes(keyword))
  ) {
    return false
  }
  if (filters.departmentId && item.departmentId !== filters.departmentId) return false
  if (filters.sourceType && item.sourceType !== filters.sourceType) return false
  if (filters.status && item.status !== filters.status) return false
  if (filters.priorityGroup && item.priorityGroup !== filters.priorityGroup) return false
  return dueMatches(item.dueAt, filters.due, now)
}

export function filterMyWorkItems(
  items: WorkItemViewDto[],
  filters: MyWorkFilterState,
  now = new Date(),
): WorkItemViewDto[] {
  return items.filter((item) => matchesMyWorkFilters(item, filters, now))
}

export function myWorkCategoryCounts(
  work: MyWorkDto | null,
  filters: MyWorkFilterState,
  now = new Date(),
): Record<WorkCategory, number> {
  return Object.fromEntries(
    (['all', ...categoryKeys] as WorkCategory[]).map((category) => [
      category,
      filterMyWorkItems(workCategoryItems(work, category), filters, now).length,
    ]),
  ) as Record<WorkCategory, number>
}

export function hasActiveMyWorkFilters(filters: MyWorkFilterState): boolean {
  return Object.values(filters).some((value) => value !== '')
}
