import { describe, expect, it } from "vitest";

import {
  AnonymousGuard,
  AppointmentAccessGuard,
  AuthenticatedGuard,
  CustomerReadGuard,
  MembershipReadGuard,
  ReportingReadGuard,
  SchedulingManageGuard,
  workspaceLandingPath,
} from "./authGuards";

const location = {
  pathname: "/account",
  search: "",
  hash: "",
  state: null,
  key: "test",
};

describe("authentication route guards", () => {
  it("redirects anonymous users away from protected routes", () => {
    expect(
      new AuthenticatedGuard().canActivate({
        location,
        state: { authentication: "anonymous", permissions: [] },
      }),
    ).toMatchObject({ allow: false, redirectTo: "/login" });
  });

  it("redirects authenticated users away from the login route", () => {
    expect(
      new AnonymousGuard().canActivate({
        location,
        state: { authentication: "authenticated", permissions: [] },
      }),
    ).toMatchObject({ allow: false, redirectTo: "/account" });
  });

  it("allows only sessions carrying the route permission", () => {
    const guard = new CustomerReadGuard();
    expect(
      guard.canActivate({
        location,
        state: { authentication: "authenticated", permissions: ["customers.read"] },
      }),
    ).toMatchObject({ allow: true });
    expect(
      guard.canActivate({
        location,
        state: { authentication: "authenticated", permissions: [] },
      }),
    ).toMatchObject({ allow: false, redirectTo: "/account" });
  });

  it("keeps scheduling management behind its dedicated permission", () => {
    const guard = new SchedulingManageGuard();
    expect(
      guard.canActivate({
        location,
        state: { authentication: "authenticated", permissions: ["scheduling.manage"] },
      }),
    ).toMatchObject({ allow: true });
    expect(
      guard.canActivate({
        location,
        state: { authentication: "authenticated", permissions: ["availability.read"] },
      }),
    ).toMatchObject({ allow: false });
  });

  it("allows both tenant and employee-own appointment read surfaces", () => {
    const guard = new AppointmentAccessGuard();
    for (const permission of ["appointments.read", "appointments.read-own"]) {
      expect(
        guard.canActivate({
          location,
          state: { authentication: "authenticated", permissions: [permission] },
        }),
      ).toMatchObject({ allow: true });
    }
    expect(
      guard.canActivate({
        location,
        state: { authentication: "authenticated", permissions: ["availability.read"] },
      }),
    ).toMatchObject({ allow: false });
  });

  it("keeps reporting and team routes behind their own permissions", () => {
    expect(
      new ReportingReadGuard().canActivate({
        location,
        state: { authentication: "authenticated", permissions: ["reporting.read"] },
      }),
    ).toMatchObject({ allow: true });
    expect(
      new MembershipReadGuard().canActivate({
        location,
        state: { authentication: "authenticated", permissions: ["reporting.read"] },
      }),
    ).toMatchObject({ allow: false, redirectTo: "/account" });
  });

  it("selects a permission-aware workspace landing route", () => {
    expect(workspaceLandingPath(["reporting.read", "appointments.read"])).toBe("/dashboard");
    expect(workspaceLandingPath(["appointments.read-own"])).toBe("/appointments");
    expect(workspaceLandingPath([])).toBe("/account");
  });
});
