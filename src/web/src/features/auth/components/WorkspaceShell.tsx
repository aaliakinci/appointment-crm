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
import { workspaceNavigationFor, type WorkspacePath } from "../model/workspaceNavigation";

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

  const navigation = workspaceNavigationFor(session.activeTenant.permissions);
  const mainId = `${id}.main`;

  return (
    <Box id={id} sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <a
        id={`${id}.skipLink`}
        className="skip-link"
        href={`#${mainId}`}
        onClick={(event) => {
          event.preventDefault();
          document.getElementById(mainId)?.focus();
        }}
      >
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
                  {t(item.labelKey)}
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
      <Container
        id={mainId}
        component="main"
        maxWidth="xl"
        tabIndex={-1}
        sx={{ py: { xs: 4, md: 6 } }}
      >
        {children}
      </Container>
    </Box>
  );
}
