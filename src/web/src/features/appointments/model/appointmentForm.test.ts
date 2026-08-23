import { describe, expect, it } from "vitest";

import { createAppointmentFormDefinition, createRescheduleFormDefinition } from "./appointmentForm";

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function getFieldKind(definition: unknown, name: string): unknown {
  if (!isRecord(definition)) return undefined;

  const fields = definition["fields"];
  if (!Array.isArray(fields)) return undefined;

  const field = (fields as unknown[]).find(
    (candidate) => isRecord(candidate) && candidate["name"] === name,
  );

  return isRecord(field) ? field["kind"] : undefined;
}

describe("appointment form date fields", () => {
  it("uses Lily UI date fields for create and reschedule", () => {
    const t = (key: string) => key;
    const create = createAppointmentFormDefinition(t, "2026-08-23");
    const reschedule = createRescheduleFormDefinition(t, "2026-08-24");

    expect(getFieldKind(create, "date")).toBe("date");
    expect(getFieldKind(reschedule, "date")).toBe("date");
  });
});
