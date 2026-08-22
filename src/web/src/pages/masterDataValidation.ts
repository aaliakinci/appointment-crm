import type { CustomerInput, EmployeeInput, ServiceInput } from "@/api";

export type MasterDataValidationError = "name" | "email" | "phone" | "notes" | "duration" | "price";

export function validateCustomerInput(input: CustomerInput): MasterDataValidationError | null {
  const common = validateContact(input.name, input.email, input.phone);
  if (common) {
    return common;
  }

  return input.notes && input.notes.trim().length > 2_000 ? "notes" : null;
}

export function validateEmployeeInput(input: EmployeeInput): MasterDataValidationError | null {
  return validateContact(input.name, input.email, input.phone);
}

export function validateServiceInput(input: ServiceInput): MasterDataValidationError | null {
  if (input.name.trim().length < 2 || input.name.trim().length > 160) {
    return "name";
  }

  if (
    !Number.isInteger(input.durationMinutes) ||
    input.durationMinutes < 5 ||
    input.durationMinutes > 480 ||
    input.durationMinutes % 5 !== 0
  ) {
    return "duration";
  }

  if (input.price < 0 || input.price > 1_000_000 || !Number.isFinite(input.price)) {
    return "price";
  }

  return null;
}

function validateContact(
  name: string,
  email: string | null,
  phone: string | null,
): MasterDataValidationError | null {
  const normalizedName = name.trim();
  if (normalizedName.length < 2 || normalizedName.length > 160) {
    return "name";
  }

  if (email && !/^\S+@\S+\.\S+$/.test(email.trim())) {
    return "email";
  }

  if (phone) {
    const digitCount = phone.replace(/\D/g, "").length;
    if (digitCount < 7 || digitCount > 15) {
      return "phone";
    }
  }

  return null;
}
