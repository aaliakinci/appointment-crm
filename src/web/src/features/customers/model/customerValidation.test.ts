import { describe, expect, it } from "vitest";

import { validateCustomerInput } from "./customerValidation";

describe("customer validation", () => {
  it("accepts normalized optional contact data", () => {
    expect(
      validateCustomerInput({
        name: "Ayşe Demir",
        email: "ayse@example.test",
        phone: "+90 555 010 20 30",
        notes: null,
      }),
    ).toBeNull();
  });

  it("rejects notes above the bounded length", () => {
    expect(
      validateCustomerInput({
        name: "Ayşe Demir",
        email: null,
        phone: null,
        notes: "x".repeat(2_001),
      }),
    ).toBe("notes");
  });
});
