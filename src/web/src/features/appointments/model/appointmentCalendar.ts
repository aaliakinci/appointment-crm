export interface AppointmentCalendarRange {
  readonly fromDate: string;
  readonly toDate: string;
}

export function appointmentCalendarRange(dates: readonly string[]): AppointmentCalendarRange {
  const fromDate = dates[0];
  const toDate = dates[dates.length - 1];
  if (!fromDate || !toDate) {
    throw new TypeError("The appointment calendar requires at least one date.");
  }

  return { fromDate, toDate };
}

export function appointmentCalendarSelectionChanged(current: string, next: string): boolean {
  return current !== next;
}
