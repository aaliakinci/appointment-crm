import { useState } from "react";

import { transitionAppointment } from "../api/appointmentApi";
import type {
  Appointment,
  AppointmentScope,
  AppointmentTransition,
} from "../api/appointmentContract";
import { appointmentErrorMessage } from "../model/appointmentErrors";

interface UseAppointmentQuickActionsOptions {
  readonly onChanged: () => void;
  readonly scope: AppointmentScope;
  readonly t: (key: string) => string;
}

export function useAppointmentQuickActions({
  onChanged,
  scope,
  t,
}: UseAppointmentQuickActionsOptions) {
  const [pendingId, setPendingId] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function transition(appointment: Appointment, action: AppointmentTransition) {
    setPendingId(appointment.id);
    setError(null);
    try {
      await transitionAppointment(scope, appointment.id, action, {
        expectedRevision: appointment.revision,
        reason: null,
      });
      onChanged();
    } catch (transitionError) {
      setError(appointmentErrorMessage(transitionError, t, "app:appointments.transitionError"));
      onChanged();
    } finally {
      setPendingId(null);
    }
  }

  return { error, pendingId, transition, clearError: () => setError(null) };
}
