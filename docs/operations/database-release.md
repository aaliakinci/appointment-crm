# Database release operations

Appointment CRM uses forward-only EF Core migrations. A release runs migration once as a dedicated job before the new API image receives traffic. API replicas never migrate the database during normal startup.

## Pre-release controls

1. Record the immutable API and web image digests and the target migration ID.
2. Stop if CI has not passed both the empty-database and previous-release upgrade tests.
3. Confirm the migration is compatible with the currently running API image. Prefer additive columns, tables, and indexes; split destructive changes across releases.
4. Create a PostgreSQL backup and verify that its object-store checksum, encryption, retention, and restore credentials are available. Use the repository's guarded procedure in [backup and restore](backup-restore.md).
5. Run the migration job exactly once with the same API artifact that will be deployed:

   ```bash
   dotnet AppointmentCrm.Api.dll --migrate
   ```

6. Verify migration completion before starting or promoting API replicas. Then run readiness and product smoke checks through the web reverse proxy.

The repository's local Compose `migrate` service demonstrates this ordering. Production uses the one-shot service in `deploy/compose.release.yaml`; only that job may enable public demo seeding. Long-running API replicas keep `DemoSeed__Enabled=false`.

## Backup and restore rehearsal

A backup is useful only after a restore has been proven. Restore to a new database; never overwrite the only production copy during a rehearsal.

```bash
pg_dump --format=custom --no-owner --no-acl --dbname="$SOURCE_DATABASE_URL" --file=appointment-crm.dump
createdb --template=template0 appointment_crm_restore_rehearsal
pg_restore --exit-on-error --clean --if-exists --no-owner --no-acl \
  --dbname="$RESTORE_DATABASE_URL" appointment-crm.dump
```

After restore, run migration status, tenant-count consistency checks, readiness, and a read-only authenticated smoke. Record the backup identifier, source migration, restore duration, row-count checks, and operator. Destroy only the isolated rehearsal database after evidence is retained.

The automated `MigrationQualificationTests` complement, but do not replace, the operational backup rehearsal. They create guarded temporary databases, prove empty-to-latest and previous-release-to-latest migration paths, and verify that pre-existing outbox data survives the current upgrade.

## Failure and rollback strategy

Production schema rollback with `dotnet ef database update <old migration>` is prohibited. A migration can include lossy data transformations or DDL that an automatic `Down` method cannot safely reverse.

| Failure point | Recovery |
| --- | --- |
| Migration job fails before completion | Keep the old application serving traffic when safe, stop the release, diagnose, and ship a corrective forward migration. |
| New application fails after a backward-compatible migration | Route traffic back to the recorded previous application image; leave the additive schema in place; then issue a fixed application or forward migration. |
| New schema is incompatible with the previous application | Enter maintenance mode and prefer a corrective forward migration. If recovery objectives require restore, restore the verified backup into a new database and switch only after integrity and smoke checks pass. |
| Data corruption is suspected | Stop writes, preserve forensic copies, restore into a new database, validate tenant and appointment invariants, and switch through the approved incident process. |

An application-image rollback is allowed only when the release review explicitly proves the old image remains compatible with the migrated schema. Every destructive change must use an expand/migrate/contract sequence across releases.

## Required evidence

- CI run containing unit, integration, migration, concurrency, E2E, accessibility, dependency, and image scan gates
- source commit, release version, image digests, and migration IDs
- backup identifier and last successful restore rehearsal
- migration job logs and duration
- post-release readiness and authenticated smoke results
- rollback compatibility decision and named operator
