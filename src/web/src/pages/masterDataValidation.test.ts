import { describe, expect, it } from "vitest";

import {
  validateCustomerInput,
  validateEmployeeInput,
  validateServiceInput,
} from "./masterDataValidation";

describe("master-data validation", () => {
  it("accepts normalized optional customer contact data", () => {
    expect(
      validateCustomerInput({
        name: "Ayşe Demir",
        email: "ayse@example.test",
        phone: "+90 555 010 20 30",
        notes: null,
      }),
    ).toBeNull();
  });

  it("rejects customer notes above the bounded length", () => {
    expect(
      validateCustomerInput({
        name: "Ayşe Demir",
        email: null,
        phone: null,
        notes: "x".repeat(2_001),
      }),
    ).toBe("notes");
  });

  it("rejects malformed employee contact data", () => {
    expect(
      validateEmployeeInput({
        userId: null,
        name: "Employee",
        email: "not-an-email",
        phone: null,
      }),
    ).toBe("email");
  });

  it("enforces service duration increments and bounded price", () => {
    expect(
      validateServiceInput({
        name: "Consultation",
        durationMinutes: 31,
        price: 100,
        currency: "TRY",
      }),
    ).toBe("duration");
    expect(
      validateServiceInput({
        name: "Consultation",
        durationMinutes: 30,
        price: 1_000_001,
        currency: "TRY",
      }),
    ).toBe("price");
  });
});
