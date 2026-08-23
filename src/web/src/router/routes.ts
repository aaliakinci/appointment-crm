import type { LilyPageComponent } from "@lily_platform/lily_ui";
import { createLilyRouterKit } from "@lily_platform/lily_ui/router";
import { createElement, lazy, Suspense } from "react";

import { SystemStatusPage } from "@/pages/SystemStatusPage";

import {
  AnonymousGuard,
  AppointmentAccessGuard,
  AuthenticatedGuard,
  CustomerReadGuard,
  EmployeeReadGuard,
  ServiceReadGuard,
  SchedulingManageGuard,
  ReportingReadGuard,
  MembershipReadGuard,
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
appGuardRegistry.register(AppointmentAccessGuard);
appGuardRegistry.register(ReportingReadGuard);
appGuardRegistry.register(MembershipReadGuard);

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
const LazyAppointmentsPage = lazy(async () => ({
  default: (await import("@/pages/AppointmentsPage")).AppointmentsPage,
}));
const LazyDashboardPage = lazy(async () => ({
  default: (await import("@/pages/DashboardPage")).DashboardPage,
}));
const LazyTeamPage = lazy(async () => ({
  default: (await import("@/pages/TeamPage")).TeamPage,
}));
const LazyAuditPage = lazy(async () => ({
  default: (await import("@/pages/AuditPage")).AuditPage,
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
const appointmentsPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyAppointmentsPage, props));
const dashboardPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyDashboardPage, props));
const teamPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyTeamPage, props));
const auditPage: LilyPageComponent = (props) =>
  createElement(Suspense, { fallback: null }, createElement(LazyAuditPage, props));

export const APP_ROUTES = routerKit.createRoutes([
  { id: "system-status", path: "/", page: systemStatusPage },
  { id: "login", path: "/login", page: loginPage, guards: [AnonymousGuard] },
  { id: "account", path: "/account", page: accountPage, guards: [AuthenticatedGuard] },
  {
    id: "dashboard",
    path: "/dashboard",
    page: dashboardPage,
    guards: [AuthenticatedGuard, ReportingReadGuard],
  },
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
    id: "appointments",
    path: "/appointments",
    page: appointmentsPage,
    guards: [AuthenticatedGuard, AppointmentAccessGuard],
  },
  {
    id: "scheduling",
    path: "/scheduling",
    page: schedulingPage,
    guards: [AuthenticatedGuard, SchedulingManageGuard],
  },
  {
    id: "team",
    path: "/team",
    page: teamPage,
    guards: [AuthenticatedGuard, MembershipReadGuard],
  },
  {
    id: "audit",
    path: "/audit",
    page: auditPage,
    guards: [AuthenticatedGuard, ReportingReadGuard],
  },
]);
