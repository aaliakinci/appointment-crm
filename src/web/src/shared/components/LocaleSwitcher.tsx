import { Button } from "@lily_platform/lily_ui/ui/atoms/Button";
import { Stack } from "@lily_platform/lily_ui/ui/atoms/Stack";

interface LocaleSwitcherProps {
  readonly id: string;
  readonly label: string;
  readonly locale: string;
  readonly onChange: (locale: "tr-TR" | "en-US") => void;
}

export function LocaleSwitcher({ id, label, locale, onChange }: LocaleSwitcherProps) {
  return (
    <Stack id={id} direction="row" spacing={0.5} role="group" aria-label={label}>
      <Button
        id={`${id}.tr`}
        size="small"
        variant={locale === "tr-TR" ? "contained" : "text"}
        aria-pressed={locale === "tr-TR"}
        onClick={() => onChange("tr-TR")}
      >
        TR
      </Button>
      <Button
        id={`${id}.en`}
        size="small"
        variant={locale === "en-US" ? "contained" : "text"}
        aria-pressed={locale === "en-US"}
        onClick={() => onChange("en-US")}
      >
        EN
      </Button>
    </Stack>
  );
}
