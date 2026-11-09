import { apiRequest, createIdempotencyKey } from '@/api/http'
import type {
  AccessChangeRequestDto,
  CreateIamDepartmentRequest,
  CreateRoleRequest,
  DepartmentTreeNodeDto,
  IamUserDetailDto,
  IdentityAdministrationSnapshotDto,
  InviteUserRequest,
  PagedResult,
  RoleDto,
  RoleMemberDto,
  SetRoleMembersRequest,
  SetRolePermissionsRequest,
  SetUserDataScopesRequest,
  SetUserMembershipsRequest,
  UpdateIamDepartmentRequest,
  IamUserListItemDto,
} from '@/api/types/contracts'

export const iamApi = {
  async getSnapshot(search = ''): Promise<IdentityAdministrationSnapshotDto> {
    const query = search ? `?search=${encodeURIComponent(search)}` : ''
    const response = await apiRequest<IdentityAdministrationSnapshotDto>(`/api/v1/iam/snapshot${query}`)
    return response.data
  },

  async createDepartment(request: CreateIamDepartmentRequest): Promise<DepartmentTreeNodeDto> {
    const response = await apiRequest<DepartmentTreeNodeDto>('/api/v1/iam/departments', {
      method: 'POST',
      body: JSON.stringify(request),
    })
    return response.data
  },

  async updateDepartment(
    id: string,
    request: UpdateIamDepartmentRequest,
  ): Promise<DepartmentTreeNodeDto> {
    const response = await apiRequest<DepartmentTreeNodeDto>(`/api/v1/iam/departments/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    })
    return response.data
  },

  async setDepartmentStatus(id: string, activate: boolean): Promise<DepartmentTreeNodeDto> {
    const response = await apiRequest<DepartmentTreeNodeDto>(
      `/api/v1/iam/departments/${id}/actions/${activate ? 'reactivate' : 'deactivate'}`,
      { method: 'POST', body: JSON.stringify({}) },
    )
    return response.data
  },

  async listUsers(
    page = 1,
    pageSize = 50,
    search = '',
  ): Promise<PagedResult<IamUserListItemDto>> {
    const params = new URLSearchParams({
      page: String(page),
      pageSize: String(pageSize),
    })
    if (search) params.set('search', search)
    const query = `?${params.toString()}`
    const response = await apiRequest<PagedResult<IamUserListItemDto>>(`/api/v1/iam/users${query}`)
    return response.data
  },

  async listAllUsers(search = ''): Promise<IamUserListItemDto[]> {
    const rows: IamUserListItemDto[] = []
    let page = 1
    let total = 0
    do {
      const result = await this.listUsers(page, 100, search)
      rows.push(...result.items)
      total = result.total
      page += 1
    } while (rows.length < total && page <= 1000)
    return rows
  },

  async getUser(id: string): Promise<IamUserDetailDto> {
    const response = await apiRequest<IamUserDetailDto>(`/api/v1/iam/users/${id}`)
    return response.data
  },

  async inviteUser(request: InviteUserRequest): Promise<IamUserDetailDto> {
    const response = await apiRequest<IamUserDetailDto>('/api/v1/iam/users', {
      method: 'POST',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify(request),
    })
    return response.data
  },

  async importUsers(
    users: Array<{ userName: string; displayName: string; departmentId: string; isPrimary: boolean; isManager: boolean }>,
  ): Promise<IamUserListItemDto[]> {
    const response = await apiRequest<IamUserListItemDto[]>('/api/v1/iam/users/import', {
      method: 'POST',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify({ users }),
    })
    return response.data
  },

  async setUserStatus(id: string, activate: boolean): Promise<IamUserDetailDto> {
    const response = await apiRequest<IamUserDetailDto>(
      `/api/v1/iam/users/${id}/actions/${activate ? 'reactivate' : 'deactivate'}`,
      { method: 'POST', body: JSON.stringify({}) },
    )
    return response.data
  },

  async setUserDataScopes(id: string, request: SetUserDataScopesRequest): Promise<IamUserDetailDto> {
    const response = await apiRequest<IamUserDetailDto>(`/api/v1/iam/users/${id}/data-scopes`, {
      method: 'PUT',
      body: JSON.stringify(request),
    })
    return response.data
  },

  async setUserMemberships(
    id: string,
    request: SetUserMembershipsRequest,
  ): Promise<IamUserDetailDto> {
    const response = await apiRequest<IamUserDetailDto>(`/api/v1/iam/users/${id}/memberships`, {
      method: 'PUT',
      body: JSON.stringify(request),
    })
    return response.data
  },

  async createRole(request: CreateRoleRequest): Promise<RoleDto> {
    const response = await apiRequest<RoleDto>('/api/v1/iam/roles', {
      method: 'POST',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify(request),
    })
    return response.data
  },

  async copyRole(
    id: string,
    request: { code: string; name: string; description: string | null },
  ): Promise<RoleDto> {
    const response = await apiRequest<RoleDto>(`/api/v1/iam/roles/${id}/actions/copy`, {
      method: 'POST',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify({
        code: request.code,
        name: request.name,
        description: request.description,
      }),
    })
    return response.data
  },

  async updateRole(
    id: string,
    request: {
      expectedVersion: number
      name: string
      roleType: 'System' | 'Business' | 'AgentService'
      description: string | null
      isActive: boolean
    },
  ): Promise<RoleDto> {
    const response = await apiRequest<RoleDto>(`/api/v1/iam/roles/${id}`, {
      method: 'PUT',
      body: JSON.stringify(request),
    })
    return response.data
  },

  async setRoleStatus(id: string, activate: boolean): Promise<RoleDto> {
    const response = await apiRequest<RoleDto>(
      `/api/v1/iam/roles/${id}/actions/${activate ? 'reactivate' : 'deactivate'}`,
      { method: 'POST', body: JSON.stringify({}) },
    )
    return response.data
  },

  async setRolePermissions(id: string, request: SetRolePermissionsRequest): Promise<RoleDto> {
    const response = await apiRequest<RoleDto>(`/api/v1/iam/roles/${id}/permissions`, {
      method: 'PUT',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify(request),
    })
    return response.data
  },

  async getRoleMembers(id: string): Promise<RoleMemberDto[]> {
    const response = await apiRequest<RoleMemberDto[]>(`/api/v1/iam/roles/${id}/members`)
    return response.data
  },

  async setRoleMembers(id: string, request: SetRoleMembersRequest): Promise<RoleMemberDto[]> {
    const response = await apiRequest<RoleMemberDto[]>(`/api/v1/iam/roles/${id}/members`, {
      method: 'PUT',
      headers: { 'Idempotency-Key': createIdempotencyKey() },
      body: JSON.stringify(request),
    })
    return response.data
  },

  async approveAccessRequest(id: string, comment: string): Promise<AccessChangeRequestDto> {
    const response = await apiRequest<AccessChangeRequestDto>(
      `/api/v1/iam/access-requests/${id}/actions/approve`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ comment }),
      },
    )
    return response.data
  },

  async rejectAccessRequest(id: string, comment: string): Promise<AccessChangeRequestDto> {
    const response = await apiRequest<AccessChangeRequestDto>(
      `/api/v1/iam/access-requests/${id}/actions/reject`,
      {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ comment }),
      },
    )
    return response.data
  },
}
