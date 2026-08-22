import {
  nullableString,
  requireArray,
  requireBoolean,
  requireRecord,
  requireString,
} from "@/shared/api/contractDecoder";
import { decodePage, type PageQuery, type PagedResponse } from "@/shared/api/paginationContract";

export interface EmployeeService {
  readonly id: string;
  readonly name: string;
  readonly isActive: boolean;
}

export interface Employee {
  readonly id: string;
  readonly userId: string | null;
  readonly name: string;
  readonly email: string | null;
  readonly phone: string | null;
  readonly isActive: boolean;
  readonly services: readonly EmployeeService[];
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}

export interface EmployeeInput {
  readonly userId: string | null;
  readonly name: string;
  readonly email: string | null;
  readonly phone: string | null;
}

export interface CreateEmployeeInput extends EmployeeInput {
  readonly serviceIds: readonly string[];
}

export interface EmployeeQuery extends PageQuery {
  readonly isActive?: boolean;
  readonly serviceId?: string;
}

export interface EmployeeUserOption {
  readonly userId: string;
  readonly displayName: string;
  readonly email: string;
  readonly role: string;
  readonly isLinked: boolean;
}

export const decodeEmployeePage = (body: unknown): PagedResponse<Employee> =>
  decodePage(body, decodeEmployee);

export function decodeEmployee(body: unknown): Employee {
  const value = requireRecord(body, "employee");
  return {
    id: requireString(value.id, "employee.id"),
    userId: nullableString(value.userId, "employee.userId"),
    name: requireString(value.name, "employee.name"),
    email: nullableString(value.email, "employee.email"),
    phone: nullableString(value.phone, "employee.phone"),
    isActive: requireBoolean(value.isActive, "employee.isActive"),
    services: requireArray(value.services, "employee.services").map(decodeEmployeeService),
    createdAtUtc: requireString(value.createdAtUtc, "employee.createdAtUtc"),
    updatedAtUtc: requireString(value.updatedAtUtc, "employee.updatedAtUtc"),
  };
}

export function decodeEmployeeUserOptions(body: unknown): readonly EmployeeUserOption[] {
  return requireArray(body, "employee user options").map((item) => {
    const value = requireRecord(item, "employee user option");
    return {
      userId: requireString(value.userId, "employeeUserOption.userId"),
      displayName: requireString(value.displayName, "employeeUserOption.displayName"),
      email: requireString(value.email, "employeeUserOption.email"),
      role: requireString(value.role, "employeeUserOption.role"),
      isLinked: requireBoolean(value.isLinked, "employeeUserOption.isLinked"),
    };
  });
}

function decodeEmployeeService(body: unknown): EmployeeService {
  const value = requireRecord(body, "employee service");
  return {
    id: requireString(value.id, "employeeService.id"),
    name: requireString(value.name, "employeeService.name"),
    isActive: requireBoolean(value.isActive, "employeeService.isActive"),
  };
}
