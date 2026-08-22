import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import { useEffect, useState } from "react";

import { useAppTranslation } from "@/i18n";

import type { TenantOption } from "../api/authContract";
import { useAuth } from "../model/authContext";

export function useAccountSession() {
  const navigate = useLilyNavigate();
  const { t } = useAppTranslation();
  const auth = useAuth();
  const session = auth.session;
  const [tenants, setTenants] = useState<readonly TenantOption[]>([]);
  const [selectedTenantId, setSelectedTenantId] = useState(session?.activeTenant.id ?? "");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const controller = new AbortController();
    void auth
      .listAvailableTenants(controller.signal)
      .then((availableTenants) => {
        if (!controller.signal.aborted) {
          setTenants(availableTenants);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setError(t("app:account.tenantLoadError"));
        }
      });
    return () => controller.abort();
  }, [auth, t]);

  async function changeTenant() {
    if (!session || selectedTenantId === session.activeTenant.id) {
      return;
    }

    setBusy(true);
    setError(null);
    try {
      await auth.switchTenant(selectedTenantId);
    } catch {
      setSelectedTenantId(session.activeTenant.id);
      setError(t("app:account.switchError"));
    } finally {
      setBusy(false);
    }
  }

  async function endSession(revokeAll: boolean) {
    setBusy(true);
    setError(null);
    try {
      if (revokeAll) {
        await auth.revokeAllSessions();
      } else {
        await auth.logout();
      }
      await navigate("/login");
    } catch {
      setError(t("app:account.logoutError"));
    } finally {
      setBusy(false);
    }
  }

  return {
    busy,
    error,
    session,
    tenants,
    selectedTenantId,
    setSelectedTenantId,
    changeTenant,
    endSession,
  };
}
