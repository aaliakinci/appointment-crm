import { describe, expect, it } from "vitest";

import { decodeAppointmentDetail, decodeAppointmentPage } from "./appointmentContract";

const appointment = {
  id: "appointment-1",
  customerId: "customer-1",
  customerName: "Ada",
  employeeId: "employee-1",
  employeeName: "Grace",
  serviceId: "service-1",
  serviceName: "Consultation",
  serviceDurationMinutes: 30,
  servicePrice: 750,
  serviceCurrency: "TRY",
  status: "scheduled",
  startsAtUtc: "2026-08-24T07:00:00Z",
  endsAtUtc: "2026-08-24T07:30:00Z",
  localStart: "2026-08-24T10:00:00+03:00",
  localEnd: "2026-08-24T10:30:00+03:00",
  timeZone: "Europe/Istanbul",
  notes: null,
  revision: 1,
  createdAtUtc: "2026-08-23T10:00:00Z",
  updatedAtUtc: "2026-08-23T10:00:00Z",
};

describe("appointment API contract", () => {
  it("decodes appointment pages and snapshot values", () => {
    const page = decodeAppointmentPage({
      items: [appointment],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1,
    });
    expect(page.items[0]).toMatchObject({ serviceName: "Consultation", revision: 1 });
  });

  it("decodes status history and rejects unknown statuses", () => {
    expect(
      decodeAppointmentDetail({
        appointment,
        statusHistory: [
          {
            id: "history-1",
            fromStatus: null,
            toStatus: "scheduled",
            actorName: "Receptionist",
            reason: null,
            occurredAtUtc: "2026-08-23T10:00:00Z",
          },
        ],
      }).statusHistory,
    ).toHaveLength(1);
    expect(() =>
      decodeAppointmentDetail({
        appointment: { ...appointment, status: "waiting" },
        statusHistory: [],
      }),
    ).toThrow(TypeError);
  });
});
