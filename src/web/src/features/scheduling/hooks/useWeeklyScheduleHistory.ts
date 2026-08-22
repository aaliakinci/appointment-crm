import { useCallback, useEffect, useRef, useState } from "react";

import { listWeeklyScheduleVersions, restoreWeeklyScheduleVersion } from "../api/schedulingApi";
import type { WeeklySchedule, WeeklyScheduleVersion } from "../api/schedulingContract";
import { isScheduleVersionConflict, schedulingErrorMessage } from "../model/schedulingErrors";
import { useMountedRef } from "./useMountedRef";

interface UseWeeklyScheduleHistoryOptions {
  readonly employeeId?: string;
  readonly onConflict: (error: unknown) => void;
  readonly onRestored: (schedule: WeeklySchedule) => void;
  readonly t: (key: string) => string;
}

export function useWeeklyScheduleHistory({
  employeeId,
  onConflict,
  onRestored,
  t,
}: UseWeeklyScheduleHistoryOptions) {
  const mounted = useMountedRef();
  const activeLoad = useRef<AbortController | null>(null);
  const [open, setOpen] = useState(false);
  const [items, setItems] = useState<readonly WeeklyScheduleVersion[]>([]);
  const [page, setPageState] = useState(1);
  const [pages, setPages] = useState(0);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [selectedVersion, setSelectedVersion] = useState<WeeklyScheduleVersion | null>(null);
  const [restoreCandidate, setRestoreCandidate] = useState<WeeklyScheduleVersion | null>(null);

  const load = useCallback(
    (targetPage: number) => {
      activeLoad.current?.abort();
      const controller = new AbortController();
      activeLoad.current = controller;
      return listWeeklyScheduleVersions(employeeId, targetPage, controller.signal)
        .then((result) => {
          if (controller.signal.aborted) return;
          setItems(result.items);
          setPages(result.totalPages);
          setError(null);
          setSelectedVersion((current) =>
            current && result.items.some((version) => version.id === current.id) ? current : null,
          );
        })
        .catch((loadError: unknown) => {
          if (!controller.signal.aborted) {
            setError(schedulingErrorMessage(loadError, t, "app:scheduling.historyLoadError"));
          }
        })
        .finally(() => {
          if (!controller.signal.aborted) setLoading(false);
        });
    },
    [employeeId, t],
  );

  useEffect(() => {
    if (open) void load(page);
    return () => activeLoad.current?.abort();
  }, [load, open, page]);

  async function restore(expectedRevision: number) {
    if (!restoreCandidate) return;
    setError(null);
    try {
      const restored = await restoreWeeklyScheduleVersion(
        restoreCandidate.id,
        { expectedRevision, changeNote: null },
        employeeId,
      );
      if (!mounted.current) return;
      onRestored(restored);
      setRestoreCandidate(null);
      setSelectedVersion(null);
      setLoading(true);
      if (page === 1) {
        await load(1);
      } else {
        setPageState(1);
      }
    } catch (restoreError) {
      if (!mounted.current) return;
      setRestoreCandidate(null);
      if (isScheduleVersionConflict(restoreError)) {
        onConflict(restoreError);
      } else {
        setError(schedulingErrorMessage(restoreError, t, "app:scheduling.weeklySaveError"));
      }
    }
  }

  const reloadFirstPage = useCallback(() => {
    if (!open) return;
    setLoading(true);
    if (page === 1) void load(1);
    else setPageState(1);
  }, [load, open, page]);

  return {
    open,
    items,
    page,
    pages,
    loading,
    error,
    selectedVersion,
    restoreCandidate,
    openHistory: () => {
      setLoading(true);
      setPageState(1);
      setOpen(true);
    },
    closeHistory: () => setOpen(false),
    setPage: (value: number) => {
      setLoading(true);
      setPageState(value);
    },
    selectVersion: setSelectedVersion,
    requestRestore: setRestoreCandidate,
    cancelRestore: () => setRestoreCandidate(null),
    restore,
    reloadFirstPage,
  };
}
