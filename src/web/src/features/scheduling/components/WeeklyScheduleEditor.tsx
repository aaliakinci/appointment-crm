import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { LilyForm } from "@lily_platform/lily_ui/ui/forms";

import type { useWeeklySchedule } from "../hooks/useWeeklySchedule";
import type { WeeklyHoursFormValues } from "../model/weeklyScheduleForm";
import { PeriodArrayEditor } from "./PeriodArrayEditor";

interface WeeklyScheduleEditorProps {
  readonly employeeId?: string;
  readonly id: string;
  readonly onSubmit: (values: WeeklyHoursFormValues) => Promise<void>;
  readonly t: (key: string) => string;
  readonly weekly: ReturnType<typeof useWeeklySchedule>;
}

export function WeeklyScheduleEditor({
  employeeId,
  id,
  onSubmit,
  t,
  weekly,
}: WeeklyScheduleEditorProps) {
  if (!weekly.canEdit || !weekly.editing || !weekly.schedule) return null;

  return (
    <Stack id={id} spacing={2}>
      <Alert id={`${id}.publishNotice`} severity={employeeId ? "warning" : "info"}>
        {employeeId
          ? t("app:scheduling.employeeScheduleWarning")
          : t("app:scheduling.publishNotice")}
      </Alert>
      <LilyForm
        key={weekly.revision}
        definition={weekly.definition}
        instanceId={`${id}.form.${weekly.revision}`}
        initialValues={weekly.initialValues}
        controller={weekly.controller}
        arrayRenderers={{
          periods: ({ item, index, updateItem }) => (
            <PeriodArrayEditor
              id={`${id}.period.${index}`}
              includeDay
              item={item}
              updateItem={updateItem}
              t={t}
            />
          ),
        }}
        onSubmit={onSubmit}
        onSubmitError={({ error }) => weekly.handleMutationError(error)}
      />
      <Stack id={`${id}.actions`} direction="row" spacing={1}>
        <Button
          id={`${id}.publish`}
          variant="contained"
          loading={weekly.formStatus.isSubmitting}
          disabled={
            !weekly.formStatus.isDirty &&
            weekly.state !== "unconfigured" &&
            weekly.state !== "inherited"
          }
          onClick={() => void weekly.controller.submit()}
        >
          {t("app:scheduling.publishChanges")}
        </Button>
        <Button id={`${id}.cancel`} onClick={weekly.cancelEdit}>
          {t("app:common.close")}
        </Button>
      </Stack>
    </Stack>
  );
}
