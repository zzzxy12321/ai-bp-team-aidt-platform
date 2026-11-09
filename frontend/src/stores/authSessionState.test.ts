import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { authApi } from '@/api/modules/authApi'
import { useAuthStore } from './auth'

vi.mock('@/api/modules/authApi', () => ({
  authApi: {
    getSession: vi.fn(),
    getPermissions: vi.fn(),
    refreshCsrf: vi.fn(),
    login: vi.fn(),
    logout: vi.fn(),
  },
}))

describe('auth session authorization refresh', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setActivePinia(createPinia())
  })

  it('blocks stale cookies and asks the user to sign in again', async () => {
    vi.mocked(authApi.getSession).mockResolvedValue({
      userId: 'user-1',
      displayName: '研发工程师',
      organizationId: 'org-1',
      isAuthenticated: true,
      isServiceIdentity: false,
      roles: ['rd-engineer'],
      authorizationState: 'Stale',
      authorizationMessage: '权限已更新，请重新登录。',
    })

    const auth = useAuthStore()
    await auth.initialize()

    expect(auth.requiresReauthentication).toBe(true)
    expect(auth.permissions).toEqual([])
    expect(auth.errorMessage).toBe('权限已更新，请重新登录。')
    expect(authApi.getPermissions).not.toHaveBeenCalled()
  })
})
