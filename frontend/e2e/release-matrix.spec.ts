import { chromium, expect, test, type Browser, type BrowserContext, type Page } from '@playwright/test'
import fs from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const testDirectory = fileURLToPath(new URL('.', import.meta.url))
const evidenceDir = process.env.AIDT_EVIDENCE_DIR
  ? path.resolve(process.env.AIDT_EVIDENCE_DIR)
  : path.resolve(testDirectory, '../../docs/project/test-artifacts')
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://127.0.0.1:5173'

const browsers = [
  {
    name: 'edge',
    executablePath: 'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  },
  {
    name: 'chrome',
    executablePath: `${process.env.LOCALAPPDATA}/Google/Chrome/Application/chrome.exe`,
  },
]

const releaseViewports = [
  { width: 1920, height: 1080, zoom: 1, label: '1920x1080' },
  { width: 1536, height: 864, zoom: 1.25, label: '1920x1080-zoom125' },
  { width: 1440, height: 900, zoom: 1, label: '1440x900' },
  { width: 1366, height: 768, zoom: 1, label: '1366x768-zoom100' },
  { width: 1093, height: 614, zoom: 1.25, label: '1366x768-zoom125' },
  { width: 911, height: 512, zoom: 1.5, label: '1366x768-zoom150' },
  { width: 1280, height: 720, zoom: 1, label: '1280x720' },
  { width: 1024, height: 768, zoom: 1, label: '1024x768' },
  { width: 820, height: 900, zoom: 1, label: '820x900' },
  { width: 600, height: 518, zoom: 1, label: '600x518' },
  { width: 390, height: 844, zoom: 1, label: '390x844' },
]

async function loginAsAdmin(page: Page) {
  await page.goto('/login')
  await page.getByTestId('login-user').click()
  await page.getByRole('option', { name: '平台管理员', exact: true }).click()
  await page.getByTestId('login-submit').click()
  await expect(page).toHaveURL(/\/$/)
  await expect(page.getByTestId('my-work-page')).toBeVisible()
}

async function assertNoPageOverflow(page: Page) {
  const metrics = await page.evaluate(() => {
    const app = document.querySelector<HTMLElement>('.app-layout')
    return {
      documentOverflow: document.documentElement.scrollWidth - window.innerWidth,
      appOverflow: app ? app.scrollWidth - app.clientWidth : 0,
    }
  })
  expect(metrics.documentOverflow).toBeLessThanOrEqual(1)
  expect(metrics.appOverflow).toBeLessThanOrEqual(1)
}

async function assertDesktopShell(page: Page) {
  const sidebar = page.getByTestId('app-sidebar')
  const navigation = page.getByTestId('sidebar-navigation')
  const main = page.locator('.main-content')

  await expect(sidebar).toBeVisible()
  await expect(page.getByTestId('sidebar-drawer-trigger')).toBeHidden()
  expect(await sidebar.evaluate((element) => Math.round(element.getBoundingClientRect().top))).toBe(0)
  expect(await navigation.evaluate((element) => getComputedStyle(element).overflowY)).toBe('auto')

  const active = page.locator('[data-testid="sidebar-navigation"] .nav-link.active').first()
  await expect(active).toBeVisible()
  const [activeBox, initialNavBox] = await Promise.all([active.boundingBox(), navigation.boundingBox()])
  expect(activeBox).not.toBeNull()
  expect(initialNavBox).not.toBeNull()
  expect(activeBox!.y).toBeGreaterThanOrEqual(initialNavBox!.y - 1)
  expect(activeBox!.y + activeBox!.height).toBeLessThanOrEqual(
    initialNavBox!.y + initialNavBox!.height + 1,
  )

  const openGroups = page.locator('details.department-group[open]')
  await expect(openGroups).toHaveCount(1)
  const closed = page.locator('details.department-group:not([open])').first()
  if (await closed.count()) {
    await closed.locator('summary').click()
    await expect(page.locator('details.department-group[open]')).toHaveCount(1)
  }

  await navigation.evaluate((element) => {
    element.scrollTop = element.scrollHeight
  })
  const navMetrics = await navigation.evaluate((element) => ({
    scrollTop: element.scrollTop,
    scrollHeight: element.scrollHeight,
    clientHeight: element.clientHeight,
  }))
  if (navMetrics.scrollHeight > navMetrics.clientHeight) {
    expect(navMetrics.scrollTop).toBeGreaterThan(0)
  }

  const footer = page.getByTestId('navigation-preferences')
  await footer.scrollIntoViewIfNeeded()
  await expect(footer).toBeVisible()
  const footerBottom = await footer.evaluate((element) => element.getBoundingClientRect().bottom)
  expect(footerBottom).toBeLessThanOrEqual((await page.evaluate(() => window.innerHeight)) + 1)

  await main.evaluate((element) => {
    element.scrollTop = 3000
  })
  expect(await main.evaluate((element) => element.scrollTop)).toBeGreaterThan(0)
  expect(await sidebar.evaluate((element) => Math.round(element.getBoundingClientRect().top))).toBe(0)
}

async function assertDrawerShell(page: Page, viewportHeight: number) {
  const trigger = page.getByTestId('sidebar-drawer-trigger')
  const sidebar = page.getByTestId('app-sidebar')
  await expect(trigger).toBeVisible()
  await expect(sidebar).toHaveAttribute('aria-hidden', 'true')
  await trigger.click()
  await expect(sidebar).toHaveClass(/drawer-open/)
  await expect(page.getByTestId('sidebar-backdrop')).toBeVisible()
  await expect(sidebar).toBeInViewport()
  expect((await sidebar.boundingBox())?.width ?? 999).toBeLessThanOrEqual(281)

  const navigation = page.getByTestId('sidebar-navigation')
  await navigation.evaluate((element) => {
    element.scrollTop = element.scrollHeight
  })
  const footer = sidebar.getByTestId('navigation-preferences')
  await expect(footer).toBeVisible()
  const footerBottom = await footer.evaluate((element) => element.getBoundingClientRect().bottom)
  expect(footerBottom).toBeLessThanOrEqual(viewportHeight + 1)

  await page.keyboard.press('Escape')
  await expect(sidebar).not.toHaveClass(/drawer-open/)
  await expect(trigger).toBeFocused()
}

async function assertTableOwnsHorizontalScroll(page: Page) {
  await page.goto('/objects')
  const table = page.locator('.main-content .el-table').first()
  await expect(table).toBeVisible()
  const metrics = await table.evaluate((element) => {
    const wrapper = element.querySelector<HTMLElement>('.el-scrollbar__wrap')
    const main = element.closest<HTMLElement>('.main-content')
    return {
      tableClientWidth: element.clientWidth,
      tableScrollWidth: element.scrollWidth,
      wrapperClientWidth: wrapper?.clientWidth ?? 0,
      wrapperScrollWidth: wrapper?.scrollWidth ?? 0,
      mainClientWidth: main?.clientWidth ?? 0,
      mainScrollWidth: main?.scrollWidth ?? 0,
    }
  })
  expect(metrics.tableScrollWidth).toBeLessThanOrEqual(metrics.tableClientWidth + 1)
  expect(metrics.mainScrollWidth).toBeLessThanOrEqual(metrics.mainClientWidth + 1)
  expect(metrics.wrapperScrollWidth).toBeGreaterThanOrEqual(metrics.wrapperClientWidth)
}

async function captureForBrowser(
  browser: Browser,
  browserName: string,
  viewport: (typeof releaseViewports)[number],
) {
  const context: BrowserContext = await browser.newContext({
    baseURL,
    viewport: { width: viewport.width, height: viewport.height },
    deviceScaleFactor: viewport.zoom,
    locale: 'zh-CN',
    timezoneId: 'Asia/Shanghai',
  })
  try {
    const page = await context.newPage()
    await loginAsAdmin(page)
    await page.goto('/minutes')
    await expect(page.getByTestId('minutes-page')).toBeVisible()
    await assertNoPageOverflow(page)

    if (viewport.width > 820) {
      await assertDesktopShell(page)
    } else {
      await assertDrawerShell(page, viewport.height)
    }

    await page.goto('/minutes')
    await expect(page.getByTestId('minutes-page')).toBeVisible()
    const zoomPercent = Math.round(viewport.zoom * 100)
    await page.screenshot({
      path: path.join(
        evidenceDir,
        `ux005-release-${browserName}-${viewport.label}-${zoomPercent}.png`,
      ),
    })

    if (viewport.width > 820 && (viewport.width <= 1366 || viewport.zoom > 1)) {
      await assertTableOwnsHorizontalScroll(page)
    }

    await page.goto('/')
    await expect(page.getByTestId('my-work-page')).toBeVisible()
    await assertNoPageOverflow(page)
    await page.screenshot({
      path: path.join(
        evidenceDir,
        `ux005-release-my-work-${browserName}-${viewport.label}.png`,
      ),
    })
  } finally {
    await context.close()
  }
}

test('@desktop UX-005 release matrix covers Edge/Chrome, zoom and critical widths', async () => {
  test.setTimeout(300_000)
  await fs.mkdir(evidenceDir, { recursive: true })
  for (const browserConfig of browsers) {
    const browser = await chromium.launch({
      headless: true,
      executablePath: browserConfig.executablePath,
    })
    try {
      for (const viewport of releaseViewports) {
        await captureForBrowser(browser, browserConfig.name, viewport)
      }
    } finally {
      await browser.close()
    }
  }
})

test('@desktop deployed root shell always revalidates the cached application entry', async ({
  request,
}) => {
  test.skip(
    process.env.PLAYWRIGHT_USE_EXISTING_DEPLOY !== '1',
    'Only the deployed Nginx container can verify release cache headers.',
  )
  const response = await request.get(new URL('/', baseURL).toString(), {
    headers: { 'Cache-Control': 'no-cache' },
  })
  expect(response.ok()).toBeTruthy()
  expect((await response.headers())['cache-control']).toContain('no-store')
})
