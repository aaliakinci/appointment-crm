import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { Availability, AvailabilitySlot } from "@/features/scheduling";

interface AvailabilitySlotPickerProps {
  readonly availability: Availability | null;
  readonly id: string;
  readonly onSelect: (slot: AvailabilitySlot) => void;
  readonly selectedSlot: AvailabilitySlot | null;
  readonly t: (key: string) => string;
}

export function AvailabilitySlotPicker({
  availability,
  id,
  onSelect,
  selectedSlot,
  t,
}: AvailabilitySlotPickerProps) {
  if (!availability) return null;
  if (availability.slots.length === 0) {
    return (
      <Alert id={`${id}.empty`} severity="warning">
        {t("app:appointments.noSlots")}
      </Alert>
    );
  }

  return (
    <Stack id={id} spacing={1}>
      <Typography id={`${id}.label`} component="h3" variant="subtitle2">
        {t("app:appointments.selectSlot")}
      </Typography>
      <Typography id={`${id}.meta`} variant="body2" color="text.secondary">
        {availability.date} · {availability.serviceDurationMinutes} {t("app:services.minutes")} ·{" "}
        {availability.timeZone}
      </Typography>
      <Stack id={`${id}.options`} direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
        {availability.slots.map((slot) => (
          <Button
            id={`${id}.${slot.startUtc}`}
            key={slot.startUtc}
            size="small"
            variant={selectedSlot?.startUtc === slot.startUtc ? "contained" : "outlined"}
            onClick={() => onSelect(slot)}
          >
            {slot.localStart.slice(11, 16)}–{slot.localEnd.slice(11, 16)}
          </Button>
        ))}
      </Stack>
    </Stack>
  );
}
