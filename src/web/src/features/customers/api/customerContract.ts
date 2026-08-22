import { nullableString, requireRecord, requireString } from "@/shared/api/contractDecoder";
import { decodePage, type PageQuery, type PagedResponse } from "@/shared/api/paginationContract";

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

export const decodeCustomerPage = (body: unknown): PagedResponse<Customer> =>
  decodePage(body, decodeCustomer);

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
