import { apiRequest, createIdempotencyKey } from '@/api/http'
import type {
  AgentIdentityOptionsDto,
  AgentProfileChangeRequestDto,
  AgentProfileContextDto,
  AgentProfileDetailDto,
  AgentProfilePublishResultDto,
  AgentProfileValidationDto,
  AgentProfileVersionDto,
  AgentRunDto,
  CreateAgentProfileDraftRequest,
  CreateNextAgentProfileDraftRequest,
  DisableAgentProfileRequest,
  PagedResult,
  PublishAgentProfileVersionRequest,
  ResumeAgentProfileRequest,
  ReviewAgentProfileChangeRequest,
  RollbackAgentProfileRequest,
  UpdateAgentProfileDraftRequest,
  ValidateAgentProfileVersionRequest,
} from '@/api/types/contracts'

export type AgentProfileListQuery = {
  page?: number
  pageSize?: number
  lifecycleStatus?: string
  departmentCode?: string
  search?: string
}

export const agentApi = {
  async list(query: AgentProfileListQuery = {}): Promise<PagedResult<AgentProfileDetailDto>> {
    const params = new URLSearchParams({
      page: String(query.page ?? 1),
      pageSize: String(query.pageSize ?? 100),
    })
    if (query.lifecycleStatus) params.set('lifecycleStatus', query.lifecycleStatus)
    if (query.departmentCode) params.set('departmentCode', query.departmentCode)
    if (query.search) params.set('search', query.search)
    return (await apiRequest<PagedResult<AgentProfileDetailDto>>(
      `/api/v1/agents/manage?${params.toString()}`,
    )).data
  },

  async options(profileId?: string): Promise<AgentIdentityOptionsDto> {
    const query = profileId ? `?profileId=${encodeURIComponent(profileId)}` : ''
    return (await apiRequest<AgentIdentityOptionsDto>(`/api/v1/agents/options${query}`)).data
  },

  async detail(profileId: string): Promise<AgentProfileDetailDto> {
    return (await apiRequest<AgentProfileDetailDto>(`/api/v1/agents/${profileId}`)).data
  },

  async context(profileId: string): Promise<AgentProfileContextDto> {
    return (await apiRequest<AgentProfileContextDto>(
      `/api/v1/agents/${profileId}/context`,
    )).data
  },

  async startRun(
    profileId: string,
    options: {
      action?: string
      riskLevel?: 'Low' | 'Medium' | 'High'
      model?: string | null
      toolCode?: string | null
    } = {},
  ): Promise<AgentRunDto> {
    return (await apiRequest<AgentRunDto>('/api/v1/agents/runs', {
      method: 'POST',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify({
        agentProfileId: profileId,
        action: options.action ?? 'Draft',
        amount: 0,
        riskLevel: options.riskLevel ?? 'Low',
        model: options.model === undefined ? 'aidt-rule-sim' : options.model,
        toolCode: options.toolCode ?? null,
      }),
    })).data
  },

  async versions(profileId: string): Promise<AgentProfileVersionDto[]> {
    return (await apiRequest<AgentProfileVersionDto[]>(
      `/api/v1/agents/${profileId}/versions`,
    )).data
  },

  async createDraft(request: CreateAgentProfileDraftRequest): Promise<AgentProfileDetailDto> {
    return (await apiRequest<AgentProfileDetailDto>('/api/v1/agents/drafts', {
      method: 'POST',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify(request),
    })).data
  },

  async createNextDraft(
    profileId: string,
    request: CreateNextAgentProfileDraftRequest,
  ): Promise<AgentProfileVersionDto> {
    return (await apiRequest<AgentProfileVersionDto>(`/api/v1/agents/${profileId}/drafts`, {
      method: 'POST',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify(request),
    })).data
  },

  async updateDraft(
    profileId: string,
    versionId: string,
    request: UpdateAgentProfileDraftRequest,
  ): Promise<AgentProfileVersionDto> {
    return (await apiRequest<AgentProfileVersionDto>(
      `/api/v1/agents/${profileId}/drafts/${versionId}`,
      {
        method: 'PUT',
        body: JSON.stringify(request),
      },
    )).data
  },

  async validate(
    profileId: string,
    versionId: string,
    request: ValidateAgentProfileVersionRequest,
  ): Promise<AgentProfileValidationDto> {
    return (await apiRequest<AgentProfileValidationDto>(
      `/api/v1/agents/${profileId}/versions/${versionId}/actions/validate`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(request),
      },
    )).data
  },

  async publish(
    profileId: string,
    versionId: string,
    request: PublishAgentProfileVersionRequest,
  ): Promise<AgentProfilePublishResultDto> {
    return (await apiRequest<AgentProfilePublishResultDto>(
      `/api/v1/agents/${profileId}/versions/${versionId}/actions/publish`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(request),
      },
    )).data
  },

  async disable(
    profileId: string,
    request: DisableAgentProfileRequest,
  ): Promise<AgentProfileDetailDto> {
    return (await apiRequest<AgentProfileDetailDto>(
      `/api/v1/agents/${profileId}/actions/disable`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(request),
      },
    )).data
  },

  async resume(
    profileId: string,
    request: ResumeAgentProfileRequest,
  ): Promise<AgentProfileDetailDto> {
    return (await apiRequest<AgentProfileDetailDto>(
      `/api/v1/agents/${profileId}/actions/resume`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(request),
      },
    )).data
  },

  async rollback(
    profileId: string,
    request: RollbackAgentProfileRequest,
  ): Promise<AgentProfilePublishResultDto> {
    return (await apiRequest<AgentProfilePublishResultDto>(
      `/api/v1/agents/${profileId}/actions/rollback`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(request),
      },
    )).data
  },

  async changeRequests(status = 'Pending'): Promise<AgentProfileChangeRequestDto[]> {
    return (await apiRequest<AgentProfileChangeRequestDto[]>(
      `/api/v1/agents/change-requests?status=${encodeURIComponent(status)}`,
    )).data
  },

  async reviewChangeRequest(
    requestId: string,
    request: ReviewAgentProfileChangeRequest,
  ): Promise<AgentProfilePublishResultDto> {
    return (await apiRequest<AgentProfilePublishResultDto>(
      `/api/v1/agents/change-requests/${requestId}/actions/review`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(request),
      },
    )).data
  },
}
