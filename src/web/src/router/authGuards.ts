import { RouteGuard, type GuardContext, type GuardResult } from "@lily_platform/lily_ui/router";

import type { AppRouterState } from "./routes";

export class AuthenticatedGuard extends RouteGuard<AppRouterState> {
  readonly id = "authenticated";

  canActivate(context: GuardContext<AppRouterState>): GuardResult {
    return context.state?.authentication === "authenticated"
      ? { allow: true }
      : { allow: false, redirectTo: "/login", replace: true, reason: "authentication-required" };
  }
}

export class AnonymousGuard extends RouteGuard<AppRouterState> {
  readonly id = "anonymous";

  canActivate(context: GuardContext<AppRouterState>): GuardResult {
    return context.state?.authentication === "authenticated"
      ? {
          allow: false,
          redirectTo: workspaceLandingPath(context.state.permissions),
          replace: true,
          reason: "already-authenticated",
        }
      : { allow: true };
  }
}

abstract class PermissionGuard extends RouteGuard<AppRouterState> {
  abstract readonly permission: string;

  canActivate(context: GuardContext<AppRouterState>): GuardResult {
    return context.state?.permissions.includes(this.permission)
      ? { allow: true }
      : { allow: false, redirectTo: "/account", replace: true, reason: "permission-required" };
  }
}

export class ReportingReadGuard extends PermissionGuard {
  readonly id = "reporting-read";
  readonly permission = "reporting.read";
}

export class MembershipReadGuard extends PermissionGuard {
  readonly id = "memberships-read";
  readonly permission = "memberships.read";
}

export class CustomerReadGuard extends PermissionGuard {
  readonly id = "customers-read";
  readonly permission = "customers.read";
}

export class ServiceReadGuard extends PermissionGuard {
  readonly id = "services-read";
  readonly permission = "services.read";
}

export class EmployeeReadGuard extends PermissionGuard {
  readonly id = "employees-read";
  readonly permission = "employees.read";
}

export class SchedulingManageGuard extends PermissionGuard {
  readonly id = "scheduling-manage";
  readonly permission = "scheduling.manage";
}

export class AppointmentAccessGuard extends RouteGuard<AppRouterState> {
  readonly id = "appointments-access";

  canActivate(context: GuardContext<AppRouterState>): GuardResult {
    const permissions = context.state?.permissions ?? [];
    return permissions.includes("appointments.read") ||
      permissions.includes("appointments.read-own")
      ? { allow: true }
      : { allow: false, redirectTo: "/account", replace: true, reason: "permission-required" };
  }
}

export function workspaceLandingPath(permissions: readonly string[]): string {
  if (permissions.includes("reporting.read")) return "/dashboard";
  if (permissions.includes("appointments.read") || permissions.includes("appointments.read-own")) {
    return "/appointments";
  }
  return "/account";
}
