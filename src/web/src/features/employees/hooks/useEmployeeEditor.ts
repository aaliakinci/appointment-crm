import {
  createLilyFormController,
  useLilyFormStatus,
  type LilyFormBindings,
} from "@lily_platform/lily_ui/ui/forms";
import { useMemo, useState } from "react";

import type { ServiceOffering } from "@/features/services/catalog";
import { mapApiValidationError } from "@/shared/forms";

import {
  createEmployee,
  setEmployeeActive,
  setEmployeeServices,
  updateEmployee,
} from "../api/employeeApi";
import type { Employee, EmployeeUserOption } from "../api/employeeContract";
import {
  createEmployeeFormDefinition,
  toCreateEmployeeInput,
  toEmployeeFormValues,
  toEmployeeInput,
  type EmployeeFormValues,
} from "../model/employeeForm";

interface UseEmployeeEditorOptions {
  readonly canManage: boolean;
  readonly onSaved: () => void;
  readonly serviceOptions: readonly ServiceOffering[];
  readonly t: (key: string) => string;
  readonly userOptions: readonly EmployeeUserOption[];
}

export function useEmployeeEditor({
  canManage,
  onSaved,
  serviceOptions,
  t,
  userOptions,
}: UseEmployeeEditorOptions) {
  const [open, setOpen] = useState(false);
  const [selected, setSelected] = useState<Employee | null>(null);
  const [revision, setRevision] = useState(0);
  const [mutationPending, setMutationPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const definition = useMemo(() => createEmployeeFormDefinition(t), [t]);
  const controller = useMemo(() => createLilyFormController<EmployeeFormValues>(), []);
  const formSubmitting = useLilyFormStatus(controller, (status) => status.isSubmitting);
  const initialValues = useMemo(() => toEmployeeFormValues(selected), [selected]);
  const availableUsers = useMemo(
    () => userOptions.filter((option) => !option.isLinked || option.userId === selected?.userId),
    [selected?.userId, userOptions],
  );
  const bindings = useMemo<LilyFormBindings<EmployeeFormValues>>(
    () => ({
      userId: {
        visible: canManage,
        options: [
          { id: "none", value: "", label: t("app:employees.noUser") },
          ...availableUsers.map((option) => ({
            id: option.userId,
            value: option.userId,
            label: `${option.displayName} — ${option.role}`,
          })),
        ],
      },
      serviceIds: {
        options: serviceOptions.map((service) => ({
          id: service.id,
          value: service.id,
          label: service.name,
          disabled: !service.isActive,
        })),
      },
    }),
    [availableUsers, canManage, serviceOptions, t],
  );
  const busy = formSubmitting || mutationPending;

  function openCreate() {
    setSelected(null);
    setRevision((value) => value + 1);
    setError(null);
    setOpen(true);
  }

  function openDetail(employee: Employee) {
    setSelected(employee);
    setRevision((value) => value + 1);
    setError(null);
    setOpen(true);
  }

  async function submit(values: EmployeeFormValues) {
    setError(null);
    try {
      if (selected) {
        await updateEmployee(selected.id, toEmployeeInput(values));
        await setEmployeeServices(selected.id, values.serviceIds);
      } else {
        await createEmployee(toCreateEmployeeInput(values));
      }
      setOpen(false);
      onSaved();
    } catch (submitError) {
      const invalid = mapApiValidationError<EmployeeFormValues>(submitError, [
        "userId",
        "name",
        "email",
        "phone",
        "serviceIds",
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
      await setEmployeeActive(selected.id, !selected.isActive);
      setOpen(false);
      onSaved();
    } catch {
      setError(t("app:employees.activationError"));
    } finally {
      setMutationPending(false);
    }
  }

  return {
    bindings,
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
    handleSubmitError: () => setError(t("app:employees.saveError")),
  };
}
