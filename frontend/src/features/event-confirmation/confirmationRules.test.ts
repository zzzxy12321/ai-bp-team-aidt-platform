import { describe, expect, it } from 'vitest'
import {
  formatShanghaiDate,
  fromShanghaiInput,
  isConfirmationDraftComplete,
  toShanghaiInput,
} from './confirmationRules'

describe('event confirmation Shanghai time rules', () => {
  it('keeps the requested Shanghai calendar day without shifting to the next day', () => {
    expect(toShanghaiInput('2025-08-15T10:00:00.000Z')).toBe('2025-08-15T18:00:00')
    expect(fromShanghaiInput('2025-08-15T18:00:00')).toBe('2025-08-15T18:00:00+08:00')
  })

  it('converts an offset timestamp back to the same Shanghai wall time', () => {
    expect(toShanghaiInput('2025-08-15T18:00:00+08:00')).toBe('2025-08-15T18:00:00')
    expect(formatShanghaiDate('2025-08-15T18:00:00+08:00')).toMatch(/2025.*08.*15.*18:00/)
  })

  it('rejects incomplete wall-time input instead of guessing a timezone', () => {
    expect(fromShanghaiInput('2025-08-15T18:00')).toBeNull()
    expect(toShanghaiInput(null)).toBe('')
  })

  it('blocks final confirmation without a responsible person or deadline', () => {
    const base = {
      projectId: 'project-1',
      assigneeId: 'user-1',
      dueLocal: '2026-12-20T18:00:00',
      content: '确认任务',
      flowMode: 'none' as const,
      workflowInstanceId: '',
      workflowDefinitionId: '',
    }
    expect(isConfirmationDraftComplete(base)).toBe(true)
    expect(isConfirmationDraftComplete({ ...base, assigneeId: '' })).toBe(false)
    expect(isConfirmationDraftComplete({ ...base, dueLocal: '' })).toBe(false)
    expect(isConfirmationDraftComplete({ ...base, flowMode: 'new' })).toBe(false)
  })
})
