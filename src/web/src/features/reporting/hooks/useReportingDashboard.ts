import { useEffect, useMemo, useState } from "react";

import { listEmployees, type Employee } from "@/features/employees/catalog";

import { getReportingDashboard } from "../api/reportingApi";
import type { ReportingDashboard, ReportingQuery, ReportingStatus } from "../api/reportingContract";

interface UseReportingDashboardOptions {
  readonly today: string;
}

export function useReportingDashboard({ today }: UseReportingDashboardOptions) {
  const defaultFrom = useMemo(() => addDays(today, -29), [today]);
  const [draftFromDate, setDraftFromDate] = useState(defaultFrom);
  const [draftToDate, setDraftToDate] = useState(today);
  const [fromDate, setFromDate] = useState(defaultFrom);
  const [toDate, setToDate] = useState(today);
  const [employeeId, setEmployeeId] = useState("");
  const [status, setStatus] = useState<ReportingStatus | "">("");
  const [dashboard, setDashboard] = useState<ReportingDashboard | null>(null);
  const [employees, setEmployees] = useState<readonly Employee[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);

  useEffect(() => {
    const abortController = new AbortController();
    const query: ReportingQuery = {
      fromDate,
      toDate,
      employeeId: employeeId || undefined,
      status: status || undefined,
    };
    void getReportingDashboard(query, abortController.signal)
      .then((value) => {
        if (!abortController.signal.aborted) {
          setDashboard(value);
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
  }, [employeeId, fromDate, reloadVersion, status, toDate]);

  useEffect(() => {
    const abortController = new AbortController();
    void listEmployees(
      { page: 1, pageSize: 100, isActive: true, sortBy: "name", sortDirection: "asc" },
      abortController.signal,
    ).then((page) => {
      if (!abortController.signal.aborted) setEmployees(page.items);
    });
    return () => abortController.abort();
  }, []);

  function applyDateRange() {
    if (!isValidRange(draftFromDate, draftToDate)) return false;
    if (draftFromDate === fromDate && draftToDate === toDate) return true;
    setLoading(true);
    setFromDate(draftFromDate);
    setToDate(draftToDate);
    return true;
  }

  return {
    dashboard,
    draftFromDate,
    draftToDate,
    employeeId,
    employees,
    fromDate,
    loadError,
    loading,
    status,
    toDate,
    applyDateRange,
    reload: () => {
      setLoading(true);
      setReloadVersion((value) => value + 1);
    },
    setDraftFromDate,
    setDraftToDate,
    setEmployeeId: (value: string) => {
      if (value === employeeId) return;
      setLoading(true);
      setEmployeeId(value);
    },
    setStatus: (value: ReportingStatus | "") => {
      if (value === status) return;
      setLoading(true);
      setStatus(value);
    },
  };
}

function addDays(date: string, days: number): string {
  const value = new Date(`${date}T12:00:00Z`);
  value.setUTCDate(value.getUTCDate() + days);
  return value.toISOString().slice(0, 10);
}

function isValidRange(fromDate: string, toDate: string): boolean {
  const from = new Date(`${fromDate}T12:00:00Z`);
  const to = new Date(`${toDate}T12:00:00Z`);
  const days = Math.floor((to.valueOf() - from.valueOf()) / 86_400_000);
  return Number.isFinite(days) && days >= 0 && days <= 91;
}
