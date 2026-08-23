import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { LilyForm } from "@lily_platform/lily_ui/ui/forms";

import type { useCustomerEditor } from "../hooks/useCustomerEditor";
import type { useCustomerAppointmentHistory } from "../hooks/useCustomerAppointmentHistory";
import { CustomerAppointmentHistory } from "./CustomerAppointmentHistory";

interface CustomerEditorDialogProps {
  readonly id: string;
  readonly editor: ReturnType<typeof useCustomerEditor>;
  readonly history: ReturnType<typeof useCustomerAppointmentHistory>;
  readonly t: (key: string) => string;
}

export function CustomerEditorDialog({ id, editor, history, t }: CustomerEditorDialogProps) {
  return (
    <Dialog
      id={id}
      open={editor.open}
      fullWidth
      maxWidth={editor.selected ? "md" : "sm"}
      dialogTitle={
        editor.selected ? t("app:customers.detailTitle") : t("app:customers.createTitle")
      }
      onOpenChange={(open) => !open && editor.close()}
      content={
        <Stack id={`${id}.fields`} spacing={2} sx={{ pt: 1 }}>
          {editor.error && (
            <Alert id={`${id}.error`} severity="error">
              {editor.error}
            </Alert>
          )}
          <LilyForm
            definition={editor.definition}
            instanceId={`${id}.form.${editor.revision}`}
            initialValues={editor.initialValues}
            controller={editor.controller}
            disabled={!editor.editable || editor.busy}
            onSubmit={editor.submit}
            onSubmitInvalid={editor.clearError}
            onSubmitError={editor.handleSubmitError}
          />
          {editor.selected && (
            <CustomerAppointmentHistory id={`${id}.appointments`} history={history} t={t} />
          )}
        </Stack>
      }
      actions={
        <Stack id={`${id}.actions`} direction="row" spacing={1}>
          {editor.selected && editor.editable && (
            <Button
              id={`${id}.archive`}
              color="warning"
              disabled={editor.busy}
              onClick={() => void editor.archive()}
            >
              {t("app:customers.archive")}
            </Button>
          )}
          <Button id={`${id}.close`} disabled={editor.busy} onClick={editor.close}>
            {t("app:common.close")}
          </Button>
          {editor.canManage && !editor.archived && (
            <Button
              id={`${id}.save`}
              variant="contained"
              loading={editor.formSubmitting}
              disabled={editor.mutationPending}
              onClick={() => void editor.controller.submit()}
            >
              {t("app:common.save")}
            </Button>
          )}
        </Stack>
      }
    />
  );
}
