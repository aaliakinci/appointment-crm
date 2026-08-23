import { installCleanupOnSignals, runCompose } from "./lib/commands.mjs";

const projectName = "appointment-crm-container-smoke";
const postgresPort = process.env.APPOINTMENTCRM_SMOKE_POSTGRES_PORT ?? "55434";
const redisPort = process.env.APPOINTMENTCRM_SMOKE_REDIS_PORT ?? "56380";
const apiPort = process.env.APPOINTMENTCRM_SMOKE_API_PORT ?? "58081";
const webPort = process.env.APPOINTMENTCRM_SMOKE_WEB_PORT ?? "55174";
const password = "Smoke-local-2026!";
const baseUrl = `http://127.0.0.1:${apiPort}`;
const origin = `http://localhost:${webPort}`;
const employeeId = "60000000-0000-0000-0000-000000000001";
const serviceId = "50000000-0000-0000-0000-000000000001";
const customerId = "40000000-0000-0000-0000-000000000001";
const composeEnvironment = {
  APPOINTMENTCRM_POSTGRES_PORT: postgresPort,
  APPOINTMENTCRM_REDIS_PORT: redisPort,
  APPOINTMENTCRM_API_PORT: apiPort,
  APPOINTMENTCRM_WEB_PORT: webPort,
  APPOINTMENTCRM_DEMO_PASSWORD: password,
};

function invariant(condition, message) {
  if (!condition) {
    throw new Error(message);
  }
}

async function apiRequest(path, options = {}) {
  const headers = new Headers(options.headers);
  if (options.token) headers.set("Authorization", `Bearer ${options.token}`);
  if (options.trustedOrigin) headers.set("Origin", origin);
  if (options.body !== undefined)
    headers.set("Content-Type", "application/json");

  const response = await fetch(`${baseUrl}${path}`, {
    method: options.method ?? "GET",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });
  const text = await response.text();
  const expectedStatus = options.expectedStatus;
  const accepted =
    expectedStatus === undefined
      ? response.ok
      : Array.isArray(expectedStatus)
        ? expectedStatus.includes(response.status)
        : response.status === expectedStatus;
  if (!accepted) {
    throw new Error(
      `${options.method ?? "GET"} ${path} returned HTTP ${response.status}: ${text.slice(0, 500)}`,
    );
  }

  if (!text) return null;
  const contentType = response.headers.get("content-type") ?? "";
  return contentType.includes("json") ? JSON.parse(text) : text;
}

let cleaned = false;
async function cleanup() {
  if (cleaned) return;
  cleaned = true;
  await runCompose(projectName, ["down", "--volumes"], {
    env: composeEnvironment,
    allowFailure: true,
  });
}

installCleanupOnSignals(cleanup);

try {
  await runCompose(
    projectName,
    ["up", "--build", "--detach", "--wait", "--wait-timeout", "180"],
    { env: composeEnvironment },
  );

  const readiness = await apiRequest("/health/ready");
  invariant(readiness.status === "Healthy", "API readiness is not healthy.");
  invariant(
    readiness.checks?.["tenant-time-zones"]?.status === "Healthy",
    "Tenant time-zone readiness is not healthy.",
  );
  const dependencies = await apiRequest("/health/dependencies");
  invariant(
    dependencies.status === "Healthy",
    "API dependencies are not healthy.",
  );
  invariant(
    dependencies.checks?.["redis-cache"]?.status === "Healthy",
    "Redis dependency is not healthy.",
  );

  const authentication = await apiRequest("/api/v1/auth/login", {
    method: "POST",
    body: {
      email: "owner@demo.local",
      password,
      tenantId: "10000000-0000-0000-0000-000000000001",
    },
  });
  const token = authentication.accessToken;
  invariant(
    typeof token === "string" && token.length > 0,
    "Login did not return a token.",
  );

  const weekly = await apiRequest("/api/v1/scheduling/working-hours/tenant", {
    token,
  });
  const published = await apiRequest(
    "/api/v1/scheduling/working-hours/tenant",
    {
      method: "PUT",
      token,
      trustedOrigin: true,
      body: {
        expectedRevision: weekly.revision,
        periods: [1, 2, 3, 4, 5].map((dayOfWeek) => ({
          dayOfWeek,
          startMinute: 540,
          endMinute: 1_020,
        })),
        changeNote: "Container version smoke",
      },
    },
  );
  const history = await apiRequest(
    "/api/v1/scheduling/working-hours/tenant/versions?page=1&pageSize=10",
    { token },
  );
  const originalVersion = history.items?.find(
    (version) => version.versionNumber === 1,
  );
  invariant(
    history.totalCount >= 2 && originalVersion,
    "Schedule version history is incomplete.",
  );
  await apiRequest(
    `/api/v1/scheduling/working-hours/tenant/versions/${originalVersion.id}/restore`,
    {
      method: "POST",
      token,
      trustedOrigin: true,
      body: {
        expectedRevision: published.revision,
        changeNote: "Container restore smoke",
      },
    },
  );

  const availabilityPath =
    `/api/v1/availability?date=2035-01-15&employeeId=${employeeId}` +
    `&serviceId=${serviceId}`;
  await apiRequest(availabilityPath, { token });
  await apiRequest("/api/v1/scheduling/time-off", {
    method: "POST",
    token,
    trustedOrigin: true,
    body: {
      employeeId,
      startDate: "2035-01-15",
      startTime: "09:00:00",
      endDate: "2035-01-15",
      endTime: "10:00:00",
      timeZone: "Europe/Istanbul",
      reason: "Container timezone smoke",
    },
  });
  const timeOff = await apiRequest(
    "/api/v1/scheduling/time-off?fromDate=2035-01-15&toDate=2035-01-15",
    { token },
  );
  invariant(
    Array.isArray(timeOff) &&
      timeOff.length === 1 &&
      timeOff[0].timeZone === "Europe/Istanbul",
    "Time-off smoke response is invalid.",
  );

  const appointmentAvailability = await apiRequest(availabilityPath, { token });
  const startsAtUtc = appointmentAvailability.slots?.[0]?.startUtc;
  invariant(
    typeof startsAtUtc === "string",
    "No appointment slot was returned.",
  );
  const appointmentPayload = {
    customerId,
    employeeId,
    serviceId,
    startsAtUtc,
    notes: "Container appointment smoke",
  };
  const created = await apiRequest("/api/v1/appointments", {
    method: "POST",
    token,
    trustedOrigin: true,
    body: appointmentPayload,
    expectedStatus: 201,
  });
  const appointment = created.appointment;
  invariant(
    appointment?.status === "scheduled" &&
      appointment.serviceName === "Consultation",
    "Created appointment response is invalid.",
  );
  await apiRequest("/api/v1/appointments", {
    method: "POST",
    token,
    trustedOrigin: true,
    body: { ...appointmentPayload, notes: null },
    expectedStatus: 409,
  });

  const confirmed = await apiRequest(
    `/api/v1/appointments/${appointment.id}/confirm`,
    {
      method: "POST",
      token,
      trustedOrigin: true,
      body: { expectedRevision: appointment.revision, reason: null },
    },
  );
  invariant(
    confirmed.appointment?.status === "confirmed",
    "Appointment was not confirmed.",
  );
  await apiRequest(`/api/v1/appointments/${appointment.id}/cancel`, {
    method: "POST",
    token,
    trustedOrigin: true,
    body: {
      expectedRevision: confirmed.appointment.revision,
      reason: "Container cancellation smoke",
    },
  });
  await apiRequest("/api/v1/appointments", {
    method: "POST",
    token,
    trustedOrigin: true,
    body: { ...appointmentPayload, notes: "Rebooked after cancellation" },
    expectedStatus: 201,
  });

  const reporting = await apiRequest(
    `/api/v1/reporting/dashboard?fromDate=2035-01-15&toDate=2035-01-15&employeeId=${employeeId}`,
    { token },
  );
  invariant(
    reporting.range?.totalAppointments === 2 &&
      reporting.currency === "TRY" &&
      reporting.timeZone === "Europe/Istanbul",
    "Reporting smoke response is invalid.",
  );
  const customerHistory = await apiRequest(
    `/api/v1/customers/${customerId}/appointments?page=1&pageSize=20`,
    { token },
  );
  invariant(
    customerHistory.totalCount === 2 &&
      !customerHistory.items?.some((item) => item.customerId !== customerId),
    "Customer history smoke response is invalid.",
  );
  const profile = await apiRequest("/api/v1/account/profile", { token });
  invariant(
    profile.email === "owner@demo.local" &&
      profile.displayName === "Demo Owner",
    "Account profile smoke response is invalid.",
  );
  const sessions = await apiRequest("/api/v1/account/sessions", { token });
  invariant(
    Array.isArray(sessions) &&
      sessions.length === 1 &&
      sessions[0].isCurrent === true,
    "Account session smoke response is invalid.",
  );
  const memberships = await apiRequest("/api/v1/memberships", { token });
  invariant(
    Array.isArray(memberships) &&
      memberships.some(
        (membership) => membership.role === "Owner" && membership.isActive,
      ),
    "Membership smoke response is invalid.",
  );
  const audit = await apiRequest(
    "/api/v1/audit?action=appointment.created&page=1&pageSize=20",
    { token },
  );
  invariant(
    audit.totalCount === 2 &&
      !audit.items?.some((entry) => entry.action !== "appointment.created"),
    "Audit smoke response is invalid.",
  );

  console.log(
    "Container health, Redis dependency, scheduling, appointment, reporting, customer history, account, membership, and audit smoke passed.",
  );
} finally {
  await cleanup();
}
