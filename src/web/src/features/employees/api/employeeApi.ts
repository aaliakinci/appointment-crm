import { appHttpClient, toQueryParams, type PagedResponse } from "@/shared/api";

import {
  decodeEmployee,
  decodeEmployeePage,
  decodeEmployeeUserOptions,
  type CreateEmployeeInput,
  type Employee,
  type EmployeeInput,
  type EmployeeQuery,
  type EmployeeUserOption,
} from "./employeeContract";

export function listEmployees(
  query: EmployeeQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<Employee>> {
  return appHttpClient.getData<PagedResponse<Employee>>("/api/v1/employees", {
    signal,
    params: toQueryParams(query),
    decode: decodeEmployeePage,
    metadata: { operationName: "employees.list" },
  });
}

export function createEmployee(input: CreateEmployeeInput): Promise<Employee> {
  return appHttpClient.postData<Employee, CreateEmployeeInput>("/api/v1/employees", input, {
    decode: decodeEmployee,
    metadata: { operationName: "employees.create", replay: "deny" },
  });
}

export function updateEmployee(employeeId: string, input: EmployeeInput): Promise<Employee> {
  return appHttpClient.putData<Employee, EmployeeInput>(
    `/api/v1/employees/${encodeURIComponent(employeeId)}`,
    input,
    {
      decode: decodeEmployee,
      metadata: { operationName: "employees.update", replay: "deny" },
    },
  );
}

export function setEmployeeServices(
  employeeId: string,
  serviceIds: readonly string[],
): Promise<Employee> {
  return appHttpClient.putData<Employee, { readonly serviceIds: readonly string[] }>(
    `/api/v1/employees/${encodeURIComponent(employeeId)}/services`,
    { serviceIds },
    {
      decode: decodeEmployee,
      metadata: { operationName: "employees.set-services", replay: "deny" },
    },
  );
}

export function setEmployeeActive(employeeId: string, isActive: boolean): Promise<Employee> {
  const operation = isActive ? "activate" : "deactivate";
  return appHttpClient.postData<Employee, Record<string, never>>(
    `/api/v1/employees/${encodeURIComponent(employeeId)}/${operation}`,
    {},
    {
      decode: decodeEmployee,
      metadata: { operationName: `employees.${operation}`, replay: "deny" },
    },
  );
}

export function listEmployeeUserOptions(
  signal?: AbortSignal,
): Promise<readonly EmployeeUserOption[]> {
  return appHttpClient.getData<readonly EmployeeUserOption[]>("/api/v1/employees/user-options", {
    signal,
    decode: decodeEmployeeUserOptions,
    metadata: { operationName: "employees.list-user-options" },
  });
}
