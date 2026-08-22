import { describe, expect, it } from "vitest";

import { resolveApiBaseUrl } from "./apiBaseUrl";

describe("resolveApiBaseUrl", () => {
  it("uses the current origin when configuration is empty", () => {
    expect(resolveApiBaseUrl(undefined, "https://crm.example.test")).toBe(
      "https://crm.example.test/",
    );
  });

  it("uses and normalizes a configured HTTP base URL", () => {
    expect(resolveApiBaseUrl("https://api.example.test/root", "https://app.example.test")).toBe(
      "https://api.example.test/root/",
    );
  });

  it("rejects credentials in the configured URL", () => {
    expect(() =>
      resolveApiBaseUrl("https://user:secret@api.example.test", "https://app.example.test"),
    ).toThrow("must not include credentials");
  });

  it("rejects non-http protocols", () => {
    expect(() => resolveApiBaseUrl("file:///tmp/socket", "https://crm.example.test")).toThrow(
      "must use HTTP or HTTPS",
    );
  });
});
