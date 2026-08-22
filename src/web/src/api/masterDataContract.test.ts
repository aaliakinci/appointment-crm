import { describe, expect, it } from "vitest";

import { decodeCustomerPage, decodeEmployee, decodeService } from "./masterDataContract";

describe("master-data contracts", () => {
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

  it("rejects malformed service numbers", () => {
    expect(() =>
      decodeService({
        id: "service-a",
        name: "Service",
        durationMinutes: "30",
        price: 100,
        currency: "TRY",
        isActive: true,
        createdAtUtc: "now",
        updatedAtUtc: "now",
      }),
    ).toThrow("durationMinutes");
  });

  it("decodes employee service assignments", () => {
    const employee = decodeEmployee({
      id: "employee-a",
      userId: null,
      name: "Employee",
      email: null,
      phone: null,
      isActive: true,
      services: [{ id: "service-a", name: "Service", isActive: true }],
      createdAtUtc: "now",
      updatedAtUtc: "now",
    });

    expect(employee.services).toHaveLength(1);
  });
});
