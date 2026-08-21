import { describe, expect, it } from "vitest";

import { AnonymousGuard, AuthenticatedGuard } from "./authGuards";

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
        state: { authentication: "anonymous" },
      }),
    ).toMatchObject({ allow: false, redirectTo: "/login" });
  });

  it("redirects authenticated users away from the login route", () => {
    expect(
      new AnonymousGuard().canActivate({
        location,
        state: { authentication: "authenticated" },
      }),
    ).toMatchObject({ allow: false, redirectTo: "/account" });
  });
});
