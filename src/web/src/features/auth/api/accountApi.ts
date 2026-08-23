import { appHttpClient } from "@/shared/api";

import { authSessionStore } from "../model/authSessionStore";
import {
  decodeAccountProfile,
  decodeAccountSessions,
  type AccountProfile,
  type AccountSession,
} from "./accountContract";

export function getAccountProfile(signal?: AbortSignal): Promise<AccountProfile> {
  return appHttpClient.getData<AccountProfile>("/api/v1/account/profile", {
    signal,
    decode: decodeAccountProfile,
    metadata: { operationName: "account.profile.get" },
  });
}

export async function updateAccountProfile(displayName: string): Promise<AccountProfile> {
  const profile = await appHttpClient.putData<AccountProfile, { readonly displayName: string }>(
    "/api/v1/account/profile",
    { displayName },
    {
      decode: decodeAccountProfile,
      metadata: { operationName: "account.profile.update", replay: "deny" },
    },
  );
  const session = authSessionStore.getSnapshot().session;
  if (session) {
    authSessionStore.updateUser({
      ...session.user,
      displayName: profile.displayName,
      email: profile.email,
    });
  }
  return profile;
}

export function listAccountSessions(signal?: AbortSignal): Promise<readonly AccountSession[]> {
  return appHttpClient.getData<readonly AccountSession[]>("/api/v1/account/sessions", {
    signal,
    decode: decodeAccountSessions,
    metadata: { operationName: "account.sessions.list" },
  });
}

export async function revokeAccountSession(sessionId: string, isCurrent: boolean): Promise<void> {
  await appHttpClient.deleteData<void>(
    `/api/v1/account/sessions/${encodeURIComponent(sessionId)}`,
    {
      metadata: { operationName: "account.sessions.revoke", replay: "deny" },
    },
  );
  if (isCurrent) authSessionStore.clear();
}
