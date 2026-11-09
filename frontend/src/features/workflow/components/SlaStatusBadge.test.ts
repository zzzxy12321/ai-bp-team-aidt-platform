import { render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import SlaStatusBadge from './SlaStatusBadge.vue'

describe('SlaStatusBadge', () => {
  it('distinguishes an AgentProxy decision from a human task', () => {
    render(SlaStatusBadge, {
      props: {
        dueAt: '2026-10-31T10:00:00Z',
        status: 'Escalated',
        decisionMode: 'AgentProxy',
      },
      global: {
        stubs: {
          'el-tag': { template: '<span><slot /></span>' },
        },
      },
    })

    expect(screen.getByText('Escalated')).toBeTruthy()
    expect(screen.getByText('AgentProxy')).toBeTruthy()
  })

  it('shows when SLA is not configured', () => {
    render(SlaStatusBadge, {
      props: { dueAt: null, status: 'None', decisionMode: 'Human' },
      global: {
        stubs: {
          'el-tag': { template: '<span><slot /></span>' },
        },
      },
    })

    expect(screen.getByText(/未配置 SLA/)).toBeTruthy()
    expect(screen.queryByText('AgentProxy')).toBeNull()
  })
})
