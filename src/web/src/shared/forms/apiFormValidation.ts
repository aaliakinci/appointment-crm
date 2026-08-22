import { LilyApiError } from "@lily_platform/lily_ui/errors";
import type {
  LilyFieldIssueMap,
  LilySubmitResult,
  LilyValidationIssue,
} from "@lily_platform/lily_ui/ui/forms";

const validationFailedCode = "common.validation_failed";

export function mapApiValidationError<TValues extends object>(
  error: unknown,
  fieldNames: readonly (keyof TValues & string)[],
): Exclude<LilySubmitResult<TValues>, void> | null {
  if (!(error instanceof LilyApiError) || error.code !== validationFailedCode) {
    return null;
  }

  const details = toValidationDetails(error.details);
  if (!details) {
    return null;
  }

  const knownFields = new Map(fieldNames.map((name) => [normalizeFieldName(name), name]));
  const fieldIssues: Record<string, readonly LilyValidationIssue[]> = {};
  const formIssues: LilyValidationIssue[] = [];

  for (const [serverField, messages] of Object.entries(details)) {
    const fieldName = knownFields.get(normalizeFieldName(serverField));
    const issues = messages.map((message) => ({
      code: validationFailedCode,
      defaultMessage: message,
    }));

    if (fieldName) {
      fieldIssues[fieldName] = issues;
    } else {
      formIssues.push(...issues);
    }
  }

  return {
    status: "invalid",
    fieldIssues: fieldIssues as LilyFieldIssueMap<TValues>,
    formIssues,
  };
}

function toValidationDetails(value: unknown): Readonly<Record<string, readonly string[]>> | null {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    return null;
  }

  const entries = Object.entries(value);
  if (
    entries.some(
      ([field, messages]) =>
        field.length === 0 ||
        !Array.isArray(messages) ||
        messages.some((message) => typeof message !== "string"),
    )
  ) {
    return null;
  }

  return Object.fromEntries(entries);
}

function normalizeFieldName(value: string): string {
  const segments = value.split(/[.[\]]/).filter(Boolean);
  return (segments[segments.length - 1] ?? value).toLocaleLowerCase("en-US");
}
