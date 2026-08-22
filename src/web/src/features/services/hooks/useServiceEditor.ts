import { createLilyFormController, useLilyFormStatus } from "@lily_platform/lily_ui/ui/forms";
import { useMemo, useState } from "react";

import { mapApiValidationError } from "@/shared/forms";

import { createService, setServiceActive, updateService } from "../api/serviceApi";
import type { ServiceOffering } from "../api/serviceContract";
import {
  createServiceFormDefinition,
  toServiceFormValues,
  toServiceInput,
  type ServiceFormValues,
} from "../model/serviceForm";

interface UseServiceEditorOptions {
  readonly canManage: boolean;
  readonly currency: string;
  readonly onSaved: () => void;
  readonly t: (key: string) => string;
}

export function useServiceEditor({ canManage, currency, onSaved, t }: UseServiceEditorOptions) {
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<ServiceOffering | null>(null);
  const [revision, setRevision] = useState(0);
  const [mutationPending, setMutationPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const definition = useMemo(() => createServiceFormDefinition(t, currency), [currency, t]);
  const controller = useMemo(() => createLilyFormController<ServiceFormValues>(), []);
  const formSubmitting = useLilyFormStatus(controller, (status) => status.isSubmitting);
  const initialValues = useMemo(
    () => toServiceFormValues(selected, currency),
    [currency, selected],
  );
  const busy = formSubmitting || mutationPending;

  function openCreate() {
    setSelected(null);
    setRevision((value) => value + 1);
    setError(null);
    setOpen(true);
  }

  function openDetail(service: ServiceOffering) {
    setSelected(service);
    setRevision((value) => value + 1);
    setError(null);
    setOpen(true);
  }

  async function submit(values: ServiceFormValues) {
    setError(null);
    try {
      const input = toServiceInput(values);
      if (selected) {
        await updateService(selected.id, input);
      } else {
        await createService(input);
      }
      setOpen(false);
      onSaved();
    } catch (submitError) {
      const invalid = mapApiValidationError<ServiceFormValues>(submitError, [
        "name",
        "durationMinutes",
        "price",
        "currency",
      ]);
      if (invalid) {
        return invalid;
      }
      throw submitError;
    }
  }

  async function changeActivation() {
    if (!selected) {
      return;
    }
    setMutationPending(true);
    setError(null);
    try {
      await setServiceActive(selected.id, !selected.isActive);
      setOpen(false);
      onSaved();
    } catch {
      setError(t("app:services.activationError"));
    } finally {
      setMutationPending(false);
    }
  }

  return {
    busy,
    canManage,
    controller,
    definition,
    error,
    formSubmitting,
    initialValues,
    mutationPending,
    open,
    revision,
    selected,
    changeActivation,
    openCreate,
    openDetail,
    submit,
    close: () => !busy && setOpen(false),
    clearError: () => setError(null),
    handleSubmitError: () => setError(t("app:services.saveError")),
  };
}
