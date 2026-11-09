import { describe, expect, it } from 'vitest'
import {
  actionLabels,
  formatAmount,
  formatDataScope,
  formatToolName,
  lifecyclePresentation,
  runStatusPresentation,
} from './agentPresentation'

describe('agentPresentation', () => {
  it('uses business language for lifecycle and run states', () => {
    expect(lifecyclePresentation.Published.label).toBe('有效')
    expect(lifecyclePresentation.Disabled.label).toBe('已停用')
    expect(runStatusPresentation.AwaitingConfirmation.label).toBe('等待真人确认')
    expect(runStatusPresentation.Failed.label).toBe('已阻断或失败')
    expect(actionLabels.FormulaChange).toBe('配方变更')
    expect(formatToolName('workflow.proxy-low-risk')).toBe('低风险流程代理')
  })

  it('formats amounts and scopes without exposing raw identifiers', () => {
    expect(formatAmount(0)).toBe('不允许金额动作')
    expect(formatAmount(12_000)).toContain('12,000')
    expect(formatDataScope(
      'department:dept-1',
      new Map([['dept-1', '研发部']]),
    )).toBe('研发部')
    expect(formatDataScope('organization:org-1', new Map())).toBe('当前组织')
  })
})
