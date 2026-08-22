import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Tabs } from "@lily_platform/lily_ui/ui/atoms/Tabs";

import { isSchedulingTab, type SchedulingTab } from "../model/schedulingNavigation";

interface SchedulingTabsProps {
  readonly activeTab: SchedulingTab;
  readonly id: string;
  readonly onChange: (tab: SchedulingTab) => void;
  readonly t: (key: string) => string;
}

export function SchedulingTabs({ activeTab, id, onChange, t }: SchedulingTabsProps) {
  return (
    <Paper id={id} variant="outlined" sx={{ px: 2, pt: 1 }}>
      <Tabs
        id={`${id}.tabs`}
        aria-label={t("app:scheduling.tabsAriaLabel")}
        value={activeTab}
        showContent={false}
        variant="scrollable"
        scrollButtons="auto"
        items={[
          createTab(id, "weekly", t("app:scheduling.tabs.weekly")),
          createTab(id, "overrides", t("app:scheduling.tabs.overrides")),
          createTab(id, "timeOff", t("app:scheduling.tabs.timeOff")),
          createTab(id, "availability", t("app:scheduling.tabs.availability")),
        ]}
        onValueChange={(value) => isSchedulingTab(value) && onChange(value)}
      />
    </Paper>
  );
}

function createTab(id: string, value: SchedulingTab, label: string) {
  return {
    id: `${id}.tab.${value}`,
    value,
    label,
    content: null,
    ariaControls: `${id}.panel.${value}`,
  };
}
