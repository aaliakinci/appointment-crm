import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import { useAppTranslation } from "@/i18n";

import { useAccountSession } from "../hooks/useAccountSession";
import { useAccountProfile } from "../hooks/useAccountProfile";
import { AccountProfilePanel } from "./AccountProfilePanel";
import { ActiveSessionsPanel } from "./ActiveSessionsPanel";
import { WorkspaceShell } from "./WorkspaceShell";

interface AccountFeatureProps {
  readonly id: string;
}

export function AccountFeature({ id }: AccountFeatureProps) {
  const { t } = useAppTranslation();
  const account = useAccountSession();
  const accountProfile = useAccountProfile();
  const session = account.session;
  if (!session) {
    return null;
  }

  return (
    <WorkspaceShell id={`${id}.shell`} activePath="/account">
      <Stack id={`${id}.content`} spacing={4} sx={{ maxWidth: 920 }}>
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

        <AccountProfilePanel id={`${id}.profile`} account={accountProfile} t={t} />

        {account.error && (
          <Alert id={`${id}.error`} severity="error">
            {account.error}
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

            {account.tenants.length > 1 && (
              <Stack id={`${id}.tenantSwitch`} direction={{ xs: "column", sm: "row" }} spacing={2}>
                <Select
                  id={`${id}.tenantSelect`}
                  label={t("app:account.tenant")}
                  value={account.selectedTenantId}
                  options={account.tenants.map((tenant) => ({
                    id: tenant.id,
                    value: tenant.id,
                    label: `${tenant.name} — ${tenant.role}`,
                  }))}
                  onValueChange={(value) => account.setSelectedTenantId(String(value))}
                  fullWidth
                  disabled={account.busy}
                />
                <Button
                  id={`${id}.switchTenant`}
                  variant="outlined"
                  disabled={account.busy || account.selectedTenantId === session.activeTenant.id}
                  onClick={() => void account.changeTenant()}
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

        <ActiveSessionsPanel id={`${id}.sessions`} account={accountProfile} t={t} />

        <Stack id={`${id}.actions`} direction={{ xs: "column", sm: "row" }} spacing={2}>
          <Button
            id={`${id}.logout`}
            variant="contained"
            disabled={account.busy}
            onClick={() => void account.endSession(false)}
          >
            {t("app:account.logout")}
          </Button>
          <Button
            id={`${id}.revokeAll`}
            variant="outlined"
            color="warning"
            disabled={account.busy}
            onClick={() => void account.endSession(true)}
          >
            {t("app:account.revokeAll")}
          </Button>
        </Stack>
      </Stack>
    </WorkspaceShell>
  );
}
