import {
  listAvailableTenants,
  login,
  logout,
  refreshAuthentication,
  revokeAllSessions,
  switchTenant,
} from "@/api";
import { useEffect, useMemo, useSyncExternalStore, type PropsWithChildren } from "react";

import { authSessionStore } from "./authSessionStore";
import { AuthContext, type AuthContextValue } from "./authContext";
let initializationPromise: Promise<void> | null = null;

export function AuthProvider({ children }: PropsWithChildren) {
  const snapshot = useSyncExternalStore(
    authSessionStore.subscribe,
    authSessionStore.getSnapshot,
    authSessionStore.getSnapshot,
  );

  useEffect(() => {
    initializationPromise ??= initializeSession();
    void initializationPromise;
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      ...snapshot,
      login,
      logout,
      revokeAllSessions,
      switchTenant,
      listAvailableTenants,
    }),
    [snapshot],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

async function initializeSession(): Promise<void> {
  try {
    await refreshAuthentication();
  } catch {
    authSessionStore.clear();
  } finally {
    authSessionStore.markInitialized();
  }
}
