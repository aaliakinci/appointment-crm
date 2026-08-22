import { createLilyHttpClient } from "@lily_platform/lily_ui/http";

import { authSessionStore } from "@/state/authSessionStore";

import { resolveApiBaseUrl } from "./apiBaseUrl";
import { mapApiProblemError } from "./apiProblemContract";
import {
  decodeAuthenticationResponse,
  decodeTenantOptions,
  type AuthenticationResponse,
  type LoginRequest,
  type TenantOption,
} from "./authContract";
import { decodeHealthReport, type HealthReport } from "./healthContract";
import {
  decodeCustomer,
  decodeCustomerPage,
  decodeEmployee,
  decodeEmployeePage,
  decodeEmployeeUserOptions,
  decodeService,
  decodeServicePage,
  type CreateEmployeeInput,
  type Customer,
  type CustomerInput,
  type CustomerQuery,
  type Employee,
  type EmployeeInput,
  type EmployeeQuery,
  type EmployeeUserOption,
  type PagedResponse,
  type ServiceInput,
  type ServiceOffering,
  type ServiceQuery,
} from "./masterDataContract";

const configuredApiBaseUrl: unknown = import.meta.env.VITE_APPOINTMENT_CRM_API_URL;
let recoveryRefreshPromise: Promise<void> | null = null;

export const appApiClient = createLilyHttpClient({
  baseURL: resolveApiBaseUrl(
    typeof configuredApiBaseUrl === "string" ? configuredApiBaseUrl : undefined,
  ),
  timeoutMs: 10_000,
  credentials: "include",
  defaultHeaders: { Accept: "application/json" },
  mapApiError: mapApiProblemError,
  credentialProvider: {
    getAccessToken: () => authSessionStore.getAccessToken(),
  },
  authRecovery: {
    shouldRecover: ({ status }) => status === 401,
    refresh: async () => {
      recoveryRefreshPromise ??= refreshAuthentication()
        .then(() => undefined)
        .finally(() => {
          recoveryRefreshPromise = null;
        });
      await recoveryRefreshPromise;
    },
    onSessionExpired: () => authSessionStore.clear(),
  },
});

export function getReadiness(signal?: AbortSignal): Promise<HealthReport> {
  return appApiClient.getData<HealthReport>("/health/ready", {
    signal,
    decode: decodeHealthReport,
    metadata: { operationName: "system.readiness" },
  });
}

export async function login(request: LoginRequest): Promise<AuthenticationResponse> {
  const response = await appApiClient.postData<AuthenticationResponse, LoginRequest>(
    "/api/v1/auth/login",
    request,
    {
      decode: decodeAuthenticationResponse,
      metadata: {
        auth: "none",
        authRecovery: "none",
        operationName: "identity.login",
        replay: "deny",
      },
    },
  );
  if (!response.requiresTenantSelection) {
    authSessionStore.setAuthentication(response);
  }

  return response;
}

export async function refreshAuthentication(): Promise<AuthenticationResponse> {
  const response = await appApiClient.postData<AuthenticationResponse, Record<string, never>>(
    "/api/v1/auth/refresh",
    {},
    {
      decode: decodeAuthenticationResponse,
      metadata: {
        auth: "none",
        authRecovery: "none",
        operationName: "identity.refresh",
        replay: "deny",
      },
    },
  );
  authSessionStore.setAuthentication(response);
  return response;
}

export async function logout(): Promise<void> {
  try {
    await appApiClient.postData<void, Record<string, never>>(
      "/api/v1/auth/logout",
      {},
      {
        metadata: {
          authRecovery: "none",
          operationName: "identity.logout",
          replay: "deny",
        },
      },
    );
  } finally {
    authSessionStore.clear();
  }
}

export async function revokeAllSessions(): Promise<void> {
  try {
    await appApiClient.postData<void, Record<string, never>>(
      "/api/v1/auth/revoke-all",
      {},
      {
        metadata: {
          authRecovery: "none",
          operationName: "identity.revoke-all",
          replay: "deny",
        },
      },
    );
  } finally {
    authSessionStore.clear();
  }
}

export async function switchTenant(tenantId: string): Promise<AuthenticationResponse> {
  const response = await appApiClient.postData<AuthenticationResponse, { tenantId: string }>(
    "/api/v1/auth/switch-tenant",
    { tenantId },
    {
      decode: decodeAuthenticationResponse,
      metadata: {
        authRecovery: "none",
        operationName: "identity.switch-tenant",
        replay: "deny",
      },
    },
  );
  authSessionStore.setAuthentication(response);
  return response;
}

export function listAvailableTenants(signal?: AbortSignal): Promise<readonly TenantOption[]> {
  return appApiClient.getData<readonly TenantOption[]>("/api/v1/auth/tenants", {
    signal,
    decode: decodeTenantOptions,
    metadata: { operationName: "identity.list-tenants" },
  });
}

export function listCustomers(
  query: CustomerQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<Customer>> {
  return appApiClient.getData<PagedResponse<Customer>>("/api/v1/customers", {
    signal,
    params: toParams(query),
    decode: decodeCustomerPage,
    metadata: { operationName: "customers.list" },
  });
}

export function createCustomer(input: CustomerInput): Promise<Customer> {
  return appApiClient.postData<Customer, CustomerInput>("/api/v1/customers", input, {
    decode: decodeCustomer,
    metadata: { operationName: "customers.create", replay: "deny" },
  });
}

export function updateCustomer(customerId: string, input: CustomerInput): Promise<Customer> {
  return appApiClient.putData<Customer, CustomerInput>(
    `/api/v1/customers/${encodeURIComponent(customerId)}`,
    input,
    {
      decode: decodeCustomer,
      metadata: { operationName: "customers.update", replay: "deny" },
    },
  );
}

export function archiveCustomer(customerId: string): Promise<void> {
  return appApiClient.deleteData(`/api/v1/customers/${encodeURIComponent(customerId)}`, {
    metadata: { operationName: "customers.archive", replay: "deny" },
  });
}

export function listServices(
  query: ServiceQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<ServiceOffering>> {
  return appApiClient.getData<PagedResponse<ServiceOffering>>("/api/v1/services", {
    signal,
    params: toParams(query),
    decode: decodeServicePage,
    metadata: { operationName: "services.list" },
  });
}

export function createService(input: ServiceInput): Promise<ServiceOffering> {
  return appApiClient.postData<ServiceOffering, ServiceInput>("/api/v1/services", input, {
    decode: decodeService,
    metadata: { operationName: "services.create", replay: "deny" },
  });
}

export function updateService(serviceId: string, input: ServiceInput): Promise<ServiceOffering> {
  return appApiClient.putData<ServiceOffering, ServiceInput>(
    `/api/v1/services/${encodeURIComponent(serviceId)}`,
    input,
    {
      decode: decodeService,
      metadata: { operationName: "services.update", replay: "deny" },
    },
  );
}

export function setServiceActive(serviceId: string, isActive: boolean): Promise<ServiceOffering> {
  const operation = isActive ? "activate" : "deactivate";
  return appApiClient.postData<ServiceOffering, Record<string, never>>(
    `/api/v1/services/${encodeURIComponent(serviceId)}/${operation}`,
    {},
    {
      decode: decodeService,
      metadata: { operationName: `services.${operation}`, replay: "deny" },
    },
  );
}

export function listEmployees(
  query: EmployeeQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<Employee>> {
  return appApiClient.getData<PagedResponse<Employee>>("/api/v1/employees", {
    signal,
    params: toParams(query),
    decode: decodeEmployeePage,
    metadata: { operationName: "employees.list" },
  });
}

export function createEmployee(input: CreateEmployeeInput): Promise<Employee> {
  return appApiClient.postData<Employee, CreateEmployeeInput>("/api/v1/employees", input, {
    decode: decodeEmployee,
    metadata: { operationName: "employees.create", replay: "deny" },
  });
}

export function updateEmployee(employeeId: string, input: EmployeeInput): Promise<Employee> {
  return appApiClient.putData<Employee, EmployeeInput>(
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
  return appApiClient.putData<Employee, { readonly serviceIds: readonly string[] }>(
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
  return appApiClient.postData<Employee, Record<string, never>>(
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
  return appApiClient.getData<readonly EmployeeUserOption[]>("/api/v1/employees/user-options", {
    signal,
    decode: decodeEmployeeUserOptions,
    metadata: { operationName: "employees.list-user-options" },
  });
}

function toParams<T extends object>(query: T): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== ""),
  );
}
