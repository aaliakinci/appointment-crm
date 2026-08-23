# Contributing

Changes should preserve the established modular-monolith boundaries, tenant-isolation model, and v1 product scope. Construction notes and delivery reports are maintainer-local material and are not part of the published repository.

## Workflow

1. Start from an up-to-date `main` branch.
2. Create a short-lived branch such as `feat/customer-search`, `fix/tenant-filter`, or `docs/concurrency-adr`.
3. Keep commits focused. Preferred commit types are `feat`, `fix`, `test`, `docs`, `refactor`, `chore`, `ci`, and `build`.
4. Update tests, API contracts, migrations, and documentation together with the affected behavior.
5. Open a pull request and complete the repository checklist.

Direct pushes to `main` are not the intended workflow after branch protection is enabled.

## Dependency policy

Keep direct npm dependencies limited to the exact versions already approved in `src/web/package.json`: Lily UI, its React peer runtime, and the minimum compiler/build/quality/test toolchain. Application code must use Lily UI's public APIs for routing, state, HTTP, i18n, themes, and UI primitives; it must not import Lily's transitive packages as application-owned APIs.

Any proposed change to this rule requires explicit repository-owner approval and a rationale in the pull request.

NuGet and infrastructure dependencies must also be minimal, pinned through the appropriate lock/manifest mechanism, and justified by an application or test requirement.

## Security and tenancy

- Tenant identity comes from authenticated server context, never an untrusted request value.
- New tenant-scoped endpoints require cross-tenant integration tests.
- Do not log secrets or unnecessary personal data.
- Database migrations and seed data must be deterministic and safe to rerun where documented.

## Definition of done

A change is complete when its behavior, tests, migrations, API contracts, user-visible states, and operational impact have been updated in proportion to risk. The `Backend`, `Frontend`, `Host portability`, and `Containers` CI checks must pass before merge.
