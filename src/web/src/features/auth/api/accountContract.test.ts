import { describe, expect, it } from "vitest";

import { decodeAccountProfile, decodeAccountSessions } from "./accountContract";

describe("account contract", () => {
  it("decodes the editable profile and active session inventory", () => {
    const profile = decodeAccountProfile({
      userId: "user-a",
      email: "owner@demo.local",
      displayName: "Demo Owner",
      updatedAtUtc: "2026-08-23T09:00:00Z",
    });
    const sessions = decodeAccountSessions([
      {
        id: "session-a",
        tenantName: "Atlas Salon",
        createdAtUtc: "2026-08-23T08:00:00Z",
        lastUsedAtUtc: null,
        expiresAtUtc: "2026-09-22T08:00:00Z",
        isCurrent: true,
      },
    ]);

    expect(profile.displayName).toBe("Demo Owner");
    expect(sessions[0]).toMatchObject({ id: "session-a", isCurrent: true });
  });
});
