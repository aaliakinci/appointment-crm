import {
  nullableString,
  requireArray,
  requireBoolean,
  requireNumber,
  requireRecord,
  requireString,
} from "@/shared/api/contractDecoder";
import { decodePage, type PagedResponse } from "@/shared/api/paginationContract";

export interface SchedulePeriod {
  readonly dayOfWeek: number;
  readonly startMinute: number;
  readonly endMinute: number;
}

export interface WeeklySchedule {
  readonly employeeId: string | null;
  readonly state: "unconfigured" | "inherited" | "closed" | "custom";
  readonly source: "none" | "tenant" | "employee";
  readonly revision: number;
  readonly versionId: string | null;
  readonly versionNumber: number | null;
  readonly effectiveVersionId: string | null;
  readonly effectiveVersionNumber: number | null;
  readonly periods: readonly SchedulePeriod[];
  readonly publishedAtUtc: string | null;
  readonly publishedBy: string | null;
  readonly changeNote: string | null;
}

export interface WeeklyScheduleVersion {
  readonly id: string;
  readonly versionNumber: number;
  readonly mode: "custom" | "closed" | "inherited";
  readonly periods: readonly SchedulePeriod[];
  readonly createdAtUtc: string;
  readonly publishedBy: string | null;
  readonly changeNote: string | null;
  readonly restoredFromVersionId: string | null;
  readonly restoredFromVersionNumber: number | null;
}

export interface DateOverride {
  readonly id: string;
  readonly employeeId: string | null;
  readonly date: string;
  readonly isClosed: boolean;
  readonly periods: readonly SchedulePeriod[];
  readonly updatedAtUtc: string;
}

export interface TimeOff {
  readonly id: string;
  readonly employeeId: string;
  readonly employeeName: string;
  readonly startUtc: string;
  readonly endUtc: string;
  readonly localStartDate: string;
  readonly localStartTime: string;
  readonly localEndDate: string;
  readonly localEndTime: string;
  readonly timeZone: string;
  readonly reason: string | null;
}

export interface AvailabilitySlot {
  readonly startUtc: string;
  readonly endUtc: string;
  readonly localStart: string;
  readonly localEnd: string;
}

export interface Availability {
  readonly date: string;
  readonly employeeId: string;
  readonly serviceId: string;
  readonly serviceDurationMinutes: number;
  readonly timeZone: string;
  readonly slots: readonly AvailabilitySlot[];
}

export interface WeeklyScheduleInput {
  readonly expectedRevision: number;
  readonly periods: readonly SchedulePeriod[];
  readonly changeNote: string | null;
}

export interface RestoreWeeklyScheduleVersionInput {
  readonly expectedRevision: number;
  readonly changeNote: string | null;
}

export interface DateOverrideInput {
  readonly isClosed: boolean;
  readonly periods: readonly Pick<SchedulePeriod, "startMinute" | "endMinute">[];
}

export interface CreateTimeOffInput {
  readonly employeeId: string;
  readonly startDate: string;
  readonly startTime: string;
  readonly endDate: string;
  readonly endTime: string;
  readonly timeZone: string;
  readonly reason: string | null;
}

export function decodeWeeklySchedule(body: unknown): WeeklySchedule {
  const value = requireRecord(body, "weekly schedule");
  const state = requireString(value.state, "weeklySchedule.state");
  if (
    state !== "unconfigured" &&
    state !== "inherited" &&
    state !== "closed" &&
    state !== "custom"
  ) {
    throw new TypeError("weeklySchedule.state is not valid.");
  }
  const source = requireString(value.source, "weeklySchedule.source");
  if (source !== "none" && source !== "tenant" && source !== "employee") {
    throw new TypeError("weeklySchedule.source is not valid.");
  }

  return {
    employeeId: nullableString(value.employeeId, "weeklySchedule.employeeId"),
    state,
    source,
    revision: requireNumber(value.revision, "weeklySchedule.revision"),
    versionId: nullableString(value.versionId, "weeklySchedule.versionId"),
    versionNumber: nullableNumber(value.versionNumber, "weeklySchedule.versionNumber"),
    effectiveVersionId: nullableString(
      value.effectiveVersionId,
      "weeklySchedule.effectiveVersionId",
    ),
    effectiveVersionNumber: nullableNumber(
      value.effectiveVersionNumber,
      "weeklySchedule.effectiveVersionNumber",
    ),
    periods: requireArray(value.periods, "weeklySchedule.periods").map(decodePeriod),
    publishedAtUtc: nullableString(value.publishedAtUtc, "weeklySchedule.publishedAtUtc"),
    publishedBy: nullableString(value.publishedBy, "weeklySchedule.publishedBy"),
    changeNote: nullableString(value.changeNote, "weeklySchedule.changeNote"),
  };
}

export const decodeWeeklyScheduleVersionPage = (
  body: unknown,
): PagedResponse<WeeklyScheduleVersion> => decodePage(body, decodeWeeklyScheduleVersion);

export function decodeWeeklyScheduleVersion(body: unknown): WeeklyScheduleVersion {
  const value = requireRecord(body, "weekly schedule version");
  const mode = requireString(value.mode, "weeklyScheduleVersion.mode");
  if (mode !== "custom" && mode !== "closed" && mode !== "inherited") {
    throw new TypeError("weeklyScheduleVersion.mode is not valid.");
  }

  return {
    id: requireString(value.id, "weeklyScheduleVersion.id"),
    versionNumber: requireNumber(value.versionNumber, "weeklyScheduleVersion.versionNumber"),
    mode,
    periods: requireArray(value.periods, "weeklyScheduleVersion.periods").map(decodePeriod),
    createdAtUtc: requireString(value.createdAtUtc, "weeklyScheduleVersion.createdAtUtc"),
    publishedBy: nullableString(value.publishedBy, "weeklyScheduleVersion.publishedBy"),
    changeNote: nullableString(value.changeNote, "weeklyScheduleVersion.changeNote"),
    restoredFromVersionId: nullableString(
      value.restoredFromVersionId,
      "weeklyScheduleVersion.restoredFromVersionId",
    ),
    restoredFromVersionNumber: nullableNumber(
      value.restoredFromVersionNumber,
      "weeklyScheduleVersion.restoredFromVersionNumber",
    ),
  };
}

export function decodeDateOverrides(body: unknown): readonly DateOverride[] {
  return requireArray(body, "date overrides").map((item) => {
    const value = requireRecord(item, "date override");
    return {
      id: requireString(value.id, "dateOverride.id"),
      employeeId: nullableString(value.employeeId, "dateOverride.employeeId"),
      date: requireString(value.date, "dateOverride.date"),
      isClosed: requireBoolean(value.isClosed, "dateOverride.isClosed"),
      periods: requireArray(value.periods, "dateOverride.periods").map(decodePeriod),
      updatedAtUtc: requireString(value.updatedAtUtc, "dateOverride.updatedAtUtc"),
    };
  });
}

export function decodeDateOverride(body: unknown): DateOverride {
  return decodeDateOverrides([body])[0]!;
}

export function decodeTimeOffs(body: unknown): readonly TimeOff[] {
  return requireArray(body, "time off entries").map((item) => {
    const value = requireRecord(item, "time off");
    return {
      id: requireString(value.id, "timeOff.id"),
      employeeId: requireString(value.employeeId, "timeOff.employeeId"),
      employeeName: requireString(value.employeeName, "timeOff.employeeName"),
      startUtc: requireString(value.startUtc, "timeOff.startUtc"),
      endUtc: requireString(value.endUtc, "timeOff.endUtc"),
      localStartDate: requireString(value.localStartDate, "timeOff.localStartDate"),
      localStartTime: requireString(value.localStartTime, "timeOff.localStartTime"),
      localEndDate: requireString(value.localEndDate, "timeOff.localEndDate"),
      localEndTime: requireString(value.localEndTime, "timeOff.localEndTime"),
      timeZone: requireString(value.timeZone, "timeOff.timeZone"),
      reason: nullableString(value.reason, "timeOff.reason"),
    };
  });
}

export function decodeTimeOff(body: unknown): TimeOff {
  return decodeTimeOffs([body])[0]!;
}

export function decodeAvailability(body: unknown): Availability {
  const value = requireRecord(body, "availability");
  return {
    date: requireString(value.date, "availability.date"),
    employeeId: requireString(value.employeeId, "availability.employeeId"),
    serviceId: requireString(value.serviceId, "availability.serviceId"),
    serviceDurationMinutes: requireNumber(
      value.serviceDurationMinutes,
      "availability.serviceDurationMinutes",
    ),
    timeZone: requireString(value.timeZone, "availability.timeZone"),
    slots: requireArray(value.slots, "availability.slots").map((item) => {
      const slot = requireRecord(item, "availability slot");
      return {
        startUtc: requireString(slot.startUtc, "availabilitySlot.startUtc"),
        endUtc: requireString(slot.endUtc, "availabilitySlot.endUtc"),
        localStart: requireString(slot.localStart, "availabilitySlot.localStart"),
        localEnd: requireString(slot.localEnd, "availabilitySlot.localEnd"),
      };
    }),
  };
}

function decodePeriod(body: unknown): SchedulePeriod {
  const value = requireRecord(body, "schedule period");
  return {
    dayOfWeek: requireNumber(value.dayOfWeek, "schedulePeriod.dayOfWeek"),
    startMinute: requireNumber(value.startMinute, "schedulePeriod.startMinute"),
    endMinute: requireNumber(value.endMinute, "schedulePeriod.endMinute"),
  };
}

function nullableNumber(value: unknown, name: string): number | null {
  return value === null ? null : requireNumber(value, name);
}
