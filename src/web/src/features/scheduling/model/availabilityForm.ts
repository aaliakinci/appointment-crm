import { defineLilyForm, type LilyFormDefinition } from "@lily_platform/lily_ui/ui/forms";

import { isIsoDate } from "./localDate";

export interface AvailabilityFormValues {
  date: string;
  employeeId: string;
  serviceId: string;
}

export function createAvailabilityDefinition(
  t: (key: string) => string,
  date: string,
): LilyFormDefinition<AvailabilityFormValues> {
  return defineLilyForm<AvailabilityFormValues>({
    id: "scheduling.availability",
    defaultValues: { date, employeeId: "", serviceId: "" },
    validators: {
      onSubmit: (values) => {
        const fieldIssues: Record<string, { code: string; defaultMessage: string }[]> = {};
        if (!isIsoDate(values.date)) {
          fieldIssues.date = [
            { code: "scheduling.date", defaultMessage: t("app:scheduling.dateInvalid") },
          ];
        }
        if (!values.employeeId) {
          fieldIssues.employeeId = [
            {
              code: "scheduling.employee",
              defaultMessage: t("app:scheduling.employeeRequired"),
            },
          ];
        }
        if (!values.serviceId) {
          fieldIssues.serviceId = [
            {
              code: "scheduling.service",
              defaultMessage: t("app:scheduling.serviceRequired"),
            },
          ];
        }
        return Object.keys(fieldIssues).length === 0 ? undefined : { fieldIssues };
      },
    },
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "text",
        name: "date",
        label: t("app:scheduling.date"),
        required: true,
        helperText: "YYYY-MM-DD",
        fullWidth: true,
      },
      {
        kind: "select",
        name: "employeeId",
        label: t("app:scheduling.employee"),
        required: true,
        options: [],
        fullWidth: true,
      },
      {
        kind: "select",
        name: "serviceId",
        label: t("app:scheduling.service"),
        required: true,
        options: [],
        fullWidth: true,
      },
    ],
  });
}
