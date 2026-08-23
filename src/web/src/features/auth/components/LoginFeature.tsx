import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { LilyForm } from "@lily_platform/lily_ui/ui/forms";

import { useAppTranslation } from "@/i18n";
import { PublicShell } from "@/shared/components";

import { useLogin } from "../hooks/useLogin";

interface LoginFeatureProps {
  readonly id: string;
}

export function LoginFeature({ id }: LoginFeatureProps) {
  const navigate = useLilyNavigate();
  const { changeLocale, locale, t } = useAppTranslation();
  const login = useLogin();

  return (
    <PublicShell
      id={`${id}.shell`}
      activePath="/login"
      brandLabel={t("app:brand")}
      languageLabel={t("app:shell.language")}
      statusLabel={t("app:navigation.status")}
      loginLabel={t("app:navigation.login")}
      locale={locale}
      skipToContentLabel={t("app:shell.skipToContent")}
      portfolioNotice={t("app:shell.portfolioNotice")}
      onLocaleChange={(nextLocale) => void changeLocale(nextLocale)}
      onNavigate={(path) => void navigate(path)}
    >
      <Stack id={`${id}.content`} spacing={3} sx={{ maxWidth: 560 }}>
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
          <Typography
            id={`${id}.description`}
            component="p"
            sx={{ mt: 2, color: "text.secondary" }}
          >
            {t("app:login.description")}
          </Typography>
        </Box>

        {login.error && (
          <Alert id={`${id}.error`} severity="error">
            {login.error}
          </Alert>
        )}

        <LilyForm
          definition={login.definition}
          instanceId={`${id}.form`}
          initialValues={login.initialValues}
          initialValuesRevision={login.initialValuesRevision}
          reinitialize="always"
          bindings={login.bindings}
          controller={login.controller}
          disabled={login.submitting}
          onSubmit={login.submit}
          onSubmitInvalid={login.clearError}
          onSubmitError={login.handleSubmitError}
        />

        <Alert id={`${id}.security`} severity="info">
          {t("app:login.securityNotice")}
        </Alert>
      </Stack>
    </PublicShell>
  );
}
