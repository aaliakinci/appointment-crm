import { requireNumber, requireRecord, requireString } from "@/shared/api/contractDecoder";

export interface HealthCheckResult {
  readonly status: string;
  readonly description: string | null;
  readonly durationMilliseconds: number;
}

export interface HealthReport {
  readonly status: string;
  readonly durationMilliseconds: number;
  readonly traceId: string;
  readonly checks: Readonly<Record<string, HealthCheckResult>>;
}

export function decodeHealthReport(body: unknown): HealthReport {
  const value = requireRecord(body, "health report");
  const checks = requireRecord(value.checks, "health report checks");
  const decodedChecks: Record<string, HealthCheckResult> = {};

  for (const [name, checkBody] of Object.entries(checks)) {
    const check = requireRecord(checkBody, `health check ${name}`);
    const description = check.description;
    if (description !== null && typeof description !== "string") {
      throw new TypeError(`Health check ${name} description is invalid.`);
    }

    decodedChecks[name] = {
      status: requireString(check.status, `health check ${name}.status`),
      description,
      durationMilliseconds: requireNumber(
        check.durationMilliseconds,
        `health check ${name}.durationMilliseconds`,
      ),
    };
  }

  return {
    status: requireString(value.status, "health report status"),
    durationMilliseconds: requireNumber(
      value.durationMilliseconds,
      "health report durationMilliseconds",
    ),
    traceId: requireString(value.traceId, "health report traceId"),
    checks: decodedChecks,
  };
}
