import { nullableString, requireRecord, requireString } from "@/shared/api/contractDecoder";
import { decodePage, type PageQuery, type PagedResponse } from "@/shared/api/paginationContract";

export interface AuditEntry {
  readonly id: string;
  readonly actorUserId: string;
  readonly actorName: string;
  readonly action: string;
  readonly targetType: string;
  readonly targetId: string;
  readonly summary: string | null;
  readonly occurredAtUtc: string;
}

export interface AuditQuery extends PageQuery {
  readonly fromDate?: string;
  readonly toDate?: string;
  readonly action?: string;
  readonly targetType?: string;
  readonly actorUserId?: string;
}

export const decodeAuditPage = (body: unknown): PagedResponse<AuditEntry> =>
  decodePage(body, decodeAuditEntry);

export function decodeAuditEntry(body: unknown): AuditEntry {
  const value = requireRecord(body, "audit entry");
  return {
    id: requireString(value.id, "audit.id"),
    actorUserId: requireString(value.actorUserId, "audit.actorUserId"),
    actorName: requireString(value.actorName, "audit.actorName"),
    action: requireString(value.action, "audit.action"),
    targetType: requireString(value.targetType, "audit.targetType"),
    targetId: requireString(value.targetId, "audit.targetId"),
    summary: nullableString(value.summary, "audit.summary"),
    occurredAtUtc: requireString(value.occurredAtUtc, "audit.occurredAtUtc"),
  };
}
