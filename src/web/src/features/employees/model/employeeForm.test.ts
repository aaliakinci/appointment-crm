import { describe, expect, it } from "vitest";

import { toCreateEmployeeInput } from "./employeeForm";

describe("employee form mapping", () => {
  it("keeps service selections in the create contract", () => {
    expect(
      toCreateEmployeeInput({
        userId: " ",
        name: "  Zeynep Kaya ",
        email: "zeynep@example.test",
        phone: "",
        serviceIds: ["service-a", "service-b"],
      }),
    ).toEqual({
      userId: null,
      name: "Zeynep Kaya",
      email: "zeynep@example.test",
      phone: null,
      serviceIds: ["service-a", "service-b"],
    });
  });
});
