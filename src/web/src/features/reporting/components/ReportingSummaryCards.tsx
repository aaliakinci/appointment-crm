import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { ReportingDashboard } from "../api/reportingContract";

interface ReportingSummaryCardsProps {
  readonly dashboard: ReportingDashboard;
  readonly id: string;
  readonly t: (key: string) => string;
}

export function ReportingSummaryCards({ dashboard, id, t }: ReportingSummaryCardsProps) {
  const cards = [
    {
      key: "today",
      label: t("app:reporting.todayAppointments"),
      value: String(dashboard.todaySummary.totalAppointments),
    },
    {
      key: "range",
      label: t("app:reporting.rangeAppointments"),
      value: String(dashboard.range.totalAppointments),
    },
    {
      key: "completed",
      label: t("app:reporting.completedAppointments"),
      value: String(dashboard.range.completedAppointments),
    },
    {
      key: "revenue",
      label: t("app:reporting.completedRevenue"),
      value: formatMoney(dashboard.range.completedRevenue, dashboard.currency),
    },
  ];
  return (
    <Stack id={id} direction={{ xs: "column", sm: "row" }} spacing={2} sx={{ flexWrap: "wrap" }}>
      {cards.map((card) => (
        <Paper
          id={`${id}.${card.key}`}
          key={card.key}
          variant="outlined"
          sx={{ p: 2.5, flex: "1 1 190px" }}
        >
          <Typography id={`${id}.${card.key}.label`} variant="body2" color="text.secondary">
            {card.label}
          </Typography>
          <Typography id={`${id}.${card.key}.value`} component="p" variant="h4">
            {card.value}
          </Typography>
        </Paper>
      ))}
    </Stack>
  );
}

function formatMoney(value: number, currency: string): string {
  return new Intl.NumberFormat(undefined, { style: "currency", currency }).format(value);
}
