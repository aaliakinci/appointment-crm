import {
  requireArray,
  requireBoolean,
  requireNumber,
  requireRecord,
  requireString,
} from "@/shared/api/contractDecoder";

export const tenantRoles = ["Owner", "Manager", "Receptionist", "Employee"] as const;
export type TenantRole = (typeof tenantRoles)[number];

export interface Membership {
  readonly id: string;
  readonly userId: string;
  readonly email: string;
  readonly displayName: string;
  readonly role: TenantRole;
  readonly isActive: boolean;
  readonly updatedAtUtc: string;
}

export interface MembershipReport {
  readonly total: number;
  readonly active: number;
  readonly byRole: Readonly<Record<string, number>>;
}

export function decodeMemberships(body: unknown): readonly Membership[] {
  return requireArray(body, "memberships").map(decodeMembership);
}

export function decodeMembership(body: unknown): Membership {
  const value = requireRecord(body, "membership");
  const role = requireString(value.role, "membership.role");
  if (!tenantRoles.includes(role as TenantRole)) {
    throw new TypeError("membership.role is not recognized.");
  }
  return {
    id: requireString(value.id, "membership.id"),
    userId: requireString(value.userId, "membership.userId"),
    email: requireString(value.email, "membership.email"),
    displayName: requireString(value.displayName, "membership.displayName"),
    role: role as TenantRole,
    isActive: requireBoolean(value.isActive, "membership.isActive"),
    updatedAtUtc: requireString(value.updatedAtUtc, "membership.updatedAtUtc"),
  };
}

export function decodeMembershipReport(body: unknown): MembershipReport {
  const value = requireRecord(body, "membership report");
  const byRole = requireRecord(value.byRole, "membershipReport.byRole");
  return {
    total: requireNumber(value.total, "membershipReport.total"),
    active: requireNumber(value.active, "membershipReport.active"),
    byRole: Object.fromEntries(
      Object.entries(byRole).map(([role, count]) => [
        role,
        requireNumber(count, `membershipReport.byRole.${role}`),
      ]),
    ),
  };
}
