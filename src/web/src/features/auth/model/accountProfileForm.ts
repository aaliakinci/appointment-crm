import { defineLilyForm, type LilyFormDefinition } from "@lily_platform/lily_ui/ui/forms";

import { createFieldValidators, isValidContactName } from "@/shared/forms";

import type { AccountProfile } from "../api/accountContract";

export interface AccountProfileFormValues {
  displayName: string;
}

export function createAccountProfileFormDefinition(
  t: (key: string) => string,
): LilyFormDefinition<AccountProfileFormValues> {
  return defineLilyForm<AccountProfileFormValues>({
    id: "account.profile",
    defaultValues: { displayName: "" },
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "text",
        name: "displayName",
        label: t("app:account.displayName"),
        required: true,
        fullWidth: true,
        validators: createFieldValidators(
          isValidContactName,
          "account.display_name_invalid",
          t("app:account.displayNameValidation"),
        ),
      },
    ],
  });
}

export function toAccountProfileFormValues(
  profile: AccountProfile | null,
): AccountProfileFormValues {
  return { displayName: profile?.displayName ?? "" };
}
