import type { LilyPageComponent } from "@lily_platform/lily_ui";
import { createLilyRouterKit } from "@lily_platform/lily_ui/router";
import { createElement, lazy, Suspense } from "react";

import { SystemStatusPage } from "@/pages/SystemStatusPage";

import {
  AnonymousGuard,
  AuthenticatedGuard,
  CustomerReadGuard,
  EmployeeReadGuard,
  ServiceReadGuard,
  SchedulingManageGuard,
} from "./authGuards";

export interface AppRouterState {
  readonly authentication: "anonymous" | "authenticated";
  readonly permissions: readonly string[];
}

const routerKit = createLilyRouterKit<AppRouterState>();

export const appGuardRegistry = routerKit.createGuardRegistry();
appGuardRegistry.register(AuthenticatedGuard);
appGuardRegistry.register(AnonymousGuard);
appGuardRegistry.register(CustomerReadGuard);
appGuardRegistry.register(ServiceReadGuard);
appGuardRegistry.register(EmployeeReadGuard);
appGuardRegistry.register(SchedulingManageGuard);

const systemStatusPage: LilyPageComponent = (props) => createElement(SystemStatusPage, props);
const LazyLoginPage = lazy(async () => ({
  default: (await import("@/pages/LoginPage")).LoginPage,
}));
const LazyAccountPage = lazy(async () => ({
  default: (await import("@/pages/AccountPage")).AccountPage,
}));
const LazyCustomersPage = lazy(async () => ({
  default: (await import("@/pages/CustomersPage")).CustomersPage,
}));
const LazyServicesPage = lazy(async () => ({
  default: (await import("@/pages/ServicesPage")).ServicesPage,
}));
const LazyEmployeesPage = lazy(async () => ({
  default: (await import("@/pages/EmployeesPage")).EmployeesPage,
}));
const LazySchedulingPage = lazy(async () => ({
  default: (await import("@/pages/SchedulingPage")).SchedulingPage,
}));
const loginPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyLoginPage, props));
const accountPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyAccountPage, props));
const customersPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyCustomersPage, props));
const servicesPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyServicesPage, props));
const employeesPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyEmployeesPage, props));
const schedulingPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazySchedulingPage, props));

export const APP_ROUTES = routerKit.createRoutes([
  { id: "system-status", path: "/", page: systemStatusPage },
  { id: "login", path: "/login", page: loginPage, guards: [AnonymousGuard] },
  { id: "account", path: "/account", page: accountPage, guards: [AuthenticatedGuard] },
  {
    id: "customers",
    path: "/customers",
    page: customersPage,
    guards: [AuthenticatedGuard, CustomerReadGuard],
  },
  {
    id: "services",
    path: "/services",
    page: servicesPage,
    guards: [AuthenticatedGuard, ServiceReadGuard],
  },
  {
    id: "employees",
    path: "/employees",
    page: employeesPage,
    guards: [AuthenticatedGuard, EmployeeReadGuard],
  },
  {
    id: "scheduling",
    path: "/scheduling",
    page: schedulingPage,
    guards: [AuthenticatedGuard, SchedulingManageGuard],
  },
]);
