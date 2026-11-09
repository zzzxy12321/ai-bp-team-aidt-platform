export type CapabilityState =
  | 'available'
  | 'skeleton'
  | 'contract_ready'
  | 'planned'
  | 'blocked'
  | 'disconnected'

export interface ApiEnvelope<T> {
  code: string
  message: string
  data: T
  traceId: string
}

export interface PlatformCapability {
  featureId: string
  name: string
  state: CapabilityState
  reason: string
}

export interface PlatformCapabilities {
  projectState: string
  databaseMigrationState: string
  databaseProvider: string
  externalSystemsMode: string
  currentEnvironmentCode: DataEnvironmentCode
  maxUploadBytes: number
  capabilities: PlatformCapability[]
}

export type SessionDto = {
  userId: string
  displayName: string
  organizationId: string
  isAuthenticated: boolean
  isServiceIdentity: boolean
  roles: string[]
  authorizationState: 'Current' | 'Stale' | 'Anonymous'
  authorizationMessage: string | null
}

export type DataEnvironmentCode = 'demo' | 'test' | 'prod'

export type DepartmentDto = {
  id: string
  code: string
  name: string
  managerUserId: string | null
  isActive: boolean
  isPrimary: boolean
}

export type WorkItemDto = {
  id: string
  kind: 'Task' | 'Workflow'
  title: string
  objectCode: string
  workflowName: string
  currentStep: string
  assigneeDisplayName: string
  dueAt: string | null
  status: string
  nextAction: string
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  departmentId: string | null
  workflowInstanceId: string | null
  taskId: string | null
}

export type WorkItemViewDto = {
  id: string
  sourceType:
    | 'WorkflowTask'
    | 'ProjectTask'
    | 'EventInbox'
    | 'MinutesActionItem'
    | 'McpHumanConfirmation'
    | 'AgentRun'
  sourceLabel: string
  businessTitle: string
  contextTitle: string
  projectId: string | null
  departmentId: string | null
  departmentName: string
  currentStage: string
  assigneeId: string | null
  assigneeDisplayName: string
  dueAt: string | null
  riskLevel: 'High' | 'Medium' | 'Low'
  riskLabel: string
  priorityGroup:
    | 'PermanentHumanBoundary'
    | 'Overdue'
    | 'DueToday'
    | 'AwaitingConfirmation'
    | 'ThisWeek'
    | 'Other'
  status: string
  statusLabel: string
  nextAction: string
  sourceCount: number
  detailType: string
  detailTargetId: string
  detailPath: string
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  isPermanentHumanBoundary: boolean
  requiresConfirmation: boolean
  occurredAt: string
}

export type MyWorkSummaryDto = {
  pendingCount: number
  dueTodayCount: number
  awaitingConfirmationCount: number
  permanentHumanBoundaryCount: number
}

export type FileLinkDto = {
  entityType: string
  entityId: string
}

export type FileAttachmentDto = {
  id: string
  departmentId: string
  originalFileName: string
  mimeType: string
  sizeBytes: number
  sha256: string
  virusScanStatus: 'NotConfigured' | 'PendingManualReview' | 'Clean' | 'Infected'
  sensitivity: 'Internal' | 'Sensitive' | 'Core'
  status: 'Active' | 'DeletionRequested'
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  sourceSystem: string
  currentVersionNumber: number
  previewKind: 'pdf' | 'image' | 'text' | 'unsupported'
  links: FileLinkDto[]
  createdAt: string
  updatedAt: string
  deletionRequestedBy: string | null
  deletionRequestedAt: string | null
  deletionReason: string | null
}

export type FileVersionDto = {
  id: string
  versionNumber: number
  originalFileName: string
  mimeType: string
  sizeBytes: number
  sha256: string
  virusScanStatus: FileAttachmentDto['virusScanStatus']
  sensitivity: FileAttachmentDto['sensitivity']
  createdBy: string
  createdAt: string
}

export type MyWorkDto = {
  displayName: string
  todayFocus: WorkItemViewDto | null
  pendingItems: WorkItemViewDto[]
  initiatedByMe: WorkItemViewDto[]
  participating: WorkItemViewDto[]
  needsAttention: WorkItemViewDto[]
  recentSourceEvidence: FileAttachmentDto[]
  summary: MyWorkSummaryDto
}

export type DepartmentWorkspaceDto = {
  department: DepartmentDto
  pendingTaskCount: number
  overdueTaskCount: number
  runningWorkflowCount: number
  businessObjectCount: number
  recentFileCount: number
  pendingTasks: WorkItemDto[]
  recentObjects: BusinessObjectDto[]
  recentWorkflows: WorkItemDto[]
  recentFiles: FileAttachmentDto[]
}

export type PermissionDto = {
  resource: string
  action: string
}

export type PagedResult<T> = {
  items: T[]
  page: number
  pageSize: number
  total: number
}

export type DashboardDto = {
  pendingTaskCount: number
  runningWorkflowCount: number
  businessObjectCount: number
  todayAuditCount: number
  pendingAgentConfirmationCount: number
  databaseStatus: string
}

export type ObjectFieldDto = {
  key: string
  displayName: string
  dataType: string
  isRequired: boolean
  maxLength: number
  sensitivity: 'Internal' | 'Sensitive' | 'Core'
  defaultValue: string | null
  options: string[]
  showInList: boolean
  filterable: boolean
  sortOrder: number
  code: string | null
}

export type ObjectTypeDto = {
  id: string
  code: string
  name: string
  version: number
  status: 'Draft' | 'Validated' | 'Published' | 'Retired' | 'Superseded'
  revision: number
  publishedAt: string | null
  retiredAt: string | null
  fields: ObjectFieldDto[]
}

export type CreateObjectTypeRequest = {
  code: string
  name: string
  fields: ObjectFieldDto[]
}

export type UpdateObjectTypeDraftRequest = {
  expectedRevision: number
  name: string
  fields: ObjectFieldDto[]
}

export type ObjectTypeValidationDto = {
  objectTypeId: string
  isValid: boolean
  errors: string[]
  version: number
  revision: number
  validatedAt: string
}

export type ObjectTypeVersionDto = {
  id: string
  objectTypeId: string
  version: number
  status: ObjectTypeDto['status']
  name: string
  fields: ObjectFieldDto[]
  validatedAt: string | null
  publishedAt: string | null
  retiredAt: string | null
  createdBy: string | null
  updatedBy: string | null
  createdAt: string
  updatedAt: string
}

export type BusinessObjectDto = {
  id: string
  objectTypeId: string
  objectCode: string
  status:
    | 'Draft'
    | 'Active'
    | 'Suspended'
    | 'Archived'
    | 'Blocked'
    | 'AwaitingAcceptance'
    | 'Completed'
    | 'Terminated'
  version: number
  fields: Record<string, string | null>
  createdAt: string
  updatedAt: string
  departmentId: string
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  sourceSystem: string
  title: string | null
  createdByUserId: string | null
}

export type BusinessObjectVersionDto = {
  id: string
  businessObjectId: string
  version: number
  status: BusinessObjectDto['status']
  fields: Record<string, string | null>
  actorId: string | null
  reason: string | null
  createdAt: string
}

export type BusinessObjectProjectLinkDto = {
  projectId: string
  relationId: string
  objectCode: string
  name: string
  status: ProjectStatus
  relationCode: string
  evidenceExcerpt: string | null
  createdAt: string
}

export type BusinessObjectFileLinkDto = {
  id: string
  originalFileName: string
  mimeType: string
  sizeBytes: number
  sensitivity: FileAttachmentDto['sensitivity']
  status: FileAttachmentDto['status']
  currentVersionNumber: number
  createdAt: string
}

export type BusinessObjectContextDto = {
  projects: BusinessObjectProjectLinkDto[]
  workflows: WorkflowInstanceDto[]
  files: BusinessObjectFileLinkDto[]
  auditEvents: AuditEventDto[]
}

export type ObjectFieldUserOptionDto = {
  id: string
  userName: string
  displayName: string
  departmentId: string
  departmentName: string
}

export type ProjectStatus =
  | 'Draft'
  | 'Active'
  | 'Blocked'
  | 'AwaitingAcceptance'
  | 'Completed'
  | 'Terminated'

export type ProjectDto = {
  id: string
  objectCode: string
  name: string
  status: ProjectStatus
  statusDisplayName: string
  version: number
  ownerUserId: string
  ownerDisplayName: string
  departmentId: string
  departmentName: string
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  plannedStartDate: string | null
  plannedEndDate: string | null
  objective: string
  currentStage: string
  confidentialityLevel: 'Internal' | 'Sensitive' | 'Core'
  progressPercent: number
  memberCount: number
  milestoneCount: number
  completedMilestoneCount: number
  overdueMilestoneCount: number
  projectTaskCount: number
  completedTaskCount: number
  overdueTaskCount: number
  createdAt: string
  updatedAt: string
}

export type ProjectMemberDto = {
  projectId: string
  userId: string
  displayName: string
  userName: string
  departmentId: string
  departmentName: string
  projectRole: 'Lead' | 'Manager' | 'Contributor' | 'Viewer'
  isLead: boolean
  isActive: boolean
  joinedAt: string
  removedAt: string | null
}

export type ProjectMilestoneDto = {
  id: string
  projectId: string
  title: string
  ownerUserId: string
  ownerDisplayName: string
  plannedDate: string
  status: 'Planned' | 'InProgress' | 'Completed' | 'AtRisk' | 'Cancelled'
  isOverdue: boolean
  completedAt: string | null
  version: number
  createdAt: string
  updatedAt: string
}

export type ProjectTaskDto = {
  id: string
  projectId: string
  eventInboxId: string | null
  assigneeId: string
  assigneeDisplayName: string
  departmentId: string
  title: string
  content: string
  dueAt: string | null
  priority: 'P0' | 'P1' | 'P2'
  status: 'Pending' | 'InProgress' | 'Completed' | 'Cancelled'
  version: number
  businessObjectId: string | null
  workflowInstanceId: string | null
  workflowTaskId: string | null
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  createdAt: string
  completedAt: string | null
}

export type ProjectRelationDto = {
  id: string
  projectId: string
  entityType: string
  entityId: string
  relationCode: string
  evidenceExcerpt: string | null
  createdAt: string
}

export type ProjectWorkflowDto = {
  instance: WorkflowInstanceDto
  definitionCode: string
  definitionName: string
  tasks: WorkflowTaskDto[]
  slaRisk: string
}

export type ProjectDetailDto = {
  project: ProjectDto
  members: ProjectMemberDto[]
  milestones: ProjectMilestoneDto[]
  tasks: ProjectTaskDto[]
  relations: ProjectRelationDto[]
  relatedBusinessObjects: BusinessObjectDto[]
  relatedWorkflows: ProjectWorkflowDto[]
  files: FileAttachmentDto[]
  minutes: MinutesSourceDto[]
  auditTimeline: AuditEventDto[]
}

export type ProjectCandidateUserDto = {
  id: string
  userName: string
  displayName: string
  departmentId: string
  departmentName: string
}

export type InboxSuggestionDto = {
  id: string
  eventInboxId: string
  agentRunId: string
  suggestedProjectId: string | null
  suggestedProjectName: string | null
  suggestedBusinessObjectId: string | null
  suggestedBusinessObjectCode: string | null
  suggestedAssigneeId: string | null
  suggestedAssigneeDisplayName: string | null
  suggestedWorkflowVersionId: string | null
  suggestedWorkflowName: string | null
  suggestedDueAt: string | null
  priority: 'P0' | 'P1' | 'P2'
  riskLevel: 'Low' | 'Medium' | 'High'
  requiresHumanBoundary: boolean
  boundaryReason: string | null
  missingInformation: string
  evidenceExcerpt: string
  status: 'Suggested' | 'HighRisk' | 'Confirmed' | 'Rejected'
  createdAt: string
}

export type InboxEventDto = {
  id: string
  departmentId: string
  departmentName: string
  sourceType:
    | 'ManualText'
    | 'ManualUpload'
    | 'MinutesSource'
    | 'MinutesActionItem'
    | 'WorkflowNotification'
    | 'Attachment'
  sourceId: string | null
  sourceTitle: string | null
  attachmentId: string | null
  title: string
  rawText: string
  status: 'Received' | 'AwaitingConfirmation' | 'Confirmed' | 'Rejected'
  submittedBy: string
  submittedByDisplayName: string
  analysisRunId: string | null
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  createdAt: string
  updatedAt: string
  suggestion: InboxSuggestionDto | null
}

export type InboxConfirmationDto = {
  event: InboxEventDto
  task: ProjectTaskDto | null
  project: ProjectDto
  workflow?: WorkflowInstanceDto | null
}

export type InboxTaskTraceDto = {
  task: ProjectTaskDto
  event: InboxEventDto
  project: ProjectDto
}

export type InboxSourceOptionDto = {
  type: 'MinutesSource' | 'MinutesActionItem' | 'WorkflowNotification' | 'Attachment'
  id: string
  title: string
  departmentId: string
  detail: string
  occurredAt: string
}

export type WorkflowDefinitionDto = {
  id: string
  versionId: string
  code: string
  name: string
  versionNumber: number
  objectTypeCode: string
}

export type WorkflowInstanceDto = {
  id: string
  workflowDefinitionId: string
  workflowVersionId: string
  businessObjectId: string
  status: 'Running' | 'Waiting' | 'Completed' | 'Returned' | 'Terminated'
  version: number
  currentNodeCode: string | null
  startedAt: string
  endedAt: string | null
  departmentId: string
  startedByUserId: string | null
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  sourceSystem: string
}

export type WorkflowTaskDto = {
  id: string
  workflowInstanceId: string
  nodeCode: string
  assigneeId: string
  status: 'Pending' | 'Approved' | 'Returned' | 'Cancelled'
  version: number
  decisionComment: string | null
  createdAt: string
  completedAt: string | null
  departmentId: string
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  dueAt: string | null
  slaStatus: string
  reminderSentAt: string | null
  escalatedAt: string | null
  decisionMode: string
  agentRunId: string | null
  assigneeStrategy: string
  businessRoleCode: string | null
  businessRoleVersion: number | null
  candidateUserIds: string[] | null
  originalAssigneeId: string | null
  backupAssigneeId: string | null
  transferredBy: string | null
  transferredAt: string | null
  transferReason: string | null
  nodeName: string | null
  formDefinitionJson: string
  formDataJson: string
  formSchemaVersion: string
  formDefinitionHash: string
}

export type WorkflowTimelineItemDto = {
  id: string
  kind: string
  title: string
  detail: string | null
  actorId: string
  occurredAt: string
}

export type AuditEventDto = {
  id: string
  actorId: string
  module: string
  action: string
  entityType: string
  entityId: string
  traceId: string
  outcome: 'Succeeded' | 'Denied' | 'Failed'
  occurredAt: string
  reason: string | null
}

export type AgentAutonomyLevel = 'A0' | 'A1' | 'A2' | 'A3' | 'A4'
export type AgentRiskLevel = 'Low' | 'Medium' | 'High'
export type AgentRunStatus =
  | 'AwaitingConfirmation'
  | 'Running'
  | 'Completed'
  | 'Stopped'
  | 'Failed'

export type AgentProfileDto = {
  id: string
  name: string
  autonomyLevel: 'A0' | 'A1' | 'A2' | 'A3' | 'A4'
  amountLimit: number
  riskLimit: 'Low' | 'Medium' | 'High'
  allowedTools: string[]
  isActive: boolean
  serviceUserId?: string
  code?: string
  departmentId?: string
  description?: string
}

export type AgentProfileLifecycleStatus = 'Draft' | 'Published' | 'Disabled' | 'Retired'
export type AgentProfileVersionStatus = 'Draft' | 'Validated' | 'Published' | 'Retired'

export type AgentProfileDetailDto = {
  id: string
  code: string
  name: string
  description: string
  serviceUserId: string
  serviceUserName: string
  departmentId: string
  departmentName: string
  businessOwnerUserId: string
  businessOwnerDisplayName: string
  technicalOwnerUserId: string
  technicalOwnerDisplayName: string
  autonomyLevel: 'A0' | 'A1' | 'A2' | 'A3' | 'A4'
  riskLimit: 'Low' | 'Medium' | 'High'
  amountLimit: number
  dataScopes: string[]
  modelScopes: string[]
  allowedTools: string[]
  requiredConfirmations: string[]
  stopConditions: string[]
  lifecycleStatus: AgentProfileLifecycleStatus
  currentPublishedVersionId: string | null
  currentPublishedVersionNumber: number | null
  version: number
  revision: number
  createdBy: string
  createdByDisplayName: string
  createdAt: string
  updatedBy: string
  updatedByDisplayName: string
  updatedAt: string
  lastRunAt?: string | null
  recentRunCount?: number
}

export type AgentProfileVersionDto = {
  id: string
  agentProfileId: string
  versionNumber: number
  revision: number
  status: AgentProfileVersionStatus
  code: string
  name: string
  description: string
  serviceUserId: string
  serviceUserName: string
  departmentId: string
  businessOwnerUserId: string
  technicalOwnerUserId: string
  autonomyLevel: 'A0' | 'A1' | 'A2' | 'A3' | 'A4'
  riskLimit: 'Low' | 'Medium' | 'High'
  amountLimit: number
  dataScopes: string[]
  modelScopes: string[]
  allowedTools: string[]
  requiredConfirmations: string[]
  stopConditions: string[]
  isHighRisk: boolean
  highRiskReasons: string[]
  validationErrors: string[]
  validationWarnings: string[]
  createdBy: string
  createdByDisplayName: string
  createdAt: string
  updatedBy: string
  updatedByDisplayName: string
  updatedAt: string
  validatedBy: string | null
  validatedAt: string | null
  publishedBy: string | null
  publishedAt: string | null
  retiredAt: string | null
}

export type AgentProfileValidationDto = {
  isValid: boolean
  errors: string[]
  warnings: string[]
  isHighRisk: boolean
  highRiskReasons: string[]
  validatedAt: string
}

export type AgentProfilePublishResultDto = {
  status: 'Published' | 'AwaitingApproval' | 'Rejected' | 'Validated'
  profile: AgentProfileDetailDto | null
  changeRequestId: string | null
  highRiskReasons: string[]
}

export type CreateAgentProfileDraftRequest = {
  code: string
  name: string
  description: string
  serviceUserId: string
  departmentId: string
  businessOwnerUserId: string
  technicalOwnerUserId: string
  autonomyLevel: AgentAutonomyLevel
  riskLimit: AgentRiskLevel
  amountLimit: number
  dataScopes: string[]
  modelScopes: string[]
  allowedTools: string[]
  requiredConfirmations: string[]
  stopConditions: string[]
}

export type CreateNextAgentProfileDraftRequest = {
  expectedVersion: number
  reason?: string | null
}

export type UpdateAgentProfileDraftRequest = {
  expectedRevision: number
  name: string
  description: string
  serviceUserId: string
  autonomyLevel: AgentAutonomyLevel
  riskLimit: AgentRiskLevel
  amountLimit: number
  dataScopes: string[]
  modelScopes: string[]
  allowedTools: string[]
  requiredConfirmations: string[]
  stopConditions: string[]
}

export type ValidateAgentProfileVersionRequest = {
  expectedRevision: number
}

export type PublishAgentProfileVersionRequest = {
  expectedRevision: number
  reason?: string | null
}

export type DisableAgentProfileRequest = {
  expectedVersion: number
  reason: string
}

export type ResumeAgentProfileRequest = {
  expectedVersion: number
  reason: string
}

export type RollbackAgentProfileRequest = {
  expectedVersion: number
  targetVersionId: string
  reason: string
}

export type ReviewAgentProfileChangeRequest = {
  approve: boolean
  comment: string
  expectedRevision: number
}

export type AgentProfileChangeRequestDto = {
  id: string
  agentProfileId: string
  agentProfileName: string
  agentProfileVersionId: string
  versionNumber: number
  status: string
  riskReason: string
  requestedBy: string
  requestedByDisplayName: string
  requestedAt: string
  reviewedBy: string | null
  reviewedAt: string | null
  reviewComment: string | null
}

export type AgentDepartmentOptionDto = {
  id: string
  code: string
  name: string
  isManager: boolean
}

export type AgentPersonOptionDto = {
  id: string
  userName: string
  displayName: string
  departmentIds: string[]
}

export type AgentServiceIdentityOptionDto = {
  id: string
  userName: string
  displayName: string
  boundAgentProfileId: string | null
  boundAgentProfileName: string | null
}

export type AgentCapabilityOptionDto = {
  code: string
  name: string
  description: string
  riskLevel: 'Low' | 'Medium' | 'High'
  requiresConfirmation: boolean
  requiredPermissions: string[]
  mcpScope: string | null
}

export type AgentIdentityOptionsDto = {
  organizationId: string
  departments: AgentDepartmentOptionDto[]
  people: AgentPersonOptionDto[]
  serviceIdentities: AgentServiceIdentityOptionDto[]
  capabilities: AgentCapabilityOptionDto[]
  permanentHumanBoundaries: string[]
  confirmationRules: string[]
  stopConditions: string[]
  modelScopes: string[]
}

export type AgentRunEvidenceDto = {
  id: string
  agentProfileId: string
  agentProfileName: string
  requestedBy: string
  requestedByDisplayName: string
  action: string
  amount: number
  riskLevel: 'Low' | 'Medium' | 'High'
  status: 'AwaitingConfirmation' | 'Running' | 'Completed' | 'Stopped' | 'Failed'
  createdAt: string
  departmentId: string
  environmentCode: DataEnvironmentCode
  businessObjectId: string | null
  businessObjectCode: string | null
  businessObjectTitle: string | null
  workflowInstanceId: string | null
  workflowName: string | null
  workflowStatus: string | null
  workflowTaskId: string | null
  workflowTaskNode: string | null
  workflowTaskStatus: string | null
  minutesSourceId: string | null
  minutesSourceTitle: string | null
  model: string | null
  gatewayCallId: string | null
  gatewayMode: string | null
  externalModelCalled: boolean
  confirmationId: string | null
  humanConfirmed: boolean
  originalAssigneeId: string | null
  originalAssigneeDisplayName: string | null
  proxyTriggerMode: string | null
  compensationStatus: string | null
}

export type AgentWorkflowNodeEvidenceDto = {
  policyId: string
  workflowDefinitionCode: string
  workflowDefinitionName: string
  workflowVersionId: string
  workflowVersionNumber: number
  nodeCode: string
  status: string
  allowAgentAssist: boolean
  allowAgentProxy: boolean
  permanentHumanBoundary: boolean
  publishedAt: string
}

export type AgentWorkflowTaskEvidenceDto = {
  taskId: string
  workflowInstanceId: string
  workflowDefinitionCode: string
  workflowDefinitionName: string
  nodeCode: string
  status: string
  decisionMode: string
  createdAt: string
}

export type AgentGatewayCallEvidenceDto = {
  id: string
  agentRunId: string
  model: string
  mode: string
  status: string
  externalModelCalled: boolean
  durationMs: number
  errorMessage: string | null
  createdAt: string
}

export type AgentProfileContextDto = {
  profile: AgentProfileDetailDto
  recentRuns: AgentRunEvidenceDto[]
  workflowNodes: AgentWorkflowNodeEvidenceDto[]
  workflowTasks: AgentWorkflowTaskEvidenceDto[]
  mcpClients: McpClientSummary[]
  mcpTokens: McpTokenSummary[]
  gatewayCalls: AgentGatewayCallEvidenceDto[]
  auditTimeline: AuditEventDto[]
  canViewMcpEvidence: boolean
  canViewAuditTimeline: boolean
}

export type AgentRunDto = {
  id: string
  agentProfileId: string
  requestedBy: string
  action: string
  amount: number
  riskLevel: 'Low' | 'Medium' | 'High'
  status: 'AwaitingConfirmation' | 'Running' | 'Completed' | 'Stopped' | 'Failed'
  confirmationId: string | null
  createdAt: string
  organizationId: string
  departmentId: string
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  subjectType: string | null
  businessObjectId: string | null
  workflowInstanceId: string | null
  workflowTaskId: string | null
  minutesSourceId: string | null
  model: string | null
  gatewayCallId: string | null
  gatewayMode: string | null
  externalModelCalled: boolean
  proxyTriggerMode: string | null
  proxyTriggeredAt: string | null
  originalAssigneeId: string | null
  compensationStatus: string | null
  proxyIdempotencyKey: string | null
}

export type MinutesSourceDto = {
  id: string
  departmentId: string
  attachmentId: string | null
  title: string
  meetingAt: string
  sourceType: 'ManualText' | 'ManualUpload'
  transcript: string
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  status: string
  analysisVersion: string
  analysisRunId: string | null
  createdAt: string
}

export type MinutesSummaryDto = {
  totalCount: number
  analyzedCount: number
  pendingActionItemCount: number
  createdThisWeekCount: number
}

export type MinutesActionItemDto = {
  id: string
  minutesSourceId: string
  speaker: string
  sourceTimestamp: string
  sourceExcerpt: string
  originalContent: string
  content: string
  suggestedAssigneeId: string | null
  assigneeId: string | null
  suggestedDueAt: string | null
  dueAt: string | null
  priority: string
  suggestedObjectTypeId: string | null
  objectTypeId: string | null
  suggestedWorkflowVersionId: string | null
  workflowVersionId: string | null
  missingInformation: string
  confidence: number
  analysisVersion: string
  status: 'Draft' | 'Confirmed'
  confirmedBy: string | null
  confirmedAt: string | null
  minutesTaskId: string | null
  assigneeDisplayName: string | null
  projectId: string | null
  projectName: string | null
  businessObjectId: string | null
  businessObjectCode: string | null
  businessObjectTitle: string | null
  workflowInstanceId: string | null
  workflowInstanceStatus: string | null
}

export type MinutesTaskDto = {
  id: string
  minutesSourceId: string
  actionItemId: string
  attachmentId: string | null
  departmentId: string
  assigneeId: string
  title: string
  content: string
  dueAt: string | null
  priority: string
  businessObjectId: string | null
  workflowInstanceId: string | null
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  status: string
  createdAt: string
  projectId: string | null
  projectName: string | null
}

export type MinutesConfirmationUserDto = {
  id: string
  userName: string
  displayName: string
  departmentId: string
  departmentName: string
}

export type AgentGatewayCallDto = {
  id: string
  agentRunId: string
  model: string
  mode: string
  gatewayCallId: string | null
  externalModelCalled: boolean
  inputDigest: string
  outputDigest: string | null
  status: string
  errorCode: string | null
  errorMessage: string | null
  durationMs: number
  createdAt: string
}

export type AgentExecutionResultDto = {
  run: AgentRunDto
  call: AgentGatewayCallDto
  actionItems: MinutesActionItemDto[]
}

export type WorkflowNodePolicyDto = {
  id: string
  workflowVersionId: string
  nodeCode: string
  versionNumber: number
  approverUserId: string | null
  backupApproverUserId: string | null
  slaMinutes: number | null
  reminderMinutes: number | null
  escalationMinutes: number | null
  timeoutStrategy: string
  allowAgentAssist: boolean
  allowAgentProxy: boolean
  agentProfileId: string | null
  agentMaxRisk: string
  agentMaxAmount: number
  agentMaxAutonomy: string
  agentProxyMode: 'RemindOnly' | 'HumanConfirmedProxy' | 'PreAuthorizedAutoProxy'
  autoProxyAfterMinutes: number | null
  permanentHumanBoundary: boolean
  requiredAttachmentCount: number
  precheckItems: string | null
  status: string
  publishedAt: string
  assigneeStrategy: string
  businessRoleCode: string | null
  allowTransfer: boolean
  transferTargetStrategy: string | null
}

export type WorkflowPolicyUserDto = {
  id: string
  userName: string
  displayName: string
}

export type WorkflowConfigurationCondition = {
  field: string
  operator: 'equals' | 'notEquals' | 'greaterThan' | 'greaterThanOrEqual'
  value: string
}

export type WorkflowConfigurationNode = {
  code: string
  name: string
  assigneeUserName: string | null
  nextNode: string | null
  completeOnApprove: boolean
  condition: WorkflowConfigurationCondition | null
  assigneeStrategy: string
  businessRoleCode: string | null
  agentProfileId: string | null
  businessDescription?: string | null
  slaMinutes?: number | null
  deliverables?: string[] | null
  relatedRoleCodes?: string[] | null
  requiredAttachmentCount?: number
  completionCondition?: string | null
  fields?: WorkflowFormField[]
  actions?: WorkflowNodeAction[]
  allowAgentAssist?: boolean
  allowAgentProxy?: boolean
  permanentHumanBoundary?: boolean
  aiReview?: WorkflowAiReview
  notification?: WorkflowNotificationPolicy
  formSchemaVersion?: string
}

export type WorkflowFormField = {
  key: string
  displayName: string
  dataType:
    | 'Text'
    | 'LongText'
    | 'Number'
    | 'Money'
    | 'Percent'
    | 'Select'
    | 'MultiSelect'
    | 'User'
    | 'MultiUser'
    | 'Department'
    | 'Date'
    | 'DateTime'
    | 'DateRange'
    | 'Attachment'
    | 'Boolean'
    | 'ReadOnlySystem'
    | 'BusinessObjectField'
  required: boolean
  defaultValue: string | null
  readOnly: boolean
  visibleRoleCodes: string[] | null
  writeRoleCodes: string[] | null
  options: string[] | null
  width: string
  sortOrder: number
  dataSource: 'TaskInput' | 'BusinessObjectReference' | 'EvidenceReference'
  boundObjectFieldCode: string | null
  sensitivity: 'Public' | 'Internal' | 'Sensitive' | 'Core'
  helpText: string | null
}

export type WorkflowNodeAction = {
  code: string
  name: string
  actionType: string
  confirmationText: string | null
}

export type WorkflowAiReview = {
  enabled: boolean
  registeredAction: string | null
  agentProfileId: string | null
  readableFieldKeys: string[] | null
  materialScopes: string[] | null
  outputMode: 'SuggestionOnly'
}

export type WorkflowNotificationPolicy = {
  enabled: boolean
  beforeDueMinutes: number | null
  escalateAfterMinutes: number | null
  escalateToRoleCode: string | null
}

export type WorkflowConfigurationValidation = {
  isValid: boolean
  errors: string[]
  warnings: string[]
  objectTypeCode: string
  nodeCount: number
  assigneeCount: number
  terminalNodeCount: number
  validatedAt: string
}

export type WorkflowConfigurationSimulation = {
  isValid: boolean
  errors: string[]
  warnings: string[]
  path: string[]
  riskLevel: 'Low' | 'Medium' | 'High'
  requiresPlatformApproval: boolean
}

export type WorkflowConfigurationDto = {
  id: string
  configurationKey: string
  name: string
  description: string | null
  configurationType: 'Workflow'
  versionNumber: number
  state: 'Draft' | 'Validated' | 'Published' | 'Retired'
  revision: number
  organizationId: string
  departmentId: string
  departmentName: string
  objectTypeCode: string
  startNode: string
  nodes: WorkflowConfigurationNode[]
  createdBy: string
  createdByDisplayName: string
  createdAt: string
  updatedBy: string | null
  updatedByDisplayName: string | null
  updatedAt: string
  publishedBy: string | null
  publishedByDisplayName: string | null
  publishedAt: string | null
  publishedWorkflowDefinitionId: string | null
  publishedWorkflowVersionId: string | null
  validation: WorkflowConfigurationValidation | null
  isDefault?: boolean
  applicableRoleCodes?: string[] | null
  riskLevel?: 'Low' | 'Medium' | 'High'
  requiresPlatformApproval?: boolean
  environmentCode?: DataEnvironmentCode
  isTestData?: boolean
}

export type WorkflowConfigurationWriteRequest = {
  name: string
  description: string | null
  departmentId: string
  objectTypeCode: string
  startNode: string
  nodes: WorkflowConfigurationNode[]
  isDefault?: boolean
  applicableRoleCodes?: string[] | null
  riskLevel?: 'Low' | 'Medium' | 'High' | null
}

export type WorkflowTaskForm = {
  taskId: string
  workflowInstanceId: string
  businessObjectId: string
  departmentId: string
  nodeCode: string
  nodeName: string
  businessDescription: string | null
  dueAt: string | null
  formSchemaVersion: string
  formDefinitionHash: string
  fields: WorkflowFormField[]
  actions: WorkflowNodeAction[]
  aiReview: WorkflowAiReview
  notificationPolicyJson: string
  permanentHumanBoundary: boolean
  formDataJson: string
}

export type WorkflowTaskAiReviewResult = {
  agentRunId: string
  gatewayCallId: string | null
  mode: string
  externalModelCalled: boolean
  requiresConfirmation: boolean
  registeredAction: string
  suggestion: string
  checkedFieldKeys: string[]
  createdAt: string
}

export type CreateWorkflowConfigurationRequest = WorkflowConfigurationWriteRequest & {
  configurationKey: string
}

export type UpdateWorkflowConfigurationRequest = WorkflowConfigurationWriteRequest & {
  expectedRevision: number
}

export type WorkflowVersionNodeDto = {
  code: string
  name: string
  defaultAssigneeUserName: string | null
  nextNodeCode: string | null
}

export type SaveWorkflowNodePolicyRequest = {
  approverUserId: string | null
  backupApproverUserId: string | null
  slaMinutes: number | null
  reminderMinutes: number | null
  escalationMinutes: number | null
  timeoutStrategy: string
  allowAgentAssist: boolean
  allowAgentProxy: boolean
  agentProfileId: string | null
  agentMaxRisk: string
  agentMaxAmount: number
  agentMaxAutonomy: string
  agentProxyMode: string
  autoProxyAfterMinutes: number | null
  permanentHumanBoundary: boolean
  requiredAttachmentCount: number
  precheckItems: string | null
  publish: boolean
  assigneeStrategy: string
  businessRoleCode: string | null
  allowTransfer: boolean
  transferTargetStrategy: string | null
}

export type ConnectorDto = {
  id: string
  code: string
  name: string
  adapterType: string
  connectionStatus: string
  authorityOwner: string
}

export type DepartmentTreeNodeDto = {
  id: string
  parentId: string | null
  code: string
  name: string
  hierarchyType: 'Company' | 'Center' | 'Department' | 'Team'
  path: string
  sortOrder: number
  managerUserId: string | null
  managerDisplayName: string | null
  version: number
  isActive: boolean
  children: DepartmentTreeNodeDto[]
}

export type IamUserListItemDto = {
  id: string
  userName: string
  displayName: string
  isServiceIdentity: boolean
  isActive: boolean
  invitationStatus: string
  primaryDepartmentId: string
  primaryDepartmentName: string
  version: number
  roleCodes: string[]
  dataScopeTypes: string[]
  createdAt: string
}

export type UserDepartmentMembershipDto = {
  departmentId: string
  departmentName: string
  isPrimary: boolean
  isManager: boolean
  isActive: boolean
}

export type UserDataScopeDto = {
  scopeType: string
  scopeReference: string
  displayName: string
}

export type PermissionCatalogItemDto = {
  id: string
  code: string
  resource: string
  action: string
  displayName: string
  riskLevel: 'Standard' | 'High'
}

export type RoleMemberDto = {
  userId: string
  userName: string
  displayName: string
  scopeType: string
  scopeReference: string
  scopeDisplayName: string
  isActive: boolean
  assignedAt: string
}

export type IamUserDetailDto = {
  user: IamUserListItemDto
  memberships: UserDepartmentMembershipDto[]
  dataScopes: UserDataScopeDto[]
  roles: RoleMemberDto[]
}

export type RoleDto = {
  id: string
  code: string
  name: string
  roleType: 'System' | 'Business' | 'AgentService'
  description: string | null
  isSystem: boolean
  isActive: boolean
  version: number
  memberCount: number
  permissionCount: number
  createdAt: string
  updatedAt: string
  permissionIds: string[] | null
}

export type AccessChangeRequestDto = {
  id: string
  requestType: string
  targetUserId: string | null
  targetRoleId: string | null
  riskReason: string
  status: string
  requestedBy: string
  requestedByDisplayName: string
  requestedAt: string
  reviewedBy: string | null
  reviewedAt: string | null
  reviewComment: string | null
}

export type IdentityAdministrationSnapshotDto = {
  organizationTree: DepartmentTreeNodeDto[]
  users: PagedResult<IamUserListItemDto>
  permissions: PermissionCatalogItemDto[]
  roles: RoleDto[]
  pendingAccessRequests: AccessChangeRequestDto[]
}

export type CreateIamDepartmentRequest = {
  code: string
  name: string
  parentId: string | null
  hierarchyType: 'Center' | 'Department' | 'Team'
  managerUserId: string | null
  sortOrder: number
}

export type UpdateIamDepartmentRequest = {
  expectedVersion: number
  name: string
  parentId: string | null
  hierarchyType: 'Center' | 'Department' | 'Team'
  managerUserId: string | null
  sortOrder: number
  isActive: boolean
}

export type InviteUserRequest = {
  userName: string
  displayName: string
  departmentId: string
  isPrimary: boolean
  isManager: boolean
}

export type SetUserDataScopesRequest = {
  scopes: Array<{ scopeType: string; scopeReference: string | null }>
  expectedVersion: number
}

export type SetUserMembershipsRequest = {
  memberships: Array<{
    departmentId: string
    isPrimary: boolean
    isManager: boolean
    isActive: boolean
  }>
  expectedVersion: number
}

export type CreateRoleRequest = {
  code: string
  name: string
  roleType: 'Business' | 'AgentService'
  description: string | null
}

export type SetRolePermissionsRequest = {
  permissionIds: string[]
  expectedRoleVersion: number
}

export type SetRoleMembersRequest = {
  members: Array<{
    userId: string
    scopeType: 'organization' | 'department'
    scopeReference: string
    isActive: boolean
  }>
  expectedRoleVersion: number
}

export type NavigationItemDto = {
  id: string
  itemKey: string
  name: string
  itemType: string
  route: string
  requiredResource: string | null
  requiredAction: string | null
  sortOrder: number
  isVisible: boolean
  isEnabled: boolean
  isSystem: boolean
  icon: string | null
  isFavorite: boolean
  isHidden: boolean
}

export type NavigationGroupDto = {
  workspaceId: string
  workspaceKey: string
  workspaceName: string
  workspaceType: string
  layer: 'Company' | 'Department' | 'Platform'
  departmentId: string | null
  departmentCode: string | null
  departmentName: string | null
  isDefault: boolean
  isDemoData: boolean
  isTestData: boolean
  sortOrder: number
  items: NavigationItemDto[]
}

export type CurrentNavigationDto = {
  generatedAt: string
  currentDepartmentId: string | null
  groups: NavigationGroupDto[]
}

export type NavigationWorkspaceDto = {
  id: string
  workspaceKey: string
  name: string
  workspaceType: string
  departmentId: string | null
  departmentName: string | null
  isSystem: boolean
  isDefault: boolean
  isDemoData: boolean
  environmentCode: DataEnvironmentCode
  isTestData: boolean
  status: 'Draft' | 'Published' | 'Retired'
  version: number
  draftVersionId: string
  draftVersionNumber: number
  revision: number
  editingVersionStatus: 'Draft' | 'Published' | 'Retired'
  items: NavigationItemDto[]
  createdAt: string
  updatedAt: string
  publishedAt: string | null
}

export type NavigationItemWriteRequest = {
  itemKey: string
  name: string
  itemType: string
  route: string
  requiredResource: string | null
  requiredAction: string | null
  sortOrder: number
  isVisible: boolean
  isEnabled: boolean
  isSystem: boolean
  icon: string | null
}

export type NavigationPreferenceItem = {
  workspaceKey: string
  itemKey: string
  isFavorite: boolean
  isHidden: boolean
  sortOrder: number
}

export type NavigationPreferenceDto = NavigationPreferenceItem & {
  updatedAt: string
}

export type CreateNavigationWorkspaceRequest = {
  workspaceKey: string
  name: string
  workspaceType: string
  departmentId: string | null
  template: string
  optionalEntries: string[] | null
}

export type UpdateNavigationWorkspaceRequest = {
  expectedVersion: number
  name: string
  template: string
  items: NavigationItemWriteRequest[]
}

export type WorkbenchCardItemDto = {
  id: string
  businessTitle: string
  contextTitle: string
  status: string
  riskLevel: string
  dueAt: string | null
  detailPath: string
}

export type WorkbenchResolvedCardDto = {
  id: string
  cardType:
    | 'work_items'
    | 'workflow_tasks'
    | 'project_portfolio'
    | 'project_tasks'
    | 'business_objects'
    | 'recent_files'
    | 'minutes'
    | 'overdue_risks'
    | 'team_pending'
    | 'ai_activity'
  title: string
  description: string | null
  sortOrder: number
  width: 'Third' | 'Half' | 'Full'
  state: 'Ready' | 'Empty' | 'Error' | 'Denied' | 'NotConnected'
  message: string
  totalCount: number
  items: WorkbenchCardItemDto[]
}

export type DepartmentWorkbenchDto = {
  matched: boolean
  templateId: string | null
  templateCode: string | null
  templateName: string | null
  templateVersionId: string | null
  templateVersionNumber: number | null
  departmentId: string
  departmentName: string
  audienceTitle: string
  pageTitle: string
  primaryQuestion: string
  roleNames: string[]
  fallbackMode: string
  fallbackReason: string | null
  lastUpdatedAt: string
  cards: WorkbenchResolvedCardDto[]
}

export type WorkbenchCardWriteRequest = {
  cardType: string
  title: string
  description: string | null
  sortOrder: number
  width: 'Third' | 'Half' | 'Full'
  dataSourceType: string
  filter: Record<string, string | null>
  requiredResource: string
  requiredAction: string
  isEnabled: boolean
  capabilityState: 'Connected' | 'NotConnected'
}

export type WorkbenchAssignmentWriteRequest = {
  assignmentType: 'User' | 'Role' | 'Department' | 'Organization'
  scopeValue: string
  priority: number
}

export type WorkbenchCardAdminDto = Omit<WorkbenchCardWriteRequest, 'filter'> & {
  id: string
  filter: Record<string, string | null>
}

export type WorkbenchAssignmentDto = WorkbenchAssignmentWriteRequest & { id: string }

export type WorkbenchTemplateVersionDto = {
  id: string
  versionNumber: number
  status: 'Draft' | 'Published' | 'Retired'
  revision: number
  validationJson: string | null
  impactAnalysisJson: string | null
  createdAt: string
  publishedAt: string | null
  cards: WorkbenchCardAdminDto[]
  assignments: WorkbenchAssignmentDto[]
}

export type WorkbenchTemplateDto = {
  id: string
  code: string
  name: string
  description: string
  audienceTitle: string
  primaryQuestion: string
  departmentId: string | null
  departmentName: string | null
  isSystem: boolean
  isDefault: boolean
  status: 'Draft' | 'Published' | 'Retired'
  sortOrder: number
  version: number
  createdAt: string
  updatedAt: string
  publishedAt: string | null
  draftVersion: WorkbenchTemplateVersionDto | null
  publishedVersion: WorkbenchTemplateVersionDto | null
}

export type CreateWorkbenchTemplateRequest = {
  code: string
  name: string
  description: string
  audienceTitle: string
  primaryQuestion: string
  departmentId: string | null
  sortOrder: number
  isDefault: boolean
}

export type UpdateWorkbenchTemplateDraftRequest = {
  expectedRevision: number
  name: string
  description: string
  audienceTitle: string
  primaryQuestion: string
  departmentId: string | null
  sortOrder: number
  isDefault: boolean
  cards: WorkbenchCardWriteRequest[]
  assignments: WorkbenchAssignmentWriteRequest[]
}

export type WorkbenchTemplateValidationDto = {
  isValid: boolean
  errors: string[]
  warnings: string[]
  visibleCardCount: number
  assignmentCount: number
  impactSummary: string
}

export type TransferWorkflowTaskRequest = {
  targetUserId: string
  expectedVersion: number
  reason: string
  idempotencyKey: string
}

export type EscalateWorkflowTaskRequest = {
  expectedVersion: number
  reason: string
  idempotencyKey: string
}

export type McpToolDescriptor = {
  name: string
  title: string
  description: string
  scope: string
  readOnly: boolean
  highRisk: boolean
  resourceType: string
  existingPermissions: string[]
  agentCapability?: string
}

export type McpClientSummary = {
  clientId: string
  clientName: string
  isActive: boolean
  activeTokenCount: number
  createdAt: string
  lastUsedAt: string | null
  agentProfileId?: string | null
  agentProfileName?: string | null
  agentProfileAvailable?: boolean | null
}

export type McpTokenSummary = {
  id: string
  clientId: string
  clientName: string
  userDisplayName: string
  kind: string
  scopes: string[]
  departmentIds: string[]
  createdAt: string
  expiresAt: string
  revokedAt: string | null
  lastUsedAt: string | null
  agentProfileId?: string | null
  agentProfileName?: string | null
  agentProfileAvailable?: boolean | null
  revokedBy?: string | null
  revocationReason?: string | null
}

export type McpHumanConfirmation = {
  id: string
  clientId: string
  toolName: string
  resourceType: string
  resourceId: string
  summary: string
  requestHash: string
  status: string
  expiresAt: string
  createdAt: string
}

export type McpConfirmationTechnicalInfo = {
  clientId: string
  resourceType: string
  resourceId: string
  requestHash: string
}

export type McpHumanConfirmationView = {
  id: string
  actionTitle: string
  status: 'Pending' | 'Confirmed' | 'Rejected' | 'Expired'
  summary: string
  resourceDisplayName: string
  projectName: string | null
  willExecute: string
  willNotExecute: string
  decisionComment: string | null
  expiresAt: string
  createdAt: string
  technical: McpConfirmationTechnicalInfo
}

export type McpInvocationSummary = {
  auditId: string
  toolOrResource: string
  operation: string
  clientId: string
  actorId: string
  traceId: string
  outcome: string
  code: string | null
  durationMs: number
  occurredAt: string
}

export type McpManagementSnapshot = {
  tools: McpToolDescriptor[]
  clients: McpClientSummary[]
  tokens: McpTokenSummary[]
  confirmations: McpHumanConfirmation[]
  recentInvocations: McpInvocationSummary[]
}

export type McpServiceTokenRequest = {
  serviceUserId: string
  clientName: string
  scopes: string[]
  departmentIds: string[]
  lifetimeHours: number
  agentProfileId?: string | null
}

export type McpServiceToken = {
  clientId: string
  tokenId: string
  accessToken: string
  tokenType: string
  scope: string
  serviceIdentityName: string
  expiresAt: string
}

export type ScenarioLifecycleStatus = 'Draft' | 'Published' | 'Disabled'
export type ScenarioVersionStatus = 'Draft' | 'Validated' | 'Published' | 'Retired'
export type ScenarioStartMode =
  | 'CreateBusinessObjectAndWorkflow'
  | 'ExistingBusinessObjectAndWorkflow'
  | 'EventInbox'
  | 'ProjectTask'

export type ScenarioStepDefinition = {
  sequence: number
  code: string
  name: string
  businessDescription: string
  responsibleRole: string
  action: string
  expectedResult: string
  evidence: string
  permanentHumanBoundary: boolean
}

export type ScenarioPreparationItem = {
  code: string
  name: string
  isRequired: boolean
  sourceType: string
  description: string
}

export type ScenarioAgentUsage = {
  agentProfileCode: string
  capability: string
  boundary: string
}

export type ScenarioDefinitionPayload = {
  problemSolved: string
  startMode: ScenarioStartMode
  triggerDescription: string
  applicableDepartmentCodes: string[]
  participantRoles: string[]
  objectTypeCodes: string[]
  workflowDefinitionCode: string | null
  workflowVersionNumber: number | null
  steps: ScenarioStepDefinition[]
  preparationItems: ScenarioPreparationItem[]
  supportedFileSources: string[]
  supportedMinutesSources: string[]
  allowedAgents: ScenarioAgentUsage[]
  permanentHumanBoundaries: string[]
  permissionRequirements: string[]
  completionEvidence: string[]
}

export type ScenarioObjectTypeReference = {
  id: string
  code: string
  name: string
  version: number
  status: string
}

export type ScenarioWorkflowReference = {
  definitionId: string
  definitionCode: string
  definitionName: string
  versionId: string
  versionNumber: number
  status: string
}

export type ScenarioAgentReference = {
  id: string
  code: string
  name: string
  versionNumber: number
  lifecycleStatus: string
  allowedTools: string[]
}

export type ScenarioReferenceSnapshot = {
  objectTypes: ScenarioObjectTypeReference[]
  workflow: ScenarioWorkflowReference | null
  agents: ScenarioAgentReference[]
  frozenAt: string
}

export type ScenarioValidation = {
  isValid: boolean
  errors: string[]
  warnings: string[]
  references: ScenarioReferenceSnapshot | null
  validatedAt: string
}

export type ScenarioVersion = {
  id: string
  scenarioId: string
  versionNumber: number
  status: ScenarioVersionStatus
  revision: number
  payload: ScenarioDefinitionPayload
  references: ScenarioReferenceSnapshot | null
  validation: ScenarioValidation | null
  changeSummary: string | null
  createdBy: string
  createdByDisplayName: string
  createdAt: string
  updatedBy: string | null
  updatedByDisplayName: string | null
  updatedAt: string
  publishedBy: string | null
  publishedByDisplayName: string | null
  publishedAt: string | null
  retiredBy: string | null
  retiredByDisplayName: string | null
  retiredAt: string | null
}

export type Scenario = {
  id: string
  code: string
  name: string
  description: string
  ownerDepartmentId: string
  ownerDepartmentName: string
  lifecycleStatus: ScenarioLifecycleStatus
  version: number
  revision: number
  createdBy: string
  createdByDisplayName: string
  createdAt: string
  updatedBy: string | null
  updatedByDisplayName: string | null
  updatedAt: string
  disabledBy: string | null
  disabledByDisplayName: string | null
  disabledAt: string | null
  disabledReason: string | null
  currentPublishedVersion: ScenarioVersion | null
  draftVersion: ScenarioVersion | null
  canManage: boolean
  pendingApprovalRequestId: string | null
  pendingApprovalStatus: string | null
}

export type ScenarioStartInput = {
  departmentId?: string | null
  businessObjectId?: string | null
  objectCode?: string | null
  title?: string | null
  fields?: Record<string, string | null> | null
  projectId?: string | null
  assigneeId?: string | null
  taskTitle?: string | null
  taskContent?: string | null
  dueAt?: string | null
  priority?: string | null
  content?: string | null
  attachmentIds?: string[] | null
  minutesSourceIds?: string[] | null
  requestAgentSuggestion?: boolean
  agentProfileId?: string | null
  comment?: string | null
}

export type ScenarioPreparationCheck = {
  code: string
  name: string
  sourceType: string
  isRequired: boolean
  isProvided: boolean
  isValid: boolean
  message: string
}

export type ScenarioPreflight = {
  canStart: boolean
  scenarioId: string
  scenarioCode: string
  scenarioVersionNumber: number
  startMode: ScenarioStartMode
  departmentId: string
  departmentName: string
  blockingReasons: string[]
  warnings: string[]
  preparationChecks: ScenarioPreparationCheck[]
  references: ScenarioReferenceSnapshot
  checkedAt: string
}

export type ScenarioBusinessLink = {
  type: string
  id: string
  name: string
  role: string
  detailPath: string
}

export type ScenarioLaunch = {
  id: string
  scenarioId: string
  scenarioCode: string
  scenarioName: string
  scenarioVersionId: string
  scenarioVersionNumber: number
  departmentId: string
  departmentName: string
  startedBy: string
  startedByDisplayName: string
  startedAt: string
  businessObjectId: string | null
  businessObjectCode: string | null
  workflowInstanceId: string | null
  workflowTaskId: string | null
  projectId: string | null
  projectTaskId: string | null
  eventInboxId: string | null
  agentRunId: string | null
  attachmentIds: string[]
  minutesSourceIds: string[]
  businessLinks: ScenarioBusinessLink[]
  permanentHumanBoundaries: string[]
}

export type ScenarioLaunchSummary = {
  id: string
  scenarioId: string
  scenarioCode: string
  scenarioName: string
  departmentId: string
  departmentName: string
  environmentCode: DataEnvironmentCode
  isDemo: boolean
  startedAt: string
  currentStep: string
  waitingFor: string
  dueAt: string | null
  uploadedMaterialCount: number
  missingMaterials: string[]
  workflowStatus: string
  taskStatus: string
  projectName: string
  agentSuggestion: string
  nextAction: string
  isCompleted: boolean
  businessLinks: ScenarioBusinessLink[]
  evidencePath: string
}

export type ScenarioApprovalRequest = {
  id: string
  scenarioId: string
  scenarioCode: string
  scenarioName: string
  versionNumber: number
  riskReason: string
  status: string
  requestedBy: string
  requestedByDisplayName: string
  requestedAt: string
  reviewedBy: string | null
  reviewedByDisplayName: string | null
  reviewedAt: string | null
  reviewComment: string | null
}

export type CreateScenarioDraftRequest = {
  code: string
  name: string
  description: string
  ownerDepartmentId: string
  payload: ScenarioDefinitionPayload
}

export type UpdateScenarioDraftRequest = {
  expectedRevision: number
  name: string
  description: string
  ownerDepartmentId: string
  payload: ScenarioDefinitionPayload
}

export type CreateNextScenarioVersionDraftRequest = {
  expectedVersion: number
  changeSummary: string
}

export type ScenarioActionRequest = {
  expectedVersion: number
  comment?: string | null
}

export type RollbackScenarioRequest = {
  expectedVersion: number
  targetVersionNumber: number
  reason: string
}

export type ReviewScenarioPublishRequest = {
  expectedVersion: number
  approve: boolean
  comment: string
}
