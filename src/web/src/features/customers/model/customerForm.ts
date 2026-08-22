import { defineLilyForm, type LilyFormDefinition } from "@lily_platform/lily_ui/ui/forms";

import {
  createFieldValidators,
  isValidContactEmail,
  isValidContactName,
  isValidContactPhone,
  nullableTrimmed,
} from "@/shared/forms";

import type { Customer, CustomerInput } from "../api/customerContract";
import { isValidCustomerNotes } from "./customerValidation";

export interface CustomerFormValues {
  name: string;
  email: string;
  phone: string;
  notes: string;
}

export const emptyCustomerFormValues: CustomerFormValues = {
  name: "",
  email: "",
  phone: "",
  notes: "",
};

export function toCustomerFormValues(customer: Customer | null): CustomerFormValues {
  if (!customer) {
    return emptyCustomerFormValues;
  }

  return {
    name: customer.name,
    email: customer.email ?? "",
    phone: customer.phone ?? "",
    notes: customer.notes ?? "",
  };
}

export function toCustomerInput(values: CustomerFormValues): CustomerInput {
  return {
    name: values.name.trim(),
    email: nullableTrimmed(values.email),
    phone: nullableTrimmed(values.phone),
    notes: nullableTrimmed(values.notes),
  };
}

export function createCustomerFormDefinition(
  t: (key: string) => string,
): LilyFormDefinition<CustomerFormValues> {
  return defineLilyForm<CustomerFormValues>({
    id: "customers.editor",
    defaultValues: emptyCustomerFormValues,
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "text",
        name: "name",
        label: t("app:customers.name"),
        required: true,
        fullWidth: true,
        validators: createFieldValidators(
          isValidContactName,
          "customers.name_invalid",
          t("app:customers.nameValidation"),
        ),
      },
      {
        kind: "email",
        name: "email",
        label: t("app:customers.email"),
        fullWidth: true,
        validators: createFieldValidators(
          isValidContactEmail,
          "customers.email_invalid",
          t("app:customers.emailValidation"),
        ),
      },
      {
        kind: "text",
        name: "phone",
        label: t("app:customers.phone"),
        fullWidth: true,
        validators: createFieldValidators(
          isValidContactPhone,
          "customers.phone_invalid",
          t("app:customers.phoneValidation"),
        ),
      },
      {
        kind: "textarea",
        name: "notes",
        label: t("app:customers.notes"),
        minRows: 3,
        fullWidth: true,
        validators: createFieldValidators(
          isValidCustomerNotes,
          "customers.notes_too_long",
          t("app:customers.notesValidation"),
        ),
      },
    ],
  });
}
