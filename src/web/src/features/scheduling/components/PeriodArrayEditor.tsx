import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { TextField } from "@lily_platform/lily_ui/ui/atoms/TextField";

import type { EditablePeriod } from "../model/schedulePeriod";

interface PeriodArrayEditorProps {
  readonly id: string;
  readonly includeDay: boolean;
  readonly item: EditablePeriod;
  readonly t: (key: string) => string;
  readonly updateItem: (value: EditablePeriod) => void;
}

export function PeriodArrayEditor({ id, includeDay, item, t, updateItem }: PeriodArrayEditorProps) {
  return (
    <Stack id={id} direction={{ xs: "column", sm: "row" }} spacing={2}>
      {includeDay && (
        <Select
          id={`${id}.day`}
          label={t("app:scheduling.day")}
          value={item.dayOfWeek}
          options={Array.from({ length: 7 }, (_, index) => ({
            id: String(index + 1),
            value: String(index + 1),
            label: t(`app:scheduling.days.${String(index + 1)}`),
          }))}
          onValueChange={(value) => updateItem({ ...item, dayOfWeek: String(value) })}
          sx={{ minWidth: 160 }}
        />
      )}
      <TextField
        id={`${id}.start`}
        label={t("app:scheduling.startTime")}
        value={item.startTime}
        helperText="HH:mm"
        onValueChange={(value) => updateItem({ ...item, startTime: value })}
        fullWidth
      />
      <TextField
        id={`${id}.end`}
        label={t("app:scheduling.endTime")}
        value={item.endTime}
        helperText="HH:mm / 24:00"
        onValueChange={(value) => updateItem({ ...item, endTime: value })}
        fullWidth
      />
    </Stack>
  );
}
