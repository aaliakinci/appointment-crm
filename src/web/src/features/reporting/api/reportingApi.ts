import { appHttpClient, toQueryParams } from "@/shared/api";

import {
  decodeReportingDashboard,
  type ReportingDashboard,
  type ReportingQuery,
} from "./reportingContract";

export function getReportingDashboard(
  query: ReportingQuery,
  signal?: AbortSignal,
): Promise<ReportingDashboard> {
  return appHttpClient.getData<ReportingDashboard>("/api/v1/reporting/dashboard", {
    signal,
    params: toQueryParams(query),
    decode: decodeReportingDashboard,
    metadata: { operationName: "reporting.dashboard" },
  });
}
