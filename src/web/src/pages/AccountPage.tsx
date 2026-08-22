import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { useEffect, useState } from "react";

import type { TenantOption } from "@/api";
import { WorkspaceShell } from "@/components";
import { useAppTranslation } from "@/i18n";
import { useAuth } from "@/state";

interface AccountPageProps {
  readonly id: string;
}

export function AccountPage({ id }: AccountPageProps) {
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

  if (!session) {
    return null;
  }

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

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/account">
      <Stack id={`${id}.content`} spacing={4} sx={{ maxWidth: 760 }}>
        <Box id={`${id}.heading`}>
          <Typography
            id={`${id}.eyebrow`}
            component="p"
            variant="overline"
            sx={{ color: "primary.main" }}
          >
            {t("app:account.eyebrow")}
          </Typography>
          <Typography id={`${id}.title`} component="h1" variant="h3">
            {session.user.displayName}
          </Typography>
          <Typography id={`${id}.email`} component="p" sx={{ mt: 1, color: "text.secondary" }}>
            {session.user.email}
          </Typography>
        </Box>

        {error && (
          <Alert id={`${id}.error`} severity="error">
            {error}
          </Alert>
        )}

        <Box
          id={`${id}.tenantCard`}
          sx={{ border: 1, borderColor: "divider", borderRadius: 2, p: 3 }}
        >
          <Stack id={`${id}.tenantContent`} spacing={2.5}>
            <Box id={`${id}.tenantHeading`}>
              <Typography id={`${id}.tenantName`} component="h2" variant="h5">
                {session.activeTenant.name}
              </Typography>
              <Typography id={`${id}.tenantRole`} component="p" sx={{ color: "text.secondary" }}>
                {t("app:account.role")}: {session.activeTenant.role}
              </Typography>
            </Box>

            {tenants.length > 1 && (
              <Stack id={`${id}.tenantSwitch`} direction={{ xs: "column", sm: "row" }} spacing={2}>
                <Select
                  id={`${id}.tenantSelect`}
                  label={t("app:account.tenant")}
                  value={selectedTenantId}
                  options={tenants.map((tenant) => ({
                    id: tenant.id,
                    value: tenant.id,
                    label: `${tenant.name} — ${tenant.role}`,
                  }))}
                  onValueChange={(value) => setSelectedTenantId(String(value))}
                  fullWidth
                  disabled={busy}
                />
                <Button
                  id={`${id}.switchTenant`}
                  variant="outlined"
                  disabled={busy || selectedTenantId === session.activeTenant.id}
                  onClick={() => void changeTenant()}
                >
                  {t("app:account.switchTenant")}
                </Button>
              </Stack>
            )}

            <Stack id={`${id}.permissions`} direction="row" spacing={1} sx={{ flexWrap: "wrap" }}>
              {session.activeTenant.permissions.map((permission) => (
                <Chip
                  id={`${id}.permission.${permission}`}
                  key={permission}
                  label={permission}
                  size="small"
                  sx={{ mb: 1 }}
                />
              ))}
            </Stack>
          </Stack>
        </Box>

        <Stack id={`${id}.actions`} direction={{ xs: "column", sm: "row" }} spacing={2}>
          <Button
            id={`${id}.logout`}
            variant="contained"
            disabled={busy}
            onClick={() => void endSession(false)}
          >
            {t("app:account.logout")}
          </Button>
          <Button
            id={`${id}.revokeAll`}
            variant="outlined"
            color="warning"
            disabled={busy}
            onClick={() => void endSession(true)}
          >
            {t("app:account.revokeAll")}
          </Button>
        </Stack>
      </Stack>
    </WorkspaceShell>
  );
}
