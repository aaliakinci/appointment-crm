import { createLilyFormController, useLilyFormStatus } from "@lily_platform/lily_ui/ui/forms";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { deleteDateOverride, listDateOverrides, putDateOverride } from "../api/schedulingApi";
import type { DateOverride } from "../api/schedulingContract";
import {
  createDateOverrideDefinition,
  type DateOverrideFormValues,
} from "../model/dateOverrideForm";
import { schedulingErrorMessage } from "../model/schedulingErrors";
import { addLocalDays } from "../model/localDate";
import { emptyPeriod, fromMinute, toMinute } from "../model/schedulePeriod";
import { useMountedRef } from "./useMountedRef";

type OverrideMode = "closed" | "open";

interface UseDateOverridesOptions {
  readonly employeeId?: string;
  readonly onDirtyChange?: (dirty: boolean) => void;
  readonly t: (key: string) => string;
  readonly today: string;
}

export function useDateOverrides({ employeeId, onDirtyChange, t, today }: UseDateOverridesOptions) {
  const mounted = useMountedRef();
  const activeLoad = useRef<AbortController | null>(null);
  const [items, setItems] = useState<readonly DateOverride[]>([]);
  const [editing, setEditing] = useState<DateOverride | null>(null);
  const [revision, setRevision] = useState(0);
  const [error, setError] = useState<string | null>(null);
  const [mode, setMode] = useState<OverrideMode>("closed");
  const [modeDirty, setModeDirty] = useState(false);
  const controller = useMemo(() => createLilyFormController<DateOverrideFormValues>(), []);
  const formStatus = useLilyFormStatus(controller, (status) => status);
  const definition = useMemo(
    () => createDateOverrideDefinition(t, today, mode === "closed"),
    [mode, t, today],
  );

  const load = useCallback(() => {
    activeLoad.current?.abort();
    const abortController = new AbortController();
    activeLoad.current = abortController;
    return listDateOverrides(today, addLocalDays(today, 90), employeeId, abortController.signal)
      .then((value) => {
        if (abortController.signal.aborted) return;
        setItems(value);
        setError(null);
      })
      .catch((loadError: unknown) => {
        if (!abortController.signal.aborted) {
          setError(schedulingErrorMessage(loadError, t, "app:scheduling.overrideLoadError"));
        }
      });
  }, [employeeId, t, today]);

  useEffect(() => {
    void load();
    return () => activeLoad.current?.abort();
  }, [load]);

  useEffect(() => {
    onDirtyChange?.(formStatus.isDirty || modeDirty);
    return () => onDirtyChange?.(false);
  }, [formStatus.isDirty, modeDirty, onDirtyChange]);

  const initialValues = useMemo<DateOverrideFormValues>(
    () => ({
      date: editing?.date ?? today,
      isClosed: mode === "closed",
      periods:
        mode === "closed"
          ? []
          : editing?.periods.length
            ? editing.periods.map((period) => ({
                dayOfWeek: "0",
                startTime: fromMinute(period.startMinute),
                endTime: fromMinute(period.endMinute),
              }))
            : [{ ...emptyPeriod, dayOfWeek: "0" }],
    }),
    [editing, mode, today],
  );

  async function submit(values: DateOverrideFormValues) {
    setError(null);
    await putDateOverride(
      values.date,
      {
        isClosed: values.isClosed,
        periods: values.isClosed
          ? []
          : values.periods.map((period) => ({
              startMinute: toMinute(period.startTime),
              endMinute: toMinute(period.endTime),
            })),
      },
      employeeId,
    );
    if (!mounted.current) return;
    resetEditor();
    await load();
  }

  async function remove(date: string) {
    setError(null);
    try {
      await deleteDateOverride(date, employeeId);
      if (!mounted.current) return;
      if (editing?.date === date) resetEditor();
      await load();
    } catch (deleteError) {
      if (mounted.current) {
        setError(schedulingErrorMessage(deleteError, t, "app:scheduling.overrideSaveError"));
      }
    }
  }

  function resetEditor() {
    setEditing(null);
    setMode("closed");
    setModeDirty(false);
    setRevision((value) => value + 1);
  }

  function changeMode(value: string | string[] | null) {
    if (value !== "closed" && value !== "open") return;
    setMode(value);
    setModeDirty(true);
    setRevision((current) => current + 1);
    setError(null);
  }

  function edit(item: DateOverride) {
    setEditing(item);
    setMode(item.isClosed ? "closed" : "open");
    setModeDirty(false);
    setRevision((value) => value + 1);
  }

  return {
    items,
    editing,
    revision,
    error,
    mode,
    isDirty: formStatus.isDirty || modeDirty,
    controller,
    formStatus,
    definition,
    initialValues,
    submit,
    remove,
    changeMode,
    edit,
    cancelEdit: resetEditor,
    handleSubmitError: (submitError: unknown) =>
      setError(schedulingErrorMessage(submitError, t, "app:scheduling.overrideSaveError")),
  };
}
