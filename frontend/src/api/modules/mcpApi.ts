import { apiRequest } from '@/api/http'
import type {
  McpClientSummary,
  McpHumanConfirmation,
  McpHumanConfirmationView,
  McpInvocationSummary,
  McpManagementSnapshot,
  McpServiceToken,
  McpServiceTokenRequest,
  McpTokenSummary,
  McpToolDescriptor,
} from '@/api/types/contracts'

export const mcpApi = {
  async getSnapshot(page = 1, pageSize = 100): Promise<McpManagementSnapshot> {
    const response = await apiRequest<McpManagementSnapshot>(
      `/api/v1/mcp/snapshot?page=${page}&pageSize=${pageSize}`,
    )
    return response.data
  },

  async getOwnConfirmations(): Promise<McpHumanConfirmation[]> {
    const response = await apiRequest<McpHumanConfirmation[]>('/api/v1/mcp/confirmations/me')
    return response.data
  },

  async getOwnConfirmationViews(): Promise<McpHumanConfirmationView[]> {
    const response = await apiRequest<McpHumanConfirmationView[]>(
      '/api/v1/mcp/confirmation-views/me',
    )
    return response.data
  },

  async confirm(id: string, comment: string): Promise<McpHumanConfirmation> {
    const response = await apiRequest<McpHumanConfirmation>(
      `/api/v1/mcp/confirmations/${id}/actions/confirm`,
      {
        method: 'POST',
        body: JSON.stringify({ comment }),
      },
    )
    return response.data
  },

  async reject(id: string, comment: string): Promise<McpHumanConfirmation> {
    const response = await apiRequest<McpHumanConfirmation>(
      `/api/v1/mcp/confirmations/${id}/actions/reject`,
      {
        method: 'POST',
        body: JSON.stringify({ comment }),
      },
    )
    return response.data
  },

  async revokeClient(clientId: string): Promise<void> {
    await apiRequest<{ clientId: string; isActive: boolean }>(
      `/api/v1/mcp/clients/${encodeURIComponent(clientId)}/actions/revoke`,
      { method: 'POST' },
    )
  },

  async revokeToken(tokenId: string): Promise<void> {
    await apiRequest<{ tokenId: string; isRevoked: boolean }>(
      `/api/v1/mcp/tokens/${tokenId}/actions/revoke`,
      { method: 'POST' },
    )
  },

  async issueServiceToken(request: McpServiceTokenRequest): Promise<McpServiceToken> {
    const response = await apiRequest<McpServiceToken>(
      '/api/v1/mcp/service-tokens',
      {
        method: 'POST',
        body: JSON.stringify(request),
      },
    )
    return response.data
  },
}

export type {
  McpClientSummary,
  McpHumanConfirmation,
  McpHumanConfirmationView,
  McpInvocationSummary,
  McpManagementSnapshot,
  McpServiceToken,
  McpServiceTokenRequest,
  McpTokenSummary,
  McpToolDescriptor,
}
