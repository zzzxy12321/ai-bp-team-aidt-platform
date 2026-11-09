import { describe, expect, it } from 'vitest'
import {
  agentConfirmationStatus,
  canConfirmMinutesAction,
  eventConfirmationStatus,
  mcpConfirmationStatus,
  minutesConfirmationStatus,
} from './confirmationCenterRules'

describe('unified confirmation status rules', () => {
  it('maps event terminal and information states', () => {
    expect(eventConfirmationStatus('Received')).toBe('NeedsInformation')
    expect(eventConfirmationStatus('AwaitingConfirmation')).toBe('Pending')
    expect(eventConfirmationStatus('Confirmed')).toBe('Confirmed')
    expect(eventConfirmationStatus('Rejected')).toBe('Rejected')
  })

  it('requires assignee and due time before a minutes action can be confirmed', () => {
    const base = {
      status: 'Draft',
      assigneeId: 'user-1',
      dueAt: '2026-12-18T10:00:00Z',
      content: '整理测试记录',
    }
    expect(minutesConfirmationStatus(base)).toBe('Pending')
    expect(minutesConfirmationStatus({ ...base, assigneeId: null })).toBe('NeedsInformation')
    expect(canConfirmMinutesAction(base)).toBe(true)
    expect(canConfirmMinutesAction({ ...base, dueAt: null })).toBe(false)
  })

  it('derives expired MCP and agent decisions without inventing approval', () => {
    expect(mcpConfirmationStatus('Pending', '2026-01-01T00:00:00Z', Date.parse('2026-02-01T00:00:00Z')))
      .toBe('Expired')
    expect(agentConfirmationStatus('AwaitingConfirmation')).toBe('Pending')
    expect(agentConfirmationStatus('Stopped')).toBe('Rejected')
  })
})
