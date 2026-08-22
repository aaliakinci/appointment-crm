import type { LilyValidationIssue } from "@lily_platform/lily_ui/ui/forms";

export function createFieldValidators<TValue>(
  isValid: (value: TValue) => boolean,
  code: string,
  defaultMessage: string,
) {
  const validate = (value: TValue): LilyValidationIssue | undefined =>
    isValid(value) ? undefined : { code, defaultMessage };

  return { onBlur: validate, onSubmit: validate };
}
