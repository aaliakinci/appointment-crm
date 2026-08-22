import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { Employee } from "@/features/employees/catalog";

import { SchedulingScopeSelector } from "./SchedulingScopeSelector";
import { WeeklyHoursPanel } from "./WeeklyHoursPanel";

interface WeeklyScheduleSectionProps {
  readonly employees: readonly Employee[];
  readonly id: string;
  readonly onDirtyChange: (dirty: boolean) => void;
  readonly onScopeChange: (scope: string) => void;
  readonly scope: string;
  readonly t: (key: string) => string;
}

export function WeeklyScheduleSection({
  employees,
  id,
  onDirtyChange,
  onScopeChange,
  scope,
  t,
}: WeeklyScheduleSectionProps) {
  return (
    <Stack id={id} spacing={2}>
      <Typography id={`${id}.intro`} color="text.secondary">
        {t("app:scheduling.weeklyIntro")}
      </Typography>
      <SchedulingScopeSelector
        id={`${id}.scope`}
        label={t("app:scheduling.scope")}
        scope={scope}
        employees={employees}
        onChange={onScopeChange}
        tenantLabel={t("app:scheduling.tenantScope")}
      />
      <Alert id={`${id}.scopeHelp`} severity="info">
        {scope === "tenant"
          ? t("app:scheduling.weeklyTenantHelp")
          : t("app:scheduling.weeklyEmployeeHelp")}
      </Alert>
      <WeeklyHoursPanel
        key={scope}
        id={`${id}.panel`}
        employeeId={scope === "tenant" ? undefined : scope}
        scopeLabel={weeklyScheduleLabel(scope, employees, t)}
        onDirtyChange={onDirtyChange}
        t={t}
      />
    </Stack>
  );
}

function weeklyScheduleLabel(
  scope: string,
  employees: readonly Employee[],
  t: (key: string) => string,
): string {
  if (scope === "tenant") return t("app:scheduling.tenantScheduleTitle");
  const employeeName =
    employees.find((employee) => employee.id === scope)?.name ?? t("app:scheduling.employee");
  return `${employeeName} · ${t("app:scheduling.employeeScheduleTitle")}`;
}
