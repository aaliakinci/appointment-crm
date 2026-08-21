import { describe, expect, it } from "vitest";

import { decodeHealthReport } from "./healthContract";

describe("decodeHealthReport", () => {
  it("accepts the documented readiness contract", () => {
    const result = decodeHealthReport({
      status: "Healthy",
      durationMilliseconds: 4.2,
      traceId: "f00d",
      checks: {
        postgresql: {
          status: "Healthy",
          description: "PostgreSQL is reachable.",
          durationMilliseconds: 3.4,
        },
      },
    });

    expect(result.checks.postgresql?.status).toBe("Healthy");
  });

  it("rejects an untyped or partial response", () => {
    expect(() => decodeHealthReport({ status: "Healthy" })).toThrow(
      "Health report fields are invalid",
    );
  });
});
