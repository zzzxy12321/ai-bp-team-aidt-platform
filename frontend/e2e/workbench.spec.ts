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

async function logout(page: Page) {
  await page.getByTestId('logout-button').click()
  await expect(page).toHaveURL(/\/login/)
}

async function expectWorkbench(
  page: Page,
  roleLabel: string,
  templateCode: string,
  title: string,
) {
  await loginAs(page, roleLabel)
  await page.goto('/department')
  const workbenchPage = page.getByTestId('department-page')
  await expect(workbenchPage).toBeVisible()
  await expect(workbenchPage).toHaveAttribute('data-template-code', templateCode)
  await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible()
}

test('@desktop 六类岗位工作台按部门角色显示不同真实首屏', async ({ page }) => {
  await expectWorkbench(page, '研发工程师', 'WS-RND', '研发工作台')
  await expect(page.getByRole('heading', { name: '我的研发事项', exact: true })).toBeVisible()

  await logout(page)
  await expectWorkbench(page, '质量工程师', 'WS-QA', '质量工作台')
  await expect(page.getByRole('heading', { name: '质量待办', exact: true })).toBeVisible()

  await logout(page)
  await expectWorkbench(page, '供应链专员', 'WS-SCM', '供应链工作台')
  await expect(page.getByRole('heading', { name: '采购请购', exact: true })).toBeVisible()

  await logout(page)
  await expectWorkbench(page, '财务审批人', 'WS-FIN', '财务工作台')
  await expect(page.getByRole('heading', { name: '采购付款审批', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: '合同和发票证据', exact: true })).toBeVisible()

  await logout(page)
  await expectWorkbench(page, '项目经理', 'WS-PM', '项目经理工作台')
  await expect(page.getByRole('heading', { name: '我负责的项目', exact: true })).toBeVisible()
  await expect(page.getByRole('heading', { name: '里程碑和任务风险', exact: true })).toBeVisible()

  await logout(page)
  await expectWorkbench(page, '平台管理员', 'WS-MGR', '经理工作台')
  await expect(page.getByRole('heading', { name: '部门逾期风险', exact: true })).toBeVisible()

  await page.screenshot({
    path: path.join(evidenceDir, 'ws002-manager-1440.png'),
    fullPage: true,
  })
})

test('@desktop 无匹配岗位模板时回退通用部门工作区', async ({ page }) => {
  await loginAs(page, '生产班组')
  await page.goto('/department')
  await expect(page.getByTestId('department-page')).toHaveAttribute(
    'data-template-code',
    'FALLBACK',
  )
  await expect(page.getByText('已回退通用部门工作区')).toBeVisible()
  await expect(page.getByText('部门待办', { exact: true })).toBeVisible()
})

test('@desktop IAM 工作台模板配置页展示版本、卡片和发布边界', async ({ page }) => {
  await loginAs(page, '平台管理员')
  await page.goto('/iam')
  await page.getByRole('tab', { name: '工作台模板' }).click()
  await expect(page.getByTestId('iam-workbench-panel')).toBeVisible()
  await expect(page.getByRole('heading', { name: '研发工作台', exact: true })).toBeVisible()
  await expect(page.getByText('系统模板不能删除或停用')).toBeVisible()
  await expect(page.getByRole('button', { name: '保存草稿' })).toBeVisible()
  await expect(page.getByRole('button', { name: '版本历史' })).toBeVisible()
  await expect(page.getByTestId('publish-workbench-template')).toBeVisible()
})

test('@desktop1280 供应链工作台在 1280 宽度保持真实差异', async ({ page }) => {
  await expectWorkbench(page, '供应链专员', 'WS-SCM', '供应链工作台')
  await expect(page.getByRole('heading', { name: '合同/供应商文件', exact: true })).toBeVisible()
})

test('@mobile 研发工作台在 390px 单列且无横向溢出', async ({ page }) => {
  await expectWorkbench(page, '研发工程师', 'WS-RND', '研发工作台')
  const overflow = await page.evaluate(
    () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
  )
  expect(overflow).toBeLessThanOrEqual(1)
  await expect(page.getByRole('heading', { name: '研发文件', exact: true })).toBeVisible()
  await page.screenshot({
    path: path.join(evidenceDir, 'ws002-research-390.png'),
    fullPage: true,
  })
})
