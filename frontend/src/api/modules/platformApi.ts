import { apiRequest } from '@/api/http'
import type { PlatformCapabilities } from '@/api/types/contracts'

export const platformApi = {
  async getCapabilities(): Promise<PlatformCapabilities> {
    const response = await apiRequest<PlatformCapabilities>('/api/v1/platform/capabilities')
    return response.data
  },
}
