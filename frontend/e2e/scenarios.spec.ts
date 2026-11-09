import { expect, test, type Page } from "@playwright/test";

async function loginAs(page: Page, roleLabel: string) {
  await page.goto("/login");
  await page.getByTestId("login-user").click();
  await page.getByRole("option", { name: roleLabel, exact: true }).click();
  await page.getByTestId("login-submit").click();
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByTestId("my-work-page")).toBeVisible();
}

async function logout(page: Page) {
  await page.getByTestId("logout-button").click();
  await expect(page).toHaveURL(/\/login/);
}

test("@desktop 场景中心从 7A API 展示六类业务场景并真实发起样品转产", async ({
  page,
}) => {
  const objectCode = `SCN-UI-${Date.now()}`;
  await loginAs(page, "研发工程师");
  await page.goto("/scenarios");
  await expect(page.getByTestId("scenarios-page")).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "岗位业务场景" }),
  ).toBeVisible();
  await expect(
    page.getByText("受控 Demo 试点样例，不代表生产事项"),
  ).toBeVisible();

  const catalogResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/api/v1/scenarios?page=1") &&
      response.request().method() === "GET",
  );
  await page.reload();
  expect((await catalogResponse).ok()).toBeTruthy();

  const sampleCard = page
    .locator(".scenario-card")
    .filter({ hasText: "研发样品到转产" })
    .first();
  await expect(sampleCard).toBeVisible();
  await expect(sampleCard).toContainText("研发工程师、质量工程师");
  await expect(sampleCard).toContainText("需要审批");
  await expect(sampleCard).toContainText("永久人工边界");
  await sampleCard.getByRole("button", { name: "开始办理" }).click();

  const wizard = page.getByTestId("scenario-start-wizard");
  await expect(wizard).toBeVisible();
  await expect(wizard).toContainText("我现在要办什么");
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();

  await wizard.getByPlaceholder("例如 SAMPLE-20261103-01").fill(objectCode);
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "事项名称" })
    .locator("input, textarea")
    .fill("UI 样品到转产试点");
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "客户（必填）" })
    .locator("input, textarea")
    .fill("内部演示客户");
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "产品（必填）" })
    .locator("input, textarea")
    .fill("苯丙乳液样品");
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "客户需求（必填）" })
    .locator("input, textarea")
    .fill("验证真实场景发起链路");
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "是否试产" })
    .locator("input")
    .fill("Yes");
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  await expect(wizard).toContainText("补充文件与听记");
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  await expect(wizard).toContainText("审批路径预览");
  await expect(wizard).toContainText("不会执行：自动确认");

  const preflightResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/actions/preflight") &&
      response.request().method() === "POST",
  );
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  const preflight = await preflightResponse;
  expect(preflight.ok()).toBeTruthy();
  const preflightBody = await preflight.json();
  expect(preflightBody.data.canStart).toBe(true);
  await expect(wizard).toContainText("检查通过，可以进入真人确认");
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  await expect(wizard).toContainText("确认真人发起");
  await expect(wizard).toContainText("不会确认、审批或付款");

  const startResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/actions/start") &&
      response.request().method() === "POST",
  );
  await wizard.getByTestId("scenario-start-confirm").click();
  const started = await startResponse;
  expect(started.status()).toBe(201);
  const startedBody = await started.json();
  expect(startedBody.data.businessObjectId).toBeTruthy();
  expect(startedBody.data.workflowInstanceId).toBeTruthy();
  await expect(wizard).toContainText("场景已发起");
  await expect(wizard).toContainText("刷新或重新登录后可继续查看");
  await wizard.getByRole("button", { name: "关闭", exact: true }).click();
  await expect(page.getByText("我发起的场景").first()).toBeVisible();
  await expect(page.getByText("研发样品到转产").first()).toBeVisible();
  await expect(page.getByText("进行中").first()).toBeVisible();

  await logout(page);
  await loginAs(page, "研发工程师");
  await page.goto("/scenarios?view=my");
  await expect(
    page.locator(".launch-card").filter({ hasText: "研发样品到转产" }).first(),
  ).toBeVisible();
});

test("@desktop 配置中心与岗位工作台进入同一个场景中心", async ({ page }) => {
  await loginAs(page, "平台管理员");
  await page.goto("/configurations");
  const entry = page.getByRole("button", { name: "业务场景配置" });
  await expect(entry).toBeVisible();
  await entry.click();
  await expect(page).toHaveURL(/\/scenarios\?mode=manage/);
  await expect(page.getByTestId("scenario-management-panel")).toBeVisible();
  await expect(page.getByText("研发样品到转产").first()).toBeVisible();
  await expect(page.getByText("部门经理事项分派与审批").first()).toBeVisible();
  await expect(page.getByText("财务付款人工复核").first()).toBeVisible();
  await expect(page.getByText("质量异常到 CAPA").first()).toBeVisible();
  await expect(page.getByText("采购请购到到货验收").first()).toBeVisible();
  await expect(page.getByText("项目经理里程碑与风险").first()).toBeVisible();

  await page.goto("/department");
  await expect(
    page.getByRole("heading", { name: "业务场景", exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: "查看全部场景" }).click();
  await expect(page).toHaveURL(/\/scenarios/);
  await expect(page.getByTestId("scenarios-page")).toBeVisible();
});

test("@desktop 管理员从场景配置页完成草稿校验发布停用恢复", async ({
  page,
}) => {
  const code = `SCN-UI-MGR-${Date.now()}`;
  await loginAs(page, "平台管理员");
  await page.goto("/scenarios?mode=manage");
  await expect(page.getByTestId("scenario-management-panel")).toBeVisible();
  await page.getByRole("button", { name: "新建草稿" }).click();

  const createDialog = page.getByRole("dialog", { name: "新建场景草稿" });
  await expect(createDialog).toBeVisible();
  await createDialog.getByLabel("场景编码").fill(code);
  await createDialog.getByLabel("场景名称").fill("UI 经理事项分派试点");
  await createDialog
    .getByLabel("业务说明")
    .fill("验证场景配置从草稿到发布、停用和恢复的完整生命周期。");
  await createDialog
    .getByLabel("要解决的问题")
    .fill("把经理交办事项转成可追踪的真实任务。");
  await createDialog
    .getByLabel("什么时候应发起")
    .fill("经理收到口头事项或会议行动项时发起。");
  const createResponse = page.waitForResponse(
    (response) =>
      response.url().endsWith("/api/v1/scenarios/drafts") &&
      response.request().method() === "POST",
  );
  await createDialog.getByRole("button", { name: "创建草稿" }).click();
  const createdDraft = await createResponse;
  expect(createdDraft.status(), await createdDraft.text()).toBe(201);
  await expect(createDialog).toBeHidden();
  await expect(
    page.getByRole("heading", { name: "UI 经理事项分派试点" }),
  ).toBeVisible();

  const editor = page.locator(".admin-editor");
  await editor
    .locator(".el-form-item")
    .filter({ hasText: "要解决的问题" })
    .locator("textarea")
    .fill("把经理交办事项转成可追踪的真实任务。");
  await editor
    .locator(".el-form-item")
    .filter({ hasText: "什么时候应发起" })
    .locator("textarea")
    .fill("经理收到口头事项或会议行动项时发起。");
  await editor
    .locator(".el-form-item")
    .filter({ hasText: "适用岗位与业务角色" })
    .locator("textarea")
    .fill("部门负责人\n事项发起人");
  await editor.getByPlaceholder("步骤名称").fill("事项进入收件箱");
  await editor
    .getByPlaceholder("步骤业务说明")
    .fill("保留事项原文、来源和附件。");
  await editor
    .locator(".el-form-item")
    .filter({ hasText: "权限要求（业务可读说明）" })
    .locator("textarea")
    .fill("event-inbox:Create\nevent-inbox:Confirm");

  const saveResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/api/v1/scenarios/") &&
      response.url().includes("/drafts/") &&
      response.request().method() === "PUT",
  );
  await page.getByRole("button", { name: "保存草稿" }).click();
  const savedDraft = await saveResponse;
  expect(savedDraft.ok(), await savedDraft.text()).toBeTruthy();
  await expect(page.getByText("场景草稿已保存，Revision 已更新")).toBeVisible();

  const validateResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/actions/validate") &&
      response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "校验", exact: true }).click();
  const validation = await validateResponse;
  expect(validation.ok()).toBeTruthy();
  expect((await validation.json()).data.isValid).toBe(true);
  await expect(
    page.getByRole("dialog", { name: "场景校验结果" }),
  ).toBeVisible();
  await page.keyboard.press("Escape");

  const publishResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/actions/publish") &&
      response.request().method() === "POST",
  );
  await page.getByRole("button", { name: "发布", exact: true }).click();
  await page.getByRole("button", { name: "发布", exact: true }).last().click();
  expect((await publishResponse).ok()).toBeTruthy();
  await expect(page.getByText("场景版本已发布")).toBeVisible();

  await page.getByRole("button", { name: "停用", exact: true }).click();
  const disablePrompt = page.getByRole("dialog", { name: "停用场景" });
  await disablePrompt.locator("input").fill("UI 生命周期验证临时停用");
  await disablePrompt.getByRole("button", { name: "OK" }).click();
  await expect(page.getByText("场景已停用，历史数据未删除")).toBeVisible();
  await expect(
    page.getByText(/场景已停用：UI 生命周期验证临时停用/),
  ).toBeVisible();

  await page.getByRole("button", { name: "恢复", exact: true }).click();
  const restorePrompt = page.getByRole("dialog", { name: "恢复场景" });
  await restorePrompt.locator("input").fill("UI 生命周期验证恢复");
  await restorePrompt.getByRole("button", { name: "OK" }).click();
  await expect(page.getByText("场景已恢复，可新发起")).toBeVisible();

  await page.getByRole("button", { name: "停用", exact: true }).click();
  const cleanupPrompt = page.getByRole("dialog", { name: "停用场景" });
  await cleanupPrompt.locator("input").fill("自动化用例清理，保留历史证据");
  await cleanupPrompt.getByRole("button", { name: "OK" }).click();
  await expect(
    page.getByText("场景已停用，历史数据未删除").last(),
  ).toBeVisible();
});

test("@desktop1280 场景目录保持三列业务卡片且可进入详情", async ({ page }) => {
  await loginAs(page, "平台管理员");
  await page.goto("/scenarios");
  await expect(page.locator(".scenario-card")).toHaveCount(9);
  await expect(page.locator(".filter-panel")).toBeVisible();
  await page
    .locator(".scenario-card")
    .filter({ hasText: "财务付款人工复核" })
    .first()
    .click();
  const dialog = page.getByRole("dialog", { name: "财务付款人工复核" });
  await expect(dialog).toBeVisible();
  await expect(dialog).toContainText("永久人工边界");
  await expect(dialog).toContainText("财务付款");
});

test("@mobile 390px 可完成场景发起并查看下一步", async ({ page }) => {
  const objectCode = `SCN-MOBILE-${Date.now()}`;
  await loginAs(page, "研发工程师");
  await page.goto("/scenarios");
  await expect(page.getByTestId("scenarios-page")).toBeVisible();
  const noOverflow = await page.evaluate(
    () =>
      document.documentElement.scrollWidth <=
      document.documentElement.clientWidth + 1,
  );
  expect(noOverflow).toBeTruthy();
  await page
    .locator(".scenario-card")
    .filter({ hasText: "研发样品到转产" })
    .first()
    .click();
  const dialog = page.getByRole("dialog", { name: "研发样品到转产" });
  await expect(dialog).toBeVisible();
  await expect(
    dialog.getByRole("button", { name: "开始办理" }).last(),
  ).toBeVisible();
  await dialog.getByRole("button", { name: "开始办理" }).last().click();
  const wizard = page.getByTestId("scenario-start-wizard");
  await expect(wizard).toContainText("七步业务发起");
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  await wizard.getByPlaceholder("例如 SAMPLE-20261103-01").fill(objectCode);
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "事项名称" })
    .locator("input, textarea")
    .fill("移动端样品转产试点");
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "客户（必填）" })
    .locator("input, textarea")
    .fill("内部演示客户");
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "产品（必填）" })
    .locator("input, textarea")
    .fill("苯丙乳液样品");
  await wizard
    .locator(".el-form-item")
    .filter({ hasText: "客户需求（必填）" })
    .locator("input, textarea")
    .fill("验证移动端完整发起");
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  const preflightResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/actions/preflight") &&
      response.request().method() === "POST",
  );
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  expect((await preflightResponse).ok()).toBeTruthy();
  await wizard.getByRole("button", { name: "下一步", exact: true }).click();
  const startResponse = page.waitForResponse(
    (response) =>
      response.url().includes("/actions/start") &&
      response.request().method() === "POST",
  );
  await wizard.getByTestId("scenario-start-confirm").click();
  expect((await startResponse).status()).toBe(201);
  await expect(wizard).toContainText("场景已发起");
  await expect(wizard).toContainText("刷新或重新登录后可继续查看");
});
