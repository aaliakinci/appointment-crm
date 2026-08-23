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
  const [weekStart, setWeekStart] = useState(() => startOfIsoWeek(today));
  const dates = useMemo(() => weekDates(weekStart), [weekStart]);
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
    const query: AppointmentQuery = {
      fromDate: dates[0]!,
      toDate: dates[6]!,
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
    weekStart,
    reload,
    setEmployeeFilter: (value: string) => {
      setLoading(true);
      setEmployeeFilter(value);
    },
    setStatusFilter: (value: AppointmentStatus | "") => {
      setLoading(true);
      setStatusFilter(value);
    },
    previousWeek: () => {
      setLoading(true);
      setWeekStart((value) => addDays(value, -7));
    },
    nextWeek: () => {
      setLoading(true);
      setWeekStart((value) => addDays(value, 7));
    },
    currentWeek: () => {
      setLoading(true);
      setWeekStart(startOfIsoWeek(today));
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
