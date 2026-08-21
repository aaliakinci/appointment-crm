import { describe, expect, it } from "vitest";

import { resolveApiBaseUrl } from "./apiBaseUrl";

describe("resolveApiBaseUrl", () => {
  it("uses the current origin when configuration is empty", () => {
    expect(resolveApiBaseUrl(undefined, "https://crm.example.test")).toBe(
      "https://crm.example.test/",
    );
  });

  it("rejects embedded credentials", () => {
    expect(() =>
      resolveApiBaseUrl("https://user:secret@crm.example.test", "https://crm.example.test"),
    ).toThrow("must not include credentials");
  });

  it("rejects non-http protocols", () => {
    expect(() => resolveApiBaseUrl("file:///tmp/socket", "https://crm.example.test")).toThrow(
      "must use HTTP or HTTPS",
    );
  });
});
