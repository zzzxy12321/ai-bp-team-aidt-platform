import type {
  AgentAutonomyLevel,
  AgentIdentityOptionsDto,
  AgentProfileDetailDto,
  AgentProfileVersionDto,
  AgentRiskLevel,
} from '@/api/types/contracts'

export type AgentIdentityFormValue = {
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

export function emptyAgentForm(options: AgentIdentityOptionsDto | null): AgentIdentityFormValue {
  const firstDepartment = options?.departments.find((item) => item.isManager) ?? options?.departments[0]
  const firstOwner = options?.people[0]?.id ?? ''
  return {
    code: '',
    name: '',
    description: '',
    serviceUserId: '',
    departmentId: firstDepartment?.id ?? '',
    businessOwnerUserId: firstOwner,
    technicalOwnerUserId: firstOwner,
    autonomyLevel: 'A2',
    riskLimit: 'Low',
    amountLimit: 0,
    dataScopes: firstDepartment ? [`department:${firstDepartment.id}`] : [],
    modelScopes: ['aidt-rule-sim'],
    allowedTools: [],
    requiredConfirmations: [],
    stopConditions: ['manual-stop', 'risk-limit', 'amount-limit', 'unauthorized-tool'],
  }
}

export function cloneAgentForm(value: AgentIdentityFormValue): AgentIdentityFormValue {
  return {
    code: value.code,
    name: value.name,
    description: value.description,
    serviceUserId: value.serviceUserId,
    departmentId: value.departmentId,
    businessOwnerUserId: value.businessOwnerUserId,
    technicalOwnerUserId: value.technicalOwnerUserId,
    autonomyLevel: value.autonomyLevel,
    riskLimit: value.riskLimit,
    amountLimit: value.amountLimit,
    dataScopes: [...value.dataScopes],
    modelScopes: [...value.modelScopes],
    allowedTools: [...value.allowedTools],
    requiredConfirmations: [...value.requiredConfirmations],
    stopConditions: [...value.stopConditions],
  }
}

export function formFromProfile(profile: AgentProfileDetailDto): AgentIdentityFormValue {
  return {
    code: profile.code,
    name: profile.name,
    description: profile.description,
    serviceUserId: profile.serviceUserId,
    departmentId: profile.departmentId,
    businessOwnerUserId: profile.businessOwnerUserId,
    technicalOwnerUserId: profile.technicalOwnerUserId,
    autonomyLevel: profile.autonomyLevel,
    riskLimit: profile.riskLimit,
    amountLimit: profile.amountLimit,
    dataScopes: [...profile.dataScopes],
    modelScopes: [...profile.modelScopes],
    allowedTools: [...profile.allowedTools],
    requiredConfirmations: profile.requiredConfirmations.filter(
      (item) => !['payment', 'quality-release', 'formula-change', 'equipment-control',
        'safety-interlock', 'contract-commitment', 'customer-compensation'].includes(item),
    ),
    stopConditions: [...profile.stopConditions],
  }
}

export function formFromVersion(version: AgentProfileVersionDto): AgentIdentityFormValue {
  return {
    code: version.code,
    name: version.name,
    description: version.description,
    serviceUserId: version.serviceUserId,
    departmentId: version.departmentId,
    businessOwnerUserId: version.businessOwnerUserId,
    technicalOwnerUserId: version.technicalOwnerUserId,
    autonomyLevel: version.autonomyLevel,
    riskLimit: version.riskLimit,
    amountLimit: version.amountLimit,
    dataScopes: [...version.dataScopes],
    modelScopes: [...version.modelScopes],
    allowedTools: [...version.allowedTools],
    requiredConfirmations: version.requiredConfirmations.filter(
      (item) => !['payment', 'quality-release', 'formula-change', 'equipment-control',
        'safety-interlock', 'contract-commitment', 'customer-compensation'].includes(item),
    ),
    stopConditions: [...version.stopConditions],
  }
}

export function copyAgentForm(profile: AgentProfileDetailDto): AgentIdentityFormValue {
  const source = formFromProfile(profile)
  return {
    ...source,
    code: `${source.code}-COPY`.slice(0, 64).toUpperCase(),
    name: `${source.name}（副本）`.slice(0, 128),
    serviceUserId: '',
  }
}

export function organizationScope(options: AgentIdentityOptionsDto | null): string | null {
  return options?.organizationId ? `organization:${options.organizationId}` : null
}
