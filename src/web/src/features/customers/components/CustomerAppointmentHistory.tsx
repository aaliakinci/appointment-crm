import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Table, type TableColumn, type TableRowData } from "@lily_platform/lily_ui/ui/atoms/Table";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { useMemo } from "react";

import type { Appointment } from "@/features/appointments/catalog";

import type { useCustomerAppointmentHistory } from "../hooks/useCustomerAppointmentHistory";

interface AppointmentRow extends TableRowData {
  readonly resource: Appointment;
  readonly localStart: string;
  readonly serviceName: string;
  readonly employeeName: string;
  readonly status: string;
  readonly price: string;
}

interface CustomerAppointmentHistoryProps {
  readonly history: ReturnType<typeof useCustomerAppointmentHistory>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function CustomerAppointmentHistory({ history, id, t }: CustomerAppointmentHistoryProps) {
  const rows = useMemo<AppointmentRow[]>(
    () =>
      history.result.items.map((appointment) => ({
        id: appointment.id,
        resource: appointment,
        localStart: appointment.localStart,
        serviceName: appointment.serviceName,
        employeeName: appointment.employeeName,
        status: t(`app:appointments.status.${appointment.status}`),
        price: new Intl.NumberFormat(undefined, {
          style: "currency",
          currency: appointment.serviceCurrency,
        }).format(appointment.servicePrice),
      })),
    [history.result.items, t],
  );
  const columns = useMemo<TableColumn[]>(
    () => [
      {
        id: "localStart",
        label: t("app:customers.appointmentDate"),
        priority: "primary",
        format: (value) => formatLocalInstant(String(value)),
      },
      { id: "serviceName", label: t("app:appointments.service"), priority: "secondary" },
      { id: "employeeName", label: t("app:appointments.employee") },
      {
        id: "status",
        label: t("app:common.status"),
        format: (value) => (
          <Chip id={`${id}.status.${String(value)}`} size="small" label={String(value)} />
        ),
      },
      { id: "price", label: t("app:services.price"), priority: "tertiary" },
    ],
    [id, t],
  );
  return (
    <Stack id={id} spacing={1.5}>
      <Typography id={`${id}.title`} component="h3" variant="h6">
        {t("app:customers.appointmentHistory")}
      </Typography>
      {history.loadError && (
        <Alert id={`${id}.error`} severity="error">
          {t("app:customers.appointmentHistoryError")}
        </Alert>
      )}
      <Table
        id={`${id}.table`}
        columns={columns}
        rows={rows}
        loading={history.loading}
        emptyContent={t("app:customers.appointmentHistoryEmpty")}
        pagination
        page={history.page}
        rowsPerPage={history.pageSize}
        totalCount={history.result.totalCount}
        rowsPerPageOptions={[5, 10, 20]}
        onPageChange={history.setPage}
        onRowsPerPageChange={history.setPageSize}
        getRowAriaLabel={(row) => `${String(row.localStart)} ${String(row.serviceName)}`}
      />
    </Stack>
  );
}

function formatLocalInstant(value: string): string {
  return `${new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeZone: "UTC" }).format(
    new Date(`${value.slice(0, 10)}T12:00:00Z`),
  )} ${value.slice(11, 16)}`;
}
