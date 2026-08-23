import {
  createLilyFormController,
  useLilyFormStatus,
  type LilyFormBindings,
  type LilyFormEffects,
} from "@lily_platform/lily_ui/ui/forms";
import { useMemo, useState } from "react";

import type { Customer } from "@/features/customers/catalog";
import type { Employee } from "@/features/employees/catalog";
import { getAvailability, type Availability, type AvailabilitySlot } from "@/features/scheduling";
import type { ServiceOffering } from "@/features/services/catalog";
import { mapApiValidationError } from "@/shared/forms";

import { createAppointment } from "../api/appointmentApi";
import {
  createAppointmentFormDefinition,
  type AppointmentFormValues,
} from "../model/appointmentForm";
import { appointmentErrorMessage } from "../model/appointmentErrors";

interface UseAppointmentEditorOptions {
  readonly customers: readonly Customer[];
  readonly employees: readonly Employee[];
  readonly onSaved: () => void;
  readonly services: readonly ServiceOffering[];
  readonly t: (key: string) => string;
  readonly today: string;
}

export function useAppointmentEditor({
  customers,
  employees,
  onSaved,
  services,
  t,
  today,
}: UseAppointmentEditorOptions) {
  const [open, setOpen] = useState(false);
  const [revision, setRevision] = useState(0);
  const [serviceId, setServiceId] = useState("");
  const [availability, setAvailability] = useState<Availability | null>(null);
  const [selectedSlot, setSelectedSlot] = useState<AvailabilitySlot | null>(null);
  const [error, setError] = useState<string | null>(null);
  const controller = useMemo(() => createLilyFormController<AppointmentFormValues>(), []);
  const formSubmitting = useLilyFormStatus(controller, (status) => status.isSubmitting);
  const definition = useMemo(() => createAppointmentFormDefinition(t, today), [t, today]);
  const eligibleEmployees = useMemo(
    () =>
      employees.filter(
        (employee) => !serviceId || employee.services.some((service) => service.id === serviceId),
      ),
    [employees, serviceId],
  );
  const bindings = useMemo<LilyFormBindings<AppointmentFormValues>>(
    () => ({
      customerId: {
        options: customers.map((customer) => ({
          id: customer.id,
          value: customer.id,
          label: customer.name,
        })),
      },
      serviceId: {
        options: services.map((service) => ({
          id: service.id,
          value: service.id,
          label: `${service.name} · ${service.durationMinutes} ${t("app:services.minutes")}`,
        })),
      },
      employeeId: {
        disabled: !serviceId,
        helperText: !serviceId ? t("app:appointments.selectServiceFirst") : undefined,
        options: eligibleEmployees.map((employee) => ({
          id: employee.id,
          value: employee.id,
          label: employee.name,
        })),
      },
    }),
    [customers, eligibleEmployees, serviceId, services, t],
  );
  const effects = useMemo<LilyFormEffects<AppointmentFormValues>>(
    () => ({
      serviceId: {
        onChange: ({ value, form }) => {
          setServiceId(value);
          form.resetField("employeeId");
          clearAvailability();
        },
      },
      employeeId: { onChange: clearAvailability },
      date: { onChange: clearAvailability },
    }),
    [],
  );

  function clearAvailability() {
    setAvailability(null);
    setSelectedSlot(null);
    setError(null);
  }

  function openCreate() {
    setRevision((value) => value + 1);
    setServiceId("");
    clearAvailability();
    setOpen(true);
  }

  async function submit(values: AppointmentFormValues) {
    setError(null);
    if (!selectedSlot) {
      setAvailability(await getAvailability(values.date, values.employeeId, values.serviceId));
      return;
    }

    try {
      await createAppointment({
        customerId: values.customerId,
        employeeId: values.employeeId,
        serviceId: values.serviceId,
        startsAtUtc: selectedSlot.startUtc,
        notes: values.notes.trim() || null,
      });
      setOpen(false);
      onSaved();
    } catch (submitError) {
      const invalid = mapApiValidationError<AppointmentFormValues>(submitError, [
        "customerId",
        "employeeId",
        "serviceId",
        "date",
        "notes",
      ]);
      if (invalid) return invalid;
      throw submitError;
    }
  }

  return {
    availability,
    bindings,
    controller,
    definition,
    effects,
    error,
    formSubmitting,
    open,
    revision,
    selectedSlot,
    close: () => !formSubmitting && setOpen(false),
    openCreate,
    selectSlot: (slot: AvailabilitySlot) => setSelectedSlot(slot),
    submit,
    clearError: () => setError(null),
    handleSubmitError: (submitError: unknown) => {
      setSelectedSlot(null);
      setError(appointmentErrorMessage(submitError, t, "app:appointments.saveError"));
    },
  };
}
