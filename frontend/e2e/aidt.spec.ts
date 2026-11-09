import { expect, test, type Page, type Request } from '@playwright/test'
import { createHash, randomBytes } from 'node:crypto'
import fs from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const testDirectory = fileURLToPath(new URL('.', import.meta.url))
const evidenceDir = process.env.AIDT_EVIDENCE_DIR
  ? path.resolve(process.env.AIDT_EVIDENCE_DIR)
  : path.resolve(testDirectory, '../../docs/project/test-artifacts')

async function loginAs(page: Page, roleLabel: string) {
  await page.goto('/login')
  await page.getByTestId('login-user').click()
  await page.getByRole('option', { name: roleLabel, exact: true }).click()
  await page.getByTestId('login-submit').click()
  await expect(page).toHaveURL(/\/$/)
  await expect(page.getByTestId('my-work-page')).toBeVisible()
}

async function logout(page: Page) {
  await page.getByTestId('logout-button').click()
  await expect(page).toHaveURL(/\/login/)
}

async function filterMyWorkSource(page: Page, sourceLabel: string) {
  await page
    .getByTestId('my-work-source-filter')
    .locator('.el-select__wrapper')
    .click({ position: { x: 20, y: 18 } })
  const option = page
    .locator('.el-select-dropdown__item:visible')
    .filter({ hasText: sourceLabel })
    .first()
  await expect(option).toBeVisible()
  await option.click()
  await expect(page.getByTestId('my-work-source-filter')).toContainText(sourceLabel)
}

async function searchMyWork(page: Page, keyword: string) {
  await page.getByRole('textbox', { name: '搜索我的工作' }).fill(keyword)
  await page.waitForTimeout(450)
}

async function resetMyWork(page: Page) {
  const reset = page.getByTestId('my-work-reset')
  if (await reset.isEnabled()) {
    await reset.click()
    await expect(page.getByTestId('my-work-source-filter')).toContainText('来源类型')
  }
}

async function selectFirstVisibleOption(page: Page, text?: string) {
  const allOptions = page.locator('.el-select-dropdown__item:visible')
  await expect(allOptions.first()).toBeVisible()
  const matchingOptions = text ? allOptions.filter({ hasText: text }) : allOptions
  const genericOptions = allOptions.filter({ hasText: '数字员工服务账号' })
  const options = (await matchingOptions.count())
    ? matchingOptions
    : (await genericOptions.count())
      ? genericOptions
      : allOptions
  await expect(options.first()).toBeVisible()
  await options.first().click({ force: true })
  await expect(allOptions).toHaveCount(0)
}

async function submitInboxDialog(page: Page, dialog: ReturnType<Page['getByRole']>) {
  const submit = dialog.getByTestId('submit-event-button')
  await expect(submit).toBeEnabled()
  const responsePromise = page.waitForResponse(
    (response) => response.url().includes('/api/v1/inbox') && response.request().method() === 'POST',
  )
  await submit.click()
  expect((await responsePromise).ok()).toBeTruthy()
  await expect(dialog).toBeHidden()
}

async function createAgentDraftViaUi(
  page: Page,
  input: {
    code: string
    name: string
    description: string
    autonomy?: 'A0' | 'A1' | 'A2' | 'A3' | 'A4'
    risk?: 'Low' | 'Medium' | 'High'
    amount?: number
    toolName?: string
    serviceAccountName?: string
  },
) {
  await page.getByTestId('create-agent-identity').click()
  const dialog = page.getByRole('dialog', { name: '新建数字员工身份' })
  await dialog.getByTestId('agent-code-input').fill(input.code)
  await dialog.getByTestId('agent-name-input').fill(input.name)
  await dialog.getByTestId('agent-description-input').fill(input.description)

  await dialog.getByRole('tab', { name: '组织与负责人' }).click()
  await dialog.getByTestId('agent-department-select').click()
  const rndOption = page.getByRole('option', {
    name: '研发 (RND)',
    exact: true,
  })
  await expect(rndOption).toBeVisible()
  await rndOption.click()
  await expect(page.locator('.el-select-dropdown__item:visible')).toHaveCount(0)
  await expect(dialog.getByTestId('agent-department-select')).toContainText('研发 (RND)')
  await dialog.getByTestId('agent-business-owner-select').click()
  await selectFirstVisibleOption(page)
  await dialog.getByTestId('agent-technical-owner-select').click()
  await selectFirstVisibleOption(page)

  await dialog.getByRole('tab', { name: '身份与服务账号' }).click()
  await dialog.getByTestId('agent-service-select').click()
  await selectFirstVisibleOption(page, input.serviceAccountName ?? '数字员工服务账号')

  if (input.autonomy || input.risk || input.amount) {
    await dialog.getByRole('tab', { name: '自主等级与限额' }).click()
    if (input.autonomy) {
      await dialog.getByTestId(`agent-autonomy-${input.autonomy}`).click()
    }
    if (input.risk) {
      await dialog.getByTestId('agent-risk-limit').click()
      await page
        .getByRole('option', {
          name: input.risk === 'Low' ? '低风险' : input.risk === 'Medium' ? '中风险' : '高风险',
        })
        .click()
    }
    if (input.amount !== undefined) {
      await dialog.getByTestId('agent-amount-limit').locator('input').fill(String(input.amount))
    }
  }

  await dialog.getByRole('tab', { name: '工具与能力' }).click()
  await dialog.getByTestId(`agent-tool-${input.toolName ?? 'workflow.read'}`).click()

  const responsePromise = page.waitForResponse(
    (response) => response.url().endsWith('/api/v1/agents/drafts') && response.request().method() === 'POST',
  )
  await dialog.getByTestId('save-agent-draft').click()
  const response = await responsePromise
  expect(response.ok()).toBeTruthy()
  const payload = await response.json()
  await expect(dialog).toBeHidden()
  return {
    profileId: String(payload.data.id),
    profileVersion: Number(payload.data.version),
  }
}

async function createProjectViaUi(page: Page, name: string, objective: string) {
  await page.goto('/projects')
  await expect(page.getByTestId('projects-page')).toBeVisible()
  await page.getByTestId('create-project-button').click()
  const dialog = page.getByRole('dialog', { name: '新建项目' })
  await dialog.getByTestId('project-name-input').fill(name)
  await dialog.getByTestId('project-objective-input').fill(objective)
  const createResponse = page.waitForResponse(
    (response) => response.url().endsWith('/api/v1/projects') && response.request().method() === 'POST',
  )
  await dialog.getByTestId('submit-project-button').click()
  expect((await createResponse).ok()).toBeTruthy()
  await expect(page).toHaveURL(/\/projects\/[0-9a-f-]+/i)
  await expect(page.getByRole('heading', { name, exact: true })).toBeVisible()
}

function workflowTaskCard(page: Page, instanceId: string, nodeCode?: string) {
  const nodeSelector = nodeCode ? `[data-node-code="${nodeCode}"]` : ''
  return page.locator(
    `[data-testid="workflow-task"][data-workflow-instance-id="${instanceId}"]${nodeSelector}`,
  )
}

async function approveCurrentTask(page: Page, roleLabel: string, instanceId: string, nodeCode: string) {
  await logout(page)
  await loginAs(page, roleLabel)
  await page.goto(`/workflows?refresh=${Date.now()}`)

  const task = workflowTaskCard(page, instanceId, nodeCode)
  await expect(task).toBeVisible({ timeout: 45_000 })
  const decisionResponse = page.waitForResponse(
    (response) =>
      response.url().includes('/api/v1/workflow-tasks/') &&
      response.url().endsWith('/actions/decide') &&
      response.request().method() === 'POST',
  )
  await task.getByTestId('approve-task-button').click()
  await page.getByRole('button', { name: '确认通过', exact: true }).click()
  const response = await decisionResponse
  expect(response.ok()).toBeTruthy()
  const decisionPayload = await response.json()
  await page.reload()
  await expect(task.getByTestId('approve-task-button')).toHaveCount(0)
  return String(decisionPayload.data.id)
}

async function csrfFromPage(page: Page): Promise<string> {
  const response = await page.request.get('/api/v1/auth/csrf')
  const payload = await response.json()
  return String(payload.data.requestToken)
}

async function apiFromPage<T>(
  page: Page,
  path: string,
  method: 'GET' | 'POST' | 'PUT' = 'GET',
  body?: unknown,
  idempotencyKey?: string,
): Promise<T> {
  const csrf = method === 'GET' ? '' : await csrfFromPage(page)
  const response = await page.request.fetch(path, {
    method,
    data: body,
    headers: {
      ...(csrf ? { 'X-CSRF-TOKEN': csrf } : {}),
      ...(idempotencyKey ? { 'Idempotency-Key': idempotencyKey } : {}),
    },
  })
  if (!response.ok()) {
    throw new Error(`${method} ${path}: ${response.status()} ${await response.text()}`)
  }
  return (await response.json()).data as T
}

async function publishProjectWorkflowViaApi(page: Page, marker: string) {
  const departments = await apiFromPage<Array<{ id: string; code: string }>>(page, '/api/v1/departments')
  const departmentId = departments.find((item) => item.code === 'RND')!.id
  const configuration = await apiFromPage<{ id: string }>(
    page,
    '/api/v1/configurations',
    'POST',
    {
      configurationKey: `UX004_${Date.now()}`,
      name: `${marker} 项目事件确认流程`,
      description: 'UX-004-A E2E 原子确认与流程发起',
      departmentId,
      objectTypeCode: 'PROJECT',
      startNode: 'START',
      nodes: [
        {
          code: 'START',
          name: '项目事件确认',
          assigneeUserName: 'demo.admin',
          nextNode: null,
          completeOnApprove: true,
          condition: null,
          assigneeStrategy: 'SpecificUser',
          businessRoleCode: null,
          agentProfileId: null,
        },
      ],
    },
    `e2e-ux004-config-${Date.now()}`,
  )
  await apiFromPage(
    page,
    `/api/v1/configurations/${configuration.id}/actions/validate`,
    'POST',
    undefined,
    `e2e-ux004-validate-${Date.now()}`,
  )
  return await apiFromPage<{
    publishedWorkflowDefinitionId: string
    publishedWorkflowVersionId: string
  }>(
    page,
    `/api/v1/configurations/${configuration.id}/actions/publish`,
    'POST',
    undefined,
    `e2e-ux004-publish-${Date.now()}`,
  )
}

function base64Url(value: Buffer) {
  return value.toString('base64').replaceAll('+', '-').replaceAll('/', '_').replace(/=+$/, '')
}

async function createUnifiedWorkSources(page: Page) {
  const marker = `UX003-E2E-${Date.now()}`
  const departments = await apiFromPage<Array<{ id: string; code: string }>>(page, '/api/v1/departments')
  const departmentId = departments.find((item) => item.code === 'RND')!.id
  const session = await apiFromPage<{ userId: string }>(page, '/api/v1/auth/session')
  const agents = await apiFromPage<
    Array<{
      id: string
      name: string
      autonomyLevel: string
    }>
  >(page, '/api/v1/agents')
  const eventAgent = agents.find((item) => item.name.includes('项目事件'))!
  const businessAgent = agents.find((item) => item.name.includes('业务协同'))!
  const types = await apiFromPage<{
    items: Array<{ id: string; code: string }>
  }>(page, '/api/v1/object-types?pageSize=100')
  const sampleType = types.items.find((item) => item.code === 'CUSTOMER_SAMPLE')!
  const objectCode = `${marker}-OBJ`
  const businessObject = await apiFromPage<{ id: string }>(
    page,
    '/api/v1/business-objects',
    'POST',
    {
      objectTypeId: sampleType.id,
      objectCode,
      departmentId,
      fields: {
        Customer: 'E2E 聚合客户',
        Product: 'E2E 聚合样品',
        Requirement: marker,
        TrialRequired: 'No',
        InternalFormulaReference: 'E2E-TEST-ONLY',
      },
    },
    `e2e-ux003-object-${Date.now()}`,
  )
  const configuration = await apiFromPage<{ id: string }>(
    page,
    '/api/v1/configurations',
    'POST',
    {
      configurationKey: `UX003_${Date.now()}`,
      name: `${marker} 统一事项流程`,
      description: '统一工作事项 E2E',
      departmentId,
      objectTypeCode: 'CUSTOMER_SAMPLE',
      startNode: 'START',
      nodes: [
        {
          code: 'START',
          name: '统一工作事项确认',
          assigneeUserName: 'demo.admin',
          nextNode: null,
          completeOnApprove: true,
          condition: null,
          assigneeStrategy: 'SpecificUser',
          businessRoleCode: null,
          agentProfileId: null,
        },
      ],
    },
    `e2e-ux003-config-${Date.now()}`,
  )
  await apiFromPage(
    page,
    `/api/v1/configurations/${configuration.id}/actions/validate`,
    'POST',
    undefined,
    `e2e-ux003-validate-${Date.now()}`,
  )
  const published = await apiFromPage<{
    publishedWorkflowDefinitionId: string
    publishedWorkflowVersionId: string
  }>(
    page,
    `/api/v1/configurations/${configuration.id}/actions/publish`,
    'POST',
    undefined,
    `e2e-ux003-publish-${Date.now()}`,
  )
  const workflow = await apiFromPage<{ id: string }>(page, '/api/v1/workflow-instances', 'POST', {
    workflowDefinitionId: published.publishedWorkflowDefinitionId,
    workflowVersionId: published.publishedWorkflowVersionId,
    businessObjectId: businessObject.id,
    idempotencyKey: crypto.randomUUID(),
  })

  const project = await apiFromPage<{ id: string; name: string }>(
    page,
    '/api/v1/projects',
    'POST',
    {
      objectCode: `${marker}-PRJ`,
      name: `${marker} 聚合项目`,
      ownerUserId: session.userId,
      departmentId,
      plannedStartDate: new Date(Date.now() - 86_400_000).toISOString(),
      plannedEndDate: new Date(Date.now() + 2_592_000_000).toISOString(),
      objective: '验证统一工作事项聚合',
      currentStage: '执行中',
      confidentialityLevel: 'Internal',
      initialStatus: 'Active',
    },
    `e2e-ux003-project-${Date.now()}`,
  )

  const pendingEvent = await apiFromPage<{ id: string; title: string }>(
    page,
    '/api/v1/inbox',
    'POST',
    {
      departmentId,
      title: `${marker} 待确认事项`,
      content: '请确认负责人和截止时间。',
      sourceType: 'ManualText',
    },
    `e2e-ux003-event-${Date.now()}`,
  )
  await apiFromPage(
    page,
    `/api/v1/inbox/${pendingEvent.id}/actions/analyze`,
    'POST',
    { agentProfileId: eventAgent.id },
    `e2e-ux003-event-analyze-${Date.now()}`,
  )

  const taskEvent = await apiFromPage<{ id: string; title: string }>(
    page,
    '/api/v1/inbox',
    'POST',
    {
      departmentId,
      title: `${marker} 事件转任务去重`,
      content: '真实确认后生成项目任务，不能和事件重复显示。',
      sourceType: 'ManualText',
    },
    `e2e-ux003-task-event-${Date.now()}`,
  )
  await apiFromPage(
    page,
    `/api/v1/inbox/${taskEvent.id}/actions/analyze`,
    'POST',
    { agentProfileId: eventAgent.id },
    `e2e-ux003-task-event-analyze-${Date.now()}`,
  )
  const confirmation = await apiFromPage<{
    event: { id: string }
    task: { id: string; title: string; version: number }
  }>(
    page,
    `/api/v1/inbox/${taskEvent.id}/actions/confirm`,
    'POST',
    {
      projectId: project.id,
      assigneeId: session.userId,
      dueAt: new Date(Date.now() + 172_800_000).toISOString(),
      priority: 'P1',
      content: `${marker} 去重项目任务`,
      workflowInstanceId: null,
      createTask: true,
    },
    `e2e-ux003-task-confirm-${Date.now()}`,
  )

  const minutesSource = await apiFromPage<{ id: string; title: string }>(
    page,
    '/api/v1/minutes',
    'POST',
    {
      departmentId,
      title: `${marker} 统一事项会议`,
      meetingAt: new Date().toISOString(),
      transcript: `[09:30:00] 项目经理：请在 2026-11-10 完成 ${marker} 证据核对。`,
    },
    `e2e-ux003-minutes-${Date.now()}`,
  )
  const minutesRun = await apiFromPage<{ id: string }>(
    page,
    `/api/v1/minutes/${minutesSource.id}/actions/analyze`,
    'POST',
    { agentProfileId: businessAgent.id },
    `e2e-ux003-minutes-analyze-${Date.now()}`,
  )
  await apiFromPage(page, `/api/v1/agents/runs/${minutesRun.id}/actions/confirm`, 'POST', {
    confirmationId: crypto.randomUUID(),
  })
  await apiFromPage(page, `/api/v1/agents/runs/${minutesRun.id}/actions/execute`, 'POST')
  const minutesActions = await apiFromPage<Array<{ id: string }>>(
    page,
    `/api/v1/minutes/${minutesSource.id}/action-items`,
  )

  const agentRun = await apiFromPage<{ id: string; status: string }>(
    page,
    '/api/v1/agents/runs',
    'POST',
    {
      agentProfileId: businessAgent.id,
      action: 'Draft',
      amount: 0,
      riskLevel: 'Low',
      model: 'aidt-rule-sim',
    },
    `e2e-ux003-agent-${Date.now()}`,
  )

  return {
    marker,
    departmentId,
    workflowInstanceId: workflow.id,
    businessObjectId: businessObject.id,
    businessObjectCode: objectCode,
    projectId: project.id,
    projectName: project.name,
    pendingEventId: pendingEvent.id,
    taskEventId: taskEvent.id,
    projectTask: confirmation.task,
    minutesSourceId: minutesSource.id,
    minutesActionId: minutesActions[0]?.id ?? '',
    minutesTitle: minutesSource.title,
    agentRunId: agentRun.id,
  }
}

async function createMcpConfirmationFromDeployment(page: Page): Promise<string | null> {
  const origin =
    process.env.AIDT_DEPLOY_ORIGIN ??
    (process.env.AIDT_E2E_MCP_PORT ? `http://127.0.0.1:${process.env.AIDT_E2E_MCP_PORT}` : 'http://127.0.0.1:8088')
  const health = await page.request.get(`${origin}/health/live`).catch(() => null)
  if (!health?.ok()) return null

  const verifier = base64Url(randomBytes(48))
  const challenge = base64Url(createHash('sha256').update(verifier).digest())
  const redirectUri = 'http://127.0.0.1:45680/callback'
  const registration = await page.request.post(`${origin}/oauth/register`, {
    data: {
      client_name: `UX003 E2E ${Date.now()}`,
      redirect_uris: [redirectUri],
      token_endpoint_auth_method: 'none',
      grant_types: ['authorization_code', 'refresh_token'],
      response_types: ['code'],
      scope: 'aidt:read aidt:approve',
    },
  })
  if (!registration.ok()) return null
  const client = (await registration.json()) as { client_id: string }
  let authorizationCode = ''
  const onRequest = (request: Request) => {
    if (request.url().startsWith(redirectUri)) {
      authorizationCode = new URL(request.url()).searchParams.get('code') ?? ''
    }
  }
  page.on('request', onRequest)
  const authorizeUrl = new URL('/oauth/authorize', `${origin}/`)
  authorizeUrl.searchParams.set('client_id', client.client_id)
  authorizeUrl.searchParams.set('redirect_uri', redirectUri)
  authorizeUrl.searchParams.set('response_type', 'code')
  authorizeUrl.searchParams.set('code_challenge', challenge)
  authorizeUrl.searchParams.set('code_challenge_method', 'S256')
  authorizeUrl.searchParams.set('state', 'ux003-e2e')
  authorizeUrl.searchParams.set('scope', 'aidt:read aidt:approve')
  await page.goto(authorizeUrl.toString())
  const confirmButton = page.getByRole('button', { name: '确认授权' })
  if (!(await confirmButton.isVisible().catch(() => false))) {
    page.off('request', onRequest)
    return null
  }
  await confirmButton.click()
  await expect.poll(() => authorizationCode).not.toBe('')
  page.off('request', onRequest)
  const tokenResponse = await page.request.post(`${origin}/oauth/token`, {
    form: {
      grant_type: 'authorization_code',
      client_id: client.client_id,
      code: authorizationCode,
      redirect_uri: redirectUri,
      code_verifier: verifier,
    },
  })
  if (!tokenResponse.ok()) return null
  const tokens = (await tokenResponse.json()) as { access_token: string }
  const highRisk = await page.request.post(`${origin}/mcp`, {
    headers: {
      Authorization: `Bearer ${tokens.access_token}`,
      Accept: 'application/json, text/event-stream',
      'MCP-Protocol-Version': '2025-06-18',
    },
    data: {
      jsonrpc: '2.0',
      id: 1,
      method: 'tools/call',
      params: {
        name: 'aidt_decide_workflow_task',
        arguments: {
          arguments: {
            taskId: '11111111-1111-1111-1111-111111111111',
            decision: 'Approve',
            expectedVersion: 1,
            comment: 'UX-003 E2E 真人边界',
            idempotencyKey: `ux003-mcp-${Date.now()}`,
          },
        },
      },
    },
  })
  if (!highRisk.ok()) return null
  const responseText = await highRisk.text()
  const payloadText =
    responseText
      .split(/\r?\n/)
      .find((line) => line.startsWith('data: '))
      ?.slice(6) ?? responseText
  const payload = JSON.parse(payloadText) as {
    result: { content: Array<{ text: string }> }
  }
  const toolPayload = JSON.parse(payload.result.content[0].text) as {
    data: { confirmationId?: string; id?: string }
  }
  return (toolPayload.data.confirmationId ?? toolPayload.data.id)?.toLowerCase() ?? null
}

test('@desktop creates object and completes WF-001 across all human roles', async ({ page }) => {
  test.slow()
  await fs.mkdir(evidenceDir, { recursive: true })
  await loginAs(page, '平台管理员')
  await page.screenshot({
    path: path.join(evidenceDir, 'desktop-my-work.png'),
    fullPage: true,
  })

  await page.goto('/objects')
  await page.getByTestId('object-type-select').click()
  await page.getByRole('option', { name: /客户样品/ }).click()

  const objectCode = `E2E-${Date.now()}`
  await page.getByRole('textbox', { name: '业务编号' }).fill(objectCode)
  await page.getByRole('textbox', { name: /客户 \*/ }).fill('E2E 测试客户')
  await page.getByRole('textbox', { name: /产品 \*/ }).fill('E2E 测试产品')
  await page.getByRole('textbox', { name: /客户需求 \*/ }).fill('验证真实 SQL 流程闭环')
  await page.getByRole('textbox', { name: /是否试产/ }).fill('No')
  await page.getByRole('textbox', { name: /内部配方引用/ }).fill('E2E-CORE-SECRET')
  await page.getByTestId('create-object-button').click()
  await expect(page.getByText('业务对象已持久化')).toBeVisible()
  await expect(page.getByRole('heading', { name: objectCode, exact: true })).toBeVisible()

  await page.goto('/workflows')
  await page.getByTestId('workflow-definition-select').click()
  await page.getByRole('option', { name: /客户样品到研发转产/ }).click()
  await page.getByTestId('workflow-object-select').click()
  await page.getByRole('option', { name: new RegExp(objectCode) }).click()
  const [startResponse] = await Promise.all([
    page.waitForResponse(
      (response) => response.url().endsWith('/api/v1/workflow-instances') && response.request().method() === 'POST',
    ),
    page.getByTestId('start-workflow-button').click(),
  ])
  const startPayload = await startResponse.json()
  const instanceId = String(startPayload.data.id)
  await expect(page.getByText('流程已发起，首个待办已生成')).toBeVisible()

  await approveCurrentTask(page, '研发工程师', instanceId, 'RD_INITIATE')
  await approveCurrentTask(page, '质量工程师', instanceId, 'QA_REVIEW')
  await approveCurrentTask(page, '生产班组', instanceId, 'PRODUCTION_RELEASE')
  const finalTaskId = await approveCurrentTask(page, '平台管理员', instanceId, 'FINAL_RELEASE')

  await page.goto('/workflows')
  await page.getByRole('tab', { name: /流程实例/ }).click()
  await expect(page.getByText('Completed', { exact: true }).first()).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, 'desktop-workflow-completed.png'),
    fullPage: true,
  })

  await page.goto('/objects')
  const objectRow = page.getByRole('row').filter({ hasText: objectCode })
  await expect(objectRow).toContainText('Active')
  await objectRow.scrollIntoViewIfNeeded()
  const viewObjectButton = objectRow.getByRole('button', { name: '查看', exact: true })
  const objectTable = objectRow.locator('xpath=ancestor::div[contains(@class,"el-table")]')
  await objectTable.locator('.el-scrollbar__wrap').evaluateAll((elements) => {
    elements.forEach((element) => {
      element.scrollLeft = element.scrollWidth
    })
  })
  await viewObjectButton.click()
  await expect(page).toHaveURL(/\/workflows\?object=/)
  await page.getByRole('tab', { name: /流程实例/ }).click()
  await expect(page.getByText('Completed', { exact: true }).first()).toBeVisible()

  await page.goto(`/audit?entityType=WorkflowTask&entityId=${finalTaskId}`)
  await expect(page.getByText('TraceId').first()).toBeVisible()
  await expect(page.getByRole('row').filter({ hasText: 'Approve' }).first()).toBeVisible()

  await page.goto('/agents')
  const businessAgent = page.getByTestId('agent-identity-card').filter({ hasText: '业务协同数字员工' })
  await businessAgent.click()
  await page.getByTestId('start-agent-run-button').click()
  await expect(page.getByText('运行已创建，等待真人确认。')).toBeVisible()
  await page.getByTestId('confirm-agent-run-button').first().click()
  await page.getByRole('button', { name: '确认运行', exact: true }).click()
  await expect(page.getByText('真人确认已提交。')).toBeVisible()
  await page.getByTestId('stop-agent-run-button').first().click()
  await page
    .getByRole('dialog', { name: '停止数字员工运行' })
    .getByRole('button', { name: '立即停止', exact: true })
    .click()
  await expect(page.getByText('数字员工运行已停止。')).toBeVisible()

  await page.screenshot({
    path: path.join(evidenceDir, 'desktop-agent.png'),
    fullPage: true,
  })
})

test('@desktop employee entry separates staff and management navigation', async ({ page }) => {
  await loginAs(page, '研发工程师')
  await expect(page.getByRole('link', { name: /管理概览/ })).toHaveCount(0)
  await expect(page.getByRole('link', { name: /我的工作/ }).first()).toBeVisible()
  await expect(page.getByRole('link', { name: /部门工作区/ }).first()).toBeVisible()
  await expect(page.getByRole('link', { name: /部门文件/ }).first()).toBeVisible()
  await page.goto('/management')
  await expect(page).toHaveURL(/\/access-denied/)

  await page.goto('/department')
  await expect(page.getByTestId('department-page')).toBeVisible()
  await expect(page.getByText('研发', { exact: true }).first()).toBeVisible()
})

test('@desktop unified work center aggregates sources, deduplicates and opens existing details', async ({ page }) => {
  test.slow()
  await loginAs(page, '平台管理员')
  const sources = await createUnifiedWorkSources(page)
  let mcpConfirmationId = await createMcpConfirmationFromDeployment(page)
  if (!mcpConfirmationId) {
    const confirmations = await apiFromPage<Array<{ id: string }>>(page, '/api/v1/mcp/confirmations/me')
    mcpConfirmationId = confirmations[0]?.id ?? null
  }
  expect(mcpConfirmationId).toBeTruthy()

  await page.goto('/')
  await expect(page.getByTestId('my-work-page')).toBeVisible()
  await expect(page.getByText('MY WORK · 统一工作事项', { exact: true })).toBeVisible()
  await page.waitForTimeout(350)
  await page.screenshot({
    path: path.join(evidenceDir, 'desktop-unified-work.png'),
  })

  for (const [sourceType, sourceLabel] of [
    ['WorkflowTask', '流程待办'],
    ['ProjectTask', '项目任务'],
    ['EventInbox', '事项确认'],
    ['MinutesActionItem', '会议行动项'],
    ['McpHumanConfirmation', '智能体确认'],
    ['AgentRun', '数字员工确认'],
  ] as const) {
    await filterMyWorkSource(page, sourceLabel)
    await expect(
      page.locator(`[data-testid="unified-work-item"][data-source-type="${sourceType}"]`).first(),
    ).toBeVisible()
    await resetMyWork(page)
  }

  await resetMyWork(page)
  await searchMyWork(page, sources.marker)
  await expect(
    page.locator(
      `[data-testid="unified-work-item"][data-source-type="EventInbox"][data-source-id="${sources.taskEventId}"]`,
    ),
  ).toHaveCount(0)
  await expect(
    page.locator(
      `[data-testid="unified-work-item"][data-source-type="ProjectTask"][data-source-id="${sources.projectTask.id}"]`,
    ),
  ).toBeVisible()
  await expect(
    page.locator(
      `[data-testid="unified-work-item"][data-source-type="EventInbox"][data-source-id="${sources.pendingEventId}"]`,
    ),
  ).toBeVisible()
  await expect(
    page.locator(
      `[data-testid="unified-work-item"][data-source-type="ProjectTask"][data-source-id="${sources.projectTask.id}"]`,
    ),
  ).toBeVisible()
  await page.getByRole('tab', { name: /我发起/ }).click()
  await expect(
    page.locator(
      `[data-testid="unified-work-item"][data-source-type="ProjectTask"][data-source-id="${sources.projectTask.id}"]`,
    ),
  ).toBeVisible()
  await resetMyWork(page)
  await page.getByRole('tab', { name: /我参与/ }).click()
  await searchMyWork(page, sources.marker)
  await expect(page.locator(`[data-testid="unified-work-item"][data-source-type="WorkflowTask"]`).first()).toBeVisible()
  await resetMyWork(page)
  await page.getByRole('tab', { name: /需要关注/ }).click()
  await searchMyWork(page, mcpConfirmationId)
  await expect(
    page.locator(
      `[data-testid="unified-work-item"][data-source-type="McpHumanConfirmation"][data-source-id="${mcpConfirmationId}"]`,
    ),
  ).toBeVisible()
  await resetMyWork(page)
  await page.getByRole('tab', { name: /待我处理/ }).click()

  const visibleText = (await page.locator('[data-testid="unified-work-item"]').allTextContents()).join('\n')
  expect(visibleText).not.toMatch(/[0-9a-f]{8}-[0-9a-f-]{27,}/i)
  expect(visibleText).not.toContain('TraceId')
  expect(visibleText).not.toContain('__EFMigrationsHistory')

  const beforeRefresh = await page
    .locator('[data-testid="unified-work-item"]')
    .evaluateAll((nodes) =>
      nodes.map((node) => node.getAttribute('data-source-type') + ':' + node.getAttribute('data-source-id')),
    )
  await page.reload()
  await expect(page.getByTestId('my-work-page')).toBeVisible()
  await expect(page.locator('[data-testid="unified-work-item"]').first()).toBeVisible()
  const afterRefresh = await page
    .locator('[data-testid="unified-work-item"]')
    .evaluateAll((nodes) =>
      nodes.map((node) => node.getAttribute('data-source-type') + ':' + node.getAttribute('data-source-id')),
    )
  expect(afterRefresh).toEqual(beforeRefresh)

  await resetMyWork(page)
  await filterMyWorkSource(page, '流程待办')
  await page.locator(`[data-testid="unified-work-item"][data-source-type="WorkflowTask"]`).first().click()
  await expect(page).toHaveURL(/\/workflows\?instance=/)
  await expect(page.getByRole('heading', { name: '流程事项', exact: true })).toBeVisible()

  await page.goto('/')
  await searchMyWork(page, sources.marker)
  await page
    .locator(
      `[data-testid="unified-work-item"][data-source-type="ProjectTask"][data-source-id="${sources.projectTask.id}"]`,
    )
    .click()
  await expect(page).toHaveURL(new RegExp(`/projects/${sources.projectId}`))
  await expect(page.getByTestId('project-detail-page')).toBeVisible()

  await page.goto('/')
  await searchMyWork(page, sources.marker)
  await page
    .locator(
      `[data-testid="unified-work-item"][data-source-type="EventInbox"][data-source-id="${sources.pendingEventId}"]`,
    )
    .click()
  await expect(page).toHaveURL(new RegExp(`/confirmations\\?type=event&id=${sources.pendingEventId}`))
  await expect(page.getByTestId('confirmation-center-page')).toBeVisible()
  await page.getByRole('button', { name: '进入事件确认' }).click()
  await expect(page).toHaveURL(new RegExp(`/inbox\\?event=${sources.pendingEventId}`))
  await expect(page.locator(`[data-event-id="${sources.pendingEventId}"]`)).toHaveClass(/focused/)

  await page.goto('/')
  await searchMyWork(page, sources.marker)
  const minutesWorkItem = page
    .locator(`[data-testid="unified-work-item"][data-source-type="MinutesActionItem"]`)
    .first()
  await minutesWorkItem.click()
  await expect(page).toHaveURL(/\/confirmations\?type=minutes&id=/)
  await expect(page.getByTestId('confirmation-center-page')).toBeVisible()
  await expect(page.getByTestId('minutes-content')).toBeVisible()

  await page.goto('/')
  await searchMyWork(page, mcpConfirmationId)
  await page
    .locator(
      `[data-testid="unified-work-item"][data-source-type="McpHumanConfirmation"][data-source-id="${mcpConfirmationId}"]`,
    )
    .click()
  await expect(page).toHaveURL(new RegExp(`/confirmations\\?type=mcp&id=${mcpConfirmationId}`))
  await expect(page.getByTestId('confirmation-center-page')).toBeVisible()
  await page.getByRole('button', { name: '进入业务确认' }).click()
  await expect(page).toHaveURL(new RegExp(`/mcp-confirmations\\?confirmationId=${mcpConfirmationId}`))
  await expect(page.getByTestId('mcp-confirmation-page')).toBeVisible()

  await page.goto('/')
  await searchMyWork(page, sources.agentRunId)
  await page
    .locator(`[data-testid="unified-work-item"][data-source-type="AgentRun"][data-source-id="${sources.agentRunId}"]`)
    .click()
  await expect(page).toHaveURL(new RegExp(`/confirmations\\?type=agent&id=${sources.agentRunId}`))
  await expect(page.getByTestId('confirmation-center-page')).toBeVisible()
  await page.getByRole('button', { name: '进入人工确认' }).click()
  await expect(page).toHaveURL(new RegExp(`/agents\\?run=${sources.agentRunId}`))
  await expect(page.getByRole('tab', { name: '运行记录' })).toHaveAttribute('aria-selected', 'true')
  await expect(page.getByText(/已从深链定位运行/)).toBeVisible()

  await page.goto('/')
  await logout(page)
  await loginAs(page, '平台管理员')
  await expect(page.locator('[data-testid="unified-work-item"]').first()).toBeVisible()
  await searchMyWork(page, sources.marker)
  await expect(
    page.locator(
      `[data-testid="unified-work-item"][data-source-type="ProjectTask"][data-source-id="${sources.projectTask.id}"]`,
    ),
  ).toBeVisible()

  await apiFromPage(page, `/api/v1/project-tasks/${sources.projectTask.id}/actions/complete`, 'POST', {
    expectedVersion: sources.projectTask.version,
    comment: 'UX-003 E2E 完成事项',
  })
  await page.goto('/')
  await searchMyWork(page, sources.marker)
  await expect(
    page.locator(
      `[data-testid="unified-work-item"][data-source-type="ProjectTask"][data-source-id="${sources.projectTask.id}"]`,
    ),
  ).toHaveCount(0)
})

test('@desktop @desktop1280 @mobile unified confirmation edits minutes and reviews MCP business actions', async ({
  page,
}) => {
  test.slow()
  await loginAs(page, '平台管理员')
  const sources = await createUnifiedWorkSources(page)
  let mcpConfirmationId = await createMcpConfirmationFromDeployment(page)
  if (!mcpConfirmationId) {
    const confirmations = await apiFromPage<Array<{ id: string }>>(page, '/api/v1/mcp/confirmation-views/me')
    mcpConfirmationId = confirmations.find((item) => item.id)?.id ?? null
  }
  expect(mcpConfirmationId).toBeTruthy()
  expect(sources.minutesActionId).toBeTruthy()

  await page.goto(`/confirmations?type=minutes&id=${sources.minutesActionId}`)
  await expect(page.getByTestId('confirmation-center-page')).toBeVisible()
  await expect(page.getByTestId('minutes-content')).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, `ux004-confirmation-center-${page.viewportSize()?.width ?? 1440}.png`),
    fullPage: true,
  })
  await page.getByTestId('minutes-assignee').click()
  await page.getByRole('option', { name: /平台管理员/ }).click()
  await page.getByPlaceholder('选择截止时间').fill('2026-12-18 18:00')
  await page.keyboard.press('Tab')
  await page.getByTestId('minutes-project').click()
  await page
    .getByRole('option', { name: new RegExp(sources.projectName) })
    .evaluate((element) => (element as HTMLElement).click())
  await page.getByTestId('minutes-business-object').click()
  await page
    .getByRole('option', { name: new RegExp(sources.businessObjectCode) })
    .evaluate((element) => (element as HTMLElement).click())
  await page.getByTestId('minutes-existing-workflow').click()
  await page
    .getByRole('option')
    .filter({ hasText: /Running|Waiting/ })
    .first()
    .evaluate((element) => (element as HTMLElement).click())
  await page.getByTestId('minutes-content').fill(`${sources.marker} 人工确认后的听记任务`)
  const saveResponsePromise = page.waitForResponse(
    (response) =>
      response.url().includes(`/api/v1/minutes/${sources.minutesSourceId}/action-items/${sources.minutesActionId}`) &&
      response.request().method() === 'PUT',
  )
  await page.getByTestId('save-minutes-action').click()
  const saveResponse = await saveResponsePromise
  expect(saveResponse.ok(), await saveResponse.text()).toBeTruthy()
  await expect(page.getByTestId('confirm-minutes-action')).toBeEnabled()
  const confirmResponsePromise = page.waitForResponse(
    (response) =>
      response.url().endsWith(`/api/v1/minutes/action-items/${sources.minutesActionId}/actions/confirm`) &&
      response.request().method() === 'POST',
  )
  await page.getByTestId('confirm-minutes-action').click()
  const confirmResponse = await confirmResponsePromise
  expect(confirmResponse.ok(), await confirmResponse.text()).toBeTruthy()
  await page.reload()
  await expect(page.getByTestId('confirmation-center-page')).toBeVisible()
  await expect(page.getByText('已确认').first()).toBeVisible()

  const actions = await apiFromPage<
    Array<{
      id: string
      status: string
      assigneeId: string
      dueAt: string
      projectId: string
      businessObjectId: string
      workflowInstanceId: string
      minutesTaskId: string
    }>
  >(page, `/api/v1/minutes/${sources.minutesSourceId}/action-items`)
  const persisted = actions.find((item) => item.id === sources.minutesActionId)!
  expect(persisted.status).toBe('Confirmed')
  expect(persisted.assigneeId).toBeTruthy()
  expect(persisted.dueAt).toContain('2026-12-18T10:00:00')
  expect(persisted.projectId).toBe(sources.projectId)
  expect(persisted.businessObjectId).toBe(sources.businessObjectId)
  expect(persisted.workflowInstanceId).toBe(sources.workflowInstanceId)
  expect(persisted.minutesTaskId).toBeTruthy()

  await page.goto(`/confirmations?type=mcp&id=${mcpConfirmationId}`)
  await expect(page.getByTestId('confirmation-center-page')).toBeVisible()
  await page.getByRole('button', { name: '进入业务确认' }).click()
  await expect(page.getByTestId('mcp-confirmation-page')).toBeVisible()
  await expect(page.getByText('确认将执行', { exact: true })).toBeVisible()
  await expect(page.getByText('不会执行', { exact: true })).toBeVisible()
  await page.getByText('技术信息', { exact: true }).click()
  await expect(page.getByText('客户端', { exact: true })).toBeVisible()
  expect(await page.locator('body').innerText()).not.toMatch(/eyJ[A-Za-z0-9._-]{20,}/)
  await page.getByTestId('mcp-decision-comment').fill('已核对业务对象和影响范围')
  await page.getByTestId('confirm-mcp-confirmation').click()
  await expect(page.locator('.confirmation-card').getByText('已通过', { exact: true }).first()).toBeVisible()

  const rejectId = await createMcpConfirmationFromDeployment(page)
  expect(rejectId).toBeTruthy()
  await page.goto(`/mcp-confirmations?confirmationId=${rejectId}`)
  await page.getByTestId('mcp-decision-comment').fill('业务依据不足，拒绝')
  await page.getByTestId('reject-mcp-confirmation').click()
  await expect(page.locator('.confirmation-card').getByText('已拒绝', { exact: true }).first()).toBeVisible()
})

test('@desktop unauthorized work scope is rejected without leaking items', async ({ page }) => {
  await page.goto('/login')
  const csrf = await page.request.get('/api/v1/auth/csrf')
  const token = String(((await csrf.json()) as { data: { requestToken: string } }).data.requestToken)
  const login = await page.request.post('/api/v1/auth/demo-login', {
    headers: { 'X-CSRF-TOKEN': token },
    data: { userName: 'demo.noscope' },
  })
  expect(login.ok()).toBeTruthy()
  await page.goto('/')
  await expect(page.getByTestId('my-work-page')).toBeVisible()
  await expect(page.getByTestId('my-work-page').getByText(/没有该组织的数据范围/)).toBeVisible()
  await expect(page.locator('[data-testid="unified-work-item"]')).toHaveCount(0)

  await page.goto('/confirmations')
  await expect(page.getByTestId('confirmation-center-page')).toBeVisible()
  await expect(page.getByText(/部分来源无权限或加载失败/)).toBeVisible()
  await expect(page.getByText(/没有可访问的确认来源/)).toBeVisible()
})

test('@desktop1280 unified work center fits the compact desktop layout', async ({ page }) => {
  await loginAs(page, '平台管理员')
  await page.goto('/')
  await expect(page.getByTestId('my-work-page')).toBeVisible()
  await expect(page.getByRole('tab', { name: /待我处理/ })).toBeVisible()
  await expect(page.locator('[data-testid="unified-work-item"]').first()).toBeVisible()
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  )
  expect(overflow).toBeFalsy()
  await page.waitForTimeout(350)
  await page.screenshot({
    path: path.join(evidenceDir, 'desktop-1280-unified-work.png'),
  })
})

test('@mobile unified work center keeps task groups usable at 390px', async ({ page }) => {
  await loginAs(page, '平台管理员')
  await page.goto('/')
  await expect(page.getByTestId('my-work-page')).toBeVisible()
  await expect(page.getByRole('tab', { name: /待我处理/ })).toBeVisible()
  const cards = page.locator('[data-testid="unified-work-item"]')
  await page.waitForTimeout(350)
  await page.screenshot({
    path: path.join(evidenceDir, 'mobile-390-unified-work.png'),
  })
  if (await cards.count()) {
    await expect(cards.first()).toBeVisible()
    await cards.first().click()
    await expect(page).not.toHaveURL(/\/$/)
  }
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
  )
  expect(overflow).toBeFalsy()
})

test('@desktop file center uploads previews versions and records deletion request', async ({ page }) => {
  await loginAs(page, '平台管理员')
  await page.goto('/documents')
  await expect(page.getByTestId('documents-page')).toBeVisible()

  const marker = `E2E-FILE-${Date.now()}.pdf`
  await page.getByRole('button', { name: '上传文件', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: '上传部门文件' })
  const droppedFile = {
    name: marker,
    mimeType: 'application/pdf',
    base64: Buffer.from('%PDF-1.7\nAIDT E2E file').toString('base64'),
  }
  await dialog.getByTestId('file-drop-zone').evaluate((element, file) => {
    const bytes = Uint8Array.from(atob(file.base64), (character) => character.charCodeAt(0))
    const transfer = new DataTransfer()
    transfer.items.add(new File([bytes], file.name, { type: file.mimeType }))
    element.dispatchEvent(
      new DragEvent('drop', {
        bubbles: true,
        cancelable: true,
        dataTransfer: transfer,
      }),
    )
  }, droppedFile)
  await dialog.getByRole('button', { name: '上传', exact: true }).click()
  await expect(page.getByText('文件已上传并记录审计')).toBeVisible()

  const card = page.locator('.file-card').filter({ hasText: marker })
  await expect(card).toBeVisible()
  await expect(card.getByText('演示环境 · 测试数据')).toBeVisible()
  await card.getByRole('button', { name: '预览', exact: true }).click()
  await expect(page.getByTitle('PDF 文件预览')).toBeVisible()
  await page.keyboard.press('Escape')

  await card.getByRole('button', { name: '版本', exact: true }).click()
  const versionDialog = page.getByRole('dialog', { name: new RegExp(marker) })
  await expect(versionDialog).toBeVisible()
  const [fileChooser] = await Promise.all([
    page.waitForEvent('filechooser'),
    versionDialog.getByRole('button', { name: '上传新版本', exact: true }).click(),
  ])
  await fileChooser.setFiles({
    name: `v2-${marker}`,
    mimeType: 'application/pdf',
    buffer: Buffer.from('%PDF-1.7\nAIDT E2E file v2'),
  })
  await expect(page.getByText('新版本已保留，旧版本仍可追溯')).toBeVisible()

  await card.getByRole('button', { name: '申请删除', exact: true }).click()
  await page.getByRole('textbox').last().fill('E2E 完成，申请保留证据并标记删除')
  await page.getByRole('button', { name: '提交申请', exact: true }).click()
  await expect(page.getByText('删除申请已记录，文件内容不会被静默删除')).toBeVisible()
})

test('@desktop unapproved sensitive file is rejected for employee download', async ({ page }) => {
  const marker = `E2E-SENSITIVE-${Date.now()}.pdf`
  await loginAs(page, '平台管理员')
  await page.goto('/documents')
  await page.getByRole('button', { name: '上传文件', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: '上传部门文件' })
  await dialog.locator('input[type="file"]').setInputFiles({
    name: marker,
    mimeType: 'application/pdf',
    buffer: Buffer.from('%PDF-1.7\nAIDT sensitive E2E file'),
  })
  await dialog.getByTestId('sensitivity-select').click()
  await page.getByRole('option', { name: '敏感', exact: true }).click()
  await dialog.getByRole('button', { name: '上传', exact: true }).click()
  await expect(page.getByText('文件已上传并记录审计')).toBeVisible()

  await logout(page)
  await loginAs(page, '研发工程师')
  await page.goto('/documents')
  const sensitiveCard = page.locator('.file-card').filter({ hasText: marker })
  await expect(sensitiveCard).toBeVisible()
  await sensitiveCard.getByRole('button', { name: '下载', exact: true }).click()
  await expect(page.getByText(/敏感文件尚未完成病毒扫描/)).toBeVisible()
})

test('@desktop minutes require confirmation before action task creation', async ({ page }) => {
  const marker = `AI听记-${Date.now()}`
  await loginAs(page, '平台管理员')
  await page.goto('/minutes')
  await expect(page.getByTestId('minutes-page')).toBeVisible()
  await page.getByRole('button', { name: '导入听记', exact: true }).click()
  await page.getByPlaceholder('会议标题').fill(marker)
  await expect(page.getByTestId('minutes-transcript')).toHaveValue(/研发工程师/)
  await page.getByTestId('register-minutes-button').click()
  await expect(page.getByText('听记已登记，原始文字稿已保存')).toBeVisible()
  await expect(page.getByRole('heading', { name: marker })).toBeVisible()

  await page.getByTestId('analyze-minutes-button').click()
  await expect(page.getByTestId('confirm-minutes-run-button')).toBeVisible()
  await page.getByTestId('confirm-minutes-run-button').click()
  await expect(page.getByTestId('execute-minutes-run-button')).toBeVisible()
  await page.getByTestId('execute-minutes-run-button').click()
  await expect(page.getByText(/规则模拟完成，生成/)).toBeVisible()
  await expect(page.getByText('ExternalModelCalled=false')).toBeVisible()
  await expect(page.getByText('研发工程师', { exact: true }).first()).toBeVisible()

  const unifiedButtons = page.getByRole('button', {
    name: '统一确认',
    exact: true,
  })
  await expect(unifiedButtons.first()).toBeVisible()
  await unifiedButtons.first().click()
  await expect(page).toHaveURL(/\/confirmations\?type=minutes&id=/)
  await page.getByTestId('minutes-assignee').click()
  await page.getByRole('option', { name: /平台管理员/ }).click()
  await page.getByPlaceholder('选择截止时间').fill('2026-12-18 18:00')
  await page.keyboard.press('Tab')
  await page.getByTestId('confirm-minutes-action').click()
  await expect(page.getByText(/任务已创建/)).toBeVisible()
})

test('@desktop project manager creates project adds member and milestone', async ({ page }) => {
  const suffix = Date.now()
  const projectName = `耐候型苯丙乳液客户开发-${suffix}`
  await loginAs(page, '项目经理')
  await createProjectViaUi(page, projectName, '完成客户样品验证、试产和转产准备')
  await expect(page.getByTestId('project-detail-page')).toBeVisible()

  await page.getByTestId('add-member-button').click()
  const memberDialog = page.getByRole('dialog', { name: '添加项目成员' })
  await memberDialog.locator('.el-select').first().click()
  await page.getByRole('option', { name: /研发工程师/ }).click()
  await memberDialog.getByTestId('submit-member-button').click()
  await expect(page.getByText('项目成员已添加').last()).toBeVisible()
  await expect(page.getByText('研发工程师', { exact: true }).first()).toBeVisible()

  await page.getByTestId('add-milestone-button').click()
  const milestoneDialog = page.getByRole('dialog', { name: '新增里程碑' })
  await milestoneDialog.getByTestId('milestone-title-input').fill('客户样品确认')
  await milestoneDialog.locator('.el-date-editor input').fill('2026-11-20')
  await page.keyboard.press('Tab')
  await milestoneDialog.getByTestId('submit-milestone-button').click()
  await expect(page.getByText('里程碑已创建')).toBeVisible()
  await expect(page.getByText('客户样品确认', { exact: true })).toBeVisible()
})

test('@desktop @desktop1280 @mobile project operations persist edit members tasks evidence progress and status', async ({
  page,
}) => {
  test.setTimeout(180_000)
  await loginAs(page, '平台管理员')
  const source = await createUnifiedWorkSources(page)
  const projectUrl = `/projects/${source.projectId}`
  const editedName = `项目操作闭环-${Date.now()}`
  const taskTitle = `直接派发验收任务-${Date.now()}`
  const evidenceFileName = `pmo003-${Date.now()}.pdf`
  await page.goto(projectUrl)
  await expect(page.getByTestId('project-detail-page')).toBeVisible()

  const uploaded = await page.evaluate(
    async ({ projectId, departmentId, evidenceFileName }) => {
      const csrfResponse = await fetch('/api/v1/auth/csrf')
      const csrf = await csrfResponse.json()
      const form = new FormData()
      form.append(
        'file',
        new File(['%PDF-1.7\nPMO-003 project evidence'], evidenceFileName, {
          type: 'application/pdf',
        }),
      )
      form.append('departmentId', departmentId)
      form.append('sensitivity', 'Internal')
      form.append('businessObjectId', projectId)
      const response = await fetch('/api/v1/files', {
        method: 'POST',
        headers: { 'X-CSRF-TOKEN': csrf.data.requestToken },
        body: form,
      })
      const payload = await response.json()
      if (!response.ok) throw new Error(`${response.status} ${payload.message}`)
      return payload.data
    },
    {
      projectId: source.projectId,
      departmentId: source.departmentId,
      evidenceFileName,
    },
  )
  expect(uploaded.originalFileName).toBe(evidenceFileName)
  await page.reload()

  await page.getByTestId('add-member-button').click()
  let memberDialog = page.getByRole('dialog', { name: '添加项目成员' })
  await memberDialog.locator('.el-select').first().click()
  await page.getByRole('option', { name: /研发工程师/ }).click()
  await memberDialog.getByTestId('submit-member-button').click()
  await expect(page.getByText('项目成员已添加')).toBeVisible()

  await page.getByTestId('add-member-button').click()
  memberDialog = page.getByRole('dialog', { name: '添加项目成员' })
  await memberDialog.locator('.el-select').first().click()
  await page.getByRole('option', { name: /质量工程师/ }).click()
  await memberDialog.getByTestId('submit-member-button').click()
  await expect(page.getByText('项目成员已添加').last()).toBeVisible()

  const qualityRow = page.getByTestId('project-member-row').filter({ hasText: '质量工程师' })
  await qualityRow.getByTestId('project-member-role-select').click()
  await page.getByRole('option', { name: '只读成员', exact: true }).click()
  await expect(page.getByText('成员角色已更新').last()).toBeVisible()
  await qualityRow.getByTestId('remove-project-member-button').click()
  await page.getByRole('button', { name: '移除', exact: true }).last().click()
  await expect(page.getByText('项目成员已移除').last()).toBeVisible()

  await page.getByTestId('edit-project-button').click()
  const projectDialog = page.getByRole('dialog', { name: '编辑项目' })
  await projectDialog.getByTestId('project-name-input').fill(editedName)
  await projectDialog.getByTestId('project-owner-select').click()
  await page.getByRole('option', { name: /研发工程师/ }).click()
  await projectDialog.getByTestId('project-objective-input').fill('编辑后的项目目标，验证服务端持久化与刷新一致。')
  await projectDialog.getByTestId('project-stage-input').fill('任务执行')
  await projectDialog.getByTestId('save-project-button').click()
  await expect(page.getByText('项目信息与负责人已更新')).toBeVisible()
  await expect(page.getByTestId('project-detail-name')).toHaveText(editedName)
  await expect(page.getByTestId('project-member-row').filter({ hasText: '研发工程师' })).toContainText('主负责人')

  await page.getByTestId('create-project-task-button').click()
  const taskDialog = page.getByRole('dialog', { name: '创建项目任务' })
  await taskDialog.getByTestId('project-task-title-input').fill(taskTitle)
  await taskDialog.getByTestId('project-task-content-input').fill('负责人完成项目任务并刷新项目进度。')
  await taskDialog.getByTestId('project-task-assignee-select').click()
  await page.getByRole('option', { name: /研发工程师/ }).click()
  await taskDialog.locator('.el-date-editor input').fill('2026-12-18')
  await page.keyboard.press('Tab')
  await taskDialog.getByTestId('submit-project-task-button').click()
  await expect(page.getByText('项目任务已创建并分配给负责人')).toBeVisible()
  await expect(page.getByTestId('project-task-row').filter({ hasText: taskTitle })).toBeVisible()

  const addEvidence = async (typeLabel: string, optionText?: string) => {
    await page.getByRole('button', { name: '关联证据', exact: true }).click()
    const dialog = page.getByRole('dialog', { name: '关联项目证据' })
    await dialog.locator('.el-radio-button').filter({ hasText: typeLabel }).click()
    await dialog.getByTestId('project-evidence-select').click()
    const option = optionText
      ? page
          .getByRole('option', {
            name: new RegExp(optionText.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')),
          })
          .last()
      : page.getByRole('option').first()
    await option.click()
    await dialog.getByRole('textbox').last().fill(`PMO-003 ${typeLabel}证据`)
    await dialog.getByTestId('submit-project-evidence').click()
    await expect(page.getByText('项目证据关系已保存').last()).toBeVisible()
    await expect(dialog).toBeHidden()
  }

  await addEvidence('文件', evidenceFileName)
  await addEvidence('听记', source.minutesTitle)
  await addEvidence('流程')
  await addEvidence('业务对象', source.businessObjectCode)
  await page.reload()

  const detailAfterRelations = await apiFromPage<{
    relations: Array<{ entityType: string }>
  }>(page, `/api/v1/projects/${source.projectId}`)
  const relationTypes = new Set(detailAfterRelations.relations.map((item) => item.entityType))
  expect([...relationTypes]).toEqual(
    expect.arrayContaining(['Attachment', 'MinutesSource', 'WorkflowInstance', 'BusinessObject']),
  )

  for (const title of [source.projectTask.title, taskTitle]) {
    const taskRow = page.getByTestId('project-task-row').filter({ hasText: title })
    await expect(taskRow).toBeVisible()
    const completeResponse = page.waitForResponse(
      (response) =>
        response.url().includes('/api/v1/project-tasks/') &&
        response.url().endsWith('/actions/complete') &&
        response.request().method() === 'POST',
    )
    await taskRow.getByRole('button', { name: '完成', exact: true }).click()
    expect((await completeResponse).ok()).toBeTruthy()
    await expect(page.getByText('项目任务已完成，项目进度已重新计算').last()).toBeVisible()
  }
  const awaitingResponse = page.waitForResponse(
    (response) =>
      response.url().endsWith('/api/v1/projects/' + source.projectId + '/actions/change-status') &&
      response.request().method() === 'POST',
  )
  await page.getByTestId('project-status-select').click()
  await page
    .getByRole('option', { name: '待验收', exact: true })
    .evaluate((element) => (element as HTMLElement).click())
  expect((await awaitingResponse).ok()).toBeTruthy()
  await expect(page.getByText('项目状态已更新').last()).toBeVisible()
  const completedResponse = page.waitForResponse(
    (response) =>
      response.url().endsWith('/api/v1/projects/' + source.projectId + '/actions/change-status') &&
      response.request().method() === 'POST',
  )
  await page.getByTestId('project-status-select').click()
  await page
    .getByRole('option', { name: '已完成', exact: true })
    .evaluate((element) => (element as HTMLElement).click())
  expect((await completedResponse).ok()).toBeTruthy()
  await expect(page.getByText('项目状态已更新').last()).toBeVisible()

  await page.reload()
  await expect(page.getByTestId('project-detail-page')).toBeVisible()
  await expect(page.getByTestId('project-detail-name')).toHaveText(editedName)
  await expect(page.locator('.hero-tags')).toContainText('已完成')
  await expect(page.locator('.metrics article').filter({ hasText: '项目进度' })).toContainText('100%')
  await expect(
    page
      .getByTestId('project-task-row')
      .filter({ hasText: taskTitle })
      .getByRole('button', { name: '完成', exact: true }),
  ).toHaveCount(0)

  const persisted = await apiFromPage<{
    project: {
      name: string
      status: string
      progressPercent: number
      ownerUserId: string
    }
    tasks: Array<{ id: string; title: string; status: string }>
    relations: Array<{ entityType: string; evidenceExcerpt: string | null }>
  }>(page, `/api/v1/projects/${source.projectId}`)
  expect(persisted.project.name).toBe(editedName)
  expect(persisted.project.status).toBe('Completed')
  expect(persisted.project.progressPercent).toBe(100)
  expect(persisted.tasks.find((item) => item.title === taskTitle)?.status).toBe('Completed')
  expect(persisted.relations.some((item) => item.evidenceExcerpt?.includes('PMO-003'))).toBeTruthy()

  await page.screenshot({
    path: path.join(evidenceDir, `project-operations-${test.info().project.name}.png`),
    fullPage: true,
  })
})

test('@desktop batch3 closeout persists inbox project status time and project evidence after refresh', async ({
  page,
}) => {
  const suffix = Date.now()
  const projectName = `批次3收尾-${suffix}`
  const evidenceName = `project-auto-evidence-${suffix}.pdf`
  const eventTitle = `批次3状态持久化-${suffix}`
  await loginAs(page, '项目经理')
  await createProjectViaUi(page, projectName, '验证事件、项目状态、文件证据和上海截止时间刷新后保持')
  const projectUrl = page.url()
  const projectId = projectUrl.split('/').pop()!

  const statusSelect = page.getByTestId('project-status-select')
  await statusSelect.click()
  await page.getByRole('option', { name: '阻塞', exact: true }).click()
  await expect(page.getByText('项目状态已更新')).toBeVisible()
  await page.reload()
  await expect(page.getByTestId('project-detail-page')).toBeVisible()
  await expect(page.locator('.hero-tags')).toContainText('阻塞')

  await page.getByTestId('project-status-select').click()
  await page.getByRole('option', { name: '进行中', exact: true }).click()
  await expect(page.getByText('项目状态已更新')).toBeVisible()
  await page.reload()
  await expect(page.locator('.hero-tags')).toContainText('进行中')

  await page.getByTestId('project-status-select').click()
  await page.getByRole('option', { name: '待验收', exact: true }).click()
  await expect(page.getByText('项目状态已更新')).toBeVisible()
  await page.reload()
  await expect(page.locator('.hero-tags')).toContainText('待验收')

  const upload = await page.evaluate(
    async ({ projectId, evidenceName }) => {
      const projectResponse = await fetch(`/api/v1/projects/${projectId}`)
      const project = await projectResponse.json()
      const csrfResponse = await fetch('/api/v1/auth/csrf')
      const csrf = await csrfResponse.json()
      const form = new FormData()
      form.append(
        'file',
        new File(['%PDF-1.7\nproject evidence from closeout E2E'], evidenceName, {
          type: 'application/pdf',
        }),
      )
      form.append('departmentId', project.data.project.departmentId)
      form.append('sensitivity', 'Internal')
      form.append('businessObjectId', projectId)
      const response = await fetch('/api/v1/files', {
        method: 'POST',
        headers: {
          'X-CSRF-TOKEN': csrf.data.requestToken,
        },
        body: form,
      })
      const payload = await response.json()
      if (!response.ok) throw new Error(`${response.status} ${payload.message}`)
      return payload.data
    },
    { projectId, evidenceName },
  )
  expect(upload.originalFileName).toBe(evidenceName)

  await page.reload()
  await expect(page.getByTestId('project-detail-page')).toBeVisible()
  await expect(page.getByText(evidenceName, { exact: true })).toBeVisible()

  await page.goto('/inbox')
  await page.getByTestId('receive-event-button').click()
  const dialog = page.getByRole('dialog', { name: '接收企业事件' })
  await dialog.getByTestId('event-title-input').fill(eventTitle)
  await dialog
    .getByTestId('event-content-input')
    .fill(`项目 ${projectName} 需在截止日期：2025-08-15 前完成客户资料确认。`)
  await submitInboxDialog(page, dialog)

  let eventCard = page.locator('.event-card').filter({ hasText: eventTitle })
  await eventCard.getByRole('button', { name: 'Agent 分析建议', exact: true }).click()
  await expect(eventCard).toContainText('待确认')
  await expect(eventCard).toContainText(/2025\/0?8\/15.*18:00/)

  await page.reload()
  eventCard = page.locator('.event-card').filter({ hasText: eventTitle })
  await expect(eventCard).toContainText('待确认')
  await expect(eventCard.getByTestId('open-confirmation')).toBeVisible()

  await eventCard.getByTestId('open-confirmation').click()
  const confirmationDialog = page.getByRole('dialog', {
    name: '真人确认业务去向',
  })
  await expect(confirmationDialog.getByTestId('confirmation-impact')).toContainText('一次提交，同一事务生效')
  await confirmationDialog.getByTestId('submit-confirmation').click()
  await expect(page.getByText(/确认完成，已创建/)).toBeVisible()
  await page.reload()
  eventCard = page.locator('.event-card').filter({ hasText: eventTitle })
  await expect(eventCard).toContainText('已确认')
  await expect(eventCard.getByTestId('open-confirmation')).toHaveCount(0)

  await page.goto(projectUrl)
  await expect(page.getByTestId('project-detail-page')).toBeVisible()
  const taskRow = page.locator('.task-row').filter({ hasText: '客户资料确认' })
  await expect(taskRow).toBeVisible()
  await taskRow.getByRole('button', { name: '完成', exact: true }).click()
  await expect(page.getByText('项目任务已完成，项目进度已重新计算')).toBeVisible()

  await page.getByTestId('project-status-select').click()
  await page.getByRole('option', { name: '已完成', exact: true }).click()
  await expect(page.getByText('项目状态已更新')).toBeVisible()
  await page.reload()
  await expect(page.locator('.hero-tags')).toContainText('已完成')
  await expect(page.locator('.metrics article').filter({ hasText: '项目进度' })).toContainText('100%')
})

test('@desktop inbox keeps agent suggestions human-confirmed and creates idempotent project task', async ({ page }) => {
  const suffix = Date.now()
  const projectName = `事件转换项目-${suffix}`
  const eventTitle = `客户资料会议行动项-${suffix}`
  await loginAs(page, '项目经理')
  await createProjectViaUi(page, projectName, '验证会议事件到项目任务的完整证据链')
  await page.goto('/inbox')
  await expect(page.getByTestId('inbox-page')).toBeVisible()

  await page.getByTestId('receive-event-button').click()
  const dialog = page.getByRole('dialog', { name: '接收企业事件' })
  await dialog.getByTestId('event-title-input').fill(eventTitle)
  await dialog
    .getByTestId('event-content-input')
    .fill(`项目 ${projectName} 需在截止日期：2026-11-20 前完成客户样品资料确认。`)
  await submitInboxDialog(page, dialog)

  const eventCard = page.locator('.event-card').filter({ hasText: eventTitle })
  await expect(eventCard).toBeVisible()
  await eventCard.getByRole('button', { name: 'Agent 分析建议', exact: true }).click()
  await expect(eventCard.getByText('Agent 建议', { exact: true })).toBeVisible()
  await expect(eventCard).toContainText('建议项目')

  await eventCard.getByTestId('open-confirmation').click()
  const confirmationDialog = page.getByRole('dialog', {
    name: '真人确认业务去向',
  })
  await expect(confirmationDialog.getByTestId('confirmation-impact')).toContainText('关联项目')
  await confirmationDialog.getByTestId('submit-confirmation').click()
  await expect(page.getByText(/确认完成，已创建/)).toBeVisible()

  await page.goto('/projects')
  const projectCard = page.locator('.project-card').filter({ hasText: projectName })
  await projectCard.click()
  await expect(page.locator('.task-row')).toContainText('客户样品资料确认')
  await page.screenshot({
    path: path.join(evidenceDir, 'desktop-project-detail.png'),
    fullPage: true,
  })
})

test('@desktop @desktop1280 @mobile inbox edits context and confirms task with new workflow atomically', async ({
  page,
}) => {
  const suffix = Date.now()
  const marker = `UX004-A-${suffix}`
  const projectName = `事件确认流程项目-${suffix}`
  const eventTitle = `事件确认并发起流程-${suffix}`
  await loginAs(page, '平台管理员')
  const published = await publishProjectWorkflowViaApi(page, marker)
  await createProjectViaUi(page, projectName, '验证事件、任务和流程同事务确认及相互反查')
  const projectUrl = page.url()
  const projectId = projectUrl.split('/').pop()!

  await page.goto('/inbox')
  await page.getByTestId('receive-event-button').click()
  const createDialog = page.getByRole('dialog', { name: '接收企业事件' })
  await createDialog.getByTestId('event-title-input').fill(eventTitle)
  await createDialog
    .getByTestId('event-content-input')
    .fill(`项目 ${projectName} 需在截止日期：2026-12-20 前完成客户样品确认。`)
  await submitInboxDialog(page, createDialog)

  const eventCard = page.locator('.event-card').filter({ hasText: eventTitle })
  await eventCard.getByRole('button', { name: 'Agent 分析建议', exact: true }).click()
  await expect(eventCard.getByTestId('open-confirmation')).toBeVisible()
  await eventCard.getByTestId('open-confirmation').click()

  const dialog = page.getByRole('dialog', { name: '真人确认业务去向' })
  await expect(dialog.getByTestId('confirmation-impact')).toContainText(projectName)
  await expect(dialog.getByTestId('confirmation-impact')).toContainText('不会执行')
  await expect(dialog.getByTestId('confirmation-assignee')).toContainText('平台管理员')
  await expect(dialog.getByTestId('confirmation-impact')).toContainText(/2026\/12\/20 18:00/)
  await dialog.getByText('发起新流程', { exact: true }).click()
  await dialog.getByTestId('confirmation-new-flow').click()
  await page
    .getByRole('option', { name: new RegExp(`${marker} 项目事件确认流程`) })
    .evaluate((element) => (element as HTMLElement).click())
  await dialog.getByTestId('submit-confirmation').click()
  await expect(page.getByText(/确认完成，已创建任务.*新的业务流程/)).toBeVisible()

  await page.reload()
  await expect(page.locator('.event-card').filter({ hasText: eventTitle })).toContainText('已确认')
  const projectDetail = await apiFromPage<{
    tasks: Array<{
      eventInboxId: string
      workflowInstanceId: string
      workflowTaskId: string
    }>
    relatedWorkflows: Array<{
      instance: { id: string }
      definitionName: string
    }>
  }>(page, `/api/v1/projects/${projectId}`)
  expect(projectDetail.tasks).toHaveLength(1)
  expect(projectDetail.tasks[0].eventInboxId).toBeTruthy()
  expect(projectDetail.tasks[0].workflowTaskId).toBeTruthy()
  expect(projectDetail.relatedWorkflows).toHaveLength(1)
  expect(projectDetail.relatedWorkflows[0].instance.id).toBe(projectDetail.tasks[0].workflowInstanceId)
  expect(projectDetail.relatedWorkflows[0].definitionName).toBeTruthy()

  await page.goto(projectUrl)
  await expect(page.getByTestId('project-detail-page')).toBeVisible()
  await expect(page.locator('.task-row').first()).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, `ux004-confirmation-${page.viewportSize()?.width ?? 1440}.png`),
    fullPage: true,
  })
})

test('@desktop high-risk inbox event remains human boundary', async ({ page }) => {
  const suffix = Date.now()
  const eventTitle = `高风险边界-${suffix}`
  await loginAs(page, '项目经理')
  await page.goto('/inbox')
  await page.getByTestId('receive-event-button').click()
  const dialog = page.getByRole('dialog', { name: '接收企业事件' })
  await dialog.getByTestId('event-title-input').fill(eventTitle)
  await dialog.getByTestId('event-content-input').fill('请直接完成财务付款并办理质量放行。')
  await submitInboxDialog(page, dialog)

  const eventCard = page.locator('.event-card').filter({ hasText: eventTitle })
  await eventCard.getByRole('button', { name: 'Agent 分析建议', exact: true }).click()
  await expect(eventCard.getByText('高风险待人工判断', { exact: true })).toBeVisible()
  await expect(eventCard).toContainText('永久人工边界')
  await expect(eventCard).toContainText('不会自动付款')
})

test('@desktop workflow policy page saves and publishes low-risk auto proxy', async ({ page }) => {
  test.slow()
  await loginAs(page, '平台管理员')
  await page.goto('/workflow-policies')
  await expect(page.getByTestId('workflow-policies-page')).toBeVisible()
  await page.getByTestId('policy-version').click()
  await page.getByRole('option', { name: /研发资料完整性确认/ }).click()
  await page.getByTestId('policy-node-option').filter({ hasText: 'MATERIALS_CONFIRM' }).click()

  await page.getByTestId('save-policy-draft').click()
  await expect(page.getByText('策略草稿已保存')).toBeVisible()
  await page.getByTestId('publish-policy').click()
  await expect(page.getByText('策略已发布，新任务将冻结此版本')).toBeVisible()
  await expect(page.getByText(/PreAuthorizedAutoProxy|MATERIALS_CONFIRM/).first()).toBeVisible()

  const objectCode = `MAT-${Date.now()}`
  await page.goto('/objects')
  await page.getByTestId('object-type-select').click()
  await page.getByRole('option', { name: /客户样品/ }).click()
  await page.getByRole('textbox', { name: '业务编号' }).fill(objectCode)
  await page.getByRole('textbox', { name: /客户 \*/ }).fill('资料确认 E2E 客户')
  await page.getByRole('textbox', { name: /产品 \*/ }).fill('资料确认 E2E 产品')
  await page.getByRole('textbox', { name: /客户需求 \*/ }).fill('验证默认自动代理策略')
  await page.getByTestId('create-object-button').click()
  await expect(page.getByText('业务对象已持久化')).toBeVisible()

  await page.goto('/workflows')
  await page.getByTestId('workflow-definition-select').click()
  await page.getByRole('option', { name: /研发资料完整性确认/ }).click()
  await page.getByTestId('workflow-object-select').click()
  await page.getByRole('option', { name: new RegExp(objectCode) }).click()
  const [startResponse] = await Promise.all([
    page.waitForResponse(
      (response) => response.url().endsWith('/api/v1/workflow-instances') && response.request().method() === 'POST',
    ),
    page.getByTestId('start-workflow-button').click(),
  ])
  const instanceId = String((await startResponse.json()).data.id)
  const taskPageResponse = await page.request.get('/api/v1/workflow-tasks?pageSize=100')
  const taskPage = await taskPageResponse.json()
  const task = taskPage.data.items.find(
    (item: { workflowInstanceId: string }) => item.workflowInstanceId === instanceId,
  )
  expect(task.dueAt).toBeTruthy()

  const scanResults = await page.evaluate(
    async ({ taskId }) => {
      const csrfResponse = await fetch('/api/v1/auth/csrf')
      const csrf = await csrfResponse.json()
      const headers = {
        'X-CSRF-TOKEN': csrf.data.requestToken,
        'Idempotency-Key': crypto.randomUUID(),
      }
      await fetch(`/api/v1/workflow-sla/tasks/${taskId}/actions/simulate-timeout`, {
        method: 'POST',
        headers,
      })
      const first = await (
        await fetch('/api/v1/workflow-sla/actions/run', {
          method: 'POST',
          headers: { 'X-CSRF-TOKEN': csrf.data.requestToken },
        })
      ).json()
      const second = await (
        await fetch('/api/v1/workflow-sla/actions/run', {
          method: 'POST',
          headers: { 'X-CSRF-TOKEN': csrf.data.requestToken },
        })
      ).json()
      return [first.data, second.data]
    },
    { taskId: task.id },
  )
  expect((scanResults[0]?.agentProxied ?? 0) + (scanResults[1]?.agentProxied ?? 0)).toBeLessThanOrEqual(1)

  await page.goto(`/workflows?refresh=${Date.now()}`)
  await expect(workflowTaskCard(page, instanceId)).toContainText(/AgentProxy|Agent 代理/, {
    timeout: 45_000,
  })
})

test('@desktop employee cannot open workflow policy administration', async ({ page }) => {
  await loginAs(page, '研发工程师')
  await page.goto('/workflow-policies')
  await expect(page).toHaveURL(/\/access-denied/)
})

test('@desktop META-001-A object model lifecycle and object editing persist', async ({ page }) => {
  await loginAs(page, '平台管理员')
  const marker = `META_E2E_${Date.now()}`
  const fields = [
    {
      key: 'Name',
      displayName: '名称',
      dataType: 'Text',
      isRequired: true,
      maxLength: 100,
      sensitivity: 'Internal',
      showInList: true,
      filterable: true,
      sortOrder: 10,
      code: 'Name',
    },
    {
      key: 'Status',
      displayName: '状态',
      dataType: 'Select',
      isRequired: true,
      maxLength: 20,
      sensitivity: 'Internal',
      options: ['Open', 'Closed'],
      showInList: true,
      filterable: true,
      sortOrder: 20,
      code: 'Status',
    },
  ]
  const created = await apiFromPage<{
    id: string
    revision: number
    version: number
    status: string
  }>(page, '/api/v1/object-types', 'POST', { code: marker, name: 'META E2E 模型', fields }, crypto.randomUUID())
  expect(created.status).toBe('Draft')

  const updated = await apiFromPage<{ revision: number; version: number }>(
    page,
    `/api/v1/object-types/${created.id}/draft`,
    'PUT',
    {
      expectedRevision: created.revision,
      name: 'META E2E 模型 V2',
      fields: fields.concat({
        key: 'Memo',
        displayName: '说明',
        dataType: 'LongText',
        isRequired: false,
        maxLength: 2000,
        sensitivity: 'Sensitive',
        showInList: false,
        filterable: false,
        sortOrder: 30,
        code: 'Memo',
      }),
    },
    crypto.randomUUID(),
  )
  expect(updated.version).toBe(2)
  const validated = await apiFromPage<{ isValid: boolean; revision: number }>(
    page,
    `/api/v1/object-types/${created.id}/actions/validate`,
    'POST',
    { expectedRevision: updated.revision },
    crypto.randomUUID(),
  )
  expect(validated.isValid).toBe(true)
  const published = await apiFromPage<{ status: string; revision: number }>(
    page,
    `/api/v1/object-types/${created.id}/actions/publish`,
    'POST',
    { expectedRevision: validated.revision },
    crypto.randomUUID(),
  )
  expect(published.status).toBe('Published')

  const departments = await apiFromPage<Array<{ id: string; code: string }>>(page, '/api/v1/departments')
  const object = await apiFromPage<{
    id: string
    version: number
    fields: Record<string, string>
  }>(
    page,
    '/api/v1/business-objects',
    'POST',
    {
      objectTypeId: created.id,
      objectCode: `${marker}-OBJ`,
      departmentId: departments.find((item) => item.code === 'RND')!.id,
      fields: { Name: 'META E2E 对象', Status: 'Open' },
    },
    crypto.randomUUID(),
  )
  const edited = await apiFromPage<{
    version: number
    fields: Record<string, string>
  }>(
    page,
    `/api/v1/business-objects/${object.id}`,
    'PUT',
    {
      expectedVersion: object.version,
      fields: { Name: 'META E2E 已编辑', Status: 'Closed' },
      objectTypeId: created.id,
      objectCode: `${marker}-OBJ`,
    },
    crypto.randomUUID(),
  )
  expect(edited.version).toBe(2)
  expect(edited.fields.Name).toBe('META E2E 已编辑')

  const retired = await apiFromPage<{ status: string; revision: number }>(
    page,
    `/api/v1/object-types/${created.id}/actions/retire`,
    'POST',
    { expectedRevision: published.revision },
    crypto.randomUUID(),
  )
  expect(retired.status).toBe('Retired')
  const restored = await apiFromPage<{ status: string }>(
    page,
    `/api/v1/object-types/${created.id}/actions/restore`,
    'POST',
    { expectedRevision: retired.revision },
    crypto.randomUUID(),
  )
  expect(restored.status).toBe('Published')
  const versions = await apiFromPage<Array<{ version: number; status: string }>>(
    page,
    `/api/v1/object-types/${created.id}/versions`,
  )
  expect(versions.length).toBeGreaterThanOrEqual(2)
})

test('@desktop business modeling studio publishes a dynamic form and persists object edits', async ({ page }) => {
  test.setTimeout(180_000)
  await fs.mkdir(evidenceDir, { recursive: true })
  await loginAs(page, '平台管理员')
  const marker = `VISIT_${Date.now()}`
  const typeName = `客户回访记录-${Date.now()}`

  await page.goto('/modeling')
  await expect(page.getByTestId('business-modeling-page')).toBeVisible()
  await page.getByTestId('create-object-type').click()
  const createTypeDialog = page.getByRole('dialog', {
    name: '新建对象类型草稿',
  })
  await createTypeDialog.getByTestId('new-type-code').fill(marker)
  await createTypeDialog.getByTestId('new-type-name').fill(typeName)
  await createTypeDialog.getByRole('button', { name: '创建草稿', exact: true }).click()
  await expect(page.getByTestId(`object-type-item-${marker}`)).toBeVisible()

  await page.getByRole('textbox', { name: '对象类型名称' }).fill(typeName)
  const firstField = page.getByTestId('field-card-0')
  await firstField.getByRole('textbox', { name: '字段编码' }).fill('Name')
  await firstField.getByRole('textbox', { name: '显示名称' }).fill('姓名')
  await firstField.getByText('必填', { exact: true }).click()

  await page.getByRole('button', { name: '添加字段', exact: true }).click()
  const secondField = page.getByTestId('field-card-1')
  await secondField.getByRole('textbox', { name: '字段编码' }).fill('Name')
  await secondField.getByRole('textbox', { name: '显示名称' }).fill('处理状态')
  await secondField.getByTestId('field-type-1').click()
  await page.getByRole('option', { name: '下拉选择', exact: true }).click()
  await secondField.getByText('必填', { exact: true }).click()
  await expect(page.getByTestId('model-validation-panel')).toContainText('字段编码 Name 重复')
  await secondField.getByRole('textbox', { name: '字段编码' }).fill('Status')
  await secondField.getByTestId('field-options-1').click()
  await secondField.getByTestId('field-options-1').locator('input').fill('Open')
  await page.keyboard.press('Enter')
  await secondField.getByTestId('field-options-1').locator('input').fill('Closed')
  await page.keyboard.press('Enter')
  await page.keyboard.press('Escape')

  await page.getByRole('button', { name: '添加字段', exact: true }).click()
  const ownerField = page.getByTestId('field-card-2')
  await ownerField.getByRole('textbox', { name: '字段编码' }).fill('Owner')
  await ownerField.getByRole('textbox', { name: '显示名称' }).fill('负责人')
  await ownerField.getByTestId('field-type-2').click()
  await page.getByRole('option', { name: '人员', exact: true }).click()
  await ownerField.getByText('必填', { exact: true }).click()

  await page.getByRole('button', { name: '添加字段', exact: true }).click()
  const departmentField = page.getByTestId('field-card-3')
  await departmentField.getByRole('textbox', { name: '字段编码' }).fill('Department')
  await departmentField.getByRole('textbox', { name: '显示名称' }).fill('责任部门')
  await departmentField.getByTestId('field-type-3').click()
  await page.getByRole('option', { name: '部门', exact: true }).click()
  await departmentField.getByText('必填', { exact: true }).click()

  await page.getByRole('button', { name: '添加字段', exact: true }).click()
  const dateField = page.getByTestId('field-card-4')
  await dateField.getByRole('textbox', { name: '字段编码' }).fill('VisitAt')
  await dateField.getByRole('textbox', { name: '显示名称' }).fill('回访时间')
  await dateField.getByTestId('field-type-4').click()
  await page.getByRole('option', { name: '日期时间', exact: true }).click()

  await page.getByTestId('preview-object-form').click()
  const previewDialog = page.getByRole('dialog', { name: '表单预览' })
  await expect(previewDialog).toContainText('姓名')
  await expect(previewDialog).toContainText('负责人')
  await page.keyboard.press('Escape')

  await page.getByTestId('save-model-draft').click()
  await expect(page.getByText('新草稿版本已持久化，线上发布版本保持不变')).toBeVisible()
  await page.getByTestId('validate-model').click()
  await expect(page.getByText('服务端校验通过 · Revision')).toBeVisible()
  await page.getByTestId('publish-object-type').click()
  const publishDialog = page.getByRole('dialog', { name: '发布业务表单' })
  await publishDialog.getByRole('button', { name: '确认发布', exact: true }).click()
  await expect(page.getByText(new RegExp(`已发布 ${marker} v`))).toBeVisible()

  await page.getByRole('button', { name: '版本历史', exact: true }).click()
  const historyDialog = page.getByRole('dialog', { name: '对象类型版本历史' })
  await expect(historyDialog).toContainText('已发布')
  await page.keyboard.press('Escape')

  await page.goto('/configurations')
  await page.getByRole('button', { name: '新建配置', exact: true }).click()
  const configurationDialog = page.getByRole('dialog', {
    name: '新建流程配置',
  })
  await configurationDialog.getByTestId('configuration-object-type').click()
  await expect(page.getByRole('option', { name: new RegExp(`${typeName}.*${marker}`) })).toBeVisible()
  await page.keyboard.press('Escape')
  await page.keyboard.press('Escape')

  await page.goto('/iam')
  await page.getByRole('tab', { name: '工作台模板' }).click()
  await expect(page.getByTestId('iam-workbench-panel')).toBeVisible()
  await page.getByRole('button', { name: '添加卡片', exact: true }).first().click()
  const cardDialog = page.getByRole('dialog', { name: '添加工作台卡片' })
  await cardDialog.getByTestId('workbench-card-type-select').click()
  await page.getByRole('option', { name: '业务对象', exact: true }).click()
  await expect(cardDialog.getByTestId('workbench-object-types')).toBeVisible()
  await cardDialog.getByTestId('workbench-object-types').click()
  await expect(page.getByRole('option', { name: new RegExp(`${typeName}.*${marker}`) })).toBeVisible()
  await cardDialog.getByRole('button', { name: '取消', exact: true }).click()

  await logout(page)
  await loginAs(page, '研发工程师')
  await page.goto('/objects')
  await page.getByTestId('object-type-select').click()
  await expect(page.getByRole('option', { name: new RegExp(`${typeName}.*${marker}`) })).toBeVisible()
  await page.getByRole('option', { name: new RegExp(`${typeName}.*${marker}`) }).click()

  const objectCode = `${marker}-OBJ`
  await page.getByTestId('object-code-input').fill(objectCode)
  await page.getByLabel('姓名 *', { exact: true }).fill('张三')
  await page.getByRole('combobox', { name: '处理状态 *' }).click()
  await page.getByRole('option', { name: '选项 1', exact: true }).click()
  await page.getByRole('combobox', { name: '负责人 *' }).click()
  await page.getByRole('combobox', { name: '负责人 *' }).fill('demo.researcher')
  await expect(page.getByRole('option', { name: /研发工程师/ }).first()).toBeVisible()
  await page
    .getByRole('option', { name: /研发工程师/ })
    .first()
    .click()
  await page.getByRole('combobox', { name: '责任部门 *' }).click()
  await page.getByRole('option', { name: '研发', exact: true }).click()

  const [createResponse] = await Promise.all([
    page.waitForResponse(
      (response) => response.url().endsWith('/api/v1/business-objects') && response.request().method() === 'POST',
    ),
    page.getByTestId('create-object-button').click(),
  ])
  const createdObject = (await createResponse.json()).data as {
    id: string
    version: number
  }
  await expect(page.getByTestId('object-detail-dialog')).toBeVisible()
  await expect(page.getByText('业务对象已持久化')).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, 'desktop-business-modeling-studio.png'),
    fullPage: true,
  })

  await page.getByTestId('edit-business-object').click()
  await apiFromPage(
    page,
    `/api/v1/business-objects/${createdObject.id}`,
    'PUT',
    {
      expectedVersion: createdObject.version,
      fields: { Name: '后台并发更新', Status: 'Closed' },
    },
    crypto.randomUUID(),
  )
  await page.getByTestId('save-business-object').click()
  await expect(page.getByText(/请关闭编辑并重新读取最新版本/)).toBeVisible()
  await page.keyboard.press('Escape')
  await page.goto('/objects')
  await page.getByRole('row').filter({ hasText: objectCode }).click()
  await expect(page.getByTestId('object-detail-dialog')).toBeVisible()

  await page.getByTestId('edit-business-object').click()
  const editDialog = page.getByRole('dialog', { name: '编辑业务对象' })
  await editDialog.getByLabel('姓名 *', { exact: true }).fill('李四')
  await editDialog.getByTestId('save-business-object').click()
  await expect(page.getByTestId('object-detail-dialog')).toContainText('v3')

  await page.getByRole('tab', { name: /版本 ·/ }).click()
  await expect(page.getByTestId('object-detail-dialog')).toContainText('v2')
  await expect(page.getByTestId('object-detail-dialog')).toContainText('v3')
  await page.getByRole('tab', { name: /关联证据 ·/ }).click()
  await expect(page.getByTestId('object-detail-dialog')).toContainText('审计')
  await expect(page.getByTestId('object-detail-dialog')).toContainText('Update')
  await page.keyboard.press('Escape')

  await logout(page)
  await loginAs(page, '平台管理员')
  const departments = await apiFromPage<Array<{ id: string; code: string }>>(page, '/api/v1/departments')
  const departmentId = departments.find((item) => item.code === 'RND')!.id
  const session = await apiFromPage<{ userId: string }>(page, '/api/v1/auth/session')
  const projectName = `动态对象关联项目-${Date.now()}`
  const project = await apiFromPage<{ id: string }>(
    page,
    '/api/v1/projects',
    'POST',
    {
      objectCode: `PRJ-${Date.now()}`,
      name: projectName,
      ownerUserId: session.userId,
      departmentId,
      plannedStartDate: new Date().toISOString(),
      plannedEndDate: new Date(Date.now() + 2_592_000_000).toISOString(),
      objective: '验证业务对象跨模块关联',
      currentStage: '执行中',
      confidentialityLevel: 'Internal',
      initialStatus: 'Active',
    },
    crypto.randomUUID(),
  )
  await apiFromPage(page, `/api/v1/projects/${project.id}/relations`, 'POST', {
    entityType: 'BusinessObject',
    entityId: createdObject.id,
    relationCode: 'SupportingEvidence',
    evidenceExcerpt: '动态对象 E2E 证据',
  })

  const fileCsrf = await csrfFromPage(page)
  const fileResponse = await page.request.post('/api/v1/files', {
    headers: { 'X-CSRF-TOKEN': fileCsrf },
    multipart: {
      file: {
        name: 'dynamic-object-evidence.txt',
        mimeType: 'text/plain',
        buffer: Buffer.from('AIDT META-001-B dynamic object evidence'),
      },
      departmentId,
      sensitivity: 'Internal',
      businessObjectId: createdObject.id,
    },
  })
  expect(fileResponse.status()).toBe(201)

  const configuration = await apiFromPage<{ id: string }>(
    page,
    '/api/v1/configurations',
    'POST',
    {
      configurationKey: `META_B_${Date.now()}`,
      name: '动态对象流程联动',
      description: '验证发布对象类型可立即用于流程配置',
      departmentId,
      objectTypeCode: marker,
      startNode: 'START',
      nodes: [
        {
          code: 'START',
          name: '动态对象确认',
          assigneeUserName: 'demo.admin',
          nextNode: null,
          completeOnApprove: true,
          condition: null,
          assigneeStrategy: 'SpecificUser',
          businessRoleCode: null,
          agentProfileId: null,
        },
      ],
    },
    crypto.randomUUID(),
  )
  await apiFromPage(
    page,
    `/api/v1/configurations/${configuration.id}/actions/validate`,
    'POST',
    undefined,
    crypto.randomUUID(),
  )
  const publishedFlow = await apiFromPage<{
    publishedWorkflowDefinitionId: string
    publishedWorkflowVersionId: string
  }>(page, `/api/v1/configurations/${configuration.id}/actions/publish`, 'POST', undefined, crypto.randomUUID())
  await apiFromPage(page, '/api/v1/workflow-instances', 'POST', {
    workflowDefinitionId: publishedFlow.publishedWorkflowDefinitionId,
    workflowVersionId: publishedFlow.publishedWorkflowVersionId,
    businessObjectId: createdObject.id,
    idempotencyKey: crypto.randomUUID(),
  })

  await page.goto('/objects')
  await page.getByRole('row').filter({ hasText: objectCode }).click()
  await page.getByRole('tab', { name: /关联证据 ·/ }).click()
  const detailDialog = page.getByTestId('object-detail-dialog')
  await expect(detailDialog).toContainText(projectName)
  await expect(detailDialog).toContainText('dynamic-object-evidence.txt')
  await expect(detailDialog).toContainText('Running')
  await expect(detailDialog).toContainText('Update')
  await page.keyboard.press('Escape')

  const draftCode = `HIDDEN_${Date.now()}`
  await apiFromPage(
    page,
    '/api/v1/object-types',
    'POST',
    {
      code: draftCode,
      name: '未发布隐藏表单',
      fields: [
        {
          key: 'Name',
          displayName: '名称',
          dataType: 'Text',
          isRequired: true,
          maxLength: 100,
          sensitivity: 'Internal',
          defaultValue: null,
          options: [],
          showInList: true,
          filterable: false,
          sortOrder: 10,
          code: 'Name',
        },
      ],
    },
    crypto.randomUUID(),
  )
  await logout(page)
  await loginAs(page, '研发工程师')
  await page.goto('/objects')
  await page.getByTestId('object-type-select').click()
  await expect(page.getByRole('option', { name: new RegExp(draftCode) })).toHaveCount(0)
  await page.keyboard.press('Escape')
  await page.goto('/modeling')
  await expect(page).toHaveURL(/\/access-denied/)
})

test('@desktop1280 @mobile modeling and dynamic object pages remain usable without overflow', async ({ page }) => {
  await loginAs(page, '平台管理员')
  for (const route of ['/modeling', '/objects']) {
    await page.goto(route)
    await expect(page.locator('body')).toBeVisible()
    const dimensions = await page.evaluate(() => ({
      viewport: window.innerWidth,
      content: document.documentElement.scrollWidth,
    }))
    expect(dimensions.content).toBeLessThanOrEqual(dimensions.viewport + 1)
  }
  await page.goto('/modeling')
  await expect(page.getByTestId('business-modeling-page')).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, `modeling-${page.viewportSize()?.width ?? 1440}.png`),
    fullPage: true,
  })
})

test('@desktop employee creates an inbox event from the workbench', async ({ page }) => {
  const title = `员工事项-${Date.now()}`
  await loginAs(page, '项目经理')
  await page.getByTestId('my-work-create-trigger').click()
  await page.locator('.el-dropdown-menu__item').filter({ hasText: '研发测试申请' }).click()
  const dialog = page.getByRole('dialog', { name: '发起事项' })
  await dialog.getByLabel('事项标题').fill(title)
  await dialog.getByLabel('事项要求与说明').fill('需在截止日期：2026-11-20 前完成员工工作台事项提交联调。')
  await dialog.getByRole('button', { name: '提交并获取建议', exact: true }).click()
  await expect(page).toHaveURL(/\/inbox/)
  await expect(page.getByText(`[研发测试申请] ${title}`, { exact: true })).toBeVisible()
})

test('@desktop configuration center creates validates and publishes executable config', async ({ page }) => {
  const suffix = Date.now()
  const configurationKey = `CFG-E2E-${suffix}`
  await loginAs(page, '研发工程师')
  await page.goto('/configurations')
  await expect(page.getByTestId('configuration-center-page')).toBeVisible()
  await page.getByRole('button', { name: '新建配置', exact: true }).click()

  const createDialog = page.getByRole('dialog', { name: '新建流程配置' })
  await createDialog.getByLabel('配置编号').fill(configurationKey)
  await createDialog.getByLabel('配置名称').fill('项目测试复核审批 E2E')
  await createDialog.getByLabel('业务对象类型').click()
  await page.getByRole('option', { name: /PROJECT/ }).click()
  await createDialog.getByLabel('配置说明').fill('验证配置从草稿到发布的前端执行链路。')
  await createDialog.getByRole('button', { name: '创建草稿', exact: true }).click()
  await expect(page.getByText(configurationKey, { exact: true })).toBeVisible()
  await expect(page.getByTestId('configuration-node-0')).toContainText('发起事项')

  await page.getByRole('button', { name: '校验', exact: true }).click()
  await expect(page.getByText('配置校验通过，可以提交发布。')).toBeVisible()

  await logout(page)
  await loginAs(page, '平台管理员')
  await page.goto('/configurations')
  await page.getByTestId('configuration-state-filter').click()
  await page.getByRole('option', { name: '已校验', exact: true }).click()
  await page.locator('.version-item').filter({ hasText: configurationKey }).click()
  await page.getByRole('button', { name: '发布', exact: true }).click()
  const publishDialog = page.getByRole('dialog', { name: '确认发布配置' })
  await publishDialog.getByRole('button', { name: '发布', exact: true }).click()
  await expect(page.getByText(/已发布流程版本/)).toBeVisible()
  await expect(page.getByText('当前可执行版本：', { exact: false })).toBeVisible()
  await fs.mkdir(evidenceDir, { recursive: true })
  await page.screenshot({
    path: path.join(evidenceDir, 'desktop-configuration-center.png'),
    fullPage: true,
  })
})

test('@desktop configuration page selects an active agent profile without GUID input', async ({ page }) => {
  await loginAs(page, '研发工程师')
  await page.goto('/configurations')
  await page.getByRole('button', { name: '新建配置', exact: true }).click()
  const createDialog = page.getByRole('dialog', { name: '新建流程配置' })
  await createDialog.getByLabel('配置编号').fill(`AGENT-UI-${Date.now()}`)
  await createDialog.getByLabel('配置名称').fill('数字员工选择器 E2E')
  await createDialog.getByLabel('业务对象类型').click()
  await page.getByRole('option', { name: /PROJECT/ }).click()
  await createDialog.getByLabel('配置说明').fill('验证流程节点只能选择有效数字员工。')
  await createDialog.getByRole('button', { name: '创建草稿', exact: true }).click()
  await expect(page.getByTestId('configuration-node-0')).toBeVisible()

  await page.getByTestId('configuration-node-0').click()
  await page.getByTestId('assignee-strategy-select').click()
  await page.getByRole('option', { name: '数字员工', exact: true }).click()
  const agentSelect = page.getByTestId('agent-profile-select')
  await expect(agentSelect).toBeVisible()
  await agentSelect.click()
  await expect(page.getByRole('option', { name: /业务协同数字员工/ })).toBeVisible()
  await page.getByRole('option', { name: /业务协同数字员工/ }).click()
  await expect(page.getByText('AgentProfile GUID')).toHaveCount(0)
  await page.getByRole('button', { name: '保存草稿', exact: true }).click()
  await expect(page.getByText('配置草稿已保存，Revision 已更新')).toBeVisible()
})

test('@desktop IAM creates publishes hides and restores department navigation', async ({ page }) => {
  await loginAs(page, '平台管理员')
  await page.goto('/iam')
  await expect(page.getByTestId('iam-center-page')).toBeVisible()
  await page.getByRole('tab', { name: '导航与工作区' }).click()
  await expect(page.getByTestId('iam-navigation-panel')).toBeVisible()

  const suffix = Date.now()
  const workspaceKey = `NAV-E2E-${suffix}`
  const workspaceName = `研发部验收导航-${suffix}`
  await page.getByTestId('create-navigation-button').click()
  const dialog = page.getByRole('dialog', { name: '创建导航工作区' })
  await dialog.getByTestId('nav-workspace-key').fill(workspaceKey)
  await dialog.getByTestId('nav-workspace-name').fill(workspaceName)
  await dialog.getByRole('button', { name: '创建', exact: true }).click()
  await expect(page.getByText('导航工作区已创建，默认入口已形成草稿')).toBeVisible()
  await expect(page.getByRole('button').filter({ hasText: workspaceName })).toBeVisible()

  await page.getByTestId('publish-navigation-button').click()
  await expect(page.getByText('导航版本已发布，后端会按用户权限返回导航树')).toBeVisible()
  const publishedWorkspaces = await apiFromPage<
    Array<{ workspaceKey: string; status: string; isTestData: boolean; isDefault: boolean }>
  >(page, '/api/v1/navigation/workspaces')
  expect(
    publishedWorkspaces.find((workspace) => workspace.workspaceKey === workspaceKey),
  ).toMatchObject({ status: 'Published', isTestData: true, isDefault: false })
  await expect(page.locator('details.department-group')).toHaveCount(7)
  await expect(page.locator('.nav-section-label').filter({ hasText: workspaceName })).toHaveCount(0)

  await logout(page)
  await loginAs(page, '研发工程师')
  await expect(page.getByRole('link', { name: /部门工作区/ }).first()).toBeVisible()
  await expect(page.getByRole('link', { name: /管理概览/ })).toHaveCount(0)

  await logout(page)
  await loginAs(page, '平台管理员')
  await page.goto('/iam')
  await page.getByRole('tab', { name: '导航与工作区' }).click()
  const workspaceCard = page.getByRole('button').filter({ hasText: workspaceName }).first()
  await workspaceCard.click()
  await page.getByRole('button', { name: '停止展示', exact: true }).click()
  await expect(page.getByText('工作区已停止展示，业务数据未删除')).toBeVisible()
  await expect(page.locator('.nav-section-label').filter({ hasText: workspaceName })).toHaveCount(0)
  await page.getByRole('button', { name: '恢复入口', exact: true }).click()
  await expect(page.getByText('工作区已恢复展示')).toBeVisible()
})

test('@desktop second administrator rejects high-risk permission request with required opinion', async ({ page }) => {
  await loginAs(page, '平台管理员')
  const suffix = Date.now()
  const role = await apiFromPage<{ id: string; version: number }>(
    page,
    '/api/v1/iam/roles',
    'POST',
    {
      code: `REJECT-E2E-${suffix}`,
      name: '高风险驳回 E2E',
      roleType: 'Business',
      description: null,
    },
    crypto.randomUUID(),
  )
  const permissions = await apiFromPage<Array<{ id: string; code: string }>>(page, '/api/v1/iam/permissions')
  const highRisk = permissions.find((item) => item.code === 'identity-admin-manage')!
  await apiFromPage(
    page,
    `/api/v1/iam/roles/${role.id}/permissions`,
    'PUT',
    { permissionIds: [highRisk.id], expectedRoleVersion: role.version },
    crypto.randomUUID(),
  )
  const pending = await apiFromPage<Array<{ id: string; targetRoleId: string | null }>>(
    page,
    '/api/v1/iam/access-requests?status=Pending',
  )
  const accessRequest = pending.find((item) => item.targetRoleId === role.id)!

  await logout(page)
  await loginAs(page, '安全管理员')
  await page.goto('/iam')
  await page.getByRole('tab', { name: /高风险审核/ }).click()
  const requestCard = page.locator(`[data-request-id="${accessRequest.id}"]`)
  await expect(requestCard).toBeVisible()
  await requestCard.getByRole('button', { name: '复核驳回' }).click()
  const dialog = page.getByRole('dialog', { name: '复核驳回高风险授权' })
  await dialog.locator('textarea').fill('E2E：不符合最小权限原则')
  await dialog.getByRole('button', { name: '确认驳回' }).click()
  await expect(page.getByText('高风险授权已驳回并写入审计')).toBeVisible()
})

test('@desktop real project manager approves manager business-role node', async ({ page }) => {
  await loginAs(page, '平台管理员')
  const suffix = Date.now()
  const configurationKey = `WFROLE-E2E-${suffix}`
  const setup = await page.evaluate(
    async ({ configurationKey }) => {
      async function csrf() {
        const response = await fetch('/api/v1/auth/csrf')
        return (await response.json()).data.requestToken
      }
      async function call(path: string, method: string, body?: unknown, key?: string) {
        const response = await fetch(path, {
          method,
          headers: {
            'Content-Type': 'application/json',
            'X-CSRF-TOKEN': method === 'GET' ? '' : await csrf(),
            ...(key ? { 'Idempotency-Key': key } : {}),
          },
          body: body === undefined ? undefined : JSON.stringify(body),
        })
        const payload = await response.json()
        if (!response.ok) throw new Error(`${path}: ${response.status} ${payload.message}`)
        return payload.data
      }
      const tree = await call('/api/v1/iam/organization-tree', 'GET')
      const flatten = (nodes: Array<{ children: unknown[] }>): Array<{ id: string; code: string }> =>
        nodes.flatMap((node) => [node, ...flatten(node.children as Array<{ children: unknown[] }>)]) as Array<{
          id: string
          code: string
        }>
      const rnd = flatten(tree).find((item) => item.code === 'RND')
      const objectTypes = await call('/api/v1/object-types?pageSize=100', 'GET')
      const projectType = objectTypes.items.find((item: { code: string }) => item.code === 'PROJECT')
      const pmUsers = await call('/api/v1/iam/users?pageSize=100&search=demo.pm', 'GET')
      const qaUsers = await call('/api/v1/iam/users?pageSize=100&search=demo.qa', 'GET')
      const pm = pmUsers.items.find((item: { userName: string }) => item.userName === 'demo.pm')
      const qa = qaUsers.items.find((item: { userName: string }) => item.userName === 'demo.qa')
      const roles = await call('/api/v1/iam/roles', 'GET')
      const managerRole = roles.find((role: { code: string }) => role.code === 'business-manager-reviewer')
      const qualityRole = roles.find((role: { code: string }) => role.code === 'business-quality-reviewer')
      await call(
        `/api/v1/iam/roles/${managerRole.id}/members`,
        'PUT',
        {
          expectedRoleVersion: managerRole.version,
          members: [
            {
              userId: pm.id,
              scopeType: 'department',
              scopeReference: rnd.id,
              isActive: true,
            },
          ],
        },
        crypto.randomUUID(),
      )
      await call(
        `/api/v1/iam/roles/${qualityRole.id}/members`,
        'PUT',
        {
          expectedRoleVersion: qualityRole.version,
          members: [
            {
              userId: qa.id,
              scopeType: 'organization',
              scopeReference: 'organization',
              isActive: true,
            },
          ],
        },
        crypto.randomUUID(),
      )
      const objectCode = `PM-${Date.now()}`
      const businessObject = await call(
        '/api/v1/business-objects',
        'POST',
        {
          objectTypeId: projectType.id,
          objectCode,
          departmentId: rnd.id,
          fields: {
            ProjectName: '项目经理真实审批项目',
            OwnerUserId: pm.id,
            Objective: '真实项目经理角色审批 E2E',
            CurrentStage: '验收',
            ConfidentialityLevel: 'Internal',
          },
        },
        crypto.randomUUID(),
      )
      const configuration = await call(
        '/api/v1/configurations',
        'POST',
        {
          configurationKey,
          name: '真实项目经理审批 E2E',
          description: '研发发起、经理角色审核、质量角色复核',
          departmentId: rnd.id,
          objectTypeCode: 'PROJECT',
          startNode: 'RD_INITIATE',
          nodes: [
            {
              code: 'RD_INITIATE',
              name: '研发发起',
              assigneeUserName: 'demo.researcher',
              nextNode: 'MANAGER_REVIEW',
              completeOnApprove: false,
              condition: null,
              assigneeStrategy: 'SpecificUser',
              businessRoleCode: null,
              agentProfileId: null,
            },
            {
              code: 'MANAGER_REVIEW',
              name: '经理审核',
              assigneeUserName: null,
              nextNode: 'QA_REVIEW',
              completeOnApprove: false,
              condition: null,
              assigneeStrategy: 'BusinessRole',
              businessRoleCode: 'business-manager-reviewer',
              agentProfileId: null,
            },
            {
              code: 'QA_REVIEW',
              name: '质量复核',
              assigneeUserName: null,
              nextNode: null,
              completeOnApprove: true,
              condition: null,
              assigneeStrategy: 'BusinessRole',
              businessRoleCode: 'business-quality-reviewer',
              agentProfileId: null,
            },
          ],
        },
        crypto.randomUUID(),
      )
      await call(`/api/v1/configurations/${configuration.id}/actions/validate`, 'POST', {}, crypto.randomUUID())
      const published = await call(
        `/api/v1/configurations/${configuration.id}/actions/publish`,
        'POST',
        {},
        crypto.randomUUID(),
      )
      const instance = await call('/api/v1/workflow-instances', 'POST', {
        workflowDefinitionId: published.publishedWorkflowDefinitionId,
        workflowVersionId: published.publishedWorkflowVersionId,
        businessObjectId: businessObject.id,
        idempotencyKey: crypto.randomUUID(),
      })
      return { instanceId: instance.id as string }
    },
    { configurationKey },
  )

  await approveCurrentTask(page, '研发工程师', setup.instanceId, 'RD_INITIATE')
  await logout(page)
  await loginAs(page, '项目经理')
  await page.goto('/workflows')
  const managerTask = workflowTaskCard(page, setup.instanceId, 'MANAGER_REVIEW')
  await expect(managerTask).toContainText('业务岗位')
  await expect(managerTask).toContainText('business-manager-reviewer')
  const managerDecision = page.waitForResponse(
    (response) =>
      response.url().includes('/api/v1/workflow-tasks/') &&
      response.url().endsWith('/actions/decide') &&
      response.request().method() === 'POST',
  )
  await managerTask.getByTestId('approve-task-button').click()
  await page.getByRole('button', { name: '确认通过', exact: true }).click()
  expect((await managerDecision).ok()).toBeTruthy()

  await logout(page)
  await loginAs(page, '质量工程师')
  await page.goto('/workflows')
  const qualityTask = workflowTaskCard(page, setup.instanceId, 'QA_REVIEW')
  const qualityDecision = page.waitForResponse(
    (response) =>
      response.url().includes('/api/v1/workflow-tasks/') &&
      response.url().endsWith('/actions/decide') &&
      response.request().method() === 'POST',
  )
  await qualityTask.getByTestId('approve-task-button').click()
  await page.getByRole('button', { name: '确认通过', exact: true }).click()
  expect((await qualityDecision).ok()).toBeTruthy()
})

test('@desktop digital employee identity lifecycle persists and revokes MCP access', async ({ page }) => {
  const marker = Date.now()
  const code = `AGENT-E2E-${marker}`
  const draftName = `研发资料治理数字员工 ${marker}`
  const publishedName = `${draftName} V2`
  await loginAs(page, '平台管理员')
  await page.goto('/agents')
  await expect(page.getByTestId('agent-identity-center')).toBeVisible()

  const created = await createAgentDraftViaUi(page, {
    code,
    name: draftName,
    description: '读取研发资料和流程待办，只生成建议，所有提交由真人确认。',
    toolName: 'workflow.read',
    serviceAccountName:
      process.env.AIDT_E2E_LOWRISK_SERVICE_NAME ?? '低风险治理演练数字员工',
  })
  await expect(page.getByTestId('agent-identity-card').filter({ hasText: code })).toBeVisible()

  await page.getByTestId('edit-agent-identity').click()
  let editor = page.getByRole('dialog', { name: '编辑数字员工身份' })
  await editor.getByTestId('agent-name-input').fill(publishedName)
  const updateResponse = page.waitForResponse(
    (response) =>
      response.url().includes(`/api/v1/agents/${created.profileId}/drafts/`) && response.request().method() === 'PUT',
  )
  await editor.getByTestId('save-agent-draft').click()
  expect((await updateResponse).ok()).toBeTruthy()
  await expect(page.getByText('草稿已保存，可继续校验或发布。')).toBeVisible()

  const validateResponse = page.waitForResponse(
    (response) => response.url().endsWith('/actions/validate') && response.request().method() === 'POST',
  )
  await editor.getByTestId('validate-agent-draft').click()
  expect((await validateResponse).ok()).toBeTruthy()
  await expect(page.getByText('校验通过，可以发布')).toBeVisible()

  const publishResponse = page.waitForResponse(
    (response) => response.url().endsWith('/actions/publish') && response.request().method() === 'POST',
  )
  await editor.getByTestId('publish-agent-draft').click()
  expect((await publishResponse).ok()).toBeTruthy()
  await expect(editor).toBeHidden()
  await expect(page.getByText('新版本已发布并成为唯一有效版本。')).toBeVisible()

  await page.reload()
  await page.getByPlaceholder('搜索名称或稳定编码').fill(publishedName)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  const persistedCard = page.getByTestId('agent-identity-card').filter({ hasText: publishedName })
  await expect(persistedCard).toBeVisible()
  await persistedCard.click()
  await expect(page.getByTestId('agent-identity-detail')).toContainText('有效')

  await logout(page)
  await loginAs(page, '研发工程师')
  await page.goto('/agents')
  await page.getByPlaceholder('搜索名称或稳定编码').fill(publishedName)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  const readOnlyCard = page.getByTestId('agent-identity-card').filter({ hasText: publishedName })
  await expect(readOnlyCard).toBeVisible()
  await expect(page.getByTestId('create-agent-identity')).toHaveCount(0)
  await readOnlyCard.click()
  await expect(page.getByTestId('edit-agent-identity')).toHaveCount(0)

  await logout(page)
  await loginAs(page, '平台管理员')
  await page.goto('/agents')
  await page.getByPlaceholder('搜索名称或稳定编码').fill(publishedName)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  await page.getByTestId('agent-identity-card').filter({ hasText: publishedName }).click()
  await page.getByTestId('edit-agent-identity').click()
  editor = page.getByRole('dialog', { name: '编辑数字员工身份' })
  await editor.getByTestId('agent-name-input').fill(`${publishedName} Rollback`)
  await editor.getByTestId('save-agent-draft').click()
  await expect(page.getByText('草稿已保存，可继续校验或发布。')).toBeVisible()
  await editor.getByTestId('validate-agent-draft').click()
  await expect(page.getByText('校验通过，可以发布')).toBeVisible()
  await editor.getByTestId('publish-agent-draft').click()
  await expect(editor).toBeHidden()

  await page.getByRole('tab', { name: '版本历史' }).click()
  const rollback = page.getByTestId('rollback-agent-version-1')
  await expect(rollback).toBeVisible()
  const rollbackResponse = page.waitForResponse(
    (response) =>
      response.url().endsWith(`/api/v1/agents/${created.profileId}/actions/rollback`) &&
      response.request().method() === 'POST',
  )
  await rollback.click()
  await page.getByRole('button', { name: '继续回滚', exact: true }).click()
  const rollbackPrompt = page.getByRole('dialog', { name: '回滚原因' })
  await rollbackPrompt.getByRole('textbox').fill('E2E 验证历史版本回滚')
  await rollbackPrompt.getByRole('button', { name: '提交回滚', exact: true }).click()
  expect((await rollbackResponse).ok()).toBeTruthy()

  const runResponse = page.waitForResponse(
    (response) => response.url().endsWith('/api/v1/agents/runs') && response.request().method() === 'POST',
  )
  await page.getByRole('button', { name: '发起受控运行', exact: true }).click()
  const runPayload = await (await runResponse).json()
  const runId = String(runPayload.data.id)
  await expect(page).toHaveURL(new RegExp(`run=${runId}`))
  await page.getByRole('tab', { name: '运行记录' }).click()
  await expect(page.getByText(`已从深链定位运行 ${runId.slice(-8)}`)).toBeVisible()

  await page.goto('/mcp-center')
  await page.getByTestId('issue-mcp-service-token').click()
  const serviceDialog = page.getByRole('dialog', {
    name: '签发服务身份 MCP 令牌',
  })
  await serviceDialog.getByTestId('mcp-agent-profile-select').click()
  await page.getByRole('option', { name: new RegExp(publishedName) }).click()
  await serviceDialog.getByLabel('令牌名称').fill('数字员工 MCP E2E')
  const tokenResponse = page.waitForResponse(
    (response) => response.url().endsWith('/api/v1/mcp/service-tokens') && response.request().method() === 'POST',
  )
  await serviceDialog.getByRole('button', { name: '签发', exact: true }).click()
  expect((await tokenResponse).ok()).toBeTruthy()
  await page.getByRole('button', { name: '我已保存', exact: true }).click()

  await page.goto('/agents')
  await page.getByPlaceholder('搜索名称或稳定编码').fill(publishedName)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  await page.getByTestId('agent-identity-card').filter({ hasText: publishedName }).click()
  await page.getByRole('tab', { name: 'MCP 授权' }).click()
  await expect(page.getByText('数字员工 MCP E2E').first()).toBeVisible()

  const disableResponse = page.waitForResponse(
    (response) =>
      response.url().endsWith(`/api/v1/agents/${created.profileId}/actions/disable`) &&
      response.request().method() === 'POST',
  )
  await page.getByRole('button', { name: '停用并撤销令牌', exact: true }).click()
  const disableDialog = page.getByRole('dialog', { name: '停用数字员工' })
  await disableDialog.getByRole('textbox').fill('E2E 验证停用和令牌撤销')
  await disableDialog.getByRole('button', { name: '确认停用', exact: true }).click()
  expect((await disableResponse).ok()).toBeTruthy()
  await expect(page.getByText('数字员工已停用，服务令牌已撤销。')).toBeVisible()
  await expect(page.getByTestId('agent-identity-detail')).toContainText('已停用')

  await page.goto(`/agents?run=${runId}`)
  await expect(page.getByTestId('agent-identity-detail')).toBeVisible()
  await expect(page.getByTestId('agent-identity-detail')).toContainText('已停用')
  await expect(page.getByText(`已从深链定位运行 ${runId.slice(-8)}`)).toBeVisible()

  await page.goto('/mcp-center')
  await page.getByTestId('issue-mcp-service-token').click()
  const disabledServiceDialog = page.getByRole('dialog', {
    name: '签发服务身份 MCP 令牌',
  })
  await disabledServiceDialog.getByTestId('mcp-agent-profile-select').click()
  await expect(page.getByRole('option', { name: new RegExp(publishedName) })).toHaveCount(0)
  await page.keyboard.press('Escape')
  await disabledServiceDialog.getByRole('button', { name: '取消', exact: true }).click()

  await page.goto('/agents')
  await page.getByPlaceholder('搜索名称或稳定编码').fill(publishedName)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  await page.getByTestId('agent-identity-card').filter({ hasText: publishedName }).click()
  await page.getByRole('button', { name: '恢复身份', exact: true }).click()
  const resumeDialog = page.getByRole('dialog', { name: '恢复数字员工' })
  await resumeDialog.getByRole('textbox').fill('E2E 验证恢复后重新可选')
  await resumeDialog.getByRole('button', { name: '确认恢复', exact: true }).click()
  await expect(page.getByText('数字员工已恢复；旧令牌不会重新生效。')).toBeVisible()
})

test('@desktop high-risk digital employee change requires second administrator', async ({ page }) => {
  const marker = Date.now()
  const code = `AGENT-HIGH-E2E-${marker}`
  const name = `高风险流程治理数字员工 ${marker}`
  await loginAs(page, '平台管理员')
  await page.goto('/agents')
  await createAgentDraftViaUi(page, {
    code,
    name,
    description: '验证高风险能力变更必须经过第二位平台管理员审核。',
    autonomy: 'A3',
    risk: 'High',
    amount: 10000,
    toolName: 'workflow.proxy-low-risk',
    serviceAccountName:
      process.env.AIDT_E2E_HIGHRISK_SERVICE_NAME ?? '高风险治理演练数字员工',
  })

  await page.getByTestId('edit-agent-identity').click()
  const editor = page.getByRole('dialog', { name: '编辑数字员工身份' })
  await editor.getByTestId('validate-agent-draft').click()
  await expect(page.getByText('校验通过；发布后将进入双人审核。')).toBeVisible()
  const publishResponse = page.waitForResponse(
    (response) => response.url().endsWith('/actions/publish') && response.request().method() === 'POST',
  )
  await editor.getByTestId('publish-agent-draft').click()
  expect((await publishResponse).status()).toBe(202)
  await expect(page.getByText('该变更提升能力边界，已提交第二位平台管理员审核。')).toBeVisible()
  await expect(page.getByTestId('approve-agent-change').first()).toBeDisabled()

  await logout(page)
  await loginAs(page, '安全管理员')
  await page.goto('/agents')
  const reviewCard = page.locator('.review-row').filter({ hasText: name })
  await expect(reviewCard).toBeVisible()
  await reviewCard.getByTestId('approve-agent-change').click()
  const reviewPrompt = page.getByRole('dialog', { name: '通过高风险变更' })
  await reviewPrompt.getByRole('textbox').fill('第二位平台管理员完成高风险能力审核。')
  const reviewResponse = page.waitForResponse(
    (response) =>
      response.url().includes('/api/v1/agents/change-requests/') &&
      response.url().endsWith('/actions/review') &&
      response.request().method() === 'POST',
  )
  await reviewPrompt.getByRole('button', { name: '审核通过', exact: true }).click()
  expect((await reviewResponse).ok()).toBeTruthy()
  await page.getByPlaceholder('搜索名称或稳定编码').fill(name)
  await page.getByRole('button', { name: '查询', exact: true }).click()
  await page.getByTestId('agent-identity-card').filter({ hasText: name }).click()
  await expect(page.getByTestId('agent-identity-detail')).toContainText('有效')
})

test('@desktop1280 @mobile digital employee identity center remains operable', async ({ page }) => {
  await fs.mkdir(evidenceDir, { recursive: true })
  await loginAs(page, '平台管理员')
  await page.goto('/agents')

  await expect(page.getByTestId('agent-identity-center')).toBeVisible()
  await expect(page.getByPlaceholder('搜索名称或稳定编码')).toBeVisible()
  await expect(page.getByRole('heading', { name: '数字员工身份', exact: true })).toBeVisible()

  const dimensions = await page.evaluate(() => ({
    viewport: window.innerWidth,
    content: document.documentElement.scrollWidth,
  }))
  expect(dimensions.content).toBeLessThanOrEqual(dimensions.viewport + 1)

  const firstCard = page.getByTestId('agent-identity-card').first()
  await expect(firstCard).toBeVisible()
  await firstCard.click()

  const detail = page.getByTestId('agent-identity-detail')
  await expect(detail).toBeVisible()
  await expect(page.getByText('独立服务身份', { exact: true })).toBeVisible()
  await page.getByRole('tab', { name: '版本历史' }).click()
  await expect(page.getByRole('tab', { name: '版本历史' })).toHaveAttribute('aria-selected', 'true')

  const width = page.viewportSize()?.width ?? 0
  await page.screenshot({
    path: path.join(evidenceDir, `agent-identity-${width}.png`),
    fullPage: true,
  })
})

test('@mobile essential pages fit 390px and remain operable', async ({ page }) => {
  await fs.mkdir(evidenceDir, { recursive: true })
  await loginAs(page, '平台管理员')

  for (const route of [
    '/',
    '/department',
    '/documents',
    '/minutes',
    '/projects',
    '/inbox',
    '/workflows',
    '/objects',
    '/modeling',
    '/management',
    '/audit',
    '/agents',
    '/workflow-policies',
    '/configurations',
    '/iam',
  ]) {
    await page.goto(route)
    await expect(page.locator('body')).toBeVisible()
    const dimensions = await page.evaluate(() => ({
      viewport: window.innerWidth,
      content: document.documentElement.scrollWidth,
    }))
    expect(dimensions.content).toBeLessThanOrEqual(dimensions.viewport + 1)
  }

  await page.goto('/documents')
  await page.screenshot({
    path: path.join(evidenceDir, 'mobile-390-documents.png'),
    fullPage: true,
  })

  await page.goto('/projects')
  await page.screenshot({
    path: path.join(evidenceDir, 'mobile-390-projects.png'),
    fullPage: true,
  })

  await page.goto('/iam')
  await page.screenshot({
    path: path.join(evidenceDir, 'mobile-390-iam.png'),
    fullPage: true,
  })
})
