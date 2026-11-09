import { apiRequest, setCsrfToken } from '@/api/http'
import type { PermissionDto, SessionDto } from '@/api/types/contracts'

type CsrfResponse = {
  headerName: string
  requestToken: string
}

type LoginResponse = {
  userId: string
  displayName: string
  roles: string[]
  permissions: PermissionDto[]
}

export const authApi = {
  async refreshCsrf(): Promise<string> {
    const response = await apiRequest<CsrfResponse>('/api/v1/auth/csrf')
    setCsrfToken(response.data.requestToken)
    return response.data.requestToken
  },

  async getSession(): Promise<SessionDto> {
    const response = await apiRequest<SessionDto>('/api/v1/auth/session')
    return response.data
  },

  async login(userName: string): Promise<LoginResponse> {
    await this.refreshCsrf()
    const response = await apiRequest<LoginResponse>('/api/v1/auth/demo-login', {
      method: 'POST',
      body: JSON.stringify({ userName }),
    })
    await this.refreshCsrf()
    return response.data
  },

  async getPermissions(): Promise<PermissionDto[]> {
    const response = await apiRequest<PermissionDto[]>('/api/v1/permissions/me')
    return response.data
  },

  async logout(): Promise<void> {
    await apiRequest<{ signedOut: boolean }>('/api/v1/auth/logout', { method: 'POST' })
    setCsrfToken('')
  },
}
