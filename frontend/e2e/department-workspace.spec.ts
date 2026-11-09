import { expect, test, type Page } from '@playwright/test'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const evidenceDir = process.env.AIDT_EVIDENCE_DIR
  ? path.resolve(process.env.AIDT_EVIDENCE_DIR)
  : path.resolve(
      fileURLToPath(new URL('.', import.meta.url)),
      '../../docs/project/test-artifacts',
    )

async function loginAs(page: Page, roleLabel: string) {
  await page.goto('/login')
  await page.getByTestId('login-user').click()
  await page.getByRole('option', { name: roleLabel, exact: true }).click()
  await page.getByTestId('login-submit').click()
  await expect(page).toHaveURL(/\/$/)
  await expect(page.getByTestId('my-work-page')).toBeVisible()
}

test('@desktop @desktop1280 部门流程负责人通过可视化配置中心创建节点字段模板', async ({
  page,
}) => {
  await loginAs(page, '供应链专员')
  const companyLabel = page.getByText('公司公共入口', { exact: true })
  await expect(companyLabel).toBeVisible()
  const supply = page.locator('details.department-group').filter({ hasText: '供应链' }).first()
  await expect(supply).toBeVisible()
  await expect(supply).toHaveAttribute('open', '')
  await expect(supply.getByRole('link', { name: '部门概览' })).toBeVisible()
  await expect(supply.getByRole('link', { name: '流程配置' })).toBeVisible()
  await expect(supply.getByRole('link', { name: '部门文件' })).toBeVisible()
  await expect(supply.getByRole('link', { name: 'AI 听记' })).toBeVisible()
  await expect(supply.getByRole('link', { name: '岗位业务场景' })).toBeVisible()
  await expect(supply.getByRole('link', { name: '数字员工身份' })).toBeVisible()

  await supply.getByRole('link', { name: '流程配置' }).click()
  await expect(page.getByTestId('configuration-center-page')).toBeVisible()
  await page.getByRole('button', { name: '新建配置', exact: true }).click()
  const dialog = page.getByRole('dialog', { name: '新建流程配置' })
  const marker = Date.now()
  await dialog.getByLabel('配置编号').fill(`DEMO-SCM-${marker}`)
  await dialog.getByLabel('配置名称').fill(`供应链 Demo 节点表单 ${marker}`)
  await dialog.getByLabel('配置说明').fill('四部门试点 Demo：验证部门负责人网页建模板和节点动态字段。')
  const createResponse = page.waitForResponse(
    (response) =>
      response.url().endsWith('/api/v1/configurations') &&
      response.request().method() === 'POST',
  )
  await dialog.getByRole('button', { name: '创建草稿', exact: true }).click()
  expect((await createResponse).ok()).toBeTruthy()
  await expect(page.getByTestId('configuration-node-0')).toContainText('发起事项')

  await page.getByRole('button', { name: '单行文本', exact: true }).click()
  await expect(page.getByText('员工实际看到的表单', { exact: true })).toBeVisible()
  await expect(
    page.locator('.preview-section').getByText('单行文本', { exact: true }).first(),
  ).toBeVisible()

  await page.getByRole('button', { name: '保存草稿', exact: true }).click()
  await expect(page.getByText('配置草稿已保存，Revision 已更新')).toBeVisible()
  await page.getByRole('button', { name: '校验', exact: true }).click()
  await expect(page.getByText('配置校验通过，可以提交发布。')).toBeVisible()
  await page.getByRole('button', { name: '发布', exact: true }).click()
  const publishDialog = page.getByRole('dialog', { name: '确认发布配置' })
  await publishDialog.getByRole('button', { name: '发布', exact: true }).click()
  await expect(page.getByText(/已发布流程版本/)).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, 'department-workflow-designer.png'),
    fullPage: true,
  })
})

test('@mobile 390px 部门负责人可从部门导航进入流程配置与业务表单预览', async ({ page }) => {
  await loginAs(page, '交付专员（Demo）')
  await page.getByTestId('sidebar-drawer-trigger').click()
  await expect(page.getByTestId('app-sidebar')).toHaveClass(/drawer-open/)
  await expect(page.getByText('公司公共入口', { exact: true })).toBeVisible()
  const delivery = page.locator('details.department-group').filter({ hasText: '交付中心' }).first()
  await expect(delivery).toBeVisible()
  await expect(delivery.getByText('Demo', { exact: true })).toBeVisible()
  const configurationLink = delivery.getByRole('link', { name: '流程配置' })
  await configurationLink.scrollIntoViewIfNeeded()
  await configurationLink.click()
  await expect(page.getByTestId('configuration-center-page')).toBeVisible()
  const bodyWidth = await page.evaluate(() => document.documentElement.scrollWidth)
  expect(bodyWidth).toBeLessThanOrEqual(390)
  await page.screenshot({
    path: path.join(evidenceDir, 'department-workspace-390.png'),
    fullPage: true,
  })
})
