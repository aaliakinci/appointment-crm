import { useEffect, useState } from "react";

import type { PagedResponse } from "@/shared/api";

import { listCustomers } from "../api/customerApi";
import type { Customer } from "../api/customerContract";

const emptyPage: PagedResponse<Customer> = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
};

export function useCustomerList() {
  const [result, setResult] = useState<PagedResponse<Customer>>(emptyPage);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [includeArchived, setIncludeArchivedState] = useState(false);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);

  function reload() {
    setLoading(true);
    setReloadVersion((value) => value + 1);
  }

  useEffect(() => {
    const controller = new AbortController();
    void listCustomers(
      {
        page: page + 1,
        pageSize,
        search,
        includeArchived,
        sortBy: "name",
        sortDirection: "asc",
      },
      controller.signal,
    )
      .then((response) => {
        if (!controller.signal.aborted) {
          setResult(response);
          setLoadError(false);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) {
          setLoadError(true);
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [includeArchived, page, pageSize, reloadVersion, search]);

  return {
    result,
    page,
    pageSize,
    searchDraft,
    includeArchived,
    loading,
    loadError,
    reload,
    setSearchDraft,
    applySearch: () => {
      reload();
      setPage(0);
      setSearch(searchDraft.trim());
    },
    setIncludeArchived: (value: boolean) => {
      setLoading(true);
      setPage(0);
      setIncludeArchivedState(value);
    },
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
