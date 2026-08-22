import { requireArray, requireNumber, requireRecord } from "./contractDecoder";

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

export function decodePage<T>(body: unknown, decodeItem: (item: unknown) => T): PagedResponse<T> {
  const value = requireRecord(body, "paged response");
  return {
    items: requireArray(value.items, "pagedResponse.items").map(decodeItem),
    page: requireNumber(value.page, "pagedResponse.page"),
    pageSize: requireNumber(value.pageSize, "pagedResponse.pageSize"),
    totalCount: requireNumber(value.totalCount, "pagedResponse.totalCount"),
    totalPages: requireNumber(value.totalPages, "pagedResponse.totalPages"),
  };
}
