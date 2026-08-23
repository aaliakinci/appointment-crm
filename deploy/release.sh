#!/usr/bin/env sh
set -eu

command_name=${1:-}
runtime_file=${2:-}
release_file=${3:-}
state_directory=${4:-}
backup_file=${5:-}

usage() {
  echo "Usage: release.sh <promote|rollback|reset-demo|backup|restore-rehearsal> <runtime.env> <release.env> <state-directory> [backup-file]" >&2
  exit 2
}

test -n "$command_name" && test -f "$runtime_file" && test -f "$release_file" \
  && test -n "$state_directory" || usage

deployment_root=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
release_compose="$deployment_root/compose.release.yaml"
operations_compose="$deployment_root/compose.operations.yaml"

env_value() {
  key=$1
  file=$2
  awk -F= -v key="$key" '$1 == key {sub(/^[^=]*=/, ""); print; exit}' "$file"
}

validate_digest_reference() {
  key=$1
  value=$(env_value "$key" "$release_file")
  printf '%s\n' "$value" | grep -Eq '^ghcr\.io/[a-z0-9._/-]+@sha256:[a-f0-9]{64}$' || {
    echo "$key must be an immutable GHCR digest reference." >&2
    exit 1
  }
}

validate_release() {
  validate_digest_reference APPOINTMENTCRM_API_IMAGE
  validate_digest_reference APPOINTMENTCRM_WEB_IMAGE
  validate_digest_reference APPOINTMENTCRM_OPS_IMAGE
}

compose_release() {
  docker compose \
    --env-file "$runtime_file" \
    --env-file "$release_file" \
    --file "$release_compose" \
    "$@"
}

compose_operations() {
  docker compose \
    --env-file "$runtime_file" \
    --env-file "$release_file" \
    --file "$operations_compose" \
    "$@"
}

smoke() {
  domain=$(env_value APPOINTMENTCRM_DOMAIN "$runtime_file")
  test -n "$domain" || {
    echo "APPOINTMENTCRM_DOMAIN is missing from runtime configuration." >&2
    exit 1
  }
  "$deployment_root/smoke.sh" "https://$domain"
}

promote() {
  validate_release
  mkdir -p "$state_directory"
  compose_release --profile release pull migrate api web caddy
  compose_release --profile release run --rm migrate
  compose_release up --detach --remove-orphans api web caddy
  smoke

  if [ -f "$state_directory/current.env" ]; then
    cp "$state_directory/current.env" "$state_directory/previous.env"
  fi
  cp "$release_file" "$state_directory/current.env"
}

rollback() {
  previous="$state_directory/previous.env"
  test -f "$previous" || {
    echo "No previous release manifest is available." >&2
    exit 1
  }
  compatible=$(env_value APPOINTMENTCRM_SCHEMA_COMPATIBLE_WITH_PREVIOUS "$release_file")
  test "$compatible" = "true" || {
    echo "Rollback is blocked because schema compatibility was not approved." >&2
    exit 1
  }

  current_release_file=$release_file
  release_file=$previous
  validate_release
  compose_release pull api web caddy
  compose_release up --detach --remove-orphans api web caddy
  smoke
  cp "$current_release_file" "$state_directory/failed.env"
  cp "$previous" "$state_directory/current.env"
}

reset_demo() {
  validate_release
  compose_release stop api
  trap 'compose_release up --detach api >/dev/null 2>&1 || true' EXIT HUP INT TERM
  compose_release --profile operations run --rm demo-reset
  compose_release up --detach api web caddy
  smoke
  trap - EXIT HUP INT TERM
}

backup() {
  validate_release
  compose_operations --profile operations run --rm backup
}

restore_rehearsal() {
  validate_release
  case "$backup_file" in
    ""|*/*|*..*) usage ;;
  esac
  APPOINTMENTCRM_BACKUP_FILE=$backup_file
  secret_directory=$(env_value APPOINTMENTCRM_SECRET_DIRECTORY "$runtime_file")
  test -n "$secret_directory" || {
    echo "APPOINTMENTCRM_SECRET_DIRECTORY is missing from runtime configuration." >&2
    exit 1
  }
  APPOINTMENTCRM_RESTORE_URL_FILE="$secret_directory/postgres-restore-url"
  test -s "$APPOINTMENTCRM_RESTORE_URL_FILE" || {
    echo "The isolated restore URL secret is missing." >&2
    exit 1
  }
  export APPOINTMENTCRM_BACKUP_FILE APPOINTMENTCRM_RESTORE_URL_FILE
  compose_operations --profile operations run --rm restore-rehearsal
}

case "$command_name" in
  promote) promote ;;
  rollback) rollback ;;
  reset-demo) reset_demo ;;
  backup) backup ;;
  restore-rehearsal) restore_rehearsal ;;
  *) usage ;;
esac
