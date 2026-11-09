import { readFile } from 'node:fs/promises'
import { fileURLToPath } from 'node:url'
import path from 'node:path'

const scriptDir = path.dirname(fileURLToPath(import.meta.url))
const contractPath = path.resolve(scriptDir, '../../docs/contracts/openapi/v1.json')
const content = await readFile(contractPath, 'utf8')

let document
try {
  document = JSON.parse(content)
} catch (error) {
  console.error(`OpenAPI JSON 解析失败: ${error.message}`)
  process.exit(1)
}

const errors = []
const requiredPaths = [
  '/health/live',
  '/health/ready',
  '/api/v1/platform/capabilities',
  '/api/v1/dashboard',
  '/api/v1/my-work',
  '/api/v1/departments',
  '/api/v1/departments/{id}/members',
  '/api/v1/departments/{id}/workspace',
  '/api/v1/departments/{id}/workbench',
  '/api/v1/files',
  '/api/v1/files/{id}',
  '/api/v1/files/{id}/versions',
  '/api/v1/files/{id}/download',
  '/api/v1/files/{id}/preview',
  '/api/v1/files/{id}/actions/delete-request',
  '/api/v1/minutes',
  '/api/v1/minutes/summary',
  '/api/v1/minutes/{id}',
  '/api/v1/minutes/{id}/actions/analyze',
  '/api/v1/minutes/{id}/action-items',
  '/api/v1/minutes/{id}/action-items/{actionItemId}',
  '/api/v1/minutes/action-items/{actionItemId}/actions/confirm',
  '/api/v1/minutes/confirmation-users',
  '/api/v1/minutes/by-task/{taskId}',
  '/api/v1/workflow-versions/{id}/node-policies',
  '/api/v1/workflow-versions/{id}/nodes/{nodeCode}/policy',
  '/api/v1/workflow-sla/actions/run',
  '/api/v1/workflow-notifications',
  '/api/v1/workflow-tasks/{id}/actions/agent-proxy',
  '/api/v1/agents/runs/{id}/actions/execute',
  '/api/v1/agents/runs/{id}/gateway-calls',
  '/api/v1/agents/runs/{id}/actions/confirm-proxy',
  '/api/v1/projects',
  '/api/v1/projects/{id}',
  '/api/v1/projects/{id}/actions/change-status',
  '/api/v1/projects/{id}/member-candidates',
  '/api/v1/projects/{id}/members',
  '/api/v1/projects/{id}/members/{userId}',
  '/api/v1/projects/{id}/milestones',
  '/api/v1/projects/{id}/milestones/{milestoneId}',
  '/api/v1/projects/{id}/relations',
  '/api/v1/projects/{id}/tasks',
  '/api/v1/project-tasks/{id}/actions/complete',
  '/api/v1/project-tasks/{id}/trace',
  '/api/v1/inbox',
  '/api/v1/inbox/upload',
  '/api/v1/inbox/sources',
  '/api/v1/inbox/{id}',
  '/api/v1/inbox/{id}/actions/analyze',
  '/api/v1/inbox/{id}/actions/confirm',
  '/api/v1/inbox/{id}/actions/confirm-and-start-flow',
  '/api/v1/auth/csrf',
  '/api/v1/auth/demo-login',
  '/api/v1/auth/session',
  '/api/v1/auth/logout',
  '/api/v1/permissions/me',
  '/api/v1/object-types',
  '/api/v1/business-objects',
  '/api/v1/workflow-definitions',
  '/api/v1/workflow-instances',
  '/api/v1/workflow-instances/{id}/timeline',
  '/api/v1/workflow-tasks',
  '/api/v1/audit-events',
  '/api/v1/agents',
  '/api/v1/agents/available',
  '/api/v1/agents/options',
  '/api/v1/agents/manage',
  '/api/v1/agents/{id}',
  '/api/v1/agents/{id}/context',
  '/api/v1/agents/drafts',
  '/api/v1/agents/{id}/drafts',
  '/api/v1/agents/{id}/drafts/{versionId}',
  '/api/v1/agents/{id}/versions/{versionId}/actions/validate',
  '/api/v1/agents/{id}/versions/{versionId}/actions/publish',
  '/api/v1/agents/{id}/actions/disable',
  '/api/v1/agents/{id}/actions/resume',
  '/api/v1/agents/{id}/versions',
  '/api/v1/agents/{id}/actions/rollback',
  '/api/v1/agents/change-requests',
  '/api/v1/agents/change-requests/{requestId}/actions/review',
  '/api/v1/agents/runs',
  '/api/v1/agents/runs/{id}/actions/confirm',
  '/api/v1/agents/runs/{id}/actions/stop',
  '/api/v1/connectors',
  '/api/v1/configurations',
  '/api/v1/configurations/member-candidates',
  '/api/v1/configurations/{id}',
  '/api/v1/configurations/{id}/actions/validate',
  '/api/v1/configurations/{id}/actions/publish',
  '/api/v1/configurations/{id}/actions/rollback',
  '/api/v1/configurations/{id}/versions',
  '/api/v1/configurations/{id}/actions/disable',
  '/api/v1/configurations/{id}/actions/restore',
  '/api/v1/configurations/{id}/actions/simulate',
  '/api/v1/workflow-tasks/{id}/form',
  '/api/v1/workflow-tasks/{id}/form-draft',
  '/api/v1/workflow-tasks/{id}/actions/ai-review',
  '/api/v1/iam/snapshot',
  '/api/v1/iam/organization-tree',
  '/api/v1/iam/departments',
  '/api/v1/iam/departments/{id}',
  '/api/v1/iam/departments/{id}/actions/status-preview',
  '/api/v1/iam/departments/{id}/actions/deactivate',
  '/api/v1/iam/departments/{id}/actions/reactivate',
  '/api/v1/iam/users',
  '/api/v1/iam/users/import',
  '/api/v1/iam/users/{id}',
  '/api/v1/iam/users/{id}/memberships',
  '/api/v1/iam/users/{id}/data-scopes',
  '/api/v1/iam/users/{id}/actions/deactivate',
  '/api/v1/iam/users/{id}/actions/reactivate',
  '/api/v1/iam/permissions',
  '/api/v1/iam/roles',
  '/api/v1/iam/roles/{id}',
  '/api/v1/iam/roles/{id}/actions/copy',
  '/api/v1/iam/roles/{id}/permissions',
  '/api/v1/iam/roles/{id}/members',
  '/api/v1/iam/roles/{id}/actions/deactivate',
  '/api/v1/iam/roles/{id}/actions/reactivate',
  '/api/v1/iam/access-requests',
  '/api/v1/iam/access-requests/{id}/actions/approve',
  '/api/v1/navigation/current',
  '/api/v1/navigation/workspaces',
  '/api/v1/navigation/workspaces/{id}',
  '/api/v1/navigation/workspaces/{id}/draft',
  '/api/v1/navigation/workspaces/{id}/actions/publish',
  '/api/v1/navigation/workspaces/{id}/actions/retire',
  '/api/v1/navigation/workspaces/{id}/actions/restore',
  '/api/v1/navigation/preferences',
  '/api/v1/workbench-templates',
  '/api/v1/workbench-templates/{id}',
  '/api/v1/workbench-templates/{id}/actions/copy',
  '/api/v1/workbench-templates/{id}/draft',
  '/api/v1/workbench-templates/{id}/actions/validate',
  '/api/v1/workbench-templates/{id}/actions/publish',
  '/api/v1/workbench-templates/{id}/actions/retire',
  '/api/v1/workbench-templates/{id}/actions/restore',
  '/api/v1/workbench-templates/{id}/versions',
  '/api/v1/workflow-roles',
  '/api/v1/workflow-roles/{id}/members',
  '/api/v1/workflow-tasks/{id}/actions/transfer',
  '/api/v1/workflow-tasks/{id}/actions/escalate',
  '/api/v1/mcp/snapshot',
  '/api/v1/mcp/tools',
  '/api/v1/mcp/confirmations/me',
  '/api/v1/mcp/confirmation-views/me',
  '/api/v1/mcp/confirmations/{id}/actions/confirm',
  '/api/v1/mcp/confirmations/{id}/actions/reject',
  '/api/v1/mcp/clients/{clientId}/actions/revoke',
  '/api/v1/mcp/tokens/{id}/actions/revoke',
  '/api/v1/mcp/service-tokens',
  '/api/v1/scenarios',
  '/api/v1/scenarios/manage',
  '/api/v1/scenarios/drafts',
  '/api/v1/scenarios/{id}',
  '/api/v1/scenarios/{id}/versions/{versionId}/actions/validate',
  '/api/v1/scenarios/{id}/versions/{versionId}/actions/publish',
  '/api/v1/scenarios/{id}/actions/preflight',
  '/api/v1/scenarios/{id}/actions/start',
  '/api/v1/scenarios/launches',
  '/api/v1/scenarios/launches/{id}',
  '/api/v1/scenarios/approval-requests',
]

if (document.openapi !== '3.1.0') {
  errors.push('openapi 必须为 3.1.0')
}

for (const requiredPath of requiredPaths) {
  if (!document.paths?.[requiredPath]) {
    errors.push(`缺少路径 ${requiredPath}`)
  }
}

const operationIds = new Set()
for (const [route, methods] of Object.entries(document.paths ?? {})) {
  for (const [method, operation] of Object.entries(methods)) {
    if (method === 'parameters') continue
    if (!operation.operationId) {
      errors.push(`${method.toUpperCase()} ${route} 缺少 operationId`)
    } else if (operationIds.has(operation.operationId)) {
      errors.push(`operationId 重复: ${operation.operationId}`)
    } else {
      operationIds.add(operation.operationId)
    }
    if (['post', 'put', 'patch'].includes(method)) {
      const responses = operation.responses ?? {}
      if (!responses['400'] && !responses['409']) {
        errors.push(`${method.toUpperCase()} ${route} 缺少验证或冲突响应`)
      }
    }
  }
}

for (const requiredSchema of [
  'ApiEnvelope',
  'ErrorEnvelope',
  'BusinessObject',
  'Department',
  'MyWork',
  'DepartmentWorkspace',
  'DepartmentWorkbench',
  'WorkbenchTemplate',
  'WorkbenchTemplateValidation',
  'FileAttachment',
  'MinutesSource',
  'MinutesSummary',
  'MinutesActionItem',
  'MinutesTask',
  'WorkflowNodePolicy',
  'AgentGatewayCall',
  'WorkflowInstance',
  'WorkflowTask',
  'WorkflowFormField',
  'WorkflowNodeAction',
  'WorkflowAiReview',
  'WorkflowNotificationPolicy',
  'WorkflowTaskForm',
  'AuditEvent',
  'AgentRun',
  'AgentIdentityOptions',
  'AgentProfileContext',
  'AgentProfileDetail',
  'AgentProfileVersion',
  'AgentProfileValidation',
  'AgentProfilePublishResult',
  'AgentProfileChangeRequest',
  'Project',
  'ProjectDetail',
  'ProjectMember',
  'ProjectMilestone',
  'ProjectTask',
  'InboxEvent',
  'InboxSuggestion',
  'Scenario',
  'ScenarioLaunch',
  'ScenarioLaunchSummary',
]) {
  if (!document.components?.schemas?.[requiredSchema]) {
    errors.push(`缺少 Schema ${requiredSchema}`)
  }
}

if (errors.length > 0) {
  console.error(errors.join('\n'))
  process.exit(1)
}

console.log(`OpenAPI 静态校验通过：${Object.keys(document.paths).length} 个路径，${operationIds.size} 个 operationId。`)
