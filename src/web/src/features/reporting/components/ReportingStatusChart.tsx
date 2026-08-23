import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { ReportingStatusBreakdown } from "../api/reportingContract";

interface ReportingStatusChartProps {
  readonly id: string;
  readonly items: readonly ReportingStatusBreakdown[];
  readonly t: (key: string) => string;
}

export function ReportingStatusChart({ id, items, t }: ReportingStatusChartProps) {
  const maximum = Math.max(1, ...items.map((item) => item.count));
  return (
    <Paper id={id} variant="outlined" sx={{ p: { xs: 2, sm: 3 }, flex: 1 }}>
      <Stack id={`${id}.content`} spacing={2}>
        <Typography id={`${id}.title`} component="h2" variant="h5">
          {t("app:reporting.statusDistribution")}
        </Typography>
        {items.map((item) => (
          <Stack id={`${id}.${item.status}`} key={item.status} spacing={0.5}>
            <Stack id={`${id}.${item.status}.label`} direction="row" spacing={1}>
              <Typography id={`${id}.${item.status}.name`} variant="body2" sx={{ flex: 1 }}>
                {t(`app:appointments.status.${item.status}`)}
              </Typography>
              <Typography id={`${id}.${item.status}.count`} variant="body2">
                {item.count}
              </Typography>
            </Stack>
            <Box
              id={`${id}.${item.status}.track`}
              role="img"
              aria-label={`${t(`app:appointments.status.${item.status}`)}: ${item.count}`}
              sx={{ height: 10, borderRadius: 5, bgcolor: "action.hover", overflow: "hidden" }}
            >
              <Box
                id={`${id}.${item.status}.bar`}
                sx={{
                  width: `${(item.count / maximum) * 100}%`,
                  minWidth: item.count > 0 ? 4 : 0,
                  height: "100%",
                  bgcolor: "primary.main",
                }}
              />
            </Box>
          </Stack>
        ))}
      </Stack>
    </Paper>
  );
}
