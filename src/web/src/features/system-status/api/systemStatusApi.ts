import { appHttpClient } from "@/shared/api";

import { decodeHealthReport, type HealthReport } from "./healthContract";

export function getReadiness(signal?: AbortSignal): Promise<HealthReport> {
  return appHttpClient.getData<HealthReport>("/health/ready", {
    signal,
    decode: decodeHealthReport,
    metadata: { operationName: "system.readiness" },
  });
}
