import { LilyApiError } from "@lily_platform/lily_ui/errors";
import { describe, expect, it } from "vitest";

import { isScheduleVersionConflict, schedulingErrorMessage } from "./schedulingErrors";

const t = (key: string) => key;

describe("schedulingErrorMessage", () => {
  it("maps stable scheduling error codes to feature messages", () => {
    const error = new LilyApiError({ code: "scheduling.time_off_overlap" });

    expect(schedulingErrorMessage(error, t, "fallback")).toBe(
      "app:scheduling.errors.timeOffOverlap",
    );
  });

  it("maps stale schedule revisions without using server message text", () => {
    const error = new LilyApiError({ code: "scheduling.schedule_version_conflict" });

    expect(schedulingErrorMessage(error, t, "fallback")).toBe(
      "app:scheduling.errors.scheduleVersionConflict",
    );
    expect(isScheduleVersionConflict(error)).toBe(true);
  });

  it("does not classify other failures as stale schedule revisions", () => {
    expect(isScheduleVersionConflict(new Error("network"))).toBe(false);
    expect(
      isScheduleVersionConflict(new LilyApiError({ code: "scheduling.invalid_schedule" })),
    ).toBe(false);
  });

  it("includes the trace id for unexpected server failures", () => {
    const error = new LilyApiError({
      code: "common.unexpected_error",
      traceId: "trace-123",
      statusCode: 500,
    });

    expect(schedulingErrorMessage(error, t, "fallback")).toBe(
      "app:scheduling.errors.unexpected app:scheduling.errors.traceId: trace-123",
    );
  });

  it("uses the caller fallback for unknown errors", () => {
    expect(schedulingErrorMessage(new Error("network"), t, "fallback")).toBe("fallback");
  });
});
