import { describe, expect, it } from "vitest";

import {
  applySchedulingNavigation,
  isCurrentSchedulingLocation,
  isSchedulingTab,
  type SchedulingLocation,
} from "./schedulingNavigation";

const location: SchedulingLocation = {
  activeTab: "weekly",
  weeklyScope: "tenant",
  overrideScope: "tenant",
};

describe("scheduling navigation", () => {
  it("applies typed tab and scope targets without changing unrelated state", () => {
    expect(applySchedulingNavigation(location, { kind: "tab", tab: "timeOff" })).toEqual({
      ...location,
      activeTab: "timeOff",
    });
    expect(
      applySchedulingNavigation(location, { kind: "weeklyScope", scope: "employee-1" }),
    ).toEqual({ ...location, weeklyScope: "employee-1" });
  });

  it("recognizes no-op targets so dirty forms are not prompted unnecessarily", () => {
    expect(isCurrentSchedulingLocation(location, { kind: "tab", tab: "weekly" })).toBe(true);
    expect(
      isCurrentSchedulingLocation(location, { kind: "overrideScope", scope: "employee-1" }),
    ).toBe(false);
  });

  it("accepts only supported scheduling tab values", () => {
    expect(isSchedulingTab("availability")).toBe(true);
    expect(isSchedulingTab("customers")).toBe(false);
  });
});
