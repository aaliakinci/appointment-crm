import { LilyApiError } from "@lily_platform/lily_ui/errors";

type Translate = (key: string) => string;

const errorMessageKeys: Readonly<Record<string, string>> = {
  "scheduling.employee_not_found": "app:scheduling.errors.employeeNotFound",
  "availability.service_not_found": "app:scheduling.errors.serviceNotFound",
  "availability.employee_inactive": "app:scheduling.errors.employeeInactive",
  "availability.service_inactive": "app:scheduling.errors.serviceInactive",
  "availability.service_not_assigned": "app:scheduling.errors.serviceNotAssigned",
  "scheduling.invalid_schedule": "app:scheduling.errors.invalidSchedule",
  "scheduling.schedule_conflict": "app:scheduling.errors.scheduleConflict",
  "scheduling.schedule_version_conflict": "app:scheduling.errors.scheduleVersionConflict",
  "scheduling.schedule_version_not_found": "app:scheduling.errors.scheduleVersionNotFound",
  "scheduling.invalid_date_range": "app:scheduling.errors.invalidDateRange",
  "scheduling.time_off_overlap": "app:scheduling.errors.timeOffOverlap",
  "scheduling.time_off_not_found": "app:scheduling.errors.timeOffNotFound",
  "scheduling.time_zone_mismatch": "app:scheduling.errors.timeZoneMismatch",
  "scheduling.invalid_local_time": "app:scheduling.errors.invalidLocalTime",
  "scheduling.ambiguous_local_time": "app:scheduling.errors.ambiguousLocalTime",
  "common.unexpected_error": "app:scheduling.errors.unexpected",
};

export function schedulingErrorMessage(error: unknown, t: Translate, fallbackKey: string): string {
  if (!(error instanceof LilyApiError)) {
    return t(fallbackKey);
  }

  const messageKey = errorMessageKeys[error.code];
  const message = t(messageKey ?? fallbackKey);
  if (error.code === "common.unexpected_error" && error.traceId) {
    return `${message} ${t("app:scheduling.errors.traceId")}: ${error.traceId}`;
  }

  return message;
}

export function isScheduleVersionConflict(error: unknown): boolean {
  return error instanceof LilyApiError && error.code === "scheduling.schedule_version_conflict";
}
