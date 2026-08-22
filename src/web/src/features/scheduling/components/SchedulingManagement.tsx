import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Progress } from "@lily_platform/lily_ui/ui/atoms/Progress";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { useAuth } from "@/features/auth/session";
import { WorkspaceShell } from "@/features/auth/workspace";
import { useAppTranslation } from "@/i18n";
import { ManagementPageHeader } from "@/shared/components";

import { useSchedulingCatalogs } from "../hooks/useSchedulingCatalogs";
import { useSchedulingNavigation } from "../hooks/useSchedulingNavigation";
import { useTenantToday } from "../hooks/useTenantToday";
import { AvailabilityPanel } from "./AvailabilityPanel";
import { DateOverridesSection } from "./DateOverridesSection";
import { SchedulingTabs } from "./SchedulingTabs";
import { TimeOffPanel } from "./TimeOffPanel";
import { UnsavedSchedulingChangesDialog } from "./UnsavedSchedulingChangesDialog";
import { WeeklyScheduleSection } from "./WeeklyScheduleSection";

interface SchedulingManagementProps {
  readonly id: string;
}

export function SchedulingManagement({ id }: SchedulingManagementProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const catalogs = useSchedulingCatalogs();
  const navigation = useSchedulingNavigation();
  const timeZone = session?.activeTenant.timeZone ?? "Europe/Istanbul";
  const today = useTenantToday(timeZone);

  return (
    <WorkspaceShell
      id={`${id}.shell`}
      activePath="/scheduling"
      onNavigate={navigation.requestRoute}
    >
      <Stack id={`${id}.content`} spacing={3}>
        <ManagementPageHeader
          id={`${id}.heading`}
          eyebrow={t("app:scheduling.eyebrow")}
          title={t("app:scheduling.title")}
        />
        <Alert id={`${id}.timeZone`} severity="info">
          {t("app:scheduling.timeZoneNotice")} <strong>{timeZone}</strong>
        </Alert>
        {catalogs.loadError && (
          <Alert
            id={`${id}.loadError`}
            severity="error"
            action={
              <Button id={`${id}.retry`} size="small" onClick={catalogs.reload}>
                {t("app:common.retry")}
              </Button>
            }
          >
            {t("app:scheduling.catalogLoadError")}
          </Alert>
        )}
        {catalogs.loading ? (
          <Progress id={`${id}.loading`} />
        ) : (
          <>
            <SchedulingTabs
              id={`${id}.navigation`}
              activeTab={navigation.activeTab}
              onChange={navigation.requestTab}
              t={t}
            />
            <Box
              id={`${id}.navigation.panel.${navigation.activeTab}`}
              role="tabpanel"
              aria-labelledby={`${id}.navigation.tab.${navigation.activeTab}`}
            >
              {navigation.activeTab === "weekly" && (
                <WeeklyScheduleSection
                  id={`${id}.weeklySection`}
                  scope={navigation.weeklyScope}
                  employees={catalogs.employees}
                  onScopeChange={navigation.requestWeeklyScope}
                  onDirtyChange={navigation.setWeeklyDirty}
                  t={t}
                />
              )}
              {navigation.activeTab === "overrides" && (
                <DateOverridesSection
                  id={`${id}.overrideSection`}
                  scope={navigation.overrideScope}
                  employees={catalogs.employees}
                  onScopeChange={navigation.requestOverrideScope}
                  onDirtyChange={navigation.setOverrideDirty}
                  today={today}
                  t={t}
                />
              )}
              {navigation.activeTab === "timeOff" && (
                <TimeOffPanel
                  id={`${id}.timeOff`}
                  employees={catalogs.employees}
                  timeZone={timeZone}
                  today={today}
                  onDirtyChange={navigation.setTimeOffDirty}
                  t={t}
                />
              )}
              {navigation.activeTab === "availability" && (
                <AvailabilityPanel
                  id={`${id}.availability`}
                  employees={catalogs.employees}
                  services={catalogs.services}
                  timeZone={timeZone}
                  today={today}
                  t={t}
                />
              )}
            </Box>
          </>
        )}
      </Stack>
      <UnsavedSchedulingChangesDialog
        id={`${id}.navigationWarning`}
        open={navigation.confirmationOpen}
        onKeepEditing={navigation.keepEditing}
        onDiscard={navigation.discardAndContinue}
        t={t}
      />
    </WorkspaceShell>
  );
}
