import {
  nullableString,
  requireArray,
  requireBoolean,
  requireRecord,
  requireString,
} from "@/shared/api/contractDecoder";

export interface AccountProfile {
  readonly userId: string;
  readonly email: string;
  readonly displayName: string;
  readonly updatedAtUtc: string;
}

export interface AccountSession {
  readonly id: string;
  readonly tenantName: string;
  readonly createdAtUtc: string;
  readonly lastUsedAtUtc: string | null;
  readonly expiresAtUtc: string;
  readonly isCurrent: boolean;
}

export function decodeAccountProfile(body: unknown): AccountProfile {
  const value = requireRecord(body, "account profile");
  return {
    userId: requireString(value.userId, "profile.userId"),
    email: requireString(value.email, "profile.email"),
    displayName: requireString(value.displayName, "profile.displayName"),
    updatedAtUtc: requireString(value.updatedAtUtc, "profile.updatedAtUtc"),
  };
}

export function decodeAccountSessions(body: unknown): readonly AccountSession[] {
  return requireArray(body, "account sessions").map((item) => {
    const value = requireRecord(item, "account session");
    return {
      id: requireString(value.id, "session.id"),
      tenantName: requireString(value.tenantName, "session.tenantName"),
      createdAtUtc: requireString(value.createdAtUtc, "session.createdAtUtc"),
      lastUsedAtUtc: nullableString(value.lastUsedAtUtc, "session.lastUsedAtUtc"),
      expiresAtUtc: requireString(value.expiresAtUtc, "session.expiresAtUtc"),
      isCurrent: requireBoolean(value.isCurrent, "session.isCurrent"),
    };
  });
}
