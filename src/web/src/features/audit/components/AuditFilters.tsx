import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Select } from "@lily_platform/lily_ui/ui/atoms/Select";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { TextField } from "@lily_platform/lily_ui/ui/atoms/TextField";
import type { FormEvent } from "react";

import { LocalizedLilyDatePicker } from "@/shared/forms/LocalizedLilyDateForm";

import type { useAuditList } from "../hooks/useAuditList";

interface AuditFiltersProps {
  readonly id: string;
  readonly list: ReturnType<typeof useAuditList>;
  readonly t: (key: string) => string;
}

export function AuditFilters({ id, list, t }: AuditFiltersProps) {
  function submit(event: FormEvent<HTMLElement>) {
    event.preventDefault();
    list.applyFilters();
  }
  return (
    <Paper id={id} variant="outlined" sx={{ p: 2 }}>
      <Stack id={`${id}.form`} component="form" spacing={2} onSubmit={submit}>
        <Stack id={`${id}.primary`} direction={{ xs: "column", lg: "row" }} spacing={2}>
          <TextField
            id={`${id}.search`}
            label={t("app:common.search")}
            value={list.draftSearch}
            onValueChange={list.setDraftSearch}
            fullWidth
          />
          <LocalizedLilyDatePicker
            id={`${id}.fromDate`}
            label={t("app:audit.fromDate")}
            value={list.draftFromDate}
            onValueChange={list.setDraftFromDate}
            fullWidth
          />
          <LocalizedLilyDatePicker
            id={`${id}.toDate`}
            label={t("app:audit.toDate")}
            value={list.draftToDate}
            onValueChange={list.setDraftToDate}
            fullWidth
          />
          <Select
            id={`${id}.actor`}
            label={t("app:audit.actor")}
            value={list.actorUserId}
            options={[
              { id: "all", value: "", label: t("app:common.all") },
              ...list.memberships.map((membership) => ({
                id: membership.userId,
                value: membership.userId,
                label: membership.displayName,
              })),
            ]}
            onValueChange={(value) => list.setActorUserId(String(value))}
            sx={{ minWidth: 220 }}
          />
        </Stack>
        <Stack id={`${id}.secondary`} direction={{ xs: "column", md: "row" }} spacing={2}>
          <TextField
            id={`${id}.action`}
            label={t("app:audit.action")}
            value={list.draftAction}
            onValueChange={list.setDraftAction}
            fullWidth
          />
          <TextField
            id={`${id}.targetType`}
            label={t("app:audit.targetType")}
            value={list.draftTargetType}
            onValueChange={list.setDraftTargetType}
            fullWidth
          />
          <Button id={`${id}.apply`} type="submit" variant="outlined">
            {t("app:common.apply")}
          </Button>
        </Stack>
      </Stack>
    </Paper>
  );
}
