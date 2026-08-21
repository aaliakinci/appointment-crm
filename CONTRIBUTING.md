# Contributing

The repository is currently in bootstrap status. Contributions should preserve the architectural decisions in `docs/decisions` and the scope boundary in `ROADMAP.md`.

## Workflow

1. Start from an up-to-date `main` branch.
2. Create a short-lived branch such as `feat/customer-search`, `fix/tenant-filter`, or `docs/concurrency-adr`.
3. Keep commits focused. Preferred commit types are `feat`, `fix`, `test`, `docs`, `refactor`, `chore`, `ci`, and `build`.
4. Update tests, API contracts, migrations, and documentation together with the affected behavior.
5. Open a pull request and complete the repository checklist.

Direct pushes to `main` are not the intended workflow after branch protection is enabled.

## Dependency policy

Do not add or install any direct npm package other than the exact approved `@lily_platform/lily_ui` version. Transitive packages are owned by Lily UI's lockfile resolution and must not be imported as application-owned APIs unless Lily UI explicitly re-exports them.

Any proposed change to this rule requires an ADR and explicit repository-owner approval. See `docs/dependency-policy.md`.

NuGet and infrastructure dependencies must also be minimal, pinned through the appropriate lock/manifest mechanism, and justified by an application or test requirement.

## Security and tenancy

- Tenant identity comes from authenticated server context, never an untrusted request value.
- New tenant-scoped endpoints require cross-tenant integration tests.
- Do not log secrets or unnecessary personal data.
- Database migrations and seed data must be deterministic and safe to rerun where documented.

## Definition of done

The shared feature-level definition of done is maintained in `ROADMAP.md`. CI checks will become mandatory in Phase 1.
