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

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

export function decodeHealthReport(body: unknown): HealthReport {
  if (!isRecord(body)) {
    throw new TypeError("Health report must be an object.");
  }

  const { status, durationMilliseconds, traceId, checks } = body;
  if (
    typeof status !== "string" ||
    typeof durationMilliseconds !== "number" ||
    typeof traceId !== "string" ||
    !isRecord(checks)
  ) {
    throw new TypeError("Health report fields are invalid.");
  }

  const decodedChecks: Record<string, HealthCheckResult> = {};
  for (const [name, value] of Object.entries(checks)) {
    if (!isRecord(value)) {
      throw new TypeError(`Health check ${name} must be an object.`);
    }

    const checkStatus = value.status;
    const description = value.description;
    const checkDuration = value.durationMilliseconds;
    if (
      typeof checkStatus !== "string" ||
      (description !== null && typeof description !== "string") ||
      typeof checkDuration !== "number"
    ) {
      throw new TypeError(`Health check ${name} fields are invalid.`);
    }

    decodedChecks[name] = {
      status: checkStatus,
      description,
      durationMilliseconds: checkDuration,
    };
  }

  return { status, durationMilliseconds, traceId, checks: decodedChecks };
}
