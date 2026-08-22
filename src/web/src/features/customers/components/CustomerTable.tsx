import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Table } from "@lily_platform/lily_ui/ui/atoms/Table";
import type { TableColumn, TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { useMemo } from "react";

import type { Customer } from "../api/customerContract";
import type { useCustomerList } from "../hooks/useCustomerList";

interface CustomerRow extends TableRowData {
  readonly resource: Customer;
  readonly name: string;
  readonly contact: string;
  readonly status: string;
  readonly updatedAtUtc: string;
}

interface CustomerTableProps {
  readonly id: string;
  readonly list: ReturnType<typeof useCustomerList>;
  readonly onSelect: (customer: Customer) => void;
  readonly t: (key: string) => string;
}

export function CustomerTable({ id, list, onSelect, t }: CustomerTableProps) {
  const rows = useMemo<CustomerRow[]>(
    () =>
      list.result.items.map((customer) => ({
        id: customer.id,
        resource: customer,
        name: customer.name,
        contact: customer.email ?? customer.phone ?? "—",
        status: customer.archivedAtUtc ? t("app:common.archived") : t("app:common.active"),
        updatedAtUtc: customer.updatedAtUtc,
      })),
    [list.result.items, t],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      { id: "name", label: t("app:customers.name"), priority: "primary" },
      { id: "contact", label: t("app:customers.contact"), priority: "secondary" },
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
      {
        id: "updatedAtUtc",
        label: t("app:common.updated"),
        priority: "tertiary",
        format: (value) => formatDate(String(value)),
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
      emptyContent={t("app:customers.empty")}
      pagination
      page={list.page}
      rowsPerPage={list.pageSize}
      totalCount={list.result.totalCount}
      rowsPerPageOptions={[10, 20, 50, 100]}
      onPageChange={list.setPage}
      onRowsPerPageChange={list.setPageSize}
      getRowAriaLabel={(row) => String(row.name)}
      onRowActivate={(row) => onSelect((row as CustomerRow).resource)}
    />
  );
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(new Date(value));
}
