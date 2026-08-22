import { appHttpClient, toQueryParams, type PagedResponse } from "@/shared/api";

import {
  decodeCustomer,
  decodeCustomerPage,
  type Customer,
  type CustomerInput,
  type CustomerQuery,
} from "./customerContract";

export function listCustomers(
  query: CustomerQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<Customer>> {
  return appHttpClient.getData<PagedResponse<Customer>>("/api/v1/customers", {
    signal,
    params: toQueryParams(query),
    decode: decodeCustomerPage,
    metadata: { operationName: "customers.list" },
  });
}

export function createCustomer(input: CustomerInput): Promise<Customer> {
  return appHttpClient.postData<Customer, CustomerInput>("/api/v1/customers", input, {
    decode: decodeCustomer,
    metadata: { operationName: "customers.create", replay: "deny" },
  });
}

export function updateCustomer(customerId: string, input: CustomerInput): Promise<Customer> {
  return appHttpClient.putData<Customer, CustomerInput>(
    `/api/v1/customers/${encodeURIComponent(customerId)}`,
    input,
    {
      decode: decodeCustomer,
      metadata: { operationName: "customers.update", replay: "deny" },
    },
  );
}

export function archiveCustomer(customerId: string): Promise<void> {
  return appHttpClient.deleteData(`/api/v1/customers/${encodeURIComponent(customerId)}`, {
    metadata: { operationName: "customers.archive", replay: "deny" },
  });
}
