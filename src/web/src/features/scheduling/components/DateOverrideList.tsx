import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { DateOverride } from "../api/schedulingContract";
import { fromMinute } from "../model/schedulePeriod";

interface DateOverrideListProps {
  readonly disabled?: boolean;
  readonly id: string;
  readonly items: readonly DateOverride[];
  readonly onEdit: (item: DateOverride) => void;
  readonly onRemove: (date: string) => void;
  readonly t: (key: string) => string;
}

export function DateOverrideList({
  disabled,
  id,
  items,
  onEdit,
  onRemove,
  t,
}: DateOverrideListProps) {
  return (
    <Stack id={id} spacing={1}>
      {items.length === 0 && (
        <Typography id={`${id}.empty`} variant="body2" color="text.secondary">
          {t("app:scheduling.overrideEmpty")}
        </Typography>
      )}
      {items.map((item) => (
        <Paper id={`${id}.item.${item.id}`} key={item.id} variant="outlined" sx={{ p: 2 }}>
          <Stack
            id={`${id}.item.${item.id}.content`}
            direction={{ xs: "column", md: "row" }}
            spacing={2}
            sx={{ alignItems: { md: "center" } }}
          >
            <Typography id={`${id}.item.${item.id}.date`} sx={{ flex: 1 }}>
              {item.date} · {formatPeriods(item) || "—"}
            </Typography>
            <Chip
              id={`${id}.item.${item.id}.status`}
              label={item.isClosed ? t("app:scheduling.closed") : t("app:scheduling.open")}
              color={item.isClosed ? "default" : "success"}
              size="small"
            />
            <Button
              id={`${id}.item.${item.id}.edit`}
              size="small"
              disabled={disabled}
              onClick={() => onEdit(item)}
            >
              {t("app:scheduling.edit")}
            </Button>
            <Button
              id={`${id}.item.${item.id}.delete`}
              size="small"
              color="error"
              disabled={disabled}
              onClick={() => onRemove(item.date)}
            >
              {t("app:scheduling.delete")}
            </Button>
          </Stack>
        </Paper>
      ))}
    </Stack>
  );
}

function formatPeriods(item: DateOverride): string {
  return item.periods
    .map((period) => `${fromMinute(period.startMinute)}–${fromMinute(period.endMinute)}`)
    .join(", ");
}
