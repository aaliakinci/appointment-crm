import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { useWeeklySchedule } from "../hooks/useWeeklySchedule";
import { useWeeklyScheduleHistory } from "../hooks/useWeeklyScheduleHistory";
import type { WeeklyHoursFormValues } from "../model/weeklyScheduleForm";
import { WeeklyScheduleDialogs } from "./WeeklyScheduleDialogs";
import { WeeklyScheduleEditor } from "./WeeklyScheduleEditor";
import { WeeklyScheduleHistoryDrawer } from "./WeeklyScheduleHistoryDrawer";
import { WeeklyScheduleOverview } from "./WeeklyScheduleOverview";

interface WeeklyHoursPanelProps {
  readonly employeeId?: string;
  readonly id: string;
  readonly onDirtyChange?: (dirty: boolean) => void;
  readonly scopeLabel: string;
  readonly t: (key: string) => string;
}

export function WeeklyHoursPanel({
  employeeId,
  id,
  onDirtyChange,
  scopeLabel,
  t,
}: WeeklyHoursPanelProps) {
  const weekly = useWeeklySchedule({ employeeId, onDirtyChange, t });
  const history = useWeeklyScheduleHistory({
    employeeId,
    onConflict: weekly.handleMutationError,
    onRestored: weekly.acceptRestoredSchedule,
    t,
  });

  async function publish(values: WeeklyHoursFormValues) {
    if (await weekly.submit(values)) history.reloadFirstPage();
  }

  async function inherit() {
    if (await weekly.inherit()) history.reloadFirstPage();
  }

  return (
    <Paper id={id} variant="outlined" sx={{ p: 3 }}>
      <Stack id={`${id}.content`} spacing={2}>
        <WeeklyScheduleOverview
          id={`${id}.overview`}
          employeeId={employeeId}
          weekly={weekly}
          scopeLabel={scopeLabel}
          onOpenHistory={history.openHistory}
          onInherit={() => void inherit()}
          t={t}
        />
        <WeeklyScheduleEditor
          id={`${id}.editor`}
          employeeId={employeeId}
          weekly={weekly}
          onSubmit={publish}
          t={t}
        />
      </Stack>
      <WeeklyScheduleHistoryDrawer
        id={`${id}.history`}
        currentVersionId={weekly.schedule?.versionId}
        history={history}
        t={t}
      />
      <WeeklyScheduleDialogs id={`${id}.dialogs`} weekly={weekly} history={history} t={t} />
    </Paper>
  );
}
