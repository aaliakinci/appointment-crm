import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { Employee } from "@/features/employees/catalog";

import { useTimeOff } from "../hooks/useTimeOff";
import { TimeOffEditor } from "./TimeOffEditor";
import { TimeOffList } from "./TimeOffList";

interface TimeOffPanelProps {
  readonly employees: readonly Employee[];
  readonly id: string;
  readonly onDirtyChange?: (dirty: boolean) => void;
  readonly t: (key: string) => string;
  readonly timeZone: string;
  readonly today: string;
}

export function TimeOffPanel({
  employees,
  id,
  onDirtyChange,
  t,
  timeZone,
  today,
}: TimeOffPanelProps) {
  const editor = useTimeOff({ employees, onDirtyChange, t, timeZone, today });

  return (
    <Paper id={id} variant="outlined" sx={{ p: 3 }}>
      <Stack id={`${id}.content`} spacing={3}>
        <Stack id={`${id}.heading`} spacing={0.5}>
          <Typography id={`${id}.title`} component="h2" variant="h6">
            {t("app:scheduling.timeOffTitle")}
          </Typography>
          <Typography id={`${id}.description`} variant="body2" color="text.secondary">
            {t("app:scheduling.timeOffDescription")}
          </Typography>
          <Typography id={`${id}.timeZone`} variant="body2" color="text.secondary">
            {t("app:scheduling.timeZone")}: {timeZone}
          </Typography>
        </Stack>
        {editor.error && (
          <Alert id={`${id}.error`} severity="error">
            {editor.error}
          </Alert>
        )}
        <TimeOffEditor id={`${id}.editor`} editor={editor} t={t} />
        <TimeOffList
          id={`${id}.list`}
          items={editor.items}
          onRemove={(timeOffId) => void editor.remove(timeOffId)}
          t={t}
        />
      </Stack>
    </Paper>
  );
}
