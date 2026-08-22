import { LilyApiError } from "@lily_platform/lily_ui/errors";
import { describe, expect, it } from "vitest";

import { mapApiValidationError } from "./apiFormValidation";

interface TestFormValues {
  email: string;
  name: string;
}

describe("API form validation mapping", () => {
  it("maps case-insensitive fields and keeps unknown issues at form level", () => {
    const error = new LilyApiError({
      code: "common.validation_failed",
      message: "Validation failed.",
      details: { Email: ["Email is invalid."], request: ["The request is invalid."] },
      statusCode: 400,
    });

    const result = mapApiValidationError<TestFormValues>(error, ["email", "name"]);

    expect(result?.fieldIssues?.email?.[0]?.defaultMessage).toBe("Email is invalid.");
    expect(result?.formIssues?.[0]?.defaultMessage).toBe("The request is invalid.");
  });

  it("leaves non-validation failures to the technical error flow", () => {
    const error = new LilyApiError({ code: "customers.contact_conflict", message: "Conflict." });
    expect(mapApiValidationError<TestFormValues>(error, ["email", "name"])).toBeNull();
  });
});
