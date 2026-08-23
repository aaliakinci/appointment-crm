#!/usr/bin/env sh
set -eu

umask 077

connection_file=${APPOINTMENTCRM_MAINTENANCE_URL_FILE:-/run/secrets/Postgres__MaintenanceUrl}
encryption_file=${APPOINTMENTCRM_BACKUP_PASSPHRASE_FILE:-/run/secrets/Backup__Passphrase}
backup_directory=${APPOINTMENTCRM_BACKUP_DIRECTORY:-/backups}

test -s "$connection_file" || {
  echo "PostgreSQL maintenance URL secret is missing." >&2
  exit 1
}
test -s "$encryption_file" || {
  echo "Backup encryption passphrase secret is missing." >&2
  exit 1
}
test -d "$backup_directory" || {
  echo "Backup directory does not exist: $backup_directory" >&2
  exit 1
}

connection=$(tr -d '\r\n' < "$connection_file")
timestamp=$(date -u +%Y%m%dT%H%M%SZ)
backup_id="appointment-crm-$timestamp"
plain_dump=$(mktemp)
trap 'rm -f "$plain_dump"' EXIT HUP INT TERM

pg_dump \
  --format=custom \
  --compress=9 \
  --no-owner \
  --no-acl \
  --dbname="$connection" \
  --file="$plain_dump"

encrypted_dump="$backup_directory/$backup_id.dump.enc"
openssl enc -aes-256-cbc -salt -pbkdf2 \
  -in "$plain_dump" \
  -out "$encrypted_dump" \
  -pass "file:$encryption_file"

checksum=$(sha256sum "$encrypted_dump" | awk '{print $1}')
migration=$(psql --dbname="$connection" --tuples-only --no-align --command \
  'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;')
counts=$(psql --dbname="$connection" --tuples-only --no-align --command \
  "SELECT json_build_object(
      'tenants', (SELECT count(*) FROM tenants),
      'memberships', (SELECT count(*) FROM tenant_memberships),
      'appointments', (SELECT count(*) FROM appointments),
      'auditEntries', (SELECT count(*) FROM audit_entries),
      'outboxMessages', (SELECT count(*) FROM outbox_messages));")

metadata="$backup_directory/$backup_id.json"
printf '%s\n' \
  "{" \
  "  \"backupId\": \"$backup_id\"," \
  "  \"createdAtUtc\": \"$timestamp\"," \
  "  \"sha256\": \"$checksum\"," \
  "  \"migration\": \"$migration\"," \
  "  \"rowCounts\": $counts" \
  "}" > "$metadata"

echo "$backup_id"
