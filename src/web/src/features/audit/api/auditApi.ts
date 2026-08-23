import { appHttpClient, toQueryParams, type PagedResponse } from "@/shared/api";

import { decodeAuditPage, type AuditEntry, type AuditQuery } from "./auditContract";

export function listAuditEntries(
  query: AuditQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<AuditEntry>> {
  return appHttpClient.getData<PagedResponse<AuditEntry>>("/api/v1/audit", {
    signal,
    params: toQueryParams(query),
    decode: decodeAuditPage,
    metadata: { operationName: "audit.list" },
  });
}
