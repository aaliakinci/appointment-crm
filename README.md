# Appointment CRM

## Technology baseline

- .NET 10 LTS / ASP.NET Core / EF Core
- PostgreSQL 18
- React 18 and TypeScript 5.7 baseline required by `@lily_platform/lily_ui@0.1.0-alpha.2`
- Redis for non-authoritative, replaceable cache data only
- Docker Compose and GitHub Actions


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

For native development, use a compatible .NET 10 SDK and Node.js `22.23.1` from `src/web/.nvmrc`.

## Quality checks

With .NET 10, Node.js 22, npm, Docker, and Compose available, run the local quality gate:

```bash
./scripts/verify.sh
```

The gate verifies locked restores, formatting, zero-warning builds, backend unit/integration tests against PostgreSQL, frontend lint/type checks/tests/build, and the committed lockfiles. GitHub Actions executes the same checks and builds both runtime images. Tenant-isolation and appointment-concurrency suites will become release-blocking as those capabilities arrive.

## License

Copyright © 2026 Ali Akıncı. This repository is licensed under the [MIT License](LICENSE).
