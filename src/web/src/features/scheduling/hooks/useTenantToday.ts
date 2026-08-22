import { useEffect, useState } from "react";

import { todayInTimeZone } from "../model/localDate";

export function useTenantToday(timeZone: string): string {
  const [today, setToday] = useState(() => todayInTimeZone(timeZone));

  useEffect(() => {
    const refresh = () => setToday(todayInTimeZone(timeZone));
    refresh();
    const timer = window.setInterval(refresh, 60_000);
    return () => window.clearInterval(timer);
  }, [timeZone]);

  return today;
}
