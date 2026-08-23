import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Table, type TableColumn, type TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { useMemo } from "react";

import type { AuditEntry } from "../api/auditContract";
import type { useAuditList } from "../hooks/useAuditList";

interface AuditRow extends TableRowData {
  readonly resource: AuditEntry;
  readonly occurredAtUtc: string;
  readonly actorName: string;
  readonly action: string;
  readonly target: string;
  readonly summary: string;
}

interface AuditTableProps {
  readonly id: string;
  readonly list: ReturnType<typeof useAuditList>;
  readonly t: (key: string) => string;
}

export function AuditTable({ id, list, t }: AuditTableProps) {
  const rows = useMemo<AuditRow[]>(
    () =>
      list.result.items.map((entry) => ({
        id: entry.id,
        resource: entry,
        occurredAtUtc: entry.occurredAtUtc,
        actorName: entry.actorName,
        action: entry.action,
        target: `${entry.targetType} · ${entry.targetId.slice(0, 8)}`,
        summary: entry.summary ?? "—",
      })),
    [list.result.items],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      {
        id: "occurredAtUtc",
        label: t("app:audit.occurredAt"),
        priority: "primary",
        format: (value) =>
          new Intl.DateTimeFormat(undefined, {
            dateStyle: "medium",
            timeStyle: "short",
          }).format(new Date(String(value))),
      },
      { id: "actorName", label: t("app:audit.actor"), priority: "secondary" },
      {
        id: "action",
        label: t("app:audit.action"),
        format: (value) => (
          <Chip id={`${id}.action.${String(value)}`} size="small" label={String(value)} />
        ),
      },
      { id: "target", label: t("app:audit.target") },
      { id: "summary", label: t("app:audit.summary"), priority: "tertiary" },
    ],
    [id, t],
  );
  return (
    <Table
      id={id}
      columns={columns}
      rows={rows}
      loading={list.loading}
      emptyContent={t("app:audit.empty")}
      pagination
      page={list.page}
      rowsPerPage={list.pageSize}
      totalCount={list.result.totalCount}
      rowsPerPageOptions={[10, 20, 50, 100]}
      onPageChange={list.setPage}
      onRowsPerPageChange={list.setPageSize}
      getRowAriaLabel={(row) => `${String(row.actorName)} ${String(row.action)}`}
    />
  );
}
