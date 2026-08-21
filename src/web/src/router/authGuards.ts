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
