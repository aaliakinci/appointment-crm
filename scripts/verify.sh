#!/usr/bin/env sh
set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repository_root"

node_major=$(node --version | sed 's/^v//' | cut -d. -f1)
if [ "$node_major" -ne 22 ]; then
  echo "Node.js 22 is required; found $(node --version)." >&2
  exit 1
fi

dotnet restore AppointmentCrm.slnx --locked-mode --disable-parallel --verbosity minimal -m:1
dotnet format AppointmentCrm.slnx --verify-no-changes --no-restore --verbosity minimal
dotnet build AppointmentCrm.slnx --configuration Release --no-restore --verbosity minimal -m:1
dotnet test tests/AppointmentCrm.UnitTests/AppointmentCrm.UnitTests.csproj --configuration Release --no-build --no-restore --verbosity minimal -m:1

verification_project=appointment-crm-verify
verification_postgres_port=${APPOINTMENTCRM_VERIFY_POSTGRES_PORT:-55433}
verification_postgres_password=verify-local-only

cleanup_verification_database() {
  (
    cd "$repository_root"
    APPOINTMENTCRM_POSTGRES_DB=appointment_crm \
      APPOINTMENTCRM_POSTGRES_USER=appointment_crm \
      APPOINTMENTCRM_POSTGRES_PASSWORD="$verification_postgres_password" \
      APPOINTMENTCRM_POSTGRES_PORT="$verification_postgres_port" \
      docker compose --project-name "$verification_project" down --volumes >/dev/null
  )
}
trap cleanup_verification_database EXIT

APPOINTMENTCRM_POSTGRES_DB=appointment_crm \
  APPOINTMENTCRM_POSTGRES_USER=appointment_crm \
  APPOINTMENTCRM_POSTGRES_PASSWORD="$verification_postgres_password" \
  APPOINTMENTCRM_POSTGRES_PORT="$verification_postgres_port" \
  docker compose --project-name "$verification_project" up --detach --wait postgres
export APPOINTMENTCRM_TEST_POSTGRES="Host=127.0.0.1;Port=$verification_postgres_port;Database=appointment_crm;Username=appointment_crm;Password=$verification_postgres_password"
dotnet test tests/AppointmentCrm.IntegrationTests/AppointmentCrm.IntegrationTests.csproj --configuration Release --no-build --no-restore --verbosity minimal -m:1

cd src/web
npm ci --no-audit --no-fund
npm run format:check
npm run lint
npm run typecheck
npm test -- --run
npm run build

cd "$repository_root"
docker compose config --quiet
scripts/container-smoke.sh
