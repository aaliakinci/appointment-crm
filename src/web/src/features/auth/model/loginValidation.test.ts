import { describe, expect, it } from "vitest";

import { validateLoginInput } from "./loginValidation";

describe("login validation", () => {
  it("requires valid credentials before submission", () => {
    expect(validateLoginInput("invalid", "", false, "")).toBe("credentials");
  });

  it("requires a tenant after a multi-membership challenge", () => {
    expect(validateLoginInput("owner@demo.local", "password", true, "")).toBe("tenant");
  });

  it("accepts a complete tenant selection", () => {
    expect(validateLoginInput("owner@demo.local", "password", true, "tenant-a")).toBeNull();
  });
});
