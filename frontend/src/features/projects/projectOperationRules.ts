import type {
  ProjectDto,
  ProjectRelationDto,
} from '@/api/types/contracts'

export type ProjectRelationEntityType =
  | 'Attachment'
  | 'MinutesSource'
  | 'WorkflowInstance'
  | 'BusinessObject'

export type ProjectRelationEntity = {
  id: string
  label: string
  detail: string
}

const statusTransitions: Partial<Record<ProjectDto['status'], { label: string; value: ProjectDto['status'] }[]>> = {
  Draft: [
    { label: '进行中', value: 'Active' },
    { label: '已终止', value: 'Terminated' },
  ],
  Active: [
    { label: '阻塞', value: 'Blocked' },
    { label: '待验收', value: 'AwaitingAcceptance' },
    { label: '已完成', value: 'Completed' },
    { label: '已终止', value: 'Terminated' },
  ],
  Blocked: [
    { label: '进行中', value: 'Active' },
    { label: '已终止', value: 'Terminated' },
  ],
  AwaitingAcceptance: [
    { label: '进行中', value: 'Active' },
    { label: '已完成', value: 'Completed' },
    { label: '已终止', value: 'Terminated' },
  ],
}

const relationCodes: Record<ProjectRelationEntityType, ProjectRelationDto['relationCode']> = {
  Attachment: 'SupportingEvidence',
  MinutesSource: 'MeetingEvidence',
  WorkflowInstance: 'RelatedWorkflow',
  BusinessObject: 'RelatedEvidence',
}

export function getProjectStatusOptions(status: ProjectDto['status'] | undefined) {
  return status ? statusTransitions[status] ?? [] : []
}

export function defaultRelationCode(entityType: ProjectRelationEntityType) {
  return relationCodes[entityType]
}

export function projectDeadline(localDate: string) {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(localDate)) {
    throw new Error('项目任务截止日期必须使用 YYYY-MM-DD')
  }
  return `${localDate}T18:00:00+08:00`
}

export function evidenceOptionsFor(
  entityType: ProjectRelationEntityType,
  source: {
    files: { id: string; originalFileName: string; currentVersionNumber: number }[]
    minutes: { id: string; title: string; meetingAt: string }[]
    workflows: { id: string; status: string; currentNodeCode: string | null; startedAt: string }[]
    objects: { id: string; title: string | null; objectCode: string }[]
  },
  formatDate: (value: string | null) => string,
): ProjectRelationEntity[] {
  if (entityType === 'Attachment') {
    return source.files.map((item) => ({
      id: item.id,
      label: item.originalFileName,
      detail: `文件 · v${item.currentVersionNumber}`,
    }))
  }
  if (entityType === 'MinutesSource') {
    return source.minutes.map((item) => ({
      id: item.id,
      label: item.title,
      detail: `听记 · ${formatDate(item.meetingAt)}`,
    }))
  }
  if (entityType === 'WorkflowInstance') {
    return source.workflows.map((item) => ({
      id: item.id,
      label: `${item.status} · ${item.currentNodeCode || '无当前节点'}`,
      detail: `流程 · ${formatDate(item.startedAt)}`,
    }))
  }
  return source.objects.map((item) => ({
    id: item.id,
    label: item.title || item.objectCode,
    detail: `业务对象 · ${item.objectCode}`,
  }))
}
