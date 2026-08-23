import AxeBuilder from "@axe-core/playwright";
import { expect, test, type Page } from "@playwright/test";

const password = process.env.APPOINTMENTCRM_E2E_PASSWORD ?? "Browser-local-2026!";

async function login(page: Page, email: string) {
  await page.goto("/#/login");
  await page.locator('input[autocomplete="username"]').fill(email);
  await page.locator('input[autocomplete="current-password"]').fill(password);
  await page.locator('button[type="submit"]').click();
  await expect(page).not.toHaveURL(/#\/login$/);
}

async function expectNoSeriousAccessibilityViolations(page: Page) {
  const result = await new AxeBuilder({ page })
    .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
    .analyze();
  const blockingViolations = result.violations.filter((violation) =>
    ["serious", "critical"].includes(violation.impact ?? ""),
  );

  expect(blockingViolations, JSON.stringify(blockingViolations, null, 2)).toEqual([]);
}

test("login validation and skip link are keyboard accessible without changing the hash route", async ({
  page,
}) => {
  await page.goto("/#/login");
  await expect(page.locator('input[autocomplete="username"]')).toBeVisible();

  await page.keyboard.press("Tab");
  const skipLink = page.locator('[id$=".skipLink"]');
  await expect(skipLink).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.locator('[id$=".main"]')).toBeFocused();
  await expect(page).toHaveURL(/#\/login$/);

  await page.locator('button[type="submit"]').click();
  await expect(page.locator('input[autocomplete="username"]')).toBeFocused();
  await expect(page.locator('input[autocomplete="username"]')).toHaveAttribute(
    "aria-invalid",
    "true",
  );
  await expectNoSeriousAccessibilityViolations(page);
});

test("manager navigation exposes authorized modules and renders a recoverable API error", async ({
  page,
}) => {
  await login(page, "manager@demo.local");
  await expect(page).toHaveURL(/#\/dashboard$/);

  await expect(page.locator('[id$=".navigation.dashboard"]')).toBeVisible();
  await expect(page.locator('[id$=".navigation.scheduling"]')).toBeVisible();
  await expect(page.locator('[id$=".navigation.team"]')).toBeVisible();

  await page.route("**/api/v1/customers?**", async (route) => {
    await route.fulfill({
      status: 500,
      contentType: "application/problem+json",
      body: JSON.stringify({
        title: "Internal Server Error",
        status: 500,
        code: "common.unexpected_error",
        traceId: "phase8-browser-trace",
      }),
    });
  });
  await page.locator('[id$=".navigation.customers"]').click();
  await expect(page).toHaveURL(/#\/customers$/);
  await expect(page.locator('[id$=".loadError"]')).toBeVisible();
  await expect(page.locator('[id$=".retry"]')).toBeVisible();
});

test("employee navigation hides privileged modules", async ({ page }) => {
  await login(page, "employee@demo.local");

  await expect(page.locator('[id$=".navigation.services"]')).toBeVisible();
  await expect(page.locator('[id$=".navigation.appointments"]')).toBeVisible();
  await expect(page.locator('[id$=".navigation.account"]')).toBeVisible();
  await expect(page.locator('[id$=".navigation.dashboard"]')).toHaveCount(0);
  await expect(page.locator('[id$=".navigation.scheduling"]')).toHaveCount(0);
  await expect(page.locator('[id$=".navigation.team"]')).toHaveCount(0);
  await expect(page.locator('[id$=".navigation.audit"]')).toHaveCount(0);
});

test("authenticated workspace passes accessibility and responsive overflow gates", async ({
  page,
}) => {
  await login(page, "manager@demo.local");

  for (const viewport of [
    { width: 375, height: 667 },
    { width: 768, height: 1024 },
    { width: 1440, height: 900 },
  ]) {
    await page.setViewportSize(viewport);
    await page.reload();
    await expect(page.locator('[id$=".main"]')).toBeVisible();
    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth,
    );
    expect(hasHorizontalOverflow, `Horizontal overflow at ${viewport.width}px`).toBe(false);
  }

  await page.setViewportSize({ width: 1440, height: 900 });
  await page.reload();
  await expectNoSeriousAccessibilityViolations(page);
});
