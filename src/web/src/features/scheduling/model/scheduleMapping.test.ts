import { describe, expect, it } from "vitest";

import { createDateOverrideDefinition } from "./dateOverrideForm";
import { createAvailabilityDefinition } from "./availabilityForm";
import { addLocalDays } from "./localDate";
import { fromMinute, toMinute } from "./schedulePeriod";
import { createTimeOffDefinition } from "./timeOffForm";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function getFieldKind(definition: unknown, name: string): unknown {
  if (!isRecord(definition)) return undefined;

  const fields = definition["fields"];
  if (Array.isArray(fields)) {
    const field = (fields as unknown[]).find(
      (candidate) => isRecord(candidate) && candidate["name"] === name,
    );
    return isRecord(field) ? field["kind"] : undefined;
  }

  if (!isRecord(fields)) return undefined;
  const field = fields[name];
  return isRecord(field) ? field["kind"] : undefined;
}

describe("scheduling form mappings", () => {
  it("maps local clock values without creating browser-zone instants", () => {
    expect(toMinute("09:35")).toBe(575);
    expect(fromMinute(575)).toBe("09:35");
    expect(fromMinute(1440)).toBe("24:00");
  });

  it("adds calendar days using date-only strings", () => {
    expect(addLocalDays("2026-03-28", 1)).toBe("2026-03-29");
    expect(addLocalDays("2026-10-25", 1)).toBe("2026-10-26");
  });

  it("does not render working periods for a closed special date", () => {
    const closed = createDateOverrideDefinition((key) => key, "2026-08-22", true);
    const open = createDateOverrideDefinition((key) => key, "2026-08-22", false);
    const closedContent = "content" in closed ? (closed.content ?? []) : [];
    const openContent = "content" in open ? (open.content ?? []) : [];

    expect(closedContent.some((node) => node.kind === "array")).toBe(false);
    expect(openContent.some((node) => node.kind === "array")).toBe(true);
  });

  it("uses Lily UI date fields for every scheduling date input", () => {
    const t = (key: string) => key;
    const dateOverride = createDateOverrideDefinition(t, "2026-08-23", true);
    const availability = createAvailabilityDefinition(t, "2026-08-23");
    const timeOff = createTimeOffDefinition(t, "2026-08-23");
    expect(getFieldKind(dateOverride, "date")).toBe("date");
    expect(getFieldKind(availability, "date")).toBe("date");
    expect(getFieldKind(timeOff, "startDate")).toBe("date");
    expect(getFieldKind(timeOff, "endDate")).toBe("date");
  });
});
