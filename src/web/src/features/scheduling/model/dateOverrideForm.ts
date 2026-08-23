import {
  defineLilyDateTimeForm,
  type LilyDateTimeFormDefinition,
} from "@lily_platform/lily_ui/ui/forms/date-fields";

import { isIsoDate } from "./localDate";
import { emptyPeriod, validatePeriods, type EditablePeriod } from "./schedulePeriod";

export interface DateOverrideFormValues {
  date: string;
  isClosed: boolean;
  periods: EditablePeriod[];
}

export function createDateOverrideDefinition(
  t: (key: string) => string,
  date: string,
  isClosed: boolean,
): LilyDateTimeFormDefinition<DateOverrideFormValues> {
  return defineLilyDateTimeForm<DateOverrideFormValues>({
    id: "scheduling.date-override",
    defaultValues: {
      date,
      isClosed,
      periods: isClosed ? [] : [{ ...emptyPeriod, dayOfWeek: "0" }],
    },
    validators: {
      onSubmit: (values) => {
        if (!isIsoDate(values.date)) {
          return {
            fieldIssues: {
              date: [{ code: "scheduling.date", defaultMessage: t("app:scheduling.dateInvalid") }],
            },
          };
        }

        if (!values.isClosed && values.periods.length === 0) {
          return {
            formIssues: [
              {
                code: "scheduling.open-period",
                defaultMessage: t("app:scheduling.openPeriodRequired"),
              },
            ],
          };
        }

        return values.isClosed ? undefined : validatePeriods(values.periods, false, t);
      },
    },
    fields: {
      date: {
        kind: "date",
        label: `${t("app:scheduling.date")} *`,
        invalidText: t("app:scheduling.dateInvalid"),
        fullWidth: true,
      },
    },
    content: [
      { kind: "field", name: "date" },
      ...(isClosed
        ? []
        : [
            {
              kind: "array" as const,
              id: "date-periods",
              name: "periods" as const,
              minItems: 0,
              maxItems: 12,
              createItem: () => ({ ...emptyPeriod, dayOfWeek: "0" }),
              addLabel: t("app:scheduling.addPeriod"),
              removeLabel: t("app:scheduling.removePeriod"),
            },
          ]),
    ],
  });
}
