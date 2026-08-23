import { describe, expect, it } from "vitest";

import { decodeMembership, decodeMembershipReport } from "./membershipContract";

describe("membership contract", () => {
  it("decodes supported roles and summary counts", () => {
    const membership = decodeMembership({
      id: "membership-a",
      userId: "user-a",
      email: "manager@demo.local",
      displayName: "Demo Manager",
      role: "Manager",
      isActive: true,
      updatedAtUtc: "2026-08-23T09:00:00Z",
    });
    const report = decodeMembershipReport({
      total: 4,
      active: 3,
      byRole: { Owner: 1, Manager: 1, Receptionist: 1, Employee: 1 },
    });

    expect(membership.role).toBe("Manager");
    expect(report.byRole.Owner).toBe(1);
  });

  it("rejects roles outside the backend authorization model", () => {
    expect(() =>
      decodeMembership({
        id: "membership-a",
        userId: "user-a",
        email: "admin@demo.local",
        displayName: "Admin",
        role: "Administrator",
        isActive: true,
        updatedAtUtc: "2026-08-23T09:00:00Z",
      }),
    ).toThrow("membership.role is not recognized");
  });
});
