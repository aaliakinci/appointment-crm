import type { AuthenticationResponse, LoginRequest, TenantOption } from "@/api";
import { createContext, useContext } from "react";

import type { AuthSnapshot } from "./authSessionStore";

export interface AuthContextValue extends AuthSnapshot {
  readonly login: (request: LoginRequest) => Promise<AuthenticationResponse>;
  readonly logout: () => Promise<void>;
  readonly revokeAllSessions: () => Promise<void>;
  readonly switchTenant: (tenantId: string) => Promise<AuthenticationResponse>;
  readonly listAvailableTenants: (signal?: AbortSignal) => Promise<readonly TenantOption[]>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}
