#!/usr/bin/env sh
set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repository_root"

e2e_project=appointment-crm-browser-e2e
e2e_postgres_port=${APPOINTMENTCRM_E2E_POSTGRES_PORT:-55436}
e2e_redis_port=${APPOINTMENTCRM_E2E_REDIS_PORT:-56382}
e2e_api_port=${APPOINTMENTCRM_E2E_API_PORT:-58082}
e2e_web_port=${APPOINTMENTCRM_E2E_WEB_PORT:-55175}
e2e_password=${APPOINTMENTCRM_E2E_PASSWORD:-Browser-local-2026!}

cleanup_e2e() {
  APPOINTMENTCRM_POSTGRES_PORT="$e2e_postgres_port" \
    APPOINTMENTCRM_REDIS_PORT="$e2e_redis_port" \
    APPOINTMENTCRM_API_PORT="$e2e_api_port" \
    APPOINTMENTCRM_WEB_PORT="$e2e_web_port" \
    docker compose --project-name "$e2e_project" down --volumes >/dev/null
}
trap cleanup_e2e EXIT

APPOINTMENTCRM_POSTGRES_PORT="$e2e_postgres_port" \
  APPOINTMENTCRM_REDIS_PORT="$e2e_redis_port" \
  APPOINTMENTCRM_API_PORT="$e2e_api_port" \
  APPOINTMENTCRM_WEB_PORT="$e2e_web_port" \
  APPOINTMENTCRM_DEMO_PASSWORD="$e2e_password" \
  docker compose --project-name "$e2e_project" up --build --detach

attempt=0
until curl --fail --silent --show-error "http://127.0.0.1:$e2e_web_port/health/ready" >/dev/null; do
  attempt=$((attempt + 1))
  if [ "$attempt" -ge 120 ]; then
    docker compose --project-name "$e2e_project" logs api migrate web
    echo "Browser E2E environment did not become ready." >&2
    exit 1
  fi
  sleep 1
done

cd src/web
APPOINTMENTCRM_E2E_BASE_URL="http://127.0.0.1:$e2e_web_port" \
  APPOINTMENTCRM_E2E_PASSWORD="$e2e_password" \
  npm run e2e
