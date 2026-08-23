import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { ReportingDailyBreakdown } from "../api/reportingContract";

interface ReportingDailyChartProps {
  readonly id: string;
  readonly items: readonly ReportingDailyBreakdown[];
  readonly t: (key: string) => string;
}

export function ReportingDailyChart({ id, items, t }: ReportingDailyChartProps) {
  const visible = items.slice(-31);
  const maximum = Math.max(1, ...visible.map((item) => item.totalAppointments));
  return (
    <Paper id={id} variant="outlined" sx={{ p: { xs: 2, sm: 3 }, flex: 1 }}>
      <Stack id={`${id}.content`} spacing={2}>
        <Typography id={`${id}.title`} component="h2" variant="h5">
          {t("app:reporting.dailyActivity")}
        </Typography>
        <Box
          id={`${id}.chart`}
          sx={{
            display: "grid",
            gridTemplateColumns: `repeat(${visible.length}, minmax(8px, 1fr))`,
            gap: 0.75,
            minHeight: 180,
            alignItems: "end",
            overflowX: "auto",
          }}
        >
          {visible.map((item) => (
            <Box
              id={`${id}.${item.date}`}
              key={item.date}
              role="img"
              aria-label={`${item.date}: ${item.totalAppointments}`}
              title={`${item.date}: ${item.totalAppointments}`}
              sx={{
                height: `${Math.max(4, (item.totalAppointments / maximum) * 100)}%`,
                minHeight: item.totalAppointments > 0 ? 10 : 4,
                bgcolor: item.totalAppointments > 0 ? "primary.main" : "action.hover",
                borderRadius: "4px 4px 0 0",
              }}
            />
          ))}
        </Box>
      </Stack>
    </Paper>
  );
}
