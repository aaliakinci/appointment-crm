import {
  nullableString,
  requireArray,
  requireNumber,
  requireRecord,
  requireString,
} from "@/shared/api/contractDecoder";
import { decodePage, type PageQuery, type PagedResponse } from "@/shared/api/paginationContract";

export type AppointmentStatus = "scheduled" | "confirmed" | "completed" | "cancelled" | "no-show";

export type AppointmentScope = "tenant" | "own";

export interface Appointment {
  readonly id: string;
  readonly customerId: string;
  readonly customerName: string;
  readonly employeeId: string;
  readonly employeeName: string;
  readonly serviceId: string;
  readonly serviceName: string;
  readonly serviceDurationMinutes: number;
  readonly servicePrice: number;
  readonly serviceCurrency: string;
  readonly status: AppointmentStatus;
  readonly startsAtUtc: string;
  readonly endsAtUtc: string;
  readonly localStart: string;
  readonly localEnd: string;
  readonly timeZone: string;
  readonly notes: string | null;
  readonly revision: number;
  readonly createdAtUtc: string;
  readonly updatedAtUtc: string;
}

export interface AppointmentStatusHistory {
  readonly id: string;
  readonly fromStatus: AppointmentStatus | null;
  readonly toStatus: AppointmentStatus;
  readonly actorName: string;
  readonly reason: string | null;
  readonly occurredAtUtc: string;
}

export interface AppointmentDetail {
  readonly appointment: Appointment;
  readonly statusHistory: readonly AppointmentStatusHistory[];
}

export interface AppointmentQuery extends PageQuery {
  readonly fromDate: string;
  readonly toDate: string;
  readonly employeeId?: string;
  readonly customerId?: string;
  readonly status?: AppointmentStatus;
}

export interface CreateAppointmentInput {
  readonly customerId: string;
  readonly employeeId: string;
  readonly serviceId: string;
  readonly startsAtUtc: string;
  readonly notes: string | null;
}

export interface RescheduleAppointmentInput {
  readonly startsAtUtc: string;
  readonly expectedRevision: number;
}

export interface TransitionAppointmentInput {
  readonly expectedRevision: number;
  readonly reason: string | null;
}

export type AppointmentTransition = "confirm" | "complete" | "cancel" | "no-show";

export const decodeAppointmentPage = (body: unknown): PagedResponse<Appointment> =>
  decodePage(body, decodeAppointment);

export function decodeAppointmentDetail(body: unknown): AppointmentDetail {
  const value = requireRecord(body, "appointment detail");
  return {
    appointment: decodeAppointment(value.appointment),
    statusHistory: requireArray(value.statusHistory, "appointmentDetail.statusHistory").map(
      decodeStatusHistory,
    ),
  };
}

export function decodeAppointment(body: unknown): Appointment {
  const value = requireRecord(body, "appointment");
  return {
    id: requireString(value.id, "appointment.id"),
    customerId: requireString(value.customerId, "appointment.customerId"),
    customerName: requireString(value.customerName, "appointment.customerName"),
    employeeId: requireString(value.employeeId, "appointment.employeeId"),
    employeeName: requireString(value.employeeName, "appointment.employeeName"),
    serviceId: requireString(value.serviceId, "appointment.serviceId"),
    serviceName: requireString(value.serviceName, "appointment.serviceName"),
    serviceDurationMinutes: requireNumber(
      value.serviceDurationMinutes,
      "appointment.serviceDurationMinutes",
    ),
    servicePrice: requireNumber(value.servicePrice, "appointment.servicePrice"),
    serviceCurrency: requireString(value.serviceCurrency, "appointment.serviceCurrency"),
    status: decodeStatus(value.status, "appointment.status"),
    startsAtUtc: requireString(value.startsAtUtc, "appointment.startsAtUtc"),
    endsAtUtc: requireString(value.endsAtUtc, "appointment.endsAtUtc"),
    localStart: requireString(value.localStart, "appointment.localStart"),
    localEnd: requireString(value.localEnd, "appointment.localEnd"),
    timeZone: requireString(value.timeZone, "appointment.timeZone"),
    notes: nullableString(value.notes, "appointment.notes"),
    revision: requireNumber(value.revision, "appointment.revision"),
    createdAtUtc: requireString(value.createdAtUtc, "appointment.createdAtUtc"),
    updatedAtUtc: requireString(value.updatedAtUtc, "appointment.updatedAtUtc"),
  };
}

function decodeStatusHistory(body: unknown): AppointmentStatusHistory {
  const value = requireRecord(body, "appointment status history");
  return {
    id: requireString(value.id, "appointmentStatusHistory.id"),
    fromStatus:
      value.fromStatus === null
        ? null
        : decodeStatus(value.fromStatus, "appointmentStatusHistory.fromStatus"),
    toStatus: decodeStatus(value.toStatus, "appointmentStatusHistory.toStatus"),
    actorName: requireString(value.actorName, "appointmentStatusHistory.actorName"),
    reason: nullableString(value.reason, "appointmentStatusHistory.reason"),
    occurredAtUtc: requireString(value.occurredAtUtc, "appointmentStatusHistory.occurredAtUtc"),
  };
}

function decodeStatus(value: unknown, name: string): AppointmentStatus {
  const status = requireString(value, name);
  if (
    status !== "scheduled" &&
    status !== "confirmed" &&
    status !== "completed" &&
    status !== "cancelled" &&
    status !== "no-show"
  ) {
    throw new TypeError(`${name} is not valid.`);
  }

  return status;
}
