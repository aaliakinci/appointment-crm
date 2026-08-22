import { isValidContactEmail, isValidContactName, isValidContactPhone } from "@/shared/forms";

import type { EmployeeInput } from "../api/employeeContract";

export type EmployeeValidationError = "name" | "email" | "phone";

export function validateEmployeeInput(input: EmployeeInput): EmployeeValidationError | null {
  if (!isValidContactName(input.name)) {
    return "name";
  }
  if (!isValidContactEmail(input.email ?? "")) {
    return "email";
  }
  return isValidContactPhone(input.phone ?? "") ? null : "phone";
}
