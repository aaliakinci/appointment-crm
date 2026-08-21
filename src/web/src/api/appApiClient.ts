import { createLilyHttpClient } from "@lily_platform/lily_ui/http";

import { authSessionStore } from "@/state/authSessionStore";

import { resolveApiBaseUrl } from "./apiBaseUrl";
import {
  decodeAuthenticationResponse,
  decodeTenantOptions,
  type AuthenticationResponse,
  type LoginRequest,
  type TenantOption,
} from "./authContract";
import { decodeHealthReport, type HealthReport } from "./healthContract";

const configuredApiBaseUrl: unknown = import.meta.env.VITE_APPOINTMENT_CRM_API_URL;
let recoveryRefreshPromise: Promise<void> | null = null;

export const appApiClient = createLilyHttpClient({
  baseURL: resolveApiBaseUrl(
    typeof configuredApiBaseUrl === "string" ? configuredApiBaseUrl : undefined,
  ),
  timeoutMs: 10_000,
  credentials: "include",
  defaultHeaders: { Accept: "application/json" },
  credentialProvider: {
    getAccessToken: () => authSessionStore.getAccessToken(),
  },
  authRecovery: {
    shouldRecover: ({ status }) => status === 401,
    refresh: async () => {
      recoveryRefreshPromise ??= refreshAuthentication()
        .then(() => undefined)
        .finally(() => {
          recoveryRefreshPromise = null;
        });
      await recoveryRefreshPromise;
    },
    onSessionExpired: () => authSessionStore.clear(),
  },
});

export function getReadiness(signal?: AbortSignal): Promise<HealthReport> {
  return appApiClient.getData<HealthReport>("/health/ready", {
    signal,
    decode: decodeHealthReport,
    metadata: { operationName: "system.readiness" },
  });
}

export async function login(request: LoginRequest): Promise<AuthenticationResponse> {
  const response = await appApiClient.postData<AuthenticationResponse, LoginRequest>(
    "/api/v1/auth/login",
    request,
    {
      decode: decodeAuthenticationResponse,
      metadata: {
        auth: "none",
        authRecovery: "none",
        operationName: "identity.login",
        replay: "deny",
      },
    },
  );
  if (!response.requiresTenantSelection) {
    authSessionStore.setAuthentication(response);
  }

  return response;
}

export async function refreshAuthentication(): Promise<AuthenticationResponse> {
  const response = await appApiClient.postData<AuthenticationResponse, Record<string, never>>(
    "/api/v1/auth/refresh",
    {},
    {
      decode: decodeAuthenticationResponse,
      metadata: {
        auth: "none",
        authRecovery: "none",
        operationName: "identity.refresh",
        replay: "deny",
      },
    },
  );
  authSessionStore.setAuthentication(response);
  return response;
}

export async function logout(): Promise<void> {
  try {
    await appApiClient.postData<void, Record<string, never>>(
      "/api/v1/auth/logout",
      {},
      {
        metadata: {
          authRecovery: "none",
          operationName: "identity.logout",
          replay: "deny",
        },
      },
    );
  } finally {
    authSessionStore.clear();
  }
}

export async function revokeAllSessions(): Promise<void> {
  try {
    await appApiClient.postData<void, Record<string, never>>(
      "/api/v1/auth/revoke-all",
      {},
      {
        metadata: {
          authRecovery: "none",
          operationName: "identity.revoke-all",
          replay: "deny",
        },
      },
    );
  } finally {
    authSessionStore.clear();
  }
}

export async function switchTenant(tenantId: string): Promise<AuthenticationResponse> {
  const response = await appApiClient.postData<AuthenticationResponse, { tenantId: string }>(
    "/api/v1/auth/switch-tenant",
    { tenantId },
    {
      decode: decodeAuthenticationResponse,
      metadata: {
        authRecovery: "none",
        operationName: "identity.switch-tenant",
        replay: "deny",
      },
    },
  );
  authSessionStore.setAuthentication(response);
  return response;
}

export function listAvailableTenants(signal?: AbortSignal): Promise<readonly TenantOption[]> {
  return appApiClient.getData<readonly TenantOption[]>("/api/v1/auth/tenants", {
    signal,
    decode: decodeTenantOptions,
    metadata: { operationName: "identity.list-tenants" },
  });
}
