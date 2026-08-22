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
      ? { allow: false, redirectTo: "/account", replace: true, reason: "already-authenticated" }
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
