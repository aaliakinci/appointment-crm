export interface PageQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly search?: string;
  readonly sortBy?: string;
  readonly sortDirection?: "asc" | "desc";
}

export interface PagedResponse<T> {
  readonly items: readonly T[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
  readonly totalPages: number;
}

export interface Customer {
  readonly id: string;
  readonly name: string;
  readonly email: string | null;
  readonly phone: string | null;
  readonly notes: string | null;
  readonly archivedAtUtc: string | null;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}

export interface CustomerInput {
  readonly name: string;
  readonly email: string | null;
  readonly phone: string | null;
  readonly notes: string | null;
}

export interface CustomerQuery extends PageQuery {
  readonly includeArchived?: boolean;
}

export interface ServiceOffering {
  readonly id: string;
  readonly name: string;
  readonly durationMinutes: number;
  readonly price: number;
  readonly currency: string;
  readonly isActive: boolean;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}

export interface ServiceInput {
  readonly name: string;
  readonly durationMinutes: number;
  readonly price: number;
  readonly currency: string;
}

export interface ServiceQuery extends PageQuery {
  readonly isActive?: boolean;
}

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

export const decodeCustomerPage = (body: unknown): PagedResponse<Customer> =>
  decodePage(body, decodeCustomer);

export const decodeServicePage = (body: unknown): PagedResponse<ServiceOffering> =>
  decodePage(body, decodeService);

export const decodeEmployeePage = (body: unknown): PagedResponse<Employee> =>
  decodePage(body, decodeEmployee);

export function decodeCustomer(body: unknown): Customer {
  const value = requireRecord(body, "customer");
  return {
    id: requireString(value.id, "customer.id"),
    name: requireString(value.name, "customer.name"),
    email: nullableString(value.email, "customer.email"),
    phone: nullableString(value.phone, "customer.phone"),
    notes: nullableString(value.notes, "customer.notes"),
    archivedAtUtc: nullableString(value.archivedAtUtc, "customer.archivedAtUtc"),
    createdAtUtc: requireString(value.createdAtUtc, "customer.createdAtUtc"),
    updatedAtUtc: requireString(value.updatedAtUtc, "customer.updatedAtUtc"),
  };
}

export function decodeService(body: unknown): ServiceOffering {
  const value = requireRecord(body, "service");
  return {
    id: requireString(value.id, "service.id"),
    name: requireString(value.name, "service.name"),
    durationMinutes: requireNumber(value.durationMinutes, "service.durationMinutes"),
    price: requireNumber(value.price, "service.price"),
    currency: requireString(value.currency, "service.currency"),
    isActive: requireBoolean(value.isActive, "service.isActive"),
    createdAtUtc: requireString(value.createdAtUtc, "service.createdAtUtc"),
    updatedAtUtc: requireString(value.updatedAtUtc, "service.updatedAtUtc"),
  };
}

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

function decodePage<T>(body: unknown, decodeItem: (item: unknown) => T): PagedResponse<T> {
  const value = requireRecord(body, "paged response");
  return {
    items: requireArray(value.items, "pagedResponse.items").map(decodeItem),
    page: requireNumber(value.page, "pagedResponse.page"),
    pageSize: requireNumber(value.pageSize, "pagedResponse.pageSize"),
    totalCount: requireNumber(value.totalCount, "pagedResponse.totalCount"),
    totalPages: requireNumber(value.totalPages, "pagedResponse.totalPages"),
  };
}

function requireRecord(value: unknown, name: string): Record<string, unknown> {
  if (typeof value !== "object" || value === null || Array.isArray(value)) {
    throw new TypeError(`${name} must be an object.`);
  }

  return value as Record<string, unknown>;
}

function requireArray(value: unknown, name: string): readonly unknown[] {
  if (!Array.isArray(value)) {
    throw new TypeError(`${name} must be an array.`);
  }

  return value;
}

function requireString(value: unknown, name: string): string {
  if (typeof value !== "string" || value.length === 0) {
    throw new TypeError(`${name} must be a non-empty string.`);
  }

  return value;
}

function nullableString(value: unknown, name: string): string | null {
  return value === null ? null : requireString(value, name);
}

function requireNumber(value: unknown, name: string): number {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    throw new TypeError(`${name} must be a finite number.`);
  }

  return value;
}

function requireBoolean(value: unknown, name: string): boolean {
  if (typeof value !== "boolean") {
    throw new TypeError(`${name} must be a boolean.`);
  }

  return value;
}
