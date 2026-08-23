import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { useState, type FormEvent } from "react";

import { LocalizedLilyDatePicker } from "@/shared/forms/LocalizedLilyDateForm";

import { reportingStatuses } from "../api/reportingContract";
import type { useReportingDashboard } from "../hooks/useReportingDashboard";

interface ReportingFiltersProps {
  readonly dashboard: ReturnType<typeof useReportingDashboard>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function ReportingFilters({ dashboard, id, t }: ReportingFiltersProps) {
  const [rangeError, setRangeError] = useState(false);
  function submit(event: FormEvent<HTMLElement>) {
    event.preventDefault();
    setRangeError(!dashboard.applyDateRange());
  }
  return (
    <Paper id={id} variant="outlined" sx={{ p: 2 }}>
      <Stack id={`${id}.content`} spacing={2}>
        {rangeError && (
          <Alert id={`${id}.rangeError`} severity="error">
            {t("app:reporting.rangeValidation")}
          </Alert>
        )}
        <Stack
          id={`${id}.form`}
          component="form"
          direction={{ xs: "column", lg: "row" }}
          spacing={2}
          onSubmit={submit}
        >
          <LocalizedLilyDatePicker
            id={`${id}.fromDate`}
            label={t("app:reporting.fromDate")}
            value={dashboard.draftFromDate}
            onValueChange={dashboard.setDraftFromDate}
            fullWidth
          />
          <LocalizedLilyDatePicker
            id={`${id}.toDate`}
            label={t("app:reporting.toDate")}
            value={dashboard.draftToDate}
            onValueChange={dashboard.setDraftToDate}
            fullWidth
          />
          <Select
            id={`${id}.employee`}
            label={t("app:appointments.employee")}
            value={dashboard.employeeId}
            options={[
              { id: "all", value: "", label: t("app:common.all") },
              ...dashboard.employees.map((employee) => ({
                id: employee.id,
                value: employee.id,
                label: employee.name,
              })),
            ]}
            onValueChange={(value) => dashboard.setEmployeeId(String(value))}
            sx={{ minWidth: 220 }}
          />
          <Select
            id={`${id}.status`}
            label={t("app:common.status")}
            value={dashboard.status}
            options={[
              { id: "all", value: "", label: t("app:common.all") },
              ...reportingStatuses.map((status) => ({
                id: status,
                value: status,
                label: t(`app:appointments.status.${status}`),
              })),
            ]}
            onValueChange={(value) => dashboard.setStatus(String(value) as typeof dashboard.status)}
            sx={{ minWidth: 190 }}
          />
          <Button id={`${id}.apply`} type="submit" variant="outlined">
            {t("app:common.apply")}
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
}
