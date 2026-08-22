import {
  createLilyFormController,
  useLilyFormStatus,
  type LilyFormBindings,
} from "@lily_platform/lily_ui/ui/forms";
import { useEffect, useMemo, useRef, useState } from "react";

import type { Employee } from "@/features/employees/catalog";
import type { ServiceOffering } from "@/features/services/catalog";

import { getAvailability } from "../api/schedulingApi";
import type { Availability } from "../api/schedulingContract";
import {
  createAvailabilityDefinition,
  type AvailabilityFormValues,
} from "../model/availabilityForm";
import { schedulingErrorMessage } from "../model/schedulingErrors";

interface UseAvailabilityOptions {
  readonly employees: readonly Employee[];
  readonly services: readonly ServiceOffering[];
  readonly t: (key: string) => string;
  readonly today: string;
}

export function useAvailability({ employees, services, t, today }: UseAvailabilityOptions) {
  const activeRequest = useRef<AbortController | null>(null);
  const [availability, setAvailability] = useState<Availability | null>(null);
  const [error, setError] = useState<string | null>(null);
  const controller = useMemo(() => createLilyFormController<AvailabilityFormValues>(), []);
  const formStatus = useLilyFormStatus(controller, (status) => status);
  const definition = useMemo(() => createAvailabilityDefinition(t, today), [t, today]);
  const bindings = useMemo<LilyFormBindings<AvailabilityFormValues>>(
    () => ({
      employeeId: {
        options: employees
          .filter((employee) => employee.isActive)
          .map((employee) => ({ id: employee.id, value: employee.id, label: employee.name })),
      },
      serviceId: {
        options: services
          .filter((service) => service.isActive)
          .map((service) => ({ id: service.id, value: service.id, label: service.name })),
      },
    }),
    [employees, services],
  );

  useEffect(() => () => activeRequest.current?.abort(), []);

  async function submit(values: AvailabilityFormValues) {
    activeRequest.current?.abort();
    const abortController = new AbortController();
    activeRequest.current = abortController;
    setError(null);
    setAvailability(null);
    try {
      const result = await getAvailability(
        values.date,
        values.employeeId,
        values.serviceId,
        abortController.signal,
      );
      if (!abortController.signal.aborted) setAvailability(result);
    } catch (submitError) {
      if (abortController.signal.aborted) return;
      throw submitError;
    }
  }

  return {
    availability,
    error,
    controller,
    formStatus,
    definition,
    bindings,
    submit,
    handleSubmitError: (submitError: unknown) =>
      setError(schedulingErrorMessage(submitError, t, "app:scheduling.availabilityError")),
  };
}
