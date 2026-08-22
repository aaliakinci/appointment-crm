import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { TextField } from "@lily_platform/lily_ui/ui/atoms/TextField";
import type { FormEvent } from "react";

import type { useEmployeeList } from "../hooks/useEmployeeList";

interface EmployeeFiltersProps {
  readonly id: string;
  readonly list: ReturnType<typeof useEmployeeList>;
  readonly t: (key: string) => string;
}

export function EmployeeFilters({ id, list, t }: EmployeeFiltersProps) {
  function submit(event: FormEvent<HTMLElement>) {
    event.preventDefault();
    list.applySearch();
  }

  return (
    <Stack
      id={id}
      component="form"
      direction={{ xs: "column", lg: "row" }}
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
      <Select
        id={`${id}.service`}
        label={t("app:employees.serviceFilter")}
        value={list.serviceFilter}
        options={[
          { id: "all", value: "", label: t("app:common.all") },
          ...list.serviceOptions.map((service) => ({
            id: service.id,
            value: service.id,
            label: service.name,
          })),
        ]}
        onValueChange={(value) => list.setServiceFilter(String(value))}
        sx={{ minWidth: 220 }}
      />
      <Button id={`${id}.apply`} type="submit" variant="outlined">
        {t("app:common.apply")}
      </Button>
    </Stack>
  );
}
