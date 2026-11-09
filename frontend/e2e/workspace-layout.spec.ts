import { expect, test, type Page } from '@playwright/test'
import fs from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const testDirectory = fileURLToPath(new URL('.', import.meta.url))
const evidenceDir = process.env.AIDT_EVIDENCE_DIR
  ? path.resolve(process.env.AIDT_EVIDENCE_DIR)
  : path.resolve(testDirectory, '../../docs/project/test-artifacts')

async function loginAs(page: Page) {
  await page.goto('/login')
  await page.getByTestId('login-user').click()
  await page.getByRole('option', { name: '平台管理员', exact: true }).click()
  await page.getByTestId('login-submit').click()
  await expect(page).toHaveURL(/\/$/)
  await expect(page.getByTestId('my-work-page')).toBeVisible()
}

test('@desktop UX-005-A keeps the sidebar fixed while the body scrolls', async ({ page }) => {
  await fs.mkdir(evidenceDir, { recursive: true })
  await page.setViewportSize({ width: 1920, height: 1080 })
  await loginAs(page)
  await expect(page.getByTestId('my-work-page')).toBeVisible()
  await expect(page.locator('[data-testid="unified-work-item"]').first()).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-a-desktop-1920-sidebar.png'),
  })

  const main = page.locator('.main-content')
  const sidebar = page.getByTestId('app-sidebar')
  const navigation = page.getByTestId('sidebar-navigation')

  const scrollResult = await main.evaluate((element) => {
    element.scrollTop = 3000
    return {
      scrollTop: element.scrollTop,
      scrollHeight: element.scrollHeight,
      clientHeight: element.clientHeight,
    }
  })
  expect(scrollResult.scrollHeight).toBeGreaterThan(scrollResult.clientHeight)
  expect(scrollResult.scrollTop).toBeGreaterThan(0)
  await expect.poll(async () => Math.round((await sidebar.boundingBox())?.y ?? -1)).toBe(0)

  const navMetrics = await navigation.evaluate((element) => ({
    clientHeight: element.clientHeight,
    scrollHeight: element.scrollHeight,
    overflowY: getComputedStyle(element).overflowY,
    scrollTop: element.scrollTop,
  }))
  expect(navMetrics.overflowY).toBe('auto')

  if (navMetrics.scrollHeight > navMetrics.clientHeight) {
    await navigation.evaluate((element) => {
      element.scrollTop = element.scrollHeight
    })
    await expect.poll(() => navigation.evaluate((element) => element.scrollTop)).toBeGreaterThan(0)
  }

  const footer = page.getByTestId('navigation-preferences')
  const footerBox = await footer.boundingBox()
  expect(footerBox).not.toBeNull()
  expect((footerBox?.y ?? 0) + (footerBox?.height ?? 0)).toBeLessThanOrEqual(1080)
  await footer.scrollIntoViewIfNeeded()
  await expect(footer).toBeVisible()

  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
  expect(overflow).toBeLessThanOrEqual(1)
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-a-desktop-1920-scrolled.png'),
  })
})

test('@responsive UX-005-A keeps a compact 1280x720 shell without horizontal overflow', async ({
  page,
}) => {
  await fs.mkdir(evidenceDir, { recursive: true })
  await page.setViewportSize({ width: 1280, height: 720 })
  await loginAs(page)
  await page.goto('/documents')

  await expect(page.getByTestId('app-sidebar')).toBeInViewport()
  await expect(page.getByTestId('sidebar-drawer-trigger')).toBeHidden()
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
  expect(overflow).toBeLessThanOrEqual(1)
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-a-1280x720.png'),
  })
})

for (const viewport of [
  { width: 600, height: 518, name: '600x518-codex' },
  { width: 390, height: 844, name: '390x844-mobile' },
]) {
  test(`@responsive UX-005-A uses a ${viewport.name} navigation drawer`, async ({ page }) => {
    await fs.mkdir(evidenceDir, { recursive: true })
    await page.setViewportSize({ width: viewport.width, height: viewport.height })
    await loginAs(page)
    await page.goto('/documents')

    const trigger = page.getByTestId('sidebar-drawer-trigger')
    const sidebar = page.getByTestId('app-sidebar')
    await expect(trigger).toBeVisible()
    await trigger.click()
    await expect(sidebar).toHaveClass(/drawer-open/)
    await expect(page.getByTestId('sidebar-backdrop')).toBeVisible()
    await expect(sidebar).toBeInViewport()
    await expect(sidebar.getByTestId('navigation-preferences')).toBeVisible()
    await page.waitForTimeout(240)
    await page.screenshot({
      path: path.join(evidenceDir, `ux005-a-${viewport.name}-drawer.png`),
    })

    await page.keyboard.press('Escape')
    await expect(sidebar).not.toHaveClass(/drawer-open/)
    await expect(trigger).toBeFocused()

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(1)
    await page.screenshot({
      path: path.join(evidenceDir, `ux005-a-${viewport.name}.png`),
    })
  })
}

test('@desktop UX-005-C2 minutes uses live metrics, server filters and independent panes', async ({
  page,
}) => {
  await fs.mkdir(evidenceDir, { recursive: true })
  await page.setViewportSize({ width: 1920, height: 1080 })
  await loginAs(page)
  await page.goto('/minutes')

  await expect(page.getByRole('heading', { name: 'AI 听记', exact: true })).toBeVisible()
  await expect(page.getByTestId('minutes-stat-total')).toBeVisible()
  await expect(page.getByTestId('minutes-stat-analyzed')).toBeVisible()
  await expect(page.getByTestId('minutes-stat-pending-actions')).toBeVisible()
  await expect(page.getByTestId('minutes-stat-week')).toBeVisible()
  await expect(page.locator('.page-lead')).toHaveCount(0)
  await expect(page.locator('.source-row').first()).toBeVisible()
  await page.locator('.el-message').waitFor({ state: 'hidden', timeout: 5_000 }).catch(() => undefined)

  const workspace = page.getByTestId('minutes-workspace')
  const workspaceBox = await workspace.boundingBox()
  expect(workspaceBox?.width ?? 0).toBeGreaterThan(1260)
  expect(workspaceBox?.height ?? 0).toBeLessThan(900)
  const sourceRows = await page.locator('.source-row').count()
  expect(sourceRows).toBeGreaterThan(0)
  expect(sourceRows).toBeLessThanOrEqual(20)

  const firstTitle = (await page.locator('.source-row strong').first().innerText()).trim()
  await expect(page.locator('.detail-head h2')).toHaveText(firstTitle)
  if (sourceRows > 1) {
    const secondTitle = (await page.locator('.source-row strong').nth(1).innerText()).trim()
    await page.locator('.source-row').nth(1).click()
    await expect(page.locator('.detail-head h2')).toHaveText(secondTitle)
  }

  const scrollMetrics = await page.getByTestId('minutes-source-list').evaluate((element) => {
    const style = getComputedStyle(element)
    element.scrollTop = 240
    return {
      overflowY: style.overflowY,
      scrollHeight: element.scrollHeight,
      clientHeight: element.clientHeight,
      scrollTop: element.scrollTop,
    }
  })
  expect(scrollMetrics.overflowY).toBe('auto')
  expect(scrollMetrics.scrollHeight).toBeGreaterThanOrEqual(scrollMetrics.clientHeight)
  if (scrollMetrics.scrollHeight > scrollMetrics.clientHeight) {
    expect(scrollMetrics.scrollTop).toBeGreaterThan(0)
  }

  await page.locator('.transcript summary').click()
  const detailMetrics = await page.locator('.detail-scroll').evaluate((element) => {
    const style = getComputedStyle(element)
    element.scrollTop = 200
    return {
      overflowY: style.overflowY,
      scrollHeight: element.scrollHeight,
      clientHeight: element.clientHeight,
    }
  })
  expect(detailMetrics.overflowY).toBe('auto')
  expect(detailMetrics.scrollHeight).toBeGreaterThanOrEqual(detailMetrics.clientHeight)

  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-c2-desktop-1920.png'),
  })

  await page.getByTestId('minutes-analysis-filter').click()
  await page.getByRole('option', { name: '已分析', exact: true }).click()
  await expect(page.getByTestId('workbench-result-count')).toContainText('听记')
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-c2-filtered-1920.png'),
  })
  await page.getByTestId('minutes-reset').click()

  const searchTitle = (await page.locator('.source-row strong').first().innerText()).trim()
  await page.getByRole('textbox', { name: '搜索听记' }).fill(searchTitle)
  await page.waitForTimeout(450)
  await expect(page.locator('.source-row').first()).toBeVisible()
  const filteredTitles = await page.locator('.source-row strong').allTextContents()
  expect(filteredTitles.some((title) => title.includes(searchTitle))).toBe(true)

  const introTrigger = page.getByTestId('workbench-intro-trigger')
  await introTrigger.click()
  const intro = page.getByTestId('workbench-intro-drawer')
  await expect(intro).toContainText('业务例子')
  await expect(intro).toContainText('钉钉接入为未连接状态')
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-c2-intro-1920.png'),
  })
  await page.keyboard.press('Escape')
  await expect(introTrigger).toBeFocused()

  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
  expect(overflow).toBeLessThanOrEqual(1)
})

for (const viewport of [
  { width: 600, height: 518, name: '600x518-codex' },
  { width: 390, height: 844, name: '390x844-mobile' },
]) {
  test(`@responsive UX-005-C2 minutes stacks bounded panes at ${viewport.name}`, async ({ page }) => {
    await fs.mkdir(evidenceDir, { recursive: true })
    await page.setViewportSize({ width: viewport.width, height: viewport.height })
    await loginAs(page)
    await page.goto('/minutes')
    await expect(page.getByTestId('minutes-workspace')).toBeVisible()
    await expect(page.locator('.source-row').first()).toBeVisible()
    await page.locator('.el-message').waitFor({ state: 'hidden', timeout: 5_000 }).catch(() => undefined)

    const paneHeights = await page.locator('.minutes-source-panel, .minutes-detail-panel').evaluateAll(
      (panels) => panels.map((panel) => panel.getBoundingClientRect().height),
    )
    expect(paneHeights).toHaveLength(2)
    expect(paneHeights.every((height) => height <= viewport.height)).toBe(true)
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(1)

    await page.locator('.main-content').evaluate((element) => {
      element.scrollTop = 330
    })
    await page.waitForTimeout(100)
    await page.screenshot({
      path: path.join(evidenceDir, `ux005-c2-${viewport.name}.png`),
    })
  })
}

test('@desktop UX-005-C1 my work exposes real metrics, filters, categories and intro', async ({
  page,
}) => {
  await fs.mkdir(evidenceDir, { recursive: true })
  await page.setViewportSize({ width: 1920, height: 1080 })
  await loginAs(page)

  await expect(page.getByRole('heading', { name: '我的工作', exact: true })).toBeVisible()
  await expect(page.getByTestId('my-work-stat-pending')).toBeVisible()
  await expect(page.getByTestId('my-work-stat-due-today')).toBeVisible()
  await expect(page.getByTestId('my-work-stat-confirmation')).toBeVisible()
  await expect(page.getByTestId('my-work-stat-boundary')).toBeVisible()
  await expect(page.locator('.page-lead')).toHaveCount(0)
  await expect(page.locator('[data-testid="my-work-category"]')).toHaveCount(5)
  expect((await page.locator('.main-content').boundingBox())?.width ?? 0).toBeGreaterThan(1260)
  await expect(page.locator('[data-testid="unified-work-item"]').first()).toBeVisible()
  await page.locator('.el-message').waitFor({ state: 'hidden', timeout: 5_000 }).catch(() => undefined)
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-c1-desktop-1920.png'),
  })

  const introTrigger = page.getByTestId('workbench-intro-trigger')
  await introTrigger.click()
  const intro = page.getByTestId('workbench-intro-drawer')
  await expect(intro).toBeVisible()
  await expect(intro).toContainText('业务例子')
  await expect(intro).toContainText('永久人工边界')
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-c1-intro-1920.png'),
  })
  await page.keyboard.press('Escape')
  await expect(intro).toBeHidden()
  await expect(introTrigger).toBeFocused()

  const sourceFilter = page.getByTestId('my-work-source-filter')
  await sourceFilter.click()
  await page.getByRole('option', { name: '流程待办', exact: true }).click()
  await page.getByTestId('my-work-department-filter').click()
  await page.getByRole('option', { name: '研发', exact: true }).click()
  await expect(page.getByTestId('workbench-result-count')).toContainText('筛选结果')
  const visibleSourceTypes = await page
    .locator('[data-testid="unified-work-item"]')
    .evaluateAll((rows) => rows.map((row) => row.getAttribute('data-source-type')))
  expect(visibleSourceTypes.every((value) => value === 'WorkflowTask')).toBe(true)
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-c1-combined-filter-1920.png'),
  })

  await page.getByTestId('my-work-reset').click()
  const firstTitle = await page
    .locator('[data-testid="unified-work-item"] .item-column strong')
    .first()
    .innerText()
  const searchText = firstTitle.slice(0, 4)
  await page.getByRole('textbox', { name: '搜索我的工作' }).fill(searchText)
  const filteredTitles = await page
    .locator('[data-testid="unified-work-item"] .item-column strong')
    .allTextContents()
  expect(filteredTitles.length).toBeGreaterThan(0)
  expect(filteredTitles.every((title) => title.includes(searchText))).toBe(true)
  await page.screenshot({
    path: path.join(evidenceDir, 'ux005-c1-filtered-1920.png'),
  })

  await page.getByTestId('my-work-reset').click()
  await page.getByRole('tab', { name: /我发起/ }).click()
  await expect(page.getByRole('tab', { name: /我发起/ })).toHaveAttribute('aria-selected', 'true')
  await page.getByRole('tab', { name: /全部/ }).click()
  const beforeUrl = page.url()
  await page.locator('[data-testid="unified-work-item"]').first().click()
  await expect.poll(() => page.url()).not.toBe(beforeUrl)
})

for (const viewport of [
  { width: 600, height: 518, name: '600x518-codex' },
  { width: 390, height: 844, name: '390x844-mobile' },
]) {
  test(`@responsive UX-005-C1 my work uses cards and a ${viewport.name} filter drawer`, async ({
    page,
  }) => {
    await fs.mkdir(evidenceDir, { recursive: true })
    await page.setViewportSize({ width: viewport.width, height: viewport.height })
    await loginAs(page)

    await expect(page.getByTestId('work-item-mobile-list')).toBeVisible()
    await page.locator('.el-message').waitFor({ state: 'hidden', timeout: 5_000 }).catch(() => undefined)
    await expect(page.getByTestId('work-item-table')).toHaveCount(0)
    const filterTrigger = page.getByTestId('workbench-filter-trigger')
    await filterTrigger.click()
    const filterPanel = page.getByTestId('workbench-filters')
    await expect(filterPanel).toHaveClass(/mobile-open/)
    await expect(page.getByTestId('my-work-priority-filter')).toBeVisible()
    await filterPanel.getByRole('button', { name: '完成', exact: true }).click()
    await expect(filterPanel).not.toHaveClass(/mobile-open/)
    await expect(filterTrigger).toBeFocused()

    const overflow = await page.evaluate(() => document.documentElement.scrollWidth - window.innerWidth)
    expect(overflow).toBeLessThanOrEqual(1)
    await page.locator('.main-content').evaluate((element) => {
      element.scrollTop = 360
    })
    await page.waitForTimeout(100)
    await page.screenshot({
      path: path.join(evidenceDir, `ux005-c1-${viewport.name}.png`),
    })
  })
}
