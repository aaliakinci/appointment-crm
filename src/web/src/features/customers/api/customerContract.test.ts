import { describe, expect, it } from "vitest";

import { decodeCustomerPage } from "./customerContract";

describe("customer contract", () => {
  it("decodes a bounded customer page", () => {
    const page = decodeCustomerPage({
      items: [
        {
          id: "customer-a",
          name: "Customer",
          email: null,
          phone: null,
          notes: null,
          archivedAtUtc: null,
          createdAtUtc: "2026-08-22T00:00:00Z",
          updatedAtUtc: "2026-08-22T00:00:00Z",
        },
      ],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });

    expect(page.items[0]?.id).toBe("customer-a");
  });
});
