import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import { useEffect, useState } from "react";

import { getReadiness, type HealthReport } from "@/api";
import { PublicShell } from "@/components";
import { useAppTranslation } from "@/i18n";
import { useAuth } from "@/state";

interface SystemStatusPageProps {
  readonly id: string;
}

type LoadState =
  | { readonly kind: "loading" }
  | { readonly kind: "ready"; readonly report: HealthReport }
  | { readonly kind: "error" };

async function fetchReadinessState(signal?: AbortSignal): Promise<LoadState> {
  try {
    return { kind: "ready", report: await getReadiness(signal) };
  } catch {
    return signal?.aborted ? { kind: "loading" } : { kind: "error" };
  }
}

export function SystemStatusPage({ id }: SystemStatusPageProps) {
  const navigate = useLilyNavigate();
  const { t } = useAppTranslation();
  const { session } = useAuth();
  const [loadState, setLoadState] = useState<LoadState>({ kind: "loading" });

  useEffect(() => {
    const controller = new AbortController();
    void fetchReadinessState(controller.signal).then((state) => {
      if (!controller.signal.aborted) {
        setLoadState(state);
      }
    });
    return () => controller.abort();
  }, []);

  const databaseHealthy =
    loadState.kind === "ready" && loadState.report.checks.postgresql?.status === "Healthy";
  const apiHealthy = loadState.kind === "ready" && loadState.report.status === "Healthy";

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
      onNavigate={(path) => {
        void navigate(path);
      }}
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

        {loadState.kind === "error" && (
          <Alert id={`${id}.error`} severity="error">
            {t("app:status.error")}
          </Alert>
        )}

        <Stack id={`${id}.cards`} direction={{ xs: "column", md: "row" }} spacing={2}>
          <StatusCard
            id={`${id}.api`}
            label={t("app:status.api")}
            loading={loadState.kind === "loading"}
            healthy={apiHealthy}
            healthyLabel={t("app:status.healthy")}
            unavailableLabel={t("app:status.unavailable")}
          />
          <StatusCard
            id={`${id}.database`}
            label={t("app:status.database")}
            loading={loadState.kind === "loading"}
            healthy={databaseHealthy}
            healthyLabel={t("app:status.healthy")}
            unavailableLabel={t("app:status.unavailable")}
          />
        </Stack>

        {loadState.kind === "ready" && (
          <Typography id={`${id}.traceId`} variant="body2" sx={{ color: "text.secondary" }}>
            {t("app:status.traceId")}: {loadState.report.traceId}
          </Typography>
        )}

        <Box id={`${id}.actions`}>
          <Button
            id={`${id}.retry`}
            variant="outlined"
            disabled={loadState.kind === "loading"}
            onClick={() => {
              setLoadState({ kind: "loading" });
              void fetchReadinessState().then(setLoadState);
            }}
          >
            {loadState.kind === "loading" ? t("app:status.loading") : t("app:status.retry")}
          </Button>
        </Box>
      </Stack>
    </PublicShell>
  );
}

interface StatusCardProps {
  readonly id: string;
  readonly label: string;
  readonly loading: boolean;
  readonly healthy: boolean;
  readonly healthyLabel: string;
  readonly unavailableLabel: string;
}

function StatusCard({
  id,
  healthy,
  healthyLabel,
  label,
  loading,
  unavailableLabel,
}: StatusCardProps) {
  const statusLabel = loading ? "…" : healthy ? healthyLabel : unavailableLabel;

  return (
    <Box
      id={id}
      sx={{
        flex: 1,
        border: 1,
        borderColor: "divider",
        borderRadius: 2,
        bgcolor: "background.paper",
        p: 3,
      }}
    >
      <Typography id={`${id}.label`} component="h2" variant="h6">
        {label}
      </Typography>
      <Typography
        id={`${id}.status`}
        component="p"
        sx={{ mt: 1, color: loading ? "text.secondary" : healthy ? "success.main" : "error.main" }}
      >
        {statusLabel}
      </Typography>
    </Box>
  );
}
