import { describe, expect, it } from 'vitest'
import {
  MAIN_COLUMN_INTRO_KEYS,
  introCatalog,
} from './introCatalog'

const expectedTitles = {
  'my-work': '我的工作',
  confirmations: '统一确认',
  workflows: '流程事项',
  inbox: '事件收件箱',
  department: '部门工作区',
  documents: '部门文件',
  minutes: 'AI 听记',
  projects: '项目工作台',
  objects: '业务视图',
  scenarios: '岗位业务场景',
  agents: '数字员工身份',
  configurations: '流程配置',
  iam: 'IAM 与导航',
  audit: '审计证据',
  modeling: '业务建模工作室',
  'mcp-center': 'MCP 接入治理',
  management: '管理概览',
} as const

describe('main column intro catalog', () => {
  it('covers every directly navigable main column exactly once', () => {
    expect(MAIN_COLUMN_INTRO_KEYS).toHaveLength(17)
    expect(new Set(MAIN_COLUMN_INTRO_KEYS).size).toBe(17)
    expect(Object.keys(introCatalog).sort()).toEqual([...MAIN_COLUMN_INTRO_KEYS].sort())

    for (const key of MAIN_COLUMN_INTRO_KEYS) {
      expect(introCatalog[key].title).toBe(expectedTitles[key])
    }
  })

  it('contains a real operating path and at least two specific business examples', () => {
    const allExamples = MAIN_COLUMN_INTRO_KEYS.flatMap((key) => introCatalog[key].examples)

    for (const key of MAIN_COLUMN_INTRO_KEYS) {
      const intro = introCatalog[key]
      expect(intro.steps.length, `${key} steps`).toBeGreaterThanOrEqual(3)
      expect(intro.steps.length, `${key} steps`).toBeLessThanOrEqual(5)
      expect(intro.examples.length, `${key} examples`).toBeGreaterThanOrEqual(2)
      expect(intro.audiences.length, `${key} audiences`).toBeGreaterThan(0)
      expect(intro.dataSources.length, `${key} data sources`).toBeGreaterThan(0)
      expect(intro.permissions.length, `${key} permissions`).toBeGreaterThan(0)
      expect(intro.boundaries.length, `${key} boundaries`).toBeGreaterThan(0)
      expect(intro.nextStep.length, `${key} next step`).toBeGreaterThan(10)
    }

    expect(new Set(allExamples).size).toBe(allExamples.length)
  })

  it('uses business language and keeps technical identifiers out of employee help', () => {
    const serialized = JSON.stringify(introCatalog)

    for (const prohibited of [
      'UUID',
      'TraceId',
      'WorkflowDefinition',
      'NodeCode',
      'AgentProfileId',
      'object_id',
      'schema_name',
    ]) {
      expect(serialized).not.toContain(prohibited)
    }
    expect(serialized).not.toMatch(
      /[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}/i,
    )
  })

  it('links only to known main-column routes that were actually implemented', () => {
    const allowedRoutes = new Set([
      '/',
      '/confirmations',
      '/workflows',
      '/inbox',
      '/department',
      '/documents',
      '/minutes',
      '/projects',
      '/objects',
      '/scenarios',
      '/agents',
      '/configurations',
      '/iam',
      '/audit',
      '/modeling',
      '/mcp-center',
      '/management',
    ])
    const links = MAIN_COLUMN_INTRO_KEYS.flatMap((key) => introCatalog[key].links ?? [])

    expect(links.length).toBeGreaterThan(0)
    expect(links.every((link) => allowedRoutes.has(link.to))).toBe(true)
  })
})
