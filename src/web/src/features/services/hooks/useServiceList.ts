import { useEffect, useState } from "react";

import type { PagedResponse } from "@/shared/api";

import { listServices } from "../api/serviceApi";
import type { ServiceOffering } from "../api/serviceContract";

const emptyPage: PagedResponse<ServiceOffering> = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
};

export function useServiceList() {
  const [result, setResult] = useState<PagedResponse<ServiceOffering>>(emptyPage);
  const [page, setPageState] = useState(0);
  const [pageSize, setPageSizeState] = useState(20);
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilterState] = useState("all");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);

  function reload() {
    setLoading(true);
    setReloadVersion((value) => value + 1);
  }

  useEffect(() => {
    const controller = new AbortController();
    void listServices(
      {
        page: page + 1,
        pageSize,
        search,
        isActive: activeFilter === "all" ? undefined : activeFilter === "active",
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
  }, [activeFilter, page, pageSize, reloadVersion, search]);

  return {
    activeFilter,
    loadError,
    loading,
    page,
    pageSize,
    result,
    searchDraft,
    reload,
    setSearchDraft,
    applySearch: () => {
      reload();
      setPageState(0);
      setSearch(searchDraft.trim());
    },
    setActiveFilter: (value: string) => {
      setLoading(true);
      setPageState(0);
      setActiveFilterState(value);
    },
    setPage: (value: number) => {
      setLoading(true);
      setPageState(value);
    },
    setPageSize: (value: number) => {
      setLoading(true);
      setPageState(0);
      setPageSizeState(value);
    },
  };
}
