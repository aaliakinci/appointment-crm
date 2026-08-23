# Backup and restore

The production database requires provider-native automated backups plus an encrypted logical backup. The minimum demo policy is daily logical backup, seven daily copies, four weekly copies, and a monthly restore rehearsal. The target objectives are RPO 24 hours and RTO 60 minutes until measured production evidence supports tighter values.

The scheduled `Production operations` workflow runs backup before demo reset. `deploy/ops/backup.sh` uses PostgreSQL 18 tools, writes an AES-256 encrypted custom-format dump, records SHA-256, current EF migration, UTC timestamp, and bounded row counts, and prints no connection secret. The host must copy the encrypted dump and metadata off-host; a local Docker volume is not a backup destination by itself.

Manual backup against the current release:

```bash
  /opt/appointment-crm/environments/production/current/deploy/release.sh backup \
  /opt/appointment-crm/shared/production/runtime.env \
  /opt/appointment-crm/environments/production/state/current.env \
  /opt/appointment-crm/environments/production/state
```

Record the emitted backup ID, object-store version/checksum, source database, source migration, size, and operator.

## Restore rehearsal

Provision a separate empty database whose name contains `_restore_` or ends in `_rehearsal`; the restore tool refuses any other target name. Put its libpq URI in `postgres-restore-url`, select a backup basename from the configured backup directory, and run:

```bash
  /opt/appointment-crm/environments/production/current/deploy/release.sh restore-rehearsal \
  /opt/appointment-crm/shared/production/runtime.env \
  /opt/appointment-crm/environments/production/state/current.env \
  /opt/appointment-crm/environments/production/state \
  appointment-crm-20260823T120000Z.dump.enc
```

The command decrypts only to a guarded container temporary file, restores with `--exit-on-error`, and reports the restored database name, migration, duration, and row counts. Compare those counts with the backup metadata, run the current migration job against the restored database, and execute read-only authenticated smoke. Never point `postgres-restore-url` at staging or production.

Destroy the rehearsal database only after its evidence is stored. Update the measured RPO/RTO, backup-age alert, and release checklist. A successful EF migration test is not a substitute for this operational restore.
