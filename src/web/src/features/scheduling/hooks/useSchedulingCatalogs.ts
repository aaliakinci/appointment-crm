import { useCallback, useEffect, useRef, useState } from "react";

import { listEmployees, type Employee } from "@/features/employees/catalog";
import { listServices, type ServiceOffering } from "@/features/services/catalog";

export function useSchedulingCatalogs() {
  const [employees, setEmployees] = useState<readonly Employee[]>([]);
  const [services, setServices] = useState<readonly ServiceOffering[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const activeRequest = useRef<AbortController | null>(null);

  const load = useCallback(() => {
    activeRequest.current?.abort();
    const controller = new AbortController();
    activeRequest.current = controller;

    void Promise.all([
      listEmployees(
        {
          page: 1,
          pageSize: 100,
          isActive: true,
          sortBy: "name",
          sortDirection: "asc",
        },
        controller.signal,
      ),
      listServices(
        {
          page: 1,
          pageSize: 100,
          isActive: true,
          sortBy: "name",
          sortDirection: "asc",
        },
        controller.signal,
      ),
    ])
      .then(([employeePage, servicePage]) => {
        if (controller.signal.aborted) return;
        setEmployees(employeePage.items);
        setServices(servicePage.items);
        setLoadError(false);
      })
      .catch(() => {
        if (!controller.signal.aborted) setLoadError(true);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
  }, []);

  const reload = useCallback(() => {
    setLoading(true);
    load();
  }, [load]);

  useEffect(() => {
    load();
    return () => activeRequest.current?.abort();
  }, [load]);

  return { employees, services, loading, loadError, reload };
}
