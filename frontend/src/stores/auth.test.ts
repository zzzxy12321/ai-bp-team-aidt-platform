import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it } from 'vitest'
import { useAuthStore } from './auth'

describe('auth route contract', () => {
  beforeEach(() => {
    setActivePinia(createPinia())
  })

  it('enforces permissions by default', () => {
    const auth = useAuthStore()
    expect(auth.enforcement).toBe('enforced')
    expect(auth.can('audit:read')).toBe(false)
    auth.permissions = [{ resource: 'audit', action: 'Read' }]
    expect(auth.can('audit:read')).toBe(true)
  })
})
