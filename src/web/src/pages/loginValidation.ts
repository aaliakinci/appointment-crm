export type LoginValidationError = "credentials" | "tenant" | null;

export function validateLoginInput(
  email: string,
  password: string,
  tenantSelectionRequired: boolean,
  tenantId: string,
): LoginValidationError {
  if (!email.includes("@") || password.length === 0) {
    return "credentials";
  }

  if (tenantSelectionRequired && tenantId.length === 0) {
    return "tenant";
  }

  return null;
}
