import { useEffect, useMemo, useState } from "react";

import { listCustomers, type Customer } from "@/features/customers/catalog";
import { listEmployees, type Employee } from "@/features/employees/catalog";
import { listServices, type ServiceOffering } from "@/features/services/catalog";

import { listAppointments } from "../api/appointmentApi";
import type {
  Appointment,
  AppointmentQuery,
  AppointmentScope,
  AppointmentStatus,
} from "../api/appointmentContract";
import {
  appointmentCalendarRange,
  appointmentCalendarSelectionChanged,
} from "../model/appointmentCalendar";
import { addDays, startOfIsoWeek, tenantToday, weekDates } from "../model/appointmentDate";

interface UseAppointmentCalendarOptions {
  readonly canManage: boolean;
  readonly scope: AppointmentScope;
  readonly timeZone: string;
}

export function useAppointmentCalendar({
  canManage,
  scope,
  timeZone,
}: UseAppointmentCalendarOptions) {
  const today = useMemo(() => tenantToday(timeZone), [timeZone]);
  const [viewMode, setViewMode] = useState<"day" | "week">("week");
  const [selectedDate, setSelectedDate] = useState(today);
  const weekStart = useMemo(() => startOfIsoWeek(selectedDate), [selectedDate]);
  const dates = useMemo(
    () => (viewMode === "day" ? [selectedDate] : weekDates(weekStart)),
    [selectedDate, viewMode, weekStart],
  );
  const [appointments, setAppointments] = useState<readonly Appointment[]>([]);
  const [customers, setCustomers] = useState<readonly Customer[]>([]);
  const [employees, setEmployees] = useState<readonly Employee[]>([]);
  const [services, setServices] = useState<readonly ServiceOffering[]>([]);
  const [statusFilter, setStatusFilter] = useState<AppointmentStatus | "">("");
  const [employeeFilter, setEmployeeFilter] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [catalogError, setCatalogError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    const range = appointmentCalendarRange(dates);
    const query: AppointmentQuery = {
      ...range,
      employeeId: employeeFilter || undefined,
      status: statusFilter || undefined,
      page: 1,
      pageSize: 100,
      sortBy: "start",
      sortDirection: "asc",
    };
    void loadAllAppointmentPages(scope, query, controller.signal)
      .then((items) => {
        if (!controller.signal.aborted) {
          setAppointments(items);
          setLoadError(false);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) setLoadError(true);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    return () => controller.abort();
  }, [dates, employeeFilter, reloadVersion, scope, statusFilter]);

  useEffect(() => {
    if (!canManage) return;
    const controller = new AbortController();
    void Promise.all([
      listCustomers(
        { page: 1, pageSize: 100, sortBy: "name", sortDirection: "asc" },
        controller.signal,
      ),
      listEmployees(
        { page: 1, pageSize: 100, isActive: true, sortBy: "name", sortDirection: "asc" },
        controller.signal,
      ),
      listServices(
        { page: 1, pageSize: 100, isActive: true, sortBy: "name", sortDirection: "asc" },
        controller.signal,
      ),
    ])
      .then(([customerPage, employeePage, servicePage]) => {
        if (!controller.signal.aborted) {
          setCustomers(customerPage.items.filter((customer) => customer.archivedAtUtc === null));
          setEmployees(employeePage.items.filter((employee) => employee.isActive));
          setServices(servicePage.items.filter((service) => service.isActive));
          setCatalogError(false);
        }
      })
      .catch(() => {
        if (!controller.signal.aborted) setCatalogError(true);
      });
    return () => controller.abort();
  }, [canManage, reloadVersion]);

  function reload() {
    setLoading(true);
    setReloadVersion((value) => value + 1);
  }

  return {
    appointments,
    catalogError,
    customers,
    dates,
    employeeFilter,
    employees,
    loadError,
    loading,
    services,
    statusFilter,
    timeZone,
    today,
    selectedDate,
    viewMode,
    weekStart,
    reload,
    setEmployeeFilter: (value: string) => {
      if (!appointmentCalendarSelectionChanged(employeeFilter, value)) return;
      setLoading(true);
      setEmployeeFilter(value);
    },
    setStatusFilter: (value: AppointmentStatus | "") => {
      if (!appointmentCalendarSelectionChanged(statusFilter, value)) return;
      setLoading(true);
      setStatusFilter(value);
    },
    previousPeriod: () => {
      setLoading(true);
      setSelectedDate((value) => addDays(value, viewMode === "day" ? -1 : -7));
    },
    nextPeriod: () => {
      setLoading(true);
      setSelectedDate((value) => addDays(value, viewMode === "day" ? 1 : 7));
    },
    currentPeriod: () => {
      if (!appointmentCalendarSelectionChanged(selectedDate, today)) return;
      setLoading(true);
      setSelectedDate(today);
    },
    setSelectedDate: (value: string) => {
      if (!appointmentCalendarSelectionChanged(selectedDate, value)) return;
      setLoading(true);
      setSelectedDate(value);
    },
    setViewMode: (value: "day" | "week") => {
      if (!appointmentCalendarSelectionChanged(viewMode, value)) return;
      setLoading(true);
      setViewMode(value);
    },
  };
}

async function loadAllAppointmentPages(
  scope: AppointmentScope,
  query: AppointmentQuery,
  signal: AbortSignal,
): Promise<readonly Appointment[]> {
  const first = await listAppointments(scope, query, signal);
  if (first.totalPages <= 1) return first.items;
  const remaining = await Promise.all(
    Array.from({ length: first.totalPages - 1 }, (_, index) =>
      listAppointments(scope, { ...query, page: index + 2 }, signal),
    ),
  );
  return [first, ...remaining].flatMap((page) => page.items);
}
