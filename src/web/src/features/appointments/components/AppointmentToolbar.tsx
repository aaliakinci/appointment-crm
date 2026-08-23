import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import { LocalizedLilyDatePicker } from "@/shared/forms/LocalizedLilyDateForm";

import type { useAppointmentCalendar } from "../hooks/useAppointmentCalendar";
import { formatLocalDate } from "../model/appointmentDate";

interface AppointmentToolbarProps {
  readonly canManage: boolean;
  readonly calendar: ReturnType<typeof useAppointmentCalendar>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function AppointmentToolbar({ canManage, calendar, id, t }: AppointmentToolbarProps) {
  return (
    <Paper id={id} variant="outlined" sx={{ p: 2 }}>
      <Stack id={`${id}.content`} spacing={2}>
        <Stack
          id={`${id}.weekNavigation`}
          direction={{ xs: "column", md: "row" }}
          spacing={1}
          sx={{ alignItems: { md: "center" } }}
        >
          <Stack id={`${id}.buttons`} direction="row" spacing={1}>
            <Button id={`${id}.previous`} variant="outlined" onClick={calendar.previousPeriod}>
              {t("app:appointments.previous")}
            </Button>
            <Button id={`${id}.today`} variant="outlined" onClick={calendar.currentPeriod}>
              {t("app:appointments.today")}
            </Button>
            <Button id={`${id}.next`} variant="outlined" onClick={calendar.nextPeriod}>
              {t("app:appointments.next")}
            </Button>
          </Stack>
          <Typography id={`${id}.range`} component="p" variant="subtitle1" sx={{ flex: 1 }}>
            {calendar.viewMode === "day"
              ? formatLocalDate(calendar.dates[0]!)
              : `${formatLocalDate(calendar.dates[0]!)} – ${formatLocalDate(calendar.dates[6]!)}`}
          </Typography>
          <Typography id={`${id}.timeZone`} variant="body2" color="text.secondary">
            {calendar.timeZone}
          </Typography>
        </Stack>
        <Stack id={`${id}.filters`} direction={{ xs: "column", md: "row" }} spacing={2}>
          <Stack id={`${id}.viewMode`} direction="row" spacing={1}>
            <Button
              id={`${id}.dayView`}
              variant={calendar.viewMode === "day" ? "contained" : "outlined"}
              onClick={() => calendar.setViewMode("day")}
            >
              {t("app:appointments.dayView")}
            </Button>
            <Button
              id={`${id}.weekView`}
              variant={calendar.viewMode === "week" ? "contained" : "outlined"}
              onClick={() => calendar.setViewMode("week")}
            >
              {t("app:appointments.weekView")}
            </Button>
          </Stack>
          <LocalizedLilyDatePicker
            id={`${id}.selectedDate`}
            label={t("app:appointments.date")}
            value={calendar.selectedDate}
            onValueChange={calendar.setSelectedDate}
            sx={{ minWidth: 190 }}
          />
          <Select
            id={`${id}.status`}
            label={t("app:common.status")}
            value={calendar.statusFilter}
            options={[
              { id: "all", value: "", label: t("app:common.all") },
              ...(["scheduled", "confirmed", "completed", "cancelled", "no-show"] as const).map(
                (status) => ({
                  id: status,
                  value: status,
                  label: t(`app:appointments.status.${status}`),
                }),
              ),
            ]}
            onValueChange={(value) =>
              calendar.setStatusFilter(String(value) as typeof calendar.statusFilter)
            }
            sx={{ minWidth: 220 }}
          />
          {canManage && (
            <Select
              id={`${id}.employee`}
              label={t("app:appointments.employee")}
              value={calendar.employeeFilter}
              options={[
                { id: "all", value: "", label: t("app:common.all") },
                ...calendar.employees.map((employee) => ({
                  id: employee.id,
                  value: employee.id,
                  label: employee.name,
                })),
              ]}
              onValueChange={(value) => calendar.setEmployeeFilter(String(value))}
              sx={{ minWidth: 240 }}
            />
          )}
        </Stack>
      </Stack>
    </Paper>
  );
}
