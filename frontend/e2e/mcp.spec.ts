import { expect, test, type APIResponse, type Page } from '@playwright/test'
import { createHash, randomBytes } from 'node:crypto'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const testDirectory = fileURLToPath(new URL('.', import.meta.url))
const evidenceDir = process.env.AIDT_EVIDENCE_DIR
  ? path.resolve(process.env.AIDT_EVIDENCE_DIR)
  : path.resolve(testDirectory, '../../docs/project/test-artifacts')

async function loginAsAdmin(page: Page) {
  await page.goto('/login')
  await page.getByTestId('login-user').click()
  await page.getByRole('option', { name: '平台管理员', exact: true }).click()
  await page.getByTestId('login-submit').click()
  await expect(page).toHaveURL(/\/$/)
}

function base64Url(value: Buffer) {
  return value.toString('base64').replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '')
}

async function mcpRequest(
  page: Page,
  accessToken: string,
  id: number,
  method: string,
  params: Record<string, unknown>,
): Promise<APIResponse> {
  return page.request.post('/mcp', {
    headers: {
      Authorization: `Bearer ${accessToken}`,
      Accept: 'application/json, text/event-stream',
      'MCP-Protocol-Version': '2025-06-18',
    },
    data: {
      jsonrpc: '2.0',
      id,
      method,
      params,
    },
  })
}

async function sseJson(response: APIResponse) {
  const text = await response.text()
  const dataLine = text
    .split(/\r?\n/)
    .find((line) => line.startsWith('data: '))
  if (dataLine) {
    return JSON.parse(dataLine.slice(6)) as Record<string, unknown>
  }
  return JSON.parse(text) as Record<string, unknown>
}

test('@desktop @mcp browser OAuth issues scoped token, discovers tools, enforces revoke and human confirmation', async ({
  page,
}) => {
  test.skip(
    process.env.PLAYWRIGHT_USE_EXISTING_DEPLOY !== '1',
    'MCP authorization requires the deployed Nginx route shared with AIDT Cookie session.',
  )
  await loginAsAdmin(page)

  const verifier = base64Url(randomBytes(48))
  const challenge = base64Url(createHash('sha256').update(verifier).digest())
  const redirectUri = 'http://127.0.0.1:45679/callback'
  const registration = await page.request.post('/oauth/register', {
    data: {
      client_name: 'AIDT Playwright MCP',
      redirect_uris: [redirectUri],
      token_endpoint_auth_method: 'none',
      grant_types: ['authorization_code', 'refresh_token'],
      response_types: ['code'],
      scope: 'aidt:read aidt:approve',
    },
  })
  expect(registration.ok()).toBeTruthy()
  const client = (await registration.json()) as { client_id: string }

  let authorizationCode = ''
  page.on('request', (request) => {
    if (request.url().startsWith(redirectUri)) {
      authorizationCode = new URL(request.url()).searchParams.get('code') ?? ''
    }
  })
  const authorizeUrl = new URL('/oauth/authorize', 'http://127.0.0.1:8088')
  authorizeUrl.searchParams.set('client_id', client.client_id)
  authorizeUrl.searchParams.set('redirect_uri', redirectUri)
  authorizeUrl.searchParams.set('response_type', 'code')
  authorizeUrl.searchParams.set('code_challenge', challenge)
  authorizeUrl.searchParams.set('code_challenge_method', 'S256')
  authorizeUrl.searchParams.set('state', 'playwright-state')
  authorizeUrl.searchParams.set('scope', 'aidt:read aidt:approve')
  await page.goto(authorizeUrl.toString())
  await expect(page.getByRole('heading', { name: '授权智能体连接 AIDT' })).toBeVisible()
  await expect(page.getByText('客户端只会继承你当前已有的 AIDT 权限')).toBeVisible()
  await page.getByRole('button', { name: '确认授权' }).click()
  await expect.poll(() => authorizationCode).not.toBe('')

  const tokenResponse = await page.request.post('/oauth/token', {
    form: {
      grant_type: 'authorization_code',
      client_id: client.client_id,
      code: authorizationCode,
      redirect_uri: redirectUri,
      code_verifier: verifier,
    },
  })
  expect(tokenResponse.ok()).toBeTruthy()
  const tokens = (await tokenResponse.json()) as {
    access_token: string
    refresh_token: string
  }

  const listResponse = await mcpRequest(page, tokens.access_token, 1, 'tools/list', {})
  expect(listResponse.ok()).toBeTruthy()
  const listPayload = await sseJson(listResponse)
  const listText = JSON.stringify(listPayload)
  expect(listText).toContain('aidt_whoami')
  expect(listText).toContain('aidt_decide_workflow_task')

  const whoAmI = await mcpRequest(page, tokens.access_token, 2, 'tools/call', {
    name: 'aidt_whoami',
    arguments: {},
  })
  expect(whoAmI.ok()).toBeTruthy()
  expect(JSON.stringify(await sseJson(whoAmI)))
    .toContain('10000000-0000-0000-0000-000000000010')

  const highRisk = await mcpRequest(page, tokens.access_token, 3, 'tools/call', {
    name: 'aidt_decide_workflow_task',
    arguments: {
      arguments: {
        taskId: '11111111-1111-1111-1111-111111111111',
        decision: 'Approve',
        expectedVersion: 1,
        comment: 'Playwright 真人边界验证',
        idempotencyKey: `pw-mcp-${Date.now()}`,
      },
    },
  })
  expect(highRisk.ok()).toBeTruthy()
  const highRiskPayload = await sseJson(highRisk)
  const highRiskText = JSON.stringify(highRiskPayload)
  expect(highRiskText).toContain('HUMAN_CONFIRMATION_REQUIRED')
  const toolText = (
    highRiskPayload.result as {
      content?: Array<{ text?: string }>
    }
  ).content?.[0]?.text
  expect(toolText).toBeTruthy()
  const boundary = JSON.parse(toolText!) as {
    data?: {
      id?: string
      confirmationId?: string
    }
  }
  const confirmationId = boundary.data?.confirmationId ?? boundary.data?.id
  expect(confirmationId).toBeTruthy()

  await page.goto(`/mcp-confirmations?confirmationId=${confirmationId}`)
  await expect(page.getByTestId('mcp-confirmation-page')).toBeVisible()
  await expect(page.getByRole('heading', { name: '处理流程任务' })).toBeVisible()
  await page.getByTestId('mcp-decision-comment').fill('Playwright 核对业务影响后通过')
  await page.getByTestId('confirm-mcp-confirmation').click()
  await expect(page.getByText(/已通过；智能体可使用同一确认编号重试一次/)).toBeVisible()

  await page.goto('/mcp-center')
  await expect(page.getByTestId('mcp-center-page')).toBeVisible()
  await expect(page.getByText('aidt_decide_workflow_task').first()).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, 'mcp-center-deployed.png'),
    fullPage: true,
  })

  const snapshot = await page.request.get('/api/v1/mcp/snapshot?pageSize=100')
  expect(snapshot.ok()).toBeTruthy()
  const snapshotData = (await snapshot.json()) as {
    data: {
      tokens: Array<{ id: string; clientId: string; kind: string; revokedAt: string | null }>
    }
  }
  const accessTokenRow = snapshotData.data.tokens.find(
    (item) => item.clientId === client.client_id && item.kind === 'Access' && item.revokedAt === null,
  )
  expect(accessTokenRow).toBeTruthy()
  const csrf = await page.request.get('/api/v1/auth/csrf')
  const csrfToken = String(((await csrf.json()) as { data: { requestToken: string } }).data.requestToken)
  const revoke = await page.request.post(
    `/api/v1/mcp/tokens/${accessTokenRow!.id}/actions/revoke`,
    { headers: { 'X-CSRF-TOKEN': csrfToken } },
  )
  expect(revoke.ok()).toBeTruthy()

  const revokedCall = await mcpRequest(page, tokens.access_token, 4, 'tools/list', {})
  expect(revokedCall.status()).toBe(401)
})
