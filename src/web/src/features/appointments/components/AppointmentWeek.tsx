import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Progress } from "@lily_platform/lily_ui/ui/atoms/Progress";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { Appointment, AppointmentTransition } from "../api/appointmentContract";
import {
  formatLocalDate,
  localDateFromOffset,
  localTimeFromOffset,
} from "../model/appointmentDate";
import { AppointmentStatusChip } from "./AppointmentStatusChip";

interface AppointmentWeekProps {
  readonly appointments: readonly Appointment[];
  readonly dates: readonly string[];
  readonly id: string;
  readonly loading: boolean;
  readonly onSelect: (appointment: Appointment) => void;
  readonly onQuickTransition: (appointment: Appointment, transition: AppointmentTransition) => void;
  readonly pendingAppointmentId: string | null;
  readonly t: (key: string) => string;
  readonly today: string;
}

export function AppointmentWeek({
  appointments,
  dates,
  id,
  loading,
  onSelect,
  onQuickTransition,
  pendingAppointmentId,
  t,
  today,
}: AppointmentWeekProps) {
  if (loading) {
    return (
      <Paper
        id={id}
        variant="outlined"
        sx={{ minHeight: 360, display: "grid", placeItems: "center" }}
      >
        <Progress id={`${id}.loading`} aria-label={t("app:appointments.loading")} />
      </Paper>
    );
  }

  return (
    <Box
      id={id}
      sx={{
        display: "grid",
        gridTemplateColumns: {
          xs: "1fr",
          lg: dates.length === 1 ? "1fr" : "repeat(7, minmax(0, 1fr))",
        },
        gap: 1.5,
      }}
    >
      {dates.map((date) => {
        const items = appointments.filter(
          (appointment) => localDateFromOffset(appointment.localStart) === date,
        );
        return (
          <Paper
            id={`${id}.${date}`}
            key={date}
            variant="outlined"
            sx={{
              p: 1.5,
              minHeight: { lg: 360 },
              borderColor: date === today ? "primary.main" : undefined,
            }}
          >
            <Stack id={`${id}.${date}.content`} spacing={1}>
              <Typography
                id={`${id}.${date}.heading`}
                component="h2"
                variant="subtitle2"
                color={date === today ? "primary.main" : "text.primary"}
              >
                {formatLocalDate(date)}
              </Typography>
              {items.map((appointment) => {
                const quickTransition = getQuickTransition(appointment);
                return (
                  <Paper
                    id={`${id}.appointment.${appointment.id}`}
                    key={appointment.id}
                    variant="outlined"
                    sx={{
                      p: 1,
                      opacity: appointment.status === "cancelled" ? 0.65 : 1,
                    }}
                  >
                    <Stack id={`${id}.appointment.${appointment.id}.content`} spacing={1}>
                      <Button
                        id={`${id}.appointment.${appointment.id}.detail`}
                        color={appointment.status === "cancelled" ? "inherit" : "primary"}
                        sx={{
                          p: 0,
                          justifyContent: "flex-start",
                          textAlign: "left",
                          textTransform: "none",
                        }}
                        onClick={() => onSelect(appointment)}
                      >
                        <Stack id={`${id}.appointment.${appointment.id}.summary`} spacing={0.5}>
                          <Typography
                            id={`${id}.appointment.${appointment.id}.time`}
                            component="span"
                            variant="subtitle2"
                          >
                            {localTimeFromOffset(appointment.localStart)}–
                            {localTimeFromOffset(appointment.localEnd)}
                          </Typography>
                          <Typography
                            id={`${id}.appointment.${appointment.id}.customer`}
                            component="span"
                            variant="body2"
                          >
                            {appointment.customerName}
                          </Typography>
                          <Typography
                            id={`${id}.appointment.${appointment.id}.meta`}
                            component="span"
                            variant="caption"
                            color="text.secondary"
                          >
                            {appointment.employeeName} · {appointment.serviceName}
                          </Typography>
                          <AppointmentStatusChip
                            id={`${id}.appointment.${appointment.id}.status`}
                            status={appointment.status}
                            t={t}
                          />
                        </Stack>
                      </Button>
                      {quickTransition && (
                        <Button
                          id={`${id}.appointment.${appointment.id}.${quickTransition}`}
                          size="small"
                          variant="text"
                          loading={pendingAppointmentId === appointment.id}
                          disabled={pendingAppointmentId !== null}
                          onClick={() => onQuickTransition(appointment, quickTransition)}
                        >
                          {t(`app:appointments.actions.${quickTransition}`)}
                        </Button>
                      )}
                    </Stack>
                  </Paper>
                );
              })}
              {items.length === 0 && (
                <Typography id={`${id}.${date}.empty`} variant="caption" color="text.secondary">
                  {t("app:appointments.emptyDay")}
                </Typography>
              )}
            </Stack>
          </Paper>
        );
      })}
    </Box>
  );
}

function getQuickTransition(appointment: Appointment): AppointmentTransition | null {
  if (appointment.status === "scheduled") return "confirm";
  if (
    appointment.status === "confirmed" &&
    new Date(appointment.startsAtUtc).valueOf() <= Date.now()
  ) {
    return "complete";
  }
  return null;
}
