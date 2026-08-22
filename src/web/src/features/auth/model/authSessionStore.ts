import type { ActiveTenant, AuthenticatedUser, AuthenticationResponse } from "../api/authContract";

export interface AuthSession {
  readonly accessToken: string;
  readonly accessTokenExpiresAtUtc: string;
  readonly user: AuthenticatedUser;
  readonly activeTenant: ActiveTenant;
}

export interface AuthSnapshot {
  readonly initialized: boolean;
  readonly session: AuthSession | null;
}

let snapshot: AuthSnapshot = { initialized: false, session: null };
const listeners = new Set<() => void>();

export const authSessionStore = {
  getSnapshot: (): AuthSnapshot => snapshot,
  subscribe: (listener: () => void): (() => void) => {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  getAccessToken: (): string | null => snapshot.session?.accessToken ?? null,
  setAuthentication: (response: AuthenticationResponse): void => {
    if (
      response.requiresTenantSelection ||
      !response.accessToken ||
      !response.accessTokenExpiresAtUtc ||
      !response.user ||
      !response.activeTenant
    ) {
      throw new TypeError("A tenant-selection response cannot initialize a session.");
    }

    update({
      initialized: true,
      session: {
        accessToken: response.accessToken,
        accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
        user: response.user,
        activeTenant: response.activeTenant,
      },
    });
  },
  clear: (): void => update({ initialized: true, session: null }),
  markInitialized: (): void => update({ ...snapshot, initialized: true }),
};

function update(next: AuthSnapshot): void {
  snapshot = next;
  listeners.forEach((listener) => listener());
}
