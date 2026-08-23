#!/usr/bin/env sh
set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repository_root"

portfolio_project=appointment-crm-portfolio
portfolio_postgres_port=${APPOINTMENTCRM_PORTFOLIO_POSTGRES_PORT:-55437}
portfolio_redis_port=${APPOINTMENTCRM_PORTFOLIO_REDIS_PORT:-56383}
portfolio_api_port=${APPOINTMENTCRM_PORTFOLIO_API_PORT:-58083}
portfolio_web_port=${APPOINTMENTCRM_PORTFOLIO_WEB_PORT:-55176}
portfolio_password=${APPOINTMENTCRM_PORTFOLIO_PASSWORD:-Portfolio-local-2026!}

cleanup_portfolio() {
  APPOINTMENTCRM_POSTGRES_PORT="$portfolio_postgres_port" \
    APPOINTMENTCRM_REDIS_PORT="$portfolio_redis_port" \
    APPOINTMENTCRM_API_PORT="$portfolio_api_port" \
    APPOINTMENTCRM_WEB_PORT="$portfolio_web_port" \
    docker compose --project-name "$portfolio_project" down --volumes >/dev/null
}
trap cleanup_portfolio EXIT

APPOINTMENTCRM_POSTGRES_PORT="$portfolio_postgres_port" \
  APPOINTMENTCRM_REDIS_PORT="$portfolio_redis_port" \
  APPOINTMENTCRM_API_PORT="$portfolio_api_port" \
  APPOINTMENTCRM_WEB_PORT="$portfolio_web_port" \
  APPOINTMENTCRM_DEMO_PASSWORD="$portfolio_password" \
  docker compose --project-name "$portfolio_project" up --build --detach

attempt=0
until curl --fail --silent --show-error "http://127.0.0.1:$portfolio_web_port/health/ready" >/dev/null; do
  attempt=$((attempt + 1))
  if [ "$attempt" -ge 120 ]; then
    docker compose --project-name "$portfolio_project" logs api migrate web
    echo "Portfolio environment did not become ready." >&2
    exit 1
  fi
  sleep 1
done

cd src/web
APPOINTMENTCRM_PORTFOLIO_BASE_URL="http://localhost:$portfolio_web_port" \
  APPOINTMENTCRM_PORTFOLIO_PASSWORD="$portfolio_password" \
  APPOINTMENTCRM_PORTFOLIO_LANGUAGE=tr \
  APPOINTMENTCRM_PORTFOLIO_SEED=true \
  npm run portfolio:capture

APPOINTMENTCRM_PORTFOLIO_BASE_URL="http://localhost:$portfolio_web_port" \
  APPOINTMENTCRM_PORTFOLIO_PASSWORD="$portfolio_password" \
  APPOINTMENTCRM_PORTFOLIO_LANGUAGE=en \
  APPOINTMENTCRM_PORTFOLIO_SEED=false \
  npm run portfolio:capture
