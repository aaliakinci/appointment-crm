import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

interface StatusCardProps {
  readonly id: string;
  readonly label: string;
  readonly loading: boolean;
  readonly healthy: boolean;
  readonly healthyLabel: string;
  readonly unavailableLabel: string;
}

export function StatusCard({
  id,
  healthy,
  healthyLabel,
  label,
  loading,
  unavailableLabel,
}: StatusCardProps) {
  const statusLabel = loading ? "…" : healthy ? healthyLabel : unavailableLabel;

  return (
    <Box
      id={id}
      sx={{
        flex: 1,
        border: 1,
        borderColor: "divider",
        borderRadius: 2,
        bgcolor: "background.paper",
        p: 3,
      }}
    >
      <Typography id={`${id}.label`} component="h2" variant="h6">
        {label}
      </Typography>
      <Typography
        id={`${id}.status`}
        component="p"
        sx={{ mt: 1, color: loading ? "text.secondary" : healthy ? "success.main" : "error.main" }}
      >
        {statusLabel}
      </Typography>
    </Box>
  );
}
