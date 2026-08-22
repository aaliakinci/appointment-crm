import { describe, expect, it } from "vitest";

import { toServiceInput } from "./serviceForm";

describe("service form mapping", () => {
  it("converts numeric values to the API contract", () => {
    expect(
      toServiceInput({
        name: "  Consultation ",
        durationMinutes: 30,
        price: 250.5,
        currency: "TRY",
      }),
    ).toEqual({
      name: "Consultation",
      durationMinutes: 30,
      price: 250.5,
      currency: "TRY",
    });
  });
});
