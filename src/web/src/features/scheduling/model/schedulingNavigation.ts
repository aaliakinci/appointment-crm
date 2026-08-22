export type SchedulingTab = "weekly" | "overrides" | "timeOff" | "availability";

export type SchedulingNavigationTarget =
  | { readonly kind: "tab"; readonly tab: SchedulingTab }
  | { readonly kind: "weeklyScope"; readonly scope: string }
  | { readonly kind: "overrideScope"; readonly scope: string };

export interface SchedulingLocation {
  readonly activeTab: SchedulingTab;
  readonly overrideScope: string;
  readonly weeklyScope: string;
}

export function applySchedulingNavigation(
  location: SchedulingLocation,
  target: SchedulingNavigationTarget,
): SchedulingLocation {
  switch (target.kind) {
    case "tab":
      return { ...location, activeTab: target.tab };
    case "weeklyScope":
      return { ...location, weeklyScope: target.scope };
    case "overrideScope":
      return { ...location, overrideScope: target.scope };
  }
}

export function isCurrentSchedulingLocation(
  location: SchedulingLocation,
  target: SchedulingNavigationTarget,
): boolean {
  switch (target.kind) {
    case "tab":
      return location.activeTab === target.tab;
    case "weeklyScope":
      return location.weeklyScope === target.scope;
    case "overrideScope":
      return location.overrideScope === target.scope;
  }
}

export function isSchedulingTab(value: string | number): value is SchedulingTab {
  return (
    value === "weekly" || value === "overrides" || value === "timeOff" || value === "availability"
  );
}
