export function formatShanghaiDate(value: string | null) {
  if (!value) return '未设置'
  return new Intl.DateTimeFormat('zh-CN', {
    timeZone: 'Asia/Shanghai',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false,
  }).format(new Date(value))
}

export function toShanghaiInput(value: string | null) {
  if (!value) return ''
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Asia/Shanghai',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hourCycle: 'h23',
  }).formatToParts(new Date(value))
  const part = (type: Intl.DateTimeFormatPartTypes) =>
    parts.find((item) => item.type === type)?.value ?? '00'
  return `${part('year')}-${part('month')}-${part('day')}T${part('hour')}:${part('minute')}:${part('second')}`
}

export function fromShanghaiInput(value: string) {
  return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}$/.test(value)
    ? `${value}+08:00`
    : null
}

export function isConfirmationDraftComplete(input: {
  projectId: string
  assigneeId: string
  dueLocal: string
  content: string
  flowMode: 'none' | 'existing' | 'new'
  workflowInstanceId: string
  workflowDefinitionId: string
}) {
  if (!input.projectId || !input.assigneeId || !input.dueLocal || !input.content.trim()) {
    return false
  }
  if (input.flowMode === 'existing' && !input.workflowInstanceId) return false
  if (input.flowMode === 'new' && !input.workflowDefinitionId) return false
  return true
}
