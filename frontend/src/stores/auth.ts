import { defineStore } from 'pinia'
import { ApiError } from '@/api/http'
import { authApi } from '@/api/modules/authApi'
import type { PermissionDto, SessionDto } from '@/api/types/contracts'

const anonymousSession: SessionDto = {
  userId: '',
  displayName: '未登录',
  organizationId: '',
  isAuthenticated: false,
  isServiceIdentity: false,
  roles: [],
  authorizationState: 'Anonymous',
  authorizationMessage: null,
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    session: anonymousSession as SessionDto,
    permissions: [] as PermissionDto[],
    initialized: false,
    loading: false,
    errorMessage: '',
    requiresReauthentication: false,
    enforcement:
      import.meta.env.VITE_AUTH_ENFORCEMENT === 'contract-preview' ? 'contract-preview' : 'enforced',
  }),
  getters: {
    isAuthenticated: (state) => state.session.isAuthenticated,
    isPreviewMode: (state) => state.enforcement === 'contract-preview',
    isPlatformAdmin: (state) =>
      state.session.roles.includes('platform-admin'),
  },
  actions: {
    async initialize() {
      if (this.initialized || this.loading) {
        return
      }
      this.loading = true
      try {
        this.session = await authApi.getSession()
        if (this.session.authorizationState === 'Stale') {
          this.requiresReauthentication = true
          this.permissions = []
          this.errorMessage =
            this.session.authorizationMessage ?? '权限已更新，请重新登录。'
        } else if (this.session.isAuthenticated) {
          this.permissions = await authApi.getPermissions()
          await authApi.refreshCsrf()
        }
      } catch (error) {
        this.errorMessage = error instanceof ApiError ? error.message : '读取会话失败。'
      } finally {
        this.initialized = true
        this.loading = false
      }
    },

    async login(userName: string) {
      this.loading = true
      this.errorMessage = ''
      this.requiresReauthentication = false
      try {
        const result = await authApi.login(userName)
        this.session = {
          userId: result.userId,
          displayName: result.displayName,
          organizationId: '',
          isAuthenticated: true,
          isServiceIdentity: false,
          roles: result.roles,
          authorizationState: 'Current',
          authorizationMessage: null,
        }
        this.permissions = await authApi.getPermissions()
        this.initialized = true
      } catch (error) {
        this.session = anonymousSession
        this.permissions = []
        this.requiresReauthentication = true
        this.errorMessage = error instanceof ApiError ? error.message : '登录失败。'
        throw error
      } finally {
        this.loading = false
      }
    },

    async logout() {
      await authApi.logout()
      this.session = anonymousSession
      this.permissions = []
      this.requiresReauthentication = false
      this.errorMessage = ''
    },

    can(permission?: string): boolean {
      if (!permission || this.enforcement === 'contract-preview') {
        return true
      }
      return this.permissions.some(
        (item) => `${item.resource}:${item.action}`.toLowerCase() === permission.toLowerCase(),
      )
    },
  },
})
