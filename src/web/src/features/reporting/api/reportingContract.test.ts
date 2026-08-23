import { describe, expect, it } from "vitest";

import { decodeReportingDashboard } from "./reportingContract";

const headline = {
  totalAppointments: 3,
  scheduledAppointments: 1,
  confirmedAppointments: 0,
  completedAppointments: 1,
  cancelledAppointments: 1,
  noShowAppointments: 0,
  completedRevenue: 750,
};

describe("reporting contract", () => {
  it("decodes the tenant-local dashboard projection", () => {
    const dashboard = decodeReportingDashboard({
      fromDate: "2026-08-22",
      toDate: "2026-08-23",
      today: "2026-08-23",
      timeZone: "Europe/Istanbul",
      currency: "TRY",
      range: headline,
      todaySummary: { ...headline, totalAppointments: 1 },
      byStatus: [{ status: "completed", count: 1, completedRevenue: 750 }],
      byEmployee: [
        {
          employeeId: "employee-a",
          employeeName: "Demo Employee",
          totalAppointments: 3,
          completedAppointments: 1,
          noShowAppointments: 0,
          completedRevenue: 750,
        },
      ],
      byDay: [
        {
          date: "2026-08-22",
          totalAppointments: 2,
          completedAppointments: 1,
          completedRevenue: 750,
        },
      ],
    });

    expect(dashboard.range.completedRevenue).toBe(750);
    expect(dashboard.byStatus[0]?.status).toBe("completed");
  });

  it("rejects an unknown appointment status", () => {
    expect(() =>
      decodeReportingDashboard({
        fromDate: "2026-08-22",
        toDate: "2026-08-23",
        today: "2026-08-23",
        timeZone: "Europe/Istanbul",
        currency: "TRY",
        range: headline,
        todaySummary: headline,
        byStatus: [{ status: "draft", count: 1, completedRevenue: 0 }],
        byEmployee: [],
        byDay: [],
      }),
    ).toThrow("statusBreakdown.status is not recognized");
  });
});
