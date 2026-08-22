import { useEffect, useState } from "react";

import { listServices, type ServiceOffering } from "@/features/services/catalog";
import type { PagedResponse } from "@/shared/api";

import { listEmployees, listEmployeeUserOptions } from "../api/employeeApi";
import type { Employee, EmployeeUserOption } from "../api/employeeContract";

const emptyPage: PagedResponse<Employee> = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
};

export function useEmployeeList(canManage: boolean) {
  const [result, setResult] = useState<PagedResponse<Employee>>(emptyPage);
  const [serviceOptions, setServiceOptions] = useState<readonly ServiceOffering[]>([]);
  const [userOptions, setUserOptions] = useState<readonly EmployeeUserOption[]>([]);
  const [page, setPageState] = useState(0);
  const [pageSize, setPageSizeState] = useState(20);
  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilterState] = useState("all");
  const [serviceFilter, setServiceFilterState] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);

  function reload() {
    setLoading(true);
    setReloadVersion((value) => value + 1);
  }

  useEffect(() => {
    const controller = new AbortController();
    const employeesPromise = listEmployees(
      {
        page: page + 1,
        pageSize,
        search,
        isActive: activeFilter === "all" ? undefined : activeFilter === "active",
        serviceId: serviceFilter || undefined,
        sortBy: "name",
        sortDirection: "asc",
      },
      controller.signal,
    );
    const servicesPromise = listServices(
      { page: 1, pageSize: 100, sortBy: "name", sortDirection: "asc" },
      controller.signal,
    );
    const usersPromise = canManage
      ? listEmployeeUserOptions(controller.signal)
      : Promise.resolve([] as readonly EmployeeUserOption[]);

    void Promise.all([employeesPromise, servicesPromise, usersPromise])
      .then(([employees, services, users]) => {
        if (!controller.signal.aborted) {
          setResult(employees);
          setServiceOptions(services.items);
          setUserOptions(users);
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
  }, [activeFilter, canManage, page, pageSize, reloadVersion, search, serviceFilter]);

  return {
    activeFilter,
    loadError,
    loading,
    page,
    pageSize,
    result,
    searchDraft,
    serviceFilter,
    serviceOptions,
    userOptions,
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
    setServiceFilter: (value: string) => {
      setLoading(true);
      setPageState(0);
      setServiceFilterState(value);
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
