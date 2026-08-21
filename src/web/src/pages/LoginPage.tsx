import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { useLilyNavigate } from "@lily_platform/lily_ui/router";

import { PublicShell } from "@/components";
import { useAppTranslation } from "@/i18n";

interface LoginPageProps {
  readonly id: string;
}

export function LoginPage({ id }: LoginPageProps) {
  const navigate = useLilyNavigate();
  const { t } = useAppTranslation();

  return (
    <PublicShell
      id={`${id}.shell`}
      activePath="/login"
      brandLabel={t("app:brand")}
      statusLabel={t("app:navigation.status")}
      loginLabel={t("app:navigation.login")}
      skipToContentLabel={t("app:shell.skipToContent")}
      portfolioNotice={t("app:shell.portfolioNotice")}
      onNavigate={(path) => {
        void navigate(path);
      }}
    >
      <Stack id={`${id}.content`} spacing={3} sx={{ maxWidth: 680 }}>
        <Box id={`${id}.heading`}>
          <Typography
            id={`${id}.eyebrow`}
            component="p"
            variant="overline"
            sx={{ color: "primary.main" }}
          >
            {t("app:login.eyebrow")}
          </Typography>
          <Typography id={`${id}.title`} component="h1" variant="h3">
            {t("app:login.title")}
          </Typography>
        </Box>
        <Alert id={`${id}.notice`} severity="info">
          {t("app:login.description")}
        </Alert>
        <Box id={`${id}.actions`}>
          <Button
            id={`${id}.back`}
            variant="contained"
            onClick={() => {
              void navigate("/");
            }}
          >
            {t("app:login.back")}
          </Button>
        </Box>
      </Stack>
    </PublicShell>
  );
}
