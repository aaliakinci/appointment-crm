#!/usr/bin/env sh
set -eu

umask 077

target_file=${APPOINTMENTCRM_RESTORE_URL_FILE:-/run/secrets/Postgres__RestoreUrl}
encryption_file=${APPOINTMENTCRM_BACKUP_PASSPHRASE_FILE:-/run/secrets/Backup__Passphrase}
encrypted_dump=${APPOINTMENTCRM_BACKUP_FILE:-}

test -s "$target_file" || {
  echo "Restore target URL secret is missing." >&2
  exit 1
}
test -s "$encryption_file" || {
  echo "Backup encryption passphrase secret is missing." >&2
  exit 1
}
test -n "$encrypted_dump" && test -f "$encrypted_dump" || {
  echo "APPOINTMENTCRM_BACKUP_FILE must identify an encrypted backup." >&2
  exit 1
}
metadata=${encrypted_dump%.dump.enc}.json
test -f "$metadata" || {
  echo "Backup metadata is missing: $metadata" >&2
  exit 1
}
expected_checksum=$(sed -n 's/^  "sha256": "\([a-f0-9]*\)",$/\1/p' "$metadata")
actual_checksum=$(sha256sum "$encrypted_dump" | awk '{print $1}')
test -n "$expected_checksum" && test "$actual_checksum" = "$expected_checksum" || {
  echo "Backup checksum does not match its metadata." >&2
  exit 1
}

target=$(tr -d '\r\n' < "$target_file")
target_database=$(psql --dbname="$target" --tuples-only --no-align --command \
  'SELECT current_database();')
case "$target_database" in
  *_restore_*|*_rehearsal) ;;
  *)
    echo "Refusing restore: target database must contain _restore_ or end in _rehearsal." >&2
    exit 1
    ;;
esac

plain_dump=$(mktemp)
trap 'rm -f "$plain_dump"' EXIT HUP INT TERM
openssl enc -d -aes-256-cbc -pbkdf2 \
  -in "$encrypted_dump" \
  -out "$plain_dump" \
  -pass "file:$encryption_file"

started=$(date +%s)
pg_restore \
  --exit-on-error \
  --clean \
  --if-exists \
  --no-owner \
  --no-acl \
  --dbname="$target" \
  "$plain_dump"
duration_seconds=$(($(date +%s) - started))

migration=$(psql --dbname="$target" --tuples-only --no-align --command \
  'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;')
counts=$(psql --dbname="$target" --tuples-only --no-align --command \
  "SELECT json_build_object(
      'tenants', (SELECT count(*) FROM tenants),
      'memberships', (SELECT count(*) FROM tenant_memberships),
      'appointments', (SELECT count(*) FROM appointments),
      'auditEntries', (SELECT count(*) FROM audit_entries),
      'outboxMessages', (SELECT count(*) FROM outbox_messages));")
expected_migration=$(sed -n 's/^  "migration": "\(.*\)",$/\1/p' "$metadata")
expected_counts=$(sed -n 's/^  "rowCounts": \(.*\)$/\1/p' "$metadata")
test "$migration" = "$expected_migration" || {
  echo "Restored migration does not match backup metadata." >&2
  exit 1
}
test "$counts" = "$expected_counts" || {
  echo "Restored row counts do not match backup metadata." >&2
  exit 1
}

printf '%s\n' \
  "restoreDatabase=$target_database" \
  "migration=$migration" \
  "durationSeconds=$duration_seconds" \
  "rowCounts=$counts"
