import { LilyApiError } from "@lily_platform/lily_ui/errors";

type Translate = (key: string) => string;

const errorKeys: Readonly<Record<string, string>> = {
  "appointments.not_found": "app:appointments.errors.notFound",
  "appointments.customer_not_found": "app:appointments.errors.customerNotFound",
  "appointments.customer_archived": "app:appointments.errors.customerArchived",
  "appointments.employee_not_found": "app:appointments.errors.employeeNotFound",
  "appointments.employee_inactive": "app:appointments.errors.employeeInactive",
  "appointments.service_not_found": "app:appointments.errors.serviceNotFound",
  "appointments.service_inactive": "app:appointments.errors.serviceInactive",
  "appointments.service_not_assigned": "app:appointments.errors.serviceNotAssigned",
  "appointments.slot_unavailable": "app:appointments.errors.slotUnavailable",
  "appointments.time_conflict": "app:appointments.errors.timeConflict",
  "appointments.invalid_transition": "app:appointments.errors.invalidTransition",
  "appointments.version_conflict": "app:appointments.errors.versionConflict",
  "appointments.invalid_date_range": "app:appointments.errors.invalidDateRange",
};

export function appointmentErrorMessage(error: unknown, t: Translate, fallbackKey: string): string {
  if (!(error instanceof LilyApiError)) {
    return t(fallbackKey);
  }

  const message = t(errorKeys[error.code] ?? fallbackKey);
  return error.traceId ? `${message} ${t("app:appointments.traceId")}: ${error.traceId}` : message;
}
