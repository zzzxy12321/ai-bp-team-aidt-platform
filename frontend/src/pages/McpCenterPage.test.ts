import { flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import McpCenterPage from './McpCenterPage.vue'
import { mcpApi } from '@/api/modules/mcpApi'
import { workspaceApi } from '@/api/modules/workspaceApi'
import { useAuthStore } from '@/stores/auth'

vi.mock('@/api/modules/mcpApi', () => ({
  mcpApi: {
    getSnapshot: vi.fn(),
    revokeClient: vi.fn(),
    revokeToken: vi.fn(),
  },
}))

vi.mock('@/api/modules/workspaceApi', () => ({
  workspaceApi: {
    departments: vi.fn(),
    agents: vi.fn(),
  },
}))

describe('McpCenterPage', () => {
  let pinia: ReturnType<typeof createPinia>

  beforeEach(() => {
    vi.clearAllMocks()
    pinia = createPinia()
    setActivePinia(pinia)
    vi.mocked(mcpApi.getSnapshot).mockResolvedValue({
      tools: [
        {
          name: 'aidt_whoami',
          title: '当前身份',
          description: '读取当前 MCP 用户',
          scope: 'aidt:read',
          readOnly: true,
          highRisk: false,
          resourceType: 'Identity',
          existingPermissions: ['permissions:Read'],
        },
        {
          name: 'aidt_confirm_event',
          title: '确认事件',
          description: '真人确认事件',
          scope: 'aidt:approve',
          readOnly: false,
          highRisk: true,
          resourceType: 'EventInbox',
          existingPermissions: ['event-inbox:Confirm'],
        },
      ],
      clients: [
        {
          clientId: 'client-1',
          clientName: 'Codex',
          isActive: true,
          activeTokenCount: 1,
          createdAt: '2026-11-01T00:00:00Z',
          lastUsedAt: null,
        },
      ],
      tokens: [
        {
          id: 'token-1',
          clientId: 'client-1',
          clientName: 'Codex',
          userDisplayName: '平台管理员',
          kind: 'Access',
          scopes: ['aidt:read'],
          departmentIds: ['department-1'],
          createdAt: '2026-11-01T00:00:00Z',
          expiresAt: '2026-11-01T01:00:00Z',
          revokedAt: null,
          lastUsedAt: null,
        },
      ],
      confirmations: [],
      recentInvocations: [],
    })
    vi.mocked(workspaceApi.agents).mockResolvedValue([])
    vi.mocked(workspaceApi.departments).mockResolvedValue([])
  })

  it('renders tool scope and human-confirmation boundaries', async () => {
    const auth = useAuthStore()
    auth.permissions = [
      { resource: 'mcp', action: 'Read' },
      { resource: 'mcp', action: 'Manage' },
    ]
    const wrapper = mount(McpCenterPage, {
      global: {
        plugins: [pinia],
        stubs: {
          ElButton: { template: '<button><slot /></button>' },
          ElAlert: true,
          ElSkeleton: true,
          ElTabs: { template: '<div><slot /></div>' },
          ElTabPane: { template: '<section><slot /></section>' },
          ElEmpty: true,
          ElTable: { template: '<div><slot /></div>' },
          ElTableColumn: true,
          ElTag: { template: '<span><slot /></span>' },
        },
      },
    })

    await flushPromises()

    expect(mcpApi.getSnapshot).toHaveBeenCalledTimes(1)
    expect(wrapper.get('h1').text()).toContain('MCP 接入治理')
    expect(wrapper.text()).toContain('aidt_whoami')
    expect(wrapper.text()).toContain('aidt:approve')
    expect(wrapper.text()).toContain('真人确认')
  })
})
