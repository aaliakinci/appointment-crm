import type { CustomerInput } from "../api/customerContract";
import { isValidContactEmail, isValidContactName, isValidContactPhone } from "@/shared/forms";

export type CustomerValidationError = "name" | "email" | "phone" | "notes";

export function validateCustomerInput(input: CustomerInput): CustomerValidationError | null {
  if (!isValidContactName(input.name)) {
    return "name";
  }
  if (!isValidContactEmail(input.email ?? "")) {
    return "email";
  }
  if (!isValidContactPhone(input.phone ?? "")) {
    return "phone";
  }
  return isValidCustomerNotes(input.notes ?? "") ? null : "notes";
}

export function isValidCustomerNotes(value: string): boolean {
  return value.trim().length <= 2_000;
}
