# Local development

## Docker workflow

Requirements:

- Docker Engine with Docker Compose
- `curl`

Start the full stack:

```bash
./scripts/dev.sh
```

Open <http://localhost:5173>. The script waits until the API is ready.

Stop the containers without deleting PostgreSQL data:

```bash
./scripts/dev-down.sh
```

Copy `.env.example` to `.env` only when you need to change ports or local credentials. Never commit `.env`.

## Local accounts

The development stack seeds two tenants. All sample accounts use `Demo-local-2026!` unless `APPOINTMENTCRM_DEMO_PASSWORD` is changed.

- `owner@demo.local`
- `manager@demo.local`
- `receptionist@demo.local`
- `employee@demo.local`
- `north.owner@demo.local`

These accounts and this password are not production defaults.

## Migrations

Run pending migrations and the local seed:

```bash
./scripts/migrate.sh
```

The API does not apply migrations during normal startup. Compose runs a separate one-shot migration service first.

## Native development

Use:

- .NET SDK 10
- Node.js `22.23.1`
- npm 10
- local PostgreSQL and Redis, or the Compose services

The expected Node version is stored in `src/web/.nvmrc`.

Run the API:

```bash
dotnet run --project src/server/AppointmentCrm.Api
```

Run the web development server in a second terminal:

```bash
cd src/web
npm ci
npm run dev
```

## Tests

Run the complete local quality gate:

```bash
./scripts/verify.sh
```

Run smaller checks while developing:

```bash
dotnet test tests/AppointmentCrm.UnitTests/AppointmentCrm.UnitTests.csproj
dotnet test tests/AppointmentCrm.IntegrationTests/AppointmentCrm.IntegrationTests.csproj
cd src/web && npm test -- --run
```

Integration tests need PostgreSQL and Redis. The full verification script starts isolated test services automatically.

## Portfolio assets

Regenerate the English README screenshots and the extended English and Turkish demo recordings with disposable sample data:

```bash
./scripts/capture-portfolio.sh
```

This command uses an isolated Compose project and deletes its temporary database volume when it finishes. It overwrites the portfolio files under `docs/assets`.

## Debugging

The repository's `.vscode` configuration can attach the .NET debugger to the API process inside the Compose container. Start the stack first, open **Run and Debug**, and select the container attach configuration.

Useful checks:

```bash
docker compose ps
docker compose logs --follow api
curl --fail http://localhost:5173/health/ready
```

If login fails after changing seed settings, rerun the migration job or use a fresh local database volume. Deleting a volume removes local data, so do that only when a clean database is intended.
