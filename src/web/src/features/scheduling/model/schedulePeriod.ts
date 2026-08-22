export interface EditablePeriod {
  dayOfWeek: string;
  startTime: string;
  endTime: string;
}

export const emptyPeriod: EditablePeriod = {
  dayOfWeek: "1",
  startTime: "09:00",
  endTime: "17:00",
};

export function toMinute(time: string): number {
  const [hour = "0", minute = "0"] = time.split(":");
  return Number(hour) * 60 + Number(minute);
}

export function fromMinute(value: number): string {
  if (value === 24 * 60) {
    return "24:00";
  }
  return `${String(Math.floor(value / 60)).padStart(2, "0")}:${String(value % 60).padStart(2, "0")}`;
}

export function validatePeriods(
  periods: readonly EditablePeriod[],
  includeDay: boolean,
  t: (key: string) => string,
) {
  const normalized = periods.map((period) => ({
    day: includeDay ? Number(period.dayOfWeek) : 0,
    start: toMinute(period.startTime),
    end: toMinute(period.endTime),
    startTime: period.startTime,
    endTime: period.endTime,
  }));
  const invalid = normalized.some(
    (period) =>
      (includeDay && (period.day < 1 || period.day > 7)) ||
      !isTime(period.startTime) ||
      !isTime(period.endTime, true) ||
      period.start >= period.end ||
      period.start % 5 !== 0 ||
      period.end % 5 !== 0,
  );
  const overlap = normalized.some((period, index) =>
    normalized.some(
      (other, otherIndex) =>
        index !== otherIndex &&
        period.day === other.day &&
        period.start < other.end &&
        period.end > other.start,
    ),
  );
  if (invalid || overlap) {
    return {
      formIssues: [
        {
          code: invalid ? "scheduling.period" : "scheduling.overlap",
          defaultMessage: invalid
            ? t("app:scheduling.periodInvalid")
            : t("app:scheduling.periodOverlap"),
        },
      ],
    };
  }
  return undefined;
}

export function isTime(value: string, allowEndOfDay = false): boolean {
  return /^(?:[01]\d|2[0-3]):(?:0\d|[1-5]\d)$/.test(value) || (allowEndOfDay && value === "24:00");
}
