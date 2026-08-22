import { createLilyHttpClient } from "@lily_platform/lily_ui/http";

import { resolveApiBaseUrl } from "./apiBaseUrl";
import { mapApiProblemError } from "./apiProblemContract";

interface HttpAuthenticationConfiguration {
  readonly getAccessToken: () => string | null;
  readonly refresh: () => Promise<void>;
  readonly onSessionExpired: () => void;
}

const configuredApiBaseUrl: unknown = import.meta.env.VITE_APPOINTMENT_CRM_API_URL;
let authentication: HttpAuthenticationConfiguration | null = null;
let recoveryRefreshPromise: Promise<void> | null = null;

export function configureHttpAuthentication(configuration: HttpAuthenticationConfiguration): void {
  authentication = configuration;
}

export const appHttpClient = createLilyHttpClient({
  baseURL: resolveApiBaseUrl(
    typeof configuredApiBaseUrl === "string" ? configuredApiBaseUrl : undefined,
  ),
  timeoutMs: 10_000,
  credentials: "include",
  defaultHeaders: { Accept: "application/json" },
  mapApiError: mapApiProblemError,
  credentialProvider: {
    getAccessToken: () => authentication?.getAccessToken() ?? null,
  },
  authRecovery: {
    shouldRecover: ({ status }) => status === 401 && authentication !== null,
    refresh: async () => {
      if (!authentication) {
        throw new Error("HTTP authentication has not been configured.");
      }

      recoveryRefreshPromise ??= authentication.refresh().finally(() => {
        recoveryRefreshPromise = null;
      });
      await recoveryRefreshPromise;
    },
    onSessionExpired: () => authentication?.onSessionExpired(),
  },
});
