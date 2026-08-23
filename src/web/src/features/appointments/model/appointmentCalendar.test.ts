import { describe, expect, it } from "vitest";

import {
  appointmentCalendarRange,
  appointmentCalendarSelectionChanged,
} from "./appointmentCalendar";

describe("appointment calendar state", () => {
  it("uses the same date as both boundaries in day view", () => {
    expect(appointmentCalendarRange(["2026-08-23"])).toEqual({
      fromDate: "2026-08-23",
      toDate: "2026-08-23",
    });
  });

  it("uses the first and last dates in week view", () => {
    expect(
      appointmentCalendarRange([
        "2026-08-17",
        "2026-08-18",
        "2026-08-19",
        "2026-08-20",
        "2026-08-21",
        "2026-08-22",
        "2026-08-23",
      ]),
    ).toEqual({ fromDate: "2026-08-17", toDate: "2026-08-23" });
  });

  it("treats selecting the current value as a no-op", () => {
    expect(appointmentCalendarSelectionChanged("2026-08-23", "2026-08-23")).toBe(false);
    expect(appointmentCalendarSelectionChanged("week", "day")).toBe(true);
  });
});
