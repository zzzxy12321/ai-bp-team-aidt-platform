import { apiRequest, createIdempotencyKey } from '@/api/http'
import type {
  CreateNavigationWorkspaceRequest,
  CurrentNavigationDto,
  NavigationWorkspaceDto,
  NavigationPreferenceDto,
  NavigationPreferenceItem,
  UpdateNavigationWorkspaceRequest,
} from '@/api/types/contracts'

export const navigationApi = {
  async getCurrent(): Promise<CurrentNavigationDto> {
    const response = await apiRequest<CurrentNavigationDto>('/api/v1/navigation/current')
    return response.data
  },

  async listWorkspaces(): Promise<NavigationWorkspaceDto[]> {
    const response = await apiRequest<NavigationWorkspaceDto[]>('/api/v1/navigation/workspaces')
    return response.data
  },

  async getWorkspace(id: string): Promise<NavigationWorkspaceDto> {
    const response = await apiRequest<NavigationWorkspaceDto>(`/api/v1/navigation/workspaces/${id}`)
    return response.data
  },

  async createWorkspace(request: CreateNavigationWorkspaceRequest): Promise<NavigationWorkspaceDto> {
    const response = await apiRequest<NavigationWorkspaceDto>('/api/v1/navigation/workspaces', {
      method: 'POST',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify(request),
    })
    return response.data
  },

  async updateDraft(
    id: string,
    request: UpdateNavigationWorkspaceRequest,
  ): Promise<NavigationWorkspaceDto> {
    const response = await apiRequest<NavigationWorkspaceDto>(
      `/api/v1/navigation/workspaces/${id}/draft`,
      { method: 'PUT', body: JSON.stringify(request) },
    )
    return response.data
  },

  async publish(id: string): Promise<NavigationWorkspaceDto> {
    const response = await apiRequest<NavigationWorkspaceDto>(
      `/api/v1/navigation/workspaces/${id}/actions/publish`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({}),
      },
    )
    return response.data
  },

  async setStatus(id: string, retire: boolean): Promise<NavigationWorkspaceDto> {
    const response = await apiRequest<NavigationWorkspaceDto>(
      `/api/v1/navigation/workspaces/${id}/actions/${retire ? 'retire' : 'restore'}`,
      { method: 'POST', body: JSON.stringify({}) },
    )
    return response.data
  },

  async getPreferences(): Promise<NavigationPreferenceDto[]> {
    const response = await apiRequest<NavigationPreferenceDto[]>('/api/v1/navigation/preferences')
    return response.data
  },

  async setPreferences(items: NavigationPreferenceItem[]): Promise<NavigationPreferenceDto[]> {
    const response = await apiRequest<NavigationPreferenceDto[]>('/api/v1/navigation/preferences', {
      method: 'PUT',
      body: JSON.stringify({ items }),
    })
    return response.data
  },
}
