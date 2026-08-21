#!/usr/bin/env sh
set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repository_root"

docker compose up --build --detach

attempt=0
until curl --fail --silent --show-error "http://localhost:${APPOINTMENTCRM_WEB_PORT:-5173}/health/ready" >/dev/null; do
  attempt=$((attempt + 1))
  if [ "$attempt" -ge 60 ]; then
    docker compose ps
    echo "Appointment CRM did not become ready within 60 seconds." >&2
    exit 1
  fi
  sleep 1
done

echo "Appointment CRM is ready at http://localhost:${APPOINTMENTCRM_WEB_PORT:-5173}"
