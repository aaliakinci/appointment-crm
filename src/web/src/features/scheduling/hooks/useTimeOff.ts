import {
  createLilyFormController,
  useLilyFormStatus,
  type LilyFormBindings,
} from "@lily_platform/lily_ui/ui/forms";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import type { Employee } from "@/features/employees/catalog";

import { createTimeOff, deleteTimeOff, listTimeOff } from "../api/schedulingApi";
import type { TimeOff } from "../api/schedulingContract";
import { addLocalDays, normalizeApiTime } from "../model/localDate";
import { schedulingErrorMessage } from "../model/schedulingErrors";
import { createTimeOffDefinition, type TimeOffFormValues } from "../model/timeOffForm";
import { useMountedRef } from "./useMountedRef";

interface UseTimeOffOptions {
  readonly employees: readonly Employee[];
  readonly onDirtyChange?: (dirty: boolean) => void;
  readonly t: (key: string) => string;
  readonly timeZone: string;
  readonly today: string;
}

export function useTimeOff({ employees, onDirtyChange, t, timeZone, today }: UseTimeOffOptions) {
  const mounted = useMountedRef();
  const activeLoad = useRef<AbortController | null>(null);
  const [items, setItems] = useState<readonly TimeOff[]>([]);
  const [revision, setRevision] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const controller = useMemo(() => createLilyFormController<TimeOffFormValues>(), []);
  const formStatus = useLilyFormStatus(controller, (status) => status);
  const definition = useMemo(() => createTimeOffDefinition(t, today), [t, today]);
  const bindings = useMemo<LilyFormBindings<TimeOffFormValues>>(
    () => ({
      employeeId: {
        options: employees
          .filter((employee) => employee.isActive)
          .map((employee) => ({ id: employee.id, value: employee.id, label: employee.name })),
      },
    }),
    [employees],
  );

  const load = useCallback(() => {
    activeLoad.current?.abort();
    const abortController = new AbortController();
    activeLoad.current = abortController;
    return listTimeOff(today, addLocalDays(today, 90), undefined, abortController.signal)
      .then((value) => {
        if (abortController.signal.aborted) return;
        setItems(value);
        setError(null);
      })
      .catch((loadError: unknown) => {
        if (!abortController.signal.aborted) {
          setError(schedulingErrorMessage(loadError, t, "app:scheduling.timeOffLoadError"));
        }
      });
  }, [t, today]);

  useEffect(() => {
    void load();
    return () => activeLoad.current?.abort();
  }, [load]);

  useEffect(() => {
    onDirtyChange?.(formStatus.isDirty);
    return () => onDirtyChange?.(false);
  }, [formStatus.isDirty, onDirtyChange]);

  async function submit(values: TimeOffFormValues) {
    setError(null);
    await createTimeOff({
      employeeId: values.employeeId,
      startDate: values.startDate,
      startTime: normalizeApiTime(values.startTime),
      endDate: values.endDate,
      endTime: normalizeApiTime(values.endTime),
      timeZone,
      reason: values.reason.trim() || null,
    });
    if (!mounted.current) return;
    setRevision((value) => value + 1);
    await load();
  }

  async function remove(timeOffId: string) {
    setError(null);
    try {
      await deleteTimeOff(timeOffId);
      if (mounted.current) await load();
    } catch (deleteError) {
      if (mounted.current) {
        setError(schedulingErrorMessage(deleteError, t, "app:scheduling.timeOffSaveError"));
      }
    }
  }

  return {
    items,
    revision,
    error,
    controller,
    formStatus,
    definition,
    bindings,
    submit,
    remove,
    handleSubmitError: (submitError: unknown) =>
      setError(schedulingErrorMessage(submitError, t, "app:scheduling.timeOffSaveError")),
  };
}
