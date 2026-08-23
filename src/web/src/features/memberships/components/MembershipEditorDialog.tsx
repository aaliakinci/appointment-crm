import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";
import { LilyForm } from "@lily_platform/lily_ui/ui/forms";

import type { useMembershipManagement } from "../hooks/useMembershipManagement";

interface MembershipEditorDialogProps {
  readonly id: string;
  readonly management: ReturnType<typeof useMembershipManagement>;
  readonly t: (key: string) => string;
}

export function MembershipEditorDialog({ id, management, t }: MembershipEditorDialogProps) {
  const selected = management.selected;
  return (
    <Dialog
      id={id}
      open={selected !== null}
      fullWidth
      maxWidth="sm"
      dialogTitle={t("app:memberships.detailTitle")}
      onOpenChange={(open) => !open && management.close()}
      content={
        selected ? (
          <Stack id={`${id}.content`} spacing={2} sx={{ pt: 1 }}>
            <Typography id={`${id}.name`} component="h2" variant="h6">
              {selected.displayName}
            </Typography>
            <Typography id={`${id}.email`} variant="body2" color="text.secondary">
              {selected.email}
            </Typography>
            <LilyForm
              definition={management.definition}
              instanceId={`${id}.form.${selected.id}.${selected.updatedAtUtc}`}
              initialValues={management.initialValues}
              bindings={management.bindings}
              controller={management.controller}
              disabled={!management.canManage || management.formSubmitting}
              onSubmit={management.submit}
              onSubmitInvalid={management.clearError}
              onSubmitError={management.handleSubmitError}
            />
          </Stack>
        ) : null
      }
      actions={
        <Stack id={`${id}.actions`} direction="row" spacing={1}>
          {selected && management.canManage && (
            <Button
              id={`${id}.activation`}
              color={selected.isActive ? "warning" : "success"}
              disabled={management.formSubmitting}
              loading={management.mutationPending}
              onClick={() => void management.toggleActive()}
            >
              {selected.isActive ? t("app:common.deactivate") : t("app:common.activate")}
            </Button>
          )}
          <Button id={`${id}.close`} onClick={management.close}>
            {t("app:common.close")}
          </Button>
          {management.canManage && (
            <Button
              id={`${id}.save`}
              variant="contained"
              loading={management.formSubmitting}
              disabled={management.mutationPending}
              onClick={() => void management.controller.submit()}
            >
              {t("app:common.save")}
            </Button>
          )}
        </Stack>
      }
    />
  );
}
