import type {
  AgentAutonomyLevel,
  AgentProfileLifecycleStatus,
  AgentProfileVersionStatus,
  AgentRiskLevel,
  AgentRunStatus,
} from '@/api/types/contracts'

export type BadgeTone = 'success' | 'warning' | 'danger' | 'info' | 'neutral'

export const autonomyLabels: Record<AgentAutonomyLevel, { action: string; boundary: string }> = {
  A0: { action: '只生成建议', boundary: '所有决定由人做出' },
  A1: { action: '生成草稿', boundary: '提交或发送前必须人工确认' },
  A2: { action: '执行后通知', boundary: '仅限低风险、可逆动作' },
  A3: { action: '限额执行', boundary: '超限、异常或高风险转人工' },
  A4: { action: '低风险自动闭环', boundary: '必须可监控、补偿和停止' },
}

export const riskLabels: Record<AgentRiskLevel, string> = {
  Low: '低风险',
  Medium: '中风险',
  High: '高风险',
}

export const lifecyclePresentation: Record<
  AgentProfileLifecycleStatus,
  { label: string; tone: BadgeTone; hint: string }
> = {
  Draft: { label: '草稿', tone: 'warning', hint: '尚未发布，不能用于新运行或流程节点。' },
  Published: { label: '有效', tone: 'success', hint: '可用于新运行、流程配置和 MCP 服务令牌。' },
  Disabled: { label: '已停用', tone: 'danger', hint: '新运行和令牌签发已停止，历史记录保留。' },
  Retired: { label: '已退役', tone: 'neutral', hint: '仅保留历史追溯，不可重新用于新业务。' },
}

export const versionStatusPresentation: Record<
  AgentProfileVersionStatus,
  { label: string; tone: BadgeTone }
> = {
  Draft: { label: '草稿', tone: 'warning' },
  Validated: { label: '已校验', tone: 'info' },
  Published: { label: '当前发布', tone: 'success' },
  Retired: { label: '历史版本', tone: 'neutral' },
}

export const runStatusPresentation: Record<
  AgentRunStatus,
  { label: string; tone: BadgeTone; hint: string }
> = {
  AwaitingConfirmation: {
    label: '等待真人确认',
    tone: 'warning',
    hint: '数字员工不能代替真人完成该确认。',
  },
  Running: { label: '正在运行', tone: 'success', hint: '运行中的动作仍受上限和停止开关约束。' },
  Completed: { label: '已完成', tone: 'success', hint: '运行结果已形成证据链。' },
  Stopped: { label: '已停止', tone: 'danger', hint: '已执行紧急停止，历史证据继续保留。' },
  Failed: { label: '已阻断或失败', tone: 'danger', hint: '查看原因并按业务规则转人工处理。' },
}

export const actionLabels: Record<string, string> = {
  Read: '读取资料',
  Draft: '生成草稿',
  Submit: '提交建议',
  WriteBack: '受控回写',
  Payment: '付款',
  QualityRelease: '质量放行',
  EquipmentControl: '设备控制',
  FormulaChange: '配方变更',
  SafetyInterlock: '安全联锁',
  ContractCommitment: '外部合同承诺',
  CustomerCompensation: '客户重大赔付',
}

export const confirmationLabels: Record<string, string> = {
  payment: '财务付款',
  'quality-release': '质量放行',
  'formula-change': '配方变更',
  'equipment-control': '设备控制',
  'safety-interlock': '安全联锁',
  'contract-commitment': '外部合同承诺',
  'customer-compensation': '客户重大赔付',
  'low-confidence': '低置信度结果',
  'external-impact': '可能产生外部影响',
  'data-sensitivity': '敏感数据访问',
}

export const stopConditionLabels: Record<string, string> = {
  'manual-stop': '人工紧急停止',
  'risk-limit': '超过风险上限',
  'amount-limit': '超过金额上限',
  'unauthorized-tool': '请求未授权工具',
  'validation-failure': '校验失败',
  'model-error': '模型或网关异常',
  'data-scope-violation': '数据范围越界',
}

export const changeRequestStatusLabels: Record<string, string> = {
  Pending: '等待第二管理员审核',
  Applied: '审核通过并生效',
  Rejected: '已驳回',
  Cancelled: '已取消',
}

export function formatAmount(value: number): string {
  return value > 0 ? `¥${value.toLocaleString('zh-CN', { maximumFractionDigits: 4 })}` : '不允许金额动作'
}

export function formatTime(value?: string | null): string {
  return value ? new Date(value).toLocaleString('zh-CN', { hour12: false }) : '暂无'
}

export function formatDataScope(scope: string, departmentNames: Map<string, string>): string {
  const [type, id] = scope.split(':', 2)
  if (type === 'department') return departmentNames.get(id) ?? '授权部门'
  if (type === 'organization') return '当前组织'
  return '受限范围'
}

export function formatToolName(code: string): string {
  return {
    'workflow.read': '读取流程与待办',
    'workflow.assist': '生成流程辅助意见',
    'workflow.proxy-low-risk': '低风险流程代理',
    'event.inbox.review': '审阅事件收件箱',
    'project.task.draft': '生成项目任务草稿',
    'project.read': '读取项目',
    'project.write': '维护项目任务',
    'audit.read': '读取审计证据',
    'file.metadata.read': '读取文件元数据',
    'file.upload': '上传受控文件',
    'identity.read': '读取当前身份',
  }[code] ?? code
}
