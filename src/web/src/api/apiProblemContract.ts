import { LilyApiError } from "@lily_platform/lily_ui/errors";
import type { LilyApiErrorMapperContext } from "@lily_platform/lily_ui/http";

export interface ApiProblemDetails {
  readonly type?: string;
  readonly title?: string;
  readonly status: number;
  readonly detail?: string;
  readonly instance?: string;
  readonly code: string;
  readonly traceId: string;
  readonly errors?: Readonly<Record<string, readonly string[]>>;
}

export function decodeApiProblemDetails(body: unknown): ApiProblemDetails | null {
  if (!isRecord(body)) {
    return null;
  }

  const { type, title, status, detail, instance, code, traceId, errors } = body;
  if (
    typeof status !== "number" ||
    !Number.isInteger(status) ||
    typeof code !== "string" ||
    code.length === 0 ||
    typeof traceId !== "string" ||
    traceId.length === 0 ||
    !isOptionalString(type) ||
    !isOptionalString(title) ||
    !isOptionalString(detail) ||
    !isOptionalString(instance)
  ) {
    return null;
  }

  const decodedErrors = decodeValidationErrors(errors);
  if (errors !== undefined && decodedErrors === null) {
    return null;
  }

  return {
    type,
    title,
    status,
    detail,
    instance,
    code,
    traceId,
    errors: decodedErrors ?? undefined,
  };
}

export function mapApiProblemError(context: LilyApiErrorMapperContext): LilyApiError {
  const problem = decodeApiProblemDetails(context.body);
  return new LilyApiError({
    code: problem?.code ?? "common.http_error",
    message: problem?.detail ?? problem?.title ?? `Request failed with status ${context.status}.`,
    details: problem?.errors ?? context.body,
    traceId: problem?.traceId ?? context.requestId,
    statusCode: context.status,
  });
}

function decodeValidationErrors(
  value: unknown,
): Readonly<Record<string, readonly string[]>> | null {
  if (value === undefined) {
    return null;
  }

  if (!isRecord(value)) {
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

  return Object.fromEntries(entries) as Readonly<Record<string, readonly string[]>>;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isOptionalString(value: unknown): value is string | undefined {
  return value === undefined || typeof value === "string";
}
