import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Table } from "@lily_platform/lily_ui/ui/atoms/Table";
import type { TableColumn, TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { useMemo } from "react";

import type { ServiceOffering } from "../api/serviceContract";
import type { useServiceList } from "../hooks/useServiceList";

interface ServiceRow extends TableRowData {
  readonly resource: ServiceOffering;
  readonly name: string;
  readonly duration: string;
  readonly price: string;
  readonly status: string;
}

interface ServiceTableProps {
  readonly id: string;
  readonly list: ReturnType<typeof useServiceList>;
  readonly onSelect: (service: ServiceOffering) => void;
  readonly t: (key: string) => string;
}

export function ServiceTable({ id, list, onSelect, t }: ServiceTableProps) {
  const rows = useMemo<ServiceRow[]>(
    () =>
      list.result.items.map((service) => ({
        id: service.id,
        resource: service,
        name: service.name,
        duration: `${service.durationMinutes} ${t("app:services.minutes")}`,
        price: formatMoney(service.price, service.currency),
        status: service.isActive ? t("app:common.active") : t("app:common.inactive"),
      })),
    [list.result.items, t],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      { id: "name", label: t("app:services.name"), priority: "primary" },
      { id: "duration", label: t("app:services.duration"), priority: "secondary" },
      { id: "price", label: t("app:services.price"), align: "right" },
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
      emptyContent={t("app:services.empty")}
      pagination
      page={list.page}
      rowsPerPage={list.pageSize}
      totalCount={list.result.totalCount}
      rowsPerPageOptions={[10, 20, 50, 100]}
      onPageChange={list.setPage}
      onRowsPerPageChange={list.setPageSize}
      getRowAriaLabel={(row) => String(row.name)}
      onRowActivate={(row) => onSelect((row as ServiceRow).resource)}
    />
  );
}

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(value);
}
