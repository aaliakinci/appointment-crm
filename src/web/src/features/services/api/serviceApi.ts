import { appHttpClient, toQueryParams, type PagedResponse } from "@/shared/api";

import {
  decodeService,
  decodeServicePage,
  type ServiceInput,
  type ServiceOffering,
  type ServiceQuery,
} from "./serviceContract";

export function listServices(
  query: ServiceQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<ServiceOffering>> {
  return appHttpClient.getData<PagedResponse<ServiceOffering>>("/api/v1/services", {
    signal,
    params: toQueryParams(query),
    decode: decodeServicePage,
    metadata: { operationName: "services.list" },
  });
}

export function createService(input: ServiceInput): Promise<ServiceOffering> {
  return appHttpClient.postData<ServiceOffering, ServiceInput>("/api/v1/services", input, {
    decode: decodeService,
    metadata: { operationName: "services.create", replay: "deny" },
  });
}

export function updateService(serviceId: string, input: ServiceInput): Promise<ServiceOffering> {
  return appHttpClient.putData<ServiceOffering, ServiceInput>(
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
  return appHttpClient.postData<ServiceOffering, Record<string, never>>(
    `/api/v1/services/${encodeURIComponent(serviceId)}/${operation}`,
    {},
    {
      decode: decodeService,
      metadata: { operationName: `services.${operation}`, replay: "deny" },
    },
  );
}
