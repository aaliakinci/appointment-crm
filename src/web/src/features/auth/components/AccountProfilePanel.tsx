import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Progress } from "@lily_platform/lily_ui/ui/atoms/Progress";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { LilyForm } from "@lily_platform/lily_ui/ui/forms";

import type { useAccountProfile } from "../hooks/useAccountProfile";

interface AccountProfilePanelProps {
  readonly account: ReturnType<typeof useAccountProfile>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function AccountProfilePanel({ account, id, t }: AccountProfilePanelProps) {
  return (
    <Paper id={id} variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
      <Stack id={`${id}.content`} spacing={2}>
        <Typography id={`${id}.title`} component="h2" variant="h5">
          {t("app:account.profileTitle")}
        </Typography>
        {account.loading && <Progress id={`${id}.loading`} />}
        {account.error && (
          <Alert id={`${id}.error`} severity="error">
            {account.error}
          </Alert>
        )}
        {account.profile && (
          <>
            <Typography id={`${id}.email`} variant="body2" color="text.secondary">
              {account.profile.email}
            </Typography>
            <LilyForm
              definition={account.definition}
              instanceId={`${id}.form.${account.profile.updatedAtUtc}`}
              initialValues={account.initialValues}
              controller={account.controller}
              disabled={account.formSubmitting}
              onSubmit={account.submit}
              onSubmitInvalid={account.clearError}
              onSubmitError={account.handleSubmitError}
            />
            <Button
              id={`${id}.save`}
              variant="contained"
              loading={account.formSubmitting}
              onClick={() => void account.controller.submit()}
              sx={{ alignSelf: "flex-start" }}
            >
              {t("app:account.saveProfile")}
            </Button>
          </>
        )}
      </Stack>
    </Paper>
  );
}
