import { useEffect, useState } from "react";

import { listMemberships, type Membership } from "@/features/memberships/catalog";
import type { PagedResponse } from "@/shared/api";

import { listAuditEntries } from "../api/auditApi";
import type { AuditEntry } from "../api/auditContract";

const emptyPage: PagedResponse<AuditEntry> = {
  items: [],
  page: 1,
  pageSize: 20,
  totalCount: 0,
  totalPages: 0,
};

export function useAuditList() {
  const [result, setResult] = useState<PagedResponse<AuditEntry>>(emptyPage);
  const [memberships, setMemberships] = useState<readonly Membership[]>([]);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(20);
  const [draftSearch, setDraftSearch] = useState("");
  const [draftFromDate, setDraftFromDate] = useState("");
  const [draftToDate, setDraftToDate] = useState("");
  const [draftAction, setDraftAction] = useState("");
  const [draftTargetType, setDraftTargetType] = useState("");
  const [search, setSearch] = useState("");
  const [fromDate, setFromDate] = useState("");
  const [toDate, setToDate] = useState("");
  const [action, setAction] = useState("");
  const [targetType, setTargetType] = useState("");
  const [actorUserId, setActorUserId] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);
  const [reloadVersion, setReloadVersion] = useState(0);

  useEffect(() => {
    const abortController = new AbortController();
    void listAuditEntries(
      {
        page: page + 1,
        pageSize,
        search: search || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        action: action || undefined,
        targetType: targetType || undefined,
        actorUserId: actorUserId || undefined,
        sortBy: "occurredAt",
        sortDirection: "desc",
      },
      abortController.signal,
    )
      .then((value) => {
        if (!abortController.signal.aborted) {
          setResult(value);
          setLoadError(false);
        }
      })
      .catch(() => {
        if (!abortController.signal.aborted) setLoadError(true);
      })
      .finally(() => {
        if (!abortController.signal.aborted) setLoading(false);
      });
    return () => abortController.abort();
  }, [action, actorUserId, fromDate, page, pageSize, reloadVersion, search, targetType, toDate]);

  useEffect(() => {
    const abortController = new AbortController();
    void listMemberships(abortController.signal).then((items) => {
      if (!abortController.signal.aborted) setMemberships(items);
    });
    return () => abortController.abort();
  }, []);

  function applyFilters() {
    setLoading(true);
    setPage(0);
    setSearch(draftSearch.trim());
    setFromDate(draftFromDate);
    setToDate(draftToDate);
    setAction(draftAction.trim());
    setTargetType(draftTargetType.trim());
  }

  return {
    actorUserId,
    draftAction,
    draftFromDate,
    draftSearch,
    draftTargetType,
    draftToDate,
    loadError,
    loading,
    memberships,
    page,
    pageSize,
    result,
    applyFilters,
    reload: () => {
      setLoading(true);
      setReloadVersion((value) => value + 1);
    },
    setActorUserId: (value: string) => {
      setPage(0);
      setActorUserId(value);
    },
    setDraftAction,
    setDraftFromDate,
    setDraftSearch,
    setDraftTargetType,
    setDraftToDate,
    setPage: (value: number) => {
      setLoading(true);
      setPage(value);
    },
    setPageSize: (value: number) => {
      setLoading(true);
      setPage(0);
      setPageSize(value);
    },
  };
}
