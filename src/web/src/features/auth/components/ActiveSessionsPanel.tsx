import { useLilyNavigate } from "@lily_platform/lily_ui/router";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Chip } from "@lily_platform/lily_ui/ui/atoms/Chip";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { useAccountProfile } from "../hooks/useAccountProfile";

interface ActiveSessionsPanelProps {
  readonly account: ReturnType<typeof useAccountProfile>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function ActiveSessionsPanel({ account, id, t }: ActiveSessionsPanelProps) {
  const navigate = useLilyNavigate();
  return (
    <Paper id={id} variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
      <Stack id={`${id}.content`} spacing={2}>
        <Stack
          id={`${id}.heading`}
          direction={{ xs: "column", sm: "row" }}
          spacing={1}
          sx={{ alignItems: { sm: "center" } }}
        >
          <Typography id={`${id}.title`} component="h2" variant="h5" sx={{ flex: 1 }}>
            {t("app:account.sessionsTitle")}
          </Typography>
          <Typography id={`${id}.description`} variant="body2" color="text.secondary">
            {t("app:account.sessionsDescription")}
          </Typography>
        </Stack>
        {account.sessions.map((session) => (
          <Paper id={`${id}.${session.id}`} key={session.id} variant="outlined" sx={{ p: 2 }}>
            <Stack
              id={`${id}.${session.id}.content`}
              direction={{ xs: "column", sm: "row" }}
              spacing={2}
              sx={{ alignItems: { sm: "center" } }}
            >
              <Stack id={`${id}.${session.id}.meta`} spacing={0.5} sx={{ flex: 1 }}>
                <Stack id={`${id}.${session.id}.name`} direction="row" spacing={1}>
                  <Typography id={`${id}.${session.id}.tenant`} variant="subtitle2">
                    {session.tenantName}
                  </Typography>
                  {session.isCurrent && (
                    <Chip
                      id={`${id}.${session.id}.current`}
                      size="small"
                      color="success"
                      label={t("app:account.currentSession")}
                    />
                  )}
                </Stack>
                <Typography
                  id={`${id}.${session.id}.created`}
                  variant="caption"
                  color="text.secondary"
                >
                  {t("app:account.sessionStarted")}: {formatInstant(session.createdAtUtc)} ·{" "}
                  {t("app:account.sessionExpires")}: {formatInstant(session.expiresAtUtc)}
                </Typography>
              </Stack>
              <Button
                id={`${id}.${session.id}.revoke`}
                variant="outlined"
                color={session.isCurrent ? "warning" : "primary"}
                loading={account.sessionPendingId === session.id}
                disabled={account.sessionPendingId !== null}
                onClick={() =>
                  void account.revoke(session).then(() => {
                    if (session.isCurrent && account.sessionCleared()) void navigate("/login");
                  })
                }
              >
                {t("app:account.revokeSession")}
              </Button>
            </Stack>
          </Paper>
        ))}
      </Stack>
    </Paper>
  );
}

function formatInstant(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
