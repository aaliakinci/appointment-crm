import { describe, expect, it } from "vitest";

import { decodeApiProblemDetails, mapApiProblemError } from "./apiProblemContract";

describe("API Problem Details contract", () => {
  it("decodes the stable error contract including field errors", () => {
    const problem = decodeApiProblemDetails({
      type: "https://tools.ietf.org/html/rfc9110#section-15.5.1",
      title: "Bad Request",
      status: 400,
      detail: "One or more validation errors occurred.",
      instance: "/api/v1/customers",
      code: "common.validation_failed",
      traceId: "f00d",
      errors: { email: ["Email must be a valid address."] },
    });

    expect(problem?.code).toBe("common.validation_failed");
    expect(problem?.errors?.email).toEqual(["Email must be a valid address."]);
  });

  it("maps Problem Details fields to LilyApiError", () => {
    const error = mapApiProblemError({
      status: 409,
      body: {
        title: "Conflict",
        status: 409,
        detail: "A customer with the same email already exists in this tenant.",
        code: "customers.email_conflict",
        traceId: "cafe",
      },
      headers: {},
      method: "POST",
      url: "/api/v1/customers",
    });

    expect(error.code).toBe("customers.email_conflict");
    expect(error.message).toContain("same email");
    expect(error.traceId).toBe("cafe");
    expect(error.statusCode).toBe(409);
  });

  it("uses a safe fallback for a non-contract HTTP body", () => {
    const error = mapApiProblemError({
      status: 502,
      body: "Bad Gateway",
      headers: {},
      method: "GET",
      url: "/health/ready",
      requestId: "request-id",
    });

    expect(error.code).toBe("common.http_error");
    expect(error.traceId).toBe("request-id");
  });
});
