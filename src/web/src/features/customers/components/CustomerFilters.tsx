import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Checkbox } from "@lily_platform/lily_ui/ui/atoms/Checkbox";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { TextField } from "@lily_platform/lily_ui/ui/atoms/TextField";
import type { FormEvent } from "react";

import type { useCustomerList } from "../hooks/useCustomerList";

interface CustomerFiltersProps {
  readonly id: string;
  readonly list: ReturnType<typeof useCustomerList>;
  readonly searchLabel: string;
  readonly includeArchivedLabel: string;
  readonly applyLabel: string;
}

export function CustomerFilters({
  applyLabel,
  id,
  includeArchivedLabel,
  list,
  searchLabel,
}: CustomerFiltersProps) {
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
        label={searchLabel}
        value={list.searchDraft}
        onValueChange={list.setSearchDraft}
        fullWidth
      />
      <Checkbox
        id={`${id}.includeArchived`}
        label={includeArchivedLabel}
        checked={list.includeArchived}
        onCheckedChange={list.setIncludeArchived}
      />
      <Button id={`${id}.apply`} type="submit" variant="outlined">
        {applyLabel}
      </Button>
    </Stack>
  );
}
