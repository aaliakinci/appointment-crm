import {
  createLilyFormController,
  useLilyFormStatus,
  type LilyFormEffects,
} from "@lily_platform/lily_ui/ui/forms";
import { useMemo, useState } from "react";

import { getAvailability, type Availability, type AvailabilitySlot } from "@/features/scheduling";

import { rescheduleAppointment } from "../api/appointmentApi";
import type { Appointment, AppointmentDetail } from "../api/appointmentContract";
import { localDateFromOffset } from "../model/appointmentDate";
import { appointmentErrorMessage } from "../model/appointmentErrors";
import {
  createRescheduleFormDefinition,
  type RescheduleFormValues,
} from "../model/appointmentForm";

interface UseAppointmentRescheduleOptions {
  readonly appointment: Appointment | null;
  readonly onSaved: (detail: AppointmentDetail) => void;
  readonly t: (key: string) => string;
}

export function useAppointmentReschedule({
  appointment,
  onSaved,
  t,
}: UseAppointmentRescheduleOptions) {
  const [open, setOpen] = useState(false);
  const [revision, setRevision] = useState(0);
  const [availability, setAvailability] = useState<Availability | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<AvailabilitySlot | null>(null);
  const [error, setError] = useState<string | null>(null);
  const controller = useMemo(() => createLilyFormController<RescheduleFormValues>(), []);
  const formSubmitting = useLilyFormStatus(controller, (status) => status.isSubmitting);
  const initialDate = appointment ? localDateFromOffset(appointment.localStart) : "";
  const definition = useMemo(
    () => createRescheduleFormDefinition(t, initialDate),
    [initialDate, t],
  );
  const effects = useMemo<LilyFormEffects<RescheduleFormValues>>(
    () => ({
      date: {
        onChange: () => {
          setAvailability(null);
          setSelectedSlot(null);
          setError(null);
        },
      },
    }),
    [],
  );

  async function submit(values: RescheduleFormValues) {
    if (!appointment) return;
    setError(null);
    if (!selectedSlot) {
      setAvailability(
        await getAvailability(
          values.date,
          appointment.employeeId,
          appointment.serviceId,
          appointment.id,
        ),
      );
      return;
    }

    const updated = await rescheduleAppointment(appointment.id, {
      startsAtUtc: selectedSlot.startUtc,
      expectedRevision: appointment.revision,
    });
    setOpen(false);
    onSaved(updated);
  }

  return {
    availability,
    controller,
    definition,
    effects,
    error,
    formSubmitting,
    open,
    revision,
    selectedSlot,
    openEditor: () => {
      setRevision((value) => value + 1);
      setAvailability(null);
      setSelectedSlot(null);
      setError(null);
      setOpen(true);
    },
    close: () => !formSubmitting && setOpen(false),
    selectSlot: (slot: AvailabilitySlot) => setSelectedSlot(slot),
    submit,
    handleSubmitError: (submitError: unknown) => {
      setSelectedSlot(null);
      setError(appointmentErrorMessage(submitError, t, "app:appointments.rescheduleError"));
    },
  };
}
