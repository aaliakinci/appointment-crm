import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { TextField } from "@lily_platform/lily_ui/ui/atoms/TextField";
import type { FormEvent } from "react";

import type { useServiceList } from "../hooks/useServiceList";

interface ServiceFiltersProps {
  readonly id: string;
  readonly list: ReturnType<typeof useServiceList>;
  readonly t: (key: string) => string;
}

export function ServiceFilters({ id, list, t }: ServiceFiltersProps) {
  function submit(event: FormEvent<HTMLElement>) {
    event.preventDefault();
    list.applySearch();
  }

  return (
    <Stack
      id={id}
      component="form"
      direction={{ xs: "column", md: "row" }}
      spacing={2}
      onSubmit={submit}
    >
      <TextField
        id={`${id}.search`}
        label={t("app:common.search")}
        value={list.searchDraft}
        onValueChange={list.setSearchDraft}
        fullWidth
      />
      <Select
        id={`${id}.status`}
        label={t("app:common.status")}
        value={list.activeFilter}
        options={[
          { id: "all", value: "all", label: t("app:common.all") },
          { id: "active", value: "active", label: t("app:common.active") },
          { id: "inactive", value: "inactive", label: t("app:common.inactive") },
        ]}
        onValueChange={(value) => list.setActiveFilter(String(value))}
        sx={{ minWidth: 180 }}
      />
      <Button id={`${id}.apply`} type="submit" variant="outlined">
        {t("app:common.apply")}
      </Button>
    </Stack>
  );
}
