import { defineLilyForm, type LilyFormDefinition } from "@lily_platform/lily_ui/ui/forms";

import { emptyPeriod, validatePeriods, type EditablePeriod } from "./schedulePeriod";

export interface WeeklyHoursFormValues {
  periods: EditablePeriod[];
  changeNote: string;
}

export function createWeeklyHoursDefinition(
  t: (key: string) => string,
): LilyFormDefinition<WeeklyHoursFormValues> {
  return defineLilyForm<WeeklyHoursFormValues>({
    id: "scheduling.weekly-hours",
    defaultValues: { periods: [{ ...emptyPeriod }], changeNote: "" },
    validators: {
      onSubmit: (values) =>
        values.changeNote.length > 500
          ? {
              fieldIssues: {
                changeNote: [
                  {
                    code: "scheduling.change-note",
                    defaultMessage: t("app:scheduling.changeNoteInvalid"),
                  },
                ],
              },
            }
          : validatePeriods(values.periods, true, t),
    },
    fields: {
      changeNote: {
        kind: "textarea",
        label: t("app:scheduling.changeNote"),
        helperText: t("app:scheduling.changeNoteHelp"),
        fullWidth: true,
      },
    },
    content: [
      {
        kind: "array",
        id: "weekly-periods",
        name: "periods",
        minItems: 0,
        maxItems: 42,
        createItem: () => ({ ...emptyPeriod }),
        addLabel: t("app:scheduling.addPeriod"),
        removeLabel: t("app:scheduling.removePeriod"),
      },
      { kind: "field", name: "changeNote" },
    ],
  });
}
