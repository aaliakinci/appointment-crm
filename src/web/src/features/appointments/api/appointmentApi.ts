import { appHttpClient, toQueryParams, type PagedResponse } from "@/shared/api";

import {
  decodeAppointmentDetail,
  decodeAppointmentPage,
  type Appointment,
  type AppointmentDetail,
  type AppointmentQuery,
  type AppointmentScope,
  type AppointmentTransition,
  type CreateAppointmentInput,
  type RescheduleAppointmentInput,
  type TransitionAppointmentInput,
} from "./appointmentContract";

function collectionPath(scope: AppointmentScope): string {
  return scope === "tenant" ? "/api/v1/appointments" : "/api/v1/my/appointments";
}

export function listAppointments(
  scope: AppointmentScope,
  query: AppointmentQuery,
  signal?: AbortSignal,
): Promise<PagedResponse<Appointment>> {
  return appHttpClient.getData<PagedResponse<Appointment>>(collectionPath(scope), {
    signal,
    params: toQueryParams(query),
    decode: decodeAppointmentPage,
    metadata: { operationName: `appointments.${scope}.list` },
  });
}

export function listCustomerAppointmentHistory(
  customerId: string,
  query: Pick<AppointmentQuery, "page" | "pageSize" | "search" | "sortBy" | "sortDirection">,
  signal?: AbortSignal,
): Promise<PagedResponse<Appointment>> {
  return appHttpClient.getData<PagedResponse<Appointment>>(
    `/api/v1/customers/${encodeURIComponent(customerId)}/appointments`,
    {
      signal,
      params: toQueryParams(query),
      decode: decodeAppointmentPage,
      metadata: { operationName: "appointments.customer-history" },
    },
  );
}

export function getAppointment(
  scope: AppointmentScope,
  appointmentId: string,
  signal?: AbortSignal,
): Promise<AppointmentDetail> {
  return appHttpClient.getData<AppointmentDetail>(
    `${collectionPath(scope)}/${encodeURIComponent(appointmentId)}`,
    {
      signal,
      decode: decodeAppointmentDetail,
      metadata: { operationName: `appointments.${scope}.get` },
    },
  );
}

export function createAppointment(input: CreateAppointmentInput): Promise<AppointmentDetail> {
  return appHttpClient.postData<AppointmentDetail, CreateAppointmentInput>(
    "/api/v1/appointments",
    input,
    {
      decode: decodeAppointmentDetail,
      metadata: { operationName: "appointments.create", replay: "deny" },
    },
  );
}

export function rescheduleAppointment(
  appointmentId: string,
  input: RescheduleAppointmentInput,
): Promise<AppointmentDetail> {
  return appHttpClient.putData<AppointmentDetail, RescheduleAppointmentInput>(
    `/api/v1/appointments/${encodeURIComponent(appointmentId)}/schedule`,
    input,
    {
      decode: decodeAppointmentDetail,
      metadata: { operationName: "appointments.reschedule", replay: "deny" },
    },
  );
}

export function transitionAppointment(
  scope: AppointmentScope,
  appointmentId: string,
  transition: AppointmentTransition,
  input: TransitionAppointmentInput,
): Promise<AppointmentDetail> {
  return appHttpClient.postData<AppointmentDetail, TransitionAppointmentInput>(
    `${collectionPath(scope)}/${encodeURIComponent(appointmentId)}/${transition}`,
    input,
    {
      decode: decodeAppointmentDetail,
      metadata: { operationName: `appointments.${scope}.${transition}`, replay: "deny" },
    },
  );
}
