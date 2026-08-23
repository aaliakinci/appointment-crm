import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import {
  createLilyFormController,
  useLilyFormStatus,
  type LilyFormBindings,
} from "@lily_platform/lily_ui/ui/forms";
import { useMemo, useState } from "react";

import { useAppTranslation } from "@/i18n";
import { mapApiValidationError } from "@/shared/forms";
import { workspaceLandingPath } from "@/router/authGuards";

import type { TenantOption } from "../api/authContract";
import { useAuth } from "../model/authContext";
import {
  createLoginFormDefinition,
  emptyLoginFormValues,
  type LoginFormValues,
} from "../model/loginForm";

export function useLogin() {
  const navigate = useLilyNavigate();
  const { t } = useAppTranslation();
  const auth = useAuth();
  const [tenantOptions, setTenantOptions] = useState<readonly TenantOption[]>([]);
  const [initialValues, setInitialValues] = useState<LoginFormValues>(emptyLoginFormValues);
  const [initialValuesRevision, setInitialValuesRevision] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const definition = useMemo(() => createLoginFormDefinition(t), [t]);
  const controller = useMemo(() => createLilyFormController<LoginFormValues>(), []);
  const submitting = useLilyFormStatus(controller, (status) => status.isSubmitting);
  const bindings = useMemo<LilyFormBindings<LoginFormValues>>(
    () => ({
      tenantId: {
        visible: tenantOptions.length > 0,
        options: tenantOptions.map((tenant) => ({
          id: tenant.id,
          value: tenant.id,
          label: `${tenant.name} — ${tenant.role}`,
        })),
      },
    }),
    [tenantOptions],
  );

  async function submit(values: LoginFormValues) {
    setError(null);
    if (tenantOptions.length > 0 && values.tenantId.length === 0) {
      return {
        status: "invalid" as const,
        fieldIssues: {
          tenantId: [
            {
              code: "login.tenant_required",
              defaultMessage: t("app:login.tenantRequired"),
            },
          ],
        },
      };
    }

    try {
      const response = await auth.login({
        email: values.email.trim(),
        password: values.password,
        tenantId: values.tenantId || null,
      });
      if (response.requiresTenantSelection) {
        setTenantOptions(response.tenants);
        setInitialValues({
          ...values,
          tenantId: response.tenants[0]?.id ?? "",
        });
        setInitialValuesRevision((revision) => revision + 1);
        return;
      }

      await navigate(workspaceLandingPath(response.activeTenant?.permissions ?? []));
    } catch (submitError) {
      const invalid = mapApiValidationError<LoginFormValues>(submitError, [
        "email",
        "password",
        "tenantId",
      ]);
      if (invalid) {
        return invalid;
      }

      throw submitError;
    }
  }

  return {
    bindings,
    controller,
    definition,
    error,
    initialValues,
    initialValuesRevision,
    submitting,
    submit,
    clearError: () => setError(null),
    handleSubmitError: () => setError(t("app:login.error")),
  };
}
