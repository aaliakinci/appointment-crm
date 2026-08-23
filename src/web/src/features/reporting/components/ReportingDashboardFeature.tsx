import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Progress } from "@lily_platform/lily_ui/ui/atoms/Progress";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { useAuth } from "@/features/auth/session";
import { WorkspaceShell } from "@/features/auth/workspace";
import { useAppTranslation } from "@/i18n";
import { ManagementPageHeader } from "@/shared/components";

import { useReportingDashboard } from "../hooks/useReportingDashboard";
import { ReportingDailyChart } from "./ReportingDailyChart";
import { ReportingEmployeeTable } from "./ReportingEmployeeTable";
import { ReportingFilters } from "./ReportingFilters";
import { ReportingStatusChart } from "./ReportingStatusChart";
import { ReportingSummaryCards } from "./ReportingSummaryCards";

interface ReportingDashboardFeatureProps {
  readonly id: string;
}

export function ReportingDashboardFeature({ id }: ReportingDashboardFeatureProps) {
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const today = tenantToday(session?.activeTenant.timeZone ?? "UTC");
  const reporting = useReportingDashboard({ today });
  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/dashboard">
      <Stack id={`${id}.content`} spacing={3}>
        <ManagementPageHeader
          id={`${id}.heading`}
          eyebrow={t("app:reporting.eyebrow")}
          title={t("app:reporting.title")}
        />
        <ReportingFilters id={`${id}.filters`} dashboard={reporting} t={t} />
        {reporting.loadError && (
          <Alert
            id={`${id}.error`}
            severity="error"
            action={
              <Button id={`${id}.retry`} size="small" onClick={reporting.reload}>
                {t("app:common.retry")}
              </Button>
            }
          >
            {t("app:reporting.loadError")}
          </Alert>
        )}
        {reporting.loading && <Progress id={`${id}.loading`} />}
        {reporting.dashboard && (
          <>
            <ReportingSummaryCards id={`${id}.summary`} dashboard={reporting.dashboard} t={t} />
            <Stack id={`${id}.charts`} direction={{ xs: "column", lg: "row" }} spacing={2}>
              <ReportingStatusChart
                id={`${id}.statusChart`}
                items={reporting.dashboard.byStatus}
                t={t}
              />
              <ReportingDailyChart
                id={`${id}.dailyChart`}
                items={reporting.dashboard.byDay}
                t={t}
              />
            </Stack>
            <ReportingEmployeeTable
              id={`${id}.employeeTable`}
              currency={reporting.dashboard.currency}
              items={reporting.dashboard.byEmployee}
              t={t}
            />
          </>
        )}
      </Stack>
    </WorkspaceShell>
  );
}

function tenantToday(timeZone: string): string {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date());
  const value = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  return `${value.year}-${value.month}-${value.day}`;
}
