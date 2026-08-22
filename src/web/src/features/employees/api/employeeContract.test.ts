import { describe, expect, it } from "vitest";

import { decodeEmployee } from "./employeeContract";

describe("employee contract", () => {
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
