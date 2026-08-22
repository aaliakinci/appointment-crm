export function toQueryParams<T extends object>(query: T): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(query).filter(([, value]) => value !== undefined && value !== ""),
  );
}
