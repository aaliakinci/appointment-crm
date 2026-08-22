import { defineLilyForm, type LilyFormDefinition } from "@lily_platform/lily_ui/ui/forms";

import { createFieldValidators, isValidContactName } from "@/shared/forms";

import type { ServiceInput, ServiceOffering } from "../api/serviceContract";
import { isValidServiceDuration, isValidServicePrice } from "./serviceValidation";

export interface ServiceFormValues {
  name: string;
  durationMinutes: number | null;
  price: number | null;
  currency: string;
}

export function emptyServiceFormValues(currency: string): ServiceFormValues {
  return { name: "", durationMinutes: 30, price: 0, currency };
}

export function toServiceFormValues(
  service: ServiceOffering | null,
  currency: string,
): ServiceFormValues {
  return service
    ? {
        name: service.name,
        durationMinutes: service.durationMinutes,
        price: service.price,
        currency: service.currency,
      }
    : emptyServiceFormValues(currency);
}

export function toServiceInput(values: ServiceFormValues): ServiceInput {
  return {
    name: values.name.trim(),
    durationMinutes: values.durationMinutes ?? 0,
    price: values.price ?? -1,
    currency: values.currency,
  };
}

export function createServiceFormDefinition(
  t: (key: string) => string,
  currency: string,
): LilyFormDefinition<ServiceFormValues> {
  return defineLilyForm<ServiceFormValues>({
    id: "services.editor",
    defaultValues: emptyServiceFormValues(currency),
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "text",
        name: "name",
        label: t("app:services.name"),
        required: true,
        fullWidth: true,
        validators: createFieldValidators(
          isValidContactName,
          "services.name_invalid",
          t("app:services.nameValidation"),
        ),
      },
      {
        kind: "number",
        name: "durationMinutes",
        label: t("app:services.duration"),
        required: true,
        fullWidth: true,
        inputProps: { min: 5, max: 480, step: 5 },
        validators: createFieldValidators(
          isValidServiceDuration,
          "services.duration_invalid",
          t("app:services.durationValidation"),
        ),
      },
      {
        kind: "number",
        name: "price",
        label: t("app:services.price"),
        required: true,
        fullWidth: true,
        inputProps: { min: 0, max: 1_000_000, step: 0.01 },
        validators: createFieldValidators(
          isValidServicePrice,
          "services.price_invalid",
          t("app:services.priceValidation"),
        ),
      },
      {
        kind: "text",
        name: "currency",
        label: t("app:services.currency"),
        fullWidth: true,
      },
    ],
  });
}
