import {
  LilyDateForm,
  LilyDatePicker,
  LilyDateProvider,
  type LilyDateFormProps,
  type LilyDatePickerProps,
} from "@lily_platform/lily_ui/ui/forms/date-fields";
import lilyEnglishDateLocale from "@lily_platform/lily_ui/ui/forms/date-fields/locales/en";
import lilyTurkishDateLocale from "@lily_platform/lily_ui/ui/forms/date-fields/locales/tr";
import type { ReactNode } from "react";

import { useAppTranslation } from "@/i18n";

interface LocalizedLilyDateProviderProps {
  readonly children: ReactNode;
}

export type LocalizedLilyDatePickerProps = Omit<LilyDatePickerProps, "value" | "onValueChange"> & {
  readonly value: string;
  readonly onValueChange: (value: string) => void;
};

function LocalizedLilyDateProvider({ children }: LocalizedLilyDateProviderProps) {
  const { locale } = useAppTranslation();
  const localeAdapter = locale === "tr-TR" ? lilyTurkishDateLocale : lilyEnglishDateLocale;

  return <LilyDateProvider localeAdapter={localeAdapter}>{children}</LilyDateProvider>;
}

export function LocalizedLilyDatePicker({
  value,
  onValueChange,
  invalidText,
  ...props
}: LocalizedLilyDatePickerProps) {
  const { t } = useAppTranslation();

  return (
    <LocalizedLilyDateProvider>
      <LilyDatePicker
        {...props}
        value={value}
        invalidText={invalidText ?? t("app:common.invalidDate")}
        onValueChange={(nextValue) => onValueChange(nextValue ?? "")}
      />
    </LocalizedLilyDateProvider>
  );
}

export function LocalizedLilyDateForm<TValues extends object>(props: LilyDateFormProps<TValues>) {
  return (
    <LocalizedLilyDateProvider>
      <LilyDateForm {...props} />
    </LocalizedLilyDateProvider>
  );
}
