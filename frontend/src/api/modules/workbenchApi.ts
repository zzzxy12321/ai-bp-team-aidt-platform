import { apiRequest, createIdempotencyKey } from '@/api/http'
import type {
  CreateWorkbenchTemplateRequest,
  DepartmentWorkbenchDto,
  UpdateWorkbenchTemplateDraftRequest,
  WorkbenchTemplateDto,
  WorkbenchTemplateValidationDto,
  WorkbenchTemplateVersionDto,
} from '@/api/types/contracts'

export const workbenchApi = {
  async resolve(
    departmentId: string,
    includeTestData = false,
  ): Promise<DepartmentWorkbenchDto> {
    return (
      await apiRequest<DepartmentWorkbenchDto>(
        `/api/v1/departments/${departmentId}/workbench?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async listTemplates(): Promise<WorkbenchTemplateDto[]> {
    return (await apiRequest<WorkbenchTemplateDto[]>('/api/v1/workbench-templates')).data
  },

  async getTemplate(id: string): Promise<WorkbenchTemplateDto> {
    return (await apiRequest<WorkbenchTemplateDto>(`/api/v1/workbench-templates/${id}`)).data
  },

  async createTemplate(request: CreateWorkbenchTemplateRequest): Promise<WorkbenchTemplateDto> {
    return (
      await apiRequest<WorkbenchTemplateDto>('/api/v1/workbench-templates', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(request),
      })
    ).data
  },

  async copyTemplate(
    id: string,
    request: {
      code: string
      name: string
      departmentId: string | null
      audienceTitle?: string | null
    },
  ): Promise<WorkbenchTemplateDto> {
    return (
      await apiRequest<WorkbenchTemplateDto>(
        `/api/v1/workbench-templates/${id}/actions/copy`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
          body: JSON.stringify(request),
        },
      )
    ).data
  },

  async updateDraft(
    id: string,
    request: UpdateWorkbenchTemplateDraftRequest,
  ): Promise<WorkbenchTemplateDto> {
    return (
      await apiRequest<WorkbenchTemplateDto>(`/api/v1/workbench-templates/${id}/draft`, {
        method: 'PUT',
        body: JSON.stringify(request),
      })
    ).data
  },

  async validate(id: string): Promise<WorkbenchTemplateValidationDto> {
    return (
      await apiRequest<WorkbenchTemplateValidationDto>(
        `/api/v1/workbench-templates/${id}/actions/validate`,
        { method: 'POST', body: JSON.stringify({}) },
      )
    ).data
  },

  async publish(id: string): Promise<WorkbenchTemplateDto> {
    return (
      await apiRequest<WorkbenchTemplateDto>(
        `/api/v1/workbench-templates/${id}/actions/publish`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
          body: JSON.stringify({}),
        },
      )
    ).data
  },

  async setStatus(id: string, retire: boolean): Promise<WorkbenchTemplateDto> {
    return (
      await apiRequest<WorkbenchTemplateDto>(
        `/api/v1/workbench-templates/${id}/actions/${retire ? 'retire' : 'restore'}`,
        { method: 'POST', body: JSON.stringify({}) },
      )
    ).data
  },

  async versions(id: string): Promise<WorkbenchTemplateVersionDto[]> {
    return (
      await apiRequest<WorkbenchTemplateVersionDto[]>(
        `/api/v1/workbench-templates/${id}/versions`,
      )
    ).data
  },
}
