import { describe, expect, it } from "vitest";

import { addDays, startOfIsoWeek, tenantToday, weekDates } from "./appointmentDate";

describe("appointment tenant-local date helpers", () => {
  it("derives the tenant date without using the browser time zone", () => {
    const instant = new Date("2026-08-23T22:30:00Z");
    expect(tenantToday("Europe/Istanbul", instant)).toBe("2026-08-24");
    expect(tenantToday("America/New_York", instant)).toBe("2026-08-23");
  });

  it("builds ISO Monday weeks with date-only arithmetic", () => {
    expect(startOfIsoWeek("2026-08-23")).toBe("2026-08-17");
    expect(weekDates("2026-08-17")).toEqual([
      "2026-08-17",
      "2026-08-18",
      "2026-08-19",
      "2026-08-20",
      "2026-08-21",
      "2026-08-22",
      "2026-08-23",
    ]);
    expect(addDays("2026-10-25", 1)).toBe("2026-10-26");
  });
});
