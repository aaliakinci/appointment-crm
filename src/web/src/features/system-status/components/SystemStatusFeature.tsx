import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import { useAuth } from "@/features/auth/session";
import { useAppTranslation } from "@/i18n";
import { PublicShell } from "@/shared/components";

import { useSystemReadiness } from "../hooks/useSystemReadiness";
import { StatusCard } from "./StatusCard";

interface SystemStatusFeatureProps {
  readonly id: string;
}

export function SystemStatusFeature({ id }: SystemStatusFeatureProps) {
  const navigate = useLilyNavigate();
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const readiness = useSystemReadiness();
  const { state } = readiness;
  const databaseHealthy =
    state.kind === "ready" && state.report.checks.postgresql?.status === "Healthy";
  const apiHealthy = state.kind === "ready" && state.report.status === "Healthy";

  return (
    <PublicShell
      id={`${id}.shell`}
      activePath="/"
      brandLabel={t("app:brand")}
      statusLabel={t("app:navigation.status")}
      loginLabel={session ? t("app:navigation.account") : t("app:navigation.login")}
      secondaryPath={session ? "/account" : "/login"}
      skipToContentLabel={t("app:shell.skipToContent")}
      portfolioNotice={t("app:shell.portfolioNotice")}
      onNavigate={(path) => void navigate(path)}
    >
      <Stack id={`${id}.content`} spacing={4}>
        <Box id={`${id}.heading`}>
          <Typography
            id={`${id}.eyebrow`}
            component="p"
            variant="overline"
            sx={{ color: "primary.main" }}
          >
            {t("app:status.eyebrow")}
          </Typography>
          <Typography id={`${id}.title`} component="h1" variant="h3">
            {t("app:status.title")}
          </Typography>
          <Typography
            id={`${id}.description`}
            component="p"
            sx={{ mt: 2, maxWidth: 720, color: "text.secondary" }}
          >
            {t("app:status.description")}
          </Typography>
        </Box>

        {state.kind === "error" && (
          <Alert id={`${id}.error`} severity="error">
            {t("app:status.error")}
          </Alert>
        )}

        <Stack id={`${id}.cards`} direction={{ xs: "column", md: "row" }} spacing={2}>
          <StatusCard
            id={`${id}.api`}
            label={t("app:status.api")}
            loading={state.kind === "loading"}
            healthy={apiHealthy}
            healthyLabel={t("app:status.healthy")}
            unavailableLabel={t("app:status.unavailable")}
          />
          <StatusCard
            id={`${id}.database`}
            label={t("app:status.database")}
            loading={state.kind === "loading"}
            healthy={databaseHealthy}
            healthyLabel={t("app:status.healthy")}
            unavailableLabel={t("app:status.unavailable")}
          />
        </Stack>

        {state.kind === "ready" && (
          <Typography id={`${id}.traceId`} variant="body2" sx={{ color: "text.secondary" }}>
            {t("app:status.traceId")}: {state.report.traceId}
          </Typography>
        )}

        <Box id={`${id}.actions`}>
          <Button
            id={`${id}.retry`}
            variant="outlined"
            disabled={state.kind === "loading"}
            onClick={() => void readiness.retry()}
          >
            {state.kind === "loading" ? t("app:status.loading") : t("app:status.retry")}
          </Button>
        </Box>
      </Stack>
    </PublicShell>
  );
}
