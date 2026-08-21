import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Container } from "@lily_platform/lily_ui/ui/atoms/Container";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import type { PropsWithChildren } from "react";

interface PublicShellProps extends PropsWithChildren {
  readonly id: string;
  readonly activePath: "/" | "/login";
  readonly brandLabel: string;
  readonly statusLabel: string;
  readonly loginLabel: string;
  readonly skipToContentLabel: string;
  readonly portfolioNotice: string;
  readonly onNavigate: (path: "/" | "/login") => void;
}

export function PublicShell({
  id,
  activePath,
  brandLabel,
  children,
  loginLabel,
  onNavigate,
  portfolioNotice,
  skipToContentLabel,
  statusLabel,
}: PublicShellProps) {
  const mainId = `${id}.main`;

  return (
    <Box id={id} sx={{ minHeight: "100vh", bgcolor: "background.default" }}>
      <a id={`${id}.skipLink`} className="skip-link" href={`#${mainId}`}>
        {skipToContentLabel}
      </a>
      <Box
        id={`${id}.header`}
        component="header"
        sx={{ borderBottom: 1, borderColor: "divider", bgcolor: "background.paper" }}
      >
        <Container id={`${id}.headerContainer`} maxWidth="lg">
          <Stack
            id={`${id}.headerLayout`}
            direction={{ xs: "column", sm: "row" }}
            spacing={2}
            sx={{ py: 2, alignItems: { xs: "stretch", sm: "center" } }}
          >
            <Box id={`${id}.brand`} sx={{ flex: 1 }}>
              <Typography id={`${id}.brandName`} component="span" variant="h6">
                {brandLabel}
              </Typography>
              <Typography
                id={`${id}.portfolioNotice`}
                component="span"
                variant="body2"
                sx={{ ml: 1.5, color: "text.secondary" }}
              >
                {portfolioNotice}
              </Typography>
            </Box>
            <Stack id={`${id}.navigation`} component="nav" direction="row" spacing={1}>
              <Button
                id={`${id}.statusLink`}
                variant={activePath === "/" ? "contained" : "text"}
                onClick={() => onNavigate("/")}
              >
                {statusLabel}
              </Button>
              <Button
                id={`${id}.loginLink`}
                variant={activePath === "/login" ? "contained" : "text"}
                onClick={() => onNavigate("/login")}
              >
                {loginLabel}
              </Button>
            </Stack>
          </Stack>
        </Container>
      </Box>
      <Container id={mainId} component="main" maxWidth="lg" sx={{ py: { xs: 5, md: 8 } }}>
        {children}
      </Container>
    </Box>
  );
}
