import {
  requireBoolean,
  requireNumber,
  requireRecord,
  requireString,
} from "@/shared/api/contractDecoder";
import { decodePage, type PageQuery, type PagedResponse } from "@/shared/api/paginationContract";

export interface ServiceOffering {
  readonly id: string;
  readonly name: string;
  readonly durationMinutes: number;
  readonly price: number;
  readonly currency: string;
  readonly isActive: boolean;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}

export interface ServiceInput {
  readonly name: string;
  readonly durationMinutes: number;
  readonly price: number;
  readonly currency: string;
}

export interface ServiceQuery extends PageQuery {
  readonly isActive?: boolean;
}

export const decodeServicePage = (body: unknown): PagedResponse<ServiceOffering> =>
  decodePage(body, decodeService);

export function decodeService(body: unknown): ServiceOffering {
  const value = requireRecord(body, "service");
  return {
    id: requireString(value.id, "service.id"),
    name: requireString(value.name, "service.name"),
    durationMinutes: requireNumber(value.durationMinutes, "service.durationMinutes"),
    price: requireNumber(value.price, "service.price"),
    currency: requireString(value.currency, "service.currency"),
    isActive: requireBoolean(value.isActive, "service.isActive"),
    createdAtUtc: requireString(value.createdAtUtc, "service.createdAtUtc"),
    updatedAtUtc: requireString(value.updatedAtUtc, "service.updatedAtUtc"),
  };
}
