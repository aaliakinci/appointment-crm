import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Table, type TableColumn, type TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { useMemo } from "react";

import type { Membership } from "../api/membershipContract";

interface MembershipRow extends TableRowData {
  readonly resource: Membership;
  readonly displayName: string;
  readonly email: string;
  readonly role: string;
  readonly status: string;
  readonly updatedAtUtc: string;
}

interface MembershipTableProps {
  readonly id: string;
  readonly loading: boolean;
  readonly memberships: readonly Membership[];
  readonly onSelect: (membership: Membership) => void;
  readonly t: (key: string) => string;
}

export function MembershipTable({ id, loading, memberships, onSelect, t }: MembershipTableProps) {
  const rows = useMemo<MembershipRow[]>(
    () =>
      memberships.map((membership) => ({
        id: membership.id,
        resource: membership,
        displayName: membership.displayName,
        email: membership.email,
        role: t(`app:memberships.roles.${membership.role}`),
        status: membership.isActive ? t("app:common.active") : t("app:common.inactive"),
        updatedAtUtc: membership.updatedAtUtc,
      })),
    [memberships, t],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      { id: "displayName", label: t("app:memberships.user"), priority: "primary" },
      { id: "email", label: t("app:memberships.email"), priority: "secondary" },
      { id: "role", label: t("app:memberships.role") },
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
        format: (value) =>
          new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(
            new Date(String(value)),
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
      loading={loading}
      emptyContent={t("app:memberships.empty")}
      getRowAriaLabel={(row) => String(row.displayName)}
      onRowActivate={(row) => onSelect((row as MembershipRow).resource)}
    />
  );
}
