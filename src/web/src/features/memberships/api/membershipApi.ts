import { appHttpClient } from "@/shared/api";

import {
  decodeMembership,
  decodeMembershipReport,
  decodeMemberships,
  type Membership,
  type MembershipReport,
  type TenantRole,
} from "./membershipContract";

export function listMemberships(signal?: AbortSignal): Promise<readonly Membership[]> {
  return appHttpClient.getData<readonly Membership[]>("/api/v1/memberships", {
    signal,
    decode: decodeMemberships,
    metadata: { operationName: "memberships.list" },
  });
}

export function getMembershipReport(signal?: AbortSignal): Promise<MembershipReport> {
  return appHttpClient.getData<MembershipReport>("/api/v1/memberships/report", {
    signal,
    decode: decodeMembershipReport,
    metadata: { operationName: "memberships.report" },
  });
}

export function updateMembership(
  membershipId: string,
  role: TenantRole,
  isActive: boolean,
): Promise<Membership> {
  return appHttpClient.patchData<
    Membership,
    { readonly role: TenantRole; readonly isActive: boolean }
  >(
    `/api/v1/memberships/${encodeURIComponent(membershipId)}`,
    { role, isActive },
    {
      decode: decodeMembership,
      metadata: { operationName: "memberships.update", replay: "deny" },
    },
  );
}
