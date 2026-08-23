#!/usr/bin/env sh
set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repository_root"

APPOINTMENTCRM_BUILD_CONFIGURATION=${APPOINTMENTCRM_BUILD_CONFIGURATION:-Debug}
export APPOINTMENTCRM_BUILD_CONFIGURATION

docker compose up --build --detach --wait --wait-timeout 120

echo "Appointment CRM is ready at http://localhost:${APPOINTMENTCRM_WEB_PORT:-5173}"
