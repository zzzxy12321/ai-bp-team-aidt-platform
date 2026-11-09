import { apiDownload, apiRequest } from '@/api/http'
import type {
  FileAttachmentDto,
  FileVersionDto,
  PagedResult,
} from '@/api/types/contracts'

export type UploadFileInput = {
  file: File
  departmentId: string
  businessObjectId?: string
  workflowInstanceId?: string
  workflowTaskId?: string
  sensitivity: 'Internal' | 'Sensitive' | 'Core'
}

function uploadBody(input: UploadFileInput): FormData {
  const body = new FormData()
  body.append('file', input.file)
  body.append('departmentId', input.departmentId)
  body.append('sensitivity', input.sensitivity)
  if (input.businessObjectId) body.append('businessObjectId', input.businessObjectId)
  if (input.workflowInstanceId) body.append('workflowInstanceId', input.workflowInstanceId)
  if (input.workflowTaskId) body.append('workflowTaskId', input.workflowTaskId)
  return body
}

export const fileApi = {
  async list(params: {
    departmentId?: string
    businessObjectId?: string
    includeTestData?: boolean
    page?: number
    pageSize?: number
  } = {}): Promise<PagedResult<FileAttachmentDto>> {
    const search = new URLSearchParams({
      page: String(params.page ?? 1),
      pageSize: String(params.pageSize ?? 100),
      includeTestData: String(params.includeTestData ?? false),
    })
    if (params.departmentId) search.set('departmentId', params.departmentId)
    if (params.businessObjectId) search.set('businessObjectId', params.businessObjectId)
    return (await apiRequest<PagedResult<FileAttachmentDto>>(`/api/v1/files?${search}`)).data
  },

  async get(id: string, includeTestData = false): Promise<FileAttachmentDto> {
    return (
      await apiRequest<FileAttachmentDto>(
        `/api/v1/files/${id}?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async upload(input: UploadFileInput): Promise<FileAttachmentDto> {
    return (
      await apiRequest<FileAttachmentDto>('/api/v1/files', {
        method: 'POST',
        body: uploadBody(input),
      })
    ).data
  },

  async addVersion(id: string, input: UploadFileInput): Promise<FileAttachmentDto> {
    return (
      await apiRequest<FileAttachmentDto>(`/api/v1/files/${id}/versions`, {
        method: 'POST',
        body: uploadBody(input),
      })
    ).data
  },

  async versions(id: string, includeTestData = false): Promise<FileVersionDto[]> {
    return (
      await apiRequest<FileVersionDto[]>(
        `/api/v1/files/${id}/versions?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async download(id: string, includeTestData = false): Promise<Blob> {
    return apiDownload(
      `/api/v1/files/${id}/download?includeTestData=${includeTestData}`,
    )
  },

  async preview(id: string, includeTestData = false): Promise<Blob> {
    return apiDownload(
      `/api/v1/files/${id}/preview?includeTestData=${includeTestData}`,
    )
  },

  async requestDeletion(id: string, reason: string): Promise<FileAttachmentDto> {
    return (
      await apiRequest<FileAttachmentDto>(`/api/v1/files/${id}/actions/delete-request`, {
        method: 'POST',
        body: JSON.stringify({ reason }),
      })
    ).data
  },
}
