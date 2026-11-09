import { describe, expect, it } from 'vitest'
import {
  defaultRelationCode,
  evidenceOptionsFor,
  getProjectStatusOptions,
  projectDeadline,
} from './projectOperationRules'

describe('项目操作规则', () => {
  it('仅返回后端允许的项目状态流转', () => {
    expect(getProjectStatusOptions('Draft').map((item) => item.value)).toEqual([
      'Active',
      'Terminated',
    ])
    expect(getProjectStatusOptions('Active').map((item) => item.value)).toContain(
      'AwaitingAcceptance',
    )
    expect(getProjectStatusOptions('Completed')).toEqual([])
    expect(getProjectStatusOptions('Terminated')).toEqual([])
  })

  it('按证据类型生成稳定关系编码', () => {
    expect(defaultRelationCode('Attachment')).toBe('SupportingEvidence')
    expect(defaultRelationCode('MinutesSource')).toBe('MeetingEvidence')
    expect(defaultRelationCode('WorkflowInstance')).toBe('RelatedWorkflow')
    expect(defaultRelationCode('BusinessObject')).toBe('RelatedEvidence')
  })

  it('将项目任务日期固定为上海业务日 18 点', () => {
    expect(projectDeadline('2026-12-18')).toBe('2026-12-18T18:00:00+08:00')
    expect(() => projectDeadline('2026/12/18')).toThrow()
  })

  it('为文件和听记生成业务可读候选', () => {
    const source = {
      files: [{ id: 'file-1', originalFileName: '验收记录.pdf', currentVersionNumber: 2 }],
      minutes: [{ id: 'minutes-1', title: '项目评审', meetingAt: '2026-11-02T01:00:00Z' }],
      workflows: [],
      objects: [],
    }
    expect(evidenceOptionsFor('Attachment', source, String)[0]).toEqual({
      id: 'file-1',
      label: '验收记录.pdf',
      detail: '文件 · v2',
    })
    expect(evidenceOptionsFor('MinutesSource', source, () => '2026/11/02 09:00')[0]).toEqual({
      id: 'minutes-1',
      label: '项目评审',
      detail: '听记 · 2026/11/02 09:00',
    })
  })
})
