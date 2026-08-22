import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { Availability } from "../api/schedulingContract";

interface AvailabilityResultsProps {
  readonly availability: Availability;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function AvailabilityResults({ availability, id, t }: AvailabilityResultsProps) {
  return (
    <Stack id={id} spacing={1}>
      <Typography id={`${id}.summary`} variant="body2" color="text.secondary">
        {availability.date} · {availability.serviceDurationMinutes} {t("app:services.minutes")} ·{" "}
        {availability.timeZone}
      </Typography>
      <Stack id={`${id}.slots`} direction="row" spacing={1} sx={{ flexWrap: "wrap", gap: 1 }}>
        {availability.slots.map((slot) => (
          <Chip
            id={`${id}.slot.${slot.startUtc}`}
            key={slot.startUtc}
            label={formatSlot(slot.localStart)}
            variant="outlined"
            color="success"
          />
        ))}
        {availability.slots.length === 0 && (
          <Typography id={`${id}.empty`} variant="body2">
            {t("app:scheduling.availabilityEmpty")}
          </Typography>
        )}
      </Stack>
    </Stack>
  );
}

function formatSlot(value: string): string {
  const offset = value.match(/([+-]\d{2}:\d{2}|Z)$/)?.[1] ?? "";
  return `${value.slice(11, 16)} ${offset}`.trim();
}
