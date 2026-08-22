import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import { useDateOverrides } from "../hooks/useDateOverrides";
import { DateOverrideEditor } from "./DateOverrideEditor";
import { DateOverrideList } from "./DateOverrideList";

interface DateOverridesPanelProps {
  readonly employeeId?: string;
  readonly id: string;
  readonly onDirtyChange?: (dirty: boolean) => void;
  readonly t: (key: string) => string;
  readonly today: string;
}

export function DateOverridesPanel({
  employeeId,
  id,
  onDirtyChange,
  t,
  today,
}: DateOverridesPanelProps) {
  const editor = useDateOverrides({ employeeId, onDirtyChange, t, today });

  return (
    <Paper id={id} variant="outlined" sx={{ p: 3 }}>
      <Stack id={`${id}.content`} spacing={3}>
        <Typography id={`${id}.title`} component="h2" variant="h6">
          {t("app:scheduling.overrideTitle")}
        </Typography>
        {editor.error && (
          <Alert id={`${id}.error`} severity="error">
            {editor.error}
          </Alert>
        )}
        <DateOverrideEditor id={`${id}.editor`} editor={editor} t={t} />
        <DateOverrideList
          id={`${id}.list`}
          items={editor.items}
          disabled={editor.isDirty || editor.formStatus.isSubmitting}
          onEdit={editor.edit}
          onRemove={(date) => void editor.remove(date)}
          t={t}
        />
      </Stack>
    </Paper>
  );
}
