import { Paper } from "@lily_platform/lily_ui/ui/atoms/Paper";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

import type { MembershipReport } from "../api/membershipContract";

interface MembershipSummaryCardsProps {
  readonly id: string;
  readonly report: MembershipReport;
  readonly t: (key: string) => string;
}

export function MembershipSummaryCards({ id, report, t }: MembershipSummaryCardsProps) {
  const cards = [
    { key: "total", label: t("app:memberships.total"), value: report.total },
    { key: "active", label: t("app:memberships.active"), value: report.active },
    {
      key: "owners",
      label: t("app:memberships.ownerCount"),
      value: report.byRole.Owner ?? 0,
    },
  ];
  return (
    <Stack id={id} direction={{ xs: "column", sm: "row" }} spacing={2}>
      {cards.map((card) => (
        <Paper id={`${id}.${card.key}`} key={card.key} variant="outlined" sx={{ p: 2, flex: 1 }}>
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
