import { beforeEach, describe, expect, it } from "vitest";

import type { AuthenticationResponse } from "../api/authContract";
import { authSessionStore } from "./authSessionStore";

const authenticatedResponse: AuthenticationResponse = {
  requiresTenantSelection: false,
  accessToken: "protected-access-token",
  accessTokenExpiresAtUtc: "2026-08-21T20:00:00Z",
  user: { id: "user-a", email: "owner@demo.local", displayName: "Demo Owner" },
  activeTenant: {
    id: "tenant-a",
    name: "Atlas",
    slug: "atlas",
    currency: "TRY",
    timeZone: "Europe/Istanbul",
    role: "Owner",
    permissions: ["tenant.read"],
  },
  tenants: [],
};

describe("authSessionStore", () => {
  beforeEach(() => authSessionStore.clear());

  it("keeps only the access token in process memory", () => {
    authSessionStore.setAuthentication(authenticatedResponse);
    expect(authSessionStore.getAccessToken()).toBe("protected-access-token");
    expect(JSON.stringify(authSessionStore.getSnapshot())).not.toContain("refresh");
  });

  it("rejects a tenant-selection payload as a session", () => {
    expect(() =>
      authSessionStore.setAuthentication({
        ...authenticatedResponse,
        requiresTenantSelection: true,
        accessToken: null,
      }),
    ).toThrow("cannot initialize a session");
  });

  it("updates the in-memory identity after a profile save without replacing the session", () => {
    authSessionStore.setAuthentication(authenticatedResponse);
    authSessionStore.updateUser({
      id: "user-a",
      email: "owner@demo.local",
      displayName: "Portfolio Owner",
    });

    expect(authSessionStore.getSnapshot().session).toMatchObject({
      accessToken: "protected-access-token",
      user: { displayName: "Portfolio Owner" },
    });
  });
});
