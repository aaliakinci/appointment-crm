import { describe, expect, it } from "vitest";

import {
  decodeAvailability,
  decodeWeeklySchedule,
  decodeWeeklyScheduleVersionPage,
} from "./schedulingContract";

describe("scheduling contracts", () => {
  it("retains UTC instants and explicit local offsets", () => {
    const availability = decodeAvailability({
      date: "2026-10-25",
      employeeId: "employee-a",
      serviceId: "service-a",
      serviceDurationMinutes: 30,
      timeZone: "Europe/Berlin",
      slots: [
        {
          startUtc: "2026-10-25T00:00:00+00:00",
          endUtc: "2026-10-25T00:30:00+00:00",
          localStart: "2026-10-25T02:00:00+02:00",
          localEnd: "2026-10-25T02:30:00+02:00",
        },
        {
          startUtc: "2026-10-25T01:00:00+00:00",
          endUtc: "2026-10-25T01:30:00+00:00",
          localStart: "2026-10-25T02:00:00+01:00",
          localEnd: "2026-10-25T02:30:00+01:00",
        },
      ],
    });

    expect(availability.slots.map((slot) => slot.localStart)).toEqual([
      "2026-10-25T02:00:00+02:00",
      "2026-10-25T02:00:00+01:00",
    ]);
    expect(new Set(availability.slots.map((slot) => slot.startUtc)).size).toBe(2);
  });

  it("rejects unknown weekly schedule sources", () => {
    expect(() =>
      decodeWeeklySchedule({
        employeeId: null,
        state: "custom",
        source: "cached",
        revision: 1,
        versionId: "version-1",
        versionNumber: 1,
        effectiveVersionId: "version-1",
        effectiveVersionNumber: 1,
        periods: [],
        publishedAtUtc: null,
        publishedBy: null,
        changeNote: null,
      }),
    ).toThrow(TypeError);
  });

  it("decodes immutable weekly schedule history metadata", () => {
    const page = decodeWeeklyScheduleVersionPage({
      items: [
        {
          id: "version-2",
          versionNumber: 2,
          mode: "closed",
          periods: [],
          createdAtUtc: "2026-08-22T17:00:00+00:00",
          publishedBy: null,
          changeNote: "Seasonal closure",
          restoredFromVersionId: "version-1",
          restoredFromVersionNumber: 1,
        },
      ],
      page: 1,
      pageSize: 10,
      totalCount: 1,
      totalPages: 1,
    });

    expect(page.items[0]).toMatchObject({
      versionNumber: 2,
      mode: "closed",
      publishedBy: null,
      restoredFromVersionId: "version-1",
      restoredFromVersionNumber: 1,
    });
  });
});
