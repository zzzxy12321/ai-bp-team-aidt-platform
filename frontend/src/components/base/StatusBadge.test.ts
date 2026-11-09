import { render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import StatusBadge from './StatusBadge.vue'

describe('StatusBadge', () => {
  it('renders an explicit SQL blocked state', () => {
    render(StatusBadge, {
      props: {
        label: 'SQL BLOCKED',
        tone: 'danger',
      },
    })

    expect(screen.getByText('SQL BLOCKED')).toBeTruthy()
  })
})
