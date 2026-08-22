import { describe, expect, it } from "vitest";

import { toCustomerInput } from "./customerForm";

describe("customer form mapping", () => {
  it("normalizes blank optional fields for the API contract", () => {
    expect(
      toCustomerInput({
        name: "  Ayşe Demir  ",
        email: " ",
        phone: " +90 555 010 20 30 ",
        notes: " ",
      }),
    ).toEqual({
      name: "Ayşe Demir",
      email: null,
      phone: "+90 555 010 20 30",
      notes: null,
    });
  });
});
