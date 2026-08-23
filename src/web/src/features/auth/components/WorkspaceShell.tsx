import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Container } from "@lily_platform/lily_ui/ui/atoms/Container";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import type { PropsWithChildren } from "react";

import { useAppTranslation } from "@/i18n";
import { LocaleSwitcher } from "@/shared/components";

import { useAuth } from "../model/authContext";

export type WorkspacePath =
  | "/account"
  | "/audit"
  | "/appointments"
  | "/customers"
  | "/dashboard"
  | "/employees"
  | "/scheduling"
  | "/services"
  | "/team";

interface WorkspaceShellProps extends PropsWithChildren {
  readonly id: string;
  readonly activePath: WorkspacePath;
  readonly onNavigate?: (path: WorkspacePath) => void;
}

export function WorkspaceShell({ activePath, children, id, onNavigate }: WorkspaceShellProps) {
  const navigate = useLilyNavigate();
  const { changeLocale, locale, t } = useAppTranslation();
  const { session } = useAuth();
  if (!session) {
    return null;
  }

  const permissions = new Set(session.activeTenant.permissions);
  const navigation = [
    permissions.has("reporting.read")
      ? { path: "/dashboard" as const, label: t("app:navigation.dashboard") }
      : null,
    permissions.has("customers.read")
      ? { path: "/customers" as const, label: t("app:navigation.customers") }
      : null,
    permissions.has("services.read")
      ? { path: "/services" as const, label: t("app:navigation.services") }
      : null,
    permissions.has("employees.read")
      ? { path: "/employees" as const, label: t("app:navigation.employees") }
      : null,
    permissions.has("appointments.read") || permissions.has("appointments.read-own")
      ? { path: "/appointments" as const, label: t("app:navigation.appointments") }
      : null,
    permissions.has("scheduling.manage")
      ? { path: "/scheduling" as const, label: t("app:navigation.scheduling") }
      : null,
    permissions.has("memberships.read")
      ? { path: "/team" as const, label: t("app:navigation.team") }
      : null,
    permissions.has("reporting.read")
      ? { path: "/audit" as const, label: t("app:navigation.audit") }
      : null,
    { path: "/account" as const, label: t("app:navigation.account") },
  ].filter((item) => item !== null);
  const mainId = `${id}.main`;

  return (
    <Box id={id} sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <a id={`${id}.skipLink`} className="skip-link" href={`#${mainId}`}>
        {t("app:shell.skipToContent")}
      </a>
      <Box
        id={`${id}.header`}
        component="header"
        sx={{ borderBottom: 1, borderColor: "divider", bgcolor: "background.paper" }}
      >
        <Container id={`${id}.headerContainer`} maxWidth="xl">
          <Stack
            id={`${id}.headerLayout`}
            direction={{ xs: "column", md: "row" }}
            spacing={2}
            sx={{ py: 2, alignItems: { xs: "stretch", md: "center" } }}
          >
            <Box id={`${id}.brand`} sx={{ minWidth: 220 }}>
              <Typography id={`${id}.brandName`} component="span" variant="h6">
                {t("app:brand")}
              </Typography>
              <Typography
                id={`${id}.tenantName`}
                component="span"
                variant="body2"
                sx={{ ml: 1.5, color: "text.secondary" }}
              >
                {session.activeTenant.name}
              </Typography>
            </Box>
            <Stack
              id={`${id}.navigation`}
              component="nav"
              direction="row"
              spacing={1}
              sx={{ flex: 1, flexWrap: "wrap" }}
            >
              {navigation.map((item) => (
                <Button
                  id={`${id}.navigation.${item.path.slice(1)}`}
                  key={item.path}
                  size="small"
                  variant={activePath === item.path ? "contained" : "text"}
                  onClick={() => {
                    if (onNavigate) {
                      onNavigate(item.path);
                      return;
                    }

                    void navigate(item.path);
                  }}
                >
                  {item.label}
                </Button>
              ))}
            </Stack>
            <LocaleSwitcher
              id={`${id}.locale`}
              label={t("app:shell.language")}
              locale={locale}
              onChange={(nextLocale) => void changeLocale(nextLocale)}
            />
          </Stack>
        </Container>
      </Box>
      <Container id={mainId} component="main" maxWidth="xl" sx={{ py: { xs: 4, md: 6 } }}>
        {children}
      </Container>
    </Box>
  );
}
