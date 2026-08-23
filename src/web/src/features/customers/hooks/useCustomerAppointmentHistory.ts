import { useEffect, useState } from "react";

import { listCustomerAppointmentHistory, type Appointment } from "@/features/appointments/catalog";
import type { PagedResponse } from "@/shared/api";

const emptyPage: PagedResponse<Appointment> = {
  items: [],
  page: 1,
  pageSize: 10,
  totalCount: 0,
  totalPages: 0,
};

export function useCustomerAppointmentHistory(customerId: string | null) {
  const [result, setResult] = useState<PagedResponse<Appointment>>(emptyPage);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(10);
  const [loading, setLoading] = useState(false);
  const [loadedCustomerId, setLoadedCustomerId] = useState<string | null>(null);
  const [loadError, setLoadError] = useState(false);

  useEffect(() => {
    if (!customerId) {
      return;
    }
    const abortController = new AbortController();
    void listCustomerAppointmentHistory(
      customerId,
      {
        page: page + 1,
        pageSize,
        sortBy: "start",
        sortDirection: "desc",
      },
      abortController.signal,
    )
      .then((value) => {
        if (!abortController.signal.aborted) {
          setResult(value);
          setLoadedCustomerId(customerId);
          setLoadError(false);
        }
      })
      .catch(() => {
        if (!abortController.signal.aborted) setLoadError(true);
      })
      .finally(() => {
        if (!abortController.signal.aborted) setLoading(false);
      });
    return () => abortController.abort();
  }, [customerId, page, pageSize]);

  return {
    loadError,
    loading: customerId !== null && (loading || loadedCustomerId !== customerId),
    page,
    pageSize,
    result,
    setPage: (value: number) => {
      setLoading(true);
      setPage(value);
    },
    setPageSize: (value: number) => {
      setLoading(true);
      setPage(0);
      setPageSize(value);
    },
  };
}
