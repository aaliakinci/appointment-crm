import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Progress } from "@lily_platform/lily_ui/ui/atoms/Progress";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { useWeeklySchedule } from "../hooks/useWeeklySchedule";
import { SchedulePreview } from "./SchedulePreview";

interface WeeklyScheduleOverviewProps {
  readonly employeeId?: string;
  readonly id: string;
  readonly onInherit: () => void;
  readonly onOpenHistory: () => void;
  readonly scopeLabel: string;
  readonly t: (key: string) => string;
  readonly weekly: ReturnType<typeof useWeeklySchedule>;
}

export function WeeklyScheduleOverview({
  employeeId,
  id,
  onInherit,
  onOpenHistory,
  scopeLabel,
  t,
  weekly,
}: WeeklyScheduleOverviewProps) {
  const { schedule, state } = weekly;

  return (
    <Stack id={id} spacing={2}>
      <Stack
        id={`${id}.heading`}
        direction={{ xs: "column", sm: "row" }}
        spacing={2}
        sx={{ alignItems: { sm: "center" } }}
      >
        <Stack id={`${id}.headingText`} spacing={0.5} sx={{ flex: 1 }}>
          <Typography id={`${id}.title`} component="h2" variant="h6">
            {scopeLabel}
          </Typography>
          <Typography id={`${id}.description`} component="p" variant="body2" color="text.secondary">
            {t(`app:scheduling.scheduleStateHelp.${state}`)}
          </Typography>
        </Stack>
        <Stack id={`${id}.status`} direction="row" spacing={1} sx={{ alignItems: "center" }}>
          {weekly.loading && <Progress id={`${id}.loading`} size={24} />}
          {!weekly.loading && (
            <Chip
              id={`${id}.state`}
              label={t(`app:scheduling.scheduleState.${state}`)}
              color={state === "custom" ? "success" : state === "closed" ? "warning" : "info"}
              size="small"
            />
          )}
          {schedule?.effectiveVersionNumber !== null &&
            schedule?.effectiveVersionNumber !== undefined && (
              <Chip
                id={`${id}.version`}
                label={`${t("app:scheduling.activeVersion")} v${schedule.effectiveVersionNumber}`}
                variant="outlined"
                size="small"
              />
            )}
        </Stack>
      </Stack>

      {weekly.error && (
        <Alert id={`${id}.error`} severity="error">
          <Stack id={`${id}.errorContent`} spacing={1}>
            <span>{weekly.error}</span>
            {weekly.stale && (
              <Button id={`${id}.loadCurrent`} size="small" onClick={() => void weekly.load()}>
                {t("app:scheduling.loadCurrentVersion")}
              </Button>
            )}
          </Stack>
        </Alert>
      )}
      {weekly.success && (
        <Alert id={`${id}.success`} severity="success">
          {weekly.success}
        </Alert>
      )}

      {!weekly.loading && schedule && !weekly.editing && (
        <>
          {schedule.publishedAtUtc && (
            <Typography id={`${id}.published`} variant="body2" color="text.secondary">
              {t("app:scheduling.lastPublished")}: {formatInstant(schedule.publishedAtUtc)}
              {` · ${schedule.publishedBy ?? t("app:scheduling.migrationActor")}`}
            </Typography>
          )}
          {schedule.changeNote && (
            <Alert id={`${id}.changeNote`} severity="info">
              {schedule.changeNote}
            </Alert>
          )}
          <SchedulePreview
            id={`${id}.preview`}
            periods={schedule.periods}
            emptyMessage={
              state === "unconfigured"
                ? t("app:scheduling.scheduleStateHelp.unconfigured")
                : undefined
            }
            t={t}
          />
          <Stack id={`${id}.actions`} direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
            <Button id={`${id}.edit`} variant="contained" onClick={weekly.startEditing}>
              {state === "inherited"
                ? t("app:scheduling.createEmployeeSchedule")
                : state === "unconfigured"
                  ? t("app:scheduling.createSchedule")
                  : t("app:scheduling.editSchedule")}
            </Button>
            {schedule.revision > 0 && (
              <Button id={`${id}.history`} variant="outlined" onClick={onOpenHistory}>
                {t("app:scheduling.versionHistory")}
              </Button>
            )}
            {employeeId && (state === "custom" || state === "closed") && (
              <Button id={`${id}.inherit`} color="warning" onClick={onInherit}>
                {t("app:scheduling.restoreInheritance")}
              </Button>
            )}
          </Stack>
        </>
      )}
    </Stack>
  );
}

function formatInstant(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
