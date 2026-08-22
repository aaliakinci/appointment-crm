import { useLilyBeforeUnload, useLilyNavigate } from "@lily_platform/lily_ui/router";
import { useCallback, useMemo, useState } from "react";

import type { WorkspacePath } from "@/features/auth/workspace";

import {
  applySchedulingNavigation,
  isCurrentSchedulingLocation,
  type SchedulingLocation,
  type SchedulingNavigationTarget,
  type SchedulingTab,
} from "../model/schedulingNavigation";

const initialLocation: SchedulingLocation = {
  activeTab: "weekly",
  weeklyScope: "tenant",
  overrideScope: "tenant",
};

const initialDirtyState: Record<SchedulingTab, boolean> = {
  weekly: false,
  overrides: false,
  timeOff: false,
  availability: false,
};

export function useSchedulingNavigation() {
  const navigate = useLilyNavigate();
  const [location, setLocation] = useState(initialLocation);
  const [dirtyByTab, setDirtyByTab] = useState(initialDirtyState);
  const [pending, setPending] = useState<SchedulingNavigationTarget | null>(null);
  const [pendingRoute, setPendingRoute] = useState<WorkspacePath | null>(null);
  const hasUnsavedChanges = useMemo(() => Object.values(dirtyByTab).some(Boolean), [dirtyByTab]);
  useLilyBeforeUnload(hasUnsavedChanges);

  const request = useCallback(
    (target: SchedulingNavigationTarget) => {
      if (isCurrentSchedulingLocation(location, target)) return;
      if (dirtyByTab[location.activeTab]) {
        setPending(target);
        return;
      }
      setLocation((current) => applySchedulingNavigation(current, target));
    },
    [dirtyByTab, location],
  );

  const requestRoute = useCallback(
    (path: WorkspacePath) => {
      if (path === "/scheduling") return;
      if (hasUnsavedChanges) {
        setPendingRoute(path);
        return;
      }

      void navigate(path);
    },
    [hasUnsavedChanges, navigate],
  );

  const setDirty = useCallback((tab: SchedulingTab, dirty: boolean) => {
    setDirtyByTab((current) => (current[tab] === dirty ? current : { ...current, [tab]: dirty }));
  }, []);

  const setWeeklyDirty = useCallback((dirty: boolean) => setDirty("weekly", dirty), [setDirty]);
  const setOverrideDirty = useCallback(
    (dirty: boolean) => setDirty("overrides", dirty),
    [setDirty],
  );
  const setTimeOffDirty = useCallback((dirty: boolean) => setDirty("timeOff", dirty), [setDirty]);

  const discardAndContinue = useCallback(() => {
    setDirtyByTab(initialDirtyState);
    if (pendingRoute) {
      const route = pendingRoute;
      setPendingRoute(null);
      setPending(null);
      void navigate(route);
      return;
    }
    if (pending) {
      setLocation((current) => applySchedulingNavigation(current, pending));
      setPending(null);
    }
  }, [navigate, pending, pendingRoute]);

  const keepEditing = useCallback(() => {
    setPending(null);
    setPendingRoute(null);
  }, []);

  return {
    ...location,
    pending,
    confirmationOpen: pending !== null || pendingRoute !== null,
    hasUnsavedChanges,
    requestTab: (tab: SchedulingTab) => request({ kind: "tab", tab }),
    requestWeeklyScope: (scope: string) => request({ kind: "weeklyScope", scope }),
    requestOverrideScope: (scope: string) => request({ kind: "overrideScope", scope }),
    requestRoute,
    setWeeklyDirty,
    setOverrideDirty,
    setTimeOffDirty,
    keepEditing,
    discardAndContinue,
  };
}
