import { Box } from "@lily_platform/lily_ui/ui/atoms/Box";
import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";
import { Typography } from "@lily_platform/lily_ui/ui/atoms/Typography";

interface ManagementPageHeaderProps {
  readonly id: string;
  readonly eyebrow: string;
  readonly title: string;
  readonly createLabel?: string;
  readonly onCreate?: () => void;
}

export function ManagementPageHeader({
  createLabel,
  eyebrow,
  id,
  onCreate,
  title,
}: ManagementPageHeaderProps) {
  return (
    <Stack
      id={id}
      direction={{ xs: "column", sm: "row" }}
      spacing={2}
      sx={{ alignItems: { sm: "center" } }}
    >
      <Box id={`${id}.titleBlock`} sx={{ flex: 1 }}>
        <Typography id={`${id}.eyebrow`} component="p" variant="overline" color="primary">
          {eyebrow}
        </Typography>
        <Typography id={`${id}.title`} component="h1" variant="h3">
          {title}
        </Typography>
      </Box>
      {createLabel && onCreate && (
        <Button id={`${id}.create`} variant="contained" onClick={onCreate}>
          {createLabel}
        </Button>
      )}
    </Stack>
  );
}
