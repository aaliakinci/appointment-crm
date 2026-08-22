import { describe, expect, it } from "vitest";

import { validateEmployeeInput } from "./employeeValidation";

describe("employee validation", () => {
  it("rejects malformed contact data", () => {
    expect(
      validateEmployeeInput({
        userId: null,
        name: "Employee",
        email: "not-an-email",
        phone: null,
      }),
    ).toBe("email");
  });
});
