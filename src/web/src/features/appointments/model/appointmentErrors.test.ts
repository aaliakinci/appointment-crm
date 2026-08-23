import { LilyApiError } from "@lily_platform/lily_ui/errors";
import { describe, expect, it } from "vitest";

import { appointmentErrorMessage } from "./appointmentErrors";

const t = (key: string) => key;

describe("appointmentErrorMessage", () => {
  it.each([
    ["appointments.slot_unavailable", "app:appointments.errors.slotUnavailable"],
    ["appointments.time_conflict", "app:appointments.errors.timeConflict"],
    ["appointments.invalid_transition", "app:appointments.errors.invalidTransition"],
    ["appointments.version_conflict", "app:appointments.errors.versionConflict"],
  ])("maps stable code %s without depending on server detail", (code, expected) => {
    expect(appointmentErrorMessage(new LilyApiError({ code }), t, "fallback")).toBe(expected);
  });

  it("preserves trace id on an unexpected API failure", () => {
    expect(
      appointmentErrorMessage(
        new LilyApiError({
          code: "common.unexpected_error",
          statusCode: 500,
          traceId: "trace-release-123",
        }),
        t,
        "app:appointments.saveError",
      ),
    ).toBe("app:appointments.saveError app:appointments.traceId: trace-release-123");
  });

  it("uses the caller fallback for network errors", () => {
    expect(appointmentErrorMessage(new Error("offline"), t, "fallback")).toBe("fallback");
  });
});
