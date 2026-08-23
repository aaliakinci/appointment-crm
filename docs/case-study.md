# Appointment CRM case study

## Problem

An appointment CRM looks simple until several people use it at the same time. The product must answer four difficult questions:

1. Can one business ever see another business's data?
2. Can two requests reserve the same employee and time?
3. Can local schedules be evaluated correctly around time-zone changes?
4. Can a failed operation leave appointment history, audit, or notifications out of sync?

The project was built as an internal multi-tenant CRM that treats these risks as core design concerns.

## Constraints

- One small team should be able to develop and operate it.
- PostgreSQL must remain the source of truth.
- Redis failure must not change business correctness.
- The UI should use Lily UI and stay organized by business feature.
- Release, migration, backup, and rollback work must be repeatable.
- Public booking, payments, and real notification providers are outside the first release.

## Chosen design

The backend is a modular monolith. This keeps transactions and deployment simple while preserving clear Domain, Application, Infrastructure, Contracts, and API boundaries.

Tenant isolation uses several independent controls: authenticated tenant context, permission policies, EF Core query filters, write guards, tenant-scoped keys, and two-tenant integration tests.

Appointment writes use a transaction-scoped PostgreSQL advisory lock for each tenant/employee pair. A GiST exclusion constraint remains the final rule against overlapping active appointments.

Appointment state, status history, audit data, and outbox messages are saved in one transaction. Notifications are delivered later from the durable outbox. Redis caches only data that can be rebuilt from PostgreSQL.

The frontend uses vertical feature folders. Route pages stay small, while each feature owns its API contracts, state hooks, validation, and UI components.

## Alternatives not chosen

| Alternative | Why it was not selected |
| --- | --- |
| Microservices | More deployment and consistency cost without a current team or scale need. |
| Database per tenant | Strong isolation, but too much migration and operating cost for this scope. |
| Controller-only tenant filters | Easy to forget and difficult to prove across every query. |
| Application-only slot check | Contains a race condition under concurrent requests. |
| Redis distributed lock | Makes correctness depend on an optional cache service. |
| Automatic schema rollback | A migration `Down` method cannot safely reverse every data change. |

## Measured results

| Check | Result |
| --- | --- |
| Same-slot concurrency | 8 parallel requests → 1 created, 7 conflicts |
| Transaction consistency | Exactly 1 appointment, history, audit, and outbox graph |
| Bounded read load | 20 concurrent list/report reads over at least 1,000 rows completed under the 15-second test gate |
| Backend tests | 172 unit and 64 integration tests |
| Frontend tests | 77 tests plus Chromium E2E/accessibility gates |
| Build quality | Release build with 0 warnings and 0 errors |
| Runtime image scan | 0 known high/critical findings at the recorded scan |
| Restore rehearsal | Encrypted backup restored to a separate database with matching migration and row counts |

These are repeatable test gates, not production traffic benchmarks.

## Outcome

The result is a release-ready repository with a clear product boundary and visible evidence for its highest-risk rules. A developer can run the system with one command, and a reviewer can understand the tenant and concurrency model without reading every implementation file.

The next operating step is to provide the external environment: domain, TLS issuance, hosted PostgreSQL, off-host backup storage, telemetry destination, and a public demo URL.
