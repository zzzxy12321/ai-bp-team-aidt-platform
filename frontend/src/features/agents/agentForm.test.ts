import { reactive } from 'vue'
import { describe, expect, it } from 'vitest'
import type { AgentIdentityOptionsDto, AgentProfileDetailDto } from '@/api/types/contracts'
import {
  copyAgentForm,
  cloneAgentForm,
  emptyAgentForm,
  formFromProfile,
} from './agentForm'

const options: AgentIdentityOptionsDto = {
  organizationId: 'org-1',
  departments: [
    { id: 'dept-1', code: 'RND', name: '研发部', isManager: true },
  ],
  people: [
    { id: 'user-1', userName: 'demo.pm', displayName: '项目经理', departmentIds: ['dept-1'] },
  ],
  serviceIdentities: [
    {
      id: 'service-1',
      userName: 'agent.lowrisk',
      displayName: '低风险数字员工',
      boundAgentProfileId: null,
      boundAgentProfileName: null,
    },
  ],
  capabilities: [],
  permanentHumanBoundaries: ['payment'],
  confirmationRules: ['payment', 'low-confidence'],
  stopConditions: ['manual-stop'],
  modelScopes: ['aidt-rule-sim'],
}

const profile: AgentProfileDetailDto = {
  id: 'agent-1',
  code: 'AGENT-DEMO',
  name: '业务协同数字员工',
  description: '帮助整理事项',
  serviceUserId: 'service-1',
  serviceUserName: '低风险数字员工',
  departmentId: 'dept-1',
  departmentName: '研发部',
  businessOwnerUserId: 'user-1',
  businessOwnerDisplayName: '项目经理',
  technicalOwnerUserId: 'user-1',
  technicalOwnerDisplayName: '项目经理',
  autonomyLevel: 'A2',
  riskLimit: 'Low',
  amountLimit: 0,
  dataScopes: ['department:dept-1'],
  modelScopes: ['aidt-rule-sim'],
  allowedTools: ['workflow.read'],
  requiredConfirmations: ['payment', 'low-confidence'],
  stopConditions: ['manual-stop'],
  lifecycleStatus: 'Published',
  currentPublishedVersionId: 'version-1',
  currentPublishedVersionNumber: 1,
  version: 1,
  revision: 1,
  createdBy: 'user-1',
  createdByDisplayName: '项目经理',
  createdAt: '2026-11-03T00:00:00Z',
  updatedBy: 'user-1',
  updatedByDisplayName: '项目经理',
  updatedAt: '2026-11-03T00:00:00Z',
}

describe('agentForm', () => {
  it('creates a safe low-risk default draft in the manager department', () => {
    const form = emptyAgentForm(options)
    expect(form.departmentId).toBe('dept-1')
    expect(form.autonomyLevel).toBe('A2')
    expect(form.riskLimit).toBe('Low')
    expect(form.amountLimit).toBe(0)
    expect(form.dataScopes).toContain('department:dept-1')
    expect(form.stopConditions).toContain('manual-stop')
  })

  it('keeps optional confirmation rules but omits permanently enforced ones', () => {
    const form = formFromProfile(profile)
    expect(form.requiredConfirmations).toEqual(['low-confidence'])
    expect(form.allowedTools).toEqual(['workflow.read'])
  })

  it('copies capability scope while clearing service identity and changing code', () => {
    const form = copyAgentForm(profile)
    expect(form.serviceUserId).toBe('')
    expect(form.code).toContain('COPY')
    expect(form.allowedTools).toEqual(['workflow.read'])
  })

  it('clones a reactive form without browser structuredClone proxy failures', () => {
    const source = reactive(emptyAgentForm(options))
    const clone = cloneAgentForm(source)
    clone.dataScopes.push('organization:org-1')

    expect(clone.modelScopes).toEqual(['aidt-rule-sim'])
    expect(source.dataScopes).toEqual(['department:dept-1'])
  })
})
