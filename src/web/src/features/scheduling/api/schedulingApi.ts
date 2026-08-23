import { appHttpClient, toQueryParams, type PagedResponse } from "@/shared/api";

import {
  decodeAvailability,
  decodeDateOverride,
  decodeDateOverrides,
  decodeTimeOff,
  decodeTimeOffs,
  decodeWeeklySchedule,
  decodeWeeklyScheduleVersion,
  decodeWeeklyScheduleVersionPage,
  type Availability,
  type CreateTimeOffInput,
  type DateOverride,
  type DateOverrideInput,
  type TimeOff,
  type RestoreWeeklyScheduleVersionInput,
  type WeeklySchedule,
  type WeeklyScheduleInput,
  type WeeklyScheduleVersion,
} from "./schedulingContract";

export function getWeeklySchedule(
  employeeId?: string,
  signal?: AbortSignal,
): Promise<WeeklySchedule> {
  const path = employeeId
    ? `/api/v1/scheduling/working-hours/employees/${encodeURIComponent(employeeId)}`
    : "/api/v1/scheduling/working-hours/tenant";
  return appHttpClient.getData<WeeklySchedule>(path, {
    signal,
    decode: decodeWeeklySchedule,
    metadata: { operationName: "scheduling.weekly.get" },
  });
}

export function putWeeklySchedule(
  input: WeeklyScheduleInput,
  employeeId?: string,
): Promise<WeeklySchedule> {
  const path = employeeId
    ? `/api/v1/scheduling/working-hours/employees/${encodeURIComponent(employeeId)}`
    : "/api/v1/scheduling/working-hours/tenant";
  return appHttpClient.putData<WeeklySchedule, WeeklyScheduleInput>(path, input, {
    decode: decodeWeeklySchedule,
    metadata: { operationName: "scheduling.weekly.put", replay: "deny" },
  });
}

export function restoreWeeklyInheritance(
  employeeId: string,
  expectedRevision: number,
  changeNote?: string,
): Promise<void> {
  return appHttpClient.deleteData<void>(
    `/api/v1/scheduling/working-hours/employees/${encodeURIComponent(employeeId)}`,
    {
      params: toQueryParams({ expectedRevision, changeNote }),
      metadata: { operationName: "scheduling.weekly.restore", replay: "deny" },
    },
  );
}

export function listWeeklyScheduleVersions(
  employeeId?: string,
  page = 1,
  signal?: AbortSignal,
): Promise<PagedResponse<WeeklyScheduleVersion>> {
  const scope = employeeId ? `employees/${encodeURIComponent(employeeId)}` : "tenant";
  return appHttpClient.getData<PagedResponse<WeeklyScheduleVersion>>(
    `/api/v1/scheduling/working-hours/${scope}/versions`,
    {
      signal,
      params: toQueryParams({ page, pageSize: 10, sortBy: "version", sortDirection: "desc" }),
      decode: decodeWeeklyScheduleVersionPage,
      metadata: { operationName: "scheduling.weekly.versions.list" },
    },
  );
}

export function getWeeklyScheduleVersion(
  versionId: string,
  employeeId?: string,
  signal?: AbortSignal,
): Promise<WeeklyScheduleVersion> {
  const scope = employeeId ? `employees/${encodeURIComponent(employeeId)}` : "tenant";
  return appHttpClient.getData<WeeklyScheduleVersion>(
    `/api/v1/scheduling/working-hours/${scope}/versions/${encodeURIComponent(versionId)}`,
    {
      signal,
      decode: decodeWeeklyScheduleVersion,
      metadata: { operationName: "scheduling.weekly.versions.get" },
    },
  );
}

export function restoreWeeklyScheduleVersion(
  versionId: string,
  input: RestoreWeeklyScheduleVersionInput,
  employeeId?: string,
): Promise<WeeklySchedule> {
  const scope = employeeId ? `employees/${encodeURIComponent(employeeId)}` : "tenant";
  return appHttpClient.postData<WeeklySchedule, RestoreWeeklyScheduleVersionInput>(
    `/api/v1/scheduling/working-hours/${scope}/versions/${encodeURIComponent(versionId)}/restore`,
    input,
    {
      decode: decodeWeeklySchedule,
      metadata: { operationName: "scheduling.weekly.versions.restore", replay: "deny" },
    },
  );
}

export function listDateOverrides(
  fromDate: string,
  toDate: string,
  employeeId?: string,
  signal?: AbortSignal,
): Promise<readonly DateOverride[]> {
  return appHttpClient.getData<readonly DateOverride[]>("/api/v1/scheduling/date-overrides", {
    signal,
    params: toQueryParams({ fromDate, toDate, employeeId }),
    decode: decodeDateOverrides,
    metadata: { operationName: "scheduling.overrides.list" },
  });
}

export function putDateOverride(
  date: string,
  input: DateOverrideInput,
  employeeId?: string,
): Promise<DateOverride> {
  const scope = employeeId ? `employees/${encodeURIComponent(employeeId)}` : "tenant";
  return appHttpClient.putData<DateOverride, DateOverrideInput>(
    `/api/v1/scheduling/date-overrides/${scope}/${encodeURIComponent(date)}`,
    input,
    {
      decode: decodeDateOverride,
      metadata: { operationName: "scheduling.overrides.put", replay: "deny" },
    },
  );
}

export function deleteDateOverride(date: string, employeeId?: string): Promise<void> {
  const scope = employeeId ? `employees/${encodeURIComponent(employeeId)}` : "tenant";
  return appHttpClient.deleteData<void>(
    `/api/v1/scheduling/date-overrides/${scope}/${encodeURIComponent(date)}`,
    { metadata: { operationName: "scheduling.overrides.delete", replay: "deny" } },
  );
}

export function listTimeOff(
  fromDate: string,
  toDate: string,
  employeeId?: string,
  signal?: AbortSignal,
): Promise<readonly TimeOff[]> {
  return appHttpClient.getData<readonly TimeOff[]>("/api/v1/scheduling/time-off", {
    signal,
    params: toQueryParams({ fromDate, toDate, employeeId }),
    decode: decodeTimeOffs,
    metadata: { operationName: "scheduling.time-off.list" },
  });
}

export function createTimeOff(input: CreateTimeOffInput): Promise<TimeOff> {
  return appHttpClient.postData<TimeOff, CreateTimeOffInput>("/api/v1/scheduling/time-off", input, {
    decode: decodeTimeOff,
    metadata: { operationName: "scheduling.time-off.create", replay: "deny" },
  });
}

export function deleteTimeOff(timeOffId: string): Promise<void> {
  return appHttpClient.deleteData<void>(
    `/api/v1/scheduling/time-off/${encodeURIComponent(timeOffId)}`,
    { metadata: { operationName: "scheduling.time-off.delete", replay: "deny" } },
  );
}

export function getAvailability(
  date: string,
  employeeId: string,
  serviceId: string,
  excludeAppointmentId?: string,
  signal?: AbortSignal,
): Promise<Availability> {
  return appHttpClient.getData<Availability>("/api/v1/availability", {
    signal,
    params: toQueryParams({ date, employeeId, serviceId, excludeAppointmentId }),
    decode: decodeAvailability,
    metadata: { operationName: "availability.get" },
  });
}
