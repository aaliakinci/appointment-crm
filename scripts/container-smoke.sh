#!/usr/bin/env sh
set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repository_root"

smoke_project=appointment-crm-container-smoke
smoke_postgres_port=${APPOINTMENTCRM_SMOKE_POSTGRES_PORT:-55434}
smoke_redis_port=${APPOINTMENTCRM_SMOKE_REDIS_PORT:-56380}
smoke_api_port=${APPOINTMENTCRM_SMOKE_API_PORT:-58081}
smoke_web_port=${APPOINTMENTCRM_SMOKE_WEB_PORT:-55174}
smoke_password=Smoke-local-2026!

cleanup_smoke() {
  APPOINTMENTCRM_POSTGRES_PORT="$smoke_postgres_port" \
    APPOINTMENTCRM_REDIS_PORT="$smoke_redis_port" \
    APPOINTMENTCRM_API_PORT="$smoke_api_port" \
    APPOINTMENTCRM_WEB_PORT="$smoke_web_port" \
    docker compose --project-name "$smoke_project" down --volumes >/dev/null
}
trap cleanup_smoke EXIT

APPOINTMENTCRM_POSTGRES_PORT="$smoke_postgres_port" \
  APPOINTMENTCRM_REDIS_PORT="$smoke_redis_port" \
  APPOINTMENTCRM_API_PORT="$smoke_api_port" \
  APPOINTMENTCRM_WEB_PORT="$smoke_web_port" \
  APPOINTMENTCRM_DEMO_PASSWORD="$smoke_password" \
  docker compose --project-name "$smoke_project" up --build --detach

attempt=0
until readiness=$(curl --fail --silent --show-error \
  "http://127.0.0.1:$smoke_api_port/health/ready"); do
  attempt=$((attempt + 1))
  if [ "$attempt" -ge 90 ]; then
    docker compose --project-name "$smoke_project" logs api migrate
    echo "Container smoke environment did not become ready." >&2
    exit 1
  fi
  sleep 1
done

printf '%s' "$readiness" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (body.status !== "Healthy" || body.checks?.["tenant-time-zones"]?.status !== "Healthy") {
    process.exit(1);
  }
});'

dependencies=$(curl --fail --silent --show-error \
  "http://127.0.0.1:$smoke_api_port/health/dependencies")
printf '%s' "$dependencies" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (body.status !== "Healthy" || body.checks?.["redis-cache"]?.status !== "Healthy") {
    process.exit(1);
  }
});'

login_response=$(curl --fail-with-body --silent --show-error \
  --request POST \
  --header "Content-Type: application/json" \
  --data "{\"email\":\"owner@demo.local\",\"password\":\"$smoke_password\",\"tenantId\":\"10000000-0000-0000-0000-000000000001\"}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/auth/login")
access_token=$(printf '%s' "$login_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => process.stdout.write(JSON.parse(input).accessToken));')

authorization="Authorization: Bearer $access_token"
origin="Origin: http://localhost:$smoke_web_port"
employee_id=60000000-0000-0000-0000-000000000001
service_id=50000000-0000-0000-0000-000000000001
customer_id=40000000-0000-0000-0000-000000000001

weekly_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/scheduling/working-hours/tenant")
weekly_revision=$(printf '%s' "$weekly_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => process.stdout.write(String(JSON.parse(input).revision)));')

published_weekly=$(curl --fail-with-body --silent --show-error \
  --request PUT \
  --header "$authorization" \
  --header "$origin" \
  --header "Content-Type: application/json" \
  --data "{\"expectedRevision\":$weekly_revision,\"periods\":[{\"dayOfWeek\":1,\"startMinute\":540,\"endMinute\":1020},{\"dayOfWeek\":2,\"startMinute\":540,\"endMinute\":1020},{\"dayOfWeek\":3,\"startMinute\":540,\"endMinute\":1020},{\"dayOfWeek\":4,\"startMinute\":540,\"endMinute\":1020},{\"dayOfWeek\":5,\"startMinute\":540,\"endMinute\":1020}],\"changeNote\":\"Container version smoke\"}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/scheduling/working-hours/tenant")
published_revision=$(printf '%s' "$published_weekly" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => process.stdout.write(String(JSON.parse(input).revision)));')

history_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/scheduling/working-hours/tenant/versions?page=1&pageSize=10")
original_version_id=$(printf '%s' "$history_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  const original = body.items?.find(version => version.versionNumber === 1);
  if (body.totalCount < 2 || original == null) process.exit(1);
  process.stdout.write(original.id);
});')

curl --fail-with-body --silent --show-error \
  --request POST \
  --header "$authorization" \
  --header "$origin" \
  --header "Content-Type: application/json" \
  --data "{\"expectedRevision\":$published_revision,\"changeNote\":\"Container restore smoke\"}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/scheduling/working-hours/tenant/versions/$original_version_id/restore" \
  >/dev/null

curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/availability?date=2035-01-15&employeeId=$employee_id&serviceId=$service_id" \
  >/dev/null

curl --fail-with-body --silent --show-error \
  --request POST \
  --header "$authorization" \
  --header "$origin" \
  --header "Content-Type: application/json" \
  --data "{\"employeeId\":\"$employee_id\",\"startDate\":\"2035-01-15\",\"startTime\":\"09:00:00\",\"endDate\":\"2035-01-15\",\"endTime\":\"10:00:00\",\"timeZone\":\"Europe/Istanbul\",\"reason\":\"Container timezone smoke\"}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/scheduling/time-off" \
  >/dev/null

time_off_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/scheduling/time-off?fromDate=2035-01-15&toDate=2035-01-15")
printf '%s' "$time_off_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (!Array.isArray(body) || body.length !== 1 || body[0].timeZone !== "Europe/Istanbul") {
    process.exit(1);
  }
});'

appointment_availability=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/availability?date=2035-01-15&employeeId=$employee_id&serviceId=$service_id")
appointment_start=$(printf '%s' "$appointment_availability" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const first = JSON.parse(input).slots?.[0];
  if (first == null) process.exit(1);
  process.stdout.write(first.startUtc);
});')

appointment_response=$(curl --fail-with-body --silent --show-error \
  --request POST \
  --header "$authorization" \
  --header "$origin" \
  --header "Content-Type: application/json" \
  --data "{\"customerId\":\"$customer_id\",\"employeeId\":\"$employee_id\",\"serviceId\":\"$service_id\",\"startsAtUtc\":\"$appointment_start\",\"notes\":\"Container appointment smoke\"}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/appointments")
appointment_identity=$(printf '%s' "$appointment_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const appointment = JSON.parse(input).appointment;
  if (appointment?.status !== "scheduled" || appointment.serviceName !== "Consultation") {
    process.exit(1);
  }
  process.stdout.write(`${appointment.id} ${appointment.revision}`);
});')
set -- $appointment_identity
appointment_id=$1
appointment_revision=$2

conflict_status=$(curl --silent --show-error \
  --output /dev/null \
  --write-out "%{http_code}" \
  --request POST \
  --header "$authorization" \
  --header "$origin" \
  --header "Content-Type: application/json" \
  --data "{\"customerId\":\"$customer_id\",\"employeeId\":\"$employee_id\",\"serviceId\":\"$service_id\",\"startsAtUtc\":\"$appointment_start\",\"notes\":null}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/appointments")
if [ "$conflict_status" != "409" ]; then
  echo "Expected an appointment conflict but received HTTP $conflict_status." >&2
  exit 1
fi

confirmed_response=$(curl --fail-with-body --silent --show-error \
  --request POST \
  --header "$authorization" \
  --header "$origin" \
  --header "Content-Type: application/json" \
  --data "{\"expectedRevision\":$appointment_revision,\"reason\":null}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/appointments/$appointment_id/confirm")
confirmed_revision=$(printf '%s' "$confirmed_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const appointment = JSON.parse(input).appointment;
  if (appointment?.status !== "confirmed") process.exit(1);
  process.stdout.write(String(appointment.revision));
});')

curl --fail-with-body --silent --show-error \
  --request POST \
  --header "$authorization" \
  --header "$origin" \
  --header "Content-Type: application/json" \
  --data "{\"expectedRevision\":$confirmed_revision,\"reason\":\"Container cancellation smoke\"}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/appointments/$appointment_id/cancel" \
  >/dev/null

curl --fail-with-body --silent --show-error \
  --request POST \
  --header "$authorization" \
  --header "$origin" \
  --header "Content-Type: application/json" \
  --data "{\"customerId\":\"$customer_id\",\"employeeId\":\"$employee_id\",\"serviceId\":\"$service_id\",\"startsAtUtc\":\"$appointment_start\",\"notes\":\"Rebooked after cancellation\"}" \
  "http://127.0.0.1:$smoke_api_port/api/v1/appointments" \
  >/dev/null

reporting_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/reporting/dashboard?fromDate=2035-01-15&toDate=2035-01-15&employeeId=$employee_id")
printf '%s' "$reporting_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (body.range?.totalAppointments !== 2 || body.currency !== "TRY" || body.timeZone !== "Europe/Istanbul") {
    process.exit(1);
  }
});'

customer_history_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/customers/$customer_id/appointments?page=1&pageSize=20")
printf '%s' "$customer_history_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (body.totalCount !== 2 || body.items?.some(item => item.customerId !== "40000000-0000-0000-0000-000000000001")) {
    process.exit(1);
  }
});'

profile_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/account/profile")
printf '%s' "$profile_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (body.email !== "owner@demo.local" || body.displayName !== "Demo Owner") process.exit(1);
});'

sessions_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/account/sessions")
printf '%s' "$sessions_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (!Array.isArray(body) || body.length !== 1 || body[0].isCurrent !== true) process.exit(1);
});'

memberships_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/memberships")
printf '%s' "$memberships_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (!Array.isArray(body) || !body.some(item => item.role === "Owner" && item.isActive)) process.exit(1);
});'

audit_response=$(curl --fail-with-body --silent --show-error \
  --header "$authorization" \
  "http://127.0.0.1:$smoke_api_port/api/v1/audit?action=appointment.created&page=1&pageSize=20")
printf '%s' "$audit_response" | node -e '
let input = "";
process.stdin.on("data", chunk => input += chunk);
process.stdin.on("end", () => {
  const body = JSON.parse(input);
  if (body.totalCount !== 2 || body.items?.some(item => item.action !== "appointment.created")) {
    process.exit(1);
  }
});'

echo "Container health, Redis dependency, scheduling, appointment, reporting, customer history, account, membership, and audit smoke passed."
