export type WorkspacePath =
  | "/account"
  | "/audit"
  | "/appointments"
  | "/customers"
  | "/dashboard"
  | "/employees"
  | "/scheduling"
  | "/services"
  | "/team";

export interface WorkspaceNavigationItem {
  readonly path: WorkspacePath;
  readonly labelKey: string;
}

export function workspaceNavigationFor(
  permissions: readonly string[],
): readonly WorkspaceNavigationItem[] {
  const allowed = new Set(permissions);
  return [
    allowed.has("reporting.read")
      ? { path: "/dashboard" as const, labelKey: "app:navigation.dashboard" }
      : null,
    allowed.has("customers.read")
      ? { path: "/customers" as const, labelKey: "app:navigation.customers" }
      : null,
    allowed.has("services.read")
      ? { path: "/services" as const, labelKey: "app:navigation.services" }
      : null,
    allowed.has("employees.read")
      ? { path: "/employees" as const, labelKey: "app:navigation.employees" }
      : null,
    allowed.has("appointments.read") || allowed.has("appointments.read-own")
      ? { path: "/appointments" as const, labelKey: "app:navigation.appointments" }
      : null,
    allowed.has("scheduling.manage")
      ? { path: "/scheduling" as const, labelKey: "app:navigation.scheduling" }
      : null,
    allowed.has("memberships.read")
      ? { path: "/team" as const, labelKey: "app:navigation.team" }
      : null,
    allowed.has("reporting.read")
      ? { path: "/audit" as const, labelKey: "app:navigation.audit" }
      : null,
    { path: "/account" as const, labelKey: "app:navigation.account" },
  ].filter((item) => item !== null);
}
