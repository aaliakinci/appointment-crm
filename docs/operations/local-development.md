# Local development

## Docker workflow

Requirements:

- Docker Desktop or Docker Engine with Compose v2

Start the full stack:

```text
docker compose up --build --detach --wait --wait-timeout 120
```

Open <http://localhost:5173>. Compose waits until the API and its dependencies are ready.

Stop the containers without deleting PostgreSQL data:

```text
docker compose down
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

## Supported development hosts

| Host                                               | Local Compose | Native .NET/Node | Full verification         |
| -------------------------------------------------- | ------------- | ---------------- | ------------------------- |
| Linux                                              | Supported     | Supported        | Supported                 |
| macOS with Docker Desktop                          | Supported     | Supported        | `node scripts/verify.mjs` |
| Windows with Docker Desktop using Linux containers | Supported     | Supported        | `node scripts/verify.mjs` |

WSL2 or Git Bash can still run the legacy `.sh` wrappers, but they are not required. The canonical Compose and Node commands above work from Bash, macOS Terminal, PowerShell, and compatible Windows terminals. Production deployment remains Linux-only because its host permissions, paths, and release controls are deliberately designed for Linux.

## Migrations

Run pending migrations and the local seed:

```text
docker compose run --rm migrate
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

```text
dotnet run --project src/server/AppointmentCrm.Api
```

Run the web development server in a second terminal:

```text
cd src/web
npm ci
npm run dev
```

## Tests

Run the complete local quality gate:

```text
node scripts/verify.mjs
```

Run smaller checks while developing:

```text
dotnet test tests/AppointmentCrm.UnitTests/AppointmentCrm.UnitTests.csproj
dotnet test tests/AppointmentCrm.IntegrationTests/AppointmentCrm.IntegrationTests.csproj
cd src/web && npm test -- --run
```

Run the isolated browser journey:

```text
node scripts/browser-e2e.mjs
```

Integration tests need PostgreSQL and Redis. The full verification script starts isolated test services automatically.

## Portfolio assets

Regenerate the English README screenshots and the extended English and Turkish demo recordings with disposable sample data:

```text
node scripts/capture-portfolio.mjs
```

This command uses an isolated Compose project and deletes its temporary database volume when it finishes. It overwrites the portfolio files under `docs/assets`.

## Debugging

The repository's `.vscode` configuration can attach the .NET debugger to the API process inside the Compose container. Start the stack first, open **Run and Debug**, and select the container attach configuration.

Useful checks:

```text
docker compose ps
docker compose logs --follow api
```

Open <http://localhost:5173/health/ready> to inspect readiness without requiring a host-side `curl` installation.

If login fails after changing seed settings, rerun the migration job or use a fresh local database volume. Deleting a volume removes local data, so do that only when a clean database is intended.
