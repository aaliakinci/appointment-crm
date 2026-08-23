import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Progress } from "@lily_platform/lily_ui/ui/atoms/Progress";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { Appointment, AppointmentTransition } from "../api/appointmentContract";
import type { useAppointmentDetail } from "../hooks/useAppointmentDetail";
import { localTimeFromOffset } from "../model/appointmentDate";
import { AppointmentStatusChip } from "./AppointmentStatusChip";

interface AppointmentDetailDialogProps {
  readonly canManage: boolean;
  readonly detail: ReturnType<typeof useAppointmentDetail>;
  readonly id: string;
  readonly onReschedule: () => void;
  readonly t: (key: string) => string;
}

export function AppointmentDetailDialog({
  canManage,
  detail,
  id,
  onReschedule,
  t,
}: AppointmentDetailDialogProps) {
  const appointment = detail.detail?.appointment ?? detail.selected;
  const transitions = appointment ? allowedTransitions(appointment, canManage) : [];
  const canReschedule =
    canManage &&
    appointment !== null &&
    (appointment.status === "scheduled" || appointment.status === "confirmed");

  return (
    <Dialog
      id={id}
      open={detail.open}
      fullWidth
      maxWidth="md"
      dialogTitle={t("app:appointments.detailTitle")}
      onOpenChange={(open) => !open && detail.close()}
      content={
        <Stack id={`${id}.content`} spacing={2} sx={{ pt: 1 }}>
          {detail.loading && <Progress id={`${id}.loading`} />}
          {detail.error && (
            <Alert id={`${id}.error`} severity="error">
              {detail.error}
            </Alert>
          )}
          {appointment && (
            <>
              <Stack
                id={`${id}.summary`}
                direction={{ xs: "column", sm: "row" }}
                spacing={2}
                sx={{ alignItems: { sm: "center" } }}
              >
                <Stack id={`${id}.summaryText`} spacing={0.5} sx={{ flex: 1 }}>
                  <Typography id={`${id}.customer`} component="h2" variant="h6">
                    {appointment.customerName}
                  </Typography>
                  <Typography id={`${id}.schedule`} variant="body1">
                    {formatDate(appointment.localStart)} ·{" "}
                    {localTimeFromOffset(appointment.localStart)}–
                    {localTimeFromOffset(appointment.localEnd)}
                  </Typography>
                  <Typography id={`${id}.timeZone`} variant="body2" color="text.secondary">
                    {appointment.timeZone}
                  </Typography>
                </Stack>
                <AppointmentStatusChip id={`${id}.status`} status={appointment.status} t={t} />
              </Stack>
              <Paper id={`${id}.snapshot`} variant="outlined" sx={{ p: 2 }}>
                <Stack id={`${id}.snapshot.content`} spacing={0.5}>
                  <Typography id={`${id}.snapshot.title`} component="h3" variant="subtitle2">
                    {t("app:appointments.serviceSnapshot")}
                  </Typography>
                  <Typography id={`${id}.service`} variant="body1">
                    {appointment.serviceName} · {appointment.serviceDurationMinutes}{" "}
                    {t("app:services.minutes")}
                  </Typography>
                  <Typography id={`${id}.price`} variant="body2" color="text.secondary">
                    {formatPrice(appointment.servicePrice, appointment.serviceCurrency)} ·{" "}
                    {appointment.employeeName}
                  </Typography>
                </Stack>
              </Paper>
              {appointment.notes && (
                <Alert id={`${id}.notes`} severity="info">
                  {appointment.notes}
                </Alert>
              )}
              {detail.detail && (
                <Stack id={`${id}.history`} spacing={1}>
                  <Typography id={`${id}.history.title`} component="h3" variant="subtitle1">
                    {t("app:appointments.history")}
                  </Typography>
                  {detail.detail.statusHistory.map((history) => (
                    <Paper
                      id={`${id}.history.${history.id}`}
                      key={history.id}
                      variant="outlined"
                      sx={{ p: 1.5 }}
                    >
                      <Typography id={`${id}.history.${history.id}.transition`} variant="body2">
                        {history.fromStatus
                          ? `${t(`app:appointments.status.${history.fromStatus}`)} → `
                          : ""}
                        {t(`app:appointments.status.${history.toStatus}`)}
                      </Typography>
                      <Typography
                        id={`${id}.history.${history.id}.meta`}
                        variant="caption"
                        color="text.secondary"
                      >
                        {history.actorName} · {formatInstant(history.occurredAtUtc)}
                        {history.reason ? ` · ${history.reason}` : ""}
                      </Typography>
                    </Paper>
                  ))}
                </Stack>
              )}
            </>
          )}
        </Stack>
      }
      actions={
        <Stack id={`${id}.actions`} direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
          {canReschedule && (
            <Button
              id={`${id}.reschedule`}
              variant="outlined"
              disabled={detail.mutationPending}
              onClick={onReschedule}
            >
              {t("app:appointments.reschedule")}
            </Button>
          )}
          {transitions.map((transition) => (
            <Button
              id={`${id}.${transition}`}
              key={transition}
              variant={
                transition === "confirm" || transition === "complete" ? "contained" : "outlined"
              }
              color={transition === "cancel" ? "warning" : "primary"}
              disabled={detail.mutationPending}
              onClick={() => void detail.transition(transition)}
            >
              {t(`app:appointments.actions.${transition}`)}
            </Button>
          ))}
          <Button id={`${id}.close`} disabled={detail.mutationPending} onClick={detail.close}>
            {t("app:common.close")}
          </Button>
        </Stack>
      }
    />
  );
}

function allowedTransitions(
  appointment: Appointment,
  canManage: boolean,
): readonly AppointmentTransition[] {
  const started = new Date(appointment.startsAtUtc).valueOf() <= Date.now();
  if (appointment.status === "scheduled") {
    return [
      "confirm",
      ...(started ? (["no-show"] as const) : []),
      ...(canManage ? (["cancel"] as const) : []),
    ];
  }
  if (appointment.status === "confirmed") {
    return [
      ...(started ? (["complete", "no-show"] as const) : []),
      ...(canManage ? (["cancel"] as const) : []),
    ];
  }
  return [];
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "full", timeZone: "UTC" }).format(
    new Date(`${value.slice(0, 10)}T12:00:00Z`),
  );
}

function formatInstant(value: string): string {
  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium", timeStyle: "short" }).format(
    new Date(value),
  );
}

function formatPrice(value: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(value);
}
