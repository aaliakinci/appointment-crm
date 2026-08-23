import { createLilyFormController, useLilyFormStatus } from "@lily_platform/lily_ui/ui/forms";
import { useEffect, useMemo, useState } from "react";

import { useAppTranslation } from "@/i18n";
import { mapApiValidationError } from "@/shared/forms";

import {
  getAccountProfile,
  listAccountSessions,
  revokeAccountSession,
  updateAccountProfile,
} from "../api/accountApi";
import type { AccountProfile, AccountSession } from "../api/accountContract";
import { authSessionStore } from "../model/authSessionStore";
import {
  createAccountProfileFormDefinition,
  toAccountProfileFormValues,
  type AccountProfileFormValues,
} from "../model/accountProfileForm";

export function useAccountProfile() {
  const { t } = useAppTranslation();
  const [profile, setProfile] = useState<AccountProfile | null>(null);
  const [sessions, setSessions] = useState<readonly AccountSession[]>([]);
  const [loading, setLoading] = useState(true);
  const [sessionPendingId, setSessionPendingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);
  const controller = useMemo(() => createLilyFormController<AccountProfileFormValues>(), []);
  const definition = useMemo(() => createAccountProfileFormDefinition(t), [t]);
  const formSubmitting = useLilyFormStatus(controller, (status) => status.isSubmitting);
  const initialValues = useMemo(() => toAccountProfileFormValues(profile), [profile]);

  useEffect(() => {
    const abortController = new AbortController();
    void Promise.all([
      getAccountProfile(abortController.signal),
      listAccountSessions(abortController.signal),
    ])
      .then(([nextProfile, nextSessions]) => {
        if (!abortController.signal.aborted) {
          setProfile(nextProfile);
          setSessions(nextSessions);
          setError(null);
        }
      })
      .catch(() => {
        if (!abortController.signal.aborted) setError(t("app:account.profileLoadError"));
      })
      .finally(() => {
        if (!abortController.signal.aborted) setLoading(false);
      });
    return () => abortController.abort();
  }, [revision, t]);

  async function submit(values: AccountProfileFormValues) {
    setError(null);
    try {
      const updated = await updateAccountProfile(values.displayName.trim());
      setProfile(updated);
    } catch (submitError) {
      const invalid = mapApiValidationError<AccountProfileFormValues>(submitError, ["displayName"]);
      if (invalid) return invalid;
      throw submitError;
    }
  }

  async function revoke(session: AccountSession) {
    setSessionPendingId(session.id);
    setError(null);
    try {
      await revokeAccountSession(session.id, session.isCurrent);
      if (!session.isCurrent) {
        setSessions((items) => items.filter((item) => item.id !== session.id));
      }
    } catch {
      setError(t("app:account.sessionRevokeError"));
    } finally {
      setSessionPendingId(null);
    }
  }

  return {
    controller,
    definition,
    error,
    formSubmitting,
    initialValues,
    loading,
    profile,
    revision,
    sessionPendingId,
    sessions,
    submit,
    revoke,
    clearError: () => setError(null),
    handleSubmitError: () => setError(t("app:account.profileSaveError")),
    reload: () => {
      setLoading(true);
      setRevision((value) => value + 1);
    },
    sessionCleared: () => authSessionStore.getSnapshot().session === null,
  };
}
