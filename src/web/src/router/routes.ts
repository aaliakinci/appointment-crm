import type { LilyPageComponent } from "@lily_platform/lily_ui";
import { createLilyRouterKit } from "@lily_platform/lily_ui/router";
import { createElement, lazy, Suspense } from "react";

import { SystemStatusPage } from "@/pages/SystemStatusPage";

import { AnonymousGuard, AuthenticatedGuard } from "./authGuards";

export interface AppRouterState {
  readonly authentication: "anonymous" | "authenticated";
}

const routerKit = createLilyRouterKit<AppRouterState>();

export const appGuardRegistry = routerKit.createGuardRegistry();
appGuardRegistry.register(AuthenticatedGuard);
appGuardRegistry.register(AnonymousGuard);

const systemStatusPage: LilyPageComponent = (props) => createElement(SystemStatusPage, props);
const LazyLoginPage = lazy(async () => ({
  default: (await import("@/pages/LoginPage")).LoginPage,
}));
const LazyAccountPage = lazy(async () => ({
  default: (await import("@/pages/AccountPage")).AccountPage,
}));
const loginPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyLoginPage, props));
const accountPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyAccountPage, props));

export const APP_ROUTES = routerKit.createRoutes([
  { id: "system-status", path: "/", page: systemStatusPage },
  { id: "login", path: "/login", page: loginPage, guards: [AnonymousGuard] },
  { id: "account", path: "/account", page: accountPage, guards: [AuthenticatedGuard] },
]);
