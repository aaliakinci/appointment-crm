export function isValidContactName(value: string): boolean {
  const length = value.trim().length;
  return length >= 2 && length <= 160;
}

export function isValidContactEmail(value: string): boolean {
  const normalized = value.trim();
  return normalized.length === 0 || /^\S+@\S+\.\S+$/.test(normalized);
}

export function isValidContactPhone(value: string): boolean {
  const normalized = value.trim();
  if (normalized.length === 0) {
    return true;
  }

  const digitCount = normalized.replace(/\D/g, "").length;
  return digitCount >= 7 && digitCount <= 15;
}

export function nullableTrimmed(value: string): string | null {
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : null;
}
