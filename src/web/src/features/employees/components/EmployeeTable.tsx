import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Table } from "@lily_platform/lily_ui/ui/atoms/Table";
import type { TableColumn, TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { useMemo } from "react";

import type { Employee } from "../api/employeeContract";
import type { useEmployeeList } from "../hooks/useEmployeeList";

interface EmployeeRow extends TableRowData {
  readonly resource: Employee;
  readonly name: string;
  readonly services: string;
  readonly contact: string;
  readonly status: string;
}

interface EmployeeTableProps {
  readonly id: string;
  readonly list: ReturnType<typeof useEmployeeList>;
  readonly onSelect: (employee: Employee) => void;
  readonly t: (key: string) => string;
}

export function EmployeeTable({ id, list, onSelect, t }: EmployeeTableProps) {
  const rows = useMemo<EmployeeRow[]>(
    () =>
      list.result.items.map((employee) => ({
        id: employee.id,
        resource: employee,
        name: employee.name,
        services: employee.services.map((service) => service.name).join(", ") || "—",
        contact: employee.email ?? employee.phone ?? "—",
        status: employee.isActive ? t("app:common.active") : t("app:common.inactive"),
      })),
    [list.result.items, t],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      { id: "name", label: t("app:employees.name"), priority: "primary" },
      { id: "services", label: t("app:employees.services"), priority: "secondary" },
      { id: "contact", label: t("app:employees.contact"), priority: "tertiary" },
      {
        id: "status",
        label: t("app:common.status"),
        format: (value) => (
          <Chip
            id={`${id}.status.${String(value)}`}
            size="small"
            color={value === t("app:common.active") ? "success" : "default"}
            label={String(value)}
          />
        ),
      },
    ],
    [id, t],
  );

  return (
    <Table
      id={id}
      columns={columns}
      rows={rows}
      loading={list.loading}
      emptyContent={t("app:employees.empty")}
      pagination
      page={list.page}
      rowsPerPage={list.pageSize}
      totalCount={list.result.totalCount}
      rowsPerPageOptions={[10, 20, 50, 100]}
      onPageChange={list.setPage}
      onRowsPerPageChange={list.setPageSize}
      getRowAriaLabel={(row) => String(row.name)}
      onRowActivate={(row) => onSelect((row as EmployeeRow).resource)}
    />
  );
}
