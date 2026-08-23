import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Dialog } from "@lily_platform/lily_ui/ui/atoms/Dialog";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { LilyForm } from "@lily_platform/lily_ui/ui/forms";

import type { useAppointmentReschedule } from "../hooks/useAppointmentReschedule";
import { AvailabilitySlotPicker } from "./AvailabilitySlotPicker";

interface AppointmentRescheduleDialogProps {
  readonly id: string;
  readonly reschedule: ReturnType<typeof useAppointmentReschedule>;
  readonly t: (key: string) => string;
}

export function AppointmentRescheduleDialog({
  id,
  reschedule,
  t,
}: AppointmentRescheduleDialogProps) {
  return (
    <Dialog
      id={id}
      open={reschedule.open}
      fullWidth
      maxWidth="sm"
      dialogTitle={t("app:appointments.rescheduleTitle")}
      onOpenChange={(open) => !open && reschedule.close()}
      content={
        <Stack id={`${id}.content`} spacing={2} sx={{ pt: 1 }}>
          {reschedule.error && (
            <Alert id={`${id}.error`} severity="error">
              {reschedule.error}
            </Alert>
          )}
          <LilyForm
            definition={reschedule.definition}
            instanceId={`${id}.form.${reschedule.revision}`}
            controller={reschedule.controller}
            effects={reschedule.effects}
            disabled={reschedule.formSubmitting}
            onSubmit={reschedule.submit}
            onSubmitError={({ error }) => reschedule.handleSubmitError(error)}
          />
          <AvailabilitySlotPicker
            id={`${id}.slots`}
            availability={reschedule.availability}
            selectedSlot={reschedule.selectedSlot}
            onSelect={reschedule.selectSlot}
            t={t}
          />
        </Stack>
      }
      actions={
        <Stack id={`${id}.actions`} direction="row" spacing={1}>
          <Button
            id={`${id}.close`}
            disabled={reschedule.formSubmitting}
            onClick={reschedule.close}
          >
            {t("app:common.close")}
          </Button>
          <Button
            id={`${id}.submit`}
            variant="contained"
            loading={reschedule.formSubmitting}
            onClick={() => void reschedule.controller.submit()}
          >
            {reschedule.selectedSlot
              ? t("app:appointments.reschedule")
              : t("app:appointments.findAvailability")}
          </Button>
        </Stack>
      }
    />
  );
}
