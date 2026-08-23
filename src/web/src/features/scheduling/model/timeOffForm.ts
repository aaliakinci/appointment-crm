import {
  defineLilyDateTimeForm,
  type LilyDateTimeFormDefinition,
} from "@lily_platform/lily_ui/ui/forms/date-fields";

import { createFieldValidators } from "@/shared/forms";

import { isIsoDate } from "./localDate";
import { isTime } from "./schedulePeriod";

export interface TimeOffFormValues {
  employeeId: string;
  startDate: string;
  startTime: string;
  endDate: string;
  endTime: string;
  reason: string;
}

export function createTimeOffDefinition(
  t: (key: string) => string,
  date: string,
): LilyDateTimeFormDefinition<TimeOffFormValues> {
  const dateValidators = createFieldValidators(
    isIsoDate,
    "scheduling.date",
    t("app:scheduling.dateInvalid"),
  );
  const timeValidators = createFieldValidators(
    isTime,
    "scheduling.time",
    t("app:scheduling.timeInvalid"),
  );
  return defineLilyDateTimeForm<TimeOffFormValues>({
    id: "scheduling.time-off",
    defaultValues: {
      employeeId: "",
      startDate: date,
      startTime: "09:00",
      endDate: date,
      endTime: "10:00",
      reason: "",
    },
    validators: {
      onSubmit: (values) =>
        values.employeeId
          ? undefined
          : {
              fieldIssues: {
                employeeId: [
                  {
                    code: "scheduling.employee",
                    defaultMessage: t("app:scheduling.employeeRequired"),
                  },
                ],
              },
            },
    },
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "select",
        name: "employeeId",
        label: t("app:scheduling.employee"),
        required: true,
        options: [],
        fullWidth: true,
      },
      {
        kind: "date",
        name: "startDate",
        label: `${t("app:scheduling.startDate")} *`,
        invalidText: t("app:scheduling.dateInvalid"),
        validators: dateValidators,
        fullWidth: true,
      },
      {
        kind: "text",
        name: "startTime",
        label: t("app:scheduling.startTime"),
        required: true,
        helperText: "HH:mm",
        validators: timeValidators,
        fullWidth: true,
      },
      {
        kind: "date",
        name: "endDate",
        label: `${t("app:scheduling.endDate")} *`,
        invalidText: t("app:scheduling.dateInvalid"),
        validators: dateValidators,
        fullWidth: true,
      },
      {
        kind: "text",
        name: "endTime",
        label: t("app:scheduling.endTime"),
        required: true,
        helperText: "HH:mm",
        validators: timeValidators,
        fullWidth: true,
      },
      {
        kind: "text",
        name: "reason",
        label: t("app:scheduling.reason"),
        fullWidth: true,
      },
    ],
  });
}
