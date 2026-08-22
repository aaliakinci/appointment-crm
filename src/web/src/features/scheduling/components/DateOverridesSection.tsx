import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { Employee } from "@/features/employees/catalog";

import { DateOverridesPanel } from "./DateOverridesPanel";
import { SchedulingScopeSelector } from "./SchedulingScopeSelector";

interface DateOverridesSectionProps {
  readonly employees: readonly Employee[];
  readonly id: string;
  readonly onDirtyChange: (dirty: boolean) => void;
  readonly onScopeChange: (scope: string) => void;
  readonly scope: string;
  readonly t: (key: string) => string;
  readonly today: string;
}

export function DateOverridesSection({
  employees,
  id,
  onDirtyChange,
  onScopeChange,
  scope,
  t,
  today,
}: DateOverridesSectionProps) {
  return (
    <Stack id={id} spacing={2}>
      <Typography id={`${id}.intro`} color="text.secondary">
        {t("app:scheduling.overrideIntro")}
      </Typography>
      <SchedulingScopeSelector
        id={`${id}.scope`}
        label={t("app:scheduling.overrideScope")}
        scope={scope}
        employees={employees}
        onChange={onScopeChange}
        tenantLabel={t("app:scheduling.tenantScope")}
      />
      <Alert id={`${id}.scopeHelp`} severity="info">
        {scope === "tenant"
          ? t("app:scheduling.overrideTenantHelp")
          : t("app:scheduling.overrideEmployeeHelp")}
      </Alert>
      <DateOverridesPanel
        key={scope}
        id={`${id}.panel`}
        employeeId={scope === "tenant" ? undefined : scope}
        onDirtyChange={onDirtyChange}
        today={today}
        t={t}
      />
    </Stack>
  );
}
