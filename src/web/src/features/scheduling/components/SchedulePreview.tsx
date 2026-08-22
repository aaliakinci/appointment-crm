import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { SchedulePeriod } from "../api/schedulingContract";
import { fromMinute } from "../model/schedulePeriod";

interface SchedulePreviewProps {
  readonly emptyMessage?: string;
  readonly id: string;
  readonly periods: readonly SchedulePeriod[];
  readonly t: (key: string) => string;
}

export function SchedulePreview({ emptyMessage, id, periods, t }: SchedulePreviewProps) {
  if (periods.length === 0) {
    return (
      <Alert id={`${id}.empty`} severity="warning">
        {emptyMessage ?? t("app:scheduling.closedWeek")}
      </Alert>
    );
  }

  return (
    <Stack id={id} spacing={0.75}>
      {groupPeriods(periods).map(([day, ranges]) => (
        <Stack id={`${id}.day.${day}`} key={day} direction="row" spacing={2}>
          <Typography id={`${id}.day.${day}.label`} sx={{ minWidth: 110 }}>
            {t(`app:scheduling.days.${day}`)}
          </Typography>
          <Typography id={`${id}.day.${day}.ranges`} color="text.secondary">
            {ranges.join(", ")}
          </Typography>
        </Stack>
      ))}
    </Stack>
  );
}

function groupPeriods(periods: readonly SchedulePeriod[]): readonly [number, readonly string[]][] {
  const groups = new Map<number, string[]>();
  for (const period of periods) {
    const ranges = groups.get(period.dayOfWeek) ?? [];
    ranges.push(`${fromMinute(period.startMinute)}–${fromMinute(period.endMinute)}`);
    groups.set(period.dayOfWeek, ranges);
  }
  return [...groups.entries()].sort(([left], [right]) => left - right);
}
