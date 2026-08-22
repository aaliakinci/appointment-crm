import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import type { useWeeklySchedule } from "../hooks/useWeeklySchedule";
import type { useWeeklyScheduleHistory } from "../hooks/useWeeklyScheduleHistory";

interface WeeklyScheduleDialogsProps {
  readonly history: ReturnType<typeof useWeeklyScheduleHistory>;
  readonly id: string;
  readonly t: (key: string) => string;
  readonly weekly: ReturnType<typeof useWeeklySchedule>;
}

export function WeeklyScheduleDialogs({ history, id, t, weekly }: WeeklyScheduleDialogsProps) {
  return (
    <>
      <Dialog
        id={`${id}.discard`}
        open={weekly.discardOpen}
        dialogTitle={t("app:scheduling.discardTitle")}
        content={t("app:scheduling.discardMessage")}
        actions={
          <Stack id={`${id}.discard.actions`} direction="row" spacing={1}>
            <Button id={`${id}.discard.stay`} onClick={weekly.keepEditing}>
              {t("app:scheduling.keepEditing")}
            </Button>
            <Button id={`${id}.discard.confirm`} color="error" onClick={weekly.discardChanges}>
              {t("app:scheduling.discardChanges")}
            </Button>
          </Stack>
        }
      />
      <Dialog
        id={`${id}.restore`}
        open={history.restoreCandidate !== null}
        dialogTitle={t("app:scheduling.restoreVersionTitle")}
        content={t("app:scheduling.restoreVersionMessage")}
        actions={
          <Stack id={`${id}.restore.actions`} direction="row" spacing={1}>
            <Button id={`${id}.restore.cancel`} onClick={history.cancelRestore}>
              {t("app:common.close")}
            </Button>
            <Button
              id={`${id}.restore.confirm`}
              variant="contained"
              onClick={() => weekly.schedule && void history.restore(weekly.schedule.revision)}
            >
              {t("app:scheduling.restoreAsNewVersion")}
            </Button>
          </Stack>
        }
      />
    </>
  );
}
