export type LoginValidationError = "credentials" | "tenant" | null;

export function validateLoginInput(
  email: string,
  password: string,
  tenantSelectionRequired: boolean,
  tenantId: string,
): LoginValidationError {
  if (!isValidLoginEmail(email) || !hasLoginPassword(password)) {
    return "credentials";
  }

  if (tenantSelectionRequired && tenantId.length === 0) {
    return "tenant";
  }

  return null;
}

export function isValidLoginEmail(value: string): boolean {
  return /^\S+@\S+\.\S+$/.test(value.trim());
}

export function hasLoginPassword(value: string): boolean {
  return value.length > 0;
}
