import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";

import type { AppointmentStatus } from "../api/appointmentContract";

interface AppointmentStatusChipProps {
  readonly id: string;
  readonly status: AppointmentStatus;
  readonly t: (key: string) => string;
}

export function AppointmentStatusChip({ id, status, t }: AppointmentStatusChipProps) {
  const color =
    status === "confirmed"
      ? "info"
      : status === "completed"
        ? "success"
        : status === "cancelled"
          ? "default"
          : status === "no-show"
            ? "warning"
            : "primary";
  return <Chip id={id} label={t(`app:appointments.status.${status}`)} color={color} size="small" />;
}
