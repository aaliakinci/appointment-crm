#!/usr/bin/env sh
set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
cd "$repository_root"

temporary_directory=$(mktemp -d)
trap 'rm -rf "$temporary_directory"' EXIT HUP INT TERM

for secret in \
  postgres-connection \
  redis-connection \
  demo-password \
  data-protection-password \
  data-protection.pfx \
  postgres-maintenance-url \
  postgres-restore-url \
  backup-passphrase; do
  touch "$temporary_directory/$secret"
done

api_image=ghcr.io/aaliakinci/appointment_crm/api@sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa
web_image=ghcr.io/aaliakinci/appointment_crm/web@sha256:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb
ops_image=ghcr.io/aaliakinci/appointment_crm/ops@sha256:cccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccccc

APPOINTMENTCRM_SECRET_DIRECTORY="$temporary_directory" \
APPOINTMENTCRM_API_IMAGE="$api_image" \
APPOINTMENTCRM_WEB_IMAGE="$web_image" \
docker compose \
  --env-file deploy/environments/staging.env.example \
  --file deploy/compose.release.yaml \
  config --quiet

APPOINTMENTCRM_SECRET_DIRECTORY="$temporary_directory" \
APPOINTMENTCRM_OPS_IMAGE="$ops_image" \
APPOINTMENTCRM_BACKUP_FILE=fixture.dump.enc \
docker compose \
  --env-file deploy/environments/staging.env.example \
  --file deploy/compose.operations.yaml \
  config --quiet

sh -n \
  deploy/generate-data-protection-certificate.sh \
  deploy/ops/backup.sh \
  deploy/ops/restore-rehearsal.sh \
  deploy/release.sh \
  deploy/smoke.sh \
  scripts/publish-release.sh \
  scripts/uptime-check.sh
