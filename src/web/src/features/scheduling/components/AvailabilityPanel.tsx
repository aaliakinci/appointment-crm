import { Alert } from "@lily_platform/lily_ui/ui/atoms/Alert";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { Employee } from "@/features/employees/catalog";
import type { ServiceOffering } from "@/features/services/catalog";
import { LocalizedLilyDateForm } from "@/shared/forms/LocalizedLilyDateForm";

import { useAvailability } from "../hooks/useAvailability";
import { AvailabilityResults } from "./AvailabilityResults";

interface AvailabilityPanelProps {
  readonly employees: readonly Employee[];
  readonly id: string;
  readonly services: readonly ServiceOffering[];
  readonly t: (key: string) => string;
  readonly timeZone: string;
  readonly today: string;
}

export function AvailabilityPanel({
  employees,
  id,
  services,
  t,
  timeZone,
  today,
}: AvailabilityPanelProps) {
  const query = useAvailability({ employees, services, t, today });

  return (
    <Paper id={id} variant="outlined" sx={{ p: 3 }}>
      <Stack id={`${id}.content`} spacing={3}>
        <Stack id={`${id}.heading`} spacing={0.5}>
          <Typography id={`${id}.title`} component="h2" variant="h6">
            {t("app:scheduling.availabilityTitle")}
          </Typography>
          <Typography id={`${id}.description`} variant="body2" color="text.secondary">
            {t("app:scheduling.availabilityDescription")} {timeZone}
          </Typography>
        </Stack>
        {query.error && (
          <Alert id={`${id}.error`} severity="error">
            {query.error}
          </Alert>
        )}
        <LocalizedLilyDateForm
          definition={query.definition}
          instanceId={`${id}.form`}
          bindings={query.bindings}
          controller={query.controller}
          onSubmit={query.submit}
          onSubmitError={({ error }) => query.handleSubmitError(error)}
        />
        <Button
          id={`${id}.calculate`}
          variant="contained"
          loading={query.formStatus.isSubmitting}
          sx={{ alignSelf: "flex-start" }}
          onClick={() => void query.controller.submit()}
        >
          {t("app:scheduling.calculate")}
        </Button>
        {query.availability && (
          <AvailabilityResults id={`${id}.result`} availability={query.availability} t={t} />
        )}
      </Stack>
    </Paper>
  );
}
