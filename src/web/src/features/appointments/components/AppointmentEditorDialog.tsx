import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

import { LocalizedLilyDateForm } from "@/shared/forms/LocalizedLilyDateForm";

import type { useAppointmentEditor } from "../hooks/useAppointmentEditor";
import { AvailabilitySlotPicker } from "./AvailabilitySlotPicker";

interface AppointmentEditorDialogProps {
  readonly editor: ReturnType<typeof useAppointmentEditor>;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function AppointmentEditorDialog({ editor, id, t }: AppointmentEditorDialogProps) {
  return (
    <Dialog
      id={id}
      open={editor.open}
      fullWidth
      maxWidth="sm"
      dialogTitle={t("app:appointments.createTitle")}
      onOpenChange={(open) => !open && editor.close()}
      content={
        <Stack id={`${id}.content`} spacing={2} sx={{ pt: 1 }}>
          {editor.error && (
            <Alert id={`${id}.error`} severity="error">
              {editor.error}
            </Alert>
          )}
          <LocalizedLilyDateForm
            definition={editor.definition}
            instanceId={`${id}.form.${editor.revision}`}
            bindings={editor.bindings}
            controller={editor.controller}
            effects={editor.effects}
            disabled={editor.formSubmitting}
            onSubmit={editor.submit}
            onSubmitInvalid={editor.clearError}
            onSubmitError={({ error }) => editor.handleSubmitError(error)}
          />
          <AvailabilitySlotPicker
            id={`${id}.slots`}
            availability={editor.availability}
            selectedSlot={editor.selectedSlot}
            onSelect={editor.selectSlot}
            t={t}
          />
        </Stack>
      }
      actions={
        <Stack id={`${id}.actions`} direction="row" spacing={1}>
          <Button id={`${id}.close`} disabled={editor.formSubmitting} onClick={editor.close}>
            {t("app:common.close")}
          </Button>
          <Button
            id={`${id}.submit`}
            variant="contained"
            loading={editor.formSubmitting}
            onClick={() => void editor.controller.submit()}
          >
            {editor.selectedSlot
              ? t("app:appointments.create")
              : t("app:appointments.findAvailability")}
          </Button>
        </Stack>
      }
    />
  );
}
