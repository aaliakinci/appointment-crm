import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Drawer } from "@lily_platform/lily_ui/ui/atoms/Drawer";
import { Pagination } from "@lily_platform/lily_ui/ui/atoms/Pagination";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Progress } from "@lily_platform/lily_ui/ui/atoms/Progress";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { useWeeklyScheduleHistory } from "../hooks/useWeeklyScheduleHistory";
import { SchedulePreview } from "./SchedulePreview";

interface WeeklyScheduleHistoryDrawerProps {
  readonly currentVersionId: string | null | undefined;
  readonly history: ReturnType<typeof useWeeklyScheduleHistory>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function WeeklyScheduleHistoryDrawer({
  currentVersionId,
  history,
  id,
  t,
}: WeeklyScheduleHistoryDrawerProps) {
  return (
    <Drawer
      id={id}
      anchor="right"
      width={560}
      open={history.open}
      onOpenChange={(open) => !open && history.closeHistory()}
      header={
        <Stack id={`${id}.header`} direction="row" spacing={2} sx={{ alignItems: "center", p: 2 }}>
          <Typography id={`${id}.title`} component="h2" variant="h6" sx={{ flex: 1 }}>
            {t("app:scheduling.versionHistory")}
          </Typography>
          <Button id={`${id}.close`} onClick={history.closeHistory}>
            {t("app:common.close")}
          </Button>
        </Stack>
      }
    >
      <Stack id={`${id}.content`} spacing={2} sx={{ p: 2 }}>
        {history.loading && <Progress id={`${id}.loading`} />}
        {history.error && (
          <Alert id={`${id}.error`} severity="error">
            {history.error}
          </Alert>
        )}
        {!history.loading && history.items.length === 0 && (
          <Typography id={`${id}.empty`} color="text.secondary">
            {t("app:scheduling.historyEmpty")}
          </Typography>
        )}
        {history.items.map((version) => {
          const isCurrent = version.id === currentVersionId;
          return (
            <Paper
              id={`${id}.version.${version.id}`}
              key={version.id}
              variant="outlined"
              sx={{ p: 2 }}
            >
              <Stack id={`${id}.version.${version.id}.content`} spacing={1}>
                <Stack
                  id={`${id}.version.${version.id}.heading`}
                  direction="row"
                  spacing={1}
                  sx={{ alignItems: "center" }}
                >
                  <Typography id={`${id}.version.${version.id}.title`} sx={{ flex: 1 }}>
                    v{version.versionNumber} · {t(`app:scheduling.versionMode.${version.mode}`)}
                  </Typography>
                  {isCurrent && (
                    <Chip
                      id={`${id}.version.${version.id}.current`}
                      label={t("app:scheduling.currentVersion")}
                      color="success"
                      size="small"
                    />
                  )}
                </Stack>
                <Typography
                  id={`${id}.version.${version.id}.metadata`}
                  variant="body2"
                  color="text.secondary"
                >
                  {formatInstant(version.createdAtUtc)} ·{" "}
                  {version.publishedBy ?? t("app:scheduling.migrationActor")}
                </Typography>
                {version.changeNote && (
                  <Typography id={`${id}.version.${version.id}.note`} variant="body2">
                    {version.changeNote}
                  </Typography>
                )}
                {version.restoredFromVersionNumber !== null && (
                  <Typography
                    id={`${id}.version.${version.id}.restoredFrom`}
                    variant="body2"
                    color="text.secondary"
                  >
                    {t("app:scheduling.restoredFromVersion")} v{version.restoredFromVersionNumber}
                  </Typography>
                )}
                <Stack id={`${id}.version.${version.id}.actions`} direction="row" spacing={1}>
                  <Button
                    id={`${id}.version.${version.id}.inspect`}
                    size="small"
                    onClick={() => history.selectVersion(version)}
                  >
                    {t("app:scheduling.inspectVersion")}
                  </Button>
                  {!isCurrent && (
                    <Button
                      id={`${id}.version.${version.id}.restore`}
                      size="small"
                      onClick={() => history.requestRestore(version)}
                    >
                      {t("app:scheduling.restoreAsNewVersion")}
                    </Button>
                  )}
                </Stack>
              </Stack>
            </Paper>
          );
        })}
        {history.pages > 1 && (
          <Pagination
            id={`${id}.pagination`}
            page={history.page}
            count={history.pages}
            onPageChange={history.setPage}
          />
        )}
        {history.selectedVersion && (
          <Paper id={`${id}.detail`} variant="outlined" sx={{ p: 2 }}>
            <Stack id={`${id}.detail.content`} spacing={1}>
              <Typography id={`${id}.detail.title`} component="h3" variant="subtitle1">
                {t("app:scheduling.versionDetail")} · v{history.selectedVersion.versionNumber}
              </Typography>
              <SchedulePreview
                id={`${id}.detail.preview`}
                periods={history.selectedVersion.periods}
                emptyMessage={
                  history.selectedVersion.mode === "inherited"
                    ? t("app:scheduling.inheritedVersion")
                    : undefined
                }
                t={t}
              />
            </Stack>
          </Paper>
        )}
      </Stack>
    </Drawer>
  );
}

function formatInstant(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
