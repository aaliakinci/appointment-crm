# Appointment CRM

## Technology baseline

- .NET 10 LTS / ASP.NET Core / EF Core
- PostgreSQL 18
- React 18 and TypeScript 5.7 baseline required by `@lily_platform/lily_ui@0.1.0-alpha.2`
- Redis for non-authoritative, replaceable cache data only
- Docker Compose and GitHub Actions
- Caddy TLS ingress and immutable-digest staging/production promotion

## Current capabilities

- Tenant-aware login, explicit tenant selection, tenant switching, logout, and account-wide session revocation
- Short-lived protected access tokens plus single-use rotating refresh tokens held in a strict `HttpOnly` cookie
- Server-side session validation, refresh-token reuse detection, and immediate invalidation after user or role changes
- Role and permission policies for owner, manager, receptionist, and employee access
- Tenant query filters, write guards, and database constraints that prevent cross-tenant membership access
- Tenant-scoped customer, service, and employee management through ASP.NET Core controllers and Lily UI list/form/detail screens
- Bounded server-side pagination, search, filtering, sorting, customer contact normalization, service pricing/duration validation, and employee-service assignments
- Versioned tenant/employee working schedules, date overrides, time off, IANA time-zone handling, and availability calculation
- Appointment lifecycle with service snapshots, optimistic concurrency, PostgreSQL-enforced overlap protection, day/week operations, customer history, and dashboard reporting
- Durable outbox processing with bounded retry, an idempotent demo notification provider, tenant-scoped Redis dashboard caching with PostgreSQL fallback, and OpenTelemetry instrumentation
- Same-tenant account-to-employee linking and actor-attributed audit records for critical create, archive, and activation changes

## Local development

Docker Engine with Compose support and `curl` are the only prerequisites for the full local stack. From a clean clone, run:

```bash
./scripts/dev.sh
```

The command builds the images, starts PostgreSQL 18, Redis, the migration job, API, and web reverse proxy, then waits for readiness. Open <http://localhost:5173>. Useful local endpoints are:

- Web and same-origin API proxy: <http://localhost:5173>
- Readiness: <http://localhost:5173/health/ready>
- OpenAPI document: <http://localhost:5173/openapi/v1.json>
- Direct API port: <http://localhost:8080>

Stop the stack without deleting its database volume:

```bash
./scripts/dev-down.sh
```

The checked-in defaults are for an isolated developer machine only. Copy `.env.example` to `.env` before changing ports or credentials; never commit `.env`. PostgreSQL migrations run as a one-shot service before the API starts and can be rerun with `./scripts/migrate.sh`.

The local stack seeds two sample tenants and the following accounts. Their development-only password comes from `APPOINTMENTCRM_DEMO_PASSWORD` and defaults to `Demo-local-2026!`:

- `owner@demo.local` — owner in both sample tenants
- `manager@demo.local` — manager in Atlas Salon
- `receptionist@demo.local` — receptionist in Atlas Salon
- `employee@demo.local` — employee in Atlas Salon
- `north.owner@demo.local` — owner in Northwind Consulting

Keep demo seeding disabled outside disposable development environments and replace every sample credential before shared use. Production deployments must serve HTTPS, keep `APPOINTMENTCRM_SECURE_COOKIE=true`, and configure a persistent, externally protected ASP.NET Data Protection key ring. Development and test instances intentionally use ephemeral keys.

For native development, use a compatible .NET 10 SDK and Node.js `22.23.1` from `src/web/.nvmrc`.

## Quality checks

With .NET 10, Node.js 22, npm, Docker, and Compose available, run the local quality gate:

```bash
./scripts/verify.sh
```

The gate verifies locked restores, formatting, zero-warning builds, backend unit/integration tests against PostgreSQL, frontend lint/type checks/tests/build, and the committed lockfiles. GitHub Actions additionally executes an isolated Chromium E2E/accessibility suite and scans the runtime images. Authentication, session rotation, authorization, tenant isolation, fresh/upgrade migrations, appointment concurrency, immutable image promotion, and public HTTPS smoke are release-blocking.

Release operations are recorded in the [deployment runbook](docs/operations/deployment.md), [database release operations](docs/operations/database-release.md), [backup/restore procedure](docs/operations/backup-restore.md), and [public demo operations](docs/operations/public-demo.md). Current product boundaries remain in [known limitations](KNOWN_LIMITATIONS.md).

## License

Copyright © 2026 Ali Akıncı. This repository is licensed under the [MIT License](LICENSE).
