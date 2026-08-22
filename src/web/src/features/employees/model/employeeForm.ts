import { defineLilyForm, type LilyFormDefinition } from "@lily_platform/lily_ui/ui/forms";

import {
  createFieldValidators,
  isValidContactEmail,
  isValidContactName,
  isValidContactPhone,
  nullableTrimmed,
} from "@/shared/forms";

import type { CreateEmployeeInput, Employee, EmployeeInput } from "../api/employeeContract";

export interface EmployeeFormValues {
  userId: string;
  name: string;
  email: string;
  phone: string;
  serviceIds: string[];
}

export const emptyEmployeeFormValues: EmployeeFormValues = {
  userId: "",
  name: "",
  email: "",
  phone: "",
  serviceIds: [],
};

export function toEmployeeFormValues(employee: Employee | null): EmployeeFormValues {
  if (!employee) {
    return emptyEmployeeFormValues;
  }

  return {
    userId: employee.userId ?? "",
    name: employee.name,
    email: employee.email ?? "",
    phone: employee.phone ?? "",
    serviceIds: employee.services
      .filter((service) => service.isActive)
      .map((service) => service.id),
  };
}

export function toEmployeeInput(values: EmployeeFormValues): EmployeeInput {
  return {
    userId: nullableTrimmed(values.userId),
    name: values.name.trim(),
    email: nullableTrimmed(values.email),
    phone: nullableTrimmed(values.phone),
  };
}

export function toCreateEmployeeInput(values: EmployeeFormValues): CreateEmployeeInput {
  return { ...toEmployeeInput(values), serviceIds: values.serviceIds };
}

export function createEmployeeFormDefinition(
  t: (key: string) => string,
): LilyFormDefinition<EmployeeFormValues> {
  return defineLilyForm<EmployeeFormValues>({
    id: "employees.editor",
    defaultValues: emptyEmployeeFormValues,
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "text",
        name: "name",
        label: t("app:employees.name"),
        required: true,
        fullWidth: true,
        validators: createFieldValidators(
          isValidContactName,
          "employees.name_invalid",
          t("app:employees.nameValidation"),
        ),
      },
      {
        kind: "email",
        name: "email",
        label: t("app:employees.email"),
        fullWidth: true,
        validators: createFieldValidators(
          isValidContactEmail,
          "employees.email_invalid",
          t("app:employees.emailValidation"),
        ),
      },
      {
        kind: "text",
        name: "phone",
        label: t("app:employees.phone"),
        fullWidth: true,
        validators: createFieldValidators(
          isValidContactPhone,
          "employees.phone_invalid",
          t("app:employees.phoneValidation"),
        ),
      },
      {
        kind: "select",
        name: "userId",
        label: t("app:employees.user"),
        options: [],
        fullWidth: true,
      },
      {
        kind: "multiselect",
        name: "serviceIds",
        label: t("app:employees.services"),
        options: [],
        fullWidth: true,
      },
    ],
  });
}
