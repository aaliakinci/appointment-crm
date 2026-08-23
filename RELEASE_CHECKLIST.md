# Release checklist

Use this checklist for `1.0.0-rc.1` and later candidates. A checked item represents collected evidence, not an assumption.

## Source and artifacts

- [ ] Working tree contains only reviewed release changes; no secrets, local reports, or generated test output are tracked.
- [ ] Backend and frontend versions identify the intended release candidate.
- [ ] Dependency lockfiles are current and CI restores them without mutation.
- [ ] API and web images are built once and their immutable digests are recorded.
- [ ] Release notes describe user-visible changes, database changes, and known limitations.

## Automated quality gates

- [ ] Locked .NET restore, formatting, warning-free Release build, unit tests, and full PostgreSQL integration suite pass.
- [ ] Tenant isolation, server authorization matrix, transaction rollback, appointment conflict/concurrency, bounded load, and deterministic critical journey tests pass.
- [ ] Empty-to-latest and previous-release-to-latest migrations pass; demo seed is idempotent and disabled by the production default.
- [ ] Frontend formatting, lint, type check, component/model tests, and production build pass.
- [ ] Chromium E2E validation, permission visibility, recoverable error state, responsive overflow, keyboard, and serious/critical accessibility gates pass.
- [ ] NuGet and npm report no high/critical known vulnerability; API and web image scans pass at the same threshold.
- [ ] Container health, Redis fallback/dependency behavior, migration ordering, and authenticated smoke pass.

## Database and recovery

- [ ] Migration compatibility with both current and previous application images is explicitly classified.
- [ ] A fresh encrypted PostgreSQL backup exists and its identifier/checksum is recorded.
- [ ] The last isolated restore rehearsal is inside the agreed recovery window.
- [ ] The one-shot migration job identity and target migration are recorded.
- [ ] Previous application image digests and the corrective forward-migration path are available.

## Runtime configuration

- [ ] Demo seed is disabled and no demo password is present in production configuration.
- [ ] HTTPS, secure refresh cookies, trusted origins, forwarded headers, and persistent protected Data Protection keys are configured.
- [ ] Database, Redis, telemetry, and signing credentials come from the approved secret store.
- [ ] Rate limits, health probes, log redaction, telemetry destination, retention, and alert ownership are confirmed.
- [ ] No real customer PII exists in demo data or screenshots.
- [ ] Public demo exposes only the minimum-privilege receptionist credential; privileged demo credentials are not shared.

## Promotion and verification

- [ ] Migration job completes before any new API replica receives traffic.
- [ ] Readiness and dependency health pass through the deployed ingress.
- [ ] Login, customer/service/employee reads, availability, appointment write/conflict, dashboard, outbox delivery, and audit smoke pass.
- [ ] The deployed version and image digests match the approved candidate.
- [ ] Rollback decision window, operator, and communication channel are active.
- [ ] Repository owner creates the commit, tag, push, and release only after all evidence is accepted.
- [ ] External uptime probes and backup-age/restore-age alert ownership are active.
