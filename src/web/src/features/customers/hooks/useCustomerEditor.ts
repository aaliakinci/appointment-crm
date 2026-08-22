import { createLilyFormController, useLilyFormStatus } from "@lily_platform/lily_ui/ui/forms";
import { useMemo, useState } from "react";

import { mapApiValidationError } from "@/shared/forms";

import { archiveCustomer, createCustomer, updateCustomer } from "../api/customerApi";
import type { Customer } from "../api/customerContract";
import {
  createCustomerFormDefinition,
  toCustomerFormValues,
  toCustomerInput,
  type CustomerFormValues,
} from "../model/customerForm";

interface UseCustomerEditorOptions {
  readonly canManage: boolean;
  readonly onSaved: () => void;
  readonly t: (key: string) => string;
}

export function useCustomerEditor({ canManage, onSaved, t }: UseCustomerEditorOptions) {
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<Customer | null>(null);
  const [revision, setRevision] = useState(0);
  const [mutationPending, setMutationPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const definition = useMemo(() => createCustomerFormDefinition(t), [t]);
  const controller = useMemo(() => createLilyFormController<CustomerFormValues>(), []);
  const formSubmitting = useLilyFormStatus(controller, (status) => status.isSubmitting);
  const initialValues = useMemo(() => toCustomerFormValues(selected), [selected]);
  const archived = selected?.archivedAtUtc != null;
  const editable = canManage && !archived;
  const busy = formSubmitting || mutationPending;

  function openCreate() {
    setSelected(null);
    setRevision((value) => value + 1);
    setError(null);
    setOpen(true);
  }

  function openDetail(customer: Customer) {
    setSelected(customer);
    setRevision((value) => value + 1);
    setError(null);
    setOpen(true);
  }

  async function submit(values: CustomerFormValues) {
    setError(null);
    try {
      const input = toCustomerInput(values);
      if (selected) {
        await updateCustomer(selected.id, input);
      } else {
        await createCustomer(input);
      }
      setOpen(false);
      onSaved();
    } catch (submitError) {
      const invalid = mapApiValidationError<CustomerFormValues>(submitError, [
        "name",
        "email",
        "phone",
        "notes",
      ]);
      if (invalid) {
        return invalid;
      }
      throw submitError;
    }
  }

  async function archive() {
    if (!selected) {
      return;
    }
    setMutationPending(true);
    setError(null);
    try {
      await archiveCustomer(selected.id);
      setOpen(false);
      onSaved();
    } catch {
      setError(t("app:customers.archiveError"));
    } finally {
      setMutationPending(false);
    }
  }

  return {
    archived,
    busy,
    canManage,
    controller,
    definition,
    editable,
    error,
    formSubmitting,
    initialValues,
    mutationPending,
    open,
    revision,
    selected,
    archive,
    openCreate,
    openDetail,
    submit,
    close: () => !busy && setOpen(false),
    clearError: () => setError(null),
    handleSubmitError: () => setError(t("app:customers.saveError")),
  };
}
