import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

interface UnsavedSchedulingChangesDialogProps {
  readonly id: string;
  readonly onDiscard: () => void;
  readonly onKeepEditing: () => void;
  readonly open: boolean;
  readonly t: (key: string) => string;
}

export function UnsavedSchedulingChangesDialog({
  id,
  onDiscard,
  onKeepEditing,
  open,
  t,
}: UnsavedSchedulingChangesDialogProps) {
  return (
    <Dialog
      id={id}
      open={open}
      dialogTitle={t("app:scheduling.discardTitle")}
      content={t("app:scheduling.discardMessage")}
      actions={
        <Stack id={`${id}.actions`} direction="row" spacing={1}>
          <Button id={`${id}.stay`} onClick={onKeepEditing}>
            {t("app:scheduling.keepEditing")}
          </Button>
          <Button id={`${id}.discard`} color="error" onClick={onDiscard}>
            {t("app:scheduling.discardChanges")}
          </Button>
        </Stack>
      }
    />
  );
}
