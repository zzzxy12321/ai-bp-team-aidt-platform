import { render, screen } from '@testing-library/vue'
import { describe, expect, it } from 'vitest'
import FeaturePlaceholder from './FeaturePlaceholder.vue'

describe('FeaturePlaceholder', () => {
  it('renders feature copy without pretending persistence is available', () => {
    render(FeaturePlaceholder, {
      props: {
        title: '流程实例',
        description: 'SQL Server 尚未就绪。',
        status: 'SQL BLOCKED',
        tone: 'danger',
      },
    })

    expect(screen.getByRole('heading', { name: '流程实例' })).toBeTruthy()
    expect(screen.getByText('SQL Server 尚未就绪。')).toBeTruthy()
    expect(screen.getByText('SQL BLOCKED')).toBeTruthy()
  })
})
