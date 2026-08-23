import {
  defineLilyDateTimeForm,
  type LilyDateTimeFormDefinition,
} from "@lily_platform/lily_ui/ui/forms/date-fields";

export interface AppointmentFormValues {
  customerId: string;
  serviceId: string;
  employeeId: string;
  date: string;
  notes: string;
}

export interface RescheduleFormValues {
  date: string;
}

export function createAppointmentFormDefinition(
  t: (key: string) => string,
  date: string,
): LilyDateTimeFormDefinition<AppointmentFormValues> {
  return defineLilyDateTimeForm<AppointmentFormValues>({
    id: "appointments.create",
    defaultValues: { customerId: "", serviceId: "", employeeId: "", date, notes: "" },
    validators: {
      onSubmit: (values) => requiredIssues(values, t),
    },
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "select",
        name: "customerId",
        label: t("app:appointments.customer"),
        required: true,
        options: [],
        fullWidth: true,
      },
      {
        kind: "select",
        name: "serviceId",
        label: t("app:appointments.service"),
        required: true,
        options: [],
        fullWidth: true,
      },
      {
        kind: "select",
        name: "employeeId",
        label: t("app:appointments.employee"),
        required: true,
        options: [],
        fullWidth: true,
      },
      {
        kind: "date",
        name: "date",
        label: `${t("app:appointments.date")} *`,
        invalidText: t("app:appointments.dateInvalid"),
        fullWidth: true,
      },
      {
        kind: "textarea",
        name: "notes",
        label: t("app:appointments.notes"),
        minRows: 2,
        fullWidth: true,
      },
    ],
  });
}

export function createRescheduleFormDefinition(
  t: (key: string) => string,
  date: string,
): LilyDateTimeFormDefinition<RescheduleFormValues> {
  return defineLilyDateTimeForm<RescheduleFormValues>({
    id: "appointments.reschedule",
    defaultValues: { date },
    validators: {
      onSubmit: (values) =>
        isIsoDate(values.date)
          ? undefined
          : {
              fieldIssues: {
                date: [
                  {
                    code: "appointments.date",
                    defaultMessage: t("app:appointments.dateInvalid"),
                  },
                ],
              },
            },
    },
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "date",
        name: "date",
        label: `${t("app:appointments.date")} *`,
        invalidText: t("app:appointments.dateInvalid"),
        fullWidth: true,
      },
    ],
  });
}

function requiredIssues(values: AppointmentFormValues, t: (key: string) => string) {
  const fieldIssues: Record<string, { code: string; defaultMessage: string }[]> = {};
  for (const field of ["customerId", "serviceId", "employeeId"] as const) {
    if (!values[field]) {
      fieldIssues[field] = [
        {
          code: `appointments.${field}`,
          defaultMessage: t("app:appointments.requiredSelection"),
        },
      ];
    }
  }
  if (!isIsoDate(values.date)) {
    fieldIssues.date = [
      { code: "appointments.date", defaultMessage: t("app:appointments.dateInvalid") },
    ];
  }
  if (values.notes.trim().length > 1_000) {
    fieldIssues.notes = [
      { code: "appointments.notes", defaultMessage: t("app:appointments.notesInvalid") },
    ];
  }
  return Object.keys(fieldIssues).length === 0 ? undefined : { fieldIssues };
}

function isIsoDate(value: string): boolean {
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) return false;
  const parsed = new Date(`${value}T00:00:00Z`);
  return !Number.isNaN(parsed.valueOf()) && parsed.toISOString().slice(0, 10) === value;
}
