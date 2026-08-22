import { describe, expect, it } from "vitest";

import { AnonymousGuard, AuthenticatedGuard, CustomerReadGuard } from "./authGuards";

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
});
