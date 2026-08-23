import { defineLilyForm, type LilyFormDefinition } from "@lily_platform/lily_ui/ui/forms";

import type { Membership, TenantRole } from "../api/membershipContract";

export interface MembershipFormValues {
  role: TenantRole;
}

export function createMembershipFormDefinition(
  t: (key: string) => string,
): LilyFormDefinition<MembershipFormValues> {
  return defineLilyForm<MembershipFormValues>({
    id: "memberships.editor",
    defaultValues: { role: "Employee" },
    containerProps: { spacing: 2 },
    fields: [
      {
        kind: "select",
        name: "role",
        label: t("app:memberships.role"),
        required: true,
        fullWidth: true,
        options: [],
      },
    ],
  });
}

export function toMembershipFormValues(membership: Membership): MembershipFormValues {
  return { role: membership.role };
}
