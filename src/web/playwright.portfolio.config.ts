import { defineConfig, devices } from "@playwright/test";

const baseURL = process.env.APPOINTMENTCRM_PORTFOLIO_BASE_URL ?? "http://localhost:55176";
const portfolioLanguage = process.env.APPOINTMENTCRM_PORTFOLIO_LANGUAGE === "en" ? "en" : "tr";

export default defineConfig({
  testDir: "./portfolio",
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: [["line"]],
  use: {
    ...devices["Desktop Chrome"],
    baseURL,
    locale: portfolioLanguage === "en" ? "en-US" : "tr-TR",
    timezoneId: "Europe/Istanbul",
    viewport: { width: 1440, height: 900 },
    video: { mode: "on", size: { width: 1280, height: 800 } },
  },
});
