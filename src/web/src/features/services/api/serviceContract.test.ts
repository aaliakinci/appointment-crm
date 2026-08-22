import { describe, expect, it } from "vitest";

import { decodeService } from "./serviceContract";

describe("service contract", () => {
  it("rejects malformed numeric fields", () => {
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
});
