import { createLilyHttpClient } from "@lily_platform/lily_ui/http";

import { resolveApiBaseUrl } from "./apiBaseUrl";
import { decodeHealthReport, type HealthReport } from "./healthContract";

const configuredApiBaseUrl: unknown = import.meta.env.VITE_APPOINTMENT_CRM_API_URL;

export const appApiClient = createLilyHttpClient({
  baseURL: resolveApiBaseUrl(
    typeof configuredApiBaseUrl === "string" ? configuredApiBaseUrl : undefined,
  ),
  timeoutMs: 10_000,
  credentials: "include",
  defaultHeaders: { Accept: "application/json" },
});

export function getReadiness(signal?: AbortSignal): Promise<HealthReport> {
  return appApiClient.getData<HealthReport>("/health/ready", {
    signal,
    decode: decodeHealthReport,
    metadata: { operationName: "system.readiness" },
  });
}
