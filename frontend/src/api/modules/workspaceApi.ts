import { apiRequest, createIdempotencyKey } from '@/api/http'
import type {
  AgentProfileDto,
  AgentExecutionResultDto,
  AgentGatewayCallDto,
  AgentRunDto,
  AuditEventDto,
  BusinessObjectDto,
  BusinessObjectContextDto,
  BusinessObjectVersionDto,
  ConnectorDto,
  CreateObjectTypeRequest,
  DashboardDto,
  DepartmentDto,
  DepartmentWorkspaceDto,
  MyWorkDto,
  MinutesActionItemDto,
  MinutesConfirmationUserDto,
  MinutesSourceDto,
  MinutesSummaryDto,
  MinutesTaskDto,
  ObjectFieldUserOptionDto,
  ObjectTypeDto,
  ObjectTypeValidationDto,
  ObjectTypeVersionDto,
  PagedResult,
  WorkflowDefinitionDto,
  WorkflowInstanceDto,
  WorkflowTaskDto,
  WorkflowTaskForm,
  WorkflowTaskAiReviewResult,
  WorkflowTimelineItemDto,
  WorkflowNodePolicyDto,
  WorkflowPolicyUserDto,
  WorkflowVersionNodeDto,
  SaveWorkflowNodePolicyRequest,
  ProjectDto,
  ProjectDetailDto,
  ProjectMemberDto,
  ProjectMilestoneDto,
  ProjectCandidateUserDto,
  ProjectTaskDto,
  ProjectRelationDto,
  InboxEventDto,
  InboxConfirmationDto,
  InboxSourceOptionDto,
  InboxTaskTraceDto,
  TransferWorkflowTaskRequest,
  EscalateWorkflowTaskRequest,
} from '@/api/types/contracts'

export const workspaceApi = {
  async myWork(includeTestData = false): Promise<MyWorkDto> {
    return (
      await apiRequest<MyWorkDto>(`/api/v1/my-work?includeTestData=${includeTestData}`)
    ).data
  },

  async departments(): Promise<DepartmentDto[]> {
    return (await apiRequest<DepartmentDto[]>('/api/v1/departments')).data
  },

  async departmentWorkspace(
    departmentId: string,
    includeTestData = false,
  ): Promise<DepartmentWorkspaceDto> {
    return (
      await apiRequest<DepartmentWorkspaceDto>(
        `/api/v1/departments/${departmentId}/workspace?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async dashboard(includeTestData = false): Promise<DashboardDto> {
    return (
      await apiRequest<DashboardDto>(
        `/api/v1/dashboard?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async objectTypes(): Promise<PagedResult<ObjectTypeDto>> {
    return (await apiRequest<PagedResult<ObjectTypeDto>>('/api/v1/object-types?pageSize=100')).data
  },

  async publishedObjectTypes(): Promise<ObjectTypeDto[]> {
    return (await apiRequest<ObjectTypeDto[]>('/api/v1/object-types/published')).data
  },

  async createObjectType(input: CreateObjectTypeRequest): Promise<ObjectTypeDto> {
    return (
      await apiRequest<ObjectTypeDto>('/api/v1/object-types', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(input),
      })
    ).data
  },

  async updateObjectTypeDraft(
    id: string,
    input: { expectedRevision: number; name: string; fields: ObjectTypeDto['fields'] },
  ): Promise<ObjectTypeDto> {
    return (
      await apiRequest<ObjectTypeDto>(`/api/v1/object-types/${id}/draft`, {
        method: 'PUT',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(input),
      })
    ).data
  },

  async validateObjectType(id: string, expectedRevision: number): Promise<ObjectTypeValidationDto> {
    return (
      await apiRequest<ObjectTypeValidationDto>(`/api/v1/object-types/${id}/actions/validate`, {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ expectedRevision }),
      })
    ).data
  },

  async publishObjectType(id: string, expectedRevision: number): Promise<ObjectTypeDto> {
    return (
      await apiRequest<ObjectTypeDto>(`/api/v1/object-types/${id}/actions/publish`, {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ expectedRevision }),
      })
    ).data
  },

  async retireObjectType(id: string, expectedRevision: number): Promise<ObjectTypeDto> {
    return (
      await apiRequest<ObjectTypeDto>(`/api/v1/object-types/${id}/actions/retire`, {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ expectedRevision }),
      })
    ).data
  },

  async restoreObjectType(id: string, expectedRevision: number): Promise<ObjectTypeDto> {
    return (
      await apiRequest<ObjectTypeDto>(`/api/v1/object-types/${id}/actions/restore`, {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ expectedRevision }),
      })
    ).data
  },

  async objectTypeVersions(id: string): Promise<ObjectTypeVersionDto[]> {
    return (
      await apiRequest<ObjectTypeVersionDto[]>(`/api/v1/object-types/${id}/versions`)
    ).data
  },

  async businessObjects(
    includeTestData = false,
  ): Promise<PagedResult<BusinessObjectDto>> {
    return (
      await apiRequest<PagedResult<BusinessObjectDto>>(
        `/api/v1/business-objects?pageSize=100&includeTestData=${includeTestData}`,
      )
    ).data
  },

  async createBusinessObject(
    objectTypeId: string,
    objectCode: string,
    fields: Record<string, string | null>,
    departmentId?: string,
  ): Promise<BusinessObjectDto> {
    return (
      await apiRequest<BusinessObjectDto>('/api/v1/business-objects', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ objectTypeId, objectCode, fields, departmentId }),
      })
    ).data
  },

  async businessObject(
    id: string,
    includeTestData = false,
  ): Promise<BusinessObjectDto> {
    return (
      await apiRequest<BusinessObjectDto>(
        `/api/v1/business-objects/${id}?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async updateBusinessObject(
    id: string,
    expectedVersion: number,
    fields: Record<string, string | null>,
  ): Promise<BusinessObjectDto> {
    return (
      await apiRequest<BusinessObjectDto>(`/api/v1/business-objects/${id}`, {
        method: 'PUT',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ expectedVersion, fields }),
      })
    ).data
  },

  async businessObjectVersions(
    id: string,
    includeTestData = false,
  ): Promise<BusinessObjectVersionDto[]> {
    return (
      await apiRequest<BusinessObjectVersionDto[]>(
        `/api/v1/business-objects/${id}/versions?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async businessObjectContext(
    id: string,
    includeTestData = false,
  ): Promise<BusinessObjectContextDto> {
    return (
      await apiRequest<BusinessObjectContextDto>(
        `/api/v1/business-objects/${id}/context?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async objectFieldUsers(
    search = '',
    departmentId?: string,
  ): Promise<ObjectFieldUserOptionDto[]> {
    const query = new URLSearchParams({ limit: '100' })
    if (search.trim()) query.set('search', search.trim())
    if (departmentId) query.set('departmentId', departmentId)
    return (
      await apiRequest<ObjectFieldUserOptionDto[]>(
        `/api/v1/business-objects/field-options/users?${query}`,
      )
    ).data
  },

  async workflowDefinitions(): Promise<WorkflowDefinitionDto[]> {
    return (await apiRequest<WorkflowDefinitionDto[]>('/api/v1/workflow-definitions')).data
  },

  async workflowInstances(
    includeTestData = false,
  ): Promise<PagedResult<WorkflowInstanceDto>> {
    return (
      await apiRequest<PagedResult<WorkflowInstanceDto>>(
        `/api/v1/workflow-instances?pageSize=100&includeTestData=${includeTestData}`,
      )
    ).data
  },

  async startWorkflow(
    workflowDefinitionId: string,
    workflowVersionId: string,
    businessObjectId: string,
  ): Promise<WorkflowInstanceDto> {
    return (
      await apiRequest<WorkflowInstanceDto>('/api/v1/workflow-instances', {
        method: 'POST',
        body: JSON.stringify({
          workflowDefinitionId,
          workflowVersionId,
          businessObjectId,
          idempotencyKey: createIdempotencyKey(),
        }),
      })
    ).data
  },

  async tasks(includeTestData = false): Promise<PagedResult<WorkflowTaskDto>> {
    return (
      await apiRequest<PagedResult<WorkflowTaskDto>>(
        `/api/v1/workflow-tasks?pageSize=100&includeTestData=${includeTestData}`,
      )
    ).data
  },

  async decideTask(
    taskId: string,
    decision: 'Approve' | 'Return' | 'Terminate',
    expectedVersion: number,
    comment: string,
    formDataJson = '{}',
    formDefinitionHash = '',
  ): Promise<WorkflowTaskDto> {
    return (
      await apiRequest<WorkflowTaskDto>(`/api/v1/workflow-tasks/${taskId}/actions/decide`, {
        method: 'POST',
        body: JSON.stringify({
          decision,
          expectedVersion,
          comment,
          idempotencyKey: createIdempotencyKey(),
          formDataJson,
          formDefinitionHash,
        }),
      })
    ).data
  },

  async taskForm(taskId: string): Promise<WorkflowTaskForm> {
    return (
      await apiRequest<WorkflowTaskForm>(`/api/v1/workflow-tasks/${taskId}/form`)
    ).data
  },

  async saveTaskFormDraft(
    taskId: string,
    expectedVersion: number,
    formDataJson: string,
    formDefinitionHash: string,
  ): Promise<WorkflowTaskDto> {
    return (
      await apiRequest<WorkflowTaskDto>(`/api/v1/workflow-tasks/${taskId}/form-draft`, {
        method: 'PUT',
        body: JSON.stringify({
          expectedVersion,
          formDataJson,
          formDefinitionHash,
          idempotencyKey: createIdempotencyKey(),
        }),
      })
    ).data
  },

  async runTaskAiReview(taskId: string): Promise<WorkflowTaskAiReviewResult> {
    return (
      await apiRequest<WorkflowTaskAiReviewResult>(
        `/api/v1/workflow-tasks/${taskId}/actions/ai-review`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
        },
      )
    ).data
  },

  async transferTask(
    taskId: string,
    input: TransferWorkflowTaskRequest,
  ): Promise<WorkflowTaskDto> {
    return (
      await apiRequest<WorkflowTaskDto>(
        `/api/v1/workflow-tasks/${taskId}/actions/transfer`,
        { method: 'POST', body: JSON.stringify(input) },
      )
    ).data
  },

  async escalateTask(
    taskId: string,
    input: EscalateWorkflowTaskRequest,
  ): Promise<WorkflowTaskDto> {
    return (
      await apiRequest<WorkflowTaskDto>(
        `/api/v1/workflow-tasks/${taskId}/actions/escalate`,
        { method: 'POST', body: JSON.stringify(input) },
      )
    ).data
  },

  async timeline(
    instanceId: string,
    includeTestData = false,
  ): Promise<WorkflowTimelineItemDto[]> {
    return (
      await apiRequest<WorkflowTimelineItemDto[]>(
        `/api/v1/workflow-instances/${instanceId}/timeline?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async audit(query: {
    page?: number
    pageSize?: number
    entityType?: string
    entityId?: string
  } = {}): Promise<PagedResult<AuditEventDto>> {
    const params = new URLSearchParams({
      page: String(query.page ?? 1),
      pageSize: String(query.pageSize ?? 100),
    })
    if (query.entityType) params.set('entityType', query.entityType)
    if (query.entityId) params.set('entityId', query.entityId)
    return (
      await apiRequest<PagedResult<AuditEventDto>>(
        `/api/v1/audit-events?${params.toString()}`,
      )
    ).data
  },

  async agents(): Promise<AgentProfileDto[]> {
    return (await apiRequest<AgentProfileDto[]>('/api/v1/agents')).data
  },

  async agentRuns(): Promise<PagedResult<AgentRunDto>> {
    return (await apiRequest<PagedResult<AgentRunDto>>('/api/v1/agents/runs?pageSize=100')).data
  },

  async startAgentRun(agentProfileId: string): Promise<AgentRunDto> {
    return (
      await apiRequest<AgentRunDto>('/api/v1/agents/runs', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({
          agentProfileId,
          action: 'Draft',
          amount: 0,
          riskLevel: 'Low',
          model: 'aidt-rule-sim',
        }),
      })
    ).data
  },

  async confirmAgentRun(runId: string): Promise<AgentRunDto> {
    return (
      await apiRequest<AgentRunDto>(`/api/v1/agents/runs/${runId}/actions/confirm`, {
        method: 'POST',
        body: JSON.stringify({ confirmationId: crypto.randomUUID() }),
      })
    ).data
  },

  async stopAgentRun(runId: string): Promise<AgentRunDto> {
    return (
      await apiRequest<AgentRunDto>(`/api/v1/agents/runs/${runId}/actions/stop`, {
        method: 'POST',
      })
    ).data
  },

  async connectors(): Promise<ConnectorDto[]> {
    return (await apiRequest<ConnectorDto[]>('/api/v1/connectors')).data
  },

  async minutes(query: {
    page?: number
    pageSize?: number
    includeTestData?: boolean
    search?: string
    departmentId?: string
    status?: string
    actionItemStatus?: string
    sourceType?: string
    meetingFrom?: string
    meetingTo?: string
  } = {}): Promise<PagedResult<MinutesSourceDto>> {
    const params = new URLSearchParams({
      page: String(query.page ?? 1),
      pageSize: String(query.pageSize ?? 100),
      includeTestData: String(query.includeTestData ?? false),
    })
    for (const key of [
      'search',
      'departmentId',
      'status',
      'actionItemStatus',
      'sourceType',
      'meetingFrom',
      'meetingTo',
    ] as const) {
      const value = query[key]
      if (value) params.set(key, value)
    }
    return (
      await apiRequest<PagedResult<MinutesSourceDto>>(
        `/api/v1/minutes?${params.toString()}`,
      )
    ).data
  },

  async minutesSummary(includeTestData = false): Promise<MinutesSummaryDto> {
    return (
      await apiRequest<MinutesSummaryDto>(
        `/api/v1/minutes/summary?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async registerMinutes(input: {
    departmentId: string
    title: string
    meetingAt?: string
    transcript: string
  }): Promise<MinutesSourceDto> {
    return (
      await apiRequest<MinutesSourceDto>('/api/v1/minutes', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(input),
      })
    ).data
  },

  async uploadMinutes(input: {
    file: File
    departmentId: string
    title: string
    meetingAt?: string
  }): Promise<MinutesSourceDto> {
    const body = new FormData()
    body.append('file', input.file)
    body.append('departmentId', input.departmentId)
    body.append('title', input.title)
    if (input.meetingAt) body.append('meetingAt', input.meetingAt)
    return (
      await apiRequest<MinutesSourceDto>('/api/v1/minutes/upload', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body,
      })
    ).data
  },

  async analyzeMinutes(sourceId: string, agentProfileId: string): Promise<AgentRunDto> {
    return (
      await apiRequest<AgentRunDto>(`/api/v1/minutes/${sourceId}/actions/analyze`, {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ agentProfileId }),
      })
    ).data
  },

  async minutesActionItems(sourceId: string): Promise<MinutesActionItemDto[]> {
    return (
      await apiRequest<MinutesActionItemDto[]>(`/api/v1/minutes/${sourceId}/action-items`)
    ).data
  },

  async minutesConfirmationUsers(departmentId: string): Promise<MinutesConfirmationUserDto[]> {
    return (
      await apiRequest<MinutesConfirmationUserDto[]>(
        `/api/v1/minutes/confirmation-users?departmentId=${departmentId}`,
      )
    ).data
  },

  async updateMinutesAction(
    sourceId: string,
    item: MinutesActionItemDto,
  ): Promise<MinutesActionItemDto> {
    return (
      await apiRequest<MinutesActionItemDto>(
        `/api/v1/minutes/${sourceId}/action-items/${item.id}`,
        {
          method: 'PUT',
          body: JSON.stringify({
            assigneeId: item.assigneeId,
            dueAt: item.dueAt,
            content: item.content,
            priority: item.priority,
            objectTypeId: item.objectTypeId,
            workflowVersionId: item.workflowVersionId,
            missingInformation: item.missingInformation,
            projectId: item.projectId,
            businessObjectId: item.businessObjectId,
            workflowInstanceId: item.workflowInstanceId,
          }),
        },
      )
    ).data
  },

  async confirmMinutesAction(item: MinutesActionItemDto): Promise<MinutesTaskDto> {
    return (
      await apiRequest<MinutesTaskDto>(
        `/api/v1/minutes/action-items/${item.id}/actions/confirm`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
          body: JSON.stringify({
            assigneeId: item.assigneeId,
            dueAt: item.dueAt,
            content: item.content,
            priority: item.priority,
            objectTypeId: item.objectTypeId,
            workflowVersionId: item.workflowVersionId,
            businessObjectId: item.businessObjectId,
            workflowInstanceId: item.workflowInstanceId,
            projectId: item.projectId,
          }),
        },
      )
    ).data
  },

  async executeAgentRun(runId: string): Promise<AgentExecutionResultDto> {
    return (
      await apiRequest<AgentExecutionResultDto>(
        `/api/v1/agents/runs/${runId}/actions/execute`,
        { method: 'POST' },
      )
    ).data
  },

  async agentGatewayCalls(runId: string): Promise<AgentGatewayCallDto[]> {
    return (
      await apiRequest<AgentGatewayCallDto[]>(
        `/api/v1/agents/runs/${runId}/gateway-calls`,
      )
    ).data
  },

  async nodePolicies(workflowVersionId: string): Promise<WorkflowNodePolicyDto[]> {
    return (
      await apiRequest<WorkflowNodePolicyDto[]>(
        `/api/v1/workflow-versions/${workflowVersionId}/node-policies`,
      )
    ).data
  },

  async policyUsers(): Promise<WorkflowPolicyUserDto[]> {
    return (await apiRequest<WorkflowPolicyUserDto[]>('/api/v1/workflow-policy/users')).data
  },

  async workflowVersionNodes(workflowVersionId: string): Promise<WorkflowVersionNodeDto[]> {
    return (
      await apiRequest<WorkflowVersionNodeDto[]>(
        `/api/v1/workflow-versions/${workflowVersionId}/nodes`,
      )
    ).data
  },

  async saveNodePolicy(
    workflowVersionId: string,
    nodeCode: string,
    body: SaveWorkflowNodePolicyRequest,
  ): Promise<WorkflowNodePolicyDto> {
    return (
      await apiRequest<WorkflowNodePolicyDto>(
        `/api/v1/workflow-versions/${workflowVersionId}/nodes/${nodeCode}/policy`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
          body: JSON.stringify(body),
        },
      )
    ).data
  },

  async requestAgentProxy(taskId: string): Promise<AgentRunDto> {
    return (
      await apiRequest<AgentRunDto>(
        `/api/v1/workflow-tasks/${taskId}/actions/agent-proxy`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
        },
      )
    ).data
  },

  async confirmAgentProxy(runId: string): Promise<AgentRunDto> {
    return (
      await apiRequest<AgentRunDto>(
        `/api/v1/agents/runs/${runId}/actions/confirm-proxy`,
        {
          method: 'POST',
          body: JSON.stringify({ confirmationId: crypto.randomUUID() }),
        },
      )
    ).data
  },

  async projects(includeTestData = false): Promise<PagedResult<ProjectDto>> {
    return (
      await apiRequest<PagedResult<ProjectDto>>(
        `/api/v1/projects?pageSize=100&includeTestData=${includeTestData}`,
      )
    ).data
  },

  async project(id: string, includeTestData = false): Promise<ProjectDetailDto> {
    return (
      await apiRequest<ProjectDetailDto>(
        `/api/v1/projects/${id}?includeTestData=${includeTestData}`,
      )
    ).data
  },

  async createProject(input: {
    objectCode: string
    name: string
    ownerUserId: string
    departmentId: string
    plannedStartDate: string | null
    plannedEndDate: string | null
    objective: string
    currentStage: string
    confidentialityLevel: 'Internal' | 'Sensitive' | 'Core'
    initialStatus?: 'Draft' | 'Active'
  }): Promise<ProjectDto> {
    return (
      await apiRequest<ProjectDto>('/api/v1/projects', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(input),
      })
    ).data
  },

  async updateProject(
    projectId: string,
    input: {
      expectedVersion: number
      name: string
      ownerUserId: string
      plannedStartDate: string | null
      plannedEndDate: string | null
      objective: string
      currentStage: string
      confidentialityLevel: ProjectDto['confidentialityLevel']
    },
  ): Promise<ProjectDto> {
    return (
      await apiRequest<ProjectDto>(`/api/v1/projects/${projectId}`, {
        method: 'PUT',
        body: JSON.stringify(input),
      })
    ).data
  },

  async changeProjectStatus(
    projectId: string,
    expectedVersion: number,
    status: ProjectDto['status'],
    comment = '',
  ): Promise<ProjectDto> {
    return (
      await apiRequest<ProjectDto>(
        `/api/v1/projects/${projectId}/actions/change-status`,
        {
          method: 'POST',
          body: JSON.stringify({ expectedVersion, status, comment }),
        },
      )
    ).data
  },

  async projectMemberCandidates(projectId: string): Promise<ProjectCandidateUserDto[]> {
    return (
      await apiRequest<ProjectCandidateUserDto[]>(
        `/api/v1/projects/${projectId}/member-candidates`,
      )
    ).data
  },

  async addProjectMember(
    projectId: string,
    userId: string,
    projectRole: ProjectMemberDto['projectRole'] = 'Contributor',
    isLead = false,
  ): Promise<ProjectMemberDto> {
    return (
      await apiRequest<ProjectMemberDto>(`/api/v1/projects/${projectId}/members`, {
        method: 'POST',
        body: JSON.stringify({ userId, projectRole, isLead }),
      })
    ).data
  },

  async removeProjectMember(projectId: string, userId: string, reason = ''): Promise<ProjectMemberDto> {
    return (
      await apiRequest<ProjectMemberDto>(
        `/api/v1/projects/${projectId}/members/${userId}`,
        {
          method: 'DELETE',
          body: JSON.stringify({ reason }),
        },
      )
    ).data
  },

  async addMilestone(
    projectId: string,
    input: {
      title: string
      ownerUserId: string
      plannedDate: string
      status: ProjectMilestoneDto['status']
    },
  ): Promise<ProjectMilestoneDto> {
    return (
      await apiRequest<ProjectMilestoneDto>(`/api/v1/projects/${projectId}/milestones`, {
        method: 'POST',
        body: JSON.stringify(input),
      })
    ).data
  },

  async updateMilestone(
    projectId: string,
    milestoneId: string,
    input: {
      expectedVersion: number
      title: string
      ownerUserId: string
      plannedDate: string
      status: ProjectMilestoneDto['status']
    },
  ): Promise<ProjectMilestoneDto> {
    return (
      await apiRequest<ProjectMilestoneDto>(
        `/api/v1/projects/${projectId}/milestones/${milestoneId}`,
        { method: 'PUT', body: JSON.stringify(input) },
      )
    ).data
  },

  async addProjectRelation(
    projectId: string,
    input: {
      entityType: string
      entityId: string
      relationCode: string
      evidenceExcerpt?: string | null
    },
  ): Promise<ProjectRelationDto> {
    return (
      await apiRequest<ProjectRelationDto>(`/api/v1/projects/${projectId}/relations`, {
        method: 'POST',
        body: JSON.stringify(input),
      })
    ).data
  },

  async createProjectTask(
    projectId: string,
    input: {
      assigneeId: string
      title: string
      content: string
      dueAt: string
      priority: ProjectTaskDto['priority']
    },
  ): Promise<ProjectTaskDto> {
    return (
      await apiRequest<ProjectTaskDto>(`/api/v1/projects/${projectId}/tasks`, {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(input),
      })
    ).data
  },

  async completeProjectTask(
    taskId: string,
    expectedVersion: number,
    comment = '',
  ): Promise<ProjectTaskDto> {
    return (
      await apiRequest<ProjectTaskDto>(
        `/api/v1/project-tasks/${taskId}/actions/complete`,
        { method: 'POST', body: JSON.stringify({ expectedVersion, comment }) },
      )
    ).data
  },

  async inbox(includeTestData = false, status?: string): Promise<PagedResult<InboxEventDto>> {
    const statusQuery = status ? `&status=${encodeURIComponent(status)}` : ''
    return (
      await apiRequest<PagedResult<InboxEventDto>>(
        `/api/v1/inbox?pageSize=100&includeTestData=${includeTestData}${statusQuery}`,
      )
    ).data
  },

  async inboxSources(departmentId: string): Promise<InboxSourceOptionDto[]> {
    return (
      await apiRequest<InboxSourceOptionDto[]>(
        `/api/v1/inbox/sources?departmentId=${departmentId}`,
      )
    ).data
  },

  async receiveInboxEvent(input: {
    departmentId: string
    title: string
    content: string
    sourceType?: InboxEventDto['sourceType']
    sourceId?: string | null
    attachmentId?: string | null
  }): Promise<InboxEventDto> {
    return (
      await apiRequest<InboxEventDto>('/api/v1/inbox', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify(input),
      })
    ).data
  },

  async uploadInboxEvent(input: {
    file: File
    departmentId: string
    title: string
  }): Promise<InboxEventDto> {
    const body = new FormData()
    body.append('file', input.file)
    body.append('departmentId', input.departmentId)
    body.append('title', input.title)
    return (
      await apiRequest<InboxEventDto>('/api/v1/inbox/upload', {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body,
      })
    ).data
  },

  async analyzeInboxEvent(eventId: string, agentProfileId: string): Promise<InboxEventDto> {
    return (
      await apiRequest<InboxEventDto>(`/api/v1/inbox/${eventId}/actions/analyze`, {
        method: 'POST',
        headers: { 'Idempotency-Key': createIdempotencyKey() },
        body: JSON.stringify({ agentProfileId }),
      })
    ).data
  },

  async confirmInboxEvent(
    eventId: string,
    input: {
      projectId: string
      assigneeId?: string | null
      dueAt?: string | null
      priority?: 'P0' | 'P1' | 'P2'
      content?: string | null
      workflowInstanceId?: string | null
      workflowDefinitionId?: string | null
      workflowVersionId?: string | null
      createTask?: boolean
    },
  ): Promise<InboxConfirmationDto> {
    return (
      await apiRequest<InboxConfirmationDto>(
        `/api/v1/inbox/${eventId}/actions/confirm`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
          body: JSON.stringify({ createTask: true, ...input }),
        },
      )
    ).data
  },

  async confirmAndStartInboxFlow(
    eventId: string,
    input: {
      projectId: string
      assigneeId: string
      dueAt: string
      priority: 'P0' | 'P1' | 'P2'
      content: string
      workflowInstanceId?: string | null
      workflowDefinitionId?: string | null
      workflowVersionId?: string | null
      createTask: boolean
    },
  ): Promise<InboxConfirmationDto> {
    return (
      await apiRequest<InboxConfirmationDto>(
        `/api/v1/inbox/${eventId}/actions/confirm-and-start-flow`,
        {
          method: 'POST',
          headers: { 'Idempotency-Key': createIdempotencyKey() },
          body: JSON.stringify(input),
        },
      )
    ).data
  },

  async traceProjectTask(taskId: string): Promise<InboxTaskTraceDto> {
    return (
      await apiRequest<InboxTaskTraceDto>(`/api/v1/project-tasks/${taskId}/trace`)
    ).data
  },
}
