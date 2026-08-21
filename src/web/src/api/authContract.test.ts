import { describe, expect, it } from "vitest";

import { decodeAuthenticationResponse, decodeTenantOptions } from "./authContract";

describe("authentication contract", () => {
  it("decodes a tenant-selection response without accepting a session token", () => {
    const response = decodeAuthenticationResponse({
      requiresTenantSelection: true,
      accessToken: null,
      accessTokenExpiresAtUtc: null,
      user: null,
      activeTenant: null,
      tenants: [{ id: "tenant-a", name: "Atlas", slug: "atlas", role: "Owner" }],
    });

    expect(response.requiresTenantSelection).toBe(true);
    expect(response.tenants).toHaveLength(1);
  });

  it("rejects an authenticated response with missing identity fields", () => {
    expect(() =>
      decodeAuthenticationResponse({
        requiresTenantSelection: false,
        accessToken: "protected-token",
        accessTokenExpiresAtUtc: null,
        user: null,
        activeTenant: null,
        tenants: [],
      }),
    ).toThrow("missing its session fields");
  });

  it("rejects malformed tenant collections", () => {
    expect(() => decodeTenantOptions({})).toThrow("tenant options must be an array");
  });
});
