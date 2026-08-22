import type { ServiceInput } from "../api/serviceContract";
import { isValidContactName } from "@/shared/forms";

export type ServiceValidationError = "name" | "duration" | "price";

export function validateServiceInput(input: ServiceInput): ServiceValidationError | null {
  if (!isValidContactName(input.name)) {
    return "name";
  }
  if (!isValidServiceDuration(input.durationMinutes)) {
    return "duration";
  }
  return isValidServicePrice(input.price) ? null : "price";
}

export function isValidServiceDuration(value: number | null): boolean {
  return value !== null && Number.isInteger(value) && value >= 5 && value <= 480 && value % 5 === 0;
}

export function isValidServicePrice(value: number | null): boolean {
  return value !== null && Number.isFinite(value) && value >= 0 && value <= 1_000_000;
}
