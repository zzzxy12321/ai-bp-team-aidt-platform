import { expect, test, type Page } from '@playwright/test'
import fs from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { introCatalog, type MainColumnIntroKey } from '../src/features/workbench-intro/introCatalog'

const testDirectory = fileURLToPath(new URL('.', import.meta.url))
const evidenceDir = process.env.AIDT_EVIDENCE_DIR
  ? path.resolve(process.env.AIDT_EVIDENCE_DIR)
  : path.resolve(testDirectory, '../../docs/project/test-artifacts')

const pages: Array<{ key: MainColumnIntroKey; route: string }> = [
  { key: 'my-work', route: '/' },
  { key: 'confirmations', route: '/confirmations' },
  { key: 'workflows', route: '/workflows' },
  { key: 'inbox', route: '/inbox' },
  { key: 'department', route: '/department' },
  { key: 'documents', route: '/documents' },
  { key: 'minutes', route: '/minutes' },
  { key: 'projects', route: '/projects' },
  { key: 'objects', route: '/objects' },
  { key: 'scenarios', route: '/scenarios' },
  { key: 'agents', route: '/agents' },
  { key: 'configurations', route: '/configurations' },
  { key: 'iam', route: '/iam' },
  { key: 'audit', route: '/audit' },
  { key: 'modeling', route: '/modeling' },
  { key: 'mcp-center', route: '/mcp-center' },
  { key: 'management', route: '/management' },
]

async function loginAs(page: Page, roleLabel = '平台管理员') {
  await page.goto('/login')
  await page.getByTestId('login-user').click()
  await page.getByRole('option', { name: roleLabel, exact: true }).click()
  await page.getByTestId('login-submit').click()
  await expect(page).toHaveURL(/\/$/)
  await expect(page.getByTestId('my-work-page')).toBeVisible()
}

async function expectNoPageOverflow(page: Page) {
  const metrics = await page.evaluate(() => {
    const app = document.querySelector<HTMLElement>('.app-layout')
    return {
      document: document.documentElement.scrollWidth - window.innerWidth,
      app: app ? app.scrollWidth - app.clientWidth : 0,
    }
  })
  expect(metrics.document).toBeLessThanOrEqual(1)
  expect(metrics.app).toBeLessThanOrEqual(1)
}

async function openAndVerifyIntro(page: Page, key: MainColumnIntroKey, screenshotName?: string) {
  const intro = introCatalog[key]
  const trigger = page.getByTestId('workbench-intro-trigger')
  await expect(trigger).toBeVisible()
  await trigger.click()

  const drawer = page.getByTestId('workbench-intro-drawer')
  await expect(drawer).toBeVisible()
  const panel = drawer.getByRole('dialog')
  await expect(panel.getByRole('heading', { level: 2 })).toHaveText(intro.title)
  await expect(drawer).toContainText('这个页面做什么')
  await expect(drawer).toContainText('业务操作路径')
  await expect(drawer).toContainText('业务例子')
  await expect(drawer).toContainText(intro.examples[0])
  await expect(drawer).toContainText(intro.examples[1])
  await expect(drawer).toContainText('数据来源与权限范围')
  await expect(drawer).toContainText('人工确认与安全边界')
  await expect(drawer).toContainText('建议的下一步')

  const scrollMetrics = await panel.evaluate((element) => ({
    overflowY: getComputedStyle(element).overflowY,
    overflowX: getComputedStyle(element).overflowX,
    clientWidth: element.clientWidth,
    scrollWidth: element.scrollWidth,
    clientHeight: element.clientHeight,
    scrollHeight: element.scrollHeight,
  }))
  expect(scrollMetrics.overflowY).toBe('auto')
  expect(scrollMetrics.overflowX).not.toBe('auto')
  expect(scrollMetrics.scrollWidth).toBeLessThanOrEqual(scrollMetrics.clientWidth + 1)

  if (screenshotName) {
    await page.screenshot({
      path: path.join(evidenceDir, screenshotName),
    })
  }

  await page.keyboard.press('Escape')
  await expect(drawer).toBeHidden()
  await expect(trigger).toBeFocused()
}

test('@desktop UX-006 every directly navigable main column exposes the correct introduction', async ({
  page,
}) => {
  test.setTimeout(240_000)
  await fs.mkdir(evidenceDir, { recursive: true })
  await page.setViewportSize({ width: 1920, height: 1080 })
  await loginAs(page)

  for (const item of pages) {
    await page.goto(item.route)
    await expectNoPageOverflow(page)
    await openAndVerifyIntro(page, item.key, `ux006-intro-${item.key}-1920.png`)
    await expect(page.locator('.page-lead')).toHaveCount(0)
  }
})
test('@desktop UX-006 introduction survives refresh and a new login', async ({ page }) => {
  await page.setViewportSize({ width: 1440, height: 900 })
  await loginAs(page)
  await expect(page.getByTestId('workbench-intro-trigger')).toBeVisible()

  await page.reload()
  await expect(page.getByTestId('my-work-page')).toBeVisible()
  await expect(page.getByTestId('workbench-intro-trigger')).toBeVisible()

  await page.getByTestId('logout-button').click()
  await expect(page).toHaveURL(/\/login/)
  await loginAs(page)
  await expect(page.getByTestId('workbench-intro-trigger')).toBeVisible()
})

test('@desktop UX-006 unauthorized users never see a main-column introduction', async ({ page }) => {
  await page.setViewportSize({ width: 1280, height: 720 })
  await loginAs(page, '研发工程师')
  await page.goto('/management')
  await expect(page).toHaveURL(/\/access-denied/)
  await expect(page.getByTestId('workbench-intro-trigger')).toHaveCount(0)
})

test('@desktop UX-006 release responsive matrix keeps the intro drawer usable', async ({ page }) => {
  test.setTimeout(180_000)
  await fs.mkdir(evidenceDir, { recursive: true })
  await loginAs(page)

  const viewports = [
    { width: 1920, height: 1080, label: '1920x1080' },
    { width: 1366, height: 768, label: '1366x768-100' },
    { width: 1093, height: 614, label: '1366x768-125' },
    { width: 911, height: 512, label: '1366x768-150' },
    { width: 1280, height: 720, label: '1280x720' },
    { width: 820, height: 900, label: '820x900' },
    { width: 600, height: 518, label: '600x518' },
    { width: 390, height: 844, label: '390x844' },
    { width: 320, height: 720, label: '320x720' },
  ]

  for (const viewport of viewports) {
    await page.setViewportSize({ width: viewport.width, height: viewport.height })
    await page.goto('/')
    await expectNoPageOverflow(page)
    const trigger = page.getByTestId('workbench-intro-trigger')
    await trigger.click()
    const drawer = page.getByTestId('workbench-intro-drawer')
    await expect(drawer).toBeVisible()
    const panel = drawer.getByRole('dialog')

    const box = await panel.boundingBox()
    expect(box).not.toBeNull()
    expect((box?.width ?? 0)).toBeLessThanOrEqual(viewport.width + 1)
    if (viewport.width <= 720) {
      expect((box?.width ?? 0)).toBeGreaterThanOrEqual(viewport.width - 1)
    } else {
      expect((box?.width ?? 0)).toBeLessThanOrEqual(421)
    }

    const metrics = await panel.evaluate((element) => {
      element.scrollTop = element.scrollHeight
      return {
        clientHeight: element.clientHeight,
        scrollHeight: element.scrollHeight,
        clientWidth: element.clientWidth,
        scrollWidth: element.scrollWidth,
        scrollTop: element.scrollTop,
      }
    })
    expect(metrics.scrollWidth).toBeLessThanOrEqual(metrics.clientWidth + 1)
    if (metrics.scrollHeight > metrics.clientHeight) {
      expect(metrics.scrollTop).toBeGreaterThan(0)
    }
    await expect(drawer.getByText('建议的下一步', { exact: true })).toBeVisible()

    await page.screenshot({
      path: path.join(evidenceDir, `ux006-responsive-${viewport.label}.png`),
    })
    await page.keyboard.press('Escape')
    await expect(trigger).toBeFocused()
  }
})

test('@desktop UX-006 shell screenshots include full sidebar and navigation bottom', async ({
  page,
}) => {
  await fs.mkdir(evidenceDir, { recursive: true })
  await page.setViewportSize({ width: 1920, height: 1080 })
  await loginAs(page)
  await page.goto('/documents')
  await expect(page.getByTestId('app-sidebar')).toBeInViewport()
  await page.screenshot({
    path: path.join(evidenceDir, 'ux006-shell-1920-sidebar.png'),
  })

  const navigation = page.getByTestId('sidebar-navigation')
  await navigation.evaluate((element) => {
    element.scrollTop = element.scrollHeight
  })
  await expect(page.getByTestId('navigation-preferences')).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, 'ux006-shell-1920-nav-bottom.png'),
  })
})
