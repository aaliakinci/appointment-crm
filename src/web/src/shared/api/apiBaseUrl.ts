import { LilyValidationError } from "@lily_platform/lily_ui/errors";

const TEST_FALLBACK_ORIGIN = "http://127.0.0.1:8080";

export function resolveApiBaseUrl(value: string | undefined, origin?: string): string {
  const runtimeOrigin = origin ?? globalThis.location?.origin ?? TEST_FALLBACK_ORIGIN;
  const candidate = value?.trim() || runtimeOrigin;

  let parsedUrl: URL;
  try {
    parsedUrl = new URL(candidate, runtimeOrigin);
  } catch (cause) {
    throw new LilyValidationError({
      code: "AppointmentCrm.Api.InvalidBaseUrl",
      message: "Appointment CRM API base URL is invalid.",
      cause,
    });
  }

  if (parsedUrl.protocol !== "http:" && parsedUrl.protocol !== "https:") {
    throw new LilyValidationError({
      code: "AppointmentCrm.Api.UnsupportedProtocol",
      message: "Appointment CRM API base URL must use HTTP or HTTPS.",
    });
  }

  if (parsedUrl.username || parsedUrl.password) {
    throw new LilyValidationError({
      code: "AppointmentCrm.Api.CredentialsNotAllowed",
      message: "Appointment CRM API base URL must not include credentials.",
    });
  }

  parsedUrl.search = "";
  parsedUrl.hash = "";
  parsedUrl.pathname = `${parsedUrl.pathname.replace(/\/+$/u, "")}/`;

  return parsedUrl.toString();
}
