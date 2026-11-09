import { apiRequest, createIdempotencyKey } from '@/api/http'
import type {
  CreateWorkflowConfigurationRequest,
  PagedResult,
  UpdateWorkflowConfigurationRequest,
  WorkflowConfigurationDto,
  WorkflowConfigurationValidation,
  WorkflowConfigurationSimulation,
  WorkflowPolicyUserDto,
  RoleDto,
} from '@/api/types/contracts'

export const configurationApi = {
  async list(input: {
    state?: string
    configurationKey?: string
  } = {}): Promise<PagedResult<WorkflowConfigurationDto>> {
    const query = new URLSearchParams({ pageSize: '100' })
    if (input.state) query.set('state', input.state)
    if (input.configurationKey) query.set('configurationKey', input.configurationKey)
    return (
      await apiRequest<PagedResult<WorkflowConfigurationDto>>(
        `/api/v1/configurations?${query.toString()}`,
      )
    ).data
  },

  async get(id: string): Promise<WorkflowConfigurationDto> {
    return (await apiRequest<WorkflowConfigurationDto>(`/api/v1/configurations/${id}`)).data
  },

  async memberCandidates(): Promise<WorkflowPolicyUserDto[]> {
    return (
      await apiRequest<WorkflowPolicyUserDto[]>(
        '/api/v1/configurations/member-candidates',
      )
    ).data
  },

  async workflowRoles(): Promise<RoleDto[]> {
    return (await apiRequest<RoleDto[]>('/api/v1/workflow-roles')).data
  },

  async create(input: CreateWorkflowConfigurationRequest): Promise<WorkflowConfigurationDto> {
    return (
      await apiRequest<WorkflowConfigurationDto>('/api/v1/configurations', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(input),
      })
    ).data
  },

  async update(
    id: string,
    input: UpdateWorkflowConfigurationRequest,
  ): Promise<WorkflowConfigurationDto> {
    return (
      await apiRequest<WorkflowConfigurationDto>(`/api/v1/configurations/${id}`, {
        method: 'PUT',
        body: JSON.stringify(input),
      })
    ).data
  },

  async validate(id: string): Promise<WorkflowConfigurationValidation> {
    return (
      await apiRequest<WorkflowConfigurationValidation>(
        `/api/v1/configurations/${id}/actions/validate`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
        },
      )
    ).data
  },

  async publish(id: string): Promise<WorkflowConfigurationDto> {
    return (
      await apiRequest<WorkflowConfigurationDto>(
        `/api/v1/configurations/${id}/actions/publish`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
        },
      )
    ).data
  },

  async rollback(id: string, comment = ''): Promise<WorkflowConfigurationDto> {
    return (
      await apiRequest<WorkflowConfigurationDto>(
        `/api/v1/configurations/${id}/actions/rollback`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
          body: JSON.stringify({ comment }),
        },
      )
    ).data
  },

  async versions(id: string): Promise<WorkflowConfigurationDto[]> {
    return (
      await apiRequest<WorkflowConfigurationDto[]>(
        `/api/v1/configurations/${id}/versions`,
      )
    ).data
  },

  async setStatus(id: string, disable: boolean): Promise<WorkflowConfigurationDto> {
    return (
      await apiRequest<WorkflowConfigurationDto>(
        `/api/v1/configurations/${id}/actions/${disable ? 'disable' : 'restore'}`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
        },
      )
    ).data
  },

  async simulate(
    id: string,
    businessObjectId?: string,
  ): Promise<WorkflowConfigurationSimulation> {
    return (
      await apiRequest<WorkflowConfigurationSimulation>(
        `/api/v1/configurations/${id}/actions/simulate`,
        {
          method: 'POST',
          body: JSON.stringify({ businessObjectId: businessObjectId || null }),
        },
      )
    ).data
  },
}
