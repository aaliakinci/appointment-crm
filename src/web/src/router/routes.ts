import type { LilyPageComponent } from "@lily_platform/lily_ui";
import { createLilyRouterKit } from "@lily_platform/lily_ui/router";
import { createElement } from "react";

import { LoginPage } from "@/pages/LoginPage";
import { SystemStatusPage } from "@/pages/SystemStatusPage";

export interface AppRouterState {
  readonly authentication: "anonymous";
}

export const APP_ROUTER_STATE = {
  authentication: "anonymous",
} as const satisfies AppRouterState;

const routerKit = createLilyRouterKit<AppRouterState>();

export const appGuardRegistry = routerKit.createGuardRegistry();

const systemStatusPage: LilyPageComponent = (props) => createElement(SystemStatusPage, props);
const loginPage: LilyPageComponent = (props) => createElement(LoginPage, props);

export const APP_ROUTES = routerKit.createRoutes([
  { id: "system-status", path: "/", page: systemStatusPage },
  { id: "login", path: "/login", page: loginPage },
]);
