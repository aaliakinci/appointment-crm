import {
  defineLilyForm,
  type LilyFormDefinition,
  type LilyValidationIssue,
} from "@lily_platform/lily_ui/ui/forms";

import { hasLoginPassword, isValidLoginEmail } from "./loginValidation";

export interface LoginFormValues {
  email: string;
  password: string;
  tenantId: string;
}

export const emptyLoginFormValues: LoginFormValues = {
  email: "",
  password: "",
  tenantId: "",
};

export function createLoginFormDefinition(
  t: (key: string) => string,
): LilyFormDefinition<LoginFormValues> {
  const invalidEmail = (): LilyValidationIssue => ({
    code: "login.email_invalid",
    defaultMessage: t("app:login.emailValidation"),
  });
  const passwordRequired = (): LilyValidationIssue => ({
    code: "login.password_required",
    defaultMessage: t("app:login.passwordValidation"),
  });

  return defineLilyForm<LoginFormValues>({
    id: "login.form",
    defaultValues: emptyLoginFormValues,
    containerProps: { spacing: 2.5 },
    fields: [
      {
        kind: "email",
        name: "email",
        label: t("app:login.email"),
        autoComplete: "username",
        required: true,
        fullWidth: true,
        validators: {
          onBlur: (value) => (isValidLoginEmail(value) ? undefined : invalidEmail()),
          onSubmit: (value) => (isValidLoginEmail(value) ? undefined : invalidEmail()),
        },
      },
      {
        kind: "password",
        name: "password",
        label: t("app:login.password"),
        autoComplete: "current-password",
        required: true,
        fullWidth: true,
        validators: {
          onBlur: (value) => (hasLoginPassword(value) ? undefined : passwordRequired()),
          onSubmit: (value) => (hasLoginPassword(value) ? undefined : passwordRequired()),
        },
      },
      {
        kind: "select",
        name: "tenantId",
        label: t("app:login.tenant"),
        options: [],
        required: true,
        fullWidth: true,
      },
    ],
    actions: [
      {
        id: "submit",
        kind: "submit",
        label: t("app:login.submit"),
        variant: "contained",
      },
    ],
  });
}
