import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { LilyForm } from "@lily_platform/lily_ui/ui/forms";

import type { useEmployeeEditor } from "../hooks/useEmployeeEditor";

interface EmployeeEditorDialogProps {
  readonly id: string;
  readonly editor: ReturnType<typeof useEmployeeEditor>;
  readonly t: (key: string) => string;
}

export function EmployeeEditorDialog({ id, editor, t }: EmployeeEditorDialogProps) {
  return (
    <Dialog
      id={id}
      open={editor.open}
      fullWidth
      maxWidth="sm"
      dialogTitle={
        editor.selected ? t("app:employees.detailTitle") : t("app:employees.createTitle")
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
            bindings={editor.bindings}
            controller={editor.controller}
            disabled={!editor.canManage || editor.busy}
            onSubmit={editor.submit}
            onSubmitInvalid={editor.clearError}
            onSubmitError={editor.handleSubmitError}
          />
        </Stack>
      }
      actions={
        <Stack id={`${id}.actions`} direction="row" spacing={1}>
          {editor.selected && editor.canManage && (
            <Button
              id={`${id}.activation`}
              color={editor.selected.isActive ? "warning" : "success"}
              disabled={editor.busy}
              onClick={() => void editor.changeActivation()}
            >
              {editor.selected.isActive ? t("app:common.deactivate") : t("app:common.activate")}
            </Button>
          )}
          <Button id={`${id}.close`} disabled={editor.busy} onClick={editor.close}>
            {t("app:common.close")}
          </Button>
          {editor.canManage && (
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
