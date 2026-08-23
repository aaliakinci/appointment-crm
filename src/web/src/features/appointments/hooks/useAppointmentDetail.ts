import { useEffect, useState } from "react";

import { getAppointment, transitionAppointment } from "../api/appointmentApi";
import type {
  Appointment,
  AppointmentDetail,
  AppointmentScope,
  AppointmentTransition,
} from "../api/appointmentContract";
import { appointmentErrorMessage } from "../model/appointmentErrors";

interface UseAppointmentDetailOptions {
  readonly onChanged: () => void;
  readonly scope: AppointmentScope;
  readonly t: (key: string) => string;
}

export function useAppointmentDetail({ onChanged, scope, t }: UseAppointmentDetailOptions) {
  const [selected, setSelected] = useState<Appointment | null>(null);
  const [detail, setDetail] = useState<AppointmentDetail | null>(null);
  const [loading, setLoading] = useState(false);
  const [mutationPending, setMutationPending] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!selected) return;
    const controller = new AbortController();
    void getAppointment(scope, selected.id, controller.signal)
      .then((value) => {
        if (!controller.signal.aborted) setDetail(value);
      })
      .catch((loadError: unknown) => {
        if (!controller.signal.aborted) {
          setError(appointmentErrorMessage(loadError, t, "app:appointments.detailLoadError"));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [scope, selected, t]);

  async function transition(action: AppointmentTransition) {
    if (!detail) return;
    setMutationPending(true);
    setError(null);
    try {
      const updated = await transitionAppointment(scope, detail.appointment.id, action, {
        expectedRevision: detail.appointment.revision,
        reason: null,
      });
      setDetail(updated);
      onChanged();
    } catch (transitionError) {
      setError(appointmentErrorMessage(transitionError, t, "app:appointments.transitionError"));
    } finally {
      setMutationPending(false);
    }
  }

  return {
    detail,
    error,
    loading,
    mutationPending,
    open: selected !== null,
    selected,
    openDetail: (appointment: Appointment) => {
      setDetail(null);
      setLoading(true);
      setError(null);
      setSelected(appointment);
    },
    close: () => !mutationPending && setSelected(null),
    transition,
    updateDetail: (value: AppointmentDetail) => {
      setDetail(value);
      onChanged();
    },
  };
}
