import {
  nullableString,
  requireArray,
  requireBoolean,
  requireRecord,
  requireString,
} from "@/shared/api/contractDecoder";

export interface TenantOption {
  readonly id: string;
  readonly name: string;
  readonly slug: string;
  readonly role: string;
}

export interface AuthenticatedUser {
  readonly id: string;
  readonly email: string;
  readonly displayName: string;
}

export interface ActiveTenant {
  readonly id: string;
  readonly name: string;
  readonly slug: string;
  readonly currency: string;
  readonly timeZone: string;
  readonly role: string;
  readonly permissions: readonly string[];
}

export interface AuthenticationResponse {
  readonly requiresTenantSelection: boolean;
  readonly accessToken: string | null;
  readonly accessTokenExpiresAtUtc: string | null;
  readonly user: AuthenticatedUser | null;
  readonly activeTenant: ActiveTenant | null;
  readonly tenants: readonly TenantOption[];
}

export interface LoginRequest {
  readonly email: string;
  readonly password: string;
  readonly tenantId: string | null;
}

export function decodeAuthenticationResponse(body: unknown): AuthenticationResponse {
  const value = requireRecord(body, "authentication response");
  const requiresTenantSelection = requireBoolean(
    value.requiresTenantSelection,
    "requiresTenantSelection",
  );
  const tenants = requireArray(value.tenants, "tenants").map(decodeTenantOption);
  const accessToken = nullableString(value.accessToken, "accessToken");
  const accessTokenExpiresAtUtc = nullableString(
    value.accessTokenExpiresAtUtc,
    "accessTokenExpiresAtUtc",
  );
  const user = value.user === null ? null : decodeUser(value.user);
  const activeTenant = value.activeTenant === null ? null : decodeActiveTenant(value.activeTenant);

  if (
    !requiresTenantSelection &&
    (!accessToken || !accessTokenExpiresAtUtc || !user || !activeTenant)
  ) {
    throw new TypeError("Authenticated response is missing its session fields.");
  }

  return {
    requiresTenantSelection,
    accessToken,
    accessTokenExpiresAtUtc,
    user,
    activeTenant,
    tenants,
  };
}

export function decodeTenantOptions(body: unknown): readonly TenantOption[] {
  return requireArray(body, "tenant options").map(decodeTenantOption);
}

function decodeTenantOption(body: unknown): TenantOption {
  const value = requireRecord(body, "tenant option");
  return {
    id: requireString(value.id, "id"),
    name: requireString(value.name, "name"),
    slug: requireString(value.slug, "slug"),
    role: requireString(value.role, "role"),
  };
}

function decodeUser(body: unknown): AuthenticatedUser {
  const value = requireRecord(body, "authenticated user");
  return {
    id: requireString(value.id, "user.id"),
    email: requireString(value.email, "user.email"),
    displayName: requireString(value.displayName, "user.displayName"),
  };
}

function decodeActiveTenant(body: unknown): ActiveTenant {
  const value = requireRecord(body, "activeTenant");
  return {
    id: requireString(value.id, "activeTenant.id"),
    name: requireString(value.name, "activeTenant.name"),
    slug: requireString(value.slug, "activeTenant.slug"),
    currency: requireString(value.currency, "activeTenant.currency"),
    timeZone: requireString(value.timeZone, "activeTenant.timeZone"),
    role: requireString(value.role, "activeTenant.role"),
    permissions: requireArray(value.permissions, "activeTenant.permissions").map((permission) =>
      requireString(permission, "permission"),
    ),
  };
}
