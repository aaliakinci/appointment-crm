import { beforeEach, describe, expect, it } from "vitest";

import type { AuthenticationResponse } from "@/api";

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
    expect(authSessionStore.getSnapshot().session?.activeTenant.id).toBe("tenant-a");
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
});
