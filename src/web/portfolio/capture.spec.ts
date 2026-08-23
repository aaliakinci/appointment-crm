import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { mkdir } from "node:fs/promises";
import path from "node:path";

const password = process.env.APPOINTMENTCRM_PORTFOLIO_PASSWORD ?? "Portfolio-local-2026!";
const customerId = "40000000-0000-0000-0000-000000000001";
const serviceId = "50000000-0000-0000-0000-000000000001";
const employeeId = "60000000-0000-0000-0000-000000000001";
const screenshotDirectory = path.resolve(process.cwd(), "../../docs/assets/screenshots");
const assetDirectory = path.resolve(screenshotDirectory, "..");
const portfolioLanguage = process.env.APPOINTMENTCRM_PORTFOLIO_LANGUAGE === "en" ? "en" : "tr";
const captureEnglishScreenshots = portfolioLanguage === "en";
const seedAppointment = process.env.APPOINTMENTCRM_PORTFOLIO_SEED !== "false";
const tourPause = 1_600;

interface AuthenticationResponse {
  readonly accessToken: string;
}

interface AvailabilityResponse {
  readonly slots: readonly { readonly startUtc: string }[];
}

async function loginApi(request: APIRequestContext): Promise<string> {
  const response = await request.post("/api/v1/auth/login", {
    data: { email: "manager@demo.local", password, tenantId: null },
  });
  expect(response.ok()).toBe(true);
  return ((await response.json()) as AuthenticationResponse).accessToken;
}

async function seedPortfolioAppointment(request: APIRequestContext, origin: string) {
  const accessToken = await loginApi(request);
  const date = nextMondayInIstanbul();
  const headers = { Authorization: `Bearer ${accessToken}`, Origin: origin };
  const availability = await request.get("/api/v1/availability", {
    headers,
    params: { date, employeeId, serviceId },
  });
  expect(availability.ok()).toBe(true);
  const slots = ((await availability.json()) as AvailabilityResponse).slots;
  expect(slots.length).toBeGreaterThan(0);

  const created = await request.post("/api/v1/appointments", {
    headers,
    data: {
      customerId,
      employeeId,
      serviceId,
      startsAtUtc: slots[0].startUtc,
      notes: "Portfolio demo appointment",
    },
  });
  expect(created.status()).toBe(201);
}

async function loginUi(page: Page) {
  await page.goto("/#/login");
  const localeButton = page.locator(`[id$=".locale.${portfolioLanguage}"]`);
  await localeButton.click();
  await expect(localeButton).toHaveAttribute("aria-pressed", "true");
  await page.locator('input[autocomplete="username"]').fill("manager@demo.local");
  await page.locator('input[autocomplete="current-password"]').fill(password);
  await page.locator('button[type="submit"]').click();
  await expect(page).toHaveURL(/#\/dashboard$/);
}

async function navigateTo(page: Page, path: string) {
  await page.locator(`[id$=".navigation.${path}"]`).click();
  await expect(page).toHaveURL(new RegExp(`#/${path}$`));
  await page.waitForTimeout(tourPause);
}

function nextMondayInIstanbul(): string {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Europe/Istanbul",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date());
  const value = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  const today = new Date(`${value.year}-${value.month}-${value.day}T12:00:00+03:00`);
  const daysUntilNextMonday = (8 - today.getDay()) % 7 || 7;
  today.setUTCDate(today.getUTCDate() + daysUntilNextMonday);
  return today.toISOString().slice(0, 10);
}

test(`capture ${portfolioLanguage} portfolio tour from isolated demo data`, async ({
  page,
  request,
  baseURL,
}) => {
  expect(baseURL).toBeTruthy();
  await page.emulateMedia({ reducedMotion: "reduce" });
  await mkdir(screenshotDirectory, { recursive: true });
  if (seedAppointment) {
    await seedPortfolioAppointment(request, baseURL!);
  }
  await loginUi(page);

  await expect(page.locator('[id$=".summary"]')).toBeVisible();
  await page.waitForTimeout(tourPause);
  if (captureEnglishScreenshots) {
    await page.screenshot({
      path: path.join(screenshotDirectory, "dashboard.png"),
      fullPage: true,
    });
  }

  await navigateTo(page, "customers");
  await navigateTo(page, "services");
  await navigateTo(page, "employees");
  await navigateTo(page, "appointments");
  await page.locator('[id$=".toolbar.next"]').click();
  const appointment = page.getByText("Ayşe Demir", { exact: true }).first();
  await expect(appointment).toBeVisible();
  await page.waitForTimeout(tourPause);
  await appointment.click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.waitForTimeout(tourPause);
  if (captureEnglishScreenshots) {
    await page.screenshot({
      path: path.join(screenshotDirectory, "appointments.png"),
      fullPage: true,
    });
  }
  await page.waitForTimeout(tourPause);
  await page.locator('[id$=".detailDialog.close"]').click();

  await navigateTo(page, "scheduling");
  await expect(page.locator('[id$=".weeklySection.panel.overview.state"]')).toBeVisible();
  if (captureEnglishScreenshots) {
    await page.screenshot({
      path: path.join(screenshotDirectory, "scheduling.png"),
      fullPage: true,
    });
  }

  await navigateTo(page, "audit");
  await navigateTo(page, "dashboard");

  const video = page.video();
  await page.close();
  await video?.saveAs(path.join(assetDirectory, `appointment-crm-demo-${portfolioLanguage}.webm`));
});
