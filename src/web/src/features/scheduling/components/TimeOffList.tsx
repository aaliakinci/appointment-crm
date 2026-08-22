import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { TimeOff } from "../api/schedulingContract";

interface TimeOffListProps {
  readonly id: string;
  readonly items: readonly TimeOff[];
  readonly onRemove: (id: string) => void;
  readonly t: (key: string) => string;
}

export function TimeOffList({ id, items, onRemove, t }: TimeOffListProps) {
  return (
    <Stack id={id} spacing={1}>
      {items.length === 0 && (
        <Typography id={`${id}.empty`} variant="body2" color="text.secondary">
          {t("app:scheduling.timeOffEmpty")}
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
            <Stack id={`${id}.item.${item.id}.summary`} spacing={0.5} sx={{ flex: 1 }}>
              <Typography id={`${id}.item.${item.id}.employee`}>{item.employeeName}</Typography>
              <Typography id={`${id}.item.${item.id}.range`} variant="body2" color="text.secondary">
                {item.localStartDate} {item.localStartTime.slice(0, 5)} – {item.localEndDate}{" "}
                {item.localEndTime.slice(0, 5)} ({item.timeZone})
              </Typography>
              {item.reason && (
                <Typography id={`${id}.item.${item.id}.reason`} variant="body2">
                  {item.reason}
                </Typography>
              )}
            </Stack>
            <Button
              id={`${id}.item.${item.id}.delete`}
              size="small"
              color="error"
              onClick={() => onRemove(item.id)}
            >
              {t("app:scheduling.delete")}
            </Button>
          </Stack>
        </Paper>
      ))}
    </Stack>
  );
}
