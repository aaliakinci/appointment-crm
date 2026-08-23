export function tenantToday(timeZone: string, now = new Date()): string {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(now);
  const value = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  return `${value.year}-${value.month}-${value.day}`;
}

export function startOfIsoWeek(date: string): string {
  const value = parseDate(date);
  const daysSinceMonday = (value.getUTCDay() + 6) % 7;
  value.setUTCDate(value.getUTCDate() - daysSinceMonday);
  return formatDate(value);
}

export function addDays(date: string, days: number): string {
  const value = parseDate(date);
  value.setUTCDate(value.getUTCDate() + days);
  return formatDate(value);
}

export function weekDates(weekStart: string): readonly string[] {
  return Array.from({ length: 7 }, (_, index) => addDays(weekStart, index));
}

export function localDateFromOffset(value: string): string {
  return value.slice(0, 10);
}

export function localTimeFromOffset(value: string): string {
  return value.slice(11, 16);
}

export function formatLocalDate(date: string, locale?: string): string {
  return new Intl.DateTimeFormat(locale, {
    weekday: "short",
    day: "numeric",
    month: "short",
  }).format(parseDate(date));
}

function parseDate(value: string): Date {
  return new Date(`${value}T12:00:00Z`);
}

function formatDate(value: Date): string {
  return value.toISOString().slice(0, 10);
}
