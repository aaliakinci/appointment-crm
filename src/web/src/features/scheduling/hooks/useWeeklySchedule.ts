import { createLilyFormController, useLilyFormStatus } from "@lily_platform/lily_ui/ui/forms";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import {
  getWeeklySchedule,
  putWeeklySchedule,
  restoreWeeklyInheritance,
} from "../api/schedulingApi";
import type { WeeklySchedule } from "../api/schedulingContract";
import { isScheduleVersionConflict, schedulingErrorMessage } from "../model/schedulingErrors";
import { fromMinute, toMinute } from "../model/schedulePeriod";
import {
  createWeeklyHoursDefinition,
  type WeeklyHoursFormValues,
} from "../model/weeklyScheduleForm";
import { useMountedRef } from "./useMountedRef";

interface UseWeeklyScheduleOptions {
  readonly employeeId?: string;
  readonly onDirtyChange?: (dirty: boolean) => void;
  readonly t: (key: string) => string;
}

export function useWeeklySchedule({ employeeId, onDirtyChange, t }: UseWeeklyScheduleOptions) {
  const mounted = useMountedRef();
  const activeLoad = useRef<AbortController | null>(null);
  const [schedule, setSchedule] = useState<WeeklySchedule | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [stale, setStale] = useState(false);
  const [success, setSuccess] = useState<string | null>(null);
  const [revision, setRevision] = useState(0);
  const [editing, setEditing] = useState(false);
  const [discardOpen, setDiscardOpen] = useState(false);
  const definition = useMemo(() => createWeeklyHoursDefinition(t), [t]);
  const controller = useMemo(() => createLilyFormController<WeeklyHoursFormValues>(), []);
  const formStatus = useLilyFormStatus(controller, (status) => status);
  const dirty = editing && formStatus.isDirty;

  const load = useCallback(() => {
    activeLoad.current?.abort();
    const controller = new AbortController();
    activeLoad.current = controller;

    return getWeeklySchedule(employeeId, controller.signal)
      .then((value) => {
        if (controller.signal.aborted) return false;
        setError(null);
        setStale(false);
        setSchedule(value);
        setEditing(false);
        setRevision((current) => current + 1);
        return true;
      })
      .catch((loadError: unknown) => {
        if (!controller.signal.aborted) {
          setError(schedulingErrorMessage(loadError, t, "app:scheduling.weeklyLoadError"));
        }
        return false;
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
  }, [employeeId, t]);

  useEffect(() => {
    void load();
    return () => activeLoad.current?.abort();
  }, [load]);

  useEffect(() => {
    onDirtyChange?.(dirty);
    return () => onDirtyChange?.(false);
  }, [dirty, onDirtyChange]);

  const initialValues = useMemo<WeeklyHoursFormValues>(
    () => ({
      periods:
        schedule?.state === "unconfigured"
          ? Array.from({ length: 5 }, (_, index) => ({
              dayOfWeek: String(index + 1),
              startTime: "09:00",
              endTime: "17:00",
            }))
          : (schedule?.periods.map((period) => ({
              dayOfWeek: String(period.dayOfWeek),
              startTime: fromMinute(period.startMinute),
              endTime: fromMinute(period.endMinute),
            })) ?? []),
      changeNote: "",
    }),
    [schedule],
  );

  async function submit(values: WeeklyHoursFormValues): Promise<boolean> {
    if (!schedule) return false;
    setError(null);
    setStale(false);
    setSuccess(null);
    const saved = await putWeeklySchedule(
      {
        expectedRevision: schedule.revision,
        periods: values.periods.map((period) => ({
          dayOfWeek: Number(period.dayOfWeek),
          startMinute: toMinute(period.startTime),
          endMinute: toMinute(period.endTime),
        })),
        changeNote: values.changeNote.trim() || null,
      },
      employeeId,
    );
    if (!mounted.current) return false;
    setSchedule(saved);
    setEditing(false);
    setRevision((current) => current + 1);
    setSuccess(t("app:scheduling.publishSuccess"));
    return true;
  }

  async function inherit(): Promise<boolean> {
    if (!employeeId || !schedule) return false;
    setLoading(true);
    setError(null);
    setSuccess(null);
    try {
      await restoreWeeklyInheritance(employeeId, schedule.revision);
      if (!mounted.current) return false;
      const loaded = await load();
      if (!mounted.current || !loaded) return false;
      setSuccess(t("app:scheduling.inheritanceSuccess"));
      return true;
    } catch (restoreError) {
      if (mounted.current) {
        setStale(isScheduleVersionConflict(restoreError));
        setError(schedulingErrorMessage(restoreError, t, "app:scheduling.weeklySaveError"));
        setLoading(false);
      }
      return false;
    }
  }

  const handleMutationError = useCallback(
    (mutationError: unknown) => {
      setStale(isScheduleVersionConflict(mutationError));
      setError(schedulingErrorMessage(mutationError, t, "app:scheduling.weeklySaveError"));
    },
    [t],
  );

  const acceptRestoredSchedule = useCallback(
    (restored: WeeklySchedule) => {
      setSchedule(restored);
      setRevision((current) => current + 1);
      setEditing(false);
      setStale(false);
      setError(null);
      setSuccess(t("app:scheduling.restoreVersionSuccess"));
    },
    [t],
  );

  function cancelEdit() {
    if (formStatus.isDirty) {
      setDiscardOpen(true);
      return;
    }
    setEditing(false);
    setRevision((current) => current + 1);
  }

  function discardChanges() {
    setDiscardOpen(false);
    setEditing(false);
    setRevision((current) => current + 1);
  }

  return {
    schedule,
    loading,
    error,
    stale,
    success,
    revision,
    editing,
    discardOpen,
    definition,
    controller,
    formStatus,
    initialValues,
    state: schedule?.state ?? "unconfigured",
    canEdit: !loading && schedule !== null,
    submit,
    inherit,
    load: () => {
      setLoading(true);
      return load();
    },
    handleMutationError,
    acceptRestoredSchedule,
    cancelEdit,
    discardChanges,
    startEditing: () => setEditing(true),
    keepEditing: () => setDiscardOpen(false),
  };
}
