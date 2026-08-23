import { Table, type TableColumn, type TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { useMemo } from "react";

import type { ReportingEmployeeBreakdown } from "../api/reportingContract";

interface EmployeeRow extends TableRowData {
  readonly employeeName: string;
  readonly totalAppointments: number;
  readonly completedAppointments: number;
  readonly noShowAppointments: number;
  readonly completedRevenue: string;
}

interface ReportingEmployeeTableProps {
  readonly currency: string;
  readonly id: string;
  readonly items: readonly ReportingEmployeeBreakdown[];
  readonly t: (key: string) => string;
}

export function ReportingEmployeeTable({ currency, id, items, t }: ReportingEmployeeTableProps) {
  const rows = useMemo<EmployeeRow[]>(
    () =>
      items.map((item) => ({
        id: item.employeeId,
        employeeName: item.employeeName,
        totalAppointments: item.totalAppointments,
        completedAppointments: item.completedAppointments,
        noShowAppointments: item.noShowAppointments,
        completedRevenue: new Intl.NumberFormat(undefined, {
          style: "currency",
          currency,
        }).format(item.completedRevenue),
      })),
    [currency, items],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      { id: "employeeName", label: t("app:appointments.employee"), priority: "primary" },
      { id: "totalAppointments", label: t("app:reporting.total") },
      { id: "completedAppointments", label: t("app:reporting.completed") },
      { id: "noShowAppointments", label: t("app:reporting.noShow") },
      { id: "completedRevenue", label: t("app:reporting.revenue"), priority: "secondary" },
    ],
    [t],
  );
  return (
    <Table
      id={id}
      columns={columns}
      rows={rows}
      emptyContent={t("app:reporting.emptyEmployees")}
      getRowAriaLabel={(row) => String(row.employeeName)}
    />
  );
}
