import { describe, expect, it } from "vitest";

import { decodeAuditPage } from "./auditContract";

describe("audit contract", () => {
  it("decodes the safe audit list projection", () => {
    const page = decodeAuditPage({
      items: [
        {
          id: "audit-a",
          actorUserId: "user-a",
          actorName: "Demo Owner",
          action: "membership.authorization-changed",
          targetType: "membership",
          targetId: "membership-a",
          summary: "Role changed from Receptionist to Employee.",
          occurredAtUtc: "2026-08-23T09:00:00Z",
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });

    expect(page.items[0]).toMatchObject({
      actorName: "Demo Owner",
      action: "membership.authorization-changed",
    });
  });
});
