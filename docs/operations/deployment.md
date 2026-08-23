# Deployment operations

Appointment CRM ships a provider-neutral, single-host demo deployment built from immutable OCI image digests. Staging and production use separate hosts, databases, Redis instances, Docker volumes, secret directories, DNS names, and GitHub Environments. The local `compose.yaml` is not used for either environment.

## Runtime topology

```text
Internet -- HTTPS --> Caddy --> nginx/web --> ASP.NET Core API --> PostgreSQL
                                             |
                                             +--> Redis (optional cache)
                                             +--> OTLP collector (optional telemetry)
```

Only Caddy publishes host ports. PostgreSQL, Redis, nginx, and the API are not exposed publicly. Caddy obtains and renews TLS certificates after the domain's A/AAAA record points at the host and inbound TCP 80/443 plus UDP 443 are allowed. Nginx has a fixed private address so ASP.NET Core can trust exactly that forwarding proxy.

The deployment is intended for one small portfolio-demo host per environment. A multi-host deployment must replace the local Data Protection volume with shared durable key storage and replace Compose orchestration with a platform-native release job.

## Host prerequisites

- A dedicated staging or production Linux host with Docker Engine and Compose v2
- A dedicated non-root deploy account with UID/GID `1654`, allowed to run Docker; this matches the API and operations containers
- DNS and firewall access for ports 80 and 443
- Externally managed PostgreSQL and Redis endpoints using TLS
- An OTLP endpoint when telemetry export is required
- An off-host encrypted destination for files produced in the backup directory
- A host-side GHCR login with read-only package scope when the published images are private

Use `deploy/environments/staging.env.example` or `production.env.example` to create `/opt/appointment-crm/shared/<environment>/runtime.env`. Actual `.env` files and secrets are never copied from the repository.

Create `/opt/appointment-crm/shared/<environment>/secrets` and `backups` owned by UID/GID `1654` with mode `0700`; every secret file must use mode `0600`. This keeps secrets readable by the non-root job containers without making them world-readable:

| File                       | Content                                                                                                         |
| -------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `postgres-connection`      | Npgsql connection string for the application/migration role                                                     |
| `redis-connection`         | Redis connection string with TLS/authentication settings                                                        |
| `demo-password`            | Unique 12+ character password for `receptionist@demo.local`                                                     |
| `data-protection-password` | High-entropy password for the key-encryption certificate                                                        |
| `data-protection.pfx`      | Password-protected certificate containing its private key                                                       |
| `postgres-maintenance-url` | libpq URI used by `pg_dump`                                                                                     |
| `postgres-restore-url`     | Temporary libpq URI for an isolated rehearsal database only; create only for the rehearsal and remove afterward |
| `backup-passphrase`        | High-entropy AES backup encryption passphrase                                                                   |

Generate the Data Protection certificate once, back it up in the approved secret store, and keep the same key volume/certificate across application restarts:

```bash
deploy/generate-data-protection-certificate.sh \
  /opt/appointment-crm/shared/production/secrets \
  /opt/appointment-crm/shared/production/secrets/data-protection-password
```

The application refuses to start outside Development/Testing when ephemeral keys are enabled or the persistent path, certificate, or certificate password is absent.

## GitHub deployment controls

Create `staging`, `production`, `production-operations`, and `production-monitoring` GitHub Environments. Configure these environment secrets:

- `DEPLOY_HOST`
- `DEPLOY_USER`
- `DEPLOY_SSH_PRIVATE_KEY`
- `DEPLOY_SSH_KNOWN_HOSTS` (pinned host key; do not use runtime `ssh-keyscan`)

Set `DEPLOY_ROOT` to `/opt/appointment-crm`. Define `DEMO_BASE_URL` as a repository-level Actions variable because the uptime job uses it before entering the `production-monitoring` Environment. Keep that Environment for monitoring ownership and access control. Configure at least one required reviewer on `production`; do not put a reviewer gate on scheduled `production-operations`.

The release workflow builds API, web, and database-operations images once for `linux/amd64` and once for `linux/arm64`. It uploads commit-scoped architecture images, scans both architectures, and publishes the final multi-architecture manifest only after every scan passes. The workflow records the manifest digests, deploys them to staging, runs HTTPS smoke, pauses at the protected production Environment, and then deploys the identical digests. No image is rebuilt during promotion.

For a tag release, a successful production promotion also creates or updates the GitHub Release. It publishes the prepared notes and attaches a JSON manifest with the source commit, database migration, and exact API/web/operations image digests. A manual workflow run deploys a version but does not create a GitHub Release.

## Release and migration sequence

1. Confirm the quality workflow passed for the release commit.
2. Trigger `Release and deploy` with a SemVer value, or push a repository-owner-created SemVer tag.
3. The staging job transfers only the deployment bundle and immutable digest manifest.
4. `deploy/release.sh promote` pulls artifacts and runs the API image once with `--migrate` before starting new API replicas.
5. The script verifies liveness, readiness, and the anonymous system endpoint through public HTTPS.
6. Review staging, approve the protected production job, and repeat with the same digest manifest.

Normal API startup never calls `Database.Migrate`. A failed migration stops promotion before the new API image starts.

## Rollback

The host retains current and previous digest manifests. Roll back only when the release manifest explicitly records `APPOINTMENTCRM_SCHEMA_COMPATIBLE_WITH_PREVIOUS=true`:

```bash
/opt/appointment-crm/environments/production/current/deploy/release.sh rollback \
  /opt/appointment-crm/shared/production/runtime.env \
  /opt/appointment-crm/environments/production/state/current.env \
  /opt/appointment-crm/environments/production/state
```

Rollback changes application images; it never executes migration `Down`. If schema compatibility is not approved, stop and follow the corrective forward-migration or verified-restore decision in `database-release.md`.

## Monitoring

The scheduled uptime workflow probes `/health/live` and `/health/ready` from outside the deployment network every ten minutes and fails when either endpoint is unavailable or exceeds two seconds. GitHub Actions failure notifications are the minimum alert channel; production operation ownership must route them to a monitored mailbox or incident channel.

Runtime alerts should additionally cover five-minute API 5xx ratio above 2%, p95 request latency above one second for fifteen minutes, PostgreSQL connection failure, outbox terminal failures above zero, backup age above 26 hours, and restore rehearsal age above 30 days. Redis degradation affects performance, not readiness or correctness.
