import {
  requireArray,
  requireNumber,
  requireRecord,
  requireString,
} from "@/shared/api/contractDecoder";

export const reportingStatuses = [
  "scheduled",
  "confirmed",
  "completed",
  "cancelled",
  "no-show",
] as const;
export type ReportingStatus = (typeof reportingStatuses)[number];

export interface ReportingHeadline {
  readonly totalAppointments: number;
  readonly scheduledAppointments: number;
  readonly confirmedAppointments: number;
  readonly completedAppointments: number;
  readonly cancelledAppointments: number;
  readonly noShowAppointments: number;
  readonly completedRevenue: number;
}

export interface ReportingStatusBreakdown {
  readonly status: ReportingStatus;
  readonly count: number;
  readonly completedRevenue: number;
}

export interface ReportingEmployeeBreakdown {
  readonly employeeId: string;
  readonly employeeName: string;
  readonly totalAppointments: number;
  readonly completedAppointments: number;
  readonly noShowAppointments: number;
  readonly completedRevenue: number;
}

export interface ReportingDailyBreakdown {
  readonly date: string;
  readonly totalAppointments: number;
  readonly completedAppointments: number;
  readonly completedRevenue: number;
}

export interface ReportingDashboard {
  readonly fromDate: string;
  readonly toDate: string;
  readonly today: string;
  readonly timeZone: string;
  readonly currency: string;
  readonly range: ReportingHeadline;
  readonly todaySummary: ReportingHeadline;
  readonly byStatus: readonly ReportingStatusBreakdown[];
  readonly byEmployee: readonly ReportingEmployeeBreakdown[];
  readonly byDay: readonly ReportingDailyBreakdown[];
}

export interface ReportingQuery {
  readonly fromDate: string;
  readonly toDate: string;
  readonly employeeId?: string;
  readonly status?: ReportingStatus;
}

export function decodeReportingDashboard(body: unknown): ReportingDashboard {
  const value = requireRecord(body, "reporting dashboard");
  return {
    fromDate: requireString(value.fromDate, "reporting.fromDate"),
    toDate: requireString(value.toDate, "reporting.toDate"),
    today: requireString(value.today, "reporting.today"),
    timeZone: requireString(value.timeZone, "reporting.timeZone"),
    currency: requireString(value.currency, "reporting.currency"),
    range: decodeHeadline(value.range, "reporting.range"),
    todaySummary: decodeHeadline(value.todaySummary, "reporting.todaySummary"),
    byStatus: requireArray(value.byStatus, "reporting.byStatus").map(decodeStatus),
    byEmployee: requireArray(value.byEmployee, "reporting.byEmployee").map(decodeEmployee),
    byDay: requireArray(value.byDay, "reporting.byDay").map(decodeDay),
  };
}

function decodeHeadline(body: unknown, name: string): ReportingHeadline {
  const value = requireRecord(body, name);
  return {
    totalAppointments: requireNumber(value.totalAppointments, `${name}.totalAppointments`),
    scheduledAppointments: requireNumber(
      value.scheduledAppointments,
      `${name}.scheduledAppointments`,
    ),
    confirmedAppointments: requireNumber(
      value.confirmedAppointments,
      `${name}.confirmedAppointments`,
    ),
    completedAppointments: requireNumber(
      value.completedAppointments,
      `${name}.completedAppointments`,
    ),
    cancelledAppointments: requireNumber(
      value.cancelledAppointments,
      `${name}.cancelledAppointments`,
    ),
    noShowAppointments: requireNumber(value.noShowAppointments, `${name}.noShowAppointments`),
    completedRevenue: requireNumber(value.completedRevenue, `${name}.completedRevenue`),
  };
}

function decodeStatus(body: unknown): ReportingStatusBreakdown {
  const value = requireRecord(body, "status breakdown");
  const status = requireString(value.status, "statusBreakdown.status");
  if (!reportingStatuses.includes(status as ReportingStatus)) {
    throw new TypeError("statusBreakdown.status is not recognized.");
  }
  return {
    status: status as ReportingStatus,
    count: requireNumber(value.count, "statusBreakdown.count"),
    completedRevenue: requireNumber(value.completedRevenue, "statusBreakdown.completedRevenue"),
  };
}

function decodeEmployee(body: unknown): ReportingEmployeeBreakdown {
  const value = requireRecord(body, "employee breakdown");
  return {
    employeeId: requireString(value.employeeId, "employeeBreakdown.employeeId"),
    employeeName: requireString(value.employeeName, "employeeBreakdown.employeeName"),
    totalAppointments: requireNumber(
      value.totalAppointments,
      "employeeBreakdown.totalAppointments",
    ),
    completedAppointments: requireNumber(
      value.completedAppointments,
      "employeeBreakdown.completedAppointments",
    ),
    noShowAppointments: requireNumber(
      value.noShowAppointments,
      "employeeBreakdown.noShowAppointments",
    ),
    completedRevenue: requireNumber(value.completedRevenue, "employeeBreakdown.completedRevenue"),
  };
}

function decodeDay(body: unknown): ReportingDailyBreakdown {
  const value = requireRecord(body, "daily breakdown");
  return {
    date: requireString(value.date, "dailyBreakdown.date"),
    totalAppointments: requireNumber(value.totalAppointments, "dailyBreakdown.totalAppointments"),
    completedAppointments: requireNumber(
      value.completedAppointments,
      "dailyBreakdown.completedAppointments",
    ),
    completedRevenue: requireNumber(value.completedRevenue, "dailyBreakdown.completedRevenue"),
  };
}
