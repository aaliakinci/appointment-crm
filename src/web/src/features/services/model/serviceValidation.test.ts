import { describe, expect, it } from "vitest";

import { validateServiceInput } from "./serviceValidation";

describe("service validation", () => {
  it("enforces duration increments and bounded price", () => {
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
